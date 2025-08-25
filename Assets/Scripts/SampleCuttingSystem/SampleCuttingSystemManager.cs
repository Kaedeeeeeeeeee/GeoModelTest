using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 样本切割系统管理器
    /// 协调所有切割系统组件，提供统一的接口
    /// </summary>
    public class SampleCuttingSystemManager : MonoBehaviour
    {
        [Header("系统组件")]
        [SerializeField] private SampleCuttingGame cuttingGame;
        [SerializeField] private SampleLayerAnalyzer layerAnalyzer;
        [SerializeField] private LayerDatabaseMapper databaseMapper;
        [SerializeField] private CuttingStationUI stationUI;
        [SerializeField] private SingleLayerSampleGenerator sampleGenerator;
        
        [Header("拖拽区域")]
        [SerializeField] private RectTransform dropZone;             // 拖拽区域
        [SerializeField] private Image dropZoneImage;               // 拖拽区域图片
        [SerializeField] private Text dropZoneText;                 // 拖拽区域文字
        [SerializeField] private Color normalColor = Color.white;   // 正常颜色
        [SerializeField] private Color highlightColor = Color.green; // 高亮颜色
        [SerializeField] private Color errorColor = Color.red;      // 错误颜色
        
        [Header("系统设置")]
        [SerializeField] private bool enableDebugMode = false;      // 调试模式
        [SerializeField] private bool autoInitialize = true;        // 自动初始化
        [SerializeField] private Transform cuttingStationTransform; // 切割台位置
        
        // 系统状态
        private CuttingStationState currentState = new CuttingStationState();
        private List<CuttingRecord> cuttingHistory = new List<CuttingRecord>();
        
        // 事件
        public System.Action<SingleLayerSample[]> OnSamplesGenerated;
        public System.Action<CuttingRecord> OnCuttingCompleted;
        public System.Action<string> OnSystemError;
        
        void Awake()
        {
            if (autoInitialize)
            {
                InitializeSystem();
            }
        }
        
        void Start()
        {
            SetupInitialState();
        }
        
        /// <summary>
        /// 初始化系统
        /// </summary>
        public void InitializeSystem()
        {
            // 获取或创建必要组件
            EnsureComponents();
            
            // 连接组件事件
            ConnectComponentEvents();
            
            // 设置初始状态
            currentState.Reset();
            
            Debug.Log("样本切割系统初始化完成");
        }
        
        /// <summary>
        /// 确保所有组件存在
        /// </summary>
        private void EnsureComponents()
        {
            if (cuttingGame == null)
                cuttingGame = GetComponent<SampleCuttingGame>() ?? gameObject.AddComponent<SampleCuttingGame>();
                
            if (layerAnalyzer == null)
                layerAnalyzer = GetComponent<SampleLayerAnalyzer>() ?? gameObject.AddComponent<SampleLayerAnalyzer>();
                
            if (databaseMapper == null)
                databaseMapper = GetComponent<LayerDatabaseMapper>() ?? gameObject.AddComponent<LayerDatabaseMapper>();
                
            if (stationUI == null)
                stationUI = GetComponent<CuttingStationUI>() ?? gameObject.AddComponent<CuttingStationUI>();
                
            if (sampleGenerator == null)
                sampleGenerator = GetComponent<SingleLayerSampleGenerator>() ?? gameObject.AddComponent<SingleLayerSampleGenerator>();
        }
        
        /// <summary>
        /// 连接组件事件
        /// </summary>
        private void ConnectComponentEvents()
        {
            // TODO: 连接切割游戏的事件
            // 例如：cuttingGame.OnCuttingSuccess += HandleCuttingSuccess;
        }
        
        /// <summary>
        /// 设置初始状态
        /// </summary>
        private void SetupInitialState()
        {
            SetDropZoneState(DropZoneState.Normal);
            
            if (stationUI != null)
            {
                stationUI.SetUIState(CuttingStationUI.UIState.WaitingForSample);
            }
        }
        
        /// <summary>
        /// 开始切割样本（主要入口点）
        /// </summary>
        public bool StartCuttingSample(GeometricSampleReconstructor.ReconstructedSample sample)
        {
            if (sample == null)
            {
                LogError("样本为空，无法开始切割");
                return false;
            }
            
            if (currentState.isOccupied)
            {
                LogError("切割台当前被占用，请等待");
                return false;
            }
            
            // 验证样本是否可以切割
            if (!ValidateSample(sample))
            {
                return false;
            }
            
            // 开始切割流程
            return BeginCuttingProcess(sample);
        }
        
        /// <summary>
        /// 验证样本
        /// </summary>
        private bool ValidateSample(GeometricSampleReconstructor.ReconstructedSample sample)
        {
            if (layerAnalyzer == null)
            {
                LogError("地层分析器未找到");
                return false;
            }
            
            if (!layerAnalyzer.CanSampleBeCut(sample))
            {
                LogError("该样本不需要切割（只有单层或无效）");
                SetDropZoneState(DropZoneState.Error);
                return false;
            }
            
            var sampleInfo = layerAnalyzer.GetSampleInfo(sample);
            if (sampleInfo == null || sampleInfo.layerCount <= 1)
            {
                LogError("样本分析失败或只有单层");
                SetDropZoneState(DropZoneState.Error);
                return false;
            }
            
            Log($"样本验证通过: {sampleInfo.layerCount} 层，需要 {sampleInfo.estimatedCuts} 次切割");
            return true;
        }
        
        /// <summary>
        /// 开始切割流程
        /// </summary>
        private bool BeginCuttingProcess(GeometricSampleReconstructor.ReconstructedSample sample)
        {
            try
            {
                // 更新状态
                currentState.isOccupied = true;
                currentState.currentSampleID = sample.sampleID;
                currentState.currentPhase = CuttingStationState.CuttingPhase.Loading;
                currentState.sessionStartTime = System.DateTime.Now;
                
                // 加载样本到UI
                if (stationUI != null)
                {
                    stationUI.LoadSample(sample);
                }
                
                // 开始切割游戏
                if (cuttingGame != null)
                {
                    cuttingGame.StartCutting(sample);
                }
                
                Log($"开始切割样本: {sample.sampleID}");
                return true;
            }
            catch (System.Exception e)
            {
                LogError($"开始切割流程失败: {e.Message}");
                ResetSystem();
                return false;
            }
        }
        
        /// <summary>
        /// 处理切割成功
        /// </summary>
        public void HandleCuttingSuccess(GeometricSampleReconstructor.ReconstructedSample originalSample)
        {
            Log("切割成功，开始生成单层样本");
            
            // 生成单层样本
            if (sampleGenerator != null)
            {
                var generatedSamples = sampleGenerator.GenerateSamplesFromMultiLayer(originalSample);
                
                if (generatedSamples.Length > 0)
                {
                    // 记录切割历史
                    RecordCuttingHistory(originalSample, generatedSamples, true);
                    
                    // 触发事件
                    OnSamplesGenerated?.Invoke(generatedSamples);
                    
                    Log($"成功生成 {generatedSamples.Length} 个单层样本");
                }
                else
                {
                    LogError("样本生成失败");
                }
            }
            
            // 完成切割
            CompleteCutting();
        }
        
        /// <summary>
        /// 处理切割失败
        /// </summary>
        public void HandleCuttingFailure(GeometricSampleReconstructor.ReconstructedSample originalSample)
        {
            LogError("切割失败，样本已损坏");
            
            // 记录失败历史
            RecordCuttingHistory(originalSample, new SingleLayerSample[0], false);
            
            // 清理失败的样本
            if (originalSample?.sampleContainer != null)
            {
                Destroy(originalSample.sampleContainer);
            }
            
            // 重置系统
            ResetSystem();
        }
        
        /// <summary>
        /// 完成切割
        /// </summary>
        private void CompleteCutting()
        {
            currentState.currentPhase = CuttingStationState.CuttingPhase.Completing;
            
            // 延迟重置系统
            Invoke(nameof(ResetSystem), 3f);
        }
        
        /// <summary>
        /// 重置系统
        /// </summary>
        public void ResetSystem()
        {
            currentState.Reset();
            SetDropZoneState(DropZoneState.Normal);
            
            if (stationUI != null)
            {
                stationUI.ResetUI();
            }
            
            Log("切割系统已重置");
        }
        
        /// <summary>
        /// 记录切割历史
        /// </summary>
        private void RecordCuttingHistory(GeometricSampleReconstructor.ReconstructedSample originalSample, 
            SingleLayerSample[] generatedSamples, bool successful)
        {
            var record = new CuttingRecord
            {
                originalSampleID = originalSample.sampleID,
                cuttingTime = System.DateTime.Now,
                totalLayers = originalSample.layerSegments?.Length ?? 0,
                successfulCuts = successful ? (originalSample.layerSegments?.Length ?? 0) - 1 : 0,
                failedCuts = successful ? 0 : 1,
                resultingSampleIDs = generatedSamples.Select(s => s.sampleID).ToArray(),
                cuttingPosition = cuttingStationTransform?.position ?? Vector3.zero
            };
            
            cuttingHistory.Add(record);
            OnCuttingCompleted?.Invoke(record);
            
            Log($"记录切割历史: {record.originalSampleID} -> {record.resultingSampleIDs.Length} 个样本");
        }
        
        /// <summary>
        /// 设置拖拽区域状态
        /// </summary>
        private void SetDropZoneState(DropZoneState state)
        {
            if (dropZoneImage == null) return;
            
            switch (state)
            {
                case DropZoneState.Normal:
                    dropZoneImage.color = normalColor;
                    if (dropZoneText != null)
                        dropZoneText.text = "将多层样本拖拽到此处";
                    break;
                    
                case DropZoneState.Highlight:
                    dropZoneImage.color = highlightColor;
                    if (dropZoneText != null)
                        dropZoneText.text = "松开鼠标放置样本";
                    break;
                    
                case DropZoneState.Error:
                    dropZoneImage.color = errorColor;
                    if (dropZoneText != null)
                        dropZoneText.text = "无效样本或系统忙碌";
                    break;
            }
        }
        
        /// <summary>
        /// 拖拽区域状态
        /// </summary>
        private enum DropZoneState
        {
            Normal,     // 正常状态
            Highlight,  // 高亮状态（可以放置）
            Error       // 错误状态（不能放置）
        }
        
        /// <summary>
        /// 获取系统状态
        /// </summary>
        public CuttingStationState GetSystemState()
        {
            return currentState;
        }
        
        /// <summary>
        /// 获取切割历史
        /// </summary>
        public CuttingRecord[] GetCuttingHistory()
        {
            return cuttingHistory.ToArray();
        }
        
        /// <summary>
        /// 清理切割历史
        /// </summary>
        public void ClearCuttingHistory()
        {
            cuttingHistory.Clear();
            Log("切割历史已清理");
        }
        
        /// <summary>
        /// 获取系统统计信息
        /// </summary>
        public SystemStatistics GetStatistics()
        {
            var stats = new SystemStatistics();
            
            if (cuttingHistory.Count > 0)
            {
                stats.totalCuttingSessions = cuttingHistory.Count;
                stats.successfulSessions = cuttingHistory.Count(r => r.IsCompletelySuccessful());
                stats.failedSessions = stats.totalCuttingSessions - stats.successfulSessions;
                stats.successRate = (float)stats.successfulSessions / stats.totalCuttingSessions;
                stats.totalSamplesGenerated = cuttingHistory.Sum(r => r.resultingSampleIDs?.Length ?? 0);
                stats.averageSamplesPerSession = stats.totalSamplesGenerated / (float)stats.totalCuttingSessions;
            }
            
            return stats;
        }
        
        [System.Serializable]
        public class SystemStatistics
        {
            public int totalCuttingSessions;
            public int successfulSessions;
            public int failedSessions;
            public float successRate;
            public int totalSamplesGenerated;
            public float averageSamplesPerSession;
        }
        
        /// <summary>
        /// 日志输出
        /// </summary>
        private void Log(string message)
        {
            if (enableDebugMode)
            {
                Debug.Log($"[SampleCuttingSystem] {message}");
            }
        }
        
        /// <summary>
        /// 错误日志输出
        /// </summary>
        private void LogError(string message)
        {
            Debug.LogError($"[SampleCuttingSystem] {message}");
            OnSystemError?.Invoke(message);
        }
        
        /// <summary>
        /// 检查系统健康状态
        /// </summary>
        public bool CheckSystemHealth()
        {
            bool isHealthy = true;
            
            if (cuttingGame == null)
            {
                LogError("切割游戏组件缺失");
                isHealthy = false;
            }
            
            if (layerAnalyzer == null)
            {
                LogError("地层分析器组件缺失");
                isHealthy = false;
            }
            
            if (databaseMapper == null)
            {
                LogError("数据库映射器组件缺失");
                isHealthy = false;
            }
            
            if (stationUI == null)
            {
                LogError("UI管理器组件缺失");
                isHealthy = false;
            }
            
            if (sampleGenerator == null)
            {
                LogError("样本生成器组件缺失");
                isHealthy = false;
            }
            
            if (isHealthy)
            {
                Log("系统健康检查通过");
            }
            
            return isHealthy;
        }
        
        /// <summary>
        /// Editor测试方法
        /// </summary>
        [ContextMenu("检查系统健康状态")]
        private void TestSystemHealth()
        {
            CheckSystemHealth();
        }
        
        [ContextMenu("显示系统统计")]
        private void ShowSystemStatistics()
        {
            var stats = GetStatistics();
            Debug.Log($"=== 切割系统统计 ===");
            Debug.Log($"总切割会话: {stats.totalCuttingSessions}");
            Debug.Log($"成功会话: {stats.successfulSessions}");
            Debug.Log($"失败会话: {stats.failedSessions}");
            Debug.Log($"成功率: {stats.successRate:P1}");
            Debug.Log($"总生成样本: {stats.totalSamplesGenerated}");
            Debug.Log($"平均每次生成: {stats.averageSamplesPerSession:F1}");
        }
        
        [ContextMenu("重置系统")]
        private void EditorResetSystem()
        {
            ResetSystem();
        }
    }
}