"""Extract UI crops from the supplied HTML; coordinates refer to its embedded images."""
import base64
import io
import re
import uuid
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
source = next((root / "Assets/Docs").glob("* (3).html")).read_text(encoding="utf-8-sig")
assets = list(re.finditer(r"data:([^;,]+);base64,([A-Za-z0-9+/=]+)", source))
target = root / "Assets/Castle/Resources/Castle/UiArt"
target.mkdir(exist_ok=True)
# The web prototype reuses full scene images as CSS backgrounds/crops.
crops = {
    "PortraitLady": (8, (100, 65, 520, 690)),
    "PortraitCountess": (8, (535, 55, 1030, 710)),
    "InvitationItem": (9, (285, 290, 810, 665)),
    "JournalBackdrop": (10, None),
    "MenuPrimary": (13, (693, 344, 980, 405)),
    "MenuSecondary": (13, (693, 412, 980, 480)),
}
for name, (index, bounds) in crops.items():
    image = Image.open(io.BytesIO(base64.b64decode(assets[index][2]))).convert("RGB")
    if bounds:
        image = image.crop(bounds)
    # Remove baked menu labels using the adjacent texture from the same button.
    if name.startswith("Menu"):
        patch = image.crop((32, 14, 65, image.height - 6))
        image.paste(patch.resize((image.width - 64, image.height - 20)), (32, 14))
    path = target / (name + ".png")
    image.save(path)
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        meta.write_text("fileFormatVersion: 2\nguid: " + uuid.uuid4().hex + "\n", encoding="utf-8")
folder_meta = Path(str(target) + ".meta")
if not folder_meta.exists():
    folder_meta.write_text("fileFormatVersion: 2\nguid: " + uuid.uuid4().hex + "\nfolderAsset: yes\n", encoding="utf-8")
