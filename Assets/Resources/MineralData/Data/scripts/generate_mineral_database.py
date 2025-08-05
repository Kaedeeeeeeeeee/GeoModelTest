#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
仙台地质数据转换脚本
将CSV数据转换为游戏可用的JSON格式
"""

import csv
import json
import os

def process_csv_to_json(csv_file_path, output_path):
    """
    将CSV文件转换为结构化的JSON数据库
    """
    
    # 存储所有数据
    mineral_database = {
        "version": "1.0",
        "lastUpdated": "2025-01-27", 
        "description": "仙台地区地质样本矿物数据库",
        "stratigraphicLayers": []
    }
    
    current_layer_name = None
    current_rock_data = None
    layer_dict = {}
    
    with open(csv_file_path, 'r', encoding='utf-8') as file:
        reader = csv.reader(file)
        headers = next(reader)  # 跳过标题行
        
        for row in reader:
            if len(row) < 12:
                continue
                
            layer_name = row[0].strip()
            rock_type = row[1].strip() 
            mineral_name = row[2].strip()
            percentage = float(row[3]) if row[3] else 0.0
            hardness = row[4].strip()
            acid_reaction = row[5].strip() == "是"
            uv_fluorescence = row[6].strip()
            magnetism = row[7].strip()
            density = row[8].strip()
            polarized_color = row[9].strip()
            appearance = row[10].strip()
            image_file = row[11].strip()
            
            # 处理地层名称：如果为空，使用前一行的地层名
            if layer_name:
                current_layer_name = layer_name
            elif current_layer_name:
                layer_name = current_layer_name
            else:
                continue  # 没有地层信息，跳过
            
            # 创建地层数据结构
            if layer_name not in layer_dict:
                layer_id = generate_layer_id(layer_name)
                layer_dict[layer_name] = {
                    "layerId": layer_id,
                    "layerName": layer_name,
                    "layerNameEN": translate_layer_name(layer_name),
                    "layerNameJA": translate_layer_name_ja(layer_name),
                    "rockTypes": []
                }
                
            current_layer_data = layer_dict[layer_name]
            
            # 处理岩石类型：如果为空，使用前一行的岩石类型
            if rock_type:
                # 查找是否已存在该岩石类型
                rock_exists = False
                for rock in current_layer_data["rockTypes"]:
                    if rock["rockName"] == rock_type:
                        current_rock_data = rock
                        rock_exists = True
                        break
                        
                if not rock_exists:
                    rock_id = generate_rock_id(layer_name, rock_type)
                    current_rock_data = {
                        "rockId": rock_id,
                        "rockName": rock_type,
                        "rockNameEN": translate_rock_name(rock_type),
                        "rockNameJA": translate_rock_name_ja(rock_type),
                        "minerals": []
                    }
                    current_layer_data["rockTypes"].append(current_rock_data)
            elif current_rock_data is None:
                # 如果没有当前岩石且岩石类型为空，跳过
                continue
            
            # 处理矿物
            if mineral_name:
                mineral_id = generate_mineral_id(mineral_name)
                mineral_data = {
                    "mineralId": mineral_id,
                    "mineralName": mineral_name,
                    "mineralNameEN": translate_mineral_name(mineral_name),
                    "mineralNameJA": translate_mineral_name_ja(mineral_name),
                    "percentage": percentage,
                    "properties": {
                        "mohsHardness": hardness,
                        "acidReaction": acid_reaction,
                        "uvFluorescence": uv_fluorescence,
                        "magnetism": magnetism,
                        "density": density,
                        "polarizedColor": polarized_color,
                        "appearance": appearance,
                        "imageFile": generate_image_filename(mineral_name, mineral_id),
                        "modelFile": generate_model_filename(mineral_name, mineral_id)
                    }
                }
                current_rock_data["minerals"].append(mineral_data)
    
    # 转换为列表
    mineral_database["stratigraphicLayers"] = list(layer_dict.values())
    
    # 保存JSON文件
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(mineral_database, f, ensure_ascii=False, indent=2)
    
    print(f"数据库已生成: {output_path}")
    print(f"地层数量: {len(mineral_database['stratigraphicLayers'])}")
    
    return mineral_database

def generate_layer_id(layer_name):
    """生成地层ID"""
    name_map = {
        "青葉山層": "sendai_aobayama",
        "大年寺層": "sendai_dainenji", 
        "向山層": "sendai_mukoyama",
        "広瀬川凝灰岩部層": "sendai_hirosegawa_tuff",
        "竜ノ口層": "sendai_ryunokuchi"
    }
    return name_map.get(layer_name, layer_name.lower().replace("層", "").replace(" ", "_"))

def generate_rock_id(layer_name, rock_type):
    """生成岩石ID"""
    layer_prefix = generate_layer_id(layer_name).split("_")[-1]
    rock_suffix = rock_type.replace("/", "_").replace(" ", "_").lower()
    return f"{layer_prefix}_{rock_suffix}"

def generate_mineral_id(mineral_name):
    """生成矿物ID - 扩展版本包含所有矿物"""
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

def translate_layer_name(chinese_name):
    """翻译地层名称为英文"""
    translations = {
        "青葉山層": "Aobayama Formation",
        "大年寺層": "Dainenji Formation",
        "向山層": "Mukoyama Formation", 
        "広瀬川凝灰岩部層": "Hirosegawa Tuff Member",
        "竜ノ口層": "Ryunokuchi Formation"
    }
    return translations.get(chinese_name, chinese_name)

def translate_rock_name(chinese_name):
    """翻译岩石名称为英文"""
    translations = {
        "砾岩": "Conglomerate",
        "火山灰": "Volcanic Ash",
        "粉砂岩/砂岩": "Siltstone/Sandstone",
        "砂岩/粉砂岩": "Sandstone/Siltstone",
        "亚炭": "Lignite",
        "英安岩质熔结凝灰岩": "Dacitic Welded Tuff",
        "粉砂岩/细粒砂岩": "Siltstone/Fine Sandstone"
    }
    return translations.get(chinese_name, chinese_name)

def translate_mineral_name(chinese_name):
    """翻译矿物名称为英文 - 扩展版本"""
    translations = {
        "石英": "Quartz",
        "斜长石": "Plagioclase",
        "辉石": "Pyroxene", 
        "角闪石": "Amphibole",
        "磁铁矿": "Magnetite",
        "橄榄石": "Olivine",
        "长石": "Feldspar",
        "黑云母": "Biotite",
        "锆石": "Zircon",
        "火山玻璃": "Volcanic Glass",
        "普通辉石": "Augite",
        "紫苏辉石": "Hypersthene", 
        "石榴石": "Garnet",
        "粘土矿物": "Clay Minerals",
        "钛磁铁矿": "Titanomagnetite",
        "斜方辉石": "Orthopyroxene",
        "普通角闪石": "Common Amphibole",
        "碳化植物遗体": "Carbonized Plant Remains",
        "蒙脱石": "Montmorillonite",
        "埃洛石": "Halloysite", 
        "高岭石": "Kaolinite",
        "伊利石": "Illite",
        "硅化木": "Silicified Wood",
        # 新增翻译
        "碳化植物遗体 (非矿物)": "Carbonized Plant Remains (Non-mineral)",
        "黑云母 (高钾型中常见)": "Biotite (High-K Type)",
        "伊利石 (蚀变产物)": "Illite (Alteration Product)",
        "部分为二氧化硅交代的硅化木": "Partially Silicified Wood",
        "高温石英": "High-Temperature Quartz",
        "石英 (沉积)": "Quartz (Sedimentary)",
        "火山灰": "Volcanic Ash",
        "浮石": "Pumice",
        "高温石英 (火山)": "High-Temperature Quartz (Volcanic)",
        "重矿物": "Heavy Minerals",
        "细粒石英": "Fine-Grained Quartz",
        "碳质物": "Carbonaceous Material"
    }
    return translations.get(chinese_name, chinese_name)

def translate_layer_name_ja(chinese_name):
    """翻译地层名称为日文"""
    translations = {
        "青葉山層": "青葉山層",
        "大年寺層": "大年寺層",
        "向山層": "向山層",
        "広瀬川凝灰岩部層": "広瀬川凝灰岩部層",
        "竜ノ口層": "竜ノ口層",
        "亀岡層": "亀岡層"
    }
    return translations.get(chinese_name, chinese_name)

def translate_rock_name_ja(chinese_name):
    """翻译岩石名称为日文"""
    translations = {
        "砾岩": "礫岩",
        "火山灰": "火山灰",
        "粉砂岩/砂岩": "シルト岩/砂岩",
        "砂岩/粉砂岩": "砂岩/シルト岩", 
        "亚炭": "亜炭",
        "英安岩质熔结凝灰岩": "デイサイト質溶結凝灰岩",
        "粉砂岩/细粒砂岩": "シルト岩/細粒砂岩",
        "凝灰岩": "凝灰岩",
        "凝灰质砂岩": "凝灰質砂岩",
        "粉砂岩": "シルト岩"
    }
    return translations.get(chinese_name, chinese_name)

def translate_mineral_name_ja(chinese_name):
    """翻译矿物名称为日文"""
    translations = {
        "石英": "石英",
        "斜长石": "斜長石",
        "辉石": "輝石",
        "角闪石": "角閃石",
        "磁铁矿": "磁鉄鉱",
        "橄榄石": "橄欖石",
        "长石": "長石",
        "黑云母": "黒雲母",
        "锆石": "ジルコン",
        "火山玻璃": "火山ガラス",
        "普通辉石": "普通輝石",
        "紫苏辉石": "紫蘇輝石",
        "石榴石": "ザクロ石",
        "粘土矿物": "粘土鉱物",
        "钛磁铁矿": "チタン磁鉄鉱",
        "斜方辉石": "斜方輝石",
        "普通角闪石": "普通角閃石",
        "碳化植物遗体": "炭化植物遺体",
        "蒙脱石": "モンモリロナイト",
        "埃洛石": "ハロイサイト",
        "高岭石": "カオリナイト",
        "伊利石": "イライト",
        "硅化木": "珪化木",
        "碳化植物遗体 (非矿物)": "炭化植物遺体 (非鉱物)",
        "黑云母 (高钾型中常见)": "黒雲母 (高カリウム型)",
        "伊利石 (蚀变产物)": "イライト (変質産物)",
        "部分为二氧化硅交代的硅化木": "部分珪化木",
        "高温石英": "高温石英",
        "石英 (沉积)": "石英 (堆積)",
        "火山灰": "火山灰",
        "浮石": "軽石",
        "高温石英 (火山)": "高温石英 (火山)",
        "重矿物": "重鉱物",
        "细粒石英": "細粒石英",
        "碳质物": "炭質物"
    }
    return translations.get(chinese_name, chinese_name)

def generate_image_filename(mineral_name, mineral_id):
    """生成图片文件名"""
    return f"{mineral_id}_001.jpg"

def generate_model_filename(mineral_name, mineral_id):
    """生成模型文件名"""
    return f"{mineral_id}_001.fbx"

if __name__ == "__main__":
    csv_path = "../../MineralRelated/仙台地层岩石矿物分析-完整-新.csv"
    output_path = "SendaiMineralDatabase.json"
    
    if os.path.exists(csv_path):
        process_csv_to_json(csv_path, output_path)
    else:
        print(f"CSV文件不存在: {csv_path}")