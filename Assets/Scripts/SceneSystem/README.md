# 场景切换器系统说明

## 系统概述
场景切换器是一个集成到工具系统中的特殊工具，允许玩家在不同场景之间自由切换。工具ID为999，在Tab工具轮盘中显示。

## 功能特性

### 1. 场景切换器工具 (SceneSwitcherTool)
- **工具ID**: 999
- **功能**: 点击鼠标左键显示场景选择UI
- **视觉效果**: 金黄色发光设备，装备时显示在玩家手中
- **动画**: 激活时有上举和旋转动画

### 2. 场景管理器 (GameSceneManager)
- **单例模式**: 确保全局唯一性
- **场景配置**: 支持多个场景的配置和管理
- **异步加载**: 平滑的场景切换体验
- **UI管理**: 自动创建和管理场景选择界面

### 3. 数据持久化 (PlayerPersistentData)
- **玩家位置**: 在场景间保持玩家位置和朝向
- **工具状态**: 保持当前装备的工具
- **场景独立**: 每个场景的样本数据独立，切换后重新开始

### 4. 场景自动设置 (SceneAutoSetup)
- **Player系统**: 自动创建FirstPersonController和摄像机
- **UI系统**: 自动创建InventoryUISystem和EventSystem
- **工具系统**: 自动创建ToolManager和场景切换器
- **智能检测**: 只在缺失组件时才创建，避免重复

## 使用方法

### 玩家操作
1. 按 **Tab** 键打开工具轮盘
2. 选择 **场景切换器** (工具ID: 999)
3. 点击 **鼠标左键** 显示场景选择UI
4. 选择要切换的场景
5. 等待加载完成

### 场景选择UI
- **当前场景**: 显示为灰色，不可点击
- **其他场景**: 显示为蓝色，可点击切换
- **关闭按钮**: 红色"关闭"按钮取消选择

## 开发者设置

### 1. 使用编辑器工具
1. 菜单栏选择 **Tools > 场景切换器设置**
2. 点击 **创建场景切换器预制体**
3. 点击 **设置场景切换器初始化器**
4. 点击 **添加场景管理器到场景**

### 2. Build Settings配置
确保在Build Settings中添加所有场景：
- Assets/Scenes/MainScene.unity
- Assets/3D Laboratory Environment with Appratus/Scenes/Laboratory Scene.unity

### 3. 手动设置
如果需要手动设置：

#### 创建场景管理器
```csharp
GameObject managerObj = new GameObject("GameSceneManager");
GameSceneManager sceneManager = managerObj.AddComponent<GameSceneManager>();
managerObj.AddComponent<PlayerPersistentData>();
DontDestroyOnLoad(managerObj);
```

#### 添加场景切换器初始化器
```csharp
GameObject initializerObj = new GameObject("SceneSwitcherInitializer");
SceneSwitcherInitializer initializer = initializerObj.AddComponent<SceneSwitcherInitializer>();
```

## 场景配置

### 默认场景配置
```csharp
public SceneConfig[] availableScenes = {
    new SceneConfig("MainScene", "野外", "主要的地质勘探场景"),
    new SceneConfig("Laboratory Scene", "研究室", "样本分析和研究场景")
};
```

### 添加新场景
1. 在GameSceneManager的availableScenes数组中添加新的SceneConfig
2. 确保场景文件存在于项目中
3. 在Build Settings中添加场景

## 技术细节

### 文件结构
```
Assets/Scripts/SceneSystem/
├── GameSceneManager.cs       # 场景管理器
├── PlayerPersistentData.cs   # 数据持久化
├── SceneSwitcherInitializer.cs # 初始化器
├── SceneAutoSetup.cs         # 场景自动设置
├── SceneInitializer.cs       # 场景初始化监听器
├── SceneSetupTester.cs       # 系统测试器
└── README.md                 # 说明文档

Assets/Scripts/Tools/
└── SceneSwitcherTool.cs      # 场景切换器工具

Assets/Scripts/Editor/
└── SceneSwitcherSetupTool.cs # 编辑器设置工具
```

### 依赖关系
- **SceneSwitcherTool** 依赖 **GameSceneManager**
- **GameSceneManager** 依赖 **PlayerPersistentData**
- **SceneSwitcherInitializer** 管理整个系统的初始化

### 数据持久化
- 玩家位置和朝向
- 当前装备的工具ID
- 场景独立性（样本数据不跨场景保存）

## 故障排除

### 常见问题
1. **场景切换器不显示在工具轮盘中**
   - 检查SceneSwitcherInitializer是否存在
   - 确认工具ID为"999"
   - 检查ToolManager和InventoryUISystem是否正常

2. **场景切换失败**
   - 检查Build Settings中是否包含目标场景
   - 确认场景文件路径正确
   - 检查控制台错误信息

3. **数据持久化不工作**
   - 确认PlayerPersistentData组件存在
   - 检查enableDataPersistence是否为true
   - 确认场景间有正确的初始化流程

### 调试方法
- 查看控制台日志
- 检查场景管理器的单例状态
- 验证工具系统的初始化顺序

## 扩展功能

### 添加新工具
参考SceneSwitcherTool的实现，创建继承自CollectionTool的新工具类。

### 场景预加载
可以在GameSceneManager中添加场景预加载功能，提升切换体验。

### 更丰富的UI
可以扩展场景选择UI，添加场景预览图、描述等功能。

### 数据持久化扩展
可以扩展PlayerPersistentData，支持更多游戏数据的保存。当前系统采用场景独立设计，每个场景的探索数据独立管理。

## 注意事项

1. **工具ID唯一性**: 确保工具ID "999" 不与其他工具冲突
2. **场景初始化顺序**: 确保场景管理器在其他系统之前初始化
3. **内存管理**: 场景切换时注意清理不必要的资源
4. **性能优化**: 大场景切换时可能需要优化加载时间

---

**开发者**: 场景切换器系统
**版本**: 1.0
**更新日期**: 2025-07-15