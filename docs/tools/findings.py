"""
Audit findings register tooling.

    python docs/tools/findings.py summary     # counts by severity and status
    python docs/tools/findings.py table       # Markdown status table for an addendum
    python docs/tools/findings.py open        # open / partially fixed items only

The register (docs/audits/findings-register.json) is the single source of truth for finding IDs
and their status across audits. Each new audit appends an entry to "audits" and a history entry
to every finding it re-checks, so trends can be compared audit to audit.
"""
from __future__ import annotations

import json
import sys
from collections import Counter
from pathlib import Path

REGISTER = Path(__file__).resolve().parents[2] / "docs" / "audits" / "findings-register.json"
SEVERITIES = ["Critical", "High", "Medium", "Low"]
PILL = {"Fixed": "fixed", "Partially fixed": "partial", "Open": "open", "Accepted risk": "na", "Not applicable": "na"}


def load() -> dict:
    return json.loads(REGISTER.read_text(encoding="utf-8"))


def summary(reg: dict) -> str:
    findings = reg["findings"]
    lines = [f"{len(findings)} findings"]
    for sev in SEVERITIES:
        counts = Counter(f["currentStatus"] for f in findings if f["severity"] == sev)
        lines.append(f"{sev}: " + ", ".join(f"{k} {v}" for k, v in sorted(counts.items())))
    return "\n".join(lines)


def table(reg: dict, only_open: bool = False) -> str:
    rows = ["| ID | Finding | Severity | Baseline ref | Status | Evidence / verification | Remaining action |",
            "|---|---|---|---|---|---|---|"]
    order = {s: i for i, s in enumerate(SEVERITIES)}
    for f in sorted(reg["findings"], key=lambda f: (order[f["severity"]], f["id"])):
        if only_open and f["currentStatus"] == "Fixed":
            continue
        latest = f["history"][-1]
        ref = "New" if f["baselineReference"] == "new" else f["baselineReference"]
        pill = f'<span class="pill {PILL.get(f["currentStatus"], "na")}">{f["currentStatus"]}</span>'
        evidence = latest.get("evidence", "").replace("|", "\\|")
        remaining = (f.get("remainingAction") or "—").replace("|", "\\|")
        rows.append(f"| {f['id']} | {f['title']} | {f['severity']} | {ref} | {pill} | {evidence} | {remaining} |")
    return "\n".join(rows)


if __name__ == "__main__":
    register = load()
    command = sys.argv[1] if len(sys.argv) > 1 else "summary"
    if command == "table":
        print(table(register))
    elif command == "open":
        print(table(register, only_open=True))
    else:
        print(summary(register))
