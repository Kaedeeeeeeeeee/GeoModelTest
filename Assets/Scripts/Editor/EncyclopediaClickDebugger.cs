using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Encyclopedia;
using UnityEngine.EventSystems;

public class EncyclopediaClickDebugger : EditorWindow
{
    [MenuItem("Tools/图鉴系统/点击调试器")]
    public static void ShowWindow()
    {
        GetWindow<EncyclopediaClickDebugger>("图鉴点击调试器");
    }

    private void OnGUI()
    {
        GUILayout.Label("=== 🔍 图鉴点击调试器 ===", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            GUILayout.Label("⚠️ 请先运行游戏", EditorStyles.helpBox);
            return;
        }

        GUILayout.Space(10);

        if (GUILayout.Button("📊 检查Canvas层级", GUILayout.Height(30)))
        {
            CheckCanvasLayers();
        }

        if (GUILayout.Button("🎯 检查图鉴条目按钮", GUILayout.Height(30)))
        {
            CheckEncyclopediaButtons();
        }

        if (GUILayout.Button("🖱️检查EventSystem", GUILayout.Height(30)))
        {
            CheckEventSystem();
        }

        if (GUILayout.Button("🔧 修复所有按钮事件", GUILayout.Height(30)))
        {
            FixAllButtonEvents();
        }
    }

    private void CheckCanvasLayers()
    {
        Debug.Log("=== 📊 检查Canvas层级 ===");

        var allCanvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in allCanvases)
        {
            Debug.Log($"Canvas: {canvas.name} | sortingOrder: {canvas.sortingOrder} | active: {canvas.gameObject.activeInHierarchy}");
        }

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI != null)
        {
            var canvas = encyclopediaUI.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"✅ 图鉴Canvas: {canvas.name} | sortingOrder: {canvas.sortingOrder}");
            }
            else
            {
                Debug.LogError("❌ 图鉴UI没有找到Canvas");
            }
        }
        else
        {
            Debug.LogError("❌ 没有找到EncyclopediaUI");
        }
    }

    private void CheckEncyclopediaButtons()
    {
        Debug.Log("=== 🎯 检查图鉴条目按钮 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("❌ 没有找到EncyclopediaUI");
            return;
        }

        // 查找所有条目按钮
        var entryButtons = encyclopediaUI.GetComponentsInChildren<Button>(true);
        Debug.Log($"找到 {entryButtons.Length} 个按钮");

        int validButtons = 0;
        int buttonWithEvents = 0;

        foreach (var button in entryButtons)
        {
            if (button.name.Contains("EntryItem") || button.transform.parent.name.Contains("EntryItem"))
            {
                validButtons++;

                bool isInteractable = button.interactable;
                bool hasEvents = button.onClick.GetPersistentEventCount() > 0;
                bool isActive = button.gameObject.activeInHierarchy;

                Debug.Log($"按钮: {button.name} | 可交互: {isInteractable} | 有事件: {hasEvents} | 激活: {isActive}");

                if (hasEvents) buttonWithEvents++;

                // 检查是否被其他UI遮挡
                var graphic = button.GetComponent<Graphic>();
                if (graphic != null && !graphic.raycastTarget)
                {
                    Debug.LogWarning($"⚠️ 按钮 {button.name} 的raycastTarget为false");
                }
            }
        }

        Debug.Log($"✅ 有效按钮: {validButtons}, 有事件的按钮: {buttonWithEvents}");
    }

    private void CheckEventSystem()
    {
        Debug.Log("=== 🖱️ 检查EventSystem ===");

        var eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log($"✅ EventSystem存在: {eventSystem.name} | 激活: {eventSystem.gameObject.activeInHierarchy}");

            var inputModule = eventSystem.currentInputModule;
            if (inputModule != null)
            {
                Debug.Log($"InputModule: {inputModule.GetType().Name}");
            }
            else
            {
                Debug.LogWarning("⚠️ 没有InputModule");
            }
        }
        else
        {
            Debug.LogError("❌ 没有找到EventSystem");
        }
    }

    private void FixAllButtonEvents()
    {
        Debug.Log("=== 🔧 修复所有按钮事件 ===");

        var encyclopediaUI = FindObjectOfType<EncyclopediaUI>();
        if (encyclopediaUI == null)
        {
            Debug.LogError("❌ 没有找到EncyclopediaUI");
            return;
        }

        // 使用反射调用RefreshEntryList重新创建按钮
        var refreshMethod = typeof(EncyclopediaUI).GetMethod("RefreshEntryList",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (refreshMethod != null)
        {
            Debug.Log("🔄 重新刷新图鉴条目列表");
            refreshMethod.Invoke(encyclopediaUI, null);
        }

        // 确保Canvas层级正确
        var canvas = encyclopediaUI.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.sortingOrder < 10000)
        {
            canvas.sortingOrder = 10001;
            Debug.Log($"🔧 调整Canvas层级为: {canvas.sortingOrder}");
        }
    }
}