#!/usr/bin/env python3
"""The offline weekly-report form, styled with the live site's own stylesheet, to fill in and keep on the computer.

Two modes, one template (offline_form_template.html):

    python3 build_offline_form.py report_input.json            # DRAFT: pre-filled with the agent's draft, before submitting
    python3 build_offline_form.py --submitted report_api.json  # BACKUP: the person's answers as submitted (raw GET /api/report)

Output: weekly_report_<week_ending>_offline.html beside the input (or -o PATH). Self-contained, no network.
What it does in the browser: autosaves in that browser; "Save file" downloads a copy of the page with the entries
inside; .md/.json export; .json import; "Check report" runs the live form's checks plus the site's silent limits
(note 500, label 120). Nothing is ever sent anywhere.
"""
import argparse
import json
import re
import sys
from datetime import date
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from wr_common import LABEL_MAX, NOTE_MAX, load, validate  # noqa: E402

TEMPLATE = Path(__file__).with_name("offline_form_template.html")


def human(week_ending):
    d = date.fromisoformat(week_ending)
    return d.strftime("%a %d %b %Y").replace(" 0", " "), d.strftime("Week ending %A %d %b %Y").replace(" 0", " "), d.strftime("%d %b").lstrip("0")


def from_input(d):
    """DRAFT state from report_input.json."""
    tasks = [{"title": r["title"], "meta": r.get("meta", ""), "outcome": r["outcome"], "links": r.get("evidence") or [""],
              "hours": f'{float(r.get("hours", 0) or 0):g}', "note": r.get("note", ""), "markDone": bool(r.get("mark_done")),
              "review": bool(r.get("needs_review"))} for r in d["rows"]]
    prev = [{"text": lw["text"], "done": lw.get("done_means", ""), "task": lw.get("task_title"), "day": lw.get("due_day", "Fri")}
            for lw in d.get("last_week", [])]
    picks = [None if lw.get("task_title") else {"met": "yes", "not met": "no"}.get(lw.get("pick")) for lw in d.get("last_week", [])]
    commits = [{"task": "__other" if c["task"] == "OTHER" else c["task"], "text": c.get("text", "") if c["task"] == "OTHER" else "",
                "day": c["due_day"], "done": c["done_means"]} for c in d["commitments"]]
    oh = d["other_hours"]
    state = {"who": d["person"], "prev": picks, "tasks": tasks, "otherHours": f'{float(oh.get("hours", 0) or 0):g}',
             "otherWhat": oh.get("label", ""), "blockers": d["blockers"], "duran": d["needs_from_duran"], "commits": commits, "savedAt": 0}
    return state, prev, d["person"], d["week_ending"], d.get("due", "")


