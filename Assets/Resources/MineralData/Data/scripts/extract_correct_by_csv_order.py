#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
正确按CSV行顺序映射的图片提取脚本
image1 -> 行2矿物, image2 -> 行3矿物, 以此类推
然后应用资源共享得到20种唯一矿物
"""

import zipfile
import os
import csv
from collections import defaultdict
import shutil

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

def get_all_mineral_rows(csv_file):
    """获取CSV中所有有矿物名称的行，保持顺序"""
    mineral_rows = []
    
    with open(csv_file, 'r', encoding='utf-8') as file:
        reader = csv.reader(file)
        headers = next(reader)
        
        for row_num, row in enumerate(reader, start=2):
            if len(row) >= 3:
                mineral_name = row[2].strip()
                
                if mineral_name:
                    mineral_rows.append({
                        'row_num': row_num,
                        'mineral_name': mineral_name,
                        'mineral_id': generate_mineral_id(mineral_name)
                    })
    
    return mineral_rows

def extract_by_csv_order(excel_file, csv_file, temp_output_dir):
    """按CSV行顺序提取图片到临时目录"""
    
    print("=" * 80)
    print("按CSV行顺序映射的图片提取工具")
    print("=" * 80)
    print(f"处理文件: {excel_file}")
    print(f"参考CSV: {csv_file}")
    print(f"临时输出目录: {temp_output_dir}")
    
    # 获取所有矿物行
    mineral_rows = get_all_mineral_rows(csv_file)
    print(f"\\nCSV中矿物记录总数: {len(mineral_rows)}")
    
    os.makedirs(temp_output_dir, exist_ok=True)
    
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
    
    # 按CSV行顺序映射
    print("\\n=== 按CSV行顺序映射图片 ===")
    mapping_count = min(len(image_files), len(mineral_rows))
    
    try:
        with zipfile.ZipFile(excel_file, 'r') as zip_file:
            for i in range(mapping_count):
                mineral = mineral_rows[i]
                image_file = image_files[i]
                
                # 获取文件扩展名
                _, ext = os.path.splitext(image_file)
                
                # 为每个矿物生成序号（处理重复矿物）
                existing_files = [f for f in os.listdir(temp_output_dir) if f.startswith(mineral['mineral_id'] + '_')]
                count = len(existing_files) + 1
                new_filename = f"{mineral['mineral_id']}_{count:03d}{ext.lower()}"
                
                # 提取并重命名
                image_data = zip_file.read(image_file)
                output_path = os.path.join(temp_output_dir, new_filename)
                
                with open(output_path, 'wb') as f:
                    f.write(image_data)
                
                print(f"{i+1:2d}. {image_file:20} -> 行{mineral['row_num']:2d} {mineral['mineral_name']:15} -> {new_filename}")
    
    except Exception as e:
        print(f"提取图片时出错: {e}")
        return
    
    print(f"\\n按CSV顺序提取完成: {mapping_count} 个图片")
    return mapping_count

def apply_resource_sharing(temp_dir, final_dir):
    """应用资源共享，每种矿物只保留一张最佳图片"""
    
    print(f"\\n=== 应用资源共享逻辑 ===")
    print(f"从临时目录: {temp_dir}")
    print(f"到最终目录: {final_dir}")
    
    os.makedirs(final_dir, exist_ok=True)
    
    # 按矿物ID分组
    mineral_groups = defaultdict(list)
    for file in os.listdir(temp_dir):
        if file.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp')):
            if '_' in file:
                base_name = file.rsplit('.', 1)[0]  # 移除扩展名
                if '_' in base_name:
                    mineral_id = base_name.rsplit('_', 1)[0]  # 移除数字部分
                    mineral_groups[mineral_id].append(file)
    
    print(f"发现 {len(mineral_groups)} 种不同矿物")
    
    # 为每种矿物选择最佳图片（优先选择_001）
    for mineral_id, files in mineral_groups.items():
        print(f"\\n处理矿物: {mineral_id} ({len(files)} 个文件)")
        
        # 优先选择 _001 版本
        best_file = None
        for file in files:
            if '_001.' in file:
                best_file = file
                print(f"  选择 _001 版本: {file}")
                break
        
        # 如果没有_001，选择第一个
        if not best_file:
            best_file = sorted(files)[0]
            print(f"  选择第一个文件: {best_file}")
        
        # 生成最终文件名
        _, ext = os.path.splitext(best_file)
        final_filename = f"{mineral_id}_001{ext.lower()}"
        
        # 复制到最终目录
        source_path = os.path.join(temp_dir, best_file)
        target_path = os.path.join(final_dir, final_filename)
        
        try:
            shutil.copy2(source_path, target_path)
            print(f"  最终文件: {final_filename}")
        except Exception as e:
            print(f"  复制失败: {e}")
    
    # 统计最终结果
    final_files = [f for f in os.listdir(final_dir) if f.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp'))]
    
    print(f"\\n=== 最终统计 ===")
    print(f"唯一矿物种类: {len(mineral_groups)}")
    print(f"最终图片文件: {len(final_files)}")
    
    print(f"\\n最终图片文件:")
    for file in sorted(final_files):
        print(f"  - {file}")

def main():
    """主函数"""
    excel_path = "../../MineralRelated/仙台地层岩石矿物分析-完整.xlsx"
    csv_path = "../../MineralRelated/仙台地层岩石矿物分析-完整-新.csv"
    temp_output_path = "../Images/Minerals_Complete"
    final_output_path = "../Images/Minerals"
    
    if not os.path.exists(excel_path):
        print(f"Excel文件不存在: {excel_path}")
        return
        
    if not os.path.exists(csv_path):
        print(f"CSV文件不存在: {csv_path}")
        return
    
    # 第一步：按CSV行顺序提取所有图片
    mapping_count = extract_by_csv_order(excel_path, csv_path, temp_output_path)
    
    if mapping_count and mapping_count > 0:
        # 第二步：应用资源共享
        apply_resource_sharing(temp_output_path, final_output_path)
        
        print("\\n" + "=" * 80)
        print("✅ 处理完成!")
        print("图片现在按正确的CSV行顺序映射，并应用了资源共享")
        print("=" * 80)

if __name__ == "__main__":
    main()