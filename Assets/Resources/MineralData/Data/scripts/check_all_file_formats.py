#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
检查Excel中所有文件格式
不限制文件类型，查看是否有遗漏的图片文件
"""

import zipfile
import os
import shutil
from collections import defaultdict

def analyze_all_files_in_excel(excel_file):
    """分析Excel中的所有文件，不限制格式"""
    print(f"分析Excel文件中的所有内容: {excel_file}")
    
    temp_zip = excel_file + '.temp.zip'
    shutil.copy2(excel_file, temp_zip)
    
    try:
        with zipfile.ZipFile(temp_zip, 'r') as zip_ref:
            all_files = zip_ref.namelist()
            
            print(f"\n=== Excel文件完整内容 ({len(all_files)} 个文件) ===")
            
            # 按目录分组
            file_groups = defaultdict(list)
            
            for file_path in all_files:
                if '/' in file_path:
                    directory = '/'.join(file_path.split('/')[:-1])
                    filename = file_path.split('/')[-1]
                else:
                    directory = 'root'
                    filename = file_path
                
                file_groups[directory].append((file_path, filename))
            
            # 显示所有目录和文件
            for directory in sorted(file_groups.keys()):
                files = file_groups[directory]
                print(f"\n目录: {directory}/ ({len(files)} 个文件)")
                
                for full_path, filename in sorted(files):
                    if filename:  # 排除目录本身
                        file_info = zip_ref.getinfo(full_path)
                        size_kb = file_info.file_size / 1024
                        
                        # 获取文件扩展名
                        ext = filename.split('.')[-1].lower() if '.' in filename else 'no_ext'
                        
                        print(f"  {filename:25} ({size_kb:8.1f} KB) .{ext}")
            
            # 统计文件扩展名
            print(f"\n=== 文件扩展名统计 ===")
            ext_counts = defaultdict(list)
            
            for file_path in all_files:
                filename = os.path.basename(file_path)
                if filename:
                    if '.' in filename:
                        ext = filename.split('.')[-1].lower()
                    else:
                        ext = 'no_extension'
                    
                    ext_counts[ext].append(file_path)
            
            for ext in sorted(ext_counts.keys()):
                files = ext_counts[ext]
                print(f".{ext:10} : {len(files):2d} 个文件")
                
                # 如果是可能的图片格式，显示文件列表
                if ext in ['jpeg', 'jpg', 'png', 'gif', 'bmp', 'tiff', 'tif', 'webp', 'svg', 'ico', 'emf', 'wmf']:
                    for file_path in files:
                        file_info = zip_ref.getinfo(file_path)
                        size_kb = file_info.file_size / 1024
                        print(f"    {file_path:35} ({size_kb:6.1f} KB)")
            
            # 检查可能被忽略的图片文件
            print(f"\n=== 可能的图片文件检查 ===")
            
            possible_image_files = []
            
            for file_path in all_files:
                filename = os.path.basename(file_path)
                if filename:
                    # 检查文件大小（图片通常比较大）
                    file_info = zip_ref.getinfo(file_path)
                    size_kb = file_info.file_size / 1024
                    
                    # 可能的图片文件条件：
                    # 1. 在media目录
                    # 2. 文件名包含image
                    # 3. 文件大小 > 5KB
                    # 4. 常见图片扩展名
                    
                    is_possible_image = False
                    reasons = []
                    
                    if 'media' in file_path:
                        is_possible_image = True
                        reasons.append("在media目录")
                    
                    if 'image' in filename.lower():
                        is_possible_image = True
                        reasons.append("文件名包含image")
                    
                    if size_kb > 5:
                        ext = filename.split('.')[-1].lower() if '.' in filename else ''
                        if ext in ['jpeg', 'jpg', 'png', 'gif', 'bmp', 'tiff', 'tif', 'webp', 'svg', 'ico', 'emf', 'wmf', 'eps', 'pdf']:
                            is_possible_image = True
                            reasons.append(f"图片扩展名(.{ext})")
                    
                    # 检查无扩展名但可能是图片的文件
                    if '.' not in filename and size_kb > 10:
                        is_possible_image = True
                        reasons.append("无扩展名但较大文件")
                    
                    if is_possible_image:
                        possible_image_files.append((file_path, size_kb, reasons))
            
            print(f"发现 {len(possible_image_files)} 个可能的图片文件:")
            for file_path, size_kb, reasons in possible_image_files:
                print(f"  {file_path:35} ({size_kb:6.1f} KB) - {', '.join(reasons)}")
                
    except Exception as e:
        print(f"分析失败: {e}")
    finally:
        if os.path.exists(temp_zip):
            os.remove(temp_zip)

def extract_all_possible_images(excel_file, output_dir):
    """提取所有可能的图片文件，不限制格式"""
    print(f"\n=== 提取所有可能的图片文件 ===")
    
    os.makedirs(output_dir, exist_ok=True)
    temp_zip = excel_file + '.temp.zip'
    shutil.copy2(excel_file, temp_zip)
    
    extracted_count = 0
    
    try:
        with zipfile.ZipFile(temp_zip, 'r') as zip_ref:
            for file_info in zip_ref.filelist:
                file_path = file_info.filename
                filename = os.path.basename(file_path)
                
                if not filename:  # 跳过目录
                    continue
                
                size_kb = file_info.file_size / 1024
                
                # 更宽松的图片检测条件
                should_extract = False
                
                # 条件1: 在media目录
                if 'media' in file_path:
                    should_extract = True
                
                # 条件2: 文件名包含image且大小>1KB
                if 'image' in filename.lower() and size_kb > 1:
                    should_extract = True
                
                # 条件3: 任何可能的图片扩展名
                if '.' in filename:
                    ext = filename.split('.')[-1].lower()
                    if ext in ['jpeg', 'jpg', 'png', 'gif', 'bmp', 'tiff', 'tif', 'webp', 'svg', 'ico', 'emf', 'wmf', 'eps', 'pdf']:
                        should_extract = True
                
                # 条件4: 无扩展名但可能是图片的文件（较大）
                if '.' not in filename and size_kb > 10:
                    should_extract = True
                
                if should_extract:
                    try:
                        # 提取文件
                        zip_ref.extract(file_path, output_dir + '_temp')
                        
                        # 复制到输出目录
                        source_path = os.path.join(output_dir + '_temp', file_path)
                        
                        # 生成目标文件名
                        if '.' not in filename:
                            target_filename = f"{filename}_extracted"
                        else:
                            target_filename = filename
                        
                        target_path = os.path.join(output_dir, f"all_{extracted_count+1:03d}_{target_filename}")
                        
                        shutil.copy2(source_path, target_path)
                        extracted_count += 1
                        
                        print(f"{extracted_count:2d}. {file_path:35} -> {target_filename:20} ({size_kb:6.1f} KB)")
                        
                    except Exception as e:
                        print(f"提取失败 {file_path}: {e}")
            
            # 清理临时目录
            temp_dir = output_dir + '_temp'
            if os.path.exists(temp_dir):
                shutil.rmtree(temp_dir)
                
    except Exception as e:
        print(f"提取失败: {e}")
    finally:
        if os.path.exists(temp_zip):
            os.remove(temp_zip)
    
    print(f"\n总共提取: {extracted_count} 个可能的图片文件")
    return extracted_count

def main():
    """主函数"""
    print("=" * 80)
    print("Excel完整文件格式分析工具")
    print("=" * 80)
    
    excel_file = "../../MineralRelated/仙台地层岩石矿物分析-完整.xlsx"
    
    if not os.path.exists(excel_file):
        print(f"Excel文件不存在: {excel_file}")
        return
    
    # 分析所有文件
    analyze_all_files_in_excel(excel_file)
    
    # 提取所有可能的图片
    output_dir = "../Images/All_Possible_Images"
    count = extract_all_possible_images(excel_file, output_dir)
    
    print(f"\n" + "=" * 80)
    print(f"分析完成!")
    print(f"所有可能的图片文件保存在: {output_dir}")
    print(f"总共提取: {count} 个文件")
    print("建议手动检查这些文件，确认是否有遗漏的图片")
    print("=" * 80)

if __name__ == "__main__":
    main()