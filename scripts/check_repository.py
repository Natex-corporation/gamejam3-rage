"""Check local Markdown targets and keep player binaries out of the source repo."""
from pathlib import Path
import json
import re
import subprocess
import sys

root = Path(__file__).resolve().parent.parent
tracked = subprocess.check_output(["git", "ls-files", "-z"], cwd=root).decode().split("\0")
errors = []
manifest = json.loads((root / "demo-manifest.json").read_text(encoding="utf-8"))
metadata = json.loads((root / "codemeta.json").read_text(encoding="utf-8"))
for field in ("name", "version", "repository", "download", "developer_kit", "checksums", "platform", "entrypoint", "source_license"):
    if not manifest.get(field):
        errors.append(f"Missing demo manifest field: {field}")
for field in ("agent_guide", "play_guide", "build_guide"):
    if not (root / manifest.get(field, "missing")).is_file():
        errors.append(f"Missing guide: {field}")
if manifest.get("version") != metadata.get("softwareVersion"):
    errors.append("Demo and CodeMeta versions disagree")
if manifest.get("entrypoint") != "Rage-Clay-Demo/Rage Demo.exe":
    errors.append("Manifest entrypoint does not match the release package")
if manifest.get("districts") != 6 or manifest.get("permanent_upgrades") != 8:
    errors.append("Manifest content counts disagree with the demo")
if "demo-manifest.json" not in (root / "llms.txt").read_text(encoding="utf-8"):
    errors.append("Agent index is missing the demo manifest")
for name in filter(None, tracked):
    path = root / name
    if path.suffix.lower() in {".dll", ".exe", ".zip", ".nupkg"}:
        errors.append(f"Binary dependency or output tracked: {name}")
    if path.suffix == ".md":
        for target in re.findall(r"!?\[[^\]]*\]\(([^\s)]+)\)", path.read_text(encoding="utf-8")):
            if "://" in target or target.startswith("#"):
                continue
            if not (path.parent / target.split("#", 1)[0]).exists():
                errors.append(f"Broken link in {name}: {target}")
if errors:
    print("\n".join(errors), file=sys.stderr)
    sys.exit(1)
print("Documentation, discovery metadata, and source-only repository checks passed.")
