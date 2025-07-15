using UnityEngine;
using UnityEditor;

public class GeologyTagSetup : EditorWindow
{
    [MenuItem("Tools/Geology/创建地质标签")]
    static void CreateGeologyTags()
    {
        // 需要的标签列表
        string[] requiredTags = {
            "GeologyLayer",
            "GeologicalSample", 
            "Player"
        };
        
        // 获取现有标签
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        bool tagsAdded = false;
        
        foreach (string tag in requiredTags)
        {
            // 检查标签是否已存在
            bool tagExists = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty tagProp = tagsProp.GetArrayElementAtIndex(i);
                if (tagProp.stringValue.Equals(tag))
                {
                    tagExists = true;
                    break;
                }
            }
            
            // 如果标签不存在，则添加
            if (!tagExists)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                newTagProp.stringValue = tag;
                tagsAdded = true;
                Debug.Log($"已添加标签: {tag}");
            }
            else
            {
                Debug.Log($"标签已存在: {tag}");
            }
        }
        
        if (tagsAdded)
        {
            tagManager.ApplyModifiedProperties();
            EditorUtility.DisplayDialog("完成", "地质系统所需标签已成功添加！", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("信息", "所有必需的标签都已存在。", "确定");
        }
    }
    
    [MenuItem("Tools/Geology/验证地质系统设置")]
    static void ValidateGeologySetup()
    {
        bool allValid = true;
        string issues = "";
        
        // 检查标签
        string[] requiredTags = { "GeologyLayer", "GeologicalSample", "Player" };
        foreach (string tag in requiredTags)
        {
            try
            {
                GameObject.FindGameObjectWithTag(tag);
            }
            catch (UnityException)
            {
                allValid = false;
                issues += $"• 缺少标签: {tag}\n";
            }
        }
        
        // 检查系统组件
        if (FindObjectOfType<LayerDetectionSystem>() == null)
        {
            allValid = false;
            issues += "• 缺少 LayerDetectionSystem\n";
        }
        
        if (FindObjectOfType<SampleReconstructionSystem>() == null)
        {
            allValid = false;
            issues += "• 缺少 SampleReconstructionSystem\n";
        }
        
        if (FindObjectOfType<FirstPersonController>() == null)
        {
            allValid = false;
            issues += "• 缺少 FirstPersonController\n";
        }
        
        if (allValid)
        {
            EditorUtility.DisplayDialog("验证成功", "地质系统设置完整，可以正常使用！", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("验证失败", $"发现以下问题：\n\n{issues}\n请使用相关工具修复这些问题。", "确定");
        }
    }
}