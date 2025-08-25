# 样本切割系统 (Sample Cutting System)

## 概述

样本切割系统是一个完整的小游戏系统，允许玩家将多层地质样本切割成纯净的单层样本，为后续的矿物提取和实验提供基础。系统采用经典的移动横条时机按键游戏机制，类似于《刺客信条：大革命》中的开锁系统。

## 核心特性

### 🎮 游戏机制
- **移动横条切割**: 横条在样本上下移动，玩家按空格键在正确时机进行切割
- **地层边界识别**: 自动识别多层样本中的地层边界和成功切割区域
- **难度自适应**: 根据地层数量自动调整成功区域大小
  - 2层样本: 40cm成功区域
  - 3层样本: 20cm成功区域  
  - 4层+样本: 10cm成功区域

### 🔬 真实地质数据
- **图鉴系统集成**: 自动从SendaiMineralDatabase.json查询地层对应的矿物信息
- **地层名称映射**: 支持中文、英文、日文地层名称的智能匹配
- **矿物成分分析**: 为每个切割后的样本提供详细的矿物组成信息

### 🎨 视觉和音效
- **2D剖面图显示**: 程序化生成地质柱状图，清晰显示地层结构
- **3D样本预览**: 实时旋转的3D样本预览
- **激光切割效果**: 嗡嗡声音效，成功/失败时的绿色/红色闪光反馈
- **多语言支持**: 完整的中英日三语言界面

### 🏭 系统集成
- **仓库系统**: 支持从仓库拖拽多层样本到切割台
- **自动收集**: 切割成功后样本自动进入玩家背包
- **实验室环境**: 专为Laboratory Scene设计的工作台

## 系统架构

### 核心组件

#### 1. SampleCuttingGame.cs
- **功能**: 切割小游戏的核心控制器
- **职责**: 横条移动、输入检测、时机判定、音效播放
- **关键方法**:
  - `StartCutting()`: 开始切割指定样本
  - `PerformCut()`: 执行切割操作
  - `HandleSuccessfulCut()`: 处理成功切割
  - `HandleFailedCut()`: 处理失败切割

#### 2. SampleLayerAnalyzer.cs
- **功能**: 样本地层结构分析
- **职责**: 识别地层边界、计算成功区域、生成切割数据
- **关键方法**:
  - `AnalyzeLayerBoundaries()`: 分析样本边界
  - `CanSampleBeCut()`: 验证样本是否可切割
  - `GetSampleInfo()`: 获取样本详细信息

#### 3. LayerDatabaseMapper.cs
- **功能**: 地层数据库映射器
- **职责**: 查询图鉴数据库，获取地层对应的矿物信息
- **关键方法**:
  - `GetMineralsForLayer()`: 根据地层名称获取矿物信息
  - `LoadDatabase()`: 加载SendaiMineralDatabase.json
  - `FindMatchingLayer()`: 智能地层名称匹配

#### 4. CuttingStationUI.cs
- **功能**: 切割台用户界面管理
- **职责**: 2D剖面图显示、3D预览、UI状态管理
- **关键方法**:
  - `LoadSample()`: 加载样本到UI
  - `GenerateLayerDiagram()`: 生成地层图
  - `CreateSamplePreview()`: 创建3D预览

#### 5. SingleLayerSampleGenerator.cs
- **功能**: 单层样本生成器
- **职责**: 创建切割后的独立样本对象
- **关键方法**:
  - `GenerateSamplesFromMultiLayer()`: 从多层样本生成单层样本
  - `CreateSampleGameObject()`: 创建样本3D对象
  - `CalculatePhysicalProperties()`: 计算物理属性

#### 6. SampleCuttingSystemManager.cs
- **功能**: 系统管理器
- **职责**: 协调所有组件，提供统一接口
- **关键方法**:
  - `StartCuttingSample()`: 主要入口点
  - `HandleCuttingSuccess()`: 成功处理流程
  - `GetStatistics()`: 获取系统统计

#### 7. WarehouseIntegration.cs
- **功能**: 仓库系统集成
- **职责**: 处理拖拽交互，验证样本类型
- **关键方法**:
  - `OnDrop()`: 处理拖拽放置
  - `ValidateDraggedObject()`: 验证拖拽对象
  - `StartCuttingProcess()`: 启动切割流程

### 数据结构

