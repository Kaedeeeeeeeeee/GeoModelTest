# Unity Geological Drilling System Project Memory

## Project Overview
Unity 3D geological drilling and sample collection system for educational geology exploration.

## Core Features

### 1. Drilling System
- BoringTool.cs: Main drilling tool (2m depth, 0.1m radius) + real-time preview
- **DrillTowerTool.cs**: Placeable drill tower for multi-depth sampling (0-10m in 2m increments)
- LayerGeometricCutter.cs: Geometric cutting with mesh boolean operations
- DrillingCylinderGenerator.cs: High-precision drilling cylinder generation

### 2. Tool Placement System
- PlaceableTool.cs: Abstract base class for placeable tools with preview
- DroneController.cs: Flying vehicle with 6-DOF movement and ground detection
- DrillCarController.cs: Ground vehicle with physics-based driving
- All tools support single-use placement restriction

### 3. UI & Inventory System
- InventoryUISystem.cs: Circular tool wheel with Tab key control
- Modified Tab workflow: Select tool → Auto-enter preview mode
- Visual feedback: Semi-transparent previews with validity colors
- Supports 8 tool slots with real-time mouse selection

#### TabUI排序规则
- **排序方式**: 按toolID数字从小到大排序
- **布局方向**: 顺时针方向排列（从12点位置开始）
- **Slot 0**: 12点位置（最小ID工具）
- **Slot 1**: 1:30位置 
- **Slot 2**: 3点位置
- **Slot 3**: 4:30位置
- **Slot 4**: 6点位置
- **Slot 5**: 7:30位置
- **Slot 6**: 9点位置
- **Slot 7**: 10:30位置（最大ID工具）

### 4. Sample Reconstruction
- GeometricSampleReconstructor.cs: Core reconstruction logic
- GeometricSampleFloating.cs: Floating display effects
- GeometricSampleComponents.cs: Sample interaction and info display

### 5. Geological Layers
- GeologyLayer.cs: Layer data structure with dip, strike, materials
- LayerDetectionSystem.cs: Layer detection and identification
- LayerSampleData.cs: Sample data structures

### 6. Scene Management System
- **SceneSwitcherTool.cs**: Scene switching tool (Tool ID: 999) with hand-held device
- **GameSceneManager.cs**: Multi-scene management with async loading and data persistence
- **PlayerPersistentData.cs**: Player state persistence across scene transitions
- **SceneSwitcherInitializer.cs**: System initialization and tool integration
- Supports MainScene (野外) and Laboratory Scene (研究室) switching

## Key Technical Solutions

### Z-fighting Fix
**Problem**: Layer overlap causing material flickering
**Solution**: 
- Implemented 0.5cm minimal safety gap (safeGap = 0.005f)
- Improved overlap detection algorithm
- Maintain actual layer thickness proportions

### Layer Depth Calculation
**Key Method**: CalculateLayerDepthRange() in GeometricSampleReconstructor.cs:427-483
- Fixed boundary layer detection errors
- Proper handling of cross-surface layers
- Maintain thin layer visibility (minimum 1cm thickness)

## File Structure
```
Assets/Scripts/
├── GeologySystem/
│   ├── GeometricCutting/
│   │   ├── GeometricSampleReconstructor.cs
│   │   ├── LayerGeometricCutter.cs
│   │   └── DrillingCylinderGenerator.cs
│   ├── GeologyLayer.cs
│   └── LayerDetectionSystem.cs
├── BoringTool.cs (with preview system)
├── **DrillTowerTool.cs (multi-depth drilling tower)**
├── **DrillTowerSetup.cs (tower initialization)**
├── **GameInitializer.cs (system initialization)**
├── PlaceableTool.cs (abstract base)
├── DroneController.cs (vehicle control + camera)
├── DrillCarController.cs (vehicle control + camera)
├── InventoryUISystem.cs (Tab wheel UI)
├── FirstPersonController.cs (mouse look control)
├── ToolManager.cs
├── SceneSystem/
│   ├── GameSceneManager.cs (multi-scene management)
│   ├── PlayerPersistentData.cs (data persistence)
│   ├── SceneSwitcherInitializer.cs (system initialization)
│   └── README.md (system documentation)
├── Tools/
│   └── SceneSwitcherTool.cs (scene switching tool)
└── Editor/
    ├── PrefabSetupTool.cs (component setup utility)
    └── SceneSwitcherSetupTool.cs (scene system setup)
```

