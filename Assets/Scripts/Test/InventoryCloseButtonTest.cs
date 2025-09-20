using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 背包关闭按钮测试脚本
/// 用于调试背包界面关闭按钮的点击问题
/// </summary>
public class InventoryCloseButtonTest : MonoBehaviour
{
    [Header("调试设置")]
    public bool enableDebugOutput = true;
    public float testInterval = 1f;

    private InventoryUI inventoryUI;
    private Button closeButton;
    private float lastTestTime;

    void Start()
    {
        // 查找InventoryUI
        inventoryUI = FindObjectOfType<InventoryUI>();
        if (inventoryUI == null)
        {
            Debug.LogError("[InventoryCloseButtonTest] 未找到InventoryUI组件！");
            return;
        }

        Debug.Log("[InventoryCloseButtonTest] 找到InventoryUI，开始监控关闭按钮");
    }

    void Update()
    {
        if (inventoryUI == null || !enableDebugOutput) return;

        // 定期检查关闭按钮状态
        if (Time.time - lastTestTime >= testInterval)
        {
            CheckCloseButtonStatus();
            lastTestTime = Time.time;
        }

        // 检查点击事件
        if (Input.GetMouseButtonDown(0))
        {
            CheckMouseClick();
        }
    }

    void CheckCloseButtonStatus()
    {
        // 通过反射或公共访问获取关闭按钮
        if (closeButton == null)
        {
            closeButton = FindCloseButton();
        }

        if (closeButton != null)
        {
            Debug.Log($"[InventoryCloseButtonTest] 关闭按钮状态 - 激活: {closeButton.gameObject.activeInHierarchy}, 可交互: {closeButton.interactable}, 名称: {closeButton.name}");

            // 检查Image组件的raycastTarget
            Image buttonImage = closeButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Debug.Log($"[InventoryCloseButtonTest] 按钮图像 - RaycastTarget: {buttonImage.raycastTarget}, 颜色: {buttonImage.color}");
            }

            // 检查Canvas层级
            Canvas buttonCanvas = closeButton.GetComponentInParent<Canvas>();
            if (buttonCanvas != null)
            {
                Debug.Log($"[InventoryCloseButtonTest] 按钮Canvas - SortingOrder: {buttonCanvas.sortingOrder}, RenderMode: {buttonCanvas.renderMode}");
            }
        }
        else
        {
            Debug.LogWarning("[InventoryCloseButtonTest] 未找到关闭按钮");
        }
    }

    Button FindCloseButton()
    {
        // 在InventoryUI的子对象中查找关闭按钮
        Button[] buttons = inventoryUI.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.name.ToLower().Contains("close") || button.name.Contains("关闭"))
            {
                Debug.Log($"[InventoryCloseButtonTest] 找到关闭按钮: {button.name}");
                return button;
            }
        }

        Debug.LogWarning($"[InventoryCloseButtonTest] 在{buttons.Length}个按钮中未找到关闭按钮");
        return null;
    }

    void CheckMouseClick()
    {
        // 检查鼠标点击位置是否命中UI元素
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        if (raycastResults.Count > 0)
        {
            foreach (var result in raycastResults)
            {
                Debug.Log($"[InventoryCloseButtonTest] 鼠标点击命中: {result.gameObject.name} (层级: {result.depth})");

                if (result.gameObject.name.ToLower().Contains("close") || result.gameObject.name.Contains("关闭"))
                {
                    Debug.Log($"[InventoryCloseButtonTest] 🎯 点击了关闭按钮相关元素: {result.gameObject.name}");

                    // 检查这个对象是否有Button组件
                    Button clickedButton = result.gameObject.GetComponent<Button>();
                    if (clickedButton != null)
                    {
                        Debug.Log($"[InventoryCloseButtonTest] ✅ 发现Button组件，可交互: {clickedButton.interactable}");

                        // 手动触发点击
                        if (clickedButton.interactable)
                        {
                            Debug.Log("[InventoryCloseButtonTest] 手动触发按钮点击");
                            clickedButton.onClick.Invoke();
                        }
                    }
                    else
                    {
                        Debug.Log($"[InventoryCloseButtonTest] ❌ 未找到Button组件");
                    }
                }
            }
        }
        else
        {
            Debug.Log("[InventoryCloseButtonTest] 鼠标点击未命中任何UI元素");
        }
    }

    void OnGUI()
    {
        if (!enableDebugOutput) return;

        GUILayout.BeginArea(new Rect(10, 650, 400, 150));
        GUILayout.Label("=== 背包关闭按钮测试 ===");

        if (inventoryUI != null)
        {
            GUILayout.Label($"背包状态: {(inventoryUI.IsInventoryOpen() ? "打开" : "关闭")}");
            GUILayout.Label($"关闭按钮: {(closeButton != null ? "找到" : "未找到")}");

            if (closeButton != null)
            {
                GUILayout.Label($"按钮可交互: {closeButton.interactable}");
                GUILayout.Label($"按钮激活: {closeButton.gameObject.activeInHierarchy}");

                if (GUILayout.Button("手动关闭背包"))
                {
                    Debug.Log("[InventoryCloseButtonTest] GUI按钮手动关闭背包");
                    inventoryUI.CloseInventory();
                }
            }
        }
        else
        {
            GUILayout.Label("InventoryUI: 未找到");
        }

        GUILayout.EndArea();
    }
}