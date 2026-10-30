#!/usr/bin/env python3
"""Prove a saved copy of the weekly report matches the live site, field by field (SHA-256).

    python3 report_hash.py report_api.json > check.js

report_api.json is the text of GET https://reports.aerosenseaviation.com/api/report, saved verbatim. The script prints
a JavaScript snippet with the expected hashes inside. Run that snippet in the browser on a reports.aerosenseaviation.com
tab (Claude in Chrome: javascript_tool). It fetches /api/report again, hashes the same fields and returns only the
differences, so the answer fits the tool's short output: {"fields": N, "mismatches": []} means the copy is exact.
"""
import hashlib
import json
import sys
from pathlib import Path


def h(s):
    return hashlib.sha256((s or "").encode("utf-8")).hexdigest()[:16]


def num(x):
    x = float(x or 0)
    return int(x) if x.is_integer() else x


def expected(r):
    e = {"label": h(r.get("hours_other_label")), "blockers": h(r.get("blockers")), "needs": h(r.get("needs_from_duran")),
         "nums": [num(r.get("hours_other")), num(r.get("hours_total")), r.get("revision"), r.get("status")]}
    for k, it in enumerate(r["items"]):
        e[f"i{k}"] = [it["id"], it.get("outcome"), num(it.get("hours")), h(it.get("note")), h("|".join(it.get("evidence_urls") or []))]
    for c in r.get("commitments", []):
        e[f"c{c['position']}"] = [c.get("task_id"), h(c.get("text")), h(c.get("done_means")), c.get("due_day")]
    e["prev"] = [c.get("met") for c in (r.get("previous") or {}).get("commitments", [])]
    return e


JS = """const EXP = %s;
const h = async s => [...new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(s || '')))].map(b => b.toString(16).padStart(2, '0')).join('').slice(0, 16);
const num = x => { x = Number(x || 0); return x; };
const r = (await (await fetch('/api/report', {credentials: 'include'})).json()).report;
const got = {label: await h(r.hours_other_label), blockers: await h(r.blockers), needs: await h(r.needs_from_duran),
  nums: [num(r.hours_other), num(r.hours_total), r.revision, r.status]};
for (let k = 0; k < r.items.length; k++) { const it = r.items[k];
  got['i' + k] = [it.id, it.outcome, num(it.hours), await h(it.note), await h((it.evidence_urls || []).join('|'))]; }
for (const c of r.commitments) got['c' + c.position] = [c.task_id, await h(c.text), await h(c.done_means), c.due_day];
got.prev = ((r.previous || {}).commitments || []).map(c => c.met);
const keys = [...new Set([...Object.keys(EXP), ...Object.keys(got)])]; let fields = 0; const bad = [];
for (const k of keys) { const a = JSON.stringify(EXP[k]), b = JSON.stringify(got[k]);
  fields += Array.isArray(EXP[k]) ? EXP[k].length : 1; if (a !== b) bad.push(k + ': saved ' + a + ' live ' + b); }
JSON.stringify({fields, mismatches: bad, items: r.items.length, commitments: r.commitments.length, status: r.status, revision: r.revision})"""


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    raw = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    print(JS % json.dumps(expected(raw.get("report", raw)), ensure_ascii=False))


if __name__ == "__main__":
    main()