## Core Parameters
- Drilling depth: 2.0m
- Drilling radius: 0.1m
- Minimum layer thickness: 0.01m (1cm)
- Safety gap: 0.005m (0.5cm)

### Tool IDs（按TabUI排序）
- SceneSwitcherTool: "999" (scene switching) → **Slot 0** (12点位置)
- SimpleDrillTool: "1000" (basic drilling) → **Slot 1** (1:30位置)
- DrillTowerTool: "1001" (multi-depth drilling) → **Slot 2** (3点位置)
- HammerTool: "1002" (geological sampling) → **Slot 3** (4:30位置)
- DroneTool: "1100" (flying vehicle) → **Slot 4** (6点位置)
- DrillCarTool: "1101" (ground vehicle) → **Slot 5** (7:30位置)

**说明**: 工具按ID从小到大排序，在TabUI中按顺时针方向排列，999作为最小ID排在12点位置（Slot 0）。

## Major Issues Resolved

### 1. Layer Overlap Problem
- Symptom: Material flickering at same position
- Root cause: Geometric overlap causing Z-fighting
- Solution: Minimal gap + precise overlap detection

### 2. Boundary Layer Detection Failure
- Symptom: Thin layers at drilling boundaries skipped
- Root cause: Incorrect skipping when layer top above drilling point
- Solution: Only skip when layer bottom above drilling point

### 3. Thickness Proportion Distortion
- Symptom: Thin layers too thick, thick layers improper
- Root cause: Minimum thickness threshold too large
- Solution: Reduce minimum to 1cm, maintain real proportions

### 4. Sample Material Not Updating (2025-07-08)
- Symptom: Drill samples retain old material appearance after layer materials updated
- Root cause: CreateLayerMaterial() was overriding layer material colors with layerColor
- Solution: Modified to preserve original material properties, added GetCurrentLayerMaterial() to fetch from MeshRenderer first

### 5. Precision Drilling System Improvements (2025-07-08)
- Enhancement: Implemented position-accurate geological sampling system
- Components: 
  - Precise layer detection using multi-level approach (bounds + raycast + mesh intersection)
  - Real material mapping system with sharedMaterial usage
  - Layer shape preservation algorithm for authentic cross-sections
  - Depth-based layer sorting for correct stratigraphic order
- Features: Maintains layer boundary shapes, accurate material colors, position-specific sampling

### 6. Drilling Point Validation System (2025-07-08)
- Problem: Samples showing incorrect materials (green samples from red surfaces)
- Root cause: Layer detection using distant layers instead of drilling point location
- Solutions:
  - Added surface layer detection priority in IsLayerInDrillingPath()
  - Implemented strict horizontal bounds checking (XZ plane validation)
  - Enhanced drilling location validation in BoringTool
  - Improved layer sorting with surface layer priority
- Result: Drilling now accurately reflects the material at the specific drilling location

### 7. Core Layer Detection Algorithm Fix (2025-07-08)
- Critical Issue: All layers incorrectly detected as "surface layers" with 0.00m depth
- Root Causes:
  - IsPointInLayer() too permissive, marking all layers as containing drilling point
  - GetLayerDepthFromStart() incorrect depth calculation algorithm
  - No distance-based pre-filtering of irrelevant layers
- Solutions:
  - Fixed IsPointInLayer() with strict bounds + raycast validation
  - Corrected depth calculation using Y-coordinate differences
  - Added PrefilterNearbyLayers() to only check relevant layers
  - Enhanced debugging output for better diagnosis
- Expected Result: Different locations now produce different layer combinations with correct depths

### 8. Intelligent Layer Thickness Distribution (2025-07-08)
- Critical Issue: Layer thickness severely distorted by forced minimum thickness (1.784m → 0.05m)
- Root Causes:
  - Simple overlap correction forcing 5cm minimum thickness
  - Additional 1cm minimum thickness in sample reconstruction
  - No consideration for real geological proportions
- Solutions:
  - Replaced overlap correction with DistributeLayersProportionally() algorithm
  - Calculates proportional scaling based on real vs drilling depth
  - Maintains relative thickness relationships between layers
  - Eliminated forced minimum thickness constraints
- Result: Layer thickness now accurately reflects real geological proportions in samples

