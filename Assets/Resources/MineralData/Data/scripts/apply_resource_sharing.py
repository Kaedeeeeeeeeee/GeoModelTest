#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
应用资源共享逻辑
确保相同矿物只保留一张图片，实现真正的资源共享
"""

import os
import shutil
from collections import defaultdict

def apply_resource_sharing_logic(source_dir, target_dir):
    """应用资源共享逻辑"""
    print(f"从 {source_dir} 应用资源共享到 {target_dir}")
    
    os.makedirs(target_dir, exist_ok=True)
    
    if not os.path.exists(source_dir):
        print(f"源目录不存在: {source_dir}")
        return
    
    # 获取所有图片文件
    all_files = []
    for file in os.listdir(source_dir):
        if file.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp')):
            all_files.append(file)
    
    print(f"找到 {len(all_files)} 个图片文件")
    
    # 按矿物ID分组
    mineral_groups = defaultdict(list)
    for file in all_files:
        if '_' in file:
            # 提取矿物ID (文件名中最后一个_之前的部分)
            base_name = file.rsplit('.', 1)[0]  # 移除扩展名
            if '_' in base_name:
                mineral_id = base_name.rsplit('_', 1)[0]  # 移除数字部分
                mineral_groups[mineral_id].append(file)
    
    print(f"发现 {len(mineral_groups)} 种不同矿物")
    
    # 为每种矿物选择最佳图片（优先选择_001，然后按质量选择）
    selected_files = {}
    
    for mineral_id, files in mineral_groups.items():
        print(f"\n处理矿物: {mineral_id} ({len(files)} 个文件)")
        
        # 优先级策略
        best_file = None
        
        # 1. 优先选择 _001 版本
        for file in files:
            if '_001.' in file:
                best_file = file
                print(f"  选择 _001 版本: {file}")
                break
        
        # 2. 如果没有_001，选择文件大小最大的（通常质量更好）
        if not best_file:
            files_with_size = []
            for file in files:
                file_path = os.path.join(source_dir, file)
                file_size = os.path.getsize(file_path)
                files_with_size.append((file, file_size))
            
            # 按文件大小排序，选择最大的
            files_with_size.sort(key=lambda x: x[1], reverse=True)
            best_file = files_with_size[0][0]
            print(f"  选择最大文件: {best_file} ({files_with_size[0][1]/1024:.1f} KB)")
        
        # 生成标准文件名
        _, ext = os.path.splitext(best_file)
        standard_filename = f"{mineral_id}_001{ext.lower()}"
        
        selected_files[mineral_id] = {
            'source_file': best_file,
            'target_file': standard_filename
        }
        
        print(f"  标准文件名: {standard_filename}")
    
    # 复制选定的文件到目标目录
    print(f"\n=== 复制文件到目标目录 ===")
    copied_count = 0
    
    for mineral_id, file_info in selected_files.items():
        source_path = os.path.join(source_dir, file_info['source_file'])
        target_path = os.path.join(target_dir, file_info['target_file'])
        
        try:
            shutil.copy2(source_path, target_path)
            print(f"✓ {file_info['source_file']:30} -> {file_info['target_file']}")
            copied_count += 1
        except Exception as e:
            print(f"✗ 复制失败 {file_info['source_file']}: {e}")
    
    print(f"\n=== 资源共享完成 ===")
    print(f"总矿物种类: {len(mineral_groups)}")
    print(f"成功复制: {copied_count} 个文件")
    print(f"资源节省: {sum(len(files) for files in mineral_groups.values()) - copied_count} 个重复文件")
    
    # 显示最终结果
    final_files = [f for f in os.listdir(target_dir) if f.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp'))]
    print(f"\n最终图片文件 ({len(final_files)} 个):")
    for file in sorted(final_files):
        print(f"  - {file}")

def create_resource_mapping_summary():
    """创建资源映射汇总"""
    target_dir = "../Images/Minerals"
    
    if not os.path.exists(target_dir):
        print(f"目标目录不存在: {target_dir}")
        return
    
    print(f"\n=== 资源映射汇总 ===")
    
    # 统计最终的图片文件
    mineral_files = {}
    
    for file in os.listdir(target_dir):
        if file.lower().endswith(('.jpg', '.jpeg', '.png', '.gif', '.bmp')):
            base_name = file.rsplit('.', 1)[0]
            if '_001' in base_name:
                mineral_id = base_name.replace('_001', '')
                mineral_files[mineral_id] = file
    
    print(f"可用的矿物图片资源 ({len(mineral_files)} 种):")
    for mineral_id in sorted(mineral_files.keys()):
        file = mineral_files[mineral_id]
        print(f"  {mineral_id:25} : {file}")
    
    # 生成资源映射代码
    print(f"\n=== Unity资源映射代码 ===")
    print("// 矿物图片资源映射")
    print("private static Dictionary<string, string> mineralImageMap = new()")
    print("{")
    for mineral_id in sorted(mineral_files.keys()):
        file = mineral_files[mineral_id]
        print(f'    {{"{mineral_id}", "{file}"}},')
    print("};")

def main():
    """主函数"""
    print("=" * 80)
    print("矿物资源共享应用工具")
    print("=" * 80)
    
    source_dir = "../Images/Minerals_Complete"
    target_dir = "../Images/Minerals"
    
    # 应用资源共享逻辑
    apply_resource_sharing_logic(source_dir, target_dir)
    
    # 创建资源映射汇总
    create_resource_mapping_summary()
    
    print("\n" + "=" * 80)
    print("资源共享应用完成!")
    print("所有相同矿物现在共享同一张图片文件")
    print("=" * 80)

if __name__ == "__main__":
    main()