# The weekly report playbook

**For:** a Claude agent that prepares one person's weekly report at Aerosense.
**Written by:** GOLF (Chris's orchestrator agent), 30 Oct 2026, from the runs on 23 and 30 Oct that Chris submitted.
**Follow it in order.** Each step names its inputs, its outputs and its check. Shell commands are given for macOS; on
Windows use `python` for `python3` and `\` paths.

---

## 1 · What this is for

Every Friday each person files a weekly report at **reports.aerosenseaviation.com**. It has their board tasks with an
outcome, hours, evidence and a note; blockers; what they need from Duran; and next week's commitments.

The company's own AI is **Duran's company Claude**, in the `aerosense-ops` repo. It runs every morning at about 04:50
SAST. It takes in the task board, chat, mail, calendar, Drive and the **Aerosense-Dev-Team-Sync** repo ("the dev
repository"), and publishes the **Preflight** page and the gate counts.

**It cannot see your private working repos or what you did with your hands.** The weekly session exists to pull
everything the person and their agents did that week out of those places and into the three surfaces the company AI
reads:

1. **the weekly report**, which also posts one comment per task card when the person submits;
2. **the task board**: card comments, and the cards that close;
3. **the team repo**: a weekly evidence page in the person's lane, which every report row links to.

The outcome each week:
- a report the person submits by **Fri 16:00 SAST**, every line true and checked;
- a backup copy of what they submitted;
- the board brought up to date.

## 2 · The rules

1. **Read-only on the sites.** The agent never types into the report, never presses Submit, Mark task done, Complete
   or Post, and never sends a `POST`. The person does those. The only exception is one action the person explicitly
   asks for, one at a time.
2. **Every number is checked at its source** (the commit, the file, the measurement) before it goes in a note. When an
   agent's figure and the raw data disagree, the better-checked one goes in, and both are recorded.
3. **The reader is Duran.** An evidence link must open for him. Private repos don't, so evidence goes on a page in the
   team repo (§8) and the report links to its headings.
4. **Say what didn't happen.** "No work this week" with the reason is a good row. Never stretch Progressed.
5. **Hours are the person's.** The agent proposes estimates and shows the measured floor (§5.6). The person sets the
   number.
6. **The person's own words win.** If they change a draft line, the backup records their version.
7. **Messages to other agents** need the person's yes for each message.

## 3 · The week's clock (SAST)

| When | What |
|---|---|
| Mon–Thu | Daily work log: agents' sessions plus the person's hands-on work (for Stefan: `STEFAN.md`, `CHATGPT_BRIDGE.md`) |
| Thu 08:00 | The report for this week opens on the site |
| Fri morning | **The session:** gather → draft → build → publish → the person enters |
| Fri 16:00 | Due. After that the site marks it Late |
| Mon 09:00 | Marked Missing |

The exact times for any week are in `GET /api/report` → `week.opens_at`, `due_at` and `missing_at`.

## 4 · Where everything is

| Source | Where | How to read it | What you take from it |
|---|---|---|---|
| The report form | https://reports.aerosenseaviation.com (also inside the board at `todo.aerosenseaviation.com/weekly-report`) | `tools/browser_snippets.md` §1: `GET /api/report` | the person's rows (one per board task they own), last week's commitments, the deadline |
| The task board | https://todo.aerosenseaviation.com/tasks | snippets §2 and §3: `/api/tasks`, `/api/tasks/<id>/comments`, `/api/archive/tasks`, `/api/activity`, `/api/chat/channels/<id>/messages` | each card's description, owner and comments; what moved since last Friday |
| Preflight | https://todo.aerosenseaviation.com/preflight | snippets §4 | what the company AI is waiting on from this person ("Watch", "Needs a call") |
| The team repo | `github.com/Aerosense-Aviation/Aerosense-Dev-Team-Sync` (local clone) | `git fetch` then `git log --since` | the lanes' published reports, messages to this person, others' work that touches theirs |
| The person's agents' repos | their own repos (for Chris: `github.com/ChrisAerosense/<repo>`; for Stefan: `kilo` and `FusionChatGPT`) | `git log --since`, read the changed files | what was actually built, with commit SHAs |
| The work log | Stefan: `FusionChatGPT/worklog/` | read the week's files | the person's hands-on work and the ChatGPT sessions |
| Session records | `~/.claude/projects/<project>/*.jsonl` | `tools/activity_hours.py` | the floor for hours; what was asked of each agent |
| **The person** | chat | ask (§5.5) | hours, hands-on work, anything the record misses |

**Reading the sites** needs **Claude in Chrome** (the browser extension) in the person's own Chrome, where they are
signed in to todo.aerosenseaviation.com. The report site signs in through the board. Read
`tools/browser_snippets.md` before the first call: it covers the extension's three quirks.

## 5 · The session, step by step

### 5.1 Open the week
- `git fetch` every repo you will read (`git fetch -q origin` in each). Note today's date and last Friday's submit time:
  that is the window, usually `SINCE = last Friday 16:00`.
- Make the week's folder in your own repo, e.g. `weekly/2026-W45/`. Every file below goes there.

### 5.2 Read the report form
- Snippet §1. Save the JSON verbatim as `report_api_<week_ending>_draft.json`.
- List the rows **in form order**, with their `meta` line (`status · technology · due date`).
- List last week's commitments: the board-linked ones (`task_id` set) score automatically (Met only if that task's
  outcome this week is Done); for the rest the person picks Met or Not met.

### 5.3 Read the board and Preflight
- Snippet §2 for the person's cards with every comment (`[NEW]` marks this week's).
- Snippet §3 for what moved on the whole board since last Friday.
- Snippet §4 for Preflight.
- **Check:** "nothing moved" is a finding too. On 30 Oct the board had no comment, chat or card move for a week, and
  the report said so.

### 5.4 Sweep the repos
For each repo, list the week's commits and read the ones that matter. Skip auto-commits such as "SessionEnd handoff":

```bash
git -C <repo> log origin/main --since="2026-10-23 16:00" --reverse --format='%ad %h %an | %s' --date=format:'%a %d %H:%M'
git -C <repo> show --stat <sha>          # what a commit touched
```

- **The team repo:** messages to the person (`inbox/<person>/`); their own lane (`reports/<person>/`, `<person>/`);
  other lanes' work that mentions them. Example: LIMA sent KILO a GPS module STEP file on 29 Oct.
- **The person's agents' repos:** what was built, with SHAs. Read status files (`.agent/status.md` NOW blocks, work
  logs) for state the commits don't say.
- **Session records** (optional): what the person asked each agent, day by day. On 30 Oct this showed that Chris wired
  and probed the second ADAU1860 himself on Tuesday: hands-on work no commit recorded.
  ```bash
  python3 tools/activity_hours.py --since 2026-10-23T16:00 --until 2026-10-30T12:00
  ```
- **Leave out what isn't company work.** On 30 Oct Chris's A320 simulator project was left out because he called it
  unrelated.

### 5.5 Ask the person
Use one short message, with your best reading so they only correct it. Ask only what changes the report:
- **Hours:** per area, the hands-on time the record can't see.
- **Decisions:** close a card or keep it open; which 3–5 commitments for next week.
- **Facts only they know:** was the support request sent; did the part arrive.

Ask with up to four multiple-choice questions, best option first. Don't ask what the record already answers.

### 5.6 Draft: `report_input.json`
Write one file. The schema is §6, and the worked example is `examples/2026-W44_chris/report_input.json`.

The rules for each field (the site and Duran enforce them):

| Field | Rule |
|---|---|
| Outcome | `Done` / `Progressed` / `Blocked` / `No work`. Done means finished, with evidence. Done on a card that was finished earlier closes it. |
| Evidence | Required for Done and Progressed. **A link Duran can open**: this week's evidence page in the team repo (§8) plus `#heading`, or a team-repo report. |
| Hours | A number. The agent's estimate from the record; the person changes it. |
| Note | **500 characters max** (the site silently keeps only the first 500 and still says Saved). Required if Blocked. Say what happened, the key number and its source, and what is not done yet. |
| Mark task done | Set it on rows whose card should close. **It is not saved with the report** (§7). |
| Other hours | Hours not on a task, plus a label of **120 characters max** (same silent cut). |
| Blockers | One line each: what stopped or slowed you, **and since when**. |
| Needs from Duran | Numbered decisions, purchases, access. Mark an item `(carried)` if it was asked last week. |
| Commitments | **3–5**. Each is a board task or "Something not on the board…" with text, a due day Mon–Fri, and a **done-means you can show on Friday** (a number, a file, a test passed). |
| Last week | For each of last week's commitments, what the record shows. Your suggested Met / Not met for the ones not on the board. |
| Board | Cards to close; one comment per card that needs more than the auto-comment; new cards to suggest. |

The hours floor: `tools/activity_hours.py` gives the merged screen time with agents (for Stefan, add
`--git <FusionChatGPT>`). Show it in `measured` so the person sees the basis. Hands-on time comes on top.

### 5.7 Write the evidence page, then build
1. Write `evidence_page_<week>.md` from `templates/evidence_page.TEMPLATE.md`:
   - one `###` heading per report row that has evidence;
   - each claim tagged `[measured]` or `[repo]`, with its commit SHA or file;
   - the status banner (§8).
2. Build, from the folder holding `report_input.json`:
   ```bash
   python3 <kit>/tools/build_copy_paste.py report_input.json     # -> weekly_report_<week_ending>.html
   python3 <kit>/tools/build_offline_form.py report_input.json   # -> weekly_report_<week_ending>_offline.html
   ```
   Both refuse to build if anything breaks the site's rules: a note over 500, a label over 120, a Done without
   evidence, 3–5 commitments, an evidence anchor missing from the page (set `evidence_page.local_md`).
3. **Check:**
   - open the copy-paste page and look at it;
   - open the offline form and press **Check report**: it should say "Passes the live form's checks";
   - grep both outputs and the page for secrets (passwords, keys, Wi-Fi details): none may appear.

### 5.8 Publish the evidence page (with the person's yes)
Follow §8. **Ask first:** it publishes to the company repo. Verify the push landed, then give the person:
- the copy-paste page;
- the offline form (their backup if the site breaks);
- a short summary: the week in a few lines, the cards to close, the hours basis.

### 5.9 The person enters and submits
They paste from the copy-paste page into the live form and submit. Then, on the live form, either they press **Mark
task done** on each card to close and confirm the dialog, or they use the board's **Complete** button (§7).

### 5.10 Read back and back up
1. Snippet §1 again. Save as `report_api_<week_ending>_submitted.json`.
2. Prove it is exact:
   ```bash
   python3 <kit>/tools/report_hash.py report_api_<week_ending>_submitted.json > check.js
   ```
   Run `check.js` on a reports.aerosenseaviation.com tab. It must return `"mismatches": []`. If it doesn't, the person
   edited after submitting: re-save and re-check.
3. Build the backup:
   ```bash
   python3 <kit>/tools/build_offline_form.py --submitted report_api_<week_ending>_submitted.json
   ```
4. Snippet §5: confirm the auto-comments landed and **which cards actually closed**.
5. Tell the person:
   - any differences between their submission and the draft (hours they changed, lines they dropped);
   - the cards still open that they meant to close;
   - where the backup is.

### 5.11 Close the week
Commit the week's folder with explicit paths (no `git add -A`), push, and verify the push landed. Update your
memory/status file: what was submitted, what is open, and what the next session should check first.

## 6 · `report_input.json`, the one file every builder reads

```jsonc
{
  "schema": "aerosense-weekly-report-input/1",
  "person": "Stefan …",                       // as the site shows it
  "week_ending": "2026-11-06",                // the Friday, ISO
  "due": "Fri 6 Nov, 16:00",
  "sources_line": "built by KILO from …",     // one line, shown under the title
  "evidence_page": {"url": "https://github.com/Aerosense-Aviation/Aerosense-Dev-Team-Sync/blob/main/reports/stefan/KILO_2026-W45_week.md",
                    "label": "reports/stefan/KILO_2026-W45_week.md (team repo)",
                    "local_md": "evidence_page_2026-W45.md"},   // lets the builder check every #anchor
  "glance": [{"area": "H1 shell", "status": "green|amber|red", "text": "…"}],
  "last_week": [{"text": "…", "done_means": "…", "task_title": "<row title> or null", "pick": "met|not met|null", "reading": "what the record shows"}],
  "rows": [{"title": "<exactly as the site lists it>", "meta": "now · H1 Headset · due 2026-11-13",
            "outcome": "Done|Progressed|Blocked|No work", "hours": 6, "evidence": ["<url>#<anchor>"],
            "mark_done": false, "note": "≤500 chars"}],
  "other_hours": {"hours": 4, "label": "≤120 chars", "detail": "longer, for the person only"},
  "blockers": "- … (since …)\n- …",
  "needs_from_duran": "1. …\n2. …",
  "commitments": [{"task": "<row title> or OTHER", "text": "only for OTHER", "due_day": "Mon..Fri", "done_means": "…"}],
  "board": {"close": ["<row title>"], "suggest_close": [{"title": "…", "why": "…"}],
            "comments": [{"card": "<title>", "outcome": "Done → Complete|Progressed", "comment": "…"}],
            "new_cards": [{"title": "…", "owner": "…", "why": "…"}]},
  "calls": ["decisions only the person can make, not for pasting"],
  "measured": "the hours floor and how it was measured"
}
```

Rows must be **the site's rows, in the site's order, with titles exactly as listed**: the form matches by title.

## 7 · How the site behaves (observed 23 and 30 Oct 2026)

- **Submit** sets `status: submitted`, a `revision` number and `timeliness` (On time / Late). The site also saves a
  Google Drive copy (`drive_file_id`).
- **On submit it posts one comment per row** on each card, from an account named **"Weekly report"**:
  - Done and Progressed rows: `Weekly report 30 Oct 2026 — Done — <evidence link>` (the link only, not the note);
  - No-work rows: `Weekly report 30 Oct 2026 — No work — <note>`.

  So the substance of a Done or Progressed row reaches the card only if the person posts a comment as well. That is
  what `board.comments` is for.
- **"Mark task done" is not part of the report.** It is an instant button: a browser `confirm()` dialog, then
  `POST /api/tasks/<id>/done`, and the button reads "Marked done". Nothing about it is saved in the report. If the
  person doesn't press and confirm it, the card stays open. "Flag needs review" works the same way
  (`POST …/needs-review`).
- **Edits after submitting are saved into the submitted report without a new revision.** On 30 Oct Chris changed one
  row's hours seven minutes after submitting: revision 1, status submitted, `updated_at` moved. The Drive copy made at
  submit may differ. Back up only after the hash check is clean.
- **Text limits:** a note keeps 500 characters and the other-hours label keeps 120. Longer text is cut while the
  footer still says Saved.
- **The board:**
  - each card row has Show comments / Edit / Complete / Delete;
  - **Complete** archives the card (closes it);
  - **never use Edit to close a card:** on cards whose category is Technical Testing, Research or Meetings, the Edit
    form drops the category and technology fields;
  - after a `navigate` the first click is often swallowed;
  - inline comments only work on cards that already have one; zero-comment cards need the card's detail page;
  - pressing Enter in a comment box posts it, so don't also click Post (you get a duplicate).
- **Never trigger a `confirm()`/`alert()` from the agent:** it blocks the extension until a human dismisses it.

## 8 · Publishing the evidence page to the team repo

Repo: `Aerosense-Aviation/Aerosense-Dev-Team-Sync`. Rules: `README.md`, `reports/README.md`,
`reports/REPORT_STATUS_BANNER.md`, `inbox/README.md`. INDIA (the steward agent) checks every landing.

- **Where:**
  - Chris's fleet uses `chris/golf/<YYYY>-W<NN>_fleet_week.md`;
  - a person's lane uses `reports/<person>/<AGENT>_<YYYY>-W<NN>_week.md` **plus a row in `reports/<person>/MANIFEST.md`**;
  - one file per week.
- **Status banner** at the top, all eight fields: STATUS, Reviewed (date + callsign), Record state (`git log -1
  --format=%h -- record/`), Source (`repo@sha` of the page in your own repo), Supersedes, What changed, Next, and the
  `<!-- aerosense-report-status: current -->` marker.
- **Links:**
  - to other team-repo files, use relative links (`../../reports/john/…`);
  - check every link resolves (`ls` each path);
  - no `file://` links, no absolute `/` links, no external scripts.
- **Git discipline:**
  1. `git pull --rebase`;
  2. `git add <the exact paths>` (never `-A`, never `commit -a`);
  3. read `git status` before you commit;
  4. commit as `[CALLSIGN] publish: …`;
  5. push;
  6. verify with `git ls-remote origin refs/heads/main` (it must equal your HEAD).
- If the push prints `Bypassed rule violations … File path is restricted`, it landed. Tell the person: the repo's push
  protection is not enforced for that key.
- **No secrets, no CEO-confidential content** (fundraise, finance, legal, HR). Engineering detail is fine, and the repo
  is company-only.

## 9 · Acceptance checklist (all must be true before you hand over)

- [ ] Every row on the site has a row in `report_input.json`, same title, same order.
- [ ] Every Done/Progressed row has a link Duran can open; every anchor exists (the builder checked).
- [ ] No note over 500 characters, no label over 120 (the builder checked).
- [ ] Every number in a note was checked at source this session.
- [ ] Last week's commitments each have a reading from the record.
- [ ] 3–5 commitments, each with a checkable done-means and a due day.
- [ ] Hours are labelled as estimates, with the measured floor shown.
- [ ] Copy-paste page and offline form built and opened; "Check report" passes.
- [ ] Evidence page published (with the person's yes) and its push verified.
- [ ] After submit: hash check clean, backup built, auto-comments seen, closed cards confirmed.
- [ ] No secrets in any output.

## 10 · Mistakes we made, so you don't

- **Treating "Mark task done" as a checkbox** (30 Oct). It is an instant action, and five cards stayed open.
- **Backing up the moment the person said "done"** (30 Oct). They edited seven minutes later. The hash check caught it.
- **Quoting a figure from a status file without checking it.** A flag in a file is a claim; verify it at its source.
- **A clean count of zero is suspicious:** check that the filter matched the field you meant (e.g. comment dates, not
  card dates).
- **Long notes:** the site cut five notes mid-sentence on 23 Oct and still said Saved.
- **The extension's output limit** hid most of a JSON response; dump it into the page and read it with
  `get_page_text` instead.
- **Evidence links into private repos:** Duran can't open them. Publish to the team repo and link there.

## 11 · Worked example

`examples/2026-W44_chris/` is Chris's report for the week ending 30 Oct 2026, end to end:

| File | What it is |
|---|---|
| `report_input.json` | the draft, as built |
| `weekly_report_2026-10-30.html` | the copy-paste page |
| `weekly_report_2026-10-30_offline.html` | the offline form, draft mode |
| `evidence_page_2026-W44.md` | the evidence page; published as `chris/golf/2026-W44_fleet_week.md`, with the banner added |
| `report_api_submitted.json` | the report as submitted, in the site's `/api/report` shape; matched the live site 113/113 fields |
| `weekly_report_2026-10-30_offline_submitted.html` | the backup, submitted mode |

Rebuild the outputs to see the tools work:

```bash
cd examples/2026-W44_chris
python3 ../../tools/build_copy_paste.py report_input.json
python3 ../../tools/build_offline_form.py report_input.json
python3 ../../tools/build_offline_form.py --submitted report_api_submitted.json
```