#### SingleLayerSample
切割后的单层样本数据：
```csharp
public class SingleLayerSample
{
    public string sampleID;              // 样本唯一ID
    public string layerName;             // 地层名称
    public GameObject sampleObject;      // 3D显示对象
    public MineralComposition[] minerals; // 矿物组成
    public bool isCutFromMultiLayer;     // 是否来自切割
    // ... 其他属性
}
```

#### MineralComposition
矿物成分数据：
```csharp
public class MineralComposition
{
    public string mineralName;          // 矿物名称
    public float percentage;            // 含量百分比
    public string imageFile;            // 图片文件
    public string modelFile;            // 3D模型文件
    public MineralProperties properties; // 详细属性
}
```

## 使用方法

### 基础设置

1. **在Laboratory Scene中添加切割台**:
```csharp
// 自动初始化（推荐）
var initializer = gameObject.AddComponent<SampleCuttingSystemInitializer>();

// 手动初始化
var manager = gameObject.AddComponent<SampleCuttingSystemManager>();
manager.InitializeSystem();
```

2. **设置拖拽区域**:
```csharp
var warehouseIntegration = gameObject.AddComponent<WarehouseIntegration>();
// 配置拖拽接收区域
```

### 使用流程

1. **玩家操作流程**:
   - 在仓库中选择多层样本
   - 拖拽样本到切割台的拖拽区域
   - 系统自动分析样本地层结构
   - 点击"开始切割"按钮
   - 观察移动的切割线，在绿色成功区域内按空格键
   - 重复切割直到所有地层分离
   - 收集生成的单层样本到背包

2. **程序调用流程**:
```csharp
// 获取系统管理器
var manager = FindObjectOfType<SampleCuttingSystemManager>();

// 检查系统状态
if (manager.GetSystemState().isOccupied)
{
    Debug.Log("切割台忙碌中");
    return;
}

// 开始切割
bool success = manager.StartCuttingSample(reconstructedSample);

// 监听事件
manager.OnSamplesGenerated += OnSamplesCreated;
manager.OnCuttingCompleted += OnCuttingFinished;
```

### 自定义配置

#### 成功区域设置
```csharp
var analyzer = GetComponent<SampleLayerAnalyzer>();
// 修改successZoneSettings数组来调整难度
```

#### 样本生成配置
```csharp
var generator = GetComponent<SingleLayerSampleGenerator>();
generator.generationConfig.sampleScale = 1.2f;      // 样本缩放
generator.generationConfig.enablePhysics = true;     // 启用物理
generator.generationConfig.autoCollect = true;       // 自动收集
```

## 调试和测试

### 调试选项
- 启用`enableDebugMode`查看详细日志
- 使用Context Menu测试各个组件
- 检查系统健康状态：`manager.CheckSystemHealth()`

### 常见问题

#### 1. 样本无法切割
- 检查样本是否有多层：`analyzer.CanSampleBeCut(sample)`
- 验证样本数据完整性：`sample.layerSegments != null`

#### 2. 矿物信息缺失
- 确保SendaiMineralDatabase.json已加载
- 检查地层名称映射：`mapper.IsLayerInDatabase(layerName)`

#### 3. UI显示异常
- 验证UI组件引用完整
- 检查Canvas设置

### 性能优化

1. **对象池**: 可以为生成的样本实现对象池
2. **异步加载**: 大型样本的分析可以异步进行
3. **LOD系统**: 3D预览可以使用简化模型

## 扩展性

### 新增切割工具
可以扩展支持不同类型的切割工具：
```csharp
public abstract class CuttingTool
{
    public abstract void PerformCut(LayerBoundary boundary);
}
```

### 新增样本类型
支持新的样本格式：
```csharp
public interface ISampleCuttable
{
    bool CanBeCut();
    LayerBoundary[] GetLayerBoundaries();
}
```

### 新增小游戏机制
可以替换移动横条机制：
```csharp
public abstract class CuttingGameMode
{
    public abstract void StartCutting();
    public abstract bool CheckCuttingAccuracy();
}
```

## 版本历史

- **v1.0**: 基础切割系统，支持移动横条游戏机制
- **v1.1**: 集成图鉴数据库，自动矿物信息查询
- **v1.2**: 仓库系统集成，拖拽交互
- **v1.3**: 多语言支持，完整UI系统

## 许可证

此系统为GeoClone1项目的一部分，遵循项目整体许可证。

---

**开发者**: Claude Code Assistant  
**最后更新**: 2025-08-18  
**兼容版本**: Unity 2022.3+ LTS