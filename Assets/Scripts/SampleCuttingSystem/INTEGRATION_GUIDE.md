# 样本切割系统实验室集成指南

## 集成步骤

### 第一步：场景准备

1. **打开Laboratory Scene**
```
Assets/3D Laboratory Environment with Appratus/Scenes/Laboratory Scene.unity
```

2. **找到合适的位置放置切割台**
建议放在实验台附近，便于玩家操作。

### 第二步：创建切割台GameObject

在Laboratory Scene中创建切割台：

```csharp
// 在场景中创建空对象
1. 右键 Hierarchy → Create Empty
2. 命名为 "SampleCuttingStation"
3. 添加 Tag: "CuttingStation"
4. 位置设置到实验台附近（例如：Vector3(2, 1, 0)）
```

### 第三步：添加系统组件

将以下脚本按顺序添加到 SampleCuttingStation：

```csharp
// 1. 核心系统管理器（必须第一个添加）
SampleCuttingSystemManager

// 2. 游戏控制器
SampleCuttingGame

// 3. 分析器组件
SampleLayerAnalyzer
LayerDatabaseMapper

// 4. UI管理器
CuttingStationUI

// 5. 样本生成器
SingleLayerSampleGenerator

// 6. 仓库集成
WarehouseIntegration

// 7. 音频组件（自动添加）
AudioSource
```

### 第四步：创建UI Canvas

1. **创建切割台专用UI**：
```csharp
// 在SampleCuttingStation下创建子对象
GameObject cuttingUI = new GameObject("CuttingStationCanvas");
Canvas canvas = cuttingUI.AddComponent<Canvas>();
canvas.renderMode = RenderMode.WorldSpace;
canvas.worldCamera = Camera.main;

// 设置Canvas位置（在切割台上方）
cuttingUI.transform.position = cuttingStationTransform.position + Vector3.up * 2f;
cuttingUI.transform.localScale = Vector3.one * 0.01f; // 缩放以适应世界空间
```

2. **创建UI面板结构**：
```
CuttingStationCanvas
├── MainPanel (Image背景)
│   ├── SampleDisplayArea
│   │   ├── Sample3DPreview (RawImage)
│   │   └── LayerDiagramArea (ScrollRect)
│   ├── CuttingArea  
│   │   ├── CuttingLine (Image)
│   │   └── SuccessZone (Image)
│   ├── InfoPanel
│   │   ├── SampleNameText (Text)
│   │   ├── LayerCountText (Text)
│   │   └── InstructionText (Text)
│   └── ButtonPanel
│       ├── StartCuttingButton (Button)
│       ├── CancelButton (Button)
│       └── SpaceKeyIcon (Image)
```

### 第五步：配置拖拽区域

1. **创建拖拽接收区域**：
```csharp
// 在实验台上创建拖拽区域
GameObject dropZone = new GameObject("SampleDropZone");
dropZone.transform.SetParent(cuttingStationTransform);
dropZone.transform.localPosition = Vector3.zero;

// 添加UI组件
RectTransform rectTransform = dropZone.AddComponent<RectTransform>();
Image backgroundImage = dropZone.AddComponent<Image>();
backgroundImage.color = new Color(1f, 1f, 1f, 0.1f); // 半透明白色

// 设置大小
rectTransform.sizeDelta = new Vector2(200, 100);
```

### 第六步：自动集成到GameInitializer

修改 `GameInitializer.cs` 来自动初始化切割系统：

```csharp
// 在GameInitializer.cs的InitializeSystems()方法中添加：

private void InitializeCuttingSystem()
{
    Debug.Log("初始化样本切割系统...");
    
    // 查找切割台
    GameObject cuttingStation = GameObject.FindGameObjectWithTag("CuttingStation");
    
    if (cuttingStation == null)
    {
        Debug.Log("当前场景没有切割台，跳过初始化");
        return;
    }
    
    // 获取或添加初始化器
    var initializer = cuttingStation.GetComponent<SampleCuttingSystemInitializer>();
    if (initializer == null)
    {
        initializer = cuttingStation.AddComponent<SampleCuttingSystemInitializer>();
    }
    
    // 初始化系统
    initializer.InitializeSystem();
    
    Debug.Log("样本切割系统初始化完成");
}

// 在Start()方法中调用
void Start()
{
    // ... 现有代码 ...
    
    InitializeCuttingSystem(); // 添加这行
}
```

### 第七步：配置音频资源

1. **准备音频文件**：
   - 激光嗡嗡声：`laser_hum.wav`
   - 成功音效：`cut_success.wav`  
   - 失败音效：`cut_failure.wav`

2. **放置到Resources文件夹**：
```
Assets/Resources/Audio/CuttingSystem/
├── laser_hum.wav
├── cut_success.wav
└── cut_failure.wav
```

