using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MineralSystem
{
    public class MineralSearchSystem : MonoBehaviour
    {
        [Header("Search UI")]
        public TMP_InputField searchInput;
        public TMP_Dropdown languageDropdown;
        public TMP_Dropdown layerFilterDropdown;
        public Toggle magneticFilter;
        public Toggle acidReactiveFilter;
        public Slider hardnessMinSlider;
        public Slider hardnessMaxSlider;
        public TextMeshProUGUI hardnessRangeText;
        
        [Header("Results Display")]
        public Transform resultsContainer;
        public GameObject mineralResultPrefab;
        public TextMeshProUGUI resultsCountText;
        
        [Header("Pagination")]
        public int resultsPerPage = 10;
        public Button previousPageButton;
        public Button nextPageButton;
        public TextMeshProUGUI pageInfoText;
        
        private List<MineralData> allMinerals;
        private List<MineralData> filteredResults;
        private int currentPage = 0;
        private string currentLanguage = "zh";
        
        private void Start()
        {
            InitializeSystem();
            SetupEventListeners();
        }
        
        private void InitializeSystem()
        {
            if (MineralDatabase.Instance != null)
            {
                allMinerals = MineralDatabase.Instance.GetAllMinerals();
                PopulateLayerDropdown();
                ResetFilters();
                PerformSearch();
            }
            else
            {
                Debug.LogError("MineralDatabase实例未找到！");
            }
        }
        
        private void SetupEventListeners()
        {
            if (searchInput != null)
                searchInput.onValueChanged.AddListener(OnSearchChanged);
            
            if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            
            if (layerFilterDropdown != null)
                layerFilterDropdown.onValueChanged.AddListener(OnLayerFilterChanged);
            
            if (magneticFilter != null)
                magneticFilter.onValueChanged.AddListener(OnFilterChanged);
            
            if (acidReactiveFilter != null)
                acidReactiveFilter.onValueChanged.AddListener(OnFilterChanged);
            
            if (hardnessMinSlider != null)
                hardnessMinSlider.onValueChanged.AddListener(OnHardnessChanged);
            
            if (hardnessMaxSlider != null)
                hardnessMaxSlider.onValueChanged.AddListener(OnHardnessChanged);
            
            if (previousPageButton != null)
                previousPageButton.onClick.AddListener(PreviousPage);
            
            if (nextPageButton != null)
                nextPageButton.onClick.AddListener(NextPage);
        }
        
        private void PopulateLayerDropdown()
        {
            if (layerFilterDropdown == null) return;
            
            layerFilterDropdown.options.Clear();
            layerFilterDropdown.options.Add(new TMP_Dropdown.OptionData("所有地层"));
            
            var layers = MineralDatabase.Instance.GetAllLayers();
            foreach (var layer in layers)
            {
                string layerName = currentLanguage == "en" ? layer.layerNameEN : layer.layerName;
                layerFilterDropdown.options.Add(new TMP_Dropdown.OptionData(layerName));
            }
            
            layerFilterDropdown.RefreshShownValue();
        }
        
        private void ResetFilters()
        {
            if (searchInput != null) searchInput.text = "";
            if (magneticFilter != null) magneticFilter.isOn = false;
            if (acidReactiveFilter != null) acidReactiveFilter.isOn = false;
            if (hardnessMinSlider != null) hardnessMinSlider.value = 1f;
            if (hardnessMaxSlider != null) hardnessMaxSlider.value = 10f;
            if (layerFilterDropdown != null) layerFilterDropdown.value = 0;
            
            UpdateHardnessRangeText();
            currentPage = 0;
        }
        
        private void OnSearchChanged(string searchTerm)
        {
            currentPage = 0;
            PerformSearch();
        }
        
        private void OnLanguageChanged(int languageIndex)
        {
            currentLanguage = languageIndex switch
            {
                1 => "en",
                2 => "ja",
                _ => "zh"
            };
            
            PopulateLayerDropdown();
            PerformSearch();
        }
        
        private void OnLayerFilterChanged(int layerIndex)
        {
            currentPage = 0;
            PerformSearch();
        }
        
        private void OnFilterChanged(bool value)
        {
            currentPage = 0;
            PerformSearch();
        }
        
        private void OnHardnessChanged(float value)
        {
            UpdateHardnessRangeText();
            currentPage = 0;
            PerformSearch();
        }
        
        private void UpdateHardnessRangeText()
        {
            if (hardnessRangeText != null && hardnessMinSlider != null && hardnessMaxSlider != null)
            {
                hardnessRangeText.text = $"硬度: {hardnessMinSlider.value:F1} - {hardnessMaxSlider.value:F1}";
            }
        }
        
        private void PerformSearch()
        {
            if (allMinerals == null || allMinerals.Count == 0)
            {
                filteredResults = new List<MineralData>();
                DisplayResults();
                return;
            }
            
            filteredResults = new List<MineralData>(allMinerals);
            
            // 文本搜索
            if (searchInput != null && !string.IsNullOrEmpty(searchInput.text))
            {
                string searchTerm = searchInput.text.ToLower();
                filteredResults = filteredResults.Where(mineral =>
                {
                    string nameToSearch = currentLanguage == "en" ? 
                        mineral.mineralNameEN.ToLower() : 
                        mineral.mineralName.ToLower();
                    return nameToSearch.Contains(searchTerm);
                }).ToList();
            }
            
            // 地层过滤
            if (layerFilterDropdown != null && layerFilterDropdown.value > 0)
            {
                var layers = MineralDatabase.Instance.GetAllLayers();
                if (layerFilterDropdown.value - 1 < layers.Count)
                {
                    var selectedLayer = layers[layerFilterDropdown.value - 1];
                    var layerMinerals = MineralDatabase.Instance.GetMineralsInLayer(selectedLayer.layerId);
                    var layerMineralIds = new HashSet<string>(layerMinerals.Select(m => m.mineralId));
                    
                    filteredResults = filteredResults.Where(mineral => 
                        layerMineralIds.Contains(mineral.mineralId)).ToList();
                }
            }
            
            // 磁性过滤
            if (magneticFilter != null && magneticFilter.isOn)
            {
                filteredResults = filteredResults.Where(mineral =>
                    mineral.properties.magnetism.Contains("磁性") &&
                    !mineral.properties.magnetism.Contains("无") &&
                    !mineral.properties.magnetism.Contains("抗磁")).ToList();
            }
            
            // 酸反应过滤
            if (acidReactiveFilter != null && acidReactiveFilter.isOn)
            {
                filteredResults = filteredResults.Where(mineral =>
                    mineral.properties.acidReaction).ToList();
            }
            
            // 硬度过滤
            if (hardnessMinSlider != null && hardnessMaxSlider != null)
            {
                float minHardness = hardnessMinSlider.value;
                float maxHardness = hardnessMaxSlider.value;
                
                filteredResults = filteredResults.Where(mineral =>
                {
                    if (TryParseHardnessRange(mineral.properties.mohsHardness, out float min, out float max))
                    {
                        return (min >= minHardness && min <= maxHardness) ||
                               (max >= minHardness && max <= maxHardness) ||
                               (min <= minHardness && max >= maxHardness);
                    }
                    return false;
                }).ToList();
            }
            
            DisplayResults();
        }
        
        private bool TryParseHardnessRange(string hardnessStr, out float min, out float max)
        {
            min = max = 0f;
            
            if (string.IsNullOrEmpty(hardnessStr)) return false;
            
            if (hardnessStr.Contains("–") || hardnessStr.Contains("-"))
            {
                var parts = hardnessStr.Split(new char[] { '–', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    return float.TryParse(parts[0].Trim(), out min) && 
                           float.TryParse(parts[1].Trim(), out max);
                }
            }
            else
            {
                if (float.TryParse(hardnessStr.Trim(), out min))
                {
                    max = min;
                    return true;
                }
            }
            
            return false;
        }
        
        private void DisplayResults()
        {
            // 清除现有结果
            if (resultsContainer != null)
            {
                foreach (Transform child in resultsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // 更新结果计数
            if (resultsCountText != null)
            {
                resultsCountText.text = $"找到 {filteredResults.Count} 个矿物";
            }
            
            // 分页显示
            int totalPages = Mathf.CeilToInt((float)filteredResults.Count / resultsPerPage);
            int startIndex = currentPage * resultsPerPage;
            int endIndex = Mathf.Min(startIndex + resultsPerPage, filteredResults.Count);
            
            for (int i = startIndex; i < endIndex; i++)
            {
                CreateMineralResultItem(filteredResults[i]);
            }
            
            UpdatePaginationUI(totalPages);
        }
        
        private void CreateMineralResultItem(MineralData mineral)
        {
            if (mineralResultPrefab == null || resultsContainer == null) return;
            
            GameObject resultItem = Instantiate(mineralResultPrefab, resultsContainer);
            
            // 假设预制体有这些组件
            var nameText = resultItem.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                string displayName = currentLanguage == "en" ? mineral.mineralNameEN : mineral.mineralName;
                nameText.text = $"{displayName} ({mineral.percentage * 100:F1}%)";
            }
            
            // 添加点击事件
            var button = resultItem.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnMineralSelected(mineral));
            }
        }
        
        private void OnMineralSelected(MineralData mineral)
        {
            Debug.Log($"选择了矿物: {mineral.mineralName}");
            
            // 这里可以触发矿物详情显示
            var infoDisplay = FindObjectOfType<MineralInfoDisplay>();
            if (infoDisplay != null)
            {
                infoDisplay.DisplayMineral(mineral.mineralId);
            }
        }
        
        private void UpdatePaginationUI(int totalPages)
        {
            if (pageInfoText != null)
            {
                pageInfoText.text = $"{currentPage + 1} / {Mathf.Max(1, totalPages)}";
            }
            
            if (previousPageButton != null)
            {
                previousPageButton.interactable = currentPage > 0;
            }
            
            if (nextPageButton != null)
            {
                nextPageButton.interactable = currentPage < totalPages - 1;
            }
        }
        
        private void PreviousPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                DisplayResults();
            }
        }
        
        private void NextPage()
        {
            int totalPages = Mathf.CeilToInt((float)filteredResults.Count / resultsPerPage);
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                DisplayResults();
            }
        }
        
        public void ClearAllFilters()
        {
            ResetFilters();
            PerformSearch();
        }
        
        public List<MineralData> GetFilteredResults()
        {
            return new List<MineralData>(filteredResults);
        }
    }
}