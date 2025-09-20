using UnityEngine;
using UnityEditor;

/// <summary>
/// 输入系统修复工具 - 修复新旧Input System冲突
/// </summary>
public class InputSystemFixer
{
    [MenuItem("Tools/研究室移动端UI/🎮 修复输入系统")]
    public static void FixInputSystem()
    {
        Debug.Log("=== 🎮 修复输入系统 ===");

        // 检查当前输入系统设置
        CheckInputSystemSettings();

        // 提供解决方案选项
        ShowInputSystemOptions();

        Debug.Log("=== 输入系统检查完成 ===");
    }

    private static void CheckInputSystemSettings()
    {
        Debug.Log("📋 检查输入系统设置:");

        // 检查输入系统状态
        Debug.Log("检查当前输入系统配置...");

        // 测试旧输入系统是否可用
        bool oldInputWorks = TestOldInputSystem();
        bool newInputWorks = TestNewInputSystem();

        Debug.Log($"旧Input系统状态: {(oldInputWorks ? "✅ 工作正常" : "❌ 不可用")}");
        Debug.Log($"新Input系统状态: {(newInputWorks ? "✅ 工作正常" : "❌ 不可用")}");

        if (!oldInputWorks && !newInputWorks)
        {
            Debug.LogError("❌ 新旧输入系统都不工作！");
        }
        else if (oldInputWorks && newInputWorks)
        {
            Debug.Log("✅ 新旧输入系统都可用 - 兼容模式");
        }
        else if (oldInputWorks)
        {
            Debug.Log("✅ 只有旧输入系统可用");
        }
        else
        {
            Debug.Log("✅ 只有新输入系统可用");
        }

        // 检查是否安装了Input System包
        CheckInputSystemPackage();
    }

    private static bool TestOldInputSystem()
    {
        try
        {
            Vector3 mousePos = Input.mousePosition;
            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    private static bool TestNewInputSystem()
    {
        try
        {
            // 尝试访问新输入系统
            var mouseType = System.Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            if (mouseType != null)
            {
                var currentProperty = mouseType.GetProperty("current");
                if (currentProperty != null)
                {
                    var mouse = currentProperty.GetValue(null);
                    return mouse != null;
                }
            }
            return false;
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    private static void CheckInputSystemPackage()
    {
        Debug.Log("📋 检查Input System包:");

        try
        {
            // 使用反射检查是否存在InputSystem相关类型
            var mouseType = System.Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            if (mouseType != null)
            {
                Debug.Log("✅ Input System包已安装");

                // 尝试获取鼠标当前状态
                var currentProperty = mouseType.GetProperty("current");
                if (currentProperty != null)
                {
                    var mouse = currentProperty.GetValue(null);
                    if (mouse != null)
                    {
                        Debug.Log("✅ 鼠标设备已检测到");

                        // 尝试获取鼠标位置
                        var positionProperty = mouse.GetType().GetProperty("position");
                        if (positionProperty != null)
                        {
                            var position = positionProperty.GetValue(mouse);
                            Debug.Log($"鼠标位置 (新Input System): {position}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ 未检测到鼠标设备");
                    }
                }
            }
            else
            {
                Debug.Log("⚠️ Input System包未安装或不可用");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ Input System包检查失败: {e.Message}");
        }
    }

    private static void ShowInputSystemOptions()
    {
        Debug.Log("🛠️ 修复选项:");
        Debug.Log("选项1: 手动在Project Settings → Player → Configuration → Active Input Handling 设置为 'Both'");
        Debug.Log("选项2: 更新代码以使用新的Input System API");
        Debug.Log("选项3: 切换回旧的Input Manager");

        // 提供手动修复指导
        EditorUtility.DisplayDialog("输入系统修复指导",
            "检测到输入系统冲突。\n\n请手动修复：\n" +
            "1. 打开 Edit → Project Settings\n" +
            "2. 选择 Player → Configuration\n" +
            "3. 将 Active Input Handling 设置为 'Both'\n" +
            "4. 重启Unity编辑器\n\n" +
            "这将同时支持新旧输入系统，解决兼容性问题。",
            "了解");
    }

    [MenuItem("Tools/研究室移动端UI/📊 输入系统状态")]
    public static void CheckInputStatus()
    {
        Debug.Log("=== 📊 输入系统状态 ===");

        // 测试旧输入系统
        try
        {
            Vector3 oldMousePos = Input.mousePosition;
            Debug.Log($"✅ 旧Input系统工作正常: {oldMousePos}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 旧Input系统失败: {e.Message}");
        }

        // 测试新输入系统
        try
        {
            var mouseType = System.Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
            if (mouseType != null)
            {
                var currentProperty = mouseType.GetProperty("current");
                if (currentProperty != null)
                {
                    var mouse = currentProperty.GetValue(null);
                    if (mouse != null)
                    {
                        var positionProperty = mouse.GetType().GetProperty("position");
                        if (positionProperty != null)
                        {
                            var position = positionProperty.GetValue(mouse);
                            Debug.Log($"✅ 新Input系统工作正常: {position}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ 新Input系统: 未检测到鼠标");
                    }
                }
            }
            else
            {
                Debug.LogWarning("⚠️ 新Input系统不可用");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 新Input系统失败: {e.Message}");
        }

        Debug.Log("=== 状态检查完成 ===");
    }
}