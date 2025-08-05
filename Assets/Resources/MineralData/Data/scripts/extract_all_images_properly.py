#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
改进的图片提取脚本
假设Excel中的21张图片按顺序对应CSV中前21个有矿物名称的行
"""

import zipfile
import os
import shutil
import csv

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
    return name_map.get(clean_name, clean_name.lower().replace(" ", "_"))

def read_all_minerals_from_csv(csv_file):
    """读取CSV中所有矿物数据，包括重复的"""
    minerals = []
    
    with open(csv_file, 'r', encoding='utf-8') as file:
        reader = csv.reader(file)
        headers = next(reader)
        
        print(f"CSV列: {headers}")
        
        for row_num, row in enumerate(reader, start=2):
            if len(row) >= 3:
                layer_name = row[0].strip()
                rock_type = row[1].strip()
                mineral_name = row[2].strip()
                
                # 所有有矿物名称的行都包含，不管是否重复
                if mineral_name:
                    minerals.append({
                        'row': row_num,
                        'layer': layer_name,
                        'rock': rock_type,
                        'mineral': mineral_name,
                        'mineral_id': generate_mineral_id(mineral_name)
                    })
    
    return minerals

def extract_and_rename_all_images(excel_file, csv_file, output_dir):
    """提取所有图片并根据CSV顺序重命名"""
    
    print(f"处理文件: {excel_file}")
    print(f"参考CSV: {csv_file}")
    print(f"输出目录: {output_dir}")
    
    # 读取所有矿物数据
    all_minerals = read_all_minerals_from_csv(csv_file)
    print(f"\nCSV中总共 {len(all_minerals)} 个矿物记录")
    
    # 显示前21个矿物
    print(f"\n前21个矿物记录:")
    for i, mineral in enumerate(all_minerals[:21]):
        print(f"{i+1:2d}. 行{mineral['row']:2d}: {mineral['mineral']} -> {mineral['mineral_id']}")
    
    os.makedirs(output_dir, exist_ok=True)
    
    # 提取Excel中的图片
    temp_zip = excel_file + '.temp.zip'
    shutil.copy2(excel_file, temp_zip)
    
    try:
        with zipfile.ZipFile(temp_zip, 'r') as zip_ref:
            # 获取所有媒体文件，按名称排序确保顺序
            media_files = []
            for file_info in zip_ref.filelist:
                if file_info.filename.startswith('xl/media/image'):
                    media_files.append(file_info.filename)
            
            # 按图片编号排序
            media_files.sort(key=lambda x: int(x.split('image')[1].split('.')[0]))
            
            print(f"\nExcel中图片文件 ({len(media_files)} 个):")
            for i, media_file in enumerate(media_files):
                print(f"{i+1:2d}. {media_file}")
            
            # 创建临时目录
            temp_dir = os.path.join(output_dir, 'temp')
            os.makedirs(temp_dir, exist_ok=True)
            
            # 提取所有图片
            extracted_files = []
            for media_file in media_files:
                zip_ref.extract(media_file, temp_dir)
                extracted_path = os.path.join(temp_dir, media_file)
                extracted_files.append(extracted_path)
            
            # 重命名图片
            print(f"\n=== 重命名图片 ===")
            renamed_count = 0
            mineral_counts = {}  # 记录每种矿物的计数
            
            for i, extracted_path in enumerate(extracted_files):
                if i < len(all_minerals):
                    mineral_data = all_minerals[i]
                    mineral_id = mineral_data['mineral_id']
                    
                    # 计算这种矿物是第几次出现
                    if mineral_id not in mineral_counts:
                        mineral_counts[mineral_id] = 0
                    mineral_counts[mineral_id] += 1
                    
                    # 生成文件名
                    _, ext = os.path.splitext(extracted_path)
                    new_filename = f"{mineral_id}_{mineral_counts[mineral_id]:03d}{ext.lower()}"
                    new_path = os.path.join(output_dir, new_filename)
                    
                    try:
                        shutil.copy2(extracted_path, new_path)
                        print(f"{i+1:2d}. {mineral_data['mineral']:25} -> {new_filename}")
                        renamed_count += 1
                    except Exception as e:
                        print(f"重命名失败: {e}")
                else:
                    print(f"警告: 图片 {i+1} 没有对应的矿物数据")
            
            # 清理临时目录
            shutil.rmtree(temp_dir)
            
            print(f"\n重命名完成: {renamed_count} 个图片")
            
            # 显示矿物统计
            print(f"\n=== 矿物图片统计 ===")
            for mineral_id in sorted(mineral_counts.keys()):
                count = mineral_counts[mineral_id]
                # 找到对应的中文名
                mineral_name = next((m['mineral'] for m in all_minerals if m['mineral_id'] == mineral_id), mineral_id)
                print(f"{mineral_id:25} : {count} 张 ({mineral_name})")
            
    except Exception as e:
        print(f"处理失败: {e}")
    finally:
        if os.path.exists(temp_zip):
            os.remove(temp_zip)

def check_remaining_minerals(csv_file, extracted_count):
    """检查剩余没有图片的矿物"""
    all_minerals = read_all_minerals_from_csv(csv_file)
    
    print(f"\n=== 没有图片的矿物 ===")
    remaining_minerals = all_minerals[extracted_count:]
    
    if remaining_minerals:
        print(f"剩余 {len(remaining_minerals)} 个矿物没有图片:")
        unique_remaining = set()
        for mineral in remaining_minerals:
            unique_remaining.add((mineral['mineral'], mineral['mineral_id']))
        
        for mineral_name, mineral_id in sorted(unique_remaining):
            print(f"  - {mineral_name} ({mineral_id})")
    else:
        print("所有矿物都有对应的图片!")

def main():
    """主函数"""
    print("=" * 80)
    print("改进的Excel图片提取工具")
    print("=" * 80)
    
    excel_file = "../../MineralRelated/仙台地层岩石矿物分析-完整.xlsx"
    csv_file = "../../MineralRelated/仙台地层岩石矿物分析-完整-新.csv"
    output_dir = "../Images/Minerals_Complete"
    
    if not os.path.exists(excel_file):
        print(f"Excel文件不存在: {excel_file}")
        return
        
    if not os.path.exists(csv_file):
        print(f"CSV文件不存在: {csv_file}")
        return
    
    # 提取和重命名所有图片
    extract_and_rename_all_images(excel_file, csv_file, output_dir)
    
    # 检查剩余矿物
    check_remaining_minerals(csv_file, 21)  # Excel中有21张图片
    
    print(f"\n" + "=" * 80)
    print(f"处理完成!")
    print(f"图片保存在: {output_dir}")
    print("=" * 80)

if __name__ == "__main__":
    main()