#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
生成矿物图片映射表格
"""

import os
import csv

def generate_mineral_mapping_table():
    """生成矿物图片映射表格"""
    
    minerals_dir = "../Images/Minerals"
    output_file = "../../MineralRelated/矿物图片映射表.csv"
    
    # 中文名称映射
    chinese_names = {
        "amphibole": "角闪石",
        "biotite": "黑云母",
        "clay_minerals": "粘土矿物",
        "feldspar": "长石",
        "garnet": "石榴石",
        "heavy_minerals": "重矿物",
        "hypersthene": "紫苏辉石",
        "illite": "伊利石",
        "illite_alteration": "伊利石 (蚀变产物)",
        "magnetite": "磁铁矿",
        "olivine": "橄榄石",
        "orthopyroxene": "斜方辉石",
        "plagioclase": "斜长石",
        "pumice": "浮石",
        "pyroxene": "辉石",
        "quartz": "石英",
        "titanomagnetite": "钛磁铁矿",
        "volcanic_ash": "火山灰",
        "volcanic_glass": "火山玻璃",
        "zircon": "锆石"
    }
    
    # 获取所有图片文件
    if not os.path.exists(minerals_dir):
        print(f"目录不存在: {minerals_dir}")
        return
    
    image_files = []
    for file in os.listdir(minerals_dir):
        if file.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp')):
            image_files.append(file)
    
    # 按文件名排序
    image_files.sort()
    
    print(f"找到 {len(image_files)} 个图片文件")
    
    # 生成映射数据
    mapping_data = []
    for file in image_files:
        # 提取矿物ID（去掉_001和扩展名）
        base_name = file.rsplit('.', 1)[0]  # 移除扩展名
        if '_001' in base_name:
            mineral_id = base_name.replace('_001', '')
        else:
            mineral_id = base_name.rsplit('_', 1)[0]
        
        # 获取中文名称
        chinese_name = chinese_names.get(mineral_id, mineral_id)
        
        mapping_data.append({
            'chinese_name': chinese_name,
            'english_id': mineral_id,
            'filename': file,
            'mapping': f"{chinese_name}-{file}"
        })
    
    # 输出到CSV文件
    os.makedirs(os.path.dirname(output_file), exist_ok=True)
    
    with open(output_file, 'w', newline='', encoding='utf-8-sig') as csvfile:
        fieldnames = ['序号', '中文名称', '英文ID', '图片文件名', '映射关系']
        writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
        
        # 写入标题行
        writer.writeheader()
        
        # 写入数据
        for i, data in enumerate(mapping_data, 1):
            writer.writerow({
                '序号': i,
                '中文名称': data['chinese_name'],
                '英文ID': data['english_id'],
                '图片文件名': data['filename'],
                '映射关系': data['mapping']
            })
    
    print(f"\\n映射表已生成: {output_file}")
    print(f"总计 {len(mapping_data)} 条记录")
    
    # 显示映射关系
    print(f"\\n=== 矿物图片映射关系 ===")
    for i, data in enumerate(mapping_data, 1):
        print(f"{i:2d}. {data['mapping']}")
    
    return output_file

def main():
    """主函数"""
    print("=" * 60)
    print("矿物图片映射表生成工具")
    print("=" * 60)
    
    output_file = generate_mineral_mapping_table()
    
    print("\\n" + "=" * 60)
    print("✅ 完成!")
    print(f"映射表已保存为CSV格式，可在Excel中打开")
    print("=" * 60)

if __name__ == "__main__":
    main()