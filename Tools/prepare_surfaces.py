"""Pack linear surface maps for Unity URP. Source files remain unchanged."""
from pathlib import Path
from PIL import Image, ImageOps

root = Path(__file__).resolve().parents[1] / "GameKayky2026/Assets/_Game/Art/Surfaces"
for folder in root.iterdir():
    if not folder.is_dir():
        continue
    roughness = Image.open(next(folder.glob("*_Roughness.jpg"))).convert("L")
    smoothness = ImageOps.invert(roughness)
    black = Image.new("L", roughness.size, 0)
    ao = Image.open(next(folder.glob("*_AmbientOcclusion.jpg"))).convert("L") if list(folder.glob("*_AmbientOcclusion.jpg")) else Image.new("L", roughness.size, 255)
    Image.merge("RGBA", (black, black, black, smoothness)).save(folder / "MetallicSmoothness.png")
    Image.merge("RGBA", (black, ao, black, smoothness)).save(folder / "TerrainMask.png")
print("Prepared twelve URP surface sets")
