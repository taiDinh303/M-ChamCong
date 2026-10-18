"""Validate maintained Markdown without dependencies or writes to the repository."""
from pathlib import Path
import json
import re
import sys
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
FILES = sorted(set(list((ROOT / "docs").glob("*.md"))
                   + list((ROOT / "figma").glob("*.md"))
                   + list((ROOT / "marixa_react").glob("*.md"))
                   + list((ROOT / "marixa_aspnet/tools").glob("*/README.md"))
                   + [ROOT / "AGENTS.md"]
                   + [ROOT / "marixa_aspnet" / name for name in ("README.md", "ARCHITECTURE.md", "BACKEND_REVIEW.md", "AGENTS.md")]
                   + [ROOT / "marixa_aspnet/supabase/README.md"]))


def slug(heading):
    heading = re.sub(r"\[([^]]+)\]\([^)]*\)", r"\1", heading).replace("`", "").lower()
    return re.sub(r"[^\w\- ]", "", heading).replace(" ", "-")


def headings(path):
    in_fence = False
    output = []
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("```"):
            in_fence = not in_fence
        elif not in_fence and re.match(r"^#{1,6} ", line):
            output.append(slug(line.split(" ", 1)[1]))
    return output


errors = []
links = anchors = json_blocks = 0
for path in FILES:
    name = path.relative_to(ROOT).as_posix()
    try:
        content = path.read_text(encoding="utf-8-sig")
    except (OSError, UnicodeError) as error:
        errors.append(f"{name}: {error}")
        continue
    if "\ufffd" in content or any(ord(c) < 32 and c not in "\r\n\t" for c in content):
        errors.append(f"{name}: invalid/replacement/control character")
    if re.search(r"\{\{[^}]+\}\}|CHƯA ĐIỀN|YOUR_PROJECT|TÊN DỰ ÁN MẪU", content):
        errors.append(f"{name}: unfilled template marker")
    fence = None
    block = []
    outside = []
    h1 = 0
    previous_table = None
    for line_no, line in enumerate(content.splitlines(), 1):
        if line.startswith("```"):
            if fence is None:
                fence, block = line[3:].strip(), []
            else:
                if fence == "json":
                    json_blocks += 1
                    try:
                        json.loads("\n".join(block))
                    except ValueError as error:
                        errors.append(f"{name}:{line_no}: invalid JSON: {error}")
                fence = None
            continue
        if fence is not None:
            block.append(line)
            continue
        outside.append(line)
        h1 += line.startswith("# ")
        if line.startswith("|"):
            columns = len(re.split(r"(?<!\\)\|", line)) - 2
            if previous_table and previous_table[0] == line_no - 1 and previous_table[1] != columns:
                errors.append(f"{name}:{line_no}: inconsistent table columns")
            previous_table = line_no, columns
    if fence is not None:
        errors.append(f"{name}: unclosed fence")
    if h1 != 1:
        errors.append(f"{name}: expected one H1, got {h1}")
    for target in re.findall(r"\[[^\]\n]*\]\(([^)]+)\)", "\n".join(outside)):
        if re.match(r"^(https?|mailto|app|codex):", target):
            continue
        target = unquote(target.strip("<>"))
        relative, _, anchor = target.partition("#")
        resolved = (path.parent / relative).resolve() if relative else path
        links += 1
        if not resolved.exists():
            errors.append(f"{name}: missing link {target}")
        elif anchor and resolved.suffix.lower() == ".md":
            anchors += 1
            if anchor not in headings(resolved):
                errors.append(f"{name}: missing anchor {target}")

print(json.dumps({"files": len(FILES), "links": links, "anchors": anchors, "json_blocks": json_blocks, "errors": errors}, ensure_ascii=True, indent=2))
sys.exit(1 if errors else 0)
