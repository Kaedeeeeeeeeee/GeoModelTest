# 图鉴系统Unity设置指南

这个指南将帮助您在Unity项目中正确设置图鉴系统。

## 自动设置方法（推荐）

### 步骤 1: 创建系统初始化器
1. 在场景中创建一个空GameObject，命名为"EncyclopediaSystem"
2. 添加`EncyclopediaInitializer`脚本到这个GameObject
3. 在Inspector中确保`Auto Create UI`选项已勾选
4. 运行游戏，系统将自动创建所有需要的组件

### 步骤 2: 验证设置
运行游戏后，检查Console输出：
- 应该看到"图鉴系统状态检查"的日志
- 确认数据系统、收集系统和UI系统都显示"✓"

### 步骤 3: 测试功能
1. 在运行时按O键打开图鉴界面
2. 可以使用EncyclopediaInitializer的右键菜单进行测试：
   - "测试发现条目"：发现一个测试条目
   - "打开图鉴"：直接打开图鉴界面

## 手动设置方法

如果需要更多控制，可以手动设置系统：

### 步骤 1: 创建数据管理器
1. 创建GameObject命名为"EncyclopediaData"
2. 添加`EncyclopediaData`脚本
3. 配置数据文件路径（默认设置通常就可以）

### 步骤 2: 创建收集管理器
1. 创建GameObject命名为"CollectionManager"  
2. 添加`CollectionManager`脚本
3. 配置保存设置

### 步骤 3: 创建UI系统
1. 创建GameObject命名为"EncyclopediaUISetup"
2. 添加`EncyclopediaUISetup`脚本
3. 在Inspector中右键点击脚本标题，选择"创建图鉴UI"
4. 系统将自动生成完整的UI结构

### 步骤 4: 连接UI引用
创建UI后，需要在`EncyclopediaUI`组件中手动连接以下引用：
- encyclopediaPanel: EncyclopediaPanel
- closeButton: EncyclopediaPanel/CloseButton
- layerTabContainer: LeftPanel/LayerTabContainer
- layerTabPrefab: LayerTabContainer/LayerTabPrefab
- statisticsText: LeftPanel/StatisticsPanel/StatisticsText
- entryListContainer: RightPanel/EntryListContainer/Viewport/Content
- entryItemPrefab: Content/EntryItemPrefab
- entryScrollRect: RightPanel/EntryListContainer
- 筛选控件：FilterPanel下的各个组件
- detailPanel: DetailPanel
- 详细信息组件：DetailPanel下的各个Text和Image组件
- model3DViewer: DetailPanel/Model3DViewer

## 资源文件结构

确保您的Resources文件夹结构如下：
```
Resources/
├── MineralData/
│   ├── Data/
│   │   └── SendaiMineralDatabase.json
│   ├── Images/
│   │   ├── Minerals/
│   │   │   ├── [矿物图片].png
│   │   └── Fossil/
│   │       ├── [化石图片].png
│   └── Models/
│       ├── Minerals/
│       │   ├── [矿物模型].prefab
│       └── Fossil/
│           ├── [化石模型].prefab
```

## 控制说明

- **O键**: 开关图鉴界面
- **鼠标拖拽**: 在3D查看器中旋转模型
- **鼠标滚轮**: 在3D查看器中缩放模型
- **重置按钮**: 重置3D模型视角

## 常见问题

### Q: 图鉴界面不显示
A: 检查Canvas是否正确创建，确保没有其他UI遮挡

### Q: 图片/模型不显示
A: 检查Resources文件夹路径是否正确，文件名是否匹配JSON数据

### Q: 数据没有加载
A: 检查SendaiMineralDatabase.json文件是否在正确位置

### Q: 按键无响应
A: 确保EncyclopediaUI脚本在激活的GameObject上

## 脚本执行顺序

系统组件的初始化顺序很重要：
1. EncyclopediaData (加载JSON和资源)
2. CollectionManager (加载收集进度)
3. EncyclopediaUI (初始化界面)

如果遇到初始化问题，可以在Project Settings > Script Execution Order中设置：
- EncyclopediaData: -100
- CollectionManager: -50  
- EncyclopediaUI: 0

## 性能优化建议

1. **图片资源**: 使用适当的压缩格式，建议512x512或1024x1024分辨率
2. **3D模型**: 控制面数，建议1000-5000面以内
3. **UI更新**: 系统已优化，只在需要时更新列表

## 扩展功能

系统已预留扩展接口：
- 添加新的筛选条件
- 自定义条目显示格式
- 集成音效和动画
- 多语言支持

---

设置完成后，您的图鉴系统就可以正常使用了！按O键即可打开精美的科技风格图鉴界面。