### 9. Position-Specific Layer Detection Fix (2025-07-08)
- Critical Issue: Wrong thickness ratios (red 20cm showing as 50%, green >2m showing as 50%)
- Root Causes:
  - CalculateLayerDepthRange() not considering drilling location spatial distribution
  - All layers incorrectly included regardless of position
  - Simple Y-coordinate comparison ignoring horizontal position
- Solutions:
  - Added IsPointInLayerHorizontalBounds() for XZ-plane validation
  - Implemented CalculateRayLayerIntersections() for precise raycast detection
  - Modified distribution algorithm to use actual depth ranges instead of artificial scaling
  - Enhanced spatial filtering to only process layers at drilling location
- Expected Result: Red surface (20cm) → 20cm red sample, Green surface (>2m) → 100% green sample

## Recent Implementations (2025-07-01)

### Tool System Overhaul
**Achievement**: Complete tool placement and vehicle control system
- **PlaceableTool Base Class**: Abstract foundation for all placeable tools
- **Single-use Restriction**: Tools can only be placed once per selection
- **Real-time Preview**: Semi-transparent objects show placement position
- **Color-coded Feedback**: Green (valid) / Red (invalid) placement indicators

### Vehicle Control Systems
**DrillCarController**: Physics-based ground vehicle
- WASD movement with realistic physics (1000kg mass, low center of mass)
- Third-person camera following with real-time position updates
- F-key interaction system for entering/exiting vehicles
- Anti-flip constraints (frozen X/Z rotation)
- Player hide/show management during vehicle operation

**DroneController**: 6-DOF flying vehicle
- WASD horizontal movement + JK vertical movement
- Ground detection system restricting WASD when landed
- Physics-based flight (no gravity, high damping for control)
- Third-person camera system matching drill car
- Hover behavior when not controlled

### UI/UX Improvements
**Tab Tool Selection Workflow**:
- Modified from: Tab → Select → Click → Preview → Click → Place
- Changed to: Tab → Select → Release Tab → Auto-preview → Click → Place
- Eliminated extra click, streamlined user experience

**InventoryUISystem Enhancements**:
- Enlarged tool wheel (60% → 90% screen size)
- Darker background for better visibility
- Mouse look disable during selection (preserve keyboard movement)
- Auto-preview activation for PlaceableTool types

### Drilling Tool Preview System
**Real-time Position Preview**: 
- Semi-transparent cylinder showing exact drilling area (1m diameter, 2m depth)
- Green/red color coding for valid/invalid positions
- Follows mouse cursor in real-time when tool equipped
- Auto-hide when tool unequipped or switched

### Technical Infrastructure
**FirstPersonController Enhancement**:
- Added `enableMouseLook` property for granular control
- Allows keyboard movement while disabling mouse camera during UI

**Editor Tools**:
- PrefabSetupTool.cs: Automated component setup for vehicles
- Configures Rigidbody, Colliders, and Controller scripts
- Capsule Collider setup for stable placement physics

## Git Repository
- Remote: https://github.com/Kaedeeeeeeeeee/GeoModelTest.git
- Branch: main
- Status: Full tool system implemented, all preview systems working

## Development Notes
- Unity 2022.3+ LTS recommended
- Requires System.Linq reference (added)
- TENKOKU sky system removed (user request)
- Input System package required for vehicle controls
- Physics materials recommended for realistic vehicle behavior

## Current Tool Arsenal
1. **钻探工具 (BoringTool)**: Geological sampling with real-time preview
2. **钻塔工具 (DrillTowerTool)**: Multi-depth drilling tower (0-10m, 5 layers)
3. **无人机 (Drone)**: Flying vehicle with ground detection
4. **钻探车 (DrillCar)**: Ground vehicle with physics simulation

All tools feature:
- Real-time position previews
- Single-use placement restriction
- Consistent UI/UX workflow
- F-key vehicle interaction
- Third-person camera systems

### New DrillTower Features (2025-07-08)
- **Multi-depth sampling**: 5 drilling cycles (0-2m, 2-4m, 4-6m, 6-8m, 8-10m)
- **Circular sample arrangement**: Samples automatically arranged in circle around tower
- **Interactive drilling**: Click tower to perform next drilling cycle
- **Depth labeling**: Each sample shows drilling sequence and depth range
- **Tower status indicators**: Visual feedback for active/inactive states
- **Global ray detection integration**: Uses v1.1 geological detection system

## Recent Implementations (2025-07-08)

