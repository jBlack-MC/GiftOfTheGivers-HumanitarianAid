"""
Builds the project deliverables from Markdown sources.

    python docs/tools/build_deliverables.py            # HTML + PDF for every source
    python docs/tools/build_deliverables.py --no-pdf   # HTML only

Sources live in docs/deliverables/src/*.md and start with a front-matter block:

    ---
    number: 01
    title: Product Requirements
    subtitle: What the system must do and for whom
    version: 1.0
    status: Approved for development
    output: 01-product-requirements      (optional; defaults to the source file name)
    ---

Output: docs/deliverables/<output>.html and .pdf (PDF rendered with headless Microsoft Edge or
Google Chrome, which must be installed). Only the Python standard library is used so the build
works on any machine with Python 3.10+.

Markdown support: headings (#..####), paragraphs, **bold**, *italic*, `code`, [links](url),
images ![caption](path) rendered as figures, fenced code (```lang), GFM tables, nested lists
(2+ space indent), blockquotes, horizontal rules, and raw HTML blocks (lines starting with "<").

Placeholders filled from docs/audits/findings-register.json (via findings.py):
    {{findings:table}}   full status table, ordered by severity
    {{findings:open}}    only findings that are not Fixed
"""
from __future__ import annotations

import argparse
import datetime as dt
import html
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "docs" / "deliverables" / "src"
OUT = ROOT / "docs" / "deliverables"


# --------------------------------------------------------------------------- inline


def slugify(text: str) -> str:
    text = re.sub(r"<[^>]+>", "", text)
    text = re.sub(r"[^\w\s-]", "", text.lower())
    return re.sub(r"[\s_]+", "-", text).strip("-")


