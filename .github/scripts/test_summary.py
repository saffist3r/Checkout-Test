"""Turns the TRX and Cobertura files from `dotnet test` into a Markdown job summary."""
import glob
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
results_dir = sys.argv[1]

trx_files = glob.glob(f"{results_dir}/**/*.trx", recursive=True)
if not trx_files:
    print("## ❌ Tests did not run\n\nNo test results were produced. Check the Build step.")
    sys.exit(0)

totals = defaultdict(int)
by_class = defaultdict(lambda: defaultdict(int))
failures = []
duration = 0.0

for path in trx_files:
    root = ET.parse(path).getroot()
    classes = {
        d.get("id"): d.find("t:TestMethod", NS).get("className").split(".")[-1]
        for d in root.iterfind(".//t:UnitTest", NS)
    }
    for r in root.iterfind(".//t:UnitTestResult", NS):
        outcome = r.get("outcome")
        cls = classes.get(r.get("testId"), "?")
        totals[outcome] += 1
        by_class[cls][outcome] += 1
        h, m, s = r.get("duration", "0:0:0").split(":")
        duration += int(h) * 3600 + int(m) * 60 + float(s)
        if outcome == "Failed":
            message = r.findtext(".//t:Message", default="", namespaces=NS).strip()
            failures.append((r.get("testName"), message.splitlines()[0] if message else ""))

passed, failed = totals["Passed"], totals["Failed"]
skipped = sum(totals.values()) - passed - failed
icon = "✅" if failed == 0 else "❌"

print(f"## {icon} {passed} passed, {failed} failed, {skipped} skipped")
print(f"\n{sum(totals.values())} tests in {duration:.1f}s\n")

coverage = glob.glob(f"{results_dir}/**/coverage.cobertura.xml", recursive=True)
if coverage:
    rates = [float(ET.parse(c).getroot().get("line-rate", 0)) for c in coverage]
    print(f"**Line coverage:** {max(rates) * 100:.1f}%\n")

print("| Test class | Passed | Failed | Skipped |")
print("|---|---:|---:|---:|")
for cls in sorted(by_class):
    c = by_class[cls]
    other = sum(c.values()) - c["Passed"] - c["Failed"]
    print(f"| {'✅' if c['Failed'] == 0 else '❌'} `{cls}` | {c['Passed']} | {c['Failed']} | {other} |")

if failures:
    print("\n### Failed tests\n")
    for name, message in failures:
        print(f"- `{name}`: {message}")
