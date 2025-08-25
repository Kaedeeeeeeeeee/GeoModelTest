using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SampleCuttingSystem
{
    /// <summary>
    /// 单层样本生成器
    /// 负责从多层样本中生成独立的单层样本
    /// </summary>
    public class SingleLayerSampleGenerator : MonoBehaviour
    {
        [Header("生成设置")]
        [SerializeField] private SampleGenerationConfig generationConfig;
        [SerializeField] private Transform sampleSpawnPoint;        // 样本生成位置
        [SerializeField] private float spawnSpacing = 0.5f;         // 样本间距
        [SerializeField] private int maxSamplesPerRow = 3;          // 每行最大样本数
        
        [Header("材质和着色器")]
        [SerializeField] private Material defaultSampleMaterial;   // 默认样本材质
        [SerializeField] private Shader sampleShader;              // 样本着色器
        [SerializeField] private bool preserveOriginalColors = true; // 保持原始颜色
        
        [Header("物理设置")]
        [SerializeField] private PhysicsMaterial samplePhysicMaterial; // 物理材质
        [SerializeField] private float defaultMass = 2f;           // 默认质量
        [SerializeField] private bool enableCollision = true;      // 启用碰撞
        
        [Header("收集设置")]
        [SerializeField] private bool autoAddToInventory = true;   // 自动添加到背包
        [SerializeField] private float collectionDelay = 1f;       // 收集延迟
        
        // 组件引用
        private LayerDatabaseMapper databaseMapper;
        private AudioSource audioSource;
        
        // 生成状态
        private List<SingleLayerSample> generatedSamples = new List<SingleLayerSample>();
        private int currentSpawnIndex = 0;
        
        void Awake()
        {
            InitializeComponents();
        }
        
        void Start()
        {
            InitializeGenerator();
        }
        
        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            databaseMapper = GetComponent<LayerDatabaseMapper>();
            if (databaseMapper == null)
                databaseMapper = gameObject.AddComponent<LayerDatabaseMapper>();
                
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
                
            if (generationConfig == null)
                generationConfig = SampleGenerationConfig.GetDefault();
        }
        
        /// <summary>
        /// 初始化生成器
        /// </summary>
        private void InitializeGenerator()
        {
            // 设置默认生成位置
            if (sampleSpawnPoint == null)
            {
                GameObject spawnPointObj = new GameObject("SampleSpawnPoint");
                spawnPointObj.transform.SetParent(transform);
                spawnPointObj.transform.localPosition = Vector3.zero;
                sampleSpawnPoint = spawnPointObj.transform;
            }
            
            // 设置默认材质
            if (defaultSampleMaterial == null)
            {
                defaultSampleMaterial = new Material(Shader.Find("Standard"));
                defaultSampleMaterial.name = "DefaultSampleMaterial";
            }
        }
        
        /// <summary>
        /// 从多层样本生成单层样本
        /// </summary>
        public SingleLayerSample[] GenerateSamplesFromMultiLayer(GeometricSampleReconstructor.ReconstructedSample originalSample)
        {
            if (originalSample?.layerSegments == null || originalSample.layerSegments.Length <= 1)
            {
                Debug.LogWarning("原始样本无效或只有单层，无需切割生成");
                return new SingleLayerSample[0];
            }
            
            var samples = new List<SingleLayerSample>();
            
            // 按深度排序地层
            var sortedLayers = originalSample.layerSegments
                .Where(segment => segment?.sourceLayer != null)
                .OrderBy(segment => segment.relativeDepth)
                .ToArray();
                
            Debug.Log($"开始生成 {sortedLayers.Length} 个单层样本");
            
            // 重置生成计数器
            currentSpawnIndex = 0;
            
            for (int i = 0; i < sortedLayers.Length; i++)
            {
                var layerSegment = sortedLayers[i];
                var singleLayerSample = GenerateSingleLayerSample(layerSegment, originalSample, i);
                
                if (singleLayerSample != null)
                {
                    samples.Add(singleLayerSample);
                    
                    // 播放生成效果
                    if (generationConfig.showGenerationEffect)
                    {
                        PlayGenerationEffect(singleLayerSample.sampleObject.transform.position);
                    }
                    
                    // 播放生成音效
                    PlayGenerationSound();
                }
            }
            
            generatedSamples.AddRange(samples);
            
            // 自动收集到背包
            if (autoAddToInventory)
            {
                StartCoroutine(AutoCollectSamples(samples.ToArray()));
            }
            
            Debug.Log($"成功生成 {samples.Count} 个单层样本");
            return samples.ToArray();
        }
        
        /// <summary>
        /// 生成单个地层样本
        /// </summary>
        private SingleLayerSample GenerateSingleLayerSample(GeometricSampleReconstructor.LayerSegment layerSegment, 
            GeometricSampleReconstructor.ReconstructedSample originalSample, int layerIndex)
        {
            try
            {
                // 创建单层样本数据
                var singleSample = new SingleLayerSample
                {
                    layerName = layerSegment.sourceLayer.layerName ?? $"地层 {layerIndex + 1}",
                    originalSampleID = originalSample.sampleID,
                    isCutFromMultiLayer = true,
                    cuttingTime = System.DateTime.Now,
                    originalThickness = CalculateLayerThickness(layerSegment, originalSample),
                    originalStartDepth = layerSegment.relativeDepth,
                    layerColor = GetLayerColor(layerSegment)
                };
                
                // 生成唯一ID
                singleSample.sampleID = SingleLayerSample.GenerateSampleID(singleSample.layerName, originalSample.sampleID);
                singleSample.originalEndDepth = singleSample.originalStartDepth + singleSample.originalThickness;
                
                // 创建3D对象
                singleSample.sampleObject = CreateSampleGameObject(layerSegment, singleSample);
                
                // 获取矿物信息
                singleSample.minerals = GetMineralsForLayer(singleSample.layerName);
                singleSample.hasMineralData = singleSample.minerals.Length > 0;
                
                // 计算物理属性
                CalculatePhysicalProperties(singleSample, layerSegment);
                
                // 设置生成位置
                PositionSample(singleSample);
                
                return singleSample;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"生成单层样本失败: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 创建样本游戏对象
        /// </summary>
        private GameObject CreateSampleGameObject(GeometricSampleReconstructor.LayerSegment layerSegment, SingleLayerSample sampleData)
        {
            // 创建主对象 - 确保它在场景根部，不依附任何父对象
            GameObject sampleObj = new GameObject($"CutSample_{sampleData.sampleID}");
            
            // 重要：确保样本对象在场景根部，不是任何其他对象的子对象
            sampleObj.transform.SetParent(null);
            
            // 添加标识标签，帮助识别这是切割系统生成的样本
            try 
            {
                sampleObj.tag = "Sample"; // 如果Sample标签存在
            }
            catch 
            {
                sampleObj.tag = "Untagged"; // 如果标签不存在使用默认
            }
            
            // 复制几何体
            if (layerSegment.segmentObject != null)
            {
                // 复制网格
                MeshFilter sourceMesh = layerSegment.segmentObject.GetComponent<MeshFilter>();
                if (sourceMesh?.sharedMesh != null)
                {
                    MeshFilter meshFilter = sampleObj.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = sourceMesh.sharedMesh;
                    sampleData.sampleMesh = sourceMesh.sharedMesh;
                }
                
                // 设置材质
                MeshRenderer meshRenderer = sampleObj.AddComponent<MeshRenderer>();
                Material sampleMaterial = CreateSampleMaterial(layerSegment, sampleData);
                meshRenderer.material = sampleMaterial;
                sampleData.sampleMaterial = sampleMaterial;
            }
            else
            {
                // 创建默认几何体
                CreateDefaultGeometry(sampleObj, sampleData);
            }
            
            // 添加物理组件
            if (generationConfig.enablePhysics)
            {
                AddPhysicsComponents(sampleObj, sampleData);
            }
            
            // 添加收集组件
            AddCollectionComponents(sampleObj, sampleData);
            
            // 设置缩放
            sampleObj.transform.localScale = Vector3.one * generationConfig.sampleScale;
            
            Debug.Log($"✅ 独立样本对象创建完成: {sampleObj.name} (父对象: {sampleObj.transform.parent?.name ?? "无"})");
            
            return sampleObj;
        }
        
        /// <summary>
        /// 创建样本材质
        /// </summary>
        private Material CreateSampleMaterial(GeometricSampleReconstructor.LayerSegment layerSegment, SingleLayerSample sampleData)
        {
            Material material;
            
            if (preserveOriginalColors && layerSegment.material != null)
            {
                // 使用原始材质颜色
                material = new Material(sampleShader ?? defaultSampleMaterial.shader);
                material.color = layerSegment.material.color;
                material.mainTexture = layerSegment.material.mainTexture;
            }
            else if (preserveOriginalColors)
            {
                // 使用地层颜色
                material = new Material(sampleShader ?? defaultSampleMaterial.shader);
                material.color = sampleData.layerColor;
            }
            else
            {
                // 使用默认材质
                material = new Material(defaultSampleMaterial);
            }
            
            material.name = $"Material_{sampleData.sampleID}";
            return material;
        }
        
        /// <summary>
        /// 创建默认几何体
        /// </summary>
        private void CreateDefaultGeometry(GameObject sampleObj, SingleLayerSample sampleData)
        {
            // 创建圆柱体几何体
            MeshFilter meshFilter = sampleObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = sampleObj.AddComponent<MeshRenderer>();
            
            // 生成简单圆柱体网格
            Mesh cylinderMesh = CreateCylinderMesh(0.1f, sampleData.originalThickness);
            meshFilter.sharedMesh = cylinderMesh;
            sampleData.sampleMesh = cylinderMesh;
            
            // 设置材质
            Material material = CreateDefaultMaterial(sampleData);
            meshRenderer.material = material;
            sampleData.sampleMaterial = material;
        }
        
        /// <summary>
        /// 创建圆柱体网格
        /// </summary>
        private Mesh CreateCylinderMesh(float radius, float height)
        {
            // 简化的圆柱体网格生成
            // 这里可以实现更复杂的网格生成逻辑
            GameObject primitiveObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Mesh mesh = primitiveObj.GetComponent<MeshFilter>().sharedMesh;
            
            // 调整尺寸
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].x *= radius * 2;
                vertices[i].z *= radius * 2;
                vertices[i].y *= height;
            }
            
            Mesh newMesh = new Mesh();
            newMesh.vertices = vertices;
            newMesh.triangles = mesh.triangles;
            newMesh.normals = mesh.normals;
            newMesh.uv = mesh.uv;
            newMesh.RecalculateBounds();
            
            DestroyImmediate(primitiveObj);
            return newMesh;
        }
        
        /// <summary>
        /// 创建默认材质
        /// </summary>
        private Material CreateDefaultMaterial(SingleLayerSample sampleData)
        {
            Material material = new Material(defaultSampleMaterial);
            material.color = sampleData.layerColor;
            material.name = $"DefaultMaterial_{sampleData.sampleID}";
            return material;
        }
        
        /// <summary>
        /// 添加物理组件
        /// </summary>
        private void AddPhysicsComponents(GameObject sampleObj, SingleLayerSample sampleData)
        {
            // 添加刚体
            Rigidbody rb = sampleObj.AddComponent<Rigidbody>();
            rb.mass = sampleData.estimatedMass > 0 ? sampleData.estimatedMass : defaultMass;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.5f;
            
            // 添加碰撞器
            if (enableCollision)
            {
                if (sampleData.sampleMesh != null)
                {
                    MeshCollider meshCollider = sampleObj.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = sampleData.sampleMesh;
                    meshCollider.convex = true;
                    meshCollider.material = samplePhysicMaterial;
                }
                else
                {
                    CapsuleCollider capsuleCollider = sampleObj.AddComponent<CapsuleCollider>();
                    capsuleCollider.radius = 0.1f;
                    capsuleCollider.height = sampleData.originalThickness;
                    capsuleCollider.material = samplePhysicMaterial;
                }
            }
        }
        
        /// <summary>
        /// 添加收集组件
        /// </summary>
        private void AddCollectionComponents(GameObject sampleObj, SingleLayerSample sampleData)
        {
            // 优先使用标准的SampleCollector，确保与现有系统兼容
            var standardCollector = sampleObj.AddComponent<SampleCollector>();
            var sampleItem = CreateSampleItemData(sampleData);
            if (sampleItem != null)
            {
                standardCollector.Setup(sampleItem);
            }
            
            // 注意：不再同时添加CuttingSystemCollector，避免重复收集导致实验台被误删
            // 如果需要切割系统专用功能，在CreateSampleItemData中处理
        }
        
        /// <summary>
        /// 创建SampleItem数据
        /// </summary>
        private SampleItem CreateSampleItemData(SingleLayerSample sampleData)
        {
            try
            {
                // 使用SampleItem的静态方法从GameObject创建
                Debug.Log($"创建样本数据: {sampleData.layerName}");
                
                // 创建基础的SampleItem数据
                var sampleItem = SampleItem.CreateFromGeologicalSample(sampleData.sampleObject, "SampleCuttingSystem");
                
                if (sampleItem != null)
                {
                    // 设置切割系统特有的属性
                    sampleItem.displayName = $"{sampleData.layerName}（已切割）";
                    sampleItem.description = $"从多层样本切割得到的单层{sampleData.layerName}样本";
                    
                    Debug.Log($"✅ 成功创建样本数据: {sampleItem.displayName}");
                    return sampleItem;
                }
                else
                {
                    Debug.LogWarning("SampleItem.CreateFromGeologicalSample 返回了 null");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"创建SampleItem失败: {e.Message}");
            }
            
            // 如果创建失败，收集器会使用默认的自动生成数据
            return null;
        }
        
        /// <summary>
        /// 计算地层厚度
        /// </summary>
        private float CalculateLayerThickness(GeometricSampleReconstructor.LayerSegment layerSegment, 
            GeometricSampleReconstructor.ReconstructedSample originalSample)
        {
            // 使用原始样本的地层分析逻辑
            var analyzer = GetComponent<SampleLayerAnalyzer>();
            if (analyzer != null)
            {
                var sampleInfo = analyzer.GetSampleInfo(originalSample);
                if (sampleInfo?.layers != null)
                {
                    var matchingLayer = sampleInfo.layers.FirstOrDefault(l => 
                        Mathf.Approximately(l.startDepth, layerSegment.relativeDepth));
                    if (matchingLayer != null)
                        return matchingLayer.thickness;
                }
            }
            
            // 默认计算方法
            return 0.2f; // 默认20cm厚度
        }
        
        /// <summary>
        /// 获取地层颜色
        /// </summary>
        private Color GetLayerColor(GeometricSampleReconstructor.LayerSegment layerSegment)
        {
            if (layerSegment.material != null)
                return layerSegment.material.color;
                
            // 根据地层名称生成颜色
            return GenerateColorFromLayerName(layerSegment.sourceLayer.layerName);
        }
        
        /// <summary>
        /// 根据地层名称生成颜色
        /// </summary>
        private Color GenerateColorFromLayerName(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
                return Color.gray;
                
            // 使用简单的哈希算法生成稳定的颜色
            int hash = layerName.GetHashCode();
            Random.State oldState = Random.state;
            Random.InitState(hash);
            
            Color color = new Color(
                Random.Range(0.3f, 0.9f),
                Random.Range(0.3f, 0.9f),
                Random.Range(0.3f, 0.9f),
                1f
            );
            
            Random.state = oldState;
            return color;
        }
        
        /// <summary>
        /// 获取地层矿物信息
        /// </summary>
        private MineralComposition[] GetMineralsForLayer(string layerName)
        {
            if (databaseMapper != null)
            {
                return databaseMapper.GetMineralsForLayer(layerName);
            }
            
            // 返回空数组而不是null
            return new MineralComposition[0];
        }
        
        /// <summary>
        /// 计算物理属性
        /// </summary>
        private void CalculatePhysicalProperties(SingleLayerSample sampleData, GeometricSampleReconstructor.LayerSegment layerSegment)
        {
            // 计算体积（简化为圆柱体）
            float radius = 0.1f; // 10cm半径
            float height = sampleData.originalThickness;
            sampleData.estimatedVolume = Mathf.PI * radius * radius * height;
            
            // 计算质量（假设密度为2.5 g/cm³）
            float density = 2.5f; // g/cm³
            sampleData.estimatedMass = sampleData.estimatedVolume * 1000f * density / 1000f; // 转换单位
            
            // 设置质心
            sampleData.centerOfMass = Vector3.zero;
        }
        
        /// <summary>
        /// 定位样本
        /// </summary>
        private void PositionSample(SingleLayerSample sampleData)
        {
            if (sampleSpawnPoint == null) return;
            
            // 计算生成位置
            Vector3 spawnPosition = CalculateSpawnPosition();
            sampleData.cuttingPosition = spawnPosition;
            
            // 设置位置
            sampleData.sampleObject.transform.position = spawnPosition + Vector3.up * generationConfig.heightOffset;
            
            currentSpawnIndex++;
        }
        
        /// <summary>
        /// 计算生成位置
        /// </summary>
        private Vector3 CalculateSpawnPosition()
        {
            Vector3 basePosition = sampleSpawnPoint.position;
            
            int row = currentSpawnIndex / maxSamplesPerRow;
            int col = currentSpawnIndex % maxSamplesPerRow;
            
            Vector3 offset = new Vector3(
                col * spawnSpacing - (maxSamplesPerRow - 1) * spawnSpacing * 0.5f,
                0,
                row * spawnSpacing
            );
            
            return basePosition + offset;
        }
        
        /// <summary>
        /// 播放生成效果
        /// </summary>
        private void PlayGenerationEffect(Vector3 position)
        {
            if (generationConfig.generationEffectPrefab != null)
            {
                GameObject effect = Instantiate(generationConfig.generationEffectPrefab, position, Quaternion.identity);
                Destroy(effect, generationConfig.effectDuration);
            }
        }
        
        /// <summary>
        /// 播放生成音效
        /// </summary>
        private void PlayGenerationSound()
        {
            if (generationConfig.generationSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(generationConfig.generationSound, generationConfig.soundVolume);
            }
        }
        
        /// <summary>
        /// 自动收集样本到背包
        /// </summary>
        private System.Collections.IEnumerator AutoCollectSamples(SingleLayerSample[] samples)
        {
            yield return new WaitForSeconds(collectionDelay);
            
            Debug.Log($"开始自动收集 {samples.Length} 个切割样本");
            
            foreach (var sample in samples)
            {
                if (sample?.sampleObject != null)
                {
                    // 使用标准收集器（现在是唯一的收集器）
                    var standardCollector = sample.sampleObject.GetComponent<SampleCollector>();
                    if (standardCollector != null)
                    {
                        Debug.Log($"使用标准收集器收集样本: {sample.sampleID}");
                        
                        // 尝试使用扩展方法收集
                        bool collected = standardCollector.TryCollectSample();
                        if (!collected)
                        {
                            Debug.LogWarning($"扩展方法收集失败，尝试强制收集: {sample.sampleID}");
                            // 如果扩展方法失败，使用强制收集
                            collected = standardCollector.ForceCollectSample();
                        }
                        
                        if (!collected)
                        {
                            Debug.LogError($"所有收集方法都失败，直接销毁样本: {sample.sampleID}");
                            // 最后的备用方案：直接销毁（但这只会销毁样本对象，不会影响实验台）
                            Destroy(sample.sampleObject, 0.5f);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"样本 {sample.sampleID} 没有收集器组件，直接销毁");
                        // 直接销毁样本对象（不影响其他对象）
                        Destroy(sample.sampleObject, 0.5f);
                    }
                }
                
                yield return new WaitForSeconds(0.2f); // 间隔收集，避免卡顿
            }
            
            Debug.Log("自动收集完成");
        }
        
        /// <summary>
        /// 获取已生成的样本列表
        /// </summary>
        public SingleLayerSample[] GetGeneratedSamples()
        {
            return generatedSamples.ToArray();
        }
        
        /// <summary>
        /// 清理已生成的样本
        /// </summary>
        public void ClearGeneratedSamples()
        {
            foreach (var sample in generatedSamples)
            {
                if (sample?.sampleObject != null)
                {
                    DestroyImmediate(sample.sampleObject);
                }
            }
            
            generatedSamples.Clear();
            currentSpawnIndex = 0;
        }
        
        /// <summary>
        /// Editor测试方法
        /// </summary>
        [ContextMenu("测试样本生成")]
        private void TestSampleGeneration()
        {
            Debug.Log("测试样本生成功能...");
            
            // 查找测试样本
            var reconstructor = FindFirstObjectByType<GeometricSampleReconstructor>();
            if (reconstructor != null)
            {
                Debug.Log("找到GeometricSampleReconstructor进行测试");
            }
            else
            {
                Debug.LogWarning("未找到测试样本");
            }
        }
    }
}