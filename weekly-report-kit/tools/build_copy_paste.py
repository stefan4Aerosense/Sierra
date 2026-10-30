#!/usr/bin/env python3
"""The copy-paste page: every weekly-report field with a Copy button, plus the task-board comments and the cards to close.

    python3 build_copy_paste.py report_input.json            ->  weekly_report_<week_ending>.html beside the input
    python3 build_copy_paste.py report_input.json -o out.html

The person opens this page beside the live form (reports.aerosenseaviation.com) and pastes field by field. Offline,
self-contained, dark theme. It refuses to build if report_input.json would not pass the live form (wr_common.validate).
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from wr_common import LABEL_MAX, NOTE_MAX, check_links, esc, load, validate  # noqa: E402

CSS = """:root{--bg:#0B0B0B;--panel:#14181c;--line:#252c32;--ink:#e8edf0;--mute:#8a96a0;--teal:#3585a0;--cyan:#38c6e8;--g:#3fb68b;--a:#e0a03a;--r:#e5534b}
*{box-sizing:border-box} body{margin:0;background:var(--bg);color:var(--ink);font:15px/1.5 -apple-system,system-ui,Segoe UI,Inter,sans-serif}
main{max-width:1180px;margin:0 auto;padding:22px 16px 80px} h1{font-size:24px;letter-spacing:.05em;margin:0} h2{font-size:18px;color:var(--teal);letter-spacing:.04em;margin:34px 0 10px;border-bottom:1px solid var(--line);padding-bottom:6px}
h3{font-size:15px;margin:18px 0 6px;color:var(--cyan)} .sub,.m{color:var(--mute)} a{color:var(--cyan)}
nav{position:sticky;top:0;z-index:5;background:#0b0b0bee;border-bottom:1px solid var(--line);padding:8px 0;margin:10px 0 0;display:flex;flex-wrap:wrap;gap:6px}
nav a{text-decoration:none;font-size:13px;padding:3px 9px;border:1px solid var(--line);border-radius:999px;color:var(--ink)}
.card,.row,.task{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:12px;margin:10px 0}
.rt{display:flex;gap:10px;align-items:baseline} .n{color:var(--mute);font-size:12px;min-width:18px} .kv{display:flex;flex-wrap:wrap;gap:14px;margin:8px 0;font-size:14px;color:var(--mute)} .kv b{color:var(--ink)}
.pill{font-size:12px;font-weight:700;padding:2px 10px;border-radius:999px;color:#0b0b0b} .done{background:var(--g)} .prog{background:var(--cyan)} .block{background:var(--r)} .none{background:#55616b;color:#e8edf0}
.cp{margin:6px 0} .cph{display:flex;justify-content:space-between;align-items:center;font-size:12px;color:var(--mute);margin-bottom:3px}
textarea{width:100%;min-height:44px;background:#0e1114;color:var(--ink);border:1px solid var(--line);border-radius:8px;padding:8px;font:13px/1.45 ui-monospace,SFMono-Regular,Menlo,monospace;resize:vertical;field-sizing:content}
button{background:#1c2a33;color:var(--cyan);border:1px solid #2b4250;border-radius:7px;padding:3px 10px;font:inherit;font-size:12px;cursor:pointer} button.ok{color:var(--g);border-color:var(--g)}
table{width:100%;border-collapse:collapse;font-size:14px} td,th{text-align:left;vertical-align:top;padding:7px 8px;border-top:1px solid var(--line)} th{color:var(--mute);font-weight:600}
.dot{display:inline-block;width:10px;height:10px;border-radius:50%;margin-right:8px} .dot.green{background:var(--g)} .dot.amber{background:var(--a)} .dot.red{background:var(--r)}
.tot{font-size:20px;font-weight:700} ul{margin:6px 0 0 18px;padding:0} li{margin:3px 0} .tablewrap{overflow-x:auto}"""

JS = """function done(b){b.textContent='Copied';b.classList.add('ok');setTimeout(()=>{b.textContent='Copy';b.classList.remove('ok')},1400)}
async function cp(t,b){try{await navigator.clipboard.writeText(t);done(b)}catch(e){const ta=document.createElement('textarea');ta.value=t;document.body.appendChild(ta);ta.select();try{document.execCommand('copy');done(b)}catch(_){}ta.remove()}}
document.querySelectorAll('button.copy').forEach(b=>b.addEventListener('click',()=>cp(b.closest('.cp').querySelector('textarea').value,b)));
document.querySelectorAll('button.copy1').forEach(b=>b.addEventListener('click',()=>cp(b.dataset.t,b)));
document.querySelectorAll('textarea').forEach(t=>{if(!CSS.supports('field-sizing','content')){t.style.height='auto';t.style.height=(t.scrollHeight+4)+'px'}});"""


def block(text, label=""):
    return (f'<div class="cp"><div class="cph"><span>{esc(label)}</span><button class="copy">Copy</button></div>'
            f'<textarea readonly spellcheck="false">{esc(text)}</textarea></div>')


def build(d):
    rows, task_h = [], 0.0
    for i, r in enumerate(d["rows"], 1):
        task_h += float(r.get("hours", 0) or 0)
        cls = {"Done": "done", "Progressed": "prog", "Blocked": "block", "No work": "none"}[r["outcome"]]
        ev = "".join(block(u, "Evidence link") for u in r.get("evidence", []) if u.strip()) or \
            '<div class="m" style="font-size:13px">No evidence link needed.</div>'
        rows.append(f'''<div class="row"><div class="rt"><span class="n">{i}</span><b>{esc(r["title"])}</b> <span class="m">{esc(r.get("meta", ""))}</span></div>
<div class="kv"><span class="pill {cls}">{r["outcome"]}</span><span>Hours <b>{float(r.get("hours", 0) or 0):g}</b> <i>(est.)</i></span>
<span>Mark task done: <b>{"YES" if r.get("mark_done") else "no"}</b></span><span>Flag needs review: <b>{"YES" if r.get("needs_review") else "no"}</b></span><span class="m">{len(r.get("note", ""))}/{NOTE_MAX} chars</span></div>
{ev}{block(r.get("note", ""), "Note")}</div>''')
    oh = d["other_hours"]
    other_h = float(oh.get("hours", 0) or 0)
    glance = "".join(f'<tr><td><span class="dot {esc(g["status"])}"></span>{esc(g["area"])}</td><td>{esc(g["text"])}</td></tr>' for g in d.get("glance", []))
    last = "".join(
        f'<tr><td>{esc(lw["text"])}<div class="m" style="font-size:13px">Done means: {esc(lw.get("done_means", ""))}</div></td>'
        f'<td>{"automatic (board task)" if lw.get("task_title") else "you pick: <b>" + esc(lw.get("pick") or "?") + "</b>"}</td><td>{esc(lw.get("reading", ""))}</td></tr>'
        for lw in d.get("last_week", []))
    commits = "".join(
        f'<tr><td>{"Something not on the board…" if c["task"] == "OTHER" else esc(c["task"])}'
        f'{"<br>" + block(c.get("text", ""), "Task text") if c["task"] == "OTHER" else ""}</td><td>{esc(c["due_day"])}</td><td>{block(c["done_means"], "Done means…")}</td></tr>'
        for c in d["commitments"])
    board = d.get("board", {})
    close = "".join(f"<li>{esc(t)}</li>" for t in board.get("close", [])) or "<li>none</li>"
    sclose = "".join(f"<li>{esc(s['title'])} <span class='m'>({esc(s.get('why', ''))})</span></li>" for s in board.get("suggest_close", [])) or "<li>none</li>"
    comments = "".join(
        f'<div class="task"><div class="tt"><b>{esc(c["card"])}</b> <span class="pill {"done" if c.get("outcome", "").startswith("Done") else "prog"}">{esc(c.get("outcome", ""))}</span></div>{block(c["comment"], "Comment")}</div>'
        for c in board.get("comments", []))
    newt = "".join(f'<tr><td>{esc(n["title"])}</td><td>{esc(n.get("owner", ""))}</td><td>{esc(n.get("why", ""))}</td><td><button class="copy1" data-t="{esc(n["title"])}">Copy</button></td></tr>'
                   for n in board.get("new_cards", []))
    calls = "".join(f"<li>{esc(c)}</li>" for c in d.get("calls", []))
    ev = d.get("evidence_page", {})
    evline = f' · evidence page: <a href="{esc(ev["url"])}">{esc(ev.get("label", ev["url"]))}</a>' if ev.get("url") else ""
    return f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Weekly report · {esc(d["week_ending"])}</title><style>{CSS}</style></head><body><main>
<h1>WEEKLY REPORT · {esc(d["person"]).upper()} · WEEK ENDING {esc(d["week_ending"])}</h1>
<div class="sub">For reports.aerosenseaviation.com (due {esc(d["due"])}) and the todo site · {esc(d.get("sources_line", ""))}{evline}</div>
<nav><a href="#big">This week</a><a href="#last">Last week's commitments</a><a href="#a">A · My tasks</a><a href="#b">B · This week</a><a href="#c">C · Commitments</a><a href="#tasks">Task board</a><a href="#new">New cards</a><a href="#calls">Your calls</a></nav>

<h2 id="big">THIS WEEK IN ONE LOOK</h2>
<div class="card tablewrap"><table><tr><th>Area</th><th>Where it stands</th></tr>{glance}</table></div>

<h2 id="last">LAST WEEK'S COMMITMENTS <span class="sub">(top of the form: the site scores board tasks itself; you pick Met / Not met for the rest)</span></h2>
<div class="card tablewrap"><table><tr><th>Commitment</th><th>Scoring</th><th>What the record shows</th></tr>{last}</table></div>

<h2 id="a">A · MY TASKS <span class="sub">(the form's order · hours are estimates: adjust)</span></h2>
<div class="card m" style="font-size:13px">On submit the site posts each row's outcome on its card by itself (Done/Progressed: the evidence link; No work: the note). <b>Mark task done</b> is NOT saved with the report: it is an instant button that asks to confirm and closes the card at once.</div>
{''.join(rows)}

<h2 id="b">B · THIS WEEK</h2>
<div class="card"><div class="kv"><span>Other hours <b>{other_h:g}</b> <i>(est.)</i></span><span>Task hours <b>{task_h:g}</b></span><span class="tot">Total ≈ {task_h + other_h:g} h</span></div>
{block(f"{other_h:g}", "Other hours (not on a task)")}{block(oh.get("label", ""), f"What the other hours were ({len(oh.get('label', ''))}/{LABEL_MAX} chars)")}
<div class="m" style="font-size:13px;margin:4px 0 10px">The detail, for you: {esc(oh.get("detail", ""))}</div>
{block(d["blockers"], "Blockers — what stopped or slowed you, and since when")}{block(d["needs_from_duran"], "What you need from Duran — decisions, purchases, access")}
<div class="m" style="font-size:13px">{esc(d.get("measured", ""))}</div></div>

<h2 id="c">C · NEXT WEEK'S COMMITMENTS <span class="sub">(pick the task in the dropdown, then paste)</span></h2>
<div class="card tablewrap"><table><tr><th>Task (dropdown)</th><th>Due day</th><th>Done means…</th></tr>{commits}</table></div>

<h2 id="tasks">TASK BOARD <span class="sub">(todo.aerosenseaviation.com/tasks)</span></h2>
<div class="card"><b>Close these (Done):</b><ul>{close}</ul>
<div class="m" style="margin-top:6px">Close each with the report row's <b>Mark task done</b> button (confirm the dialog) or the card's <b>Complete</b> button on the board. Never use Edit to close: on some categories the Edit form wipes fields.</div>
<b style="display:block;margin-top:10px">Suggest the owners close:</b><ul>{sclose}</ul></div>
<h3>Comments to post (one per card)</h3>
{comments or '<div class="m">none</div>'}

<h2 id="new">NEW CARDS (OPTIONAL)</h2>
<div class="card tablewrap"><table><tr><th>Title</th><th>Owner</th><th>Why</th><th></th></tr>{newt}</table></div>

<h2 id="calls">YOUR CALLS <span class="sub">(not for pasting)</span></h2>
<div class="card"><ul>{calls}</ul></div>
</main><script>{JS}</script></body></html>"""


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("input")
    ap.add_argument("-o", "--out")
    a = ap.parse_args()
    d = load(a.input)
    problems = validate(d) + check_links(d, a.input)
    if problems:
        sys.exit("report_input.json does not pass the live form's rules:\n- " + "\n- ".join(problems))
    out = Path(a.out) if a.out else Path(a.input).with_name(f"weekly_report_{d['week_ending']}.html")
    page = build(d)
    out.write_text(page, encoding="utf-8")
    print(out, f"{len(page) / 1024:.0f} KB, {len(d['rows'])} rows, {len(d['commitments'])} commitments")


if __name__ == "__main__":
    main()
