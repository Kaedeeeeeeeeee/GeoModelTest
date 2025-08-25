using UnityEngine;
using SampleCuttingSystem;

/// <summary>
/// 切割游戏测试器
/// 用于验证UI组件的创建和显示
/// </summary>
public class CuttingGameTester : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private bool autoTest = true;
    [SerializeField] private float testDelay = 2f;
    
    void Start()
    {
        if (autoTest)
        {
            Invoke(nameof(RunTest), testDelay);
        }
    }
    
    [ContextMenu("运行切割游戏测试")]
    public void RunTest()
    {
        Debug.Log("=== 开始切割游戏UI测试 ===");
        
        // 查找或创建SampleCuttingGame组件
        SampleCuttingGame cuttingGame = FindObjectOfType<SampleCuttingGame>();
        
        if (cuttingGame == null)
        {
            Debug.Log("未找到SampleCuttingGame，创建新的组件...");
            GameObject gameObj = new GameObject("TestCuttingGame");
            cuttingGame = gameObj.AddComponent<SampleCuttingGame>();
        }
        
        // 等待组件初始化
        StartCoroutine(ValidateUIComponents(cuttingGame));
    }
    
    private System.Collections.IEnumerator ValidateUIComponents(SampleCuttingGame cuttingGame)
    {
        yield return new WaitForSeconds(1f); // 等待初始化完成
        
        Debug.Log("=== 验证UI组件 ===");
        
        // 验证Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        bool foundCanvas = false;
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name.Contains("CuttingGame") || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Debug.Log($"✓ 找到Canvas: {canvas.name}, 排序顺序: {canvas.sortingOrder}");
                foundCanvas = true;
                break;
            }
        }
        
        if (!foundCanvas)
        {
            Debug.LogError("✗ 未找到切割游戏Canvas");
        }
        
        // 验证切割区域
        GameObject cuttingArea = GameObject.Find("CuttingArea");
        if (cuttingArea != null)
        {
            Debug.Log($"✓ 找到切割区域: {cuttingArea.name}");
            Debug.Log($"  - 激活状态: {cuttingArea.activeInHierarchy}");
            
            // 验证子组件
            CheckChildComponent(cuttingArea, "SampleDiagram");
            CheckChildComponent(cuttingArea, "CuttingLine");
            CheckChildComponent(cuttingArea, "SuccessZone");
            CheckChildComponent(cuttingArea, "InstructionText");
            CheckChildComponent(cuttingArea, "SpaceKeyIcon");
        }
        else
        {
            Debug.LogError("✗ 未找到切割区域");
        }
        
        Debug.Log("=== 测试完成 ===");
    }
    
    private void CheckChildComponent(GameObject parent, string childName)
    {
        Transform child = parent.transform.Find(childName);
        if (child != null)
        {
            Debug.Log($"  ✓ 找到子组件: {childName}");
            Debug.Log($"    - 激活状态: {child.gameObject.activeInHierarchy}");
            Debug.Log($"    - 位置: {child.position}");
            
            if (child.GetComponent<RectTransform>() != null)
            {
                RectTransform rect = child.GetComponent<RectTransform>();
                Debug.Log($"    - 尺寸: {rect.sizeDelta}");
            }
        }
        else
        {
            Debug.LogError($"  ✗ 未找到子组件: {childName}");
        }
    }
    
    void Update()
    {
        // 按T键手动运行测试
        if (Input.GetKeyDown(KeyCode.T))
        {
            RunTest();
        }
    }
}