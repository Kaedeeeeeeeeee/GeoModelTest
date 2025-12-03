using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace WorkbenchSystem
{
    public class WorkbenchController : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("The object to interact with (e.g., shelf (8))")]
        public Transform interactionTarget;
        [Tooltip("Distance required to interact")]
        public float interactionDistance = 4.5f;
        [Tooltip("Key to press for interaction")]
        public Key interactionKey = Key.E;

        [Header("Camera Settings")]
        [Tooltip("The camera to switch to when interacting")]
        public Camera workbenchCamera;
        
        [Header("UI Settings")]
        [Tooltip("Optional: UI object to show when close enough")]
        public GameObject interactionHintUI;

        [Header("Microscope Settings")]
        [Tooltip("工作台上的显微镜控制器引用")]
        public MicroscopeController microscopeController;
        [Tooltip("显微镜物体名称（用来自动查找）")]
        public string microscopeObjectName = "Microscope";
        [Tooltip("用于鼠标点击的射线检测层，默认全部")]
        public LayerMask workbenchInteractMask = ~0;

        private FirstPersonController playerController;
        private Camera mainCamera;
        private bool isInteracting = false;
        private bool isPlayerInRange = false;
        private float nextResolveTime;
        private const float referenceRefreshInterval = 0.5f;
        private const string defaultCameraName = "WorkbenchCamera";
        private float nextMissingLogTime;
        private const float missingLogInterval = 3f;
        private MicroscopeController hoveredMicroscope;

        private void Start()
        {
            ResolveReferences(true);
            EnsureInteractionPrompt();

            // Ensure workbench camera is disabled at start
            if (workbenchCamera != null)
            {
                workbenchCamera.gameObject.SetActive(false);
                workbenchCamera.enabled = false;
            }
            
            if (interactionHintUI != null)
            {
                interactionHintUI.SetActive(false);
            }
        }

        void ResolveReferences(bool isInitial = false)
        {
            nextResolveTime = Time.time + referenceRefreshInterval;

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<FirstPersonController>(FindObjectsInactive.Include);
                if (playerController == null)
                {
                    GameObject playerObj = GameObject.FindWithTag("Player");
                    if (playerObj == null)
                    {
                        playerObj = GameObject.Find("Lily");
                    }

                    if (playerObj != null)
                    {
                        playerController = playerObj.GetComponentInChildren<FirstPersonController>(true);
                    }
                }

                if (playerController == null)
                {
                    var allPlayers = FindObjectsByType<FirstPersonController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (allPlayers.Length > 0)
                    {
                        playerController = allPlayers[0];
                    }
                }

                if (playerController != null)
                {
                    mainCamera = playerController.GetComponentInChildren<Camera>(true);
                    if (mainCamera == null)
                    {
                        mainCamera = Camera.main;
                    }

                    if (!isInitial) Debug.Log("WorkbenchController: 重新获取到玩家引用");
                }
                else if (isInitial)
                {
                    Debug.LogError("WorkbenchController: Could not find FirstPersonController in the scene.");
                }
            }

            if (interactionTarget == null)
            {
                GameObject obj = GameObject.Find("shelf (8)");
                if (obj != null)
                {
                    interactionTarget = obj.transform;
                    if (!isInitial) Debug.Log("WorkbenchController: 重新绑定到 'shelf (8)'");
                }
                else if (isInitial)
                {
                    Debug.LogWarning("WorkbenchController: Interaction Target is not assigned and 'shelf (8)' was not found.");
                }
                else
                {
                    // 兼容命名差异/克隆（包含未激活对象）
                    var allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var t in allTransforms)
                    {
                        string lower = t.name.ToLowerInvariant().Replace(" ", "");
                        if (lower.Contains("shelf") && lower.Contains("8"))
                        {
                            interactionTarget = t;
                            Debug.Log("WorkbenchController: 通过模糊匹配找到 shelf (8)");
                            break;
                        }
                    }
                }
            }

            EnsureWorkbenchCamera(isInitial);
            EnsureMicroscopeReference();
            EnsureInteractionPrompt();
        }

        private void Update()
        {
            if (Time.time >= nextResolveTime)
            {
                ResolveReferences();
            }

            if (interactionTarget == null)
            {
                if (Time.time >= nextMissingLogTime)
                {
                    nextMissingLogTime = Time.time + missingLogInterval;
                    if (interactionTarget == null) Debug.LogWarning("WorkbenchController: 未找到交互目标 shelf (8)");
                }
                return;
            }
            EnsureWorkbenchCamera();

            // Check distance
            Transform playerTransform = GetPlayerTransform();
            if (playerTransform == null)
            {
                if (Time.time >= nextMissingLogTime)
                {
                    nextMissingLogTime = Time.time + missingLogInterval;
                    Debug.LogWarning("WorkbenchController: 未找到玩家 Transform，尝试使用主摄像机或其他玩家对象");
                }
                return;
            }

            float distance = Vector3.Distance(playerTransform.position, interactionTarget.position);
            isPlayerInRange = distance <= interactionDistance;

            // Handle UI Hint
            if (interactionHintUI != null)
            {
                interactionHintUI.SetActive(isPlayerInRange && !isInteracting);
            }

            // 没有键盘输入时不处理交互，但仍保持提示刷新
            if (Keyboard.current == null) return;

            // 工作台内的鼠标点击交互（显微镜）
            if (isInteracting)
            {
                UpdateHoverMicroscope();
                HandleWorkbenchClick();
            }

            // Handle Input
            if (isPlayerInRange && !isInteracting)
            {
                var keyControl = Keyboard.current[interactionKey];
                if (keyControl != null && keyControl.wasPressedThisFrame)
                {
                    EnterWorkbench();
                }
            }
            else if (isInteracting)
            {
                // Press Escape or E to exit? usually Escape or same key.
                // User didn't specify exit key, but E or Esc is standard.
                bool escapePressed = Keyboard.current[Key.Escape] != null && Keyboard.current[Key.Escape].wasPressedThisFrame;
                var keyControl = Keyboard.current[interactionKey];
                bool interactionPressed = keyControl != null && keyControl.wasPressedThisFrame;
                if (escapePressed || interactionPressed)
                {
                    ExitWorkbench();
                }
            }
        }

        public void EnterWorkbench()
        {
            EnsureWorkbenchCamera();

            isInteracting = true;

            // Disable player control
            playerController.enabled = false;
            
            // Switch cameras
            if (mainCamera != null)
            {
                mainCamera.gameObject.SetActive(false);
                mainCamera.enabled = false;
            }

            if (workbenchCamera != null)
            {
                workbenchCamera.gameObject.SetActive(true);
                workbenchCamera.enabled = true;
            }
            else
            {
                Debug.LogError("WorkbenchController: Workbench Camera is not assigned!");
                ExitWorkbench();
                return;
            }

            // Unlock cursor for workbench interaction (if needed)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            Debug.Log("Entered Workbench View");
        }

        public void ExitWorkbench()
        {
            isInteracting = false;

            // Re-enable player control
            playerController.enabled = true;

            // Switch cameras
            if (workbenchCamera != null)
            {
                workbenchCamera.enabled = false;
                workbenchCamera.gameObject.SetActive(false);
            }

            if (mainCamera != null)
            {
                mainCamera.gameObject.SetActive(true);
                mainCamera.enabled = true;
            }

            // Lock cursor back (FirstPersonController might handle this in Update, but good to be explicit)
            // FirstPersonController.Update calls HandleInput which might re-lock, but let's see.
            // The FPC has a method UpdateCursorLockState or similar?
            // It has ToggleCursorLock, but we want to force lock.
            // Looking at FPC code: "SetCursorLockState" is private.
            // But in Update it calls CheckDesktopTestModeChange -> SetCursorLockState.
            // We might need to manually lock it here.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("Exited Workbench View");
        }
        
        void EnsureWorkbenchCamera(bool isInitial = false)
        {
            if (workbenchCamera != null) return;

            // Try find active object first
            GameObject camObj = GameObject.Find(defaultCameraName);
            if (camObj == null && interactionTarget != null)
            {
                // Include inactive children
                var cams = interactionTarget.GetComponentsInChildren<Camera>(true);
                foreach (var cam in cams)
                {
                    if (cam != null)
                    {
                        camObj = cam.gameObject;
                        break;
                    }
                }
            }

            if (camObj == null)
            {
                // Fallback: find by name across inactive objects
                var allCameras = Resources.FindObjectsOfTypeAll<Camera>();
                foreach (var cam in allCameras)
                {
                    if (cam.name == defaultCameraName)
                    {
                        camObj = cam.gameObject;
                        break;
                    }
                }
            }

            if (camObj != null)
            {
                workbenchCamera = camObj.GetComponent<Camera>();
                if (workbenchCamera != null && !isInteracting)
                {
                    workbenchCamera.gameObject.SetActive(false);
                    workbenchCamera.enabled = false;
                }

                if (!isInitial) Debug.Log("WorkbenchController: 重新绑定到 WorkbenchCamera");
                return;
            }

            // Create a default camera if still missing and we have a target
            if (interactionTarget != null)
            {
                workbenchCamera = CreateDefaultCamera(interactionTarget);
                if (workbenchCamera != null && !isInitial)
                {
                    Debug.Log("WorkbenchController: 自动创建默认工作台摄像机");
                }
            }
            else if (isInitial)
            {
                Debug.LogError("WorkbenchController: Workbench Camera is not assigned and no target to create from!");
            }
        }

        Camera CreateDefaultCamera(Transform target)
        {
            if (target == null) return null;

            var camObj = new GameObject(defaultCameraName);
            var cam = camObj.AddComponent<Camera>();

            // Calculate bounds from renderers/colliders under target
            Bounds bounds = new Bounds(target.position, Vector3.one);
            bool hasBounds = false;

            var renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (!hasBounds)
            {
                var colliders = target.GetComponentsInChildren<Collider>(true);
                foreach (var c in colliders)
                {
                    if (!hasBounds)
                    {
                        bounds = c.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(c.bounds);
                    }
                }
            }

            if (!hasBounds)
            {
                bounds = new Bounds(target.position + Vector3.up * 0.8f, new Vector3(1.5f, 1f, 1.5f));
            }

            Vector3 center = bounds.center;
            float horizontalExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
            float margin = Mathf.Max(0.25f, horizontalExtent * 0.35f);
            float lookDistance = Mathf.Max(1.5f, horizontalExtent + margin);
            float height = Mathf.Max(bounds.size.y, 0.6f) + lookDistance * 0.75f;

            // 透视相机，参数贴近主角视角
            float defaultFov = 60f;
            if (Camera.main != null)
            {
                defaultFov = Camera.main.fieldOfView;
            }

            cam.orthographic = false;
            cam.fieldOfView = defaultFov;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = Mathf.Max(50f, (height + lookDistance) * 4f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.07f, 1f);
            cam.cullingMask = ~0;

            // 使用用户提供的视角（本地变换），方便自由微调
            cam.transform.SetParent(target, false);
            cam.transform.localPosition = new Vector3(0f, 1.19799995f, -0.545000017f);
            cam.transform.localRotation = new Quaternion(0.47813347f, 0f, 0f, 0.878287137f);
            cam.transform.localScale = new Vector3(0.563881993f, 0.769636571f, 0.769241333f);

            cam.gameObject.SetActive(false);
            cam.enabled = false;

            return cam;
        }

        Transform GetPlayerTransform()
        {
            if (playerController != null) return playerController.transform;
            if (mainCamera != null) return mainCamera.transform;
            return null;
        }

        void EnsureMicroscopeReference()
        {
            if (microscopeController != null) return;

            Transform searchRoot = interactionTarget != null ? interactionTarget : transform;

            var micro = searchRoot.GetComponentInChildren<MicroscopeController>(true);
            if (micro != null)
            {
                microscopeController = micro;
                return;
            }

            if (!string.IsNullOrEmpty(microscopeObjectName))
            {
                string lowerName = microscopeObjectName.ToLowerInvariant();
                var allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var t in allTransforms)
                {
                    string current = t.name.ToLowerInvariant();
                    if (current.Contains(lowerName))
                    {
                        micro = t.GetComponentInParent<MicroscopeController>(true);
                        if (micro == null)
                        {
                            micro = t.GetComponent<MicroscopeController>();
                        }
                        if (micro != null)
                        {
                            microscopeController = micro;
                            break;
                        }
                    }
                }
            }
        }

        void HandleWorkbenchClick()
        {
            // 若指针在UI上，忽略场景点击
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (workbenchCamera == null) return;

            Ray ray = workbenchCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 5f, workbenchInteractMask))
            {
                var micro = hit.collider.GetComponentInParent<MicroscopeController>();
                if (micro == null && microscopeController != null)
                {
                    // 若命中其子级，尝试使用已绑定的控制器
                    if (hit.collider.transform.IsChildOf(microscopeController.transform))
                    {
                        micro = microscopeController;
                    }
                }

                if (micro != null)
                {
                    micro.HandleClick();
                }
            }
        }

        void UpdateHoverMicroscope()
        {
            MicroscopeController current = null;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                SetHoveredMicroscope(null);
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null || workbenchCamera == null) return;

            Ray ray = workbenchCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 5f, workbenchInteractMask))
            {
                current = hit.collider.GetComponentInParent<MicroscopeController>();
                if (current == null && microscopeController != null)
                {
                    if (hit.collider.transform.IsChildOf(microscopeController.transform))
                    {
                        current = microscopeController;
                    }
                }
            }

            SetHoveredMicroscope(current);
        }

        void SetHoveredMicroscope(MicroscopeController micro)
        {
            if (hoveredMicroscope == micro) return;

            if (hoveredMicroscope != null)
            {
                hoveredMicroscope.SetHighlight(false);
            }

            hoveredMicroscope = micro;

            if (hoveredMicroscope != null)
            {
                hoveredMicroscope.SetHighlight(true);
            }
        }

        void EnsureInteractionPrompt()
        {
            if (interactionHintUI != null) return;

            // 创建一个简洁的灰色提示背景，放在屏幕下方
            GameObject canvasObj = new GameObject("WorkbenchPromptCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 180;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject panelObj = new GameObject("WorkbenchPrompt");
            panelObj.transform.SetParent(canvasObj.transform, false);

            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.2f);
            rect.anchorMax = new Vector2(0.5f, 0.2f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(320f, 70f);

            Image bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.12f, 0.78f); // 深灰半透明

            GameObject textObj = new GameObject("PromptText");
            textObj.transform.SetParent(panelObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.text = $"按 {interactionKey} 键 进入工作台";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            interactionHintUI = panelObj;
            interactionHintUI.SetActive(false);
        }

        // Simple GUI for debugging if no UI assigned
        private void OnGUI()
        {
            if (interactionHintUI == null && isPlayerInRange && !isInteracting)
            {
                GUIStyle style = new GUIStyle();
                style.fontSize = 24;
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 50, 200, 50), $"Press {interactionKey} to Interact", style);
            }
        }
    }
}
