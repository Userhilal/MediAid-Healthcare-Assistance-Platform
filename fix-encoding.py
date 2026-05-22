from pathlib import Path
import re
import sys

ROOT = Path.cwd()
SERVER = ROOT / "server"
EXTENSIONS = {".cshtml", ".html", ".cs", ".js", ".css"}

def is_target(path: Path) -> bool:
    lower = str(path).lower()
    return path.suffix.lower() in EXTENSIONS and "\\bin\\" not in lower and "\\obj\\" not in lower

files = [p for p in SERVER.rglob("*") if p.is_file() and is_target(p)]

bad_token = re.compile(r"[^\s<>\[\]{}()\"']*[\u00c2\u00c3\u00e2\u00ef\u00f0\ufffd][^\s<>\[\]{}()\"']*")

def repair_token(match):
    value = match.group(0)
    try:
        repaired = value.encode("cp1252", errors="ignore").decode("utf-8", errors="ignore")
        return repaired if repaired.strip() else value
    except Exception:
        return value

direct = {
    "\u00c3\u0160": "Ê",
    "\u00c3\u008a": "Ê",
    "\u00c3\u02c6": "È",
    "\u00c3\u0088": "È",
    "\u00c3\u2030": "É",
    "\u00c3\u00a9": "é",
    "\u00c3\u00a8": "è",
    "\u00c3\u00aa": "ê",
    "\u00c3\u00a0": "à",
    "\u00c3\u00a7": "ç",
    "\u00e2\u20ac\u201d": "-",
    "\u00e2\u20ac\u201c": "-",
    "\u00e2\u20ac\u2122": "'",
    "\u00e2\u20ac\u0153": '"',
    "\u00e2\u20ac\u009d": '"',
    "\u00e2\u20ac\u00a6": "...",
    "\u00e2\u0153\u00a8": "",
    "\u00e2\u0161\u00a0": "Attention",
    "\u00e2\u0161\u00a0\u00ef\u00b8": "Attention",
    "\u00ef\u00b8": "",
    "\u00e2\u0192\u00a3": "",
    "\u00e2\u20ac": "",
    "\u00f0\u0178\u2018\u00a8": "Dr",
    "\u00f0\u0178\u2018\u00a9": "Dr",
    "\u00f0\u0178": "",
    "\ufffd": ""
}

changed = []

for path in files:
    original = path.read_text(encoding="utf-8", errors="replace")
    text = original

    for _ in range(3):
        text = bad_token.sub(repair_token, text)

    for bad, good in direct.items():
        text = text.replace(bad, good)

    text = text.replace("⚠️", "Attention")
    text = text.replace("⚠", "Attention")
    text = text.replace("✨", "")
    text = text.replace("—", "-")

    text = text.replace("1️⃣", "1")
    text = text.replace("2️⃣", "2")
    text = text.replace("3️⃣", "3")
    text = text.replace("4️⃣", "4")
    text = text.replace("5️⃣", "5")
    text = text.replace("6️⃣", "6")
    text = text.replace("7️⃣", "7")
    text = text.replace("8️⃣", "8")
    text = text.replace("9️⃣", "9")

    text = text.replace("👨‍⚕️", "Dr")
    text = text.replace("👩‍⚕️", "Dr")
    text = text.replace("👨", "Dr")
    text = text.replace("👩", "Dr")

    text = text.replace("\u00c3", "")
    text = text.replace("\u00c2", "")
    text = text.replace("\u00e2", "")
    text = text.replace("\ufffd", "")

    if text != original:
        path.write_text(text, encoding="utf-8", newline="")
        changed.append(path)

bad_pattern = re.compile(r"\u00c3|\u00c2|\u00e2|\ufffd")
remaining = []

for path in files:
    text = path.read_text(encoding="utf-8", errors="replace")
    for line_no, line in enumerate(text.splitlines(), start=1):
        if bad_pattern.search(line):
            remaining.append((path, line_no, line))
            if len(remaining) >= 40:
                break
    if len(remaining) >= 40:
        break

print(f"Changed files: {len(changed)}")

if remaining:
    print("Encoding issues still found:")
    for path, line_no, line in remaining:
        print(f"{path}:{line_no}: {line}")
    sys.exit(1)

print("Encoding fixed.")
