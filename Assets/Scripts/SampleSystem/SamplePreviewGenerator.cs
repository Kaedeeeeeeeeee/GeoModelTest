using UnityEngine;
using System.Collections;
using System.IO;

/// <summary>
/// 样本预览图生成器 - 为地质样本生成预览图
/// </summary>
public class SamplePreviewGenerator : MonoBehaviour
{
    [Header("截图设置")]
    public Camera previewCamera;
    public int textureWidth = 256;
    public int textureHeight = 256;
    public LayerMask previewLayerMask = -1;
    
    [Header("摄像机设置")]
    public float cameraDistance = 3f;
    public Vector3 cameraOffset = new Vector3(1f, 1f, 1f);
    public Color backgroundColor = Color.white;
    
    [Header("光照设置")]
    public Light previewLight;
    public LightType lightType = LightType.Directional;
    public Color lightColor = Color.white;
    public float lightIntensity = 1f;
    
    [Header("保存设置")]
    public bool saveToFile = false;
    public string saveDirectory = "SamplePreviews";
    
    // 单例模式
    public static SamplePreviewGenerator Instance { get; private set; }
    
    // 私有成员
    private RenderTexture renderTexture;
    private GameObject previewScene;
    
    void Awake()
    {
        // 单例模式设置
        if (Instance == null)
        {
            Instance = this;
            InitializeGenerator();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 初始化生成器
    /// </summary>
    void InitializeGenerator()
    {
        SetupPreviewCamera();
        SetupPreviewLight();
        SetupRenderTexture();
        
        Debug.Log("样本预览图生成器已初始化");
    }
    
    /// <summary>
    /// 设置预览摄像机
    /// </summary>
    void SetupPreviewCamera()
    {
        if (previewCamera == null)
        {
            GameObject cameraObj = new GameObject("SamplePreviewCamera");
            cameraObj.transform.SetParent(transform);
            previewCamera = cameraObj.AddComponent<Camera>();
        }
        
        previewCamera.backgroundColor = backgroundColor;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.cullingMask = previewLayerMask;
        previewCamera.orthographic = false;
        previewCamera.fieldOfView = 45f;
        previewCamera.enabled = false; // 只在需要时启用
    }
    
    /// <summary>
    /// 设置预览光照
    /// </summary>
    void SetupPreviewLight()
    {
        if (previewLight == null)
        {
            GameObject lightObj = new GameObject("SamplePreviewLight");
            lightObj.transform.SetParent(transform);
            previewLight = lightObj.AddComponent<Light>();
        }
        
        previewLight.type = lightType;
        previewLight.color = lightColor;
        previewLight.intensity = lightIntensity;
        previewLight.enabled = false; // 只在需要时启用
        
        // 设置光照方向
        if (lightType == LightType.Directional)
        {
            previewLight.transform.rotation = Quaternion.Euler(45f, -45f, 0f);
        }
    }
    
    /// <summary>
    /// 设置渲染纹理
    /// </summary>
    void SetupRenderTexture()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
        
        renderTexture = new RenderTexture(textureWidth, textureHeight, 24);
        renderTexture.antiAliasing = 4;
        renderTexture.Create();
    }
    
    /// <summary>
    /// 生成样本预览图（同步接口，实际使用异步方法）
    /// </summary>
    public void GenerateSamplePreview(GameObject sampleObject, System.Action<Texture2D> callback)
    {
        if (sampleObject == null || previewCamera == null)
        {
            Debug.LogWarning("无法生成预览图：样本对象或摄像机为空");
            callback?.Invoke(null);
            return;
        }
        
        GenerateSamplePreviewAsync(sampleObject, callback);
    }
    
    /// <summary>
    /// 异步生成预览图
    /// </summary>
    public void GenerateSamplePreviewAsync(GameObject sampleObject, System.Action<Texture2D> callback)
    {
        if (sampleObject == null || previewCamera == null)
        {
            Debug.LogWarning("无法生成预览图：样本对象或摄像机为空");
            callback?.Invoke(null);
            return;
        }
        
        StartCoroutine(GeneratePreviewCoroutineAsync(sampleObject, callback));
    }
    
    /// <summary>
    /// 生成预览图协程
    /// </summary>
    IEnumerator GeneratePreviewCoroutineAsync(GameObject sampleObject, System.Action<Texture2D> callback)
    {
        // 创建临时预览场景
        GameObject tempSample = CreateTempSampleCopy(sampleObject);
        if (tempSample == null)
        {
            callback?.Invoke(null);
            yield break;
        }
        
        // 计算样本边界
        Bounds sampleBounds = CalculateSampleBounds(tempSample);
        
        // 设置摄像机位置
        SetupCameraForSample(sampleBounds);
        
        // 启用光照和摄像机
        previewLight.enabled = true;
        previewCamera.enabled = true;
        previewCamera.targetTexture = renderTexture;
        
        // 等待一帧确保渲染完成
        yield return new WaitForEndOfFrame();
        
        // 渲染并捕获图像
        previewCamera.Render();
        
        // 读取渲染纹理
        Texture2D previewTexture = ReadRenderTexture();
        
        // 清理临时对象
        CleanupTempObjects(tempSample);
        
        // 保存文件（如果需要）
        if (saveToFile && previewTexture != null)
        {
            SavePreviewToFile(previewTexture, sampleObject.name);
        }
        
        callback?.Invoke(previewTexture);
    }
    
    
    /// <summary>
    /// 创建临时样本副本
    /// </summary>
    GameObject CreateTempSampleCopy(GameObject original)
    {
        if (original == null) return null;
        
        // 创建临时父对象
        if (previewScene == null)
        {
            previewScene = new GameObject("SamplePreviewScene");
            previewScene.transform.SetParent(transform);
        }
        
        // 复制样本对象
        GameObject tempSample = Instantiate(original, previewScene.transform);
        tempSample.name = "TempPreview_" + original.name;
        
        // 移除可能干扰的组件
        RemoveInterferenceComponents(tempSample);
        
        // 重置位置
        tempSample.transform.localPosition = Vector3.zero;
        tempSample.transform.localRotation = Quaternion.identity;
        
        return tempSample;
    }
    
    /// <summary>
    /// 移除干扰组件
    /// </summary>
    void RemoveInterferenceComponents(GameObject obj)
    {
        // 移除交互组件
        var collectors = obj.GetComponentsInChildren<SampleCollector>();
        foreach (var collector in collectors)
        {
            DestroyImmediate(collector);
        }
        
        var placedCollectors = obj.GetComponentsInChildren<PlacedSampleCollector>();
        foreach (var collector in placedCollectors)
        {
            DestroyImmediate(collector);
        }
        
        // 移除物理组件
        var rigidbodies = obj.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rigidbodies)
        {
            DestroyImmediate(rb);
        }
        
        // 移除音频组件
        var audioSources = obj.GetComponentsInChildren<AudioSource>();
        foreach (var audio in audioSources)
        {
            DestroyImmediate(audio);
        }
        
        // 移除粒子系统
        var particles = obj.GetComponentsInChildren<ParticleSystem>();
        foreach (var particle in particles)
        {
            DestroyImmediate(particle);
        }
    }
    