def from_api(raw):
    """SUBMITTED state from the raw GET https://reports.aerosenseaviation.com/api/report response."""
    r = raw.get("report", raw)
    titles = {i["id"]: i["title"] for i in r["items"]}
    meta = lambda i: " · ".join(x for x in (i.get("status", ""), i.get("technology", ""), "due " + i["due_date"] if i.get("due_date") else "") if x)
    tasks = [{"title": i["title"], "meta": meta(i), "outcome": i.get("outcome") or "", "links": i.get("evidence_urls") or [""],
              "hours": f'{float(i.get("hours") or 0):g}', "note": i.get("note") or "", "markDone": False, "review": False} for i in r["items"]]
    pc = (r.get("previous") or {}).get("commitments", [])
    prev = [{"text": c["text"], "done": c.get("done_means", ""), "task": titles.get(c.get("task_id")) or (c["text"] if c.get("task_id") else None),
             "day": c.get("due_day", "Fri")} for c in pc]
    picks = [None if c.get("task_id") else ("yes" if c.get("met") else "no" if c.get("met") is False else None) for c in pc]
    commits = [{"task": titles.get(c.get("task_id"), "__other") if c.get("task_id") else "__other",
                "text": "" if c.get("task_id") else c["text"], "day": c.get("due_day") or "—", "done": c.get("done_means", "")}
               for c in sorted(r.get("commitments", []), key=lambda c: c.get("position", 0))]
    state = {"who": r.get("user_name", ""), "prev": picks, "tasks": tasks, "otherHours": f'{float(r.get("hours_other") or 0):g}',
             "otherWhat": r.get("hours_other_label", ""), "blockers": r.get("blockers", ""), "duran": r.get("needs_from_duran", ""),
             "commits": commits, "savedAt": 0}
    sub = r.get("submitted_at", "")
    return state, prev, r.get("user_name", ""), r["week_ending"], sub, r.get("revision"), r.get("timeliness", "")


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("input", help="report_input.json (draft) or, with --submitted, the saved GET /api/report JSON")
    ap.add_argument("--submitted", action="store_true", help="build the backup of the submitted report")
    ap.add_argument("-o", "--out")
    a = ap.parse_args()
    raw = load(a.input)
    if a.submitted:
        state, prev, person, week, sub, rev, timeliness = from_api(raw)
        when = f"Submitted {sub[:16].replace('T', ' ')} · revision {rev}." if sub else "Not submitted yet."
        mode = {"SRC": "the submitted report", "RESET": "Reset to submitted",
                "CHIP": f'<span class="chip ok">Submitted · {timeliness.lower() or "on time"}</span>' if sub else '<span class="chip warn">Draft on the site</span>',
                "BANNER": f"Your answers as submitted ({when.rstrip('.')}), read back from the site. If you need a revision while the "
                          "online form is down, edit here, press <b>Check report</b>, and send the summary or the saved file.",
                "WHEN": when, "KEY_SUFFIX": f"submitted-r{rev}"}
    else:
        problems = validate(raw)
        if problems:
            sys.exit("report_input.json does not pass the live form's rules:\n- " + "\n- ".join(problems))
        state, prev, person, week, due = from_input(raw)
        mode = {"SRC": "the draft", "RESET": "Reset to draft", "CHIP": f'<span class="chip warn">Due {due}</span>',
                "BANNER": "Pre-filled with the agent's draft (the same text as the copy-paste page). Change anything. If the online "
                          "form breaks, finish here, press <b>Check report</b>, then send the summary or the saved file.",
                "WHEN": f"Due {due}.", "KEY_SUFFIX": "draft"}
    weekhuman, weekline, asof = human(week)
    slug = re.sub(r"[^A-Za-z0-9]+", "_", person).strip("_") or "person"
    data = {"draft": state, "prev": prev, "noteMax": NOTE_MAX, "labelMax": LABEL_MAX, "person": person}
    fill = {"TITLE": f"Weekly report · {weekhuman} ({'as submitted, offline backup' if a.submitted else 'offline'})",
            "PERSON": person, "WEEKHUMAN": weekhuman, "WEEKLINE": weekline, "ASOF": asof,
            "BUILT": f"Built {date.today().isoformat()} from the live form's fields and stylesheet.",
            "FILESTEM": f"weekly_report_{slug}_{week}", "KEY": f"aerosense-weekly-report-offline-{week}-{slug}-{mode.pop('KEY_SUFFIX')}"}
    fill.update(mode)
    page = TEMPLATE.read_text(encoding="utf-8").replace("/*DATA*/", json.dumps(data, ensure_ascii=False).replace("<", "\\u003c"))
    for k, v in fill.items():
        page = page.replace(f"@@{k}@@", v)
    left = re.findall(r"@@[A-Z]+@@", page)
    assert not left, left
    out = Path(a.out) if a.out else Path(a.input).with_name(f"weekly_report_{week}_offline{'_submitted' if a.submitted else ''}.html")
    out.write_text(page, encoding="utf-8")
    print(out, f"{len(page) / 1024:.0f} KB, {len(state['tasks'])} rows, {len(state['commits'])} commitments, mode: {fill['SRC']}")


if __name__ == "__main__":
    main()
