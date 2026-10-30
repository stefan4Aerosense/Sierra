"""Shared helpers for the weekly-report kit: load, validate, escape.

Every builder in this folder reads one file, report_input.json (schema: PLAYBOOK.md §6), so the copy-paste page and
the offline form can never disagree. Python 3.8+, standard library only, works on macOS, Linux and Windows.
"""
import html
import json
import re
from pathlib import Path

SCHEMA = "aerosense-weekly-report-input/1"
OUTCOMES = ("Done", "Progressed", "Blocked", "No work")
DAYS = ("Mon", "Tue", "Wed", "Thu", "Fri")
NOTE_MAX, LABEL_MAX = 500, 120   # what reports.aerosenseaviation.com keeps; longer text is cut while the page still says Saved


def load(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def esc(s):
    return html.escape(str(s), quote=True)


def validate(d):
    """Return a list of problems; empty means the input matches what the live form will accept."""
    p = []
    if d.get("schema") != SCHEMA:
        p.append(f"schema must be {SCHEMA!r}")
    for k in ("person", "week_ending", "due", "rows", "other_hours", "blockers", "needs_from_duran", "commitments"):
        if k not in d:
            p.append(f"missing key: {k}")
    titles = [r.get("title", "") for r in d.get("rows", [])]
    for r in d.get("rows", []):
        t = r.get("title", "?")
        if r.get("outcome") not in OUTCOMES:
            p.append(f"{t}: outcome must be one of {OUTCOMES}")
        if r.get("outcome") in ("Done", "Progressed") and not any(u.strip() for u in r.get("evidence", [])):
            p.append(f"{t}: Done and Progressed need an evidence link")
        if r.get("outcome") == "Blocked" and not r.get("note", "").strip():
            p.append(f"{t}: Blocked needs a note")
        if len(r.get("note", "")) > NOTE_MAX:
            p.append(f"{t}: note is {len(r['note'])} characters; the site keeps {NOTE_MAX}")
        if r.get("mark_done") and r.get("outcome") != "Done":
            p.append(f"{t}: mark_done is set but the outcome is not Done")
    oh = d.get("other_hours", {})
    if len(oh.get("label", "")) > LABEL_MAX:
        p.append(f"other_hours.label is {len(oh['label'])} characters; the site keeps {LABEL_MAX}")
    cs = d.get("commitments", [])
    if not 3 <= len(cs) <= 5:
        p.append(f"commitments: three to five (have {len(cs)})")
    for i, c in enumerate(cs, 1):
        if c.get("task") != "OTHER" and c.get("task") not in titles:
            p.append(f"commitment {i}: task must be a row title or OTHER")
        if c.get("task") == "OTHER" and not c.get("text", "").strip():
            p.append(f"commitment {i}: OTHER needs text")
        if c.get("due_day") not in DAYS:
            p.append(f"commitment {i}: due_day must be one of {DAYS}")
        if not c.get("done_means", "").strip():
            p.append(f"commitment {i}: done_means is empty")
    for lw in d.get("last_week", []):
        if lw.get("task_title") is None and lw.get("pick") not in ("met", "not met", None):
            p.append(f"last_week {lw.get('text', '')[:40]!r}: pick must be 'met', 'not met' or null")
    board = d.get("board", {})
    for t in board.get("close", []):
        if t not in titles:
            p.append(f"board.close: {t!r} is not one of the report rows")
    return p


def anchors(md_path):
    """GitHub's heading anchors for a Markdown file: lowercase, punctuation dropped (not - or _), spaces to hyphens."""
    out = set()
    for line in Path(md_path).read_text(encoding="utf-8").splitlines():
        m = re.match(r"#{1,6} (.+)", line)
        if m:
            out.add(re.sub(r"[^\w\- ]", "", m.group(1).strip().lower()).replace(" ", "-"))
    return out


def check_links(d, input_path):
    """Every evidence link into this week's page must hit a heading that exists (needs evidence_page.local_md)."""
    ev = d.get("evidence_page", {})
    if not ev.get("url") or not ev.get("local_md"):
        return []
    md = Path(input_path).parent / ev["local_md"]
    if not md.exists():
        return [f"evidence_page.local_md not found: {md}"]
    have, p = anchors(md), []
    for r in d["rows"]:
        for u in r.get("evidence", []):
            if u.startswith(ev["url"] + "#") and u.split("#", 1)[1] not in have:
                p.append(f"{r['title']}: anchor #{u.split('#', 1)[1]} is not a heading in {ev['local_md']}")
    return p
