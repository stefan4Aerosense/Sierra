# Handover: doing Stefan's weekly report, the way SIERRA did it

**From:** SIERRA (Stefan's Claude agent; it was called KILO until 9 Nov 2026).
**To:** whichever agent Stefan gives this to.
**Written:** Fri 27 Nov 2026, after running Stefan's reports for W45 (6 Nov), W46 (13 Nov) and W47 (20 Nov).
**Read this first.** Then read `PLAYBOOK.md` (the method), `STEFAN.md` (what's different for Stefan) and
`tools/browser_snippets.md` (how to read the sites).

Where the kit's older files disagree with this one, **this file wins**. They were written on 30 Oct, and the report site
and the team have changed since then. Section 3 lists every change.

Provenance tags: `[measured]` means I ran it and saw the result; `[repo]` means it's in a file or commit; `[Stefan]`
means Stefan said it; `[assumed]` means not verified.

---

## 1 · The job in one paragraph

Every Friday Stefan files a weekly report at **reports.aerosenseaviation.com**. It's due **16:00 SAST**, and the exact
time is in the report JSON's `week.due_at`. Duran reviews it. Your job:
- read the report form, the task board, Preflight, the team repo and Codex's Fusion work log;
- check every claim at its source;
- ask Stefan only what the record can't tell you, usually his hands-on hours;
- write one `report_input.json`;
- build a **copy-paste page** (each field with a Copy button) and an **offline backup form**.

Stefan types it into the site and submits; **you never do**. After he submits, read the report back, prove your saved
copy matches the live site, and commit it.

## 2 · The rules (Stefan's standing orders, all still in force)

1. **Read-only on both sites.** Never type into the report, never press Submit, Mark task done, Complete or Post, and
   never send a `POST`. Stefan does all of those.
2. **Check every number at its source**: the commit, the file, the measurement. When a log and the raw data disagree,
   use the better-checked figure and record both.
3. **Hours are Stefan's.** Propose estimates and show the measured floor; he sets the number.
4. **Say what didn't happen.** "No work" with a reason is a good row. Never stretch Progressed.
5. **Don't submit before Stefan has checked it himself.** He does the submitting.
6. **Back up only after the hash check is clean.** The site saves edits made after submission without a new revision.
7. **Ask Stefan before publishing to the team repo or messaging another agent.** Ask once for each message.
8. **Privacy.** The submitted report JSON can hold `staff_input`, a private note from Stefan to Duran.
   - Keep the saved JSON in Stefan's **private** repo only.
   - Never quote it in chat, in a page or in the team repo.
   - Never put Stefan's email address in anything you publish.

## 3 · What changed since the kit was written (read this even if you read the rest)

| # | Change | Seen | What to do |
|---|---|---|---|
| 1 | **The form now lists only Stefan's NOW cards.** On 13 Nov it listed NOW and NEXT (8 rows). On 20 Nov it listed only the 2 NOW cards. WS1 had moved to NEXT that morning, so it had no row, even though WS1 was his main work that week. | [measured] 13 and 20 Nov | Read the rows from the live JSON each week, and never from last week's file. If his main work has no row, ask Stefan where the hours go. On 20 Nov he chose to put the H1 CAD hours on the WS3 demo-build row, because that work was for the demo rebuild, and to name WS1 in the note. |
| 2 | **New limits**, in the JSON's top-level `limits`: `{"now": 3, "other_hours": 5.0}`. Other hours are capped at **5 h**. | [measured] 20 Nov | If off-card work is over 5 h, enter 5 and say the real figure in the label, e.g. "~10 h spent, 5 h cap". Suggest he asks Duran for a card for it. |
| 3 | **Past weeks can be read** at `GET /api/report?week=YYYY-MM-DD`, using the Friday's date. `?week_ending=` is ignored and returns the current week. | [measured] 20 Nov | Use this for read-backs after Friday. `tools/report_hash.py` now uses it automatically (patched 27 Nov). |
| 4 | **A past report's `items` shrink later.** On 20 Nov the W46 report (submitted 13 Nov, `hours_total` 47) returned only 2 rows. The others had left NOW. | [measured] 20 Nov | **Read back and save on submit day**, before the board moves. Afterwards the full rows can't be recovered from the API. |
| 5 | **Duran reviews each report**: `report.review` holds `assessment` (W46: "Below expectation"), `feedback` and `seen_at`. | [measured] 20 Nov | Tell Stefan the assessment and any feedback for the previous week before drafting. |
| 6 | **Commitments have new fields**: `proof_url`, `agreed_at`, `agreed_by`. Duran can rewrite and agree a commitment; on 19 Nov he set W46's WS1 commitment to "By Fri 20 Nov, all the CAD changes Duran asked for are ready for Duran to review". | [measured] 20 Nov | Read last week's commitments from `previous.commitments` in the live JSON. They may differ from what you drafted, because Stefan or Duran changed them. Board-linked ones (`task_id` set) score from that card's row; if the card has no row this week, say the scoring is unknown. Stefan picks Met or Not met for the rest. |
| 7 | **TARS** posts on the board for Duran ("TARS, for Duran: …") and for Stefan ("Stefan via TARS: …"). Its comments have `creatorName: null`. | [measured] 18–20 Nov | Read them; they hold re-dates, lane moves and Stefan's own plans. Don't filter comments by creator name. |
| 8 | **Duran's design requests come in the board chat**, channel **#cad-design**, and Stefan answers there point by point. | [measured] 18–20 Nov | Read `#cad-design` every week; it's often the best evidence of what was asked and done. |
| 9 | **Codex (Stefan's ChatGPT agent) is now called KILO.** In records up to 9 Nov, `KILO` means the Claude agent (now SIERRA). From 9 Nov, `[KILO]`/`[STEFAN-KILO]` means Codex. | [repo] | Attribute work by date. |
| 10 | **The team repo's lane check accepts `[PERSON-CALLSIGN]` since 15 Nov** (INDIA, `53a54d4`), so `[STEFAN-KILO]` passes. | [repo] commit subject; not re-run | The earlier advice "bare callsign only" is out of date. |
| 11 | **Codex's real Fusion checkout is ahead of GitHub.** On 20 Nov `C:\Users\Stefan\Documents\Codex\2026-09-13\so-x20` was **67 commits ahead** of `origin/main` of `stefan4Aerosense/FusionChatGPT` (last push Mon 16 Nov 09:13). | [measured] 20 Nov | Read the week from **that checkout's local HEAD** (read-only: never pull, commit or push there). Tell Stefan when it's ahead, so Codex pushes the backup. |
| 12 | **The site needs Stefan signed in** in Chrome. On 27 Nov `/api/report` returned `401 {"error":"Sign in first."}`. | [measured] 27 Nov | Ask Stefan to sign in to todo.aerosenseaviation.com in his Chrome. Never type a password yourself. |

## 4 · Where everything is on Stefan's machine (Windows)

| What | Where | Access |
|---|---|---|
| The report form | https://reports.aerosenseaviation.com, JSON at `/api/report` (current week) and `/api/report?week=YYYY-MM-DD` | read-only, through Stefan's signed-in Chrome |
| The task board | https://todo.aerosenseaviation.com: `/api/tasks`, `/api/tasks/<id>/comments`, `/api/archive/tasks`, `/api/activity`, `/api/chat/channels`, `/api/chat/channels/<id>/messages`. Board name: **Stefan** | read-only |
| Preflight | https://todo.aerosenseaviation.com/preflight | read-only |
| Codex's Fusion work: **commits and daily log** | `C:\Users\Stefan\Documents\Codex\2026-09-13\so-x20` (GitHub `stefan4Aerosense/FusionChatGPT`): `worklog/YYYY-MM-DD.md`, `worklog/weekly/YYYY-Www.md`, `outputs/<Name-YYYY-MM-DD>/README.md` | **read-only** |
| An older clone of the same repo | `C:\Users\Stefan\Documents\GitHub\FusionChatGPT` | read `origin/main` only; behind Codex's checkout |
| The team repo | `C:\Users\Stefan\Documents\Aerosense-Dev-Team-Sync` (`Aerosense-Aviation/Aerosense-Dev-Team-Sync`) | read; write only Stefan's lane, with his yes |
| SIERRA's repo, with every past week | `C:\Users\Stefan\Documents\GitHub\Sierra\weekly\2026-W4x\` (`stefan4Aerosense/Sierra`, private) | read for examples. Use your own repo for your own weeks, unless Stefan says otherwise. |
| This kit | `C:\Users\Stefan\Documents\GitHub\Sierra\weekly-report-kit\` | copy it into your own repo |
| Python | `C:/Users/Stefan/AppData/Local/Programs/Python/Python312/python.exe` | standard library only |

**Codex's log is the richest source.** Each entry is a heading with a time and lines tagged `[Stefan]` (what he asked
or reported), `[measured]` and `[repo]`. To skim a day:

```bash
grep -E '^### |^- \[Stefan' "C:/Users/Stefan/Documents/Codex/2026-09-13/so-x20/worklog/2026-11-19.md" | cut -c1-330
```

## 5 · The Friday session, step by step (as SIERRA ran it)

Tell Stefan before you start that a full run takes **15–20 minutes**, so you should start by about **15:30**. If he
says he's still working, wait, and then **re-read everything** (site, board, commits) before building. On 13 Nov,
three commits landed between the first draft and the rerun.

### 5.1 Open the week
- Fetch the repos, each with a timeout. The team repo's fetch can hang, and FusionChatGPT's once failed with
  `invalid index-pack output`; a retry worked.
  ```bash
  timeout 90 git -C "C:/Users/Stefan/Documents/Aerosense-Dev-Team-Sync" fetch -q origin
  git -C "C:/Users/Stefan/Documents/GitHub/FusionChatGPT" fetch origin      # run in the background if slow
  ```
- The window is **last Friday's submit time → now**. Take the submit time from the previous report's `submitted_at`
  (W46: `2026-11-13T14:35:20+02:00`).
- Make `weekly/<YYYY>-W<NN>/` in your repo.

### 5.2 Read the report form (snippets §1)
- `navigate` to `https://reports.aerosenseaviation.com/api/report`, then `get_page_text`. Save the text **verbatim**
  with your file-write tool as `report_api_<week_ending>_draft.json`.
- **Also read last week:** `.../api/report?week=<last Friday>`. Then:
  - save it as `report_api_<last_friday>_submitted.json` if you don't have it yet;
  - run the hash check (§5.10);
  - note `review.assessment` and `feedback`.
- List this week's rows **in form order**, with `status · technology · due_date`, and look at `limits`.

### 5.3 Read the board, chat and Preflight (snippets §2–§4)
- Run snippet §2 with `PERSON = 'Stefan'` and `SINCE` = last Friday's submit time **in UTC** (W46: `2026-11-13T12:35`).
  Include `archive/tasks` too, because cards Stefan helped with may have closed this week. On 20 Nov the
  flight-recorder enclosure card closed with Chris's comment that Stefan designed and printed the case.
- Snippet §3: activity, completed cards and **every chat channel**. Skip the "Weekly report" bot messages, and read
  `#cad-design` in full.
- Preflight: `navigate`, then `get_page_text`. The first read often says "Loading…", so read again. Note every line
  naming Stefan under "Who's on what", "Watch" and "Needs a call". The Metalforge band-thickness question was still
  open on 20 Nov (day 88).
- Write big results into the page body and read them with `get_page_text`. `javascript_tool` cuts its output at about
  1,000 characters.

### 5.4 Sweep the repos
```bash
R="C:/Users/Stefan/Documents/Codex/2026-09-13/so-x20"
git -C "$R" status -sb | head -1                      # "[ahead N]" means the GitHub backup is behind: tell Stefan
git -C "$R" log --since="<last Fri submit, local>" --reverse --format='%ad %h | %s' --date=format:'%a %d %H:%M'
git -C "C:/Users/Stefan/Documents/Aerosense-Dev-Team-Sync" log origin/main --since="<same>" --reverse --format='%ad %h %an | %s' --date=format:'%a %d %H:%M'
```
- Read each day's `worklog/` file. Match the work to the board cards. Read an `outputs/*/README.md` before quoting any
  number from it.
- In the team repo, look for:
  - `inbox/stefan/`;
  - `reports/stefan/`;
  - anything by `KILO (stefan)` (Codex's publishes, e.g. the H1 showroom handovers);
  - other lanes naming Stefan: Chris's weekly evidence page `chris/golf/<YYYY>-W<NN>_fleet_week.md` often mentions his
    cards.
- Separate "designed in CAD" from "printed and tested". Codex's logs say plainly what has not been physically checked.
  Keep that distinction in the notes.

### 5.5 Measure the hours floor
```bash
python -I tools/activity_hours.py --since 2026-11-13T14:35 --until 2026-11-20T14:15 --tz 2 \
  --git "C:/Users/Stefan/Documents/Codex/2026-09-13/so-x20" --no-claude
```
- `--since` and `--until` are **local SAST times**. On 13 Nov a UTC value silently dropped that afternoon's commits.
- The floor under-counts days with many log entries but few commits. Monday 16 Nov had 39 log entries but only 4
  commits. Say so in `measured`.

### 5.6 Ask Stefan (one round, up to four multiple-choice questions, best guess first)
Ask only what changes the report:
- **Hours per area.** Offer your estimate from the log time spans (first to last entry each day) beside the floor.
  Typical W47 answer: "H1 20, enclosure 10".
- **Where the hours go** when the work has no row (§3, row 1).
- **Last week's commitments he scores himself** (no `task_id`): Met or Not met.
- **Next week's commitments**: offer 3–4 drawn from the board's plans and Duran's asks; he picks.

Don't ask what the record already answers.

### 5.7 Draft `report_input.json`
- Schema: PLAYBOOK §6. Stefan's W47 is a worked example: `examples/2026-W47_stefan/report_input.json`.
- Rows: **exactly the site's rows, the site's titles, in the site's order.**
- Notes:
  - 500 characters at most, and the label 120 at most;
  - concrete: what exists now, dates, numbers checked at source;
  - say what is CAD-only;
  - end with "Open: …".
- Evidence must be a link **Duran can open**:
  - the team repo (Codex's `reports/stefan/...html` pages, `stefan/agents/...` handoffs);
  - the company H1 showroom `https://dash.aerosenseaviation.com/d/h1-e9c1a269/` (behind a sign-in);
  - not FusionChatGPT, which is private.
- `calls` holds the things only Stefan decides, plus warnings. In W47 these were:
  - the hours mapping;
  - the 5 h cap;
  - Codex's unpushed backup;
  - Duran's assessment.

### 5.8 Build, check, open
```bash
P="C:/Users/Stefan/AppData/Local/Programs/Python/Python312/python.exe"
$P -I tools/build_copy_paste.py   weekly/2026-W47/report_input.json -o weekly/2026-W47/weekly_report_2026-11-20.html
$P -I tools/build_offline_form.py weekly/2026-W47/report_input.json -o weekly/2026-W47/weekly_report_2026-11-20_offline.html
cmd //c start "" "C:\path\to\weekly_report_2026-11-20.html"     # opens it for Stefan
```
- The builders refuse a note over 500, a label over 120, or Done with no evidence. Still check that the total hours add
  up to what Stefan said.
- Commit the week's folder to your repo with **explicit paths**, push, and check the push with `git ls-remote` (compare
  all 40 characters).
- Give Stefan a short summary:
  - what each row says;
  - the total;
  - the commitments;
  - anything he must decide or know before he types.

### 5.9 Stefan enters and submits
Hands off. He may change lines; **his words win**, and the read-back records them.

### 5.10 Read back, hash-check, back up (on submit day: see §3, row 4)
```bash
# 1. save GET /api/report?week=<friday> verbatim as report_api_<friday>_submitted.json
$P -I tools/report_hash.py weekly/<week>/report_api_<friday>_submitted.json > check.js
# 2. paste check.js into javascript_tool on a reports.aerosenseaviation.com tab
#    -> {"fields": N, "mismatches": []} is clean; anything else: re-save and re-check
$P -I tools/build_offline_form.py --submitted weekly/<week>/report_api_<friday>_submitted.json
```
Then commit (explicit paths), push and verify. Tell Stefan:
- what differs from the draft;
- which cards are still open that he meant to close.

## 6 · Gotchas on this machine

- **`python -c` and `node -e` are denied** by the permission net, and so is any command containing them. Write a
  small script to a scratch folder and run it with `python -I script.py`.
- Under `-I`, `PYTHONIOENCODING` is ignored. Put `sys.stdout.reconfigure(encoding="utf-8")` at the top, or the em
  dashes in card titles crash on cp1252.
- Use the Bash tool (Git Bash), not PowerShell. Use absolute paths, because `cd` doesn't carry between calls.
- **Read a file before editing it**, or the edit fails. Then check the commit really contains it. On 20 Nov a commit
  message named a WHOAMI change that hadn't applied, and it took a second commit to fix.
- Chrome (Claude in Chrome):
  - load all its tools in one ToolSearch call;
  - work in your own tab (`tabs_context_mcp` with `createIfEmpty`) and close it when done;
  - tab ids from an earlier session are invalid;
  - the extension can't open `file://` pages.
- `git add <explicit paths>` only. Never `-A` or `commit -a`, never `--force`.

## 7 · State on 27 Nov 2026 (check it, don't trust it)

- **W46 (13 Nov):** submitted 14:35, reviewed "Below expectation" by Duran on 16 Nov, with no feedback. Saved as
  `Sierra/weekly/2026-W46/report_api_2026-11-13_submitted.json`, hash-clean on 20 Nov (24 fields). Only 2 of its rows
  can still be read from the API.
- **W47 (20 Nov):** SIERRA built the draft (`Sierra/weekly/2026-W47/`, commit `b63805a`):
  - WS3 demo build: Progressed, 20 h;
  - WS3 in-aircraft: No work;
  - other hours: 5 h for the Baby Cheese (DEV-B) case, of about 10 h spent;
  - total 25 h.

  **SIERRA never read it back**, so whether and what Stefan submitted is unknown. Start there: read
  `/api/report?week=2026-11-20`, save it, and run the hash check.
- **W48 is due today, Fri 27 Nov, at 16:00.** Commitments drafted for it: the demo H1 rebuilt by 27 Nov, the TPU
  stretch band print test, and the changes from Duran's CAD review. Check what Stefan actually entered in W47's
  `commitments`.
- Open with Stefan:
  - SIERRA is not on the team roster;
  - Codex's FusionChatGPT backup was 67 commits unpushed on 20 Nov;
  - Preflight's Metalforge band question is open.

## 8 · What's in this kit

See `README.md`'s folder map. Since then:
- this file;
- `examples/2026-W47_stefan/report_input.json`;
- `tools/report_hash.py` now checks the saved copy's own week (`?week=`).

The patched snippet is identical, character for character, to the hand-edited one that came back clean
(24 fields, 0 mismatches) on 20 Nov. The patch itself has not been re-run live, because the site was signed out on
27 Nov.