### 第八步：本地化集成

在本地化JSON文件中添加切割系统文本：

```json
// zh-CN.json
{
    "cutting.instruction.drag": "将多层样本拖拽到此处",
    "cutting.instruction.cutting": "按空格键进行切割",
    "cutting.instruction.success": "切割成功！",
    "cutting.instruction.failed": "切割失败，样本损坏",
    "cutting.button.start": "开始切割",
    "cutting.button.cancel": "取消",
    "cutting.info.layers": "地层数量: {0}",
    "cutting.info.cuts": "需要切割: {0} 次"
}

// en-US.json
{
    "cutting.instruction.drag": "Drag multi-layer sample here",
    "cutting.instruction.cutting": "Press SPACE to cut",
    "cutting.instruction.success": "Cutting successful!",
    "cutting.instruction.failed": "Cutting failed, sample destroyed",
    "cutting.button.start": "Start Cutting",
    "cutting.button.cancel": "Cancel",
    "cutting.info.layers": "Layers: {0}",
    "cutting.info.cuts": "Cuts needed: {0}"
}
```

### 第九步：测试集成

1. **运行测试**：
```csharp
// 在切割台上添加测试脚本（临时）
[ContextMenu("测试切割系统")]
private void TestCuttingSystem()
{
    var manager = GetComponent<SampleCuttingSystemManager>();
    if (manager != null)
    {
        bool isHealthy = manager.CheckSystemHealth();
        Debug.Log($"切割系统健康状态: {isHealthy}");
        
        var stats = manager.GetStatistics();
        Debug.Log($"系统统计: {stats.totalCuttingSessions} 次会话");
    }
}
```

2. **验证拖拽功能**：
   - 打开仓库UI
   - 选择多层样本
   - 拖拽到切割台
   - 验证拖拽区域高亮显示

### 第十步：优化和调试

1. **性能优化**：
```csharp
// 在CuttingStationUI中设置合理的更新频率
void Update()
{
    // 限制更新频率
    if (Time.frameCount % 5 == 0)
    {
        UpdatePreviewRotation();
    }
}
```

2. **调试选项**：
```csharp
// 在SampleCuttingSystemManager中启用调试
[SerializeField] private bool enableDebugMode = true;
```

## 完整集成代码示例

以下是一个完整的自动集成脚本：

```csharp
[MenuItem("工具/集成样本切割系统到实验室")]
public static void IntegrateCuttingSystemToLaboratory()
{
    // 1. 检查是否在Laboratory Scene
    if (!SceneManager.GetActiveScene().name.Contains("Laboratory"))
    {
        Debug.LogError("请先打开Laboratory Scene");
        return;
    }
    
    // 2. 创建切割台
    GameObject cuttingStation = new GameObject("SampleCuttingStation");
    cuttingStation.tag = "CuttingStation";
    cuttingStation.transform.position = new Vector3(2f, 1f, 0f);
    
    // 3. 添加所有必要组件
    cuttingStation.AddComponent<SampleCuttingSystemManager>();
    cuttingStation.AddComponent<SampleCuttingGame>();
    cuttingStation.AddComponent<SampleLayerAnalyzer>();
    cuttingStation.AddComponent<LayerDatabaseMapper>();
    cuttingStation.AddComponent<CuttingStationUI>();
    cuttingStation.AddComponent<SingleLayerSampleGenerator>();
    cuttingStation.AddComponent<WarehouseIntegration>();
    cuttingStation.AddComponent<AudioSource>();
    
    // 4. 添加初始化器
    var initializer = cuttingStation.AddComponent<SampleCuttingSystemInitializer>();
    initializer.initializeOnStart = true;
    
    Debug.Log("样本切割系统已成功集成到Laboratory Scene!");
}
```

## 集成后的使用流程

1. **启动游戏** → Laboratory Scene 自动初始化切割系统
2. **打开仓库** → F键进入仓库界面  
3. **选择样本** → 点击多层地质样本
4. **拖拽样本** → 拖拽到切割台的拖拽区域
5. **开始切割** → 点击"开始切割"按钮
6. **进行切割** → 观察移动横条，在绿色区域按空格
7. **收集样本** → 切割成功后样本自动进入背包

## 故障排除

### 常见问题及解决方案

1. **切割台不可见**
   - 检查GameObject是否激活
   - 验证位置是否在相机视野内

2. **拖拽不工作**  
   - 确认WarehouseIntegration组件已添加
   - 检查UI层级和Canvas设置

3. **音效无法播放**
   - 验证音频文件是否正确放置在Resources文件夹
   - 检查AudioSource组件设置

4. **样本无法切割**
   - 确认样本有多层结构
   - 检查LayerDatabaseMapper是否成功加载数据库

这样就完成了切割系统到实验室的完整集成！