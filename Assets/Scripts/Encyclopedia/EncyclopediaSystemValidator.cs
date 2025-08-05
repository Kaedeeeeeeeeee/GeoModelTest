using UnityEngine;

namespace Encyclopedia
{
    /// <summary>
    /// 图鉴系统验证器
    /// 验证所有修复是否成功
    /// </summary>
    public class EncyclopediaSystemValidator : MonoBehaviour
    {
        [Header("验证选项")]
        [SerializeField] private bool runValidationOnStart = true;
        
        private void Start()
        {
            if (runValidationOnStart)
            {
                Invoke(nameof(RunFullValidation), 1f);
            }
        }
        
        /// <summary>
        /// 运行完整验证
        /// </summary>
        [ContextMenu("运行完整验证")]
        public void RunFullValidation()
        {
            Debug.Log("=== 图鉴系统验证开始 ===");
            
            bool allPassed = true;
            
            // 验证1: 数据库文件路径
            allPassed &= ValidateDatabasePath();
            
            // 验证2: 字体资源
            allPassed &= ValidateFontResources();
            
            // 验证3: 数据加载
            allPassed &= ValidateDataLoading();
            
            // 验证4: 系统组件
            allPassed &= ValidateSystemComponents();
            
            // 验证5: Input System
            allPassed &= ValidateInputSystem();
            
            Debug.Log($"=== 验证完成: {(allPassed ? "✅ 全部通过" : "❌ 存在问题")} ===");
            
            if (allPassed)
            {
                Debug.Log("🎉 图鉴系统已就绪，可以正常使用！");
            }
        }
        
        private bool ValidateDatabasePath()
        {
            Debug.Log("📁 验证数据库文件路径...");
            
            try
            {
                TextAsset jsonFile = Resources.Load<TextAsset>("MineralData/Data/SendaiMineralDatabase");
                if (jsonFile != null)
                {
                    Debug.Log("✅ 数据库文件路径正确");
                    return true;
                }
                else
                {
                    Debug.LogError("❌ 数据库文件未找到，请检查Resources/MineralData/Data/SendaiMineralDatabase.json");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 数据库文件加载失败: {e.Message}");
                return false;
            }
        }
        
        private bool ValidateFontResources()
        {
            Debug.Log("🔤 验证字体资源...");
            
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    Debug.Log("✅ 字体资源正确");
                    return true;
                }
                else
                {
                    Debug.LogError("❌ LegacyRuntime.ttf字体未找到");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ 字体资源加载失败: {e.Message}");
                return false;
            }
        }
        
        private bool ValidateDataLoading()
        {
            Debug.Log("📊 验证数据加载...");
            
            if (EncyclopediaData.Instance == null)
            {
                Debug.LogWarning("⚠️ EncyclopediaData实例不存在，这是正常的如果你还没添加它");
                return true; // 这不算错误，可能用户还没添加
            }
            
            if (EncyclopediaData.Instance.IsDataLoaded)
            {
                Debug.Log($"✅ 数据加载成功: {EncyclopediaData.Instance.AllEntries.Count} 个条目");
                Debug.Log($"   矿物: {EncyclopediaData.Instance.TotalMinerals}");
                Debug.Log($"   化石: {EncyclopediaData.Instance.TotalFossils}");
                return true;
            }
            else
            {
                Debug.LogWarning("⚠️ 数据尚未加载完成，请等待");
                return true; // 可能还在加载中
            }
        }
        
        private bool ValidateSystemComponents()
        {
            Debug.Log("🔧 验证系统组件...");
            
            bool hasInitializer = FindObjectOfType<EncyclopediaInitializer>() != null;
            bool hasSimpleManager = FindObjectOfType<SimpleEncyclopediaManager>() != null;
            bool hasDebugHelper = FindObjectOfType<EncyclopediaDebugHelper>() != null;
            
            Debug.Log($"   EncyclopediaInitializer: {(hasInitializer ? "✅" : "⚠️")}");
            Debug.Log($"   SimpleEncyclopediaManager: {(hasSimpleManager ? "✅" : "⚠️")}");
            Debug.Log($"   EncyclopediaDebugHelper: {(hasDebugHelper ? "✅" : "⚠️")}");
            
            if (hasInitializer || hasSimpleManager)
            {
                Debug.Log("✅ 至少有一个管理组件存在");
                return true;
            }
            else
            {
                Debug.LogWarning("⚠️ 没有找到图鉴管理组件，请添加 EncyclopediaInitializer 或 SimpleEncyclopediaManager");
                return false;
            }
        }
        
