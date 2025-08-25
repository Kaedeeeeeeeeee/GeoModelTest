# 样本切割系统快速开始指南

## 🚀 一键集成（推荐）

### 第一步：打开Laboratory Scene
```
File → Open Scene → Assets/3D Laboratory Environment with Appratus/Scenes/Laboratory Scene.unity
```

### 第二步：使用自动集成工具
```
菜单栏 → 工具 → 样本切割系统 → 集成到实验室
```

在弹出的窗口中：
1. 设置切割台位置（推荐：x=2, y=1, z=0）
2. 确保所有选项都勾选
3. 点击 **"一键集成切割系统"**

### 第三步：运行测试
1. 点击Play按钮运行游戏
2. 使用钻探工具（SimpleDrill或DrillTower）采集多层样本
3. 按F键打开仓库
4. 拖拽多层样本到切割台
5. 开始切割体验！

## 🎮 游戏操作流程

### 准备阶段
1. **野外采集**: 使用钻探工具获得多层地质样本
2. **进入实验室**: 用场景切换器(SceneSwitcher)切换到Laboratory Scene
3. **打开仓库**: 按F键打开仓库界面

### 切割操作
1. **拖拽样本**: 从仓库拖拽多层样本到切割台的拖拽区域
   - 绿色高亮 = 可以放置
   - 红色高亮 = 无效样本
2. **查看分析**: 系统自动分析样本地层结构
3. **开始切割**: 点击"开始切割"按钮
4. **进行切割**: 
   - 观察移动的白色切割线
   - 绿色区域 = 成功切割区域（大小根据地层数自动调整）
   - 在绿色区域内按**空格键**进行切割
5. **重复切割**: 继续切割直到所有地层分离
6. **收集样本**: 切割成功后，单层样本自动进入背包

### 切割技巧
- **2层样本**: 成功区域40cm，相对容易
- **3层样本**: 成功区域20cm，需要一定技巧  
- **4层+样本**: 成功区域10cm，需要精确时机
- **失败惩罚**: 切割失败样本直接报废，无法重试

## 🔧 手动集成（高级用户）

如果自动集成失败，可以手动添加：

### 1. 创建切割台对象
```csharp
// 在Laboratory Scene中
GameObject cuttingStation = new GameObject("SampleCuttingStation");
cuttingStation.tag = "CuttingStation";
cuttingStation.transform.position = new Vector3(2f, 1f, 0f);
```

### 2. 添加系统组件
```csharp
// 按顺序添加这些组件
cuttingStation.AddComponent<SampleCuttingSystemManager>();
cuttingStation.AddComponent<SampleCuttingGame>();
cuttingStation.AddComponent<SampleLayerAnalyzer>();
cuttingStation.AddComponent<LayerDatabaseMapper>();
cuttingStation.AddComponent<CuttingStationUI>();
cuttingStation.AddComponent<SingleLayerSampleGenerator>();
cuttingStation.AddComponent<WarehouseIntegration>();
cuttingStation.AddComponent<SampleCuttingSystemInitializer>();
cuttingStation.AddComponent<AudioSource>();
```

### 3. 配置初始化器
```csharp
var initializer = cuttingStation.GetComponent<SampleCuttingSystemInitializer>();
// 设置 initializeOnStart = true（在Inspector中）
```

## ❗ 常见问题

### Q: 拖拽样本没有反应
**A**: 检查以下几点：
- 确保WarehouseIntegration组件已添加到切割台
- 验证样本确实是多层样本（2层以上）
- 检查拖拽区域是否正确设置

### Q: 切割线不动
**A**: 可能的原因：
- SampleCuttingGame组件缺失
- 样本分析失败
- UI组件引用未正确设置

### Q: 没有音效
**A**: 解决方法：
- 在 `Assets/Resources/Audio/CuttingSystem/` 放置音频文件：
  - `laser_hum.wav` - 切割声音
  - `cut_success.wav` - 成功音效
  - `cut_failure.wav` - 失败音效

### Q: 系统初始化失败
**A**: 检查Console错误信息：
- 确保在Laboratory Scene中
- 检查是否缺少必要组件
- 尝试使用 `工具 → 样本切割系统 → 集成到实验室` 重新集成

### Q: 切割后没有生成样本
**A**: 检查：
- SingleLayerSampleGenerator组件是否存在
- 样本生成位置设置
- 自动收集功能是否启用

## 🔍 调试工具

### Inspector调试
在切割台对象上找到各个组件，都有Context Menu测试方法：
- **SampleLayerAnalyzer**: `测试样本分析`
- **LayerDatabaseMapper**: `显示数据库信息`
- **SampleCuttingSystemManager**: `检查系统健康状态`

### 控制台命令
```csharp
// 在任何MonoBehaviour中可以调用
var manager = FindObjectOfType<SampleCuttingSystemManager>();
bool isHealthy = manager.CheckSystemHealth();
var stats = manager.GetStatistics();
```

### 编辑器工具
```
工具 → 样本切割系统 → 集成到实验室
- 一键集成/移除
- 系统完整性检查
- 创建测试样本
```

## 📊 系统统计

游戏会自动记录：
- 总切割会话数
- 成功/失败率
- 生成的样本总数
- 平均每次生成样本数

可以通过 `manager.GetStatistics()` 获取详细数据。

---

**快速开始提示**: 
1. 打开Laboratory Scene
2. 使用 `工具 → 样本切割系统 → 集成到实验室`
3. 一键集成并开始体验！

**需要帮助?** 查看完整文档：`Assets/Scripts/SampleCuttingSystem/README.md`