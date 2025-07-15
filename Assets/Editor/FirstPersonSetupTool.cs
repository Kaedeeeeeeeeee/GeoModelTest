using UnityEngine;
using UnityEditor;

public class FirstPersonSetupTool : EditorWindow
{
    [MenuItem("Tools/First Person Setup")]
    public static void ShowWindow()
    {
        GetWindow<FirstPersonSetupTool>("First Person Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("第一人称控制器一键配置工具", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("配置第一人称控制器", GUILayout.Height(40)))
        {
            SetupFirstPersonController();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("此工具将为场景中的Lily和Terrier对象配置第一人称控制器功能", MessageType.Info);
        
        EditorGUILayout.Space();
        GUILayout.Label("配置内容：", EditorStyles.boldLabel);
        GUILayout.Label("• 为Lily添加CharacterController组件");
        GUILayout.Label("• 为Lily添加FirstPersonController脚本");
        GUILayout.Label("• 创建Camera作为Lily的子对象");
        GUILayout.Label("• 创建GroundCheck空对象");
        GUILayout.Label("• 为Terrier配置碰撞体");
        GUILayout.Label("• 配置输入系统");
    }
    
    void SetupFirstPersonController()
    {
        GameObject lily = GameObject.Find("Lily");
        GameObject terrier = GameObject.Find("Terrier");
        
        if (lily == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到名为'Lily'的游戏对象！", "确定");
            return;
        }
        
        if (terrier == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到名为'Terrier'的游戏对象！", "确定");
            return;
        }
        
        Undo.RegisterCompleteObjectUndo(lily, "Setup First Person Controller");
        
        SetupLilyCharacter(lily);
        SetupTerrierGround(terrier);
        
        EditorUtility.DisplayDialog("完成", "第一人称控制器配置完成！", "确定");
    }
    
    void SetupLilyCharacter(GameObject lily)
    {
        CharacterController charController = lily.GetComponent<CharacterController>();
        if (charController == null)
        {
            charController = lily.AddComponent<CharacterController>();
            charController.center = new Vector3(0, 1, 0);
            charController.height = 2f;
            charController.radius = 0.5f;
        }
        
        FirstPersonController fpController = lily.GetComponent<FirstPersonController>();
        if (fpController == null)
        {
            fpController = lily.AddComponent<FirstPersonController>();
        }
        
        
        Transform cameraChild = lily.transform.Find("Main Camera");
        if (cameraChild == null)
        {
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.transform.SetParent(lily.transform);
            cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0);
            
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            
            AudioListener audioListener = cameraObj.AddComponent<AudioListener>();
            
            Camera existingCamera = Camera.main;
            if (existingCamera != null && existingCamera.gameObject != cameraObj)
            {
                DestroyImmediate(existingCamera.gameObject);
            }
        }
        
        Transform groundCheck = lily.transform.Find("GroundCheck");
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(lily.transform);
            groundCheckObj.transform.localPosition = new Vector3(0, 0, 0);
            
            fpController.groundCheck = groundCheckObj.transform;
        }
        else
        {
            fpController.groundCheck = groundCheck;
        }
        
        lily.layer = LayerMask.NameToLayer("Default");
        
        Debug.Log("Lily角色配置完成！");
    }
    
    void SetupTerrierGround(GameObject terrier)
    {
        MeshCollider meshCollider = terrier.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            MeshRenderer meshRenderer = terrier.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshCollider = terrier.AddComponent<MeshCollider>();
                meshCollider.convex = false;
            }
            else
            {
                MeshCollider[] childColliders = terrier.GetComponentsInChildren<MeshCollider>();
                if (childColliders.Length == 0)
                {
                    Renderer[] renderers = terrier.GetComponentsInChildren<Renderer>();
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer.gameObject.GetComponent<MeshCollider>() == null)
                        {
                            MeshCollider childCollider = renderer.gameObject.AddComponent<MeshCollider>();
                            childCollider.convex = false;
                        }
                    }
                }
            }
        }
        
        terrier.layer = LayerMask.NameToLayer("Default");
        
        Debug.Log("Terrier地面配置完成！");
    }
}