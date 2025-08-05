using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MineralSystem
{
    public class MineralInfoDisplay : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI mineralNameText;
        public TextMeshProUGUI mineralNameEnText;
        public TextMeshProUGUI hardnessText;
        public TextMeshProUGUI densityText;
        public TextMeshProUGUI magnetismText;
        public TextMeshProUGUI appearanceText;
        public TextMeshProUGUI percentageText;
        public Image mineralImage;
        public GameObject mineralModel3D;
        
        [Header("Language Settings")]
        public string currentLanguage = "zh";
        
        private MineralData currentMineral;
        
        public void DisplayMineral(string mineralId)
        {
            currentMineral = MineralDatabase.Instance.GetMineral(mineralId);
            
            if (currentMineral != null)
            {
                UpdateUI();
                Load3DModel();
            }
            else
            {
                Debug.LogWarning($"未找到矿物: {mineralId}");
            }
        }
        
        private void UpdateUI()
        {
            if (currentMineral == null) return;
            
            // 设置矿物名称
            if (mineralNameText != null)
            {
                mineralNameText.text = GetLocalizedName();
            }
            
            if (mineralNameEnText != null)
            {
                mineralNameEnText.text = currentMineral.mineralNameEN;
            }
            
            // 设置属性信息
            if (hardnessText != null)
            {
                hardnessText.text = $"硬度: {currentMineral.properties.mohsHardness}";
            }
            
            if (densityText != null)
            {
                densityText.text = $"密度: {currentMineral.properties.density} g/cm³";
            }
            
            if (magnetismText != null)
            {
                magnetismText.text = $"磁性: {currentMineral.properties.magnetism}";
            }
            
            if (appearanceText != null)
            {
                appearanceText.text = currentMineral.properties.appearance;
            }
            
            if (percentageText != null)
            {
                percentageText.text = $"含量: {currentMineral.percentage * 100:F1}%";
            }
            
            // 加载矿物图片
            LoadMineralImage();
        }
        
        private string GetLocalizedName()
        {
            return currentLanguage.ToLower() switch
            {
                "en" => currentMineral.mineralNameEN,
                "ja" => currentMineral.mineralNameJA,
                _ => currentMineral.mineralName
            };
        }
        
        private void LoadMineralImage()
        {
            if (mineralImage != null && currentMineral != null)
            {
                Sprite mineralSprite = MineralDatabase.Instance.GetMineralImage(currentMineral.mineralId);
                if (mineralSprite != null)
                {
                    mineralImage.sprite = mineralSprite;
                    mineralImage.gameObject.SetActive(true);
                }
                else
                {
                    mineralImage.gameObject.SetActive(false);
                    Debug.LogWarning($"未找到矿物图片: {currentMineral.properties.imageFile}");
                }
            }
        }
        
        private void Load3DModel()
        {
            if (mineralModel3D != null && currentMineral != null)
            {
                // 清除之前的模型
                foreach (Transform child in mineralModel3D.transform)
                {
                    DestroyImmediate(child.gameObject);
                }
                
                GameObject modelPrefab = MineralDatabase.Instance.GetMineralModel(currentMineral.mineralId);
                if (modelPrefab != null)
                {
                    GameObject instance = Instantiate(modelPrefab, mineralModel3D.transform);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                    instance.transform.localScale = Vector3.one;
                }
                else
                {
                    Debug.LogWarning($"未找到矿物模型: {currentMineral.properties.modelFile}");
                }
            }
        }
        
        public void SetLanguage(string language)
        {
            currentLanguage = language;
            if (currentMineral != null)
            {
                UpdateUI();
            }
        }
        
        public void ToggleLanguage()
        {
            currentLanguage = currentLanguage == "zh" ? "en" : "zh";
            SetLanguage(currentLanguage);
        }
        
        public MineralData GetCurrentMineral()
        {
            return currentMineral;
        }
        
        public void ShowMineralDetails()
        {
            if (currentMineral == null) return;
            
            string details = $"矿物详情:\n" +
                           $"名称: {currentMineral.mineralName} ({currentMineral.mineralNameEN})\n" +
                           $"硬度: {currentMineral.properties.mohsHardness}\n" +
                           $"密度: {currentMineral.properties.density}\n" +
                           $"磁性: {currentMineral.properties.magnetism}\n" +
                           $"酸反应: {(currentMineral.properties.acidReaction ? "是" : "否")}\n" +
                           $"紫外荧光: {currentMineral.properties.uvFluorescence}\n" +
                           $"偏光颜色: {currentMineral.properties.polarizedColor}\n" +
                           $"外观: {currentMineral.properties.appearance}";
            
            Debug.Log(details);
        }
        
        public void ClearDisplay()
        {
            currentMineral = null;
            
            if (mineralNameText != null) mineralNameText.text = "";
            if (mineralNameEnText != null) mineralNameEnText.text = "";
            if (hardnessText != null) hardnessText.text = "";
            if (densityText != null) densityText.text = "";
            if (magnetismText != null) magnetismText.text = "";
            if (appearanceText != null) appearanceText.text = "";
            if (percentageText != null) percentageText.text = "";
            
            if (mineralImage != null)
            {
                mineralImage.sprite = null;
                mineralImage.gameObject.SetActive(false);
            }
            
            if (mineralModel3D != null)
            {
                foreach (Transform child in mineralModel3D.transform)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}