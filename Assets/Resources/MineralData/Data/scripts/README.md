# 矿物数据库脚本说明

本文件夹包含用于处理仙台地质数据的Python脚本。

## 主要脚本（重要）

### 数据库生成脚本
- **`generate_mineral_database.py`** - 主要的数据库生成脚本，将CSV转换为JSON格式
- **`add_fossils_to_database.py`** - 将化石数据添加到矿物数据库中

### 图片提取脚本
- **`extract_final_correct.py`** - 最终正确的图片提取脚本（20种唯一矿物）
- **`extract_all_images_properly.py`** - 按CSV行顺序提取图片的脚本
- **`apply_resource_sharing.py`** - 应用资源共享逻辑，消除重复图片

### 工具脚本
- **`generate_mapping_table.py`** - 生成矿物图片映射表格

## 分析和调试脚本（可选）

### 图片分析脚本
- `analyze_excel_image_mapping.py` - 分析Excel图片映射关系
- `analyze_missing_images.py` - 分析缺失的图片
- `check_all_file_formats.py` - 检查文件格式
- `check_excel_images_detailed.py` - 详细检查Excel图片

### 图片提取脚本（历史版本）
- `extract_excel_images.py` - 早期版本的图片提取脚本
- `extract_excel_images_simple.py` - 简化版图片提取脚本
- `extract_correct_by_csv_order.py` - 按CSV顺序映射的版本
- `extract_correct_mapping.py` - 正确映射逻辑的版本
- `extract_unique_minerals.py` - 唯一矿物提取版本

### 文件整理脚本
- `organize_duplicate_images.py` - 整理重复图片的脚本

## 使用说明

1. **生成完整数据库**：
   ```bash
   python3 generate_mineral_database.py
   python3 add_fossils_to_database.py
   ```

2. **提取矿物图片**：
   ```bash
   python3 extract_final_correct.py
   ```

3. **生成映射表**：
   ```bash
   python3 generate_mapping_table.py
   ```

## 文件依赖

- **输入文件**：
  - `../../MineralRelated/仙台地层岩石矿物分析-完整.xlsx`
  - `../../MineralRelated/仙台地层岩石矿物分析-完整-新.csv`
  - `../../MineralRelated/sendai_fossils_expanded.csv`

- **输出文件**：
  - `../SendaiMineralDatabase.json` - 主数据库文件
  - `../Images/Minerals/` - 矿物图片文件夹
  - `../../MineralRelated/矿物图片映射表.csv` - 映射表

## 最后更新

2025-01-27 - 完成化石数据集成和文件整理