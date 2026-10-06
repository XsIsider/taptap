"""Extract the supplied self-contained prototype assets. Requires Pillow."""
import base64
import io
import re
import sys
from pathlib import Path
from PIL import Image

source = Path(sys.argv[1]).read_text(encoding='utf-8-sig')
target = Path(__file__).resolve().parents[1] / 'Assets/Castle/Resources/Castle'
target.mkdir(parents=True, exist_ok=True)
names = ['Chinese', 'ControlRoom', 'Castle', 'Outside', 'Lounge', 'Held',
         'Invitation', 'Collected', 'PortraitSource', 'ItemSource', 'JournalSource',
         'Floor1', 'Floor2', 'Title', 'EmptyRoom']
matches = list(re.finditer(r'data:([^;,]+);base64,([A-Za-z0-9+/=]+)', source))
for name, match in zip(names, matches):
    if name in ('PortraitSource', 'ItemSource', 'JournalSource'):
        continue
    data = base64.b64decode(match[2])
    if match[1].startswith('font'):
        (target / (name + '.otf')).write_bytes(data)
    else:
        im = Image.open(io.BytesIO(data)).convert('RGB')
        im.save(target / (name + '.jpg'), quality=93)
        print(name, im.size)
print('Extracted to', target)
