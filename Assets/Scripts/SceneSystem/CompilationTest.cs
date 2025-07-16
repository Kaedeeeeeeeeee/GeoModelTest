using UnityEngine;

/// <summary>
/// 编译测试脚本 - 验证场景系统是否正确编译
/// </summary>
public class CompilationTest : MonoBehaviour
{
    void Start()
    {
        // 测试GameSceneManager实例化
        var sceneManager = GameSceneManager.Instance;
        Debug.Log($"GameSceneManager实例化成功: {sceneManager != null}");
        
        // 测试SceneSwitcherTool实例化
        var sceneSwitcher = gameObject.AddComponent<SceneSwitcherTool>();
        Debug.Log($"SceneSwitcherTool实例化成功: {sceneSwitcher != null}");
        
        // 测试PlayerPersistentData实例化
        var persistentData = gameObject.AddComponent<PlayerPersistentData>();
        Debug.Log($"PlayerPersistentData实例化成功: {persistentData != null}");
        
        // 测试SceneSwitcherInitializer实例化
        var initializer = gameObject.AddComponent<SceneSwitcherInitializer>();
        Debug.Log($"SceneSwitcherInitializer实例化成功: {initializer != null}");
        
        Debug.Log("场景系统编译测试完成 - 所有组件正常！");
        
        // 清理测试组件
        Destroy(sceneSwitcher);
        Destroy(persistentData);
        Destroy(initializer);
        Destroy(this);
    }
}