#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Excel到CSV转换工具
使用zipfile直接读取Excel内部结构
"""

import zipfile
import xml.etree.ElementTree as ET
import csv
import os

def extract_excel_data(excel_path):
    """从Excel文件中提取数据"""
    data = []
    
    try:
        with zipfile.ZipFile(excel_path, 'r') as zip_file:
            # 读取共享字符串
            shared_strings = []
            try:
                with zip_file.open('xl/sharedStrings.xml') as f:
                    tree = ET.parse(f)
                    root = tree.getroot()
                    
                    # 定义命名空间
                    ns = {'': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
                    
                    for si in root.findall('.//si', ns):
                        t = si.find('.//t', ns)
                        if t is not None:
                            shared_strings.append(t.text or "")
                        else:
                            shared_strings.append("")
            except KeyError:
                print("没有找到共享字符串文件")
            
            # 读取工作表数据
            with zip_file.open('xl/worksheets/sheet1.xml') as f:
                tree = ET.parse(f)
                root = tree.getroot()
                
                # 定义命名空间
                ns = {'': 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'}
                
                # 获取所有行
                rows = root.findall('.//row', ns)
                
                for row in rows:
                    row_data = []
                    cells = row.findall('.//c', ns)
                    
                    # 当前列索引
                    col_index = 0
                    
                    for cell in cells:
                        # 获取单元格引用 (如 A1, B1)
                        cell_ref = cell.get('r')
                        if cell_ref:
                            # 计算列位置
                            col_letter = ''.join([c for c in cell_ref if c.isalpha()])
                            target_col = 0
                            for char in col_letter:
                                target_col = target_col * 26 + (ord(char) - ord('A') + 1)
                            target_col -= 1  # 转为0-based索引
                            
                            # 填充空列
                            while col_index < target_col:
                                row_data.append("")
                                col_index += 1
                        
                        # 获取单元格值
                        cell_type = cell.get('t')
                        v = cell.find('.//v', ns)
                        
                        if v is not None:
                            if cell_type == 's':  # 共享字符串
                                idx = int(v.text)
                                if idx < len(shared_strings):
                                    row_data.append(shared_strings[idx])
                                else:
                                    row_data.append("")
                            else:  # 数值或其他
                                row_data.append(v.text or "")
                        else:
                            row_data.append("")
                        
                        col_index += 1
                    
                    if row_data:  # 只添加非空行
                        data.append(row_data)
    
    except Exception as e:
        print(f"读取Excel时发生错误: {e}")
        return []
    
    return data

def save_to_csv(data, csv_path):
    """保存数据到CSV文件"""
    if not data:
        print("没有数据可保存")
        return
    
    try:
        with open(csv_path, 'w', newline='', encoding='utf-8') as f:
            writer = csv.writer(f)
            
            # 确定最大列数
            max_cols = max(len(row) for row in data) if data else 0
            
            # 补齐所有行到相同列数
            for row in data:
                while len(row) < max_cols:
                    row.append("")
                writer.writerow(row)
        
        print(f"CSV文件已保存: {csv_path}")
        print(f"总行数: {len(data)}")
        print(f"总列数: {max_cols}")
        
    except Exception as e:
        print(f"保存CSV时发生错误: {e}")

def main():
    """主函数"""
    excel_path = "仙台地层岩石矿物分析-完整.xlsx"
    csv_path = "仙台地层岩石矿物分析-完整-新.csv"
    
    if not os.path.exists(excel_path):
        print(f"Excel文件不存在: {excel_path}")
        return
    
    print("开始读取Excel文件...")
    data = extract_excel_data(excel_path)
    
    if data:
        print("开始保存为CSV...")
        save_to_csv(data, csv_path)
        
        # 显示前几行数据预览
        print("\n前5行数据预览:")
        for i, row in enumerate(data[:5]):
            print(f"行 {i+1}: {row}")
        
        # 分析矿物种类
        if len(data) > 1:  # 跳过标题行
            minerals = set()
            for row in data[1:]:  # 跳过标题行
                if len(row) > 2 and row[2]:  # 构成矿物列
                    minerals.add(row[2].strip())
            
            print(f"\n发现的矿物种类 ({len(minerals)} 种):")
            for mineral in sorted(minerals):
                print(f"  - {mineral}")
    else:
        print("没有读取到数据")

if __name__ == "__main__":
    main()