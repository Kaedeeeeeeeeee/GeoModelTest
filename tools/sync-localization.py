#!/usr/bin/env python3
"""Generate editor copies from Resources/Localization/Data. Use --check in validation."""
import argparse
import json
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--check', action='store_true')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1] / 'Assets'
for source in sorted((root / 'Resources/Localization/Data').glob('*.json')):
    rows = json.loads(source.read_text())['texts']
    keys = [row['key'] for row in rows]
    assert len(keys) == len(set(keys)), f'Duplicate keys in {source}'
    assert all(row['key'] for row in rows), source
    destination = root / 'Scripts/Localization/Data' / source.name
    if args.check:
        assert destination.read_bytes() == source.read_bytes(), f'Out-of-date copy: {destination}'
    else:
        destination.write_bytes(source.read_bytes())
    print(f'{source.name}: {len(keys)} unique entries')
