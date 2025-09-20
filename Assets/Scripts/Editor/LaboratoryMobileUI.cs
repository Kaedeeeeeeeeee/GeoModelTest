using UnityEngine;
using UnityEditor;

public class LaboratoryMobileUI
{
    [MenuItem("Tools/研究室移动端UI/启用移动UI")]
    public static void EnableMobileUI()
    {
        Debug.Log("=== 启用研究室移动端UI ===");

        // 获取或创建MobileInputManager
        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager == null)
        {
            GameObject obj = new GameObject("MobileInputManager");
            inputManager = obj.AddComponent<MobileInputManager>();
            Debug.Log("✅ 创建了MobileInputManager");
        }

        // 启用桌面测试模式
        inputManager.EnableDesktopTestMode(true);
        Debug.Log("✅ 启用了桌面测试模式");

        // 获取或创建MobileControlsUI
        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI == null)
        {
            GameObject uiObj = new GameObject("MobileControlsUI");
            controlsUI = uiObj.AddComponent<MobileControlsUI>();
            Debug.Log("✅ 创建了MobileControlsUI");
        }

        // 强制显示
        controlsUI.forceShowOnDesktop = true;
        controlsUI.gameObject.SetActive(true);

        Debug.Log("🎉 研究室移动端UI启用完成！");
    }

    [MenuItem("Tools/研究室移动端UI/禁用移动UI")]
    public static void DisableMobileUI()
    {
        Debug.Log("=== 禁用研究室移动端UI ===");

        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager != null)
        {
            inputManager.EnableDesktopTestMode(false);
            Debug.Log("✅ 禁用了桌面测试模式");
        }

        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        if (controlsUI != null)
        {
            controlsUI.forceShowOnDesktop = false;
            controlsUI.gameObject.SetActive(false);
            Debug.Log("✅ 隐藏了移动端UI");
        }

        Debug.Log("✅ 研究室移动端UI已禁用");
    }

    [MenuItem("Tools/研究室移动端UI/检查系统状态")]
    public static void CheckStatus()
    {
        Debug.Log("=== 系统状态检查 ===");

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"当前场景: {sceneName}");

        MobileInputManager inputManager = MobileInputManager.Instance;
        Debug.Log($"MobileInputManager存在: {inputManager != null}");
        if (inputManager != null)
        {
            Debug.Log($"  桌面测试模式: {inputManager.desktopTestMode}");
        }

        MobileControlsUI controlsUI = Object.FindFirstObjectByType<MobileControlsUI>();
        Debug.Log($"MobileControlsUI存在: {controlsUI != null}");
        if (controlsUI != null)
        {
            Debug.Log($"  激活状态: {controlsUI.gameObject.activeInHierarchy}");
            Debug.Log($"  强制桌面显示: {controlsUI.forceShowOnDesktop}");
        }

        Debug.Log("=== 状态检查完成 ===");
    }
}