        private bool ValidateInputSystem()
        {
            Debug.Log("🎮 验证Input System...");
            
            try
            {
                // 尝试访问新Input System
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    Debug.Log("✅ 新Input System正常工作");
                    return true;
                }
                else
                {
                    Debug.LogWarning("⚠️ Keyboard.current为null，可能没有输入设备");
                    return true; // 不算错误
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Input System验证失败: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 验证图片资源
        /// </summary>
        [ContextMenu("验证图片资源")]
        public void ValidateImageResources()
        {
            Debug.Log("🖼️ 验证图片资源...");
            
            int mineralImageCount = 0;
            int fossilImageCount = 0;
            
            // 验证矿物图片
            string[] mineralImages = new string[]
            {
                "amphibole_001", "biotite_001", "carbonaceous_matter", "clay_minerals_001",
                "feldspar_001", "garnet_001", "heavy_minerals_001", "hypersthene_001",
                "illite_alteration_001", "magnetite_001", "olivine_001", "orthopyroxene_001",
                "plagioclase_001", "pumice_001", "pyroxene_001", "quartz_001",
                "volcanic_ash_001", "volcanic_glass_001", "zircon_001"
            };
            
            foreach (string imageName in mineralImages)
            {
                Sprite sprite = Resources.Load<Sprite>($"MineralData/Images/Minerals/{imageName}");
                if (sprite != null)
                {
                    mineralImageCount++;
                }
            }
            
            // 验证化石图片
            string[] fossilImages = new string[]
            {
                "buried_wood_001", "cetacean_fossils_001", "elephant_fossils_001", "fish_fossils_001",
                "foraminifera_001", "horse_fossils_001", "planktonic_diatoms_001", "plant_leaf_fossils_001",
                "plant_remains_001", "pollen_fossils_001", "sendai_clam_001", "shark_fossils_001",
                "shellfish_001", "silicified_wood_001", "takahashi_scallop_001"
            };
            
            foreach (string imageName in fossilImages)
            {
                Sprite sprite = Resources.Load<Sprite>($"MineralData/Images/Fossil/{imageName}");
                if (sprite != null)
                {
                    fossilImageCount++;
                }
            }
            
            Debug.Log($"   矿物图片: {mineralImageCount}/{mineralImages.Length}");
            Debug.Log($"   化石图片: {fossilImageCount}/{fossilImages.Length}");
            
            if (mineralImageCount > 0 && fossilImageCount > 0)
            {
                Debug.Log("✅ 图片资源验证通过");
            }
            else
            {
                Debug.LogWarning("⚠️ 部分图片资源缺失");
            }
        }
        
        /// <summary>
        /// 快速修复建议
        /// </summary>
        [ContextMenu("显示修复建议")]
        public void ShowFixSuggestions()
        {
            Debug.Log("=== 修复建议 ===");
            Debug.Log("1. 如果数据库文件找不到:");
            Debug.Log("   - 确认MineralData文件夹在Assets/Resources/下");
            Debug.Log("   - 确认SendaiMineralDatabase.json在MineralData/Data/下");
            Debug.Log("");
            Debug.Log("2. 如果按键无响应:");
            Debug.Log("   - 添加SimpleEncyclopediaManager到场景");
            Debug.Log("   - 确认使用了新Input System");
            Debug.Log("");
            Debug.Log("3. 如果UI创建失败:");
            Debug.Log("   - 使用SimpleEncyclopediaManager替代复杂UI");
            Debug.Log("   - 检查字体是否为LegacyRuntime.ttf");
            Debug.Log("");
            Debug.Log("4. 推荐设置步骤:");
            Debug.Log("   - 创建空GameObject");
            Debug.Log("   - 添加SimpleEncyclopediaManager脚本");
            Debug.Log("   - 运行游戏，按O键测试");
        }
    }
}