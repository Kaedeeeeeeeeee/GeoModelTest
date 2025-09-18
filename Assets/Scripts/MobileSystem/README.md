# 移动端适配系统 (Mobile Adaptation System)

## 系统概述

为Unity地质勘探教育游戏开发的全面移动端适配系统，提供触控输入、响应式UI、手势识别和触觉反馈等功能。

## 核心组件

### 1. MobileInputManager.cs
**核心输入管理系统**
- 设备类型自动检测 (PC/Mobile/Hybrid)
- 统一的输入事件系统
- 触控和传统输入的无缝切换
- 单例模式，全局访问

```csharp
// 基本使用
MobileInputManager.Instance.OnMoveInput += HandleMovement;
MobileInputManager.Instance.OnLookInput += HandleCameraRotation;
```

### 2. MobileControlsUI.cs
**虚拟控制界面**
- 动态虚拟摇杆
- 触控友好的按钮布局
- 安全区域适配
- 响应式缩放

### 3. MobileUIAdapter.cs
**响应式UI适配器**
- 设备类型分类 (Phone/Tablet/Desktop)
- 屏幕尺寸响应式调整
- 性能等级优化
- 布局模式切换

### 4. TouchGestureHandler.cs
**手势识别系统**
- 地质勘探专用手势
- 多点触控支持
- 样本检查手势
- 工具操作手势

### 5. TouchFeedbackManager.cs
**触觉反馈系统**
- 跨平台震动支持
- 多种反馈类型
- 自定义震动模式
- 音频反馈集成

### 6. SafeAreaPanel.cs
**安全区域组件**
- 刘海屏适配
- 虚拟按键避让
- 实时屏幕变化适应

### 7. MobileLayoutAdapter.cs
**布局适配器**
- 屏幕方向响应
- 网格布局优化
- 响应式列数调整

## 系统集成

### FirstPersonController 适配
- 保持PC端完整功能
- 添加移动端输入支持
- 混合输入模式

### InventoryUISystem 移动端优化
- 底部工具栏替代轮盘
- 触控友好的工具选择
- 本地化支持

### InventoryUI 移动端适配
- 响应式网格布局
- 移动端按钮大小
- 安全区域适配

## 功能特性

### 输入系统
- **多模式支持**: Auto/Desktop/Mobile/Hybrid
- **事件驱动**: 统一的输入事件系统
- **设备检测**: 自动识别设备类型
- **输入优先级**: 移动端输入优先级管理

### UI适配
- **响应式设计**: 自动适配不同屏幕尺寸
- **安全区域**: 完整的安全区域支持
- **本地化**: 多语言界面支持
- **性能优化**: 根据设备性能调整

### 手势识别
- **基础手势**: 点击、长按、滑动
- **高级手势**: 缩放、旋转、多点触控
- **专业手势**: 样本检查、工具操作
- **上下文感知**: 根据游戏状态调整手势行为

### 触觉反馈
- **多平台支持**: Android/iOS原生震动
- **反馈类型**: 按钮点击、成功、错误、警告等
- **自定义模式**: 样本收集、成就获得等专用模式
- **音频集成**: 震动+音效的完整反馈

## 使用指南

### 1. 初始化系统
系统会自动初始化，无需手动配置。

### 2. 添加移动端支持到现有UI
```csharp
// 为UI组件添加移动端适配
public class YourUI : MonoBehaviour
{
    [Header("移动端适配")]
    public bool enableMobileAdaptation = true;
    
    void Start()
    {
        if (enableMobileAdaptation && Application.isMobilePlatform)
        {
            ApplyMobileAdaptation();
        }
    }
}
```

### 3. 使用手势识别
```csharp
TouchGestureHandler gestureHandler = FindFirstObjectByType<TouchGestureHandler>();
gestureHandler.OnSampleInspect += HandleSampleInspection;
gestureHandler.OnSwipe += HandleSwipeGesture;
```

### 4. 触发触觉反馈
```csharp
TouchFeedbackManager.Instance.TriggerFeedback(FeedbackType.Success);
TouchFeedbackManager.Instance.Vibrate(VibrationIntensity.Medium, 100);
```

## 配置参数

### 设备检测阈值
- 手机: 宽度 < 800px, 对角线 < 7英寸
- 平板: 800px ≤ 宽度 < 1200px, 7-12英寸
- 桌面: 宽度 ≥ 1200px

### 性能等级
- **低**: 简化效果，降低分辨率
- **中**: 标准效果和分辨率  
- **高**: 完整效果，原始分辨率

### 手势参数
- 点击最大时长: 0.3秒
- 滑动最小距离: 80像素
- 长按最小时长: 1.0秒
- 缩放最小距离: 20像素

## 调试功能

大部分组件都包含调试界面，可在编辑器中启用：
- `showDebugInfo = true` 显示调试信息
- `enableDebugLog = true` 启用详细日志
- OnGUI调试面板显示运行时状态

## 兼容性

- **Unity版本**: 2022.3+ LTS
- **输入系统**: Unity新输入系统 (Input System Package)
- **平台支持**: PC (Windows/Mac/Linux), Mobile (iOS/Android)
- **依赖包**: 
  - Unity Input System
  - Unity UI Package

## 开发规范

1. **命名约定**: 所有移动端相关类使用`Mobile`前缀
2. **单例模式**: 管理器类使用单例模式，确保全局访问
3. **事件驱动**: 优先使用事件系统而非直接调用
4. **性能优化**: 根据设备性能动态调整功能
5. **向后兼容**: 确保PC端功能不受影响

## 注意事项

1. **输入系统**: 需要安装Unity Input System包
2. **权限设置**: Android需要VIBRATE权限
3. **性能考虑**: 在低端设备上禁用复杂手势
4. **测试覆盖**: 在多种设备和分辨率下测试

---

**版本**: v1.0  
**最后更新**: 2025-01-13  
**状态**: 开发完成，编译测试通过