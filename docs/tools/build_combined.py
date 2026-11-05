"""
Builds the single Part 3 submission document that contains every deliverable (01–12):

    python docs/tools/build_deliverables.py        # refresh the individual HTML/PDF first
    python docs/tools/build_combined.py

Writes docs/deliverables/Part3-Complete-Submission.docx  (Word, built with pandoc)
       docs/deliverables/Part3-Complete-Submission.pdf   (cover + note + contents, then each deliverable PDF)

Needs: pip install pypandoc_binary pymupdf. The note to the marker is in NOTE below.
"""
from __future__ import annotations

import html
import re
import shutil
import sys
import tempfile
import zipfile
from pathlib import Path

import pymupdf
import pypandoc

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_deliverables as bd  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "docs" / "deliverables" / "src"
OUT = ROOT / "docs" / "deliverables"
NAME = "Part3-Complete-Submission"
TITLE = "Part 3 — Complete Submission"
SUBTITLE = "Testing, Deployment and Handover · every project deliverable (01–12) in one document"
DATE = "2 November 2026"

NOTE_TITLE = "Note to the marker: problem with our Azure account"
NOTE = [
    "We have a problem with our Azure account. Because of it, we could not complete the deployment to Azure "
    "before this submission: the live web app still shows the Azure “waiting for your content” page, and the "
    "deployment stages of the pipeline (C.1 web app, C.2 Azure Function, C.3 SQL database) have not yet run in "
    "Azure DevOps. The Azure DevOps access token we had for connecting GitHub to Azure DevOps was rejected "
    "(HTTP 401 Unauthorized) when we tested it on 2 November 2026.",
    "Everything needed for the deployment is in place and documented: the pipeline (azure-pipelines.yml and "
    "pipelines/templates), the step-by-step Azure set-up and GitHub-to-Azure sync guide (Deliverable 11), the "
    "database migration and rollback scripts, and the rollback and restore procedures. The testing work (unit, "
    "integration, load, stress and Selenium UI tests) is complete, and its evidence is in Deliverable 09 and in "
    "docs/evidence/part3 in the repository.",
    "As soon as the account problem is resolved, a team member will follow Deliverable 11, run the pipeline and "
    "add the deployment screenshots listed in Deliverable 09, section 10.",
]


def sources() -> list[Path]:
    # An addendum follows the document it amends (plain name order would put it first).
    return sorted(SRC.glob("*.md"), key=lambda p: p.stem.replace("-addendum", "~addendum"))


# --------------------------------------------------------------------------- PDF


def note_html(contents: list[tuple[str, str, int]]) -> str:
    rows = "".join(
        f"<tr><td>{html.escape(n)}</td><td>{html.escape(t)}</td><td style='text-align:right'>{p}</td></tr>"
        for n, t, p in contents)
    paras = "".join(f"<p>{html.escape(p)}</p>" for p in NOTE)
    return f"""<!DOCTYPE html><html lang="en"><head><meta charset="utf-8">
<link rel="stylesheet" href="assets/deliverables.css"><title>{TITLE}</title></head><body><main class="doc">
<header class="cover">
  <div class="brand"><img src="assets/logo.svg" alt=""> Gift of the Givers · Relief Management System</div>
  <div class="docno">APPR6312 · Portfolio of Evidence</div>
  <h1>{TITLE}</h1>
  <p class="subtitle">{SUBTITLE}</p>
  <div class="meta"><div><span>Date</span><strong>{DATE}</strong></div><div><span>Deliverables</span><strong>{len(contents)}</strong></div></div>
</header>
<blockquote><h2>{NOTE_TITLE}</h2>{paras}</blockquote>
<h2>Contents</h2>
<table><thead><tr><th>No.</th><th>Deliverable</th><th style='text-align:right'>Page</th></tr></thead><tbody>{rows}</tbody></table>
</main></body></html>"""


