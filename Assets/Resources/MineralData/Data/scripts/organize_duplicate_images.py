#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
整理重复图片文件
将重复的矿物图片重命名为 mineral_002.jpg, mineral_003.jpg 等
"""

import os
from collections import defaultdict
import shutil

def organize_duplicate_images(images_dir):
    """整理重复的图片文件"""
    
    if not os.path.exists(images_dir):
        print(f"目录不存在: {images_dir}")
        return
    
    # 获取所有图片文件
    image_files = []
    for file in os.listdir(images_dir):
        if file.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp')):
            image_files.append(file)
    
    print(f"发现 {len(image_files)} 个图片文件")
    
    # 按矿物ID分组
    mineral_groups = defaultdict(list)
    for file in image_files:
        # 提取矿物ID (文件名中_001之前的部分)
        if '_001.' in file:
            mineral_id = file.split('_001.')[0]
            mineral_groups[mineral_id].append(file)
        else:
            # 处理其他格式的文件名
            base_name = file.rsplit('.', 1)[0]  # 移除扩展名
            if '_' in base_name:
                mineral_id = base_name.rsplit('_', 1)[0]  # 移除最后的数字部分
            else:
                mineral_id = base_name
            mineral_groups[mineral_id].append(file)
    
    print(f"发现 {len(mineral_groups)} 种不同的矿物")
    
    # 处理重复文件
    renamed_count = 0
    
    for mineral_id, files in mineral_groups.items():
        if len(files) > 1:
            print(f"\n处理重复矿物: {mineral_id} ({len(files)} 个文件)")
            
            # 按文件名排序，确保处理顺序一致
            files.sort()
            
            for i, old_file in enumerate(files):
                old_path = os.path.join(images_dir, old_file)
                
                # 获取文件扩展名
                _, ext = os.path.splitext(old_file)
                
                # 生成新文件名
                new_filename = f"{mineral_id}_{i+1:03d}{ext.lower()}"
                new_path = os.path.join(images_dir, new_filename)
                
                # 如果文件名已经正确，跳过
                if old_file == new_filename:
                    print(f"  保持: {old_file}")
                    continue
                
                try:
                    # 重命名文件
                    shutil.move(old_path, new_path)
                    print(f"  重命名: {old_file} -> {new_filename}")
                    renamed_count += 1
                    
                except Exception as e:
                    print(f"  错误: 重命名失败 {old_file}: {e}")
        else:
            # 单个文件，确保命名格式正确
            old_file = files[0]
            old_path = os.path.join(images_dir, old_file)
            
            _, ext = os.path.splitext(old_file)
            correct_filename = f"{mineral_id}_001{ext.lower()}"
            correct_path = os.path.join(images_dir, correct_filename)
            
            if old_file != correct_filename:
                try:
                    shutil.move(old_path, correct_path)
                    print(f"标准化: {old_file} -> {correct_filename}")
                    renamed_count += 1
                except Exception as e:
                    print(f"标准化失败 {old_file}: {e}")
    
    print(f"\n整理完成! 重命名了 {renamed_count} 个文件")
    
    # 显示最终结果
    final_files = []
    for file in os.listdir(images_dir):
        if file.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp')):
            final_files.append(file)
    
    print(f"\n最终图片文件 ({len(final_files)} 个):")
    for file in sorted(final_files):
        print(f"  - {file}")

def create_mineral_summary():
    """创建矿物图片汇总"""
    images_dir = "../Images/Minerals"
    
    if not os.path.exists(images_dir):
        return
    
    # 统计每种矿物的图片数量
    mineral_counts = defaultdict(int)
    
    for file in os.listdir(images_dir):
        if file.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp')):
            # 提取矿物ID
            base_name = file.rsplit('.', 1)[0]
            if '_' in base_name:
                mineral_id = base_name.rsplit('_', 1)[0]
                mineral_counts[mineral_id] += 1
    
    print(f"\n矿物图片统计:")
    print("-" * 40)
    for mineral_id in sorted(mineral_counts.keys()):
        count = mineral_counts[mineral_id]
        print(f"{mineral_id:25} : {count} 张图片")
    
    print(f"\n总计: {len(mineral_counts)} 种矿物, {sum(mineral_counts.values())} 张图片")

def main():
    """主函数"""
    print("=" * 60)
    print("矿物图片整理工具")
    print("=" * 60)
    
    images_dir = "../Images/Minerals"
    
    # 整理重复图片
    organize_duplicate_images(images_dir)
    
    # 创建汇总报告
    create_mineral_summary()
    
    print("\n" + "=" * 60)
    print("图片整理完成!")
    print("=" * 60)

if __name__ == "__main__":
    main()