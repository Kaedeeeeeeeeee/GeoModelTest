# Unity多语言系统 (Localization System)

## 概述

这是一个完整的Unity多语言本地化系统，支持中文、英语、日语三种语言。系统采用Key-Value映射方式，通过JSON文件存储语言数据，支持运行时动态切换语言。

## 主要特性

- ✅ 支持三种语言：中文、英语、日语
- ✅ 基于Key-Value的文本映射系统
- ✅ JSON格式的语言文件，易于编辑和维护
- ✅ 运行时动态语言切换
- ✅ 自动保存和加载语言设置
- ✅ ESC键触发的设置界面
- ✅ LocalizedText组件，可附加到任何Text组件
- ✅ 单例模式的管理器，全局访问
- ✅ 事件驱动的UI更新机制
- ✅ Editor工具支持，方便开发

## 系统架构

### 核心组件

1. **LocalizationManager** - 单例管理器，负责所有语言操作
2. **LocalizedText** - UI组件，可附加到Text/TextMeshPro组件
3. **SettingsManager** - 设置界面管理器，处理ESC键和语言切换UI
4. **LanguageSettings** - 语言设置和枚举定义
5. **LocalizationInitializer** - 系统初始化器

### 文件结构

```
Assets/Scripts/Localization/
├── LocalizationManager.cs      # 核心管理器
├── LocalizedText.cs            # 本地化文本组件
├── SettingsManager.cs          # 设置界面管理器
├── LanguageSettings.cs         # 语言设置
├── LocalizationInitializer.cs  # 系统初始化器
├── LocalizationDemo.cs         # 演示脚本
├── Editor/
│   └── LocalizationTools.cs    # 编辑器工具
└── Data/                       # 语言数据文件
    ├── zh-CN.json              # 中文
    ├── en-US.json              # 英文
    └── ja-JP.json              # 日文

Assets/Resources/Localization/Data/  # Unity Resources目录
├── zh-CN.json
├── en-US.json
└── ja-JP.json
```

## 使用方法

### 1. 系统初始化

系统会在GameInitializer中自动初始化。如果需要手动初始化：

```csharp
// 获取LocalizationManager实例（自动创建）
var localizationManager = LocalizationManager.Instance;

// 创建LocalizationInitializer来确保系统初始化
GameObject initializerObj = new GameObject("LocalizationInitializer");
LocalizationInitializer initializer = initializerObj.AddComponent<LocalizationInitializer>();
```

### 2. 为UI文本添加本地化支持

#### 方法1：通过Editor工具（推荐）

1. 打开 `Tools -> Localization -> 多语言工具`
2. 选择包含Text组件的GameObject
3. 点击"为选中的Text组件添加LocalizedText"

#### 方法2：手动添加

```csharp
// 为Text组件添加LocalizedText
Text myText = GetComponent<Text>();
LocalizedText localizedText = gameObject.AddComponent<LocalizedText>();
localizedText.TextKey = "ui.button.start";  // 设置文本键
```

#### 方法3：代码中创建

```csharp
// 在创建UI时直接添加本地化组件
GameObject buttonObj = new GameObject("StartButton");
Text buttonText = buttonObj.AddComponent<Text>();
buttonText.text = "开始游戏";  // 临时文本，会被本地化文本替换

LocalizedText localizedText = buttonObj.AddComponent<LocalizedText>();
localizedText.TextKey = "ui.button.start";
```

### 3. 语言切换

#### 通过设置界面

- 按ESC键打开设置界面
- 点击相应的语言按钮（中文/English/日本語）

#### 通过代码

```csharp
// 切换到英语
LocalizationManager.Instance.SwitchLanguage(LanguageSettings.Language.English);

// 切换到日语
LocalizationManager.Instance.SwitchLanguage(LanguageSettings.Language.Japanese);

// 切换到中文
LocalizationManager.Instance.SwitchLanguage(LanguageSettings.Language.ChineseSimplified);
```

### 4. 获取本地化文本