def build_pdf() -> Path:
    browser = bd.find_browser()
    if browser is None:
        raise SystemExit("Edge or Chrome is required for the PDF.")
    parts = []
    for source in sources():
        meta, _ = bd.parse_front_matter(source.read_text(encoding="utf-8"))
        pdf = OUT / f"{meta.get('output', source.stem)}.pdf"
        if not pdf.exists():
            raise SystemExit(f"Missing {pdf.name}: run build_deliverables.py first.")
        parts.append((meta.get("number", ""), meta.get("title", source.stem), pdf))

    profile = Path(tempfile.gettempdir()) / "deliverables-pdf-profile"

    def render_front(contents):
        html_path = OUT / f"_{NAME}-front.html"
        pdf_path = OUT / f"_{NAME}-front.pdf"
        html_path.write_text(note_html(contents), encoding="utf-8")
        if pdf_path.exists():
            pdf_path.unlink()
        if not bd.render_pdf(browser, html_path, pdf_path, profile):
            raise SystemExit("Front matter PDF failed.")
        doc = pymupdf.open(pdf_path)
        html_path.unlink()
        return doc, pdf_path

    # Render once to learn the front-matter length, then again with the real page numbers.
    front, front_path = render_front([(n, t, 0) for n, t, _ in parts])
    start = front.page_count + 1
    contents = []
    for number, title, pdf in parts:
        contents.append((number, title, start))
        start += pymupdf.open(pdf).page_count
    front.close()
    front, front_path = render_front(contents)

    combined = pymupdf.open()
    combined.insert_pdf(front)
    toc = [[1, NOTE_TITLE, 1]]
    for (number, title, pdf), (_, _, page) in zip(parts, contents):
        combined.insert_pdf(pymupdf.open(pdf))
        toc.append([1, f"{number} {title}", page])
    combined.set_toc(toc)
    combined.set_metadata({"title": TITLE, "subject": SUBTITLE, "author": "Gift of the Givers project team"})
    target = OUT / f"{NAME}.pdf"
    combined.save(target, garbage=3, deflate=True)
    front.close()
    front_path.unlink()
    return target


# --------------------------------------------------------------------------- DOCX


def svg_to_png(svg: Path, cache: Path) -> Path:
    png = cache / (svg.stem + ".png")
    if not png.exists():
        doc = pymupdf.open(svg)
        doc[0].get_pixmap(dpi=200).save(png)
    return png


def to_pandoc_markdown(text: str, cache: Path) -> str:
    """Rewrites the few HTML/SVG constructs the HTML builder supports into plain Markdown."""
    text = bd.expand_placeholders(text)

    # Colour swatches → table.
    swatches = re.findall(r'<div class="swatch"><div style="background:(#[0-9A-Fa-f]{6})"></div><p><strong>([^<]+)</strong><br>([^<]+)</p></div>', text)
    if swatches:
        table = "| Colour | Hex | Use |\n| --- | --- | --- |\n" + "".join(
            f"| {name} | {hexv} | {html.unescape(desc.split('·', 1)[-1]).strip()} |\n" for hexv, name, desc in swatches)
        text = re.sub(r'<div class="swatches">.*?\n</div>\n', table, text, flags=re.S)

    # <figure><img src alt><figcaption>…</figcaption></figure> → ![caption](src)
    text = re.sub(r'<figure><img[^>]*src="([^"]+)"[^>]*>(?:<figcaption>(.*?)</figcaption>)?</figure>',
                  lambda m: f"\n![{re.sub(r'<[^>]+>', '', m.group(2) or '')}]({m.group(1)})\n", text)
    text = re.sub(r'^</?div[^>]*>\s*$', "", text, flags=re.M)

    # SVG images → PNG (Word has no reliable SVG support).
    def image(m):
        alt, src = m.group(1), m.group(2)
        path = OUT / src
        if path.suffix.lower() == ".svg" and path.exists():
            src = svg_to_png(path, cache).as_posix()
        else:
            src = path.as_posix()
        return f"![{alt}]({src})"
    text = re.sub(r"!\[([^\]]*)\]\(([^)\s]+)\)", image, text)

    # Each deliverable becomes a level-1 heading, so its own headings move down one level.
    lines, fenced = [], False
    for line in text.split("\n"):
        if line.lstrip().startswith("```"):
            fenced = not fenced
        elif not fenced and re.match(r"^#{1,5}\s", line):
            line = "#" + line
        lines.append(line)
    text = "\n".join(lines)

    # Mermaid sources stay as code but are labelled.
    text = text.replace("```mermaid", "```text")
    return text


PAGE_BREAK = '\n```{=openxml}\n<w:p><w:r><w:br w:type="page"/></w:r></w:p>\n```\n'


