using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Canvas层级管理器 - 统一管理所有UI的Canvas层级
/// </summary>
public class CanvasHierarchyManager : MonoBehaviour
{
    [Header("Canvas层级配置")]
    [SerializeField] private int mobileControlsLayer = 100;   // 移动端控制UI
    [SerializeField] private int warehouseUILayer = 200;      // 仓库UI
    [SerializeField] private int inventoryUILayer = 250;      // 背包详情UI
    [SerializeField] private int sceneUILayer = 300;          // 场景切换UI

    // 单例模式
    private static CanvasHierarchyManager _instance;
    public static CanvasHierarchyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CanvasHierarchyManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("CanvasHierarchyManager");
                    _instance = obj.AddComponent<CanvasHierarchyManager>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 自动修复现有Canvas层级
        StartCoroutine(DelayedCanvasFixup());
    }

    private System.Collections.IEnumerator DelayedCanvasFixup()
    {
        // 等待一帧，确保所有UI组件都已初始化
        yield return null;

        FixAllCanvasLayers();

        // 每5秒检查一次Canvas层级，确保持续正确
        while (true)
        {
            yield return new WaitForSeconds(5f);
            ValidateCanvasLayers();
        }
    }

    /// <summary>
    /// 修复所有Canvas层级
    /// </summary>
    public void FixAllCanvasLayers()
    {
        Debug.Log("=== 修复所有Canvas层级 ===");

        // 1. 修复移动端控制UI
        FixMobileControlsCanvas();

        // 2. 修复仓库UI
        FixWarehouseCanvas();

        // 3. 修复背包UI
        FixInventoryCanvas();

        // 4. 修复场景切换UI
        FixSceneCanvas();

        // 5. 修复LookTouchArea配置
        FixLookTouchArea();

        Debug.Log("Canvas层级修复完成");
    }

    private void FixMobileControlsCanvas()
    {
        MobileControlsUI mobileControlsUI = FindFirstObjectByType<MobileControlsUI>();
        if (mobileControlsUI != null)
        {
            Canvas canvas = mobileControlsUI.GetComponent<Canvas>();
            if (canvas != null)
            {
                int oldOrder = canvas.sortingOrder;
                canvas.sortingOrder = mobileControlsLayer;
                canvas.overrideSorting = false; // 移动端UI不需要覆盖排序

                Debug.Log($"MobileControlsUI Canvas层级: {oldOrder} → {canvas.sortingOrder}");

                // 确保LookTouchArea不阻挡点击
                Transform lookTouchArea = mobileControlsUI.transform.Find("LookTouchArea");
                if (lookTouchArea != null)
                {
                    Image image = lookTouchArea.GetComponent<Image>();
                    if (image != null && image.raycastTarget)
                    {
                        image.raycastTarget = false;
                        Debug.Log("✅ 关闭LookTouchArea的raycastTarget");
                    }
                }
            }
        }
    }

    private void FixWarehouseCanvas()
    {
        WarehouseUI warehouseUI = FindFirstObjectByType<WarehouseUI>();
        if (warehouseUI != null && warehouseUI.warehouseCanvas != null)
        {
            Canvas canvas = warehouseUI.warehouseCanvas;
            int oldOrder = canvas.sortingOrder;
            canvas.sortingOrder = warehouseUILayer;
            canvas.overrideSorting = true; // 仓库UI需要覆盖排序

            // 确保GraphicRaycaster启用
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = true;
            }

            Debug.Log($"WarehouseUI Canvas层级: {oldOrder} → {canvas.sortingOrder}");
        }
    }

    private void FixInventoryCanvas()
    {
        InventoryUI inventoryUI = FindFirstObjectByType<InventoryUI>();
        if (inventoryUI != null)
        {
            // 检查InventoryUI的Canvas
            Canvas canvas = inventoryUI.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = inventoryUI.GetComponentInParent<Canvas>();
            }

            if (canvas != null)
            {
                int oldOrder = canvas.sortingOrder;
                canvas.sortingOrder = inventoryUILayer;
                canvas.overrideSorting = true; // 背包详情UI需要最高优先级

                Debug.Log($"InventoryUI Canvas层级: {oldOrder} → {canvas.sortingOrder}");
            }
        }
    }

    private void FixSceneCanvas()
    {
        // 查找场景切换相关的Canvas
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in allCanvases)
        {
            if (canvas.name.Contains("SceneSelection") || canvas.name.Contains("SceneSwitcher"))
            {
                int oldOrder = canvas.sortingOrder;
                canvas.sortingOrder = sceneUILayer;
                canvas.overrideSorting = true;

                Debug.Log($"场景UI Canvas ({canvas.name}) 层级: {oldOrder} → {canvas.sortingOrder}");
            }
        }
    }

    private void FixLookTouchArea()
    {
        // 查找所有LookTouchArea并关闭其raycastTarget
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("LookTouchArea"))
            {
                Image image = obj.GetComponent<Image>();
                if (image != null && image.raycastTarget)
                {
                    image.raycastTarget = false;
                    Debug.Log($"✅ 关闭 {obj.name} 的raycastTarget");
                }
            }
        }
    }

    /// <summary>
    /// 验证Canvas层级是否正确
    /// </summary>
    public void ValidateCanvasLayers()
    {
        bool needsFix = false;

        // 检查移动端UI
        MobileControlsUI mobileUI = FindFirstObjectByType<MobileControlsUI>();
        if (mobileUI != null)
        {
            Canvas canvas = mobileUI.GetComponent<Canvas>();
            if (canvas != null && canvas.sortingOrder != mobileControlsLayer)
            {
                needsFix = true;
            }
        }

        // 检查仓库UI
        WarehouseUI warehouseUI = FindFirstObjectByType<WarehouseUI>();
        if (warehouseUI?.warehouseCanvas != null && warehouseUI.warehouseCanvas.sortingOrder != warehouseUILayer)
        {
            needsFix = true;
        }

        if (needsFix)
        {
            Debug.LogWarning("检测到Canvas层级异常，自动修复中...");
            FixAllCanvasLayers();
        }
    }

    /// <summary>
    /// 获取推荐的Canvas层级
    /// </summary>
    public int GetRecommendedLayer(string canvasType)
    {
        switch (canvasType.ToLower())
        {
            case "mobile":
            case "mobilecontrols":
                return mobileControlsLayer;
            case "warehouse":
                return warehouseUILayer;
            case "inventory":
                return inventoryUILayer;
            case "scene":
                return sceneUILayer;
            default:
                return 150; // 默认层级
        }
    }

    [MenuItem("Tools/研究室移动端UI/🔧 立即修复Canvas层级")]
    public static void ForceFixCanvasLayers()
    {
        if (Application.isPlaying)
        {
            Instance.FixAllCanvasLayers();
        }
        else
        {
            Debug.LogWarning("请在游戏运行时使用此工具");
        }
    }

    [MenuItem("Tools/研究室移动端UI/📊 检查Canvas层级状态")]
    public static void CheckCanvasLayerStatus()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在游戏运行时使用此工具");
            return;
        }

        Debug.Log("=== Canvas层级状态检查 ===");

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        System.Array.Sort(canvases, (a, b) => a.sortingOrder.CompareTo(b.sortingOrder));

        foreach (var canvas in canvases)
        {
            string status = "✅";
            string recommendation = "";

            if (canvas.name.Contains("MobileControls") && canvas.sortingOrder != 100)
            {
                status = "❌";
                recommendation = " (推荐: 100)";
            }
            else if (canvas.name.Contains("Warehouse") && canvas.sortingOrder != 200)
            {
                status = "❌";
                recommendation = " (推荐: 200)";
            }

            Debug.Log($"{status} {canvas.name}: 层级 {canvas.sortingOrder}{recommendation}");
        }
    }
}