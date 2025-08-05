#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Excel图片提取和重命名脚本
从Excel文件中提取矿物图片并按照数据库命名规则重命名
"""

import os
import sys
import pandas as pd
from PIL import Image
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path
import shutil

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
    
    # 移除括号内容和特殊字符，生成标准ID
    clean_name = mineral_name.strip()
    if clean_name in name_map:
        return name_map[clean_name]
    
    # 如果没有映射，生成标准英文ID
    english_id = clean_name.lower()
    # 移除常见的中文括号内容
    english_id = english_id.replace("（", "_").replace("）", "").replace("(", "_").replace(")", "")
    english_id = english_id.replace(" ", "_").replace("-", "_").replace("、", "_")
    # 移除多余的下划线
    english_id = "_".join([part for part in english_id.split("_") if part])
    
    return english_id

def extract_images_from_xlsx(excel_file_path, output_dir):
    """
    从Excel文件中提取图片
    
    Args:
        excel_file_path: Excel文件路径
        output_dir: 输出目录
    """
    
    # 确保输出目录存在
    os.makedirs(output_dir, exist_ok=True)
    
    print(f"正在处理文件: {excel_file_path}")
    
    # 读取Excel数据
    try:
        df = pd.read_excel(excel_file_path)
        print(f"Excel数据读取成功，共 {len(df)} 行")
        print("列名:", df.columns.tolist())
    except Exception as e:
        print(f"读取Excel文件失败: {e}")
        return False
    
    # 检查是否有矿物名称列（第3列，索引为2）
    if len(df.columns) < 3:
        print("Excel文件列数不足，无法找到矿物名称列")
        return False
    
    mineral_column = df.columns[2]  # 第3列是矿物名称
    print(f"矿物名称列: {mineral_column}")
    
    # Excel文件实际上是一个ZIP文件，可以提取其中的图片
    try:
        # 复制Excel文件为ZIP文件
        temp_zip = excel_file_path + '.zip'
        shutil.copy2(excel_file_path, temp_zip)
        
        # 提取图片
        with zipfile.ZipFile(temp_zip, 'r') as zip_ref:
            # 查找图片文件
            image_files = []
            for file_info in zip_ref.filelist:
                if file_info.filename.startswith('xl/media/') and any(file_info.filename.lower().endswith(ext) for ext in ['.png', '.jpg', '.jpeg', '.gif', '.bmp']):
                    image_files.append(file_info.filename)
            
            print(f"在Excel中发现 {len(image_files)} 个图片文件")
            
            # 提取图片到临时目录
            temp_image_dir = os.path.join(output_dir, 'temp_images')
            os.makedirs(temp_image_dir, exist_ok=True)
            
            for image_file in image_files:
                zip_ref.extract(image_file, temp_image_dir)
                print(f"提取图片: {image_file}")
        
        # 清理临时ZIP文件
        os.remove(temp_zip)
        
        # 重命名图片
        rename_images_by_order(df, mineral_column, temp_image_dir, output_dir, image_files)
        
        # 清理临时目录
        shutil.rmtree(temp_image_dir)
        
        return True
        
    except Exception as e:
        print(f"提取图片失败: {e}")
        return False

def rename_images_by_order(df, mineral_column, temp_dir, output_dir, image_files):
    """
    按照Excel中的顺序重命名图片
    
    Args:
        df: Excel数据
        mineral_column: 矿物名称列
        temp_dir: 临时图片目录
        output_dir: 输出目录
        image_files: 图片文件列表
    """
    
    # 获取有效的矿物数据（去掉空值）
    valid_minerals = []
    for index, row in df.iterrows():
        mineral_name = str(row[mineral_column]).strip()
        if mineral_name and mineral_name != 'nan' and mineral_name != '':
            valid_minerals.append((index, mineral_name))
    
    print(f"发现 {len(valid_minerals)} 个有效矿物名称")
    print(f"发现 {len(image_files)} 个图片文件")
    
    # 按照顺序匹配图片和矿物
    renamed_count = 0
    
    for i, (row_index, mineral_name) in enumerate(valid_minerals):
        if i < len(image_files):
            # 生成矿物ID
            mineral_id = generate_mineral_id(mineral_name)
            
            # 原图片路径
            original_image = image_files[i]
            temp_image_path = os.path.join(temp_dir, original_image)
            
            # 获取文件扩展名
            _, ext = os.path.splitext(original_image)
            if not ext:
                ext = '.jpg'  # 默认扩展名
            
            # 新文件名
            new_filename = f"{mineral_id}_001{ext.lower()}"
            new_image_path = os.path.join(output_dir, new_filename)
            
            try:
                # 复制并重命名图片
                shutil.copy2(temp_image_path, new_image_path)
                print(f"重命名成功: {mineral_name} -> {new_filename}")
                renamed_count += 1
                
                # 验证图片是否有效
                try:
                    with Image.open(new_image_path) as img:
                        print(f"  图片验证成功: {img.size}, {img.format}")
                except Exception as e:
                    print(f"  警告: 图片可能损坏 {new_filename}: {e}")
                    
            except Exception as e:
                print(f"重命名失败: {mineral_name} -> {new_filename}: {e}")
        else:
            print(f"警告: 矿物 '{mineral_name}' 没有对应的图片")
    
    print(f"\n重命名完成! 成功处理 {renamed_count} 个图片文件")

def create_image_directory_structure():
    """创建图片目录结构"""
    base_dir = "../Images"
    dirs_to_create = [
        os.path.join(base_dir, "Minerals"),
        os.path.join(base_dir, "Rocks"), 
        os.path.join(base_dir, "Layers")
    ]
    
    for dir_path in dirs_to_create:
        os.makedirs(dir_path, exist_ok=True)
        print(f"创建目录: {dir_path}")

def main():
    """主函数"""
    print("=" * 60)
    print("Excel矿物图片提取和重命名工具")
    print("=" * 60)
    
    # Excel文件路径
    excel_file = "../../MineralRelated/仙台地层岩石矿物分析-完整.xlsx"
    
    # 检查Excel文件是否存在
    if not os.path.exists(excel_file):
        print(f"错误: Excel文件不存在 - {excel_file}")
        print("请确认文件路径是否正确")
        return False
    
    # 创建目录结构
    create_image_directory_structure()
    
    # 输出目录
    output_dir = "../Images/Minerals"
    
    # 提取和重命名图片
    success = extract_images_from_xlsx(excel_file, output_dir)
    
    if success:
        print("\n" + "=" * 60)
        print("图片提取完成!")
        print(f"图片保存位置: {os.path.abspath(output_dir)}")
        print("=" * 60)
        
        # 列出生成的文件
        if os.path.exists(output_dir):
            files = os.listdir(output_dir)
            image_files = [f for f in files if f.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp'))]
            print(f"\n生成的图片文件 ({len(image_files)} 个):")
            for file in sorted(image_files):
                print(f"  - {file}")
    else:
        print("\n图片提取失败!")
    
    return success

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n用户中断操作")
    except Exception as e:
        print(f"\n发生错误: {e}")
        import traceback
        traceback.print_exc()