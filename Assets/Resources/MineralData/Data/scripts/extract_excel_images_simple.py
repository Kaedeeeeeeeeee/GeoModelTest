#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Excel图片提取脚本 - 简化版本
使用内置库从Excel(.xlsx)文件中提取图片并重命名
"""

import os
import sys
import zipfile
import shutil
import csv
from pathlib import Path

def generate_mineral_id(mineral_name):
    """生成矿物ID - 与数据库脚本保持一致"""
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
        # 新增的矿物映射
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
    
    # 如果没有映射，生成标准英文ID
    english_id = clean_name.lower()
    english_id = english_id.replace("（", "_").replace("）", "").replace("(", "_").replace(")", "")
    english_id = english_id.replace(" ", "_").replace("-", "_").replace("、", "_")
    english_id = "_".join([part for part in english_id.split("_") if part])
    
    return english_id

def read_csv_data(csv_file_path):
    """读取CSV文件中的矿物数据"""
    minerals = []
    
    try:
        with open(csv_file_path, 'r', encoding='utf-8') as file:
            reader = csv.reader(file)
            headers = next(reader)  # 跳过标题行
            print(f"CSV列名: {headers}")
            
            for row in reader:
                if len(row) >= 3:  # 确保有足够的列
                    mineral_name = row[2].strip()  # 第3列是矿物名称
                    if mineral_name and mineral_name != '':
                        minerals.append(mineral_name)
                        
        print(f"从CSV读取到 {len(minerals)} 个矿物名称")
        return minerals
        
    except Exception as e:
        print(f"读取CSV文件失败: {e}")
        return []

def extract_images_from_xlsx(excel_file_path, output_dir):
    """从Excel文件中提取图片"""
    
    os.makedirs(output_dir, exist_ok=True)
    print(f"正在处理Excel文件: {excel_file_path}")
    
    try:
        # 复制Excel文件为ZIP文件进行解压
        temp_zip = excel_file_path + '.temp.zip'
        shutil.copy2(excel_file_path, temp_zip)
        
        # 提取图片
        with zipfile.ZipFile(temp_zip, 'r') as zip_ref:
            # 查找媒体文件夹中的图片
            image_files = []
            for file_info in zip_ref.filelist:
                filename = file_info.filename
                if filename.startswith('xl/media/') and any(filename.lower().endswith(ext) for ext in ['.png', '.jpg', '.jpeg', '.gif', '.bmp']):
                    image_files.append(filename)
            
            print(f"在Excel中发现 {len(image_files)} 个图片文件:")
            for img in image_files:
                print(f"  - {img}")
            
            if not image_files:
                print("未在Excel中找到图片文件")
                return False
            
            # 创建临时目录提取图片
            temp_dir = os.path.join(output_dir, 'temp_extracted')
            os.makedirs(temp_dir, exist_ok=True)
            
            # 提取所有图片文件
            extracted_images = []
            for image_file in image_files:
                try:
                    zip_ref.extract(image_file, temp_dir)
                    extracted_path = os.path.join(temp_dir, image_file)
                    extracted_images.append(extracted_path)
                    print(f"提取成功: {image_file}")
                except Exception as e:
                    print(f"提取失败 {image_file}: {e}")
            
            # 清理临时ZIP文件
            os.remove(temp_zip)
            
            return extracted_images, temp_dir
            
    except Exception as e:
        print(f"处理Excel文件失败: {e}")
        return False

def rename_images_by_csv(csv_file_path, extracted_images, temp_dir, output_dir):
    """根据CSV数据重命名图片"""
    
    # 读取矿物名称
    minerals = read_csv_data(csv_file_path)
    
    if not minerals:
        print("未找到矿物数据")
        return False
    
    print(f"准备重命名 {len(extracted_images)} 个图片，对应 {len(minerals)} 个矿物")
    
    renamed_count = 0
    
    # 按照顺序匹配
    for i, extracted_path in enumerate(extracted_images):
        if i < len(minerals):
            mineral_name = minerals[i]
            mineral_id = generate_mineral_id(mineral_name)
            
            # 获取文件扩展名
            _, ext = os.path.splitext(extracted_path)
            if not ext:
                ext = '.jpg'
            
            # 新文件名
            new_filename = f"{mineral_id}_001{ext.lower()}"
            new_path = os.path.join(output_dir, new_filename)
            
            try:
                # 复制并重命名
                shutil.copy2(extracted_path, new_path)
                print(f"✓ {mineral_name} -> {new_filename}")
                renamed_count += 1
                
            except Exception as e:
                print(f"✗ 重命名失败 {mineral_name}: {e}")
        else:
            print(f"警告: 图片 {extracted_path} 没有对应的矿物名称")
    
    # 清理临时目录
    try:
        shutil.rmtree(temp_dir)
        print(f"清理临时目录: {temp_dir}")
    except Exception as e:
        print(f"清理临时目录失败: {e}")
    
    print(f"\n重命名完成! 成功处理 {renamed_count} 个图片")
    return renamed_count > 0

def create_directories():
    """创建必要的目录结构"""
    dirs = [
        "../Images/Minerals",
        "../Images/Rocks",
        "../Images/Layers"
    ]
    
    for dir_path in dirs:
        os.makedirs(dir_path, exist_ok=True)
        print(f"创建目录: {dir_path}")

def main():
    """主函数"""
    print("=" * 60)
    print("Excel矿物图片提取工具 (简化版)")
    print("=" * 60)
    
    # 文件路径
    excel_file = "../../MineralRelated/仙台地层岩石矿物分析-完整.xlsx"
    csv_file = "../../MineralRelated/仙台地层岩石矿物分析-完整.csv"
    output_dir = "../Images/Minerals"
    
    # 检查文件是否存在
    if not os.path.exists(excel_file):
        print(f"错误: Excel文件不存在 - {excel_file}")
        return False
    
    if not os.path.exists(csv_file):
        print(f"错误: CSV文件不存在 - {csv_file}")
        return False
    
    # 创建目录
    create_directories()
    
    # 提取图片
    result = extract_images_from_xlsx(excel_file, output_dir)
    
    if result:
        extracted_images, temp_dir = result
        
        # 重命名图片
        success = rename_images_by_csv(csv_file, extracted_images, temp_dir, output_dir)
        
        if success:
            print("\n" + "=" * 60)
            print("图片提取和重命名完成!")
            print(f"图片保存在: {os.path.abspath(output_dir)}")
            
            # 列出生成的文件
            files = [f for f in os.listdir(output_dir) if f.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp'))]
            print(f"\n生成的图片文件 ({len(files)} 个):")
            for file in sorted(files)[:10]:  # 只显示前10个
                print(f"  - {file}")
            if len(files) > 10:
                print(f"  ... 还有 {len(files) - 10} 个文件")
            print("=" * 60)
        else:
            print("图片重命名失败!")
    else:
        print("图片提取失败!")

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n用户中断操作")
    except Exception as e:
        print(f"\n发生错误: {e}")
        import traceback
        traceback.print_exc()