#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
将化石数据添加到矿物数据库中
化石放在地层级别，与岩石并行
"""

import json
import csv
import os

def generate_fossil_id(fossil_name):
    """生成化石ID"""
    name_map = {
        "植物遺骸": "plant_remains",
        "浮遊性珪藻": "planktonic_diatoms",
        "有孔虫": "foraminifera", 
        "貝類": "shellfish",
        "陸上植物の葉化石": "terrestrial_plant_leaf_fossils",
        "花粉化石": "pollen_fossils",
        "淡水貝類": "freshwater_shellfish",
        "魚類化石": "fish_fossils",
        "スギ科の珪化木": "cupressaceae_silicified_wood",
        "樹根化石林": "root_fossil_forest",
        "センダイヌノメハマグリ": "sendai_clam",
        "タカハシホタテ": "takahashi_scallop",
        "鯨類化石": "cetacean_fossils",
        "サメ化石": "shark_fossils",
        "象化石": "elephant_fossils",
        "馬化石": "horse_fossils",
        "珪化木": "silicified_wood",
        "葉印象": "leaf_impressions",
        "埋没木": "buried_wood"
    }
    
    return name_map.get(fossil_name, fossil_name.lower().replace(" ", "_"))

def translate_fossil_name_en(chinese_name):
    """翻译化石名称为英文"""
    translations = {
        "植物遺骸": "Plant Remains",
        "浮遊性珪藻": "Planktonic Diatoms",
        "有孔虫": "Foraminifera",
        "貝類": "Shellfish",
        "陸上植物の葉化石": "Terrestrial Plant Leaf Fossils",
        "花粉化石": "Pollen Fossils", 
        "淡水貝類": "Freshwater Shellfish",
        "魚類化石": "Fish Fossils",
        "スギ科の珪化木": "Cupressaceae Silicified Wood",
        "樹根化石林": "Root Fossil Forest",
        "センダイヌノメハマグリ": "Sendai Clam",
        "タカハシホタテ": "Takahashi Scallop",
        "鯨類化石": "Cetacean Fossils",
        "サメ化石": "Shark Fossils",
        "象化石": "Elephant Fossils",
        "馬化石": "Horse Fossils",
        "珪化木": "Silicified Wood",
        "葉印象": "Leaf Impressions",
        "埋没木": "Buried Wood"
    }
    return translations.get(chinese_name, chinese_name)

def translate_fossil_name_ja(chinese_name):
    """翻译化石名称为日文（大部分已经是日文）"""
    translations = {
        "植物遺骸": "植物遺骸",
        "浮遊性珪藻": "浮遊性珪藻",
        "有孔虫": "有孔虫",
        "貝類": "貝類",
        "陸上植物の葉化石": "陸上植物の葉化石",
        "花粉化石": "花粉化石",
        "淡水貝類": "淡水貝類", 
        "魚類化石": "魚類化石",
        "スギ科の珪化木": "スギ科の珪化木",
        "樹根化石林": "樹根化石林",
        "センダイヌノメハマグリ": "センダイヌノメハマグリ",
        "タカハシホタテ": "タカハシホタテ",
        "鯨類化石": "鯨類化石",
        "サメ化石": "サメ化石",
        "象化石": "象化石",
        "馬化石": "馬化石",
        "珪化木": "珪化木",
        "葉印象": "葉印象",
        "埋没木": "埋没木"
    }
    return translations.get(chinese_name, chinese_name)

def determine_fossil_rarity(fossil_name):
    """确定化石稀有度和发现概率"""
    # 根据化石类型设定稀有度
    rare_fossils = ["鯨類化石", "サメ化石", "象化石", "馬化石", "魚類化石"]
    uncommon_fossils = ["センダイヌノメハマグリ", "タカハシホタテ", "スギ科の珪化木", "樹根化石林"]
    
    if fossil_name in rare_fossils:
        return "rare", 0.01  # 1%概率
    elif fossil_name in uncommon_fossils:
        return "uncommon", 0.03  # 3%概率
    else:
        return "common", 0.05  # 5%概率

def get_layer_name_mapping():
    """获取地层名称映射"""
    return {
        "青葉山層": "sendai_aobayama",
        "大年寺層": "sendai_dainenji",
        "向山層下部": "sendai_mukoyama",  # 注意：CSV中是"向山層下部"
        "広瀬川凝灰岩部層": "sendai_hirosegawa_tuff",
        "竜ノ口層": "sendai_ryunokuchi",
        "亀岡層": "sendai_kameoka"
    }

def read_fossils_data(csv_file):
    """读取化石数据"""
    fossils_by_layer = {}
    
    with open(csv_file, 'r', encoding='utf-8-sig') as file:
        reader = csv.reader(file)
        headers = next(reader)  # 跳过标题行
        
        for row in reader:
            if len(row) >= 2:
                layer_name = row[0].strip()
                fossil_name = row[1].strip()
                
                if layer_name and fossil_name:
                    if layer_name not in fossils_by_layer:
                        fossils_by_layer[layer_name] = []
                    
                    rarity, probability = determine_fossil_rarity(fossil_name)
                    
                    fossil_data = {
                        "fossilId": generate_fossil_id(fossil_name),
                        "fossilName": fossil_name,
                        "fossilNameEN": translate_fossil_name_en(fossil_name),
                        "fossilNameJA": translate_fossil_name_ja(fossil_name),
                        "rarity": rarity,
                        "discoveryProbability": probability,
                        "properties": {
                            "type": "fossil",
                            "imageFile": f"{generate_fossil_id(fossil_name)}_001.jpg",
                            "modelFile": f"{generate_fossil_id(fossil_name)}_001.fbx",
                            "description": f"在{layer_name}中发现的{fossil_name}"
                        }
                    }
                    
                    fossils_by_layer[layer_name].append(fossil_data)
    
    return fossils_by_layer

def add_fossils_to_database(database_file, fossils_csv, output_file):
    """将化石数据添加到数据库中"""
    
    print("=" * 80)
    print("向矿物数据库添加化石数据")
    print("=" * 80)
    
    # 读取现有数据库
    if not os.path.exists(database_file):
        print(f"数据库文件不存在: {database_file}")
        return
    
    with open(database_file, 'r', encoding='utf-8') as f:
        database = json.load(f)
    
    # 读取化石数据
    fossils_by_layer = read_fossils_data(fossils_csv)
    layer_mapping = get_layer_name_mapping()
    
    print(f"读取到化石数据，涵盖 {len(fossils_by_layer)} 个地层")
    
    # 为每个地层添加化石数据
    updated_layers = 0
    total_fossils = 0
    
    for layer in database["stratigraphicLayers"]:
        layer_id = layer["layerId"]
        layer_name = layer["layerName"]
        
        # 查找对应的化石数据
        fossils = None
        for csv_layer_name, fossil_list in fossils_by_layer.items():
            if layer_mapping.get(csv_layer_name) == layer_id:
                fossils = fossil_list
                break
        
        if fossils:
            layer["fossils"] = fossils
            updated_layers += 1
            total_fossils += len(fossils)
            print(f"✓ {layer_name}: 添加 {len(fossils)} 个化石")
            
            # 显示化石详情
            for fossil in fossils:
                print(f"  - {fossil['fossilName']} ({fossil['rarity']}, {fossil['discoveryProbability']*100:.1f}%)")
        else:
            layer["fossils"] = []
            print(f"○ {layer_name}: 无化石数据")
    
    # 更新数据库版本信息
    database["version"] = "1.1"
    database["lastUpdated"] = "2025-01-27"
    database["description"] = "仙台地区地质样本矿物数据库 (包含化石数据)"
    
    # 保存更新后的数据库
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(database, f, ensure_ascii=False, indent=2)
    
    print(f"\\n=== 更新完成 ===")
    print(f"更新的地层数: {updated_layers}")
    print(f"添加的化石总数: {total_fossils}")
    print(f"输出文件: {output_file}")
    
    # 显示化石稀有度统计
    rarity_stats = {"common": 0, "uncommon": 0, "rare": 0}
    for fossil_list in fossils_by_layer.values():
        for fossil in fossil_list:
            rarity_stats[fossil["rarity"]] += 1
    
    print(f"\\n=== 化石稀有度统计 ===")
    print(f"常见 (common): {rarity_stats['common']} 种 (5%概率)")
    print(f"少见 (uncommon): {rarity_stats['uncommon']} 种 (3%概率)")
    print(f"稀有 (rare): {rarity_stats['rare']} 种 (1%概率)")

def main():
    """主函数"""
    database_file = "SendaiMineralDatabase.json"
    fossils_csv = "../../MineralRelated/sendai_fossils_expanded.csv"
    output_file = "SendaiMineralDatabase_WithFossils.json"
    
    if not os.path.exists(database_file):
        print(f"数据库文件不存在: {database_file}")
        return
        
    if not os.path.exists(fossils_csv):
        print(f"化石CSV文件不存在: {fossils_csv}")
        return
    
    add_fossils_to_database(database_file, fossils_csv, output_file)
    
    print("\\n" + "=" * 80)
    print("✅ 化石数据添加完成!")
    print("数据结构: 地层级别 → 化石 (与岩石并行)")
    print("游戏逻辑: 在任意岩石中都可能发现该地层的化石")
    print("=" * 80)

if __name__ == "__main__":
    main()