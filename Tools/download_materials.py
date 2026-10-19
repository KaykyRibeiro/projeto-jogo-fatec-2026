"""Download the twelve CC0 surface sets used by the course material library."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import io
import json
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DEST = ROOT / "GameKayky2026/Assets/_Game/Art/Surfaces"
ASSETS = ["Grass001", "Grass004", "Grass005", "Ground037", "Ground048", "Ground054",
          "Rock030", "Rock035", "Rock051", "Gravel021", "Gravel025", "Gravel027"]

def fetch(asset):
    folder = DEST / asset
    folder.mkdir(parents=True, exist_ok=True)
    if list(folder.glob("*_Color.jpg")):
        return {"id": asset, "source": f"https://ambientcg.com/view?id={asset}", "license": "CC0-1.0"}
    request = urllib.request.Request(f"https://ambientcg.com/get?file={asset}_1K-JPG.zip", headers={"User-Agent": "UnityCourseProject/1.0"})
    with urllib.request.urlopen(request, timeout=60) as response:
        data = response.read()
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        for entry in archive.infolist():
            if entry.filename.endswith(("_Color.jpg", "_NormalGL.jpg", "_Roughness.jpg", "_AmbientOcclusion.jpg")):
                (folder / Path(entry.filename).name).write_bytes(archive.read(entry))
    print(asset, "ready", flush=True)
    return {"id": asset, "source": f"https://ambientcg.com/view?id={asset}", "license": "CC0-1.0"}

if __name__ == "__main__":
    with ThreadPoolExecutor(max_workers=4) as pool:
        records = list(pool.map(fetch, ASSETS))
    (DEST / "Sources.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