    /// <summary>
    /// 计算样本边界
    /// </summary>
    Bounds CalculateSampleBounds(GameObject sampleObject)
    {
        Renderer[] renderers = sampleObject.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length == 0)
        {
            return new Bounds(sampleObject.transform.position, Vector3.one);
        }
        
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        
        return bounds;
    }
    
    /// <summary>
    /// 为样本设置摄像机
    /// </summary>
    void SetupCameraForSample(Bounds sampleBounds)
    {
        Vector3 sampleCenter = sampleBounds.center;
        float sampleSize = Mathf.Max(sampleBounds.size.x, sampleBounds.size.y, sampleBounds.size.z);
        
        // 计算摄像机位置
        Vector3 cameraPosition = sampleCenter + cameraOffset.normalized * (sampleSize * cameraDistance);
        previewCamera.transform.position = cameraPosition;
        previewCamera.transform.LookAt(sampleCenter);
        
        // 调整光照位置
        if (previewLight != null && lightType != LightType.Directional)
        {
            previewLight.transform.position = cameraPosition + Vector3.up * sampleSize;
            previewLight.transform.LookAt(sampleCenter);
        }
    }
    
    /// <summary>
    /// 读取渲染纹理
    /// </summary>
    Texture2D ReadRenderTexture()
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        texture.Apply();
        
        RenderTexture.active = previous;
        return texture;
    }
    
    /// <summary>
    /// 清理临时对象
    /// </summary>
    void CleanupTempObjects(GameObject tempSample)
    {
        // 禁用摄像机和光照
        previewCamera.enabled = false;
        previewCamera.targetTexture = null;
        previewLight.enabled = false;
        
        // 销毁临时样本
        if (tempSample != null)
        {
            DestroyImmediate(tempSample);
        }
    }
    
    /// <summary>
    /// 保存预览图到文件
    /// </summary>
    void SavePreviewToFile(Texture2D texture, string sampleName)
    {
        if (texture == null) return;
        
        try
        {
            string directoryPath = Path.Combine(Application.persistentDataPath, saveDirectory);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            string fileName = $"{sampleName}_preview_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = Path.Combine(directoryPath, fileName);
            
            byte[] pngData = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, pngData);
            
            Debug.Log($"样本预览图已保存: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"保存预览图失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 生成简单的颜色预览
    /// </summary>
    public Texture2D GenerateSimpleColorPreview(Color color, int size = 64)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        Color[] pixels = new Color[size * size];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return texture;
    }
    
    /// <summary>
    /// 为样本项目生成图标
    /// </summary>
    public void GenerateIconForSampleItem(SampleItem sampleItem, System.Action<Sprite> callback)
    {
        if (sampleItem == null)
        {
            callback?.Invoke(null);
            return;
        }
        
        // 如果有地质层信息，使用第一层的颜色生成简单图标
        if (sampleItem.geologicalLayers != null && sampleItem.geologicalLayers.Count > 0)
        {
            Color layerColor = sampleItem.geologicalLayers[0].layerColor;
            Texture2D iconTexture = GenerateSimpleColorPreview(layerColor, 64);
            Sprite iconSprite = Sprite.Create(iconTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            callback?.Invoke(iconSprite);
        }
        else
        {
            // 生成默认图标
            Color defaultColor = sampleItem.sourceToolID switch
            {
                "1000" => new Color(0.8f, 0.6f, 0.4f), // 简易钻探 - 棕色
                "1001" => new Color(0.6f, 0.8f, 0.4f), // 钻塔 - 绿色
                _ => Color.gray
            };
            
            Texture2D iconTexture = GenerateSimpleColorPreview(defaultColor, 64);
            Sprite iconSprite = Sprite.Create(iconTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            callback?.Invoke(iconSprite);
        }
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
        
        if (previewScene != null)
        {
            DestroyImmediate(previewScene);
        }
    }
    
    /// <summary>
    /// 在Inspector中测试预览生成
    /// </summary>
    [ContextMenu("测试预览生成")]
    void TestPreviewGeneration()
    {
        // 查找场景中的第一个样本对象
        GeometricSampleInfo sample = FindFirstObjectByType<GeometricSampleInfo>();
        if (sample != null)
        {
            GenerateSamplePreviewAsync(sample.gameObject, (texture) =>
            {
                if (texture != null)
                {
                    Debug.Log($"预览图生成成功: {texture.width}x{texture.height}");
                }
                else
                {
                    Debug.LogWarning("预览图生成失败");
                }
            });
        }
        else
        {
            Debug.LogWarning("场景中未找到样本对象");
        }
    }
}