using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// 触摸手势识别系统
/// 专为地质勘探教育游戏设计的手势识别，支持样本查看、工具操作、导航等
/// </summary>
public class TouchGestureHandler : MonoBehaviour
{
    [Header("手势识别设置")]
    public bool enableGestureRecognition = true;
    public bool enableContextAware = true; // 上下文感知
    
    [Header("基础手势参数")]
    [Range(0.1f, 2.0f)]
    public float tapMaxDuration = 0.3f;
    [Range(10f, 100f)]
    public float tapMaxDistance = 30f;
    [Range(0.3f, 2.0f)]
    public float doubleTapMaxInterval = 0.5f;
    [Range(0.5f, 3.0f)]
    public float longPressMinDuration = 1.0f;
    
    [Header("滑动手势参数")]
    [Range(50f, 200f)]
    public float swipeMinDistance = 80f;
    [Range(100f, 1000f)]
    public float swipeMaxDuration = 500f; // 毫秒
    [Range(0f, 45f)]
    public float swipeMaxDeviation = 30f; // 角度偏差
    
    [Header("缩放手势参数")]
    [Range(10f, 100f)]
    public float pinchMinDistance = 20f;
    [Range(0.5f, 3.0f)]
    public float pinchSensitivity = 1.0f;
    [Range(0.1f, 10.0f)]
    public float maxZoomScale = 5.0f;
    [Range(0.1f, 1.0f)]
    public float minZoomScale = 0.2f;
    
    [Header("旋转手势参数")]
    [Range(5f, 45f)]
    public float rotateMinAngle = 15f;
    [Range(0.5f, 3.0f)]
    public float rotateSensitivity = 1.0f;
    
    [Header("边缘滑动设置")]
    [Range(20f, 100f)]
    public float edgeSwipeZone = 50f; // 边缘区域像素
    public bool enableLeftEdgeSwipe = true;
    public bool enableRightEdgeSwipe = true;
    public bool enableTopEdgeSwipe = false;
    public bool enableBottomEdgeSwipe = true;
    
    [Header("调试设置")]
    public bool enableDebugVisualization = false;
    public bool logGestureEvents = false;
    
    // 单例模式
    public static TouchGestureHandler Instance { get; private set; }
    
    // 手势事件
    public event Action<Vector2> OnTap;
    public event Action<Vector2> OnDoubleTap;
    public event Action<Vector2> OnLongPress;
    public event Action<Vector2, SwipeDirection> OnSwipe;
    public event Action<Vector2, SwipeDirection> OnEdgeSwipe;
    public event Action<Vector2, float> OnPinchStart;
    public event Action<Vector2, float, float> OnPinchUpdate; // center, scale, delta
    public event Action<Vector2, float> OnPinchEnd;
    public event Action<Vector2, float> OnRotateStart;
    public event Action<Vector2, float, float> OnRotateUpdate; // center, angle, delta
    public event Action<Vector2, float> OnRotateEnd;
    
    // 地质专用手势事件
    public event Action<Vector2> OnSampleInspect; // 长按样本查看
    public event Action<Vector2, float> OnSampleZoom; // 样本缩放
    public event Action<Vector2, float> OnSampleRotate; // 样本旋转
    public event Action<SwipeDirection> OnToolSwitch; // 滑动切换工具
    public event Action OnQuickMenu; // 三指点击快速菜单
    
    // 触摸状态
    private Dictionary<int, TouchInfo> activeTouches = new Dictionary<int, TouchInfo>();
    private List<TouchInfo> recentTaps = new List<TouchInfo>();
    
    // 手势状态
    private bool isPinching = false;
    private bool isRotating = false;
    private float lastPinchDistance = 0f;
    private float lastRotationAngle = 0f;
    private Vector2 lastPinchCenter;
    private Vector2 lastRotationCenter;
    
    // 上下文状态
    private GestureContext currentContext = GestureContext.General;
    private Camera playerCamera;
    
    public enum SwipeDirection
    {
        Left, Right, Up, Down,
        UpLeft, UpRight, DownLeft, DownRight
    }
    
    public enum GestureContext
    {
        General,        // 一般导航
        SampleViewing,  // 样本查看
        ToolSelection,  // 工具选择
        MenuNavigation, // 菜单导航
        VehicleControl  // 载具控制
    }
    
