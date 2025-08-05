#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
详细检查Excel中的所有图片
重新分析Excel文件中的图片数量和分布
"""

import zipfile
import xml.etree.ElementTree as ET
import os
import shutil

def analyze_excel_structure(excel_file):
    """详细分析Excel文件结构"""
    print(f"分析Excel文件: {excel_file}")
    
    if not os.path.exists(excel_file):
        print(f"文件不存在: {excel_file}")
        return
    
    # 复制为ZIP文件
    temp_zip = excel_file + '.temp.zip'
    shutil.copy2(excel_file, temp_zip)
    
    try:
        with zipfile.ZipFile(temp_zip, 'r') as zip_ref:
            print("\n=== Excel文件内容结构 ===")
            all_files = zip_ref.namelist()
            
            # 分类显示文件
            media_files = [f for f in all_files if f.startswith('xl/media/')]
            drawing_files = [f for f in all_files if 'drawing' in f]
            worksheet_files = [f for f in all_files if f.startswith('xl/worksheets/')]
            
            print(f"总文件数: {len(all_files)}")
            print(f"媒体文件: {len(media_files)} 个")
            print(f"绘图文件: {len(drawing_files)} 个") 
            print(f"工作表文件: {len(worksheet_files)} 个")
            
            # 详细显示媒体文件
            if media_files:
                print(f"\n=== 媒体文件列表 ({len(media_files)} 个) ===")
                for i, media_file in enumerate(media_files, 1):
                    file_info = zip_ref.getinfo(media_file)
                    size_kb = file_info.file_size / 1024
                    print(f"{i:2d}. {media_file:25} ({size_kb:6.1f} KB)")
                    
                    # 检查文件类型
                    ext = media_file.lower().split('.')[-1] if '.' in media_file else 'unknown'
                    if ext not in ['jpeg', 'jpg', 'png', 'gif', 'bmp']:
                        print(f"    警告: 非图片文件类型 - {ext}")
            
            # 检查绘图关系文件
            if drawing_files:
                print(f"\n=== 绘图关系文件 ===")
                for drawing_file in drawing_files:
                    print(f"  {drawing_file}")
            
            # 尝试分析工作表中的图片引用
            try:
                analyze_worksheet_images(zip_ref, worksheet_files)
            except Exception as e:
                print(f"分析工作表图片引用失败: {e}")
                
    except Exception as e:
        print(f"分析Excel文件失败: {e}")
    finally:
        # 清理临时文件
        if os.path.exists(temp_zip):
            os.remove(temp_zip)

def analyze_worksheet_images(zip_ref, worksheet_files):
    """分析工作表中的图片引用"""
    print(f"\n=== 工作表图片引用分析 ===")
    
    for worksheet_file in worksheet_files:
        try:
            worksheet_content = zip_ref.read(worksheet_file).decode('utf-8')
            
            # 查找图片相关的XML标记
            if '<drawing' in worksheet_content or 'image' in worksheet_content.lower():
                print(f"工作表 {worksheet_file} 包含图片引用")
                
                # 尝试解析XML
                try:
                    root = ET.fromstring(worksheet_content)
                    
                    # 查找绘图元素
                    for elem in root.iter():
                        if 'drawing' in elem.tag.lower():
                            print(f"  找到绘图元素: {elem.tag}")
                            if elem.attrib:
                                print(f"  属性: {elem.attrib}")
                                
                except ET.ParseError:
                    print(f"  XML解析失败")
                    
        except Exception as e:
            print(f"分析工作表 {worksheet_file} 失败: {e}")

def extract_all_images_force(excel_file, output_dir):
    """强制提取所有图片，包括可能遗漏的"""
    print(f"\n=== 强制提取所有图片 ===")
    
    os.makedirs(output_dir, exist_ok=True)
    temp_zip = excel_file + '.temp2.zip'
    shutil.copy2(excel_file, temp_zip)
    
    extracted_count = 0
    
    try:
        with zipfile.ZipFile(temp_zip, 'r') as zip_ref:
            # 提取所有可能的图片文件
            for file_info in zip_ref.filelist:
                filename = file_info.filename
                
                # 更宽松的图片文件检测
                is_image = False
                
                # 检查文件扩展名
                if any(filename.lower().endswith(ext) for ext in ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.tiff', '.tif']):
                    is_image = True
                
                # 检查是否在媒体目录
                if filename.startswith('xl/media/'):
                    is_image = True
                
                # 检查是否包含image关键字
                if 'image' in filename.lower():
                    is_image = True
                
                if is_image:
                    try:
                        # 提取文件
                        zip_ref.extract(filename, output_dir + '_full_extract')
                        
                        # 复制到输出目录，使用简化的文件名
                        source_path = os.path.join(output_dir + '_full_extract', filename)
                        
                        # 生成目标文件名
                        base_name = os.path.basename(filename)
                        if not base_name:
                            base_name = f"extracted_image_{extracted_count + 1}"
                        
                        # 确保有扩展名
                        if '.' not in base_name:
                            base_name += '.jpg'
                        
                        target_path = os.path.join(output_dir, f"raw_{extracted_count + 1:03d}_{base_name}")
                        
                        shutil.copy2(source_path, target_path)
                        extracted_count += 1
                        
                        # 显示文件信息
                        size_kb = file_info.file_size / 1024
                        print(f"{extracted_count:2d}. {filename:35} -> {base_name:20} ({size_kb:6.1f} KB)")
                        
                    except Exception as e:
                        print(f"提取失败 {filename}: {e}")
            
            # 清理临时提取目录
            temp_extract_dir = output_dir + '_full_extract'
            if os.path.exists(temp_extract_dir):
                shutil.rmtree(temp_extract_dir)
                
    except Exception as e:
        print(f"强制提取失败: {e}")
    finally:
        if os.path.exists(temp_zip):
            os.remove(temp_zip)
    
    print(f"\n强制提取完成: {extracted_count} 个文件")
    return extracted_count

def main():
    """主函数"""
    print("=" * 80)
    print("Excel图片详细分析工具")
    print("=" * 80)
    
    excel_file = "../../MineralRelated/仙台地层岩石矿物分析-完整.xlsx"
    
    if not os.path.exists(excel_file):
        print(f"Excel文件不存在: {excel_file}")
        return
    
    # 详细分析Excel结构
    analyze_excel_structure(excel_file)
    
    # 强制提取所有图片
    output_dir = "../Images/All_Extracted"
    count = extract_all_images_force(excel_file, output_dir)
    
    print(f"\n" + "=" * 80)
    print(f"分析完成!")
    print(f"提取的图片保存在: {output_dir}")
    print(f"总共提取: {count} 个图片文件")
    print("=" * 80)

if __name__ == "__main__":
    main()