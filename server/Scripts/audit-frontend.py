from pathlib import Path
from datetime import datetime

ROOT = Path.cwd()
SERVER = ROOT / "server"
DOCS = ROOT / "docs"
DOCS.mkdir(exist_ok=True)

views_dir = SERVER / "Views"
controllers_dir = SERVER / "Controllers"
services_dir = SERVER / "Services"

patterns = [
    "demo",
    "fake",
    "dummy",
    "lorem",
    "placeholder",
    "hardcoded",
    "coming soon",
    "sample",
    "test@example",
    "admin@example",
    "expert@example",
    "TODO",
    "FIXME"
]

def collect_files(base, suffix):
    if not base.exists():
        return []
    return sorted([p for p in base.rglob("*") if p.is_file() and p.suffix.lower() == suffix])

views = collect_files(views_dir, ".cshtml")
controllers = collect_files(controllers_dir, ".cs")
services = collect_files(services_dir, ".cs")

report = []
report.append("# MediAid Frontend and Static Content Audit")
report.append("")
report.append(f"Generated on: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
report.append("")
report.append("This audit identifies UI pages and possible demo/static content that should be reviewed.")
report.append("A finding does not always mean the content is wrong. It only means it should be checked.")
report.append("")

report.append("## Page Inventory")
report.append("")

if views:
    for view in views:
        relative = str(view.relative_to(ROOT)).replace("\\", "/")
        report.append(f"- `{relative}`")
else:
    report.append("No Razor views found.")

report.append("")
report.append("## Suspicious Static or Demo Content")
report.append("")

all_files = views + controllers + services
findings = []

for file in all_files:
    try:
        lines = file.read_text(encoding="utf-8", errors="replace").splitlines()
    except Exception:
        continue

    for line_number, line in enumerate(lines, start=1):
        lower_line = line.lower()
        for pattern in patterns:
            if pattern.lower() in lower_line:
                relative = str(file.relative_to(ROOT)).replace("\\", "/")
                cleaned = line.strip().replace("|", "\\|")
                findings.append((relative, line_number, pattern, cleaned))

if findings:
    for relative, line_number, pattern, cleaned in findings:
        report.append(f"- `{relative}` line {line_number} - pattern `{pattern}` - {cleaned}")
else:
    report.append("No obvious demo/static placeholders found.")

report.append("")
report.append("## Frontend Consistency Notes")
report.append("")
report.append("- Global UI polish layer exists in `server/wwwroot/css/frontend-polish.css`.")
report.append("- No fake business data was added by this audit.")
report.append("- Dynamic data should remain connected to controllers, services, and MongoDB.")
report.append("- Demo-only or misleading text should be replaced with real workflow explanations.")

(DOCS / "FRONTEND_AUDIT.md").write_text("\n".join(report), encoding="utf-8")
print("Frontend audit written to docs/FRONTEND_AUDIT.md")
