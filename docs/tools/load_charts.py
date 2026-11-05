"""
Draws the load/stress test charts used in the Part 3 report from the files written by
GiftOfTheGivers.LoadTests (summary.json, timeline.csv). Standard library only.

    python docs/tools/load_charts.py

Reads   docs/evidence/part3/stress-before, stress-after
Writes  docs/deliverables/assets/charts/*.svg

Static SVG for print: light surface, two-series palette validated for colour-vision
deficiency (blue #2a78d6 = before, orange #eb6834 = after), legend plus selective labels.
"""
from __future__ import annotations

import csv
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
EVIDENCE = ROOT / "docs" / "evidence" / "part3"
OUT = ROOT / "docs" / "deliverables" / "assets" / "charts"

SURFACE, INK, INK2, MUTED, GRID, AXIS = "#fcfcfb", "#0b0b0b", "#52514e", "#898781", "#e1e0d9", "#c3c2b7"
BEFORE, AFTER = "#2a78d6", "#eb6834"
FONT = "font-family='Inter, Segoe UI, Arial, sans-serif'"


def load(name: str) -> dict:
    return json.loads((EVIDENCE / name / "summary.json").read_text(encoding="utf-8"))


def nice_max(value: float) -> float:
    for power in range(0, 7):
        for step in (1, 2, 2.5, 5):
            candidate = step * 10 ** power
            if candidate >= value:
                return candidate
    return value


def frame(width: int, height: int, title: str, subtitle: str) -> list[str]:
    return [
        f"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}' width='{width}' height='{height}' {FONT}>",
        f"<rect width='{width}' height='{height}' fill='{SURFACE}'/>",
        f"<text x='16' y='26' font-size='15' font-weight='600' fill='{INK}'>{title}</text>",
        f"<text x='16' y='46' font-size='12' fill='{INK2}'>{subtitle}</text>",
    ]


def legend(parts: list[str], x: int, y: int, items: list[tuple[str, str]]) -> None:
    for label, colour in items:
        parts.append(f"<rect x='{x}' y='{y - 9}' width='10' height='10' rx='2' fill='{colour}'/>")
        parts.append(f"<text x='{x + 15}' y='{y}' font-size='12' fill='{INK2}'>{label}</text>")
        x += 30 + 7 * len(label)


def y_axis(parts, left, right, top, bottom, ymax, unit, ticks=4):
    for i in range(ticks + 1):
        value = ymax * i / ticks
        y = bottom - (bottom - top) * i / ticks
        parts.append(f"<line x1='{left}' x2='{right}' y1='{y:.1f}' y2='{y:.1f}' stroke='{GRID if i else AXIS}' stroke-width='1'/>")
        parts.append(f"<text x='{left - 6}' y='{y + 4:.1f}' font-size='11' fill='{MUTED}' text-anchor='end'>{value:,.0f}{unit}</text>")


def grouped_bars(path: Path, title: str, subtitle: str, groups: list[str], before: list[float], after: list[float],
                 unit: str, xlabel: str) -> None:
    width, height = 720, 340
    left, right, top, bottom = 70, width - 20, 80, height - 50
    ymax = nice_max(max(before + after) * 1.1)
    parts = frame(width, height, title, subtitle)
    legend(parts, left, 68, [("Before mitigation", BEFORE), ("After mitigation", AFTER)])
    y_axis(parts, left, right, top, bottom, ymax, unit)
    slot = (right - left) / len(groups)
    bar = min(28, slot / 3)
    for i, group in enumerate(groups):
        centre = left + slot * (i + 0.5)
        for offset, value, colour in ((-bar / 2 - 1, before[i], BEFORE), (bar / 2 + 1, after[i], AFTER)):
            h = (bottom - top) * value / ymax
            x = centre + offset - bar / 2
            # 4px rounded top, square base on the axis.
            parts.append(f"<path d='M{x:.1f},{bottom} v{-max(h - 4, 0):.1f} q0,-4 4,-4 h{bar - 8:.1f} q4,0 4,4 v{max(h - 4, 0):.1f} z' fill='{colour}'>"
                         f"<title>{group}: {value:,.0f}{unit}</title></path>")
            parts.append(f"<text x='{x + bar / 2:.1f}' y='{bottom - h - 5:.1f}' font-size='10' fill='{INK2}' text-anchor='middle'>{value:,.0f}</text>")
        parts.append(f"<text x='{centre:.1f}' y='{bottom + 18}' font-size='11' fill='{MUTED}' text-anchor='middle'>{group}</text>")
    parts.append(f"<text x='{(left + right) / 2:.0f}' y='{height - 12}' font-size='11' fill='{MUTED}' text-anchor='middle'>{xlabel}</text>")
    parts.append("</svg>")
    path.write_text("\n".join(parts), encoding="utf-8")