```csharp
// 获取简单文本
string text = LocalizationManager.Instance.GetText("ui.button.start");

// 获取带格式化参数的文本
string formattedText = LocalizationManager.Instance.GetText("warehouse.button.transfer_to_storage", 5);
// 结果: "放入仓库 (5)" 或 "To Storage (5)"

// 检查文本是否存在
bool hasText = LocalizationManager.Instance.HasText("ui.button.start");
```

### 5. 订阅语言切换事件

```csharp
public class MyUIController : MonoBehaviour
{
    void Start()
    {
        // 订阅语言切换事件
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }
    
    void OnDestroy()
    {
        // 取消订阅
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }
    
    private void OnLanguageChanged()
    {
        // 语言切换时的自定义处理
        Debug.Log("语言已切换");
    }
}
```

## 语言文件格式

语言文件使用JSON格式，结构如下：

```json
{
  "texts": [
    {
      "key": "ui.settings.title",
      "value": "设置"
    },
    {
      "key": "warehouse.button.transfer_to_storage",
      "value": "放入仓库 ({0})"
    }
  ]
}
```

## Key命名规范

建议使用以下命名规范：

- `ui.` - UI界面相关
  - `ui.settings.title` - 设置标题
  - `ui.button.close` - 关闭按钮
  - `ui.button.confirm` - 确认按钮

- `warehouse.` - 仓库系统相关
  - `warehouse.title` - 仓库标题
  - `warehouse.inventory.title` - 背包标题

- `tool.` - 工具相关
  - `tool.drill.name` - 钻探工具名称
  - `tool.hammer.name` - 地质锤名称

- `sample.` - 样本相关
  - `sample.collection.prompt` - 采集提示
  - `sample.description.simple_drill` - 简易钻探描述

## 开发工具

### 编辑器工具

使用 `Tools -> Localization -> 多语言工具` 可以：

- 查看系统状态
- 快速切换语言
- 为Text组件批量添加LocalizedText
- 扫描场景中的中文文本
- 管理语言文件

### 演示组件

添加 `LocalizationDemo` 组件可以：

- 实时查看不同key的本地化效果
- 测试语言切换功能
- 使用快捷键快速切换（Space键切换演示文本，L键切换语言）

## 故障排除

### 常见问题

1. **文本显示为 [key_name] 格式**
   - 检查语言文件是否正确放置在 `Assets/Resources/Localization/Data/` 目录
   - 确认JSON文件格式正确
   - 检查LocalizationManager是否正确初始化

2. **语言切换后文本不更新**
   - 确认Text组件已添加LocalizedText组件
   - 检查LocalizedText的TextKey是否正确设置
   - 确认已订阅OnLanguageChanged事件

3. **设置界面不显示**
   - 检查SettingsManager是否正确创建
   - 确认ESC键监听是否正常工作
   - 检查Canvas的sortingOrder设置

### 调试方法

```csharp
// 显示系统状态
Debug.Log(LocalizationManager.Instance.GetLanguageInfo());

// 显示所有已加载的文本键
string[] keys = LocalizationManager.Instance.GetAllTextKeys();
foreach (string key in keys)
{
    Debug.Log($"{key}: {LocalizationManager.Instance.GetText(key)}");
}
```

## 扩展

### 添加新语言

1. 在 `LanguageSettings.cs` 中添加新的语言枚举
2. 更新 `LanguageCodes` 和 `LanguageDisplayNames` 字典
3. 创建对应的JSON语言文件
4. 在SettingsManager中添加对应的UI按钮

### 支持更多文本组件

修改 `LocalizedText.cs`，在文本组件检测部分添加对新组件类型的支持。

### 自定义字体

在LocalizedText中添加字体切换功能，为不同语言指定不同的字体资源。

## 性能优化

- 语言文件在首次加载后缓存在内存中
- LocalizedText组件只在语言切换时更新文本
- 使用事件驱动机制避免轮询检查
- 支持按需加载语言文件

## 版本历史

- v1.8: 初始版本，支持中英日三语言
- 完整的UI设置界面
- Editor工具支持
- 演示和测试组件

---

*该多语言系统是Unity地质钻探教育系统v1.8版本的重要组成部分。*