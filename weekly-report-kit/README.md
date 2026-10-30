# Weekly-report kit (Aerosense)

**What it is:** the method, tools and templates a Claude agent uses to prepare one person's Friday weekly report for
reports.aerosenseaviation.com. With it the agent:
- gathers the week from the task board, Preflight, the team repo, the person's own agents' repos and the person;
- builds a copy-paste page and an offline backup form;
- publishes an evidence page Duran can open;
- after the person submits, proves the backup matches the live report field by field.

**Built by:** GOLF (Chris's orchestrator agent), 30 Oct 2026, from Chris's weekly reports of 23 and 30 Oct.
**First handed to:** KILO, Stefan's Claude agent (see `HANDOVER_TO_KILO.md`).

## Start here

| If you are… | Read |
|---|---|
| an agent preparing someone's report | `PLAYBOOK.md`, start to finish, then the worked example |
| KILO, working for Stefan | `HANDOVER_TO_KILO.md` → `PLAYBOOK.md` → `STEFAN.md` → `CHATGPT_BRIDGE.md` |
| Stefan setting up ChatGPT | `CHATGPT_BRIDGE.md` §1–§2, then paste `templates/chatgpt_project_instructions.txt` into your ChatGPT Project |
| a human checking what the agent does | this page and `PLAYBOOK.md` §1–§3 |

## The loop, one screen

```
Mon–Thu   work log every day (agents' sessions + the person's hands-on work)       templates/worklog_day
Thu       the agents' weekly summary                                               templates/worklog_week
Fri am    1 read the report form      GET /api/report                              tools/browser_snippets.md §1
          2 read the board, Preflight  cards, comments, what moved                 §2 §3 §4
          3 sweep the repos            git log --since last Friday                 PLAYBOOK §5.4
          4 ask the person             hours, hands-on work, decisions              PLAYBOOK §5.5
          5 draft report_input.json    one file, the schema in PLAYBOOK §6          templates/report_input.TEMPLATE.json
          6 build                      copy-paste page + offline form               tools/build_copy_paste.py, build_offline_form.py
          7 publish the evidence page  team repo, with the person's yes             templates/evidence_page.TEMPLATE.md
Fri ≤16   8 the person enters and submits, then closes the Done cards
Fri pm    9 read back, hash-check, back up                                          tools/report_hash.py, build_offline_form.py --submitted
         10 commit the week, update memory
```

## Folder map

```
README.md                     this page
PLAYBOOK.md                   the method: sources, steps, field rules, site behaviour, publishing, checklist, pitfalls
STEFAN.md                     what changes for Stefan (ChatGPT + physical work)
CHATGPT_BRIDGE.md             how Claude and ChatGPT share a log and messages through the FusionChatGPT repo
HANDOVER_TO_KILO.md           the message that starts KILO off (copy-paste)
tools/
  build_copy_paste.py         report_input.json → the copy-paste page (refuses input the site would reject)
  build_offline_form.py       report_input.json → offline form (draft); --submitted report_api.json → backup
  offline_form_template.html  the form, with the live site's own stylesheet
  report_hash.py              saved /api/report JSON → a browser check that proves it matches the live report
  activity_hours.py           the hours floor from Claude session records and commit times
  wr_common.py                shared checks (500/120 limits, outcomes, commitments, evidence anchors)
  browser_snippets.md         the Claude in Chrome snippets, read-only, tested 30 Oct 2026
templates/
  report_input.TEMPLATE.json        the draft, blank
  evidence_page.TEMPLATE.md         the weekly evidence page, with the team repo's status banner
  worklog_day.TEMPLATE.md           one working day: hands-on work + agent sessions
  worklog_week.TEMPLATE.md          the week, per board task, ready for the Friday session
  chatgpt_project_instructions.txt  paste into the ChatGPT Project's instructions
  message_kilo_to_chatgpt.md        KILO's first bridge message to ChatGPT
examples/2026-W44_chris/      Chris's real week 44: input, both pages, the evidence page, the submitted report, the backup
```

## Requirements

- **Python 3.8+**, standard library only (macOS, Linux, Windows). On Windows type `python`.
- **Claude in Chrome**, connected in the person's Chrome, with the person signed in to todo.aerosenseaviation.com.
- **Git** with push access to `Aerosense-Aviation/Aerosense-Dev-Team-Sync` (see that repo's `aero-ssh-bridge/`).

## Quick test (30 seconds)

```bash
cd examples/2026-W44_chris
python3 ../../tools/build_copy_paste.py report_input.json
python3 ../../tools/build_offline_form.py report_input.json
python3 ../../tools/build_offline_form.py --submitted report_api_submitted.json
```

Three HTML files appear. Open them in a browser. In the offline form, **Check report** should say "Passes the live
form's checks".
