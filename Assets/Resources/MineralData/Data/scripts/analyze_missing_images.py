#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
分析缺失图片的矿物
对比CSV中的所有矿物与实际提取的图片
"""

import csv
import os
from collections import Counter

def generate_mineral_id(mineral_name):
    """生成矿物ID"""
    name_map = {
        "石英": "quartz",
        "斜长石": "plagioclase", 
        "辉石": "pyroxene",
        "角闪石": "amphibole",
        "磁铁矿": "magnetite",
        "橄榄石": "olivine",
        "长石": "feldspar",
        "黑云母": "biotite",
        "锆石": "zircon",
        "火山玻璃": "volcanic_glass",
        "普通辉石": "augite",
        "紫苏辉石": "hypersthene",
        "石榴石": "garnet",
        "粘土矿物": "clay_minerals",
        "钛磁铁矿": "titanomagnetite",
        "斜方辉石": "orthopyroxene",
        "普通角闪石": "common_amphibole",
        "碳化植物遗体": "carbonized_plant_remains",
        "蒙脱石": "montmorillonite",
        "埃洛石": "halloysite",
        "高岭石": "kaolinite",
        "伊利石": "illite",
        "硅化木": "silicified_wood",
        "碳化植物遗体 (非矿物)": "carbonized_plant_remains",
        "黑云母 (高钾型中常见)": "biotite_high_k",
        "伊利石 (蚀变产物)": "illite_alteration",
        "部分为二氧化硅交代的硅化木": "silicified_wood_partial",
        "高温石英": "high_temp_quartz",
        "石英 (沉积)": "quartz_sedimentary",
        "火山灰": "volcanic_ash",
        "浮石": "pumice",
        "高温石英 (火山)": "high_temp_quartz_volcanic",
        "重矿物": "heavy_minerals",
        "细粒石英": "fine_grained_quartz",
        "碳质物": "carbonaceous_material"
    }
    
    clean_name = mineral_name.strip()
    if clean_name in name_map:
        return name_map[clean_name]
    
    english_id = clean_name.lower()
    english_id = english_id.replace("（", "_").replace("）", "").replace("(", "_").replace(")", "")
    english_id = english_id.replace(" ", "_").replace("-", "_").replace("、", "_")
    english_id = "_".join([part for part in english_id.split("_") if part])
    
    return english_id

def analyze_csv_minerals():
    """分析CSV中的所有矿物"""
    csv_file = "../../MineralRelated/仙台地层岩石矿物分析-完整.csv"
    
    if not os.path.exists(csv_file):
        print(f"CSV文件不存在: {csv_file}")
        return [], []
    
    all_minerals = []
    unique_minerals = set()
    
    with open(csv_file, 'r', encoding='utf-8') as file:
        reader = csv.reader(file)
        headers = next(reader)  # 跳过标题行
        
        print(f"CSV列名: {headers}")
        print(f"矿物名称列: {headers[2]}")
        
        for row_num, row in enumerate(reader, start=2):
            if len(row) >= 3:
                mineral_name = row[2].strip()
                if mineral_name and mineral_name != '':
                    mineral_id = generate_mineral_id(mineral_name)
                    all_minerals.append((row_num, mineral_name, mineral_id))
                    unique_minerals.add(mineral_id)
    
    return all_minerals, list(unique_minerals)

def analyze_extracted_images():
    """分析已提取的图片"""
    images_dir = "../Images/Minerals"
    
    if not os.path.exists(images_dir):
        print(f"图片目录不存在: {images_dir}")
        return []
    
    extracted_minerals = set()
    
    for file in os.listdir(images_dir):
        if file.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp')):
            # 提取矿物ID
            base_name = file.rsplit('.', 1)[0]
            if '_' in base_name:
                mineral_id = base_name.rsplit('_', 1)[0]
                extracted_minerals.add(mineral_id)
    
    return list(extracted_minerals)

def main():
    """主分析函数"""
    print("=" * 80)
    print("矿物图片缺失分析")
    print("=" * 80)
    
    # 分析CSV中的矿物
    all_minerals, unique_csv_minerals = analyze_csv_minerals()
    
    print(f"\nCSV分析结果:")
    print(f"  总矿物记录: {len(all_minerals)} 条")
    print(f"  独特矿物种类: {len(unique_csv_minerals)} 种")
    
    # 统计矿物出现次数
    mineral_counts = Counter([mineral_id for _, _, mineral_id in all_minerals])
    
    print(f"\n矿物出现频次统计 (前10个):")
    for mineral_id, count in mineral_counts.most_common(10):
        mineral_name = next(name for _, name, mid in all_minerals if mid == mineral_id)
        print(f"  {mineral_id:25} : {count:2d} 次 ({mineral_name})")
    
    # 分析已提取的图片
    extracted_minerals = analyze_extracted_images()
    
    print(f"\n图片提取结果:")
    print(f"  已提取图片的矿物: {len(extracted_minerals)} 种")
    
    # 找出缺失的矿物
    missing_minerals = set(unique_csv_minerals) - set(extracted_minerals)
    
    print(f"\n缺失图片的矿物 ({len(missing_minerals)} 种):")
    print("-" * 60)
    
    missing_list = []
    for mineral_id in sorted(missing_minerals):
        # 找到对应的中文名称
        mineral_name = next((name for _, name, mid in all_minerals if mid == mineral_id), mineral_id)
        count = mineral_counts[mineral_id]
        missing_list.append((mineral_id, mineral_name, count))
        print(f"  {mineral_id:25} : {mineral_name} ({count} 次出现)")
    
    # 分析原因
    print(f"\n分析结果:")
    print(f"  Excel中图片数量: 21 张")
    print(f"  CSV中矿物记录: {len(all_minerals)} 条") 
    print(f"  独特矿物种类: {len(unique_csv_minerals)} 种")
    print(f"  已提取图片矿物: {len(extracted_minerals)} 种")
    print(f"  缺失图片矿物: {len(missing_minerals)} 种")
    
    print(f"\n可能原因:")
    print(f"  1. Excel中某些矿物行没有插入图片")
    print(f"  2. 某些矿物共享相同的图片")
    print(f"  3. 图片提取脚本按顺序匹配，可能跳过了空图片位置")
    
    # 显示已有图片的矿物
    print(f"\n已有图片的矿物 ({len(extracted_minerals)} 种):")
    print("-" * 60)
    for mineral_id in sorted(extracted_minerals):
        mineral_name = next((name for _, name, mid in all_minerals if mid == mineral_id), mineral_id)
        count = mineral_counts[mineral_id]
        print(f"  {mineral_id:25} : {mineral_name} ({count} 次出现)")
    
    print("\n" + "=" * 80)

if __name__ == "__main__":
    main()