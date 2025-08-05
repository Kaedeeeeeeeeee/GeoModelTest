#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
最终正确的图片提取脚本
20种唯一矿物对应20张图片，按首次出现顺序映射
"""

import zipfile
import os
import csv
from collections import OrderedDict

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
        "伊利石 (蚀变产物)": "illite_alteration",
        "火山灰": "volcanic_ash",
        "浮石": "pumice",
        "重矿物": "heavy_minerals",
        "碳质物": "carbonaceous_material"
    }
    
    clean_name = mineral_name.strip()
    return name_map.get(clean_name, clean_name.lower().replace(" ", "_"))

def get_unique_minerals_in_order(csv_file):
    """获取CSV中唯一矿物，按首次出现顺序"""
    unique_minerals = OrderedDict()
    
    with open(csv_file, 'r', encoding='utf-8') as file:
        reader = csv.reader(file)
        headers = next(reader)
        
        for row_num, row in enumerate(reader, start=2):
            if len(row) >= 3:
                mineral_name = row[2].strip()
                
                if mineral_name and mineral_name not in unique_minerals:
                    unique_minerals[mineral_name] = {
                        'first_row': row_num,
                        'mineral_id': generate_mineral_id(mineral_name)
                    }
    
    return unique_minerals

def extract_final_correct_mapping(excel_file, csv_file, output_dir):
    """按唯一矿物首次出现顺序提取图片"""
    
    print("=" * 80)
    print("最终正确的Excel图片提取工具")
    print("20种唯一矿物 ↔ 20张图片")
    print("=" * 80)
    print(f"处理文件: {excel_file}")
    print(f"参考CSV: {csv_file}")
    print(f"输出目录: {output_dir}")
    
    # 获取唯一矿物（按首次出现顺序）
    unique_minerals = get_unique_minerals_in_order(csv_file)
    print(f"\\n发现唯一矿物种类: {len(unique_minerals)} 种")
    
    # 显示唯一矿物顺序
    print("\\n唯一矿物按首次出现顺序:")
    mineral_list = list(unique_minerals.items())
    for i, (mineral_name, info) in enumerate(mineral_list):
        print(f"{i+1:2d}. {mineral_name:20} -> {info['mineral_id']:20} (首次出现: 行{info['first_row']})")
    
    os.makedirs(output_dir, exist_ok=True)
    
    # 提取Excel中的图片
    image_files = []
    try:
        with zipfile.ZipFile(excel_file, 'r') as zip_file:
            for file_info in zip_file.filelist:
                if file_info.filename.startswith('xl/media/image') and file_info.filename.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp')):
                    image_files.append(file_info.filename)
            
            # 按文件名中的数字排序
            image_files.sort(key=lambda x: int(''.join(filter(str.isdigit, x))))
    except Exception as e:
        print(f"读取Excel文件时出错: {e}")
        return
    
    print(f"\\nExcel中图片文件 ({len(image_files)} 个):")
    for i, img_file in enumerate(image_files, 1):
        print(f"{i:2d}. {img_file}")
    
    # 检查数量是否匹配
    if len(image_files) != len(unique_minerals):
        print(f"\\n⚠️  警告: 图片数量({len(image_files)})与唯一矿物数量({len(unique_minerals)})不匹配")
        mapping_count = min(len(image_files), len(unique_minerals))
        print(f"将处理前 {mapping_count} 个映射")
    else:
        print(f"\\n✅ 图片数量与唯一矿物数量完美匹配!")
        mapping_count = len(image_files)
    
    # 一对一映射
    print("\\n=== 图片与唯一矿物映射 ===")
    
    try:
        with zipfile.ZipFile(excel_file, 'r') as zip_file:
            for i in range(mapping_count):
                mineral_name, mineral_info = mineral_list[i]
                image_file = image_files[i]
                mineral_id = mineral_info['mineral_id']
                
                # 获取文件扩展名
                _, ext = os.path.splitext(image_file)
                new_filename = f"{mineral_id}_001{ext.lower()}"
                
                # 提取并重命名
                image_data = zip_file.read(image_file)
                output_path = os.path.join(output_dir, new_filename)
                
                with open(output_path, 'wb') as f:
                    f.write(image_data)
                
                print(f"{i+1:2d}. {image_file:20} -> {mineral_name:20} -> {new_filename}")
    
    except Exception as e:
        print(f"提取图片时出错: {e}")
        return
    
    print(f"\\n提取完成: {mapping_count} 个图片")
    
    # 最终统计
    extracted_files = [f for f in os.listdir(output_dir) if f.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp'))]
    
    print(f"\\n=== 最终统计 ===")
    print(f"唯一矿物种类: {len(unique_minerals)}")
    print(f"Excel图片数量: {len(image_files)}")
    print(f"成功提取图片: {len(extracted_files)}")
    
    print(f"\\n最终图片文件 ({len(extracted_files)} 个):")
    for file in sorted(extracted_files):
        print(f"  - {file}")
    
    print("\\n" + "=" * 80)
    print("✅ 处理完成!")
    print("每种唯一矿物现在对应一张正确的图片")
    print("=" * 80)

def main():
    """主函数"""
    excel_path = "../../MineralRelated/仙台地层岩石矿物分析-完整.xlsx"
    csv_path = "../../MineralRelated/仙台地层岩石矿物分析-完整-新.csv"
    output_path = "../Images/Minerals"
    
    if not os.path.exists(excel_path):
        print(f"Excel文件不存在: {excel_path}")
        return
        
    if not os.path.exists(csv_path):
        print(f"CSV文件不存在: {csv_path}")
        return
    
    extract_final_correct_mapping(excel_path, csv_path, output_path)

if __name__ == "__main__":
    main()