def lines(path: Path, title: str, subtitle: str, xs: list[float], series: list[tuple[str, str, list[float]]],
          unit: str, xlabel: str, annotations: list[tuple[float, str]] | None = None, xunit: str = "") -> None:
    width, height = 720, 340
    left, right, top, bottom = 70, width - 20, 80, height - 50
    ymax = nice_max(max(max(values) for _, _, values in series) * 1.1)
    xmin, xmax = min(xs), max(xs)
    parts = frame(width, height, title, subtitle)
    if len(series) > 1:
        legend(parts, left, 68, [(label, colour) for label, colour, _ in series])
    y_axis(parts, left, right, top, bottom, ymax, unit)

    def sx(x):
        return left + (right - left) * (x - xmin) / ((xmax - xmin) or 1)

    def sy(y):
        return bottom - (bottom - top) * y / ymax

    for x, label in annotations or []:
        parts.append(f"<line x1='{sx(x):.1f}' x2='{sx(x):.1f}' y1='{top}' y2='{bottom}' stroke='{GRID}' stroke-dasharray='3 3'/>")
        parts.append(f"<text x='{sx(x) + 3:.1f}' y='{top + 10}' font-size='10' fill='{MUTED}'>{label}</text>")
    for label, colour, values in series:
        points = " ".join(f"{sx(x):.1f},{sy(y):.1f}" for x, y in zip(xs, values))
        parts.append(f"<polyline points='{points}' fill='none' stroke='{colour}' stroke-width='2' stroke-linejoin='round'/>")
        if len(xs) <= 12:
            for x, y in zip(xs, values):
                parts.append(f"<circle cx='{sx(x):.1f}' cy='{sy(y):.1f}' r='4' fill='{colour}' stroke='{SURFACE}' stroke-width='2'>"
                             f"<title>{label}: {y:,.0f}{unit} at {x:g}{xunit}</title></circle>")
        # Direct label at the line end.
        parts.append(f"<text x='{sx(xs[-1]) - 4:.1f}' y='{sy(values[-1]) - 8:.1f}' font-size='11' fill='{INK2}' text-anchor='end'>{values[-1]:,.0f}{unit}</text>")
    ticks = xs if len(xs) <= 12 else [xs[int(i * (len(xs) - 1) / 6)] for i in range(7)]
    for x in ticks:
        parts.append(f"<text x='{sx(x):.1f}' y='{bottom + 18}' font-size='11' fill='{MUTED}' text-anchor='middle'>{x:g}{xunit}</text>")
    parts.append(f"<text x='{(left + right) / 2:.0f}' y='{height - 12}' font-size='11' fill='{MUTED}' text-anchor='middle'>{xlabel}</text>")
    parts.append("</svg>")
    path.write_text("\n".join(parts), encoding="utf-8")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    before, after = load("stress-before"), load("stress-after")
    users = [str(s["Users"]) for s in before["Stages"]]

    grouped_bars(OUT / "stress-p95.svg", "Stress test — 95th percentile response time per stage",
                 "Lower is better. Each stage held for 20 s with no think time (local Release build, SQL Server LocalDB).",
                 users, [s["P95"] for s in before["Stages"]], [s["P95"] for s in after["Stages"]],
                 " ms", "Concurrent virtual users")

    lines(OUT / "stress-throughput.svg", "Stress test — throughput per stage",
          "Requests completed per second. Throughput peaks, then falls as the server saturates.",
          [s["Users"] for s in before["Stages"]],
          [("Before mitigation", BEFORE, [s["Throughput"] for s in before["Stages"]]),
           ("After mitigation", AFTER, [s["Throughput"] for s in after["Stages"]])],
          "", "Concurrent virtual users")

    lines(OUT / "stress-errors.svg", "Stress test — error rate per stage",
          "Requests that timed out (10 s) or returned an HTTP error, as a percentage of all requests in the stage.",
          [s["Users"] for s in before["Stages"]],
          [("Before mitigation", BEFORE, [s["ErrorRate"] * 100 for s in before["Stages"]]),
           ("After mitigation", AFTER, [s["ErrorRate"] * 100 for s in after["Stages"]])],
          "%", "Concurrent virtual users")

    endpoints = list(before["Endpoints"].keys())
    grouped_bars(OUT / "stress-endpoints.svg", "Stress test — average response time by page",
                 "Whole stress run. The home page was the bottleneck before caching; afterwards the load moves to the next database-bound pages.",
                 endpoints, [before["Endpoints"][e]["Average"] for e in endpoints], [after["Endpoints"][e]["Average"] for e in endpoints],
                 " ms", "Endpoint")

    with (EVIDENCE / "stress-after" / "timeline.csv").open(encoding="utf-8") as handle:
        rows = list(csv.DictReader(handle))
    seconds = [float(r["second"]) for r in rows]
    stage_starts = [(i * 20, f"{u} users") for i, u in enumerate(users)]
    lines(OUT / "stress-timeline.svg", "Stress test (after mitigation) — p95 response time over the run",
          "One point per second. Dashed lines mark each step up in concurrent users; spikes are requests hitting the 10 s timeout.",
          seconds, [("p95 response time", AFTER, [float(r["p95_ms"]) for r in rows])],
          " ms", "Seconds since start", stage_starts, "s")

    print(f"Charts written to {OUT}")


if __name__ == "__main__":
    main()
