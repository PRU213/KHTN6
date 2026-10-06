import re
import sys
import random

def parse_unity_yaml(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Split by the document separator
    blocks = re.split(r'^(?=--- !u!)', content, flags=re.MULTILINE)
    
    return blocks

def find_block_with_name(blocks, name):
    for block in blocks:
        if block.startswith("--- !u!1 ") and f"m_Name: {name}" in block:
            return block
    return None

def get_file_id(block):
    match = re.search(r'^--- !u!\d+ &(\d+)', block)
    if match:
        return match.group(1)
    return None

def get_component_ids(gameobject_block):
    ids = []
    for match in re.finditer(r'- component: \{fileID: (\d+)\}', gameobject_block):
        ids.append(match.group(1))
    return ids

source_blocks = parse_unity_yaml(r"d:\University\7thSemester\KHTN6\Assets\Scenes\Biology\bean_1.unity")
btn_go_block = find_block_with_name(source_blocks, "btnTamDung")
if not btn_go_block:
    print("Could not find btnTamDung in source")
    sys.exit(1)

btn_go_id = get_file_id(btn_go_block)
comp_ids = get_component_ids(btn_go_block)

print(f"btnTamDung GameObject ID: {btn_go_id}")
print(f"Component IDs: {comp_ids}")

blocks_to_copy = [btn_go_block]
for block in source_blocks:
    fid = get_file_id(block)
    if fid in comp_ids:
        blocks_to_copy.append(block)

print(f"Total blocks to copy: {len(blocks_to_copy)}")
