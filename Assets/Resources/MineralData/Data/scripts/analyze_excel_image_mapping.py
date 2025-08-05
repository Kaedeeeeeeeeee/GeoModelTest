#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
分析Excel中图片与矿物行的精确对应关系
通过解析Excel的绘图关系文件来确定每张图片对应哪一行
"""

import zipfile
import xml.etree.ElementTree as ET
import os
import shutil
import csv

def parse_drawing_relationships(zip_ref):
    """解析绘图关系文件"""
    drawing_rels = {}
    
    try:
        # 读取绘图关系文件
        rels_content = zip_ref.read('xl/drawings/_rels/drawing1.xml.rels')
        root = ET.fromstring(rels_content)
        
        print("=== 绘图关系文件分析 ===")
        
        for relationship in root.iter():
            if relationship.tag.endswith('Relationship'):
                rel_id = relationship.get('Id')
                target = relationship.get('Target')
                rel_type = relationship.get('Type')
                
                if target and target.startswith('../media/'):
                    media_file = target.replace('../media/', 'xl/media/')
                    drawing_rels[rel_id] = media_file
                    print(f"关系 {rel_id}: {media_file}")
                    
    except Exception as e:
        print(f"解析绘图关系失败: {e}")
    
    return drawing_rels

def parse_drawing_positions(zip_ref, drawing_rels):
    """解析绘图位置信息"""
    image_positions = {}
    
    try:
        # 读取绘图文件
        drawing_content = zip_ref.read('xl/drawings/drawing1.xml')
        
        # 注册命名空间
        namespaces = {
            'xdr': 'http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing',
            'a': 'http://schemas.openxmlformats.org/drawingml/2006/main',
            'r': 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'
        }
        
        root = ET.fromstring(drawing_content)
        
        print("\n=== 图片位置分析 ===")
        
        for anchor in root.findall('.//xdr:twoCellAnchor', namespaces):
            # 获取起始位置
            from_cell = anchor.find('xdr:from', namespaces)
            if from_cell is not None:
                col = from_cell.find('xdr:col', namespaces)
                row = from_cell.find('xdr:row', namespaces)
                
                if col is not None and row is not None:
                    start_col = int(col.text)
                    start_row = int(row.text) + 1  # Excel行号从1开始
                    
                    # 查找图片引用
                    pic = anchor.find('.//xdr:pic', namespaces)
                    if pic is not None:
                        blip_fill = pic.find('.//a:blipFill', namespaces)
                        if blip_fill is not None:
                            blip = blip_fill.find('a:blip', namespaces)
                            if blip is not None:
                                embed_id = blip.get('{http://schemas.openxmlformats.org/officeDocument/2006/relationships}embed')
                                
                                if embed_id in drawing_rels:
                                    media_file = drawing_rels[embed_id]
                                    image_positions[media_file] = (start_row, start_col)
                                    print(f"图片 {media_file} 位置: 行{start_row}, 列{start_col}")
                                    
    except Exception as e:
        print(f"解析绘图位置失败: {e}")
    
    return image_positions

def map_images_to_minerals(excel_file, csv_file):
    """将图片映射到具体的矿物"""
    print("=" * 80)
    print("Excel图片与矿物精确映射分析")
    print("=" * 80)
    
    # 读取CSV数据
    minerals_data = []
    with open(csv_file, 'r', encoding='utf-8') as file:
        reader = csv.reader(file)
        headers = next(reader)
        
        for row_num, row in enumerate(reader, start=2):  # Excel行号从2开始（跳过标题行）
            if len(row) >= 3:
                layer_name = row[0].strip()
                rock_type = row[1].strip()
                mineral_name = row[2].strip()
                
                if mineral_name:
                    minerals_data.append({
                        'excel_row': row_num,
                        'layer': layer_name,
                        'rock': rock_type, 
                        'mineral': mineral_name
                    })
    
    print(f"CSV数据: {len(minerals_data)} 个矿物记录")
    
    # 分析Excel图片位置
    temp_zip = excel_file + '.temp.zip'
    shutil.copy2(excel_file, temp_zip)
    
    try:
        with zipfile.ZipFile(temp_zip, 'r') as zip_ref:
            drawing_rels = parse_drawing_relationships(zip_ref)
            image_positions = parse_drawing_positions(zip_ref, drawing_rels)
            
            print(f"\n发现 {len(image_positions)} 个图片位置")
            
            # 映射图片到矿物
            print("\n=== 图片-矿物映射结果 ===")
            
            mapped_count = 0
            for media_file, (row, col) in sorted(image_positions.items(), key=lambda x: x[1][0]):
                # 查找对应行的矿物数据
                corresponding_mineral = None
                for mineral_data in minerals_data:
                    if mineral_data['excel_row'] == row:
                        corresponding_mineral = mineral_data
                        break
                
                if corresponding_mineral:
                    print(f"行{row:2d}: {media_file:20} -> {corresponding_mineral['mineral']}")
                    mapped_count += 1
                else:
                    print(f"行{row:2d}: {media_file:20} -> 未找到对应矿物")
            
            print(f"\n成功映射: {mapped_count} 个图片")
            
            # 统计没有图片的矿物
            minerals_with_images = set()
            for media_file, (row, col) in image_positions.items():
                for mineral_data in minerals_data:
                    if mineral_data['excel_row'] == row:
                        minerals_with_images.add(mineral_data['mineral'])
            
            minerals_without_images = []
            all_minerals = set(m['mineral'] for m in minerals_data)
            
            for mineral in all_minerals:
                if mineral not in minerals_with_images:
                    minerals_without_images.append(mineral)
            
            print(f"\n=== 没有图片的矿物 ({len(minerals_without_images)} 个) ===")
            for mineral in sorted(minerals_without_images):
                print(f"  - {mineral}")
                
    except Exception as e:
        print(f"分析失败: {e}")
    finally:
        if os.path.exists(temp_zip):
            os.remove(temp_zip)

def main():
    """主函数"""
    excel_file = "../../MineralRelated/仙台地层岩石矿物分析-完整.xlsx"
    csv_file = "../../MineralRelated/仙台地层岩石矿物分析-完整.csv"
    
    if not os.path.exists(excel_file):
        print(f"Excel文件不存在: {excel_file}")
        return
        
    if not os.path.exists(csv_file):
        print(f"CSV文件不存在: {csv_file}")
        return
    
    map_images_to_minerals(excel_file, csv_file)

if __name__ == "__main__":
    main()