def reference_docx(work: Path) -> Path:
    """pandoc's default template with bordered tables, a shaded header row and brand colours."""
    ref = work / "reference.docx"
    ref.write_bytes(_default_reference())
    unpacked = work / "ref"
    with zipfile.ZipFile(ref) as z:
        z.extractall(unpacked)
    styles = (unpacked / "word" / "styles.xml").read_text(encoding="utf-8")
    side = '<w:{0} w:val="single" w:sz="4" w:space="0" w:color="C9D3DE"/>'
    table_pr = ('<w:tblPr><w:tblInd w:w="0" w:type="dxa"/><w:tblBorders>'
                + "".join(side.format(s) for s in ("top", "left", "bottom", "right", "insideH", "insideV"))
                + '</w:tblBorders><w:tblCellMar><w:top w:w="60" w:type="dxa"/><w:left w:w="100" w:type="dxa"/>'
                  '<w:bottom w:w="60" w:type="dxa"/><w:right w:w="100" w:type="dxa"/></w:tblCellMar></w:tblPr>')
    header_row = ('<w:tblStylePr w:type="firstRow"><w:rPr><w:b/></w:rPr><w:tcPr>'
                  '<w:shd w:val="clear" w:color="auto" w:fill="E7F0F8"/><w:vAlign w:val="bottom"/></w:tcPr></w:tblStylePr>')

    def table_style(m):
        block = re.sub(r"<w:tblPr>.*?</w:tblPr>", table_pr, m.group(0), count=1, flags=re.S)
        return re.sub(r'<w:tblStylePr w:type="firstRow">.*?</w:tblStylePr>', header_row, block, count=1, flags=re.S)

    styles = re.sub(r'<w:style w:type="table"[^>]*w:styleId="Table"[^>]*>.*?</w:style>', table_style, styles,
                    count=1, flags=re.S)
    # Brand colour for headings and titles.
    styles = re.sub(r'(<w:style [^>]*w:styleId="(?:Heading[1-4]|Title)"[^>]*>.*?)<w:color w:val="[0-9A-Fa-f]{6}"',
                    lambda m: m.group(1) + '<w:color w:val="0F4676"', styles, flags=re.S)
    (unpacked / "word" / "styles.xml").write_text(styles, encoding="utf-8")
    styled = work / "reference-styled.docx"
    with zipfile.ZipFile(styled, "w", zipfile.ZIP_DEFLATED) as z:
        for file in unpacked.rglob("*"):
            if file.is_file():
                z.write(file, file.relative_to(unpacked).as_posix())
    return styled


def _default_reference() -> bytes:
    import subprocess
    return subprocess.run([pypandoc.get_pandoc_path(), "--print-default-data-file", "reference.docx"],
                          capture_output=True, check=True).stdout


def build_docx() -> Path:
    work = Path(tempfile.mkdtemp(prefix="combined-docx-"))
    cache = work / "png"
    cache.mkdir()
    chunks = [
        f"---\ntitle: \"{TITLE}\"\nsubtitle: \"{SUBTITLE}\"\nauthor: \"Gift of the Givers project team — APPR6312 Portfolio of Evidence\"\ndate: \"{DATE}\"\n---\n",
        f"# {NOTE_TITLE}\n\n" + "\n\n".join(f"**{NOTE[0]}**" if i == 0 else p for i, p in enumerate(NOTE)) + "\n",
    ]
    for source in sources():
        meta, body = bd.parse_front_matter(source.read_text(encoding="utf-8"))
        header = (f"# Deliverable {meta.get('number', '')}: {meta.get('title', source.stem)}\n\n"
                  f"*{meta.get('subtitle', '')}*\n\n"
                  f"Version {meta.get('version', '1.0')} · {meta.get('date', '')} · Status: {meta.get('status', '')}\n")
        chunks.append(PAGE_BREAK + header + "\n" + to_pandoc_markdown(body, cache))
    md = work / "combined.md"
    md.write_text("\n".join(chunks), encoding="utf-8")

    target = OUT / f"{NAME}.docx"
    pypandoc.convert_file(str(md), "docx", outputfile=str(target), format="markdown+pipe_tables+raw_attribute",
                          extra_args=["--toc", "--toc-depth=1", f"--reference-doc={reference_docx(work)}",
                                      f"--resource-path={OUT}"])
    shutil.rmtree(work, ignore_errors=True)
    return target


if __name__ == "__main__":
    print("PDF :", build_pdf())
    print("DOCX:", build_docx())
