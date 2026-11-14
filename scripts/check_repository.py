"""Check local Markdown targets and keep player binaries out of the source repo."""
from pathlib import Path
import re
import subprocess
import sys

root = Path(__file__).resolve().parent.parent
tracked = subprocess.check_output(["git", "ls-files", "-z"], cwd=root).decode().split("\0")
errors = []
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
print("Documentation targets and source-only repository checks passed.")