def inline(text: str) -> str:
    """Converts inline Markdown to HTML. Code spans are protected from other rules."""
    placeholders: list[str] = []

    def keep(fragment: str) -> str:
        placeholders.append(fragment)
        return f"\x00{len(placeholders) - 1}\x00"

    text = re.sub(r"`([^`]+)`", lambda m: keep(f"<code>{html.escape(m.group(1))}</code>"), text)
    # Raw inline HTML tags are kept as-is.
    text = re.sub(r"</?[a-zA-Z][^<>]*>", lambda m: keep(m.group(0)), text)
    text = html.escape(text, quote=False)
    text = re.sub(r"!\[([^\]]*)\]\(([^)\s]+)\)",
                  lambda m: keep(f'<img src="{m.group(2)}" alt="{html.escape(m.group(1))}">'), text)
    text = re.sub(r"\[([^\]]+)\]\(([^)\s]+)\)", lambda m: keep(f'<a href="{m.group(2)}">') + m.group(1) + keep("</a>"), text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", text)
    text = re.sub(r"(?<![\w*])\*([^*\n]+)\*(?![\w*])", r"<em>\1</em>", text)
    text = re.sub(r"(?<![\w_])_([^_\n]+)_(?![\w_])", r"<em>\1</em>", text)
    while "\x00" in text:
        text = re.sub(r"\x00(\d+)\x00", lambda m: placeholders[int(m.group(1))], text)
    return text


# --------------------------------------------------------------------------- blocks


def render_table(lines: list[str]) -> str:
    def cells(row: str) -> list[str]:
        row = row.strip()
        if row.startswith("|"):
            row = row[1:]
        if row.endswith("|"):
            row = row[:-1]
        # Split on pipes not escaped and not inside code spans.
        parts, current, in_code = [], "", False
        for ch in row:
            if ch == "`":
                in_code = not in_code
            if ch == "|" and not in_code:
                parts.append(current)
                current = ""
            else:
                current += ch
        parts.append(current)
        return [p.strip().replace("\\|", "|") for p in parts]

    header = cells(lines[0])
    aligns = []
    for spec in cells(lines[1]):
        if spec.startswith(":") and spec.endswith(":"):
            aligns.append("center")
        elif spec.endswith(":"):
            aligns.append("right")
        else:
            aligns.append("")
    body = [cells(l) for l in lines[2:]]
    css = ' class="long"' if len(body) > 14 else ""
    out = [f"<table{css}><thead><tr>"]
    for i, h in enumerate(header):
        style = f' style="text-align:{aligns[i]}"' if i < len(aligns) and aligns[i] else ""
        out.append(f"<th{style}>{inline(h)}</th>")
    out.append("</tr></thead><tbody>")
    for row in body:
        out.append("<tr>")
        for i in range(len(header)):
            value = row[i] if i < len(row) else ""
            style = f' style="text-align:{aligns[i]}"' if i < len(aligns) and aligns[i] else ""
            out.append(f"<td{style}>{inline(value)}</td>")
        out.append("</tr>")
    out.append("</tbody></table>")
    return "".join(out)


LIST_RE = re.compile(r"^(\s*)([-*+]|\d+\.)\s+(.*)$")


def render_list(lines: list[str]) -> str:
    """Renders a (possibly nested) list block."""
    items: list[tuple[int, bool, str]] = []
    for line in lines:
        m = LIST_RE.match(line)
        if m:
            items.append((len(m.group(1).expandtabs(4)), m.group(2)[0].isdigit(), m.group(3)))
        elif items:
            indent, ordered, text = items[-1]
            items[-1] = (indent, ordered, text + " " + line.strip())

    out: list[str] = []
    stack: list[tuple[int, str]] = []
    for indent, ordered, text in items:
        tag = "ol" if ordered else "ul"
        while stack and indent < stack[-1][0]:
            out.append(f"</li></{stack.pop()[1]}>")
        if not stack or indent > stack[-1][0]:
            out.append(f"<{tag}>")
            stack.append((indent, tag))
        else:
            out.append("</li>")
        out.append(f"<li>{inline(text)}")
    while stack:
        out.append(f"</li></{stack.pop()[1]}>")
    return "".join(out)


def figure(alt: str, src: str) -> str:
    cls = ' class="shot"' if "screenshots" in src else ""
    caption = f"<figcaption>{inline(alt)}</figcaption>" if alt else ""
    return f'<figure><img{cls} src="{src}" alt="{html.escape(alt)}">{caption}</figure>'


def markdown_to_html(md: str) -> tuple[str, list[tuple[int, str, str]]]:
    lines = md.replace("\r\n", "\n").split("\n")
    out: list[str] = []
    toc: list[tuple[int, str, str]] = []
    used_ids: dict[str, int] = {}
    i = 0
    paragraph: list[str] = []

    def flush():
        if paragraph:
            out.append(f"<p>{inline(' '.join(s.strip() for s in paragraph))}</p>")
            paragraph.clear()

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        if not stripped:
            flush()
            i += 1
            continue

        if stripped.startswith("```"):
            flush()
            lang = stripped[3:].strip()
            code = []
            i += 1
            while i < len(lines) and not lines[i].strip().startswith("```"):
                code.append(lines[i])
                i += 1
            i += 1
            body = html.escape("\n".join(code))
            if lang == "mermaid":
                out.append('<figure><pre class="mermaid-src"><code>' + body +
                           '</code></pre><figcaption>Diagram source (Mermaid) as recorded at the audit date</figcaption></figure>')
            else:
                out.append(f'<pre><code class="language-{lang}">{body}</code></pre>')
            continue

        m = re.match(r"^(#{1,4})\s+(.*)$", stripped)
        if m:
            flush()
            level = len(m.group(1))
            text = m.group(2).strip()
            anchor = slugify(text) or f"section-{len(toc) + 1}"
            if anchor in used_ids:
                used_ids[anchor] += 1
                anchor = f"{anchor}-{used_ids[anchor]}"
            else:
                used_ids[anchor] = 0
            if level == 2:
                toc.append((level, anchor, text))
            out.append(f'<h{level} id="{anchor}">{inline(text)}</h{level}>')
            i += 1
            continue

        if re.match(r"^(-{3,}|\*{3,})$", stripped):
            flush()
            out.append("<hr>")
            i += 1
            continue

        if stripped.startswith("|") and i + 1 < len(lines) and re.match(r"^\s*\|?\s*:?-{2,}", lines[i + 1]):
            flush()
            block = []
            while i < len(lines) and lines[i].strip().startswith("|"):
                block.append(lines[i])
                i += 1
            out.append(render_table(block))
            continue

        if stripped.startswith(">"):
            flush()
            block = []
            while i < len(lines) and lines[i].strip().startswith(">"):
                block.append(re.sub(r"^\s*>\s?", "", lines[i]))
                i += 1
            inner, _ = markdown_to_html("\n".join(block))
            out.append(f"<blockquote>{inner}</blockquote>")
            continue

        if LIST_RE.match(line):
            flush()
            block = []
            while i < len(lines) and (LIST_RE.match(lines[i]) or (lines[i].startswith("  ") and lines[i].strip() and block)):
                block.append(lines[i])
                i += 1
            out.append(render_list(block))
            continue

        img = re.match(r"^!\[([^\]]*)\]\(([^)\s]+)\)$", stripped)
        if img:
            flush()
            out.append(figure(img.group(1), img.group(2)))
            i += 1
            continue

        if stripped.startswith("<"):
            flush()
            block = []
            while i < len(lines) and lines[i].strip():
                block.append(lines[i])
                i += 1
            out.append("\n".join(block))
            continue

        paragraph.append(line)
        i += 1

    flush()
    return "\n".join(out), toc


# --------------------------------------------------------------------------- page


def parse_front_matter(text: str) -> tuple[dict[str, str], str]:
    meta: dict[str, str] = {}
    if text.startswith("---"):
        end = text.index("\n---", 3)
        for raw in text[3:end].strip().splitlines():
            if ":" in raw:
                key, value = raw.split(":", 1)
                meta[key.strip()] = value.strip()
        text = text[end + 4:]
    return meta, text


def page(meta: dict[str, str], body: str, toc: list[tuple[int, str, str]]) -> str:
    title = meta.get("title", "Document")
    number = meta.get("number", "")
    date = meta.get("date", dt.date.today().isoformat())
    toc_html = ""
    if toc and meta.get("toc", "true") != "false":
        items = "".join(f'<li><a href="#{a}">{inline(t)}</a></li>' for _, a, t in toc)
        toc_html = f'<nav class="toc"><h2>Contents</h2><ol>{items}</ol></nav>'
    meta_cells = [
        ("Document", f"D-{number}" if number else "—"),
        ("Version", meta.get("version", "1.0")),
        ("Date", date),
        ("Status", meta.get("status", "Current")),
    ]
    meta_html = "".join(f"<div><span>{k}</span><strong>{html.escape(v)}</strong></div>" for k, v in meta_cells)
    return f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>{html.escape(title)} — Gift of the Givers Relief Management System</title>
<link rel="stylesheet" href="assets/deliverables.css">
</head>
<body>
<main class="doc">
<header class="cover">
  <div class="brand"><img src="assets/logo.svg" alt=""> Gift of the Givers · Relief Management System</div>
  <div class="docno">Deliverable {html.escape(number)}</div>
  <h1>{html.escape(title)}</h1>
  <p class="subtitle">{inline(meta.get("subtitle", ""))}</p>
  <div class="meta">{meta_html}</div>
</header>
{toc_html}
{body}
<footer class="docfoot"><span>Gift of the Givers Relief Management System · {html.escape(title)}</span><span>Generated from docs/deliverables/src by docs/tools/build_deliverables.py</span></footer>
</main>
</body>
</html>
"""


def find_browser() -> str | None:
    candidates = [
        os.environ.get("DELIVERABLES_BROWSER", ""),
        r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        r"C:\Program Files\Google\Chrome\Application\chrome.exe",
        shutil.which("msedge") or "",
        shutil.which("google-chrome") or "",
        shutil.which("chromium") or "",
    ]
    return next((c for c in candidates if c and Path(c).exists()), None)


def render_pdf(browser: str, html_path: Path, pdf_path: Path, profile: Path) -> bool:
    result = subprocess.run(
        [browser, "--headless=new", "--disable-gpu", "--no-first-run", f"--user-data-dir={profile}",
         "--no-pdf-header-footer", f"--print-to-pdf={pdf_path}", html_path.resolve().as_uri()],
        capture_output=True, text=True, timeout=180)
    return pdf_path.exists() and "written" in (result.stdout + result.stderr)


def expand_placeholders(text: str) -> str:
    if "{{findings:" not in text:
        return text
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import findings  # noqa: E402  (local module)

    register = findings.load()
    return (text.replace("{{findings:table}}", findings.table(register))
                .replace("{{findings:open}}", findings.table(register, only_open=True)))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--no-pdf", action="store_true")
    parser.add_argument("only", nargs="*", help="Build only sources whose name contains these strings")
    args = parser.parse_args()

    browser = None if args.no_pdf else find_browser()
    if not args.no_pdf and browser is None:
        print("No Edge/Chrome found; building HTML only.", file=sys.stderr)

    profile = Path(os.environ.get("TEMP", "/tmp")) / "deliverables-pdf-profile"
    sources = sorted(SRC.glob("*.md"))
    if args.only:
        sources = [s for s in sources if any(o in s.name for o in args.only)]

    for source in sources:
        meta, text = parse_front_matter(source.read_text(encoding="utf-8"))
        text = expand_placeholders(text)
        body, toc = markdown_to_html(text)
        name = meta.get("output", source.stem)
        html_path = OUT / f"{name}.html"
        html_path.write_text(page(meta, body, toc), encoding="utf-8")
        status = "html"
        if browser:
            pdf_path = OUT / f"{name}.pdf"
            if pdf_path.exists():
                pdf_path.unlink()
            status += " + pdf" if render_pdf(browser, html_path, pdf_path, profile) else " (PDF FAILED)"
        print(f"{name}: {status}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