### DrillTower System
**Achievement**: Complete multi-depth drilling and sample management system
- **DrillTowerTool Class**: Placeable tool with 5-layer drilling capability
- **DrillTower Component**: State management for drilling cycles and sample tracking
- **DepthSampleMarker**: Component for depth identification and labeling
- **Circular Sample Layout**: Automatic positioning around tower with collision avoidance

### Integration Features
**Seamless System Integration**:
- Uses existing GeometricSampleReconstructor for real sample generation
- Compatible with v1.1 global ray detection algorithm
- Integrates with InventoryUISystem for tool selection
- Automatic tool initialization via GameInitializer

### Sample Management
**Advanced Sample Organization**:
- Environmental sample positioning (ground height detection)
- Dynamic radius adjustment based on sample count
- Depth-based height variation for visual clarity
- Player-facing text labels with rotation tracking

## Recent Implementations (2025-07-15)

### Scene Management System
**Achievement**: Complete multi-scene switching and data persistence system
- **SceneSwitcherTool**: Special tool (ID: 999) for scene switching with hand-held device
- **Scene Selection UI**: Modal UI with current scene grayed out, click to switch
- **Async Scene Loading**: Smooth scene transitions with loading feedback
- **Data Persistence**: Player position, equipped tools, and sample data preserved across scenes

### Scene System Features
**Seamless Scene Transitions**:
- Two-scene support: MainScene (野外) and Laboratory Scene (研究室)
- Real-time scene selection UI with visual feedback
- Automatic player state restoration after scene switching
- Tool integration with existing Tab wheel system

### Technical Implementation
**Multi-Scene Architecture**:
- Singleton GameSceneManager for global scene control
- PlayerPersistentData for cross-scene data management
- SceneSwitcherInitializer for automatic system setup
- Editor tools for easy system configuration

### Tool Arsenal Update
**Complete Tool Collection**:
1. **钻探工具 (BoringTool)**: Geological sampling with real-time preview
2. **钻塔工具 (DrillTowerTool)**: Multi-depth drilling tower (0-10m, 5 layers)
3. **无人机 (Drone)**: Flying vehicle with ground detection
4. **钻探车 (DrillCar)**: Ground vehicle with physics simulation
5. **地质锤 (HammerTool)**: Thin section sampling tool
6. **场景切换器 (SceneSwitcherTool)**: Multi-scene navigation tool

## Recent Implementations (2025-07-18)

### Warehouse Multi-Selection System
**Achievement**: Complete visual feedback and batch transfer system
- **MultiSelectSystem Core**: State machine with Ready/BackpackSelection/WarehouseSelection modes
- **Visual Feedback**: Green checkmark icons appear on selected items (top-right corner)
- **Smart Mode Switching**: First selected item determines selection type (backpack or warehouse)
- **Batch Transfer UI**: Dynamic button showing "放入仓库/背包 (N)" with item count

### Multi-Selection Workflow
**User Experience Flow**:
1. F键进入仓库 → 点击"多选"按钮 → 进入Ready模式
2. 点击物品 → 自动切换到BackpackSelection/WarehouseSelection模式
3. 选择更多物品 → 绿色勾选标记出现，只能选择同位置物品
4. 批量传输 → 点击"放入仓库/背包"按钮完成操作
5. 自动退出多选模式，界面刷新

### Technical Implementation
**Key Components**:
- **NotifyItemSelectionChanged()**: Global notification system for visual updates
- **CreateSelectionMark()**: Dynamic checkmark creation with 20x20 pixel custom sprite
- **UpdateButtonStates()**: Smart batch transfer button management
- **Intelligent State Management**: Prevents accidental mode exit, maintains user intent

### Visual Feedback System
**Selection Indicators**:
- Green checkmark sprite (right-top corner positioning)
- Background color changes for selected slots
- Batch transfer button text updates with item count
- Cross-panel synchronization (both backpack and warehouse panels)

### Error Prevention & UX
**Smart Restrictions**:
- Cross-location selection prevention (can't mix backpack + warehouse items)
- Capacity validation before batch operations
- Transaction rollback on partial failures
- Mode persistence until user explicitly exits

### Debug Tools
**Editor Integration**:
- **Tools → 仓库系统测试**: Complete testing toolkit
- **Context Menus**: Component-level status checking
- **Console Commands**: Real-time system state monitoring
- **Visual Feedback Testing**: Simulate selection states

---
Last updated: 2025-07-18
Project status: Complete warehouse system with multi-selection visual feedback and batch transfer functionality