    private struct TouchInfo
    {
        public int fingerId;
        public Vector2 startPosition;
        public Vector2 currentPosition;
        public Vector2 lastPosition;
        public float startTime;
        public float lastUpdateTime;
        public bool hasMovedBeyondThreshold;
        public TouchPhase phase;
    }
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        playerCamera = Camera.main;
    }
    
    void Start()
    {
        if (!enableGestureRecognition)
        {
            enabled = false;
            return;
        }
        
        Debug.Log("[TouchGestureHandler] 手势识别系统初始化完成");
    }
    
    void Update()
    {
        if (!enableGestureRecognition || Touchscreen.current == null) return;
        
        ProcessTouchInput();
        UpdateGestureRecognition();
        CleanupOldTouches();
    }
    
    /// <summary>
    /// 处理触摸输入
    /// </summary>
    void ProcessTouchInput()
    {
        var touchscreen = Touchscreen.current;
        
        for (int i = 0; i < touchscreen.touches.Count; i++)
        {
            var touch = touchscreen.touches[i];
            if (touch == null) continue;
            
            int fingerId = touch.touchId.ReadValue();
            Vector2 position = touch.position.ReadValue();
            TouchPhase phase = touch.phase.ReadValue();
            
            switch (phase)
            {
                case TouchPhase.Began:
                    OnTouchBegan(fingerId, position);
                    break;
                    
                case TouchPhase.Moved:
                    OnTouchMoved(fingerId, position);
                    break;
                    
                case TouchPhase.Ended:
                    OnTouchEnded(fingerId, position);
                    break;
                    
                case TouchPhase.Canceled:
                    OnTouchCanceled(fingerId);
                    break;
            }
        }
    }
    
    /// <summary>
    /// 触摸开始
    /// </summary>
    void OnTouchBegan(int fingerId, Vector2 position)
    {
        TouchInfo touchInfo = new TouchInfo
        {
            fingerId = fingerId,
            startPosition = position,
            currentPosition = position,
            lastPosition = position,
            startTime = Time.time,
            lastUpdateTime = Time.time,
            hasMovedBeyondThreshold = false,
            phase = TouchPhase.Began
        };
        
        activeTouches[fingerId] = touchInfo;
        
        if (logGestureEvents)
        {
            Debug.Log($"[TouchGestureHandler] 触摸开始 ID:{fingerId} 位置:{position}");
        }
    }
    
    /// <summary>
    /// 触摸移动
    /// </summary>
    void OnTouchMoved(int fingerId, Vector2 position)
    {
        if (!activeTouches.ContainsKey(fingerId)) return;
        
        TouchInfo touchInfo = activeTouches[fingerId];
        touchInfo.lastPosition = touchInfo.currentPosition;
        touchInfo.currentPosition = position;
        touchInfo.lastUpdateTime = Time.time;
        touchInfo.phase = TouchPhase.Moved;
        
        // 检查是否超过移动阈值
        if (!touchInfo.hasMovedBeyondThreshold)
        {
            float distance = Vector2.Distance(touchInfo.startPosition, position);
            if (distance > tapMaxDistance)
            {
                touchInfo.hasMovedBeyondThreshold = true;
            }
        }
        
        activeTouches[fingerId] = touchInfo;
    }
    
    /// <summary>
    /// 触摸结束
    /// </summary>
    void OnTouchEnded(int fingerId, Vector2 position)
    {
        if (!activeTouches.ContainsKey(fingerId)) return;
        
        TouchInfo touchInfo = activeTouches[fingerId];
        touchInfo.currentPosition = position;
        touchInfo.phase = TouchPhase.Ended;
        
        ProcessTouchGesture(touchInfo);
        activeTouches.Remove(fingerId);
        
        if (logGestureEvents)
        {
            Debug.Log($"[TouchGestureHandler] 触摸结束 ID:{fingerId} 位置:{position}");
        }
    }
    
    /// <summary>
    /// 触摸取消
    /// </summary>
    void OnTouchCanceled(int fingerId)
    {
        activeTouches.Remove(fingerId);
        
        if (logGestureEvents)
        {
            Debug.Log($"[TouchGestureHandler] 触摸取消 ID:{fingerId}");
        }
    }
    
    /// <summary>
    /// 处理触摸手势
    /// </summary>
    void ProcessTouchGesture(TouchInfo touchInfo)
    {
        float duration = Time.time - touchInfo.startTime;
        float distance = Vector2.Distance(touchInfo.startPosition, touchInfo.currentPosition);
        
        // 长按手势
        if (duration >= longPressMinDuration && !touchInfo.hasMovedBeyondThreshold)
        {
            ProcessLongPress(touchInfo);
            return;
        }
        
        // 滑动手势
        if (touchInfo.hasMovedBeyondThreshold && distance >= swipeMinDistance)
        {
            if (duration * 1000f <= swipeMaxDuration) // 转换为毫秒
            {
                ProcessSwipe(touchInfo);
                return;
            }
        }
        
        // 点击手势
        if (duration <= tapMaxDuration && !touchInfo.hasMovedBeyondThreshold)
        {
            ProcessTap(touchInfo);
        }
        
        // 边缘滑动
        if (touchInfo.hasMovedBeyondThreshold && IsEdgeSwipe(touchInfo))
        {
            ProcessEdgeSwipe(touchInfo);
        }
    }
    
    /// <summary>
    /// 处理点击手势
    /// </summary>
    void ProcessTap(TouchInfo touchInfo)
    {
        // 检查双击
        bool isDoubleTap = false;
        for (int i = recentTaps.Count - 1; i >= 0; i--)
        {
            TouchInfo recentTap = recentTaps[i];
            float timeDiff = touchInfo.startTime - recentTap.startTime;
            float distance = Vector2.Distance(touchInfo.startPosition, recentTap.startPosition);
            
            if (timeDiff <= doubleTapMaxInterval && distance <= tapMaxDistance)
            {
                isDoubleTap = true;
                recentTaps.RemoveAt(i);
                break;
            }
        }
        
        if (isDoubleTap)
        {
            OnDoubleTap?.Invoke(touchInfo.startPosition);
            ProcessContextualDoubleTap(touchInfo.startPosition);
            
            if (logGestureEvents)
            {
                Debug.Log($"[TouchGestureHandler] 双击手势 位置:{touchInfo.startPosition}");
            }
        }
        else
        {
            OnTap?.Invoke(touchInfo.startPosition);
            ProcessContextualTap(touchInfo.startPosition);
            
            // 添加到最近点击列表
            recentTaps.Add(touchInfo);
            
            if (logGestureEvents)
            {
                Debug.Log($"[TouchGestureHandler] 单击手势 位置:{touchInfo.startPosition}");
            }
        }
    }
    
    /// <summary>
    /// 处理长按手势
    /// </summary>
    void ProcessLongPress(TouchInfo touchInfo)
    {
        OnLongPress?.Invoke(touchInfo.startPosition);
        
        // 地质专用：样本详细查看
        if (currentContext == GestureContext.SampleViewing || IsOverSample(touchInfo.startPosition))
        {
            OnSampleInspect?.Invoke(touchInfo.startPosition);
            
            if (logGestureEvents)
            {
                Debug.Log($"[TouchGestureHandler] 样本检查手势 位置:{touchInfo.startPosition}");
            }
        }
        
        if (logGestureEvents)
        {
            Debug.Log($"[TouchGestureHandler] 长按手势 位置:{touchInfo.startPosition}");
        }
    }
    
    /// <summary>
    /// 处理滑动手势
    /// </summary>
    void ProcessSwipe(TouchInfo touchInfo)
    {
        Vector2 direction = touchInfo.currentPosition - touchInfo.startPosition;
        SwipeDirection swipeDir = GetSwipeDirection(direction);
        
        OnSwipe?.Invoke(touchInfo.startPosition, swipeDir);
        
        // 地质专用：工具切换
        if (currentContext == GestureContext.ToolSelection || 
            (currentContext == GestureContext.General && (swipeDir == SwipeDirection.Left || swipeDir == SwipeDirection.Right)))
        {
            OnToolSwitch?.Invoke(swipeDir);
            
            if (logGestureEvents)
            {
                Debug.Log($"[TouchGestureHandler] 工具切换手势 方向:{swipeDir}");
            }
        }
        
        if (logGestureEvents)
        {
            Debug.Log($"[TouchGestureHandler] 滑动手势 方向:{swipeDir} 位置:{touchInfo.startPosition}");
        }
    }
    
    /// <summary>
    /// 处理边缘滑动
    /// </summary>
    void ProcessEdgeSwipe(TouchInfo touchInfo)
    {
        Vector2 direction = touchInfo.currentPosition - touchInfo.startPosition;
        SwipeDirection swipeDir = GetSwipeDirection(direction);
        
        OnEdgeSwipe?.Invoke(touchInfo.startPosition, swipeDir);
        
        if (logGestureEvents)
        {
            Debug.Log($"[TouchGestureHandler] 边缘滑动 方向:{swipeDir} 位置:{touchInfo.startPosition}");
        }
    }
    
    /// <summary>
    /// 更新手势识别（多点触控）
    /// </summary>
    void UpdateGestureRecognition()
    {
        int touchCount = activeTouches.Count;
        
        if (touchCount == 2)
        {
            ProcessTwoFingerGestures();
        }
        else if (touchCount == 3)
        {
            ProcessThreeFingerGestures();
        }
        else if (touchCount >= 1)
        {
            // 结束多点手势
            if (isPinching)
            {
                EndPinchGesture();
            }
            if (isRotating)
            {
                EndRotateGesture();
            }
        }
    }
    
    /// <summary>
    /// 处理双指手势
    /// </summary>
    void ProcessTwoFingerGestures()
    {
        if (activeTouches.Count != 2) return;
        
        var touches = new List<TouchInfo>(activeTouches.Values);
        Vector2 touch1 = touches[0].currentPosition;
        Vector2 touch2 = touches[1].currentPosition;
        
        Vector2 center = (touch1 + touch2) * 0.5f;
        float distance = Vector2.Distance(touch1, touch2);
        float angle = Mathf.Atan2(touch2.y - touch1.y, touch2.x - touch1.x) * Mathf.Rad2Deg;
        
        // 缩放手势
        if (!isPinching && distance >= pinchMinDistance)
        {
            StartPinchGesture(center, distance);
        }
        else if (isPinching)
        {
            UpdatePinchGesture(center, distance);
        }
        
        // 旋转手势
        if (!isRotating)
        {
            StartRotateGesture(center, angle);
        }
        else
        {
            UpdateRotateGesture(center, angle);
        }
    }
    
    /// <summary>
    /// 处理三指手势
    /// </summary>
    void ProcessThreeFingerGestures()
    {
        // 三指点击 - 快速菜单
        bool allStationary = true;
        foreach (var touch in activeTouches.Values)
        {
            if (touch.hasMovedBeyondThreshold)
            {
                allStationary = false;
                break;
            }
        }
        
        if (allStationary)
        {
            float maxDuration = 0f;
            foreach (var touch in activeTouches.Values)
            {
                float duration = Time.time - touch.startTime;
                maxDuration = Mathf.Max(maxDuration, duration);
            }
            
            if (maxDuration >= tapMaxDuration && maxDuration <= tapMaxDuration * 2)
            {
                OnQuickMenu?.Invoke();
                
                if (logGestureEvents)
                {
                    Debug.Log("[TouchGestureHandler] 三指快速菜单手势");
                }
            }
        }
    }
    
    /// <summary>
    /// 开始缩放手势
    /// </summary>
    void StartPinchGesture(Vector2 center, float distance)
    {
        isPinching = true;
        lastPinchDistance = distance;
        lastPinchCenter = center;
        
        OnPinchStart?.Invoke(center, distance);
        
        if (logGestureEvents)
        {
            Debug.Log($"[TouchGestureHandler] 缩放开始 中心:{center} 距离:{distance}");
        }
    }
    
    /// <summary>
    /// 更新缩放手势
    /// </summary>
    void UpdatePinchGesture(Vector2 center, float distance)
    {
        if (!isPinching) return;
        
        float scale = distance / lastPinchDistance;
        float deltaScale = scale - 1.0f;
        
        OnPinchUpdate?.Invoke(center, scale, deltaScale * pinchSensitivity);
        
        // 地质专用：样本缩放
        if (currentContext == GestureContext.SampleViewing || IsOverSample(center))
        {
            OnSampleZoom?.Invoke(center, deltaScale * pinchSensitivity);
        }
        
        lastPinchDistance = distance;
        lastPinchCenter = center;
    }
    
    /// <summary>
    /// 结束缩放手势
    /// </summary>
    void EndPinchGesture()
    {
        if (!isPinching) return;
        
        isPinching = false;
        OnPinchEnd?.Invoke(lastPinchCenter, lastPinchDistance);
        
        if (logGestureEvents)
        {
            Debug.Log("[TouchGestureHandler] 缩放结束");
        }
    }
    
    /// <summary>
    /// 开始旋转手势
    /// </summary>
    void StartRotateGesture(Vector2 center, float angle)
    {
        isRotating = true;
        lastRotationAngle = angle;
        lastRotationCenter = center;
        
        OnRotateStart?.Invoke(center, angle);
        
        if (logGestureEvents)
        {
            Debug.Log($"[TouchGestureHandler] 旋转开始 中心:{center} 角度:{angle}");
        }
    }
    
    /// <summary>
    /// 更新旋转手势
    /// </summary>
    void UpdateRotateGesture(Vector2 center, float angle)
    {
        if (!isRotating) return;
        
        float deltaAngle = Mathf.DeltaAngle(lastRotationAngle, angle);
        
        if (Mathf.Abs(deltaAngle) >= rotateMinAngle)
        {
            OnRotateUpdate?.Invoke(center, angle, deltaAngle * rotateSensitivity);
            
            // 地质专用：样本旋转
            if (currentContext == GestureContext.SampleViewing || IsOverSample(center))
            {
                OnSampleRotate?.Invoke(center, deltaAngle * rotateSensitivity);
            }
            
            lastRotationAngle = angle;
        }
        
        lastRotationCenter = center;
    }
    
    /// <summary>
    /// 结束旋转手势
    /// </summary>
    void EndRotateGesture()
    {
        if (!isRotating) return;
        
        isRotating = false;
        OnRotateEnd?.Invoke(lastRotationCenter, lastRotationAngle);
        
        if (logGestureEvents)
        {
            Debug.Log("[TouchGestureHandler] 旋转结束");
        }
    }
    
    #region 辅助方法
    
    /// <summary>
    /// 获取滑动方向
    /// </summary>
    SwipeDirection GetSwipeDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        angle = (angle + 360) % 360; // 标准化到0-360
        
        if (angle >= 337.5f || angle < 22.5f) return SwipeDirection.Right;
        else if (angle >= 22.5f && angle < 67.5f) return SwipeDirection.UpRight;
        else if (angle >= 67.5f && angle < 112.5f) return SwipeDirection.Up;
        else if (angle >= 112.5f && angle < 157.5f) return SwipeDirection.UpLeft;
        else if (angle >= 157.5f && angle < 202.5f) return SwipeDirection.Left;
        else if (angle >= 202.5f && angle < 247.5f) return SwipeDirection.DownLeft;
        else if (angle >= 247.5f && angle < 292.5f) return SwipeDirection.Down;
        else return SwipeDirection.DownRight;
    }
    
    /// <summary>
    /// 检查是否为边缘滑动
    /// </summary>
    bool IsEdgeSwipe(TouchInfo touchInfo)
    {
        Vector2 start = touchInfo.startPosition;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        bool isLeftEdge = enableLeftEdgeSwipe && start.x <= edgeSwipeZone;
        bool isRightEdge = enableRightEdgeSwipe && start.x >= screenWidth - edgeSwipeZone;
        bool isTopEdge = enableTopEdgeSwipe && start.y >= screenHeight - edgeSwipeZone;
        bool isBottomEdge = enableBottomEdgeSwipe && start.y <= edgeSwipeZone;
        
        return isLeftEdge || isRightEdge || isTopEdge || isBottomEdge;
    }
    
    /// <summary>
    /// 检查是否在样本上方
    /// </summary>
    bool IsOverSample(Vector2 screenPosition)
    {
        if (playerCamera == null) return false;
        
        // 射线检测是否点击到样本
        Ray ray = playerCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // 检查是否击中样本相关的对象
            GameObject hitObject = hit.collider.gameObject;
            
            // 检查标签或组件
            return hitObject.CompareTag("Sample") || 
                   hitObject.GetComponent<GeometricSampleFloating>() != null ||
                   hitObject.GetComponent<SampleItem>() != null;
        }
        
        return false;
    }
    
    /// <summary>
    /// 处理上下文相关的点击
    /// </summary>
    void ProcessContextualTap(Vector2 position)
    {
        if (!enableContextAware) return;
        
        switch (currentContext)
        {
            case GestureContext.SampleViewing:
                // 在样本查看模式下，点击可能是选择或确认
                break;
                
            case GestureContext.ToolSelection:
                // 在工具选择模式下，点击选择工具
                break;
                
            case GestureContext.VehicleControl:
                // 在载具控制模式下，点击可能是导航点
                break;
        }
    }
    
    /// <summary>
    /// 处理上下文相关的双击
    /// </summary>
    void ProcessContextualDoubleTap(Vector2 position)
    {
        if (!enableContextAware) return;
        
        switch (currentContext)
        {
            case GestureContext.SampleViewing:
                // 双击进入详细查看模式
                OnSampleInspect?.Invoke(position);
                break;
                
            case GestureContext.General:
                // 通用模式下，双击可能是快速操作
                if (IsOverSample(position))
                {
                    OnSampleInspect?.Invoke(position);
                }
                break;
        }
    }
    
    /// <summary>
    /// 清理旧的触摸记录
    /// </summary>
    void CleanupOldTouches()
    {
        float currentTime = Time.time;
        
        // 清理旧的点击记录
        for (int i = recentTaps.Count - 1; i >= 0; i--)
        {
            if (currentTime - recentTaps[i].startTime > doubleTapMaxInterval * 2)
            {
                recentTaps.RemoveAt(i);
            }
        }
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 设置手势上下文
    /// </summary>
    public void SetGestureContext(GestureContext context)
    {
        currentContext = context;
        
        if (logGestureEvents)
        {
            Debug.Log($"[TouchGestureHandler] 手势上下文切换: {context}");
        }
    }
    
    /// <summary>
    /// 获取当前手势上下文
    /// </summary>
    public GestureContext GetCurrentContext()
    {
        return currentContext;
    }
    
    /// <summary>
    /// 启用/禁用手势识别
    /// </summary>
    public void SetGestureRecognition(bool enabled)
    {
        enableGestureRecognition = enabled;
        
        if (!enabled)
        {
            // 清理所有活跃触摸
            activeTouches.Clear();
            recentTaps.Clear();
            isPinching = false;
            isRotating = false;
        }
        
        Debug.Log($"[TouchGestureHandler] 手势识别: {(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 强制结束所有手势
    /// </summary>
    public void CancelAllGestures()
    {
        activeTouches.Clear();
        recentTaps.Clear();
        
        if (isPinching)
        {
            EndPinchGesture();
        }
        
        if (isRotating)
        {
            EndRotateGesture();
        }
        
        Debug.Log("[TouchGestureHandler] 所有手势已取消");
    }
    
    /// <summary>
    /// 获取当前活跃触摸数量
    /// </summary>
    public int GetActiveTouchCount()
    {
        return activeTouches.Count;
    }
    
    #endregion
    
    #region 调试功能
    
    void OnGUI()
    {
        if (!enableDebugVisualization) return;
        
        GUILayout.BeginArea(new Rect(10, 820, 400, 200));
        GUILayout.Label("=== 手势识别调试 ===");
        GUILayout.Label($"手势识别: {enableGestureRecognition}");
        GUILayout.Label($"当前上下文: {currentContext}");
        GUILayout.Label($"活跃触摸: {activeTouches.Count}");
        GUILayout.Label($"缩放中: {isPinching}");
        GUILayout.Label($"旋转中: {isRotating}");
        GUILayout.Label($"最近点击: {recentTaps.Count}");
        
        if (GUILayout.Button("切换手势识别"))
        {
            SetGestureRecognition(!enableGestureRecognition);
        }
        
        if (GUILayout.Button("取消所有手势"))
        {
            CancelAllGestures();
        }
        
        GUILayout.EndArea();
        
        // 可视化触摸点
        if (enableDebugVisualization && activeTouches.Count > 0)
        {
            foreach (var touch in activeTouches.Values)
            {
                Vector2 pos = touch.currentPosition;
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(pos.x - 10, Screen.height - pos.y - 10, 20, 20), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }
    }
    
    #endregion
}