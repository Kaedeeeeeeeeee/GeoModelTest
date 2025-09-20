using UnityEngine;
using UnityEditor;

public class SimpleMenuTest
{
    [MenuItem("Tools/研究室移动端UI/测试菜单是否可见")]
    public static void TestMenuVisible()
    {
        Debug.Log("🎉 菜单正常显示！如果你看到这条消息，说明编辑器工具可以正常工作。");
    }

    [MenuItem("Tools/研究室移动端UI/立即启用移动端UI")]
    public static void EnableMobileUIImmediately()
    {
        Debug.Log("=== 立即启用移动端UI ===");

        // 1. 创建MobileInputManager
        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager == null)
        {
            GameObject inputManagerObj = new GameObject("MobileInputManager");
            inputManager = inputManagerObj.AddComponent<MobileInputManager>();
            Debug.Log("✅ MobileInputManager 已创建");
        }

        // 2. 启用桌面测试模式
        inputManager.EnableDesktopTestMode(true);
        Debug.Log("✅ 桌面测试模式已启用");

        // 3. 创建MobileControlsUI
        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI == null)
        {
            GameObject controlsUIObj = new GameObject("MobileControlsUI");
            controlsUI = controlsUIObj.AddComponent<MobileControlsUI>();
            Debug.Log("✅ MobileControlsUI 已创建");
        }

        // 4. 配置为强制显示
        controlsUI.forceShowOnDesktop = true;
        controlsUI.gameObject.SetActive(true);
        Debug.Log("✅ 移动端UI 已设置为强制显示");

        Debug.Log("🎉 移动端UI启用完成！你现在应该能看到虚拟控制界面了。");
    }

    [MenuItem("Tools/研究室移动端UI/立即禁用移动端UI")]
    public static void DisableMobileUIImmediately()
    {
        Debug.Log("=== 立即禁用移动端UI ===");

        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager != null)
        {
            inputManager.EnableDesktopTestMode(false);
            Debug.Log("✅ 桌面测试模式已禁用");
        }

        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI != null)
        {
            controlsUI.forceShowOnDesktop = false;
            controlsUI.gameObject.SetActive(false);
            Debug.Log("✅ 移动端UI 已隐藏");
        }

        Debug.Log("✅ 移动端UI禁用完成！");
    }
}