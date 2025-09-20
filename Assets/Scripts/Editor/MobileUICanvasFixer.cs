using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// 移动端UI Canvas修复工具
/// 专门用于修复Canvas显示在世界空间而非屏幕覆盖的问题
/// </summary>
public class MobileUICanvasFixer : EditorWindow
{
    [MenuItem("Tools/移动端UI/🔧 Canvas修复工具")]
    public static void ShowWindow()
    {
        MobileUICanvasFixer window = GetWindow<MobileUICanvasFixer>();
        window.titleContent = new GUIContent("Canvas修复工具");
        window.minSize = new Vector2(350, 300);
        window.Show();
    }
    
    void OnGUI()
    {
        DrawHeader();
        DrawFixButtons();
        DrawCanvasStatus();
    }
    
    void DrawHeader()
    {
        EditorGUILayout.Space();
        
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        
        EditorGUILayout.LabelField("移动端UI Canvas修复工具", headerStyle);
        EditorGUILayout.LabelField("修复UI显示在世界空间的问题", EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
    }
    
    void DrawFixButtons()
    {
        EditorGUILayout.LabelField("🔧 修复操作", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("🎯 修复MobileControlsUI Canvas", GUILayout.Height(40)))
        {
            FixMobileControlsUICanvas();
        }
        EditorGUILayout.LabelField("    将MobileControlsUI设置为屏幕覆盖模式", EditorStyles.miniLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("🌍 检查所有Canvas", GUILayout.Height(40)))
        {
            CheckAllCanvases();
        }
        EditorGUILayout.LabelField("    检查场景中所有Canvas的设置", EditorStyles.miniLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("🔄 重新创建MobileControlsUI", GUILayout.Height(40)))
        {
            RecreateeMobileControlsUI();
        }
        EditorGUILayout.LabelField("    删除现有并重新创建MobileControlsUI", EditorStyles.miniLabel);
        EditorGUILayout.Space();
    }
    
    void DrawCanvasStatus()
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.LabelField("📊 Canvas状态", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        if (mobileControls != null)
        {
            var canvas = mobileControls.GetComponent<Canvas>();
            if (canvas != null)
            {
                EditorGUILayout.LabelField($"MobileControlsUI Canvas:");
                EditorGUILayout.LabelField($"  • 渲染模式: {canvas.renderMode}");
                EditorGUILayout.LabelField($"  • 排序层级: {canvas.sortingOrder}");
                EditorGUILayout.LabelField($"  • 位置: {mobileControls.transform.position}");
                EditorGUILayout.LabelField($"  • 缩放: {mobileControls.transform.localScale}");
                EditorGUILayout.LabelField($"  • 父对象: {(mobileControls.transform.parent != null ? mobileControls.transform.parent.name : "无")}");
                
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    EditorGUILayout.HelpBox("❌ 渲染模式不正确！应该是 ScreenSpaceOverlay", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("✅ Canvas设置正确", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.LabelField("❌ MobileControlsUI没有Canvas组件");
            }
        }
        else
        {
            EditorGUILayout.LabelField("❌ 未找到MobileControlsUI");
        }
        
        EditorGUILayout.Space();
        
        // 显示所有Canvas
        var allCanvases = FindObjectsOfType<Canvas>();
        EditorGUILayout.LabelField($"场景中的所有Canvas ({allCanvases.Length}个):");
        foreach (var canvas in allCanvases)
        {
            string status = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? "✅" : "⚠️";
            EditorGUILayout.LabelField($"  {status} {canvas.gameObject.name}: {canvas.renderMode} (层级:{canvas.sortingOrder})");
        }
    }
    
    void FixMobileControlsUICanvas()
    {
        var mobileControls = FindObjectOfType<MobileControlsUI>();
        if (mobileControls == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到MobileControlsUI组件！", "确定");
            return;
        }
        
        // 确保MobileControlsUI在根级别（没有父对象）
        if (mobileControls.transform.parent != null)
        {
            Debug.Log($"[MobileUICanvasFixer] 将MobileControlsUI从 {mobileControls.transform.parent.name} 移到根级别");
            mobileControls.transform.SetParent(null);
        }
        
        // 重置transform
        mobileControls.transform.position = Vector3.zero;
        mobileControls.transform.rotation = Quaternion.identity;
        mobileControls.transform.localScale = Vector3.one;
        
        // 获取或添加Canvas组件
        var canvas = mobileControls.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = mobileControls.gameObject.AddComponent<Canvas>();
        }
        
        // 强制设置为屏幕覆盖模式
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        // 确保有GraphicRaycaster
        if (mobileControls.GetComponent<GraphicRaycaster>() == null)
        {
            mobileControls.gameObject.AddComponent<GraphicRaycaster>();
        }
        
        // 设置CanvasScaler
        var scaler = mobileControls.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = mobileControls.gameObject.AddComponent<CanvasScaler>();
        }
        
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // 标记为已修改
        EditorUtility.SetDirty(mobileControls);
        EditorUtility.SetDirty(canvas);
        if (scaler != null) EditorUtility.SetDirty(scaler);
        
        Debug.Log("[MobileUICanvasFixer] MobileControlsUI Canvas已修复");
        EditorUtility.DisplayDialog("修复完成", 
            "MobileControlsUI Canvas已修复！\\n\\n现在应该显示在屏幕覆盖层上了。\\n请运行游戏查看效果。", 
            "确定");
    }
    
    void CheckAllCanvases()
    {
        var allCanvases = FindObjectsOfType<Canvas>();
        
        Debug.Log("=== Canvas检查报告 ===");
        foreach (var canvas in allCanvases)
        {
            Debug.Log($"Canvas: {canvas.gameObject.name}");
            Debug.Log($"  渲染模式: {canvas.renderMode}");
            Debug.Log($"  排序层级: {canvas.sortingOrder}");
            Debug.Log($"  位置: {canvas.transform.position}");
            Debug.Log($"  父对象: {(canvas.transform.parent != null ? canvas.transform.parent.name : "无")}");
            Debug.Log("---");
        }
        
        EditorUtility.DisplayDialog("检查完成", 
            $"已检查 {allCanvases.Length} 个Canvas。\\n详细信息请查看Console。", 
            "确定");
    }
    
    void RecreateeMobileControlsUI()
    {
        if (EditorUtility.DisplayDialog("确认重新创建", 
            "这将删除现有的MobileControlsUI并重新创建。\\n\\n确定要继续吗？", 
            "确定", "取消"))
        {
            // 删除现有的MobileControlsUI
            var existing = FindObjectOfType<MobileControlsUI>();
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }
            
            // 创建新的MobileControlsUI
            GameObject mobileControlsObj = new GameObject("MobileControlsUI");
            var mobileControls = mobileControlsObj.AddComponent<MobileControlsUI>();
            
            // 强制设置为测试模式
            mobileControls.forceShowOnDesktop = true;
            mobileControls.autoHideOnDesktop = false;
            
            Debug.Log("[MobileUICanvasFixer] MobileControlsUI已重新创建");
            EditorUtility.DisplayDialog("重新创建完成", 
                "MobileControlsUI已重新创建！\\n\\n已自动启用桌面测试模式。\\n请运行游戏查看效果。", 
                "确定");
        }
    }
}