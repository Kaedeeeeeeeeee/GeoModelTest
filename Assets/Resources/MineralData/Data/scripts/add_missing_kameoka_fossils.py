#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
添加缺失的亀岡層化石数据到数据库中
"""

import json
import os

def generate_fossil_id(fossil_name):
    """生成化石ID"""
    name_map = {
        "珪化木": "silicified_wood",
        "葉印象": "leaf_impressions", 
        "埋没木": "buried_wood",
        "淡水貝類": "freshwater_shellfish"
    }
    return name_map.get(fossil_name, fossil_name.lower().replace(" ", "_"))

def translate_fossil_name_en(chinese_name):
    """翻译化石名称为英文"""
    translations = {
        "珪化木": "Silicified Wood",
        "葉印象": "Leaf Impressions",
        "埋没木": "Buried Wood", 
        "淡水貝類": "Freshwater Shellfish"
    }
    return translations.get(chinese_name, chinese_name)

def translate_fossil_name_ja(chinese_name):
    """翻译化石名称为日文"""
    translations = {
        "珪化木": "珪化木",
        "葉印象": "葉印象",
        "埋没木": "埋没木",
        "淡水貝類": "淡水貝類"
    }
    return translations.get(chinese_name, chinese_name)

def determine_fossil_rarity(fossil_name):
    """确定化石稀有度和发现概率"""
    # 根据化石类型设定稀有度
    uncommon_fossils = ["珪化木", "埋没木"]
    
    if fossil_name in uncommon_fossils:
        return "uncommon", 0.03  # 3%概率
    else:
        return "common", 0.05  # 5%概率

def add_kameoka_fossils():
    """添加亀岡層的化石数据"""
    
    database_file = "../SendaiMineralDatabase.json"
    
    print("=" * 60)
    print("添加亀岡層化石数据")
    print("=" * 60)
    
    # 读取现有数据库
    if not os.path.exists(database_file):
        print(f"数据库文件不存在: {database_file}")
        return
    
    with open(database_file, 'r', encoding='utf-8') as f:
        database = json.load(f)
    
    # 亀岡層的化石数据
    kameoka_fossils_data = [
        "珪化木",
        "葉印象", 
        "埋没木",
        "淡水貝類"
    ]
    
    # 查找亀岡層
    kameoka_layer = None
    for layer in database["stratigraphicLayers"]:
        if "亀岡" in layer["layerName"] or layer["layerId"] == "sendai_kameoka":
            kameoka_layer = layer
            break
    
    if not kameoka_layer:
        print("未找到亀岡層数据")
        return
    
    print(f"找到地层: {kameoka_layer['layerName']}")
    
    # 生成化石数据
    fossils = []
    for fossil_name in kameoka_fossils_data:
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
                "description": f"在{kameoka_layer['layerName']}中发现的{fossil_name}"
            }
        }
        
        fossils.append(fossil_data)
    
    # 添加化石数据到亀岡層
    kameoka_layer["fossils"] = fossils
    
    print(f"\\n添加的化石 ({len(fossils)} 种):")
    for fossil in fossils:
        print(f"  - {fossil['fossilName']} ({fossil['rarity']}, {fossil['discoveryProbability']*100:.1f}%)")
    
    # 保存更新后的数据库
    with open(database_file, 'w', encoding='utf-8') as f:
        json.dump(database, f, ensure_ascii=False, indent=2)
    
    print(f"\\n✅ 亀岡層化石数据添加完成!")
    print(f"数据库已更新: {database_file}")

def main():
    """主函数"""
    add_kameoka_fossils()
    
    print("\\n" + "=" * 60)
    print("所有亀岡層化石数据已成功添加到数据库中")
    print("=" * 60)

if __name__ == "__main__":
    main()