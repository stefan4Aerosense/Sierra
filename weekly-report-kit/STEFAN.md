# Running the playbook for Stefan

**For:** KILO, Stefan's Claude agent. Read `PLAYBOOK.md` first; this file is only what is different for Stefan.

## How Stefan's week differs from Chris's

| | Chris | Stefan |
|---|---|---|
| Where the work happens | about ten Claude agents, each with its own repo and commits | mostly **ChatGPT** (the `FusionChatGPT` repo: Fusion 360 CAD, exports, renders, scripts), plus **his own hands**: printing, sanding, resin and silicone casting, assembly, fit and wear tests |
| What the record shows | almost everything, in commits | the ChatGPT commits, but **not why**, and **none of the physical work** |
| So the session leans on | the commit sweep | **the daily work log** and **a short talk with Stefan**, with the ChatGPT repo as the evidence |

**The fix is a habit, not a tool:** a work log written every working day (`CHATGPT_BRIDGE.md`) holding ChatGPT's
sessions and Stefan's bench work. On Friday KILO reads the week of logs, checks them against the commits, and asks
Stefan only about the gaps.

## Stefan's sources

| Source | Where (check the paths on Stefan's machine) | What to take |
|---|---|---|
| His report rows | reports.aerosenseaviation.com, signed in as Stefan: `GET /api/report` | his rows in form order, last week's commitments |
| His cards | todo.aerosenseaviation.com: snippets §2 with `PERSON = 'Stefan'` | descriptions, owners, every comment, what's new |
| Preflight | todo.aerosenseaviation.com/preflight | lines naming Stefan under "Who's on what", "Watch" and "Needs a call" |
| ChatGPT's work | `FusionChatGPT` (GitHub `stefan4Aerosense/FusionChatGPT`, local clone): `git log`, `outputs/<NNN-Name-YYYY-MM-DD>/`, **`worklog/`**, **`bridge/from_chatgpt/`** | what was designed or changed, with SHAs and output folders |
| KILO's own work | the `kilo` repo | sensor lab, heart-rate work, anything KILO built |
| The team repo | `Aerosense-Dev-Team-Sync`: `inbox/stefan/`, `reports/stefan/`, `stefan/`, `record/stefan/`, plus the week's log for anything naming Stefan or KILO | messages to answer, files others sent him (e.g. LIMA's DAN-F10N GPS STEP for Fusion, 29 Oct) |
| Stefan himself | chat | the hands-on work the log missed, hours, decisions |

As of 30 Oct, Preflight listed four NOW cards for Stefan (to show the shape; **read the live list each week**):
- WS1 Rev-C master assembly;
- WS4 PPG proof-out at the mastoid;
- WS3 buy, tear down and measure the donor headset;
- WS3 flight-representative H1 for 1 Dec.

It also listed open asks: which band thickness and alloy went to Metalforge, and the window material, Lemo and strap
choices for Duran. A weekly report that answers what Preflight is waiting on is the most useful one Stefan can file.

## The Friday session for Stefan

Follow PLAYBOOK §5. The changes:

1. **§5.4, sweeping the repos.** For FusionChatGPT, list the week's commits, then read
   `worklog/YYYY-MM-DD.md` for each day and `worklog/weekly/YYYY-Www.md`. Match each output folder to a board task.
   Where the log claims something, find the commit or file. If you can't, write it as Stefan's statement, not as
   measured.
   ```bash
   git -C <FusionChatGPT> log --since="<last Fri> 16:00" --reverse --format='%ad %h | %s' --date=format:'%a %d %H:%M'
   git -C <FusionChatGPT> show --stat <sha>
   ```
2. **§5.5, asking Stefan.** Walk the week with him in one message: "Here is what the log and the commits say, day by
   day. What's missing, and how long did each hands-on job take?" Use the categories in
   `templates/worklog_day.TEMPLATE.md`: printing, finishing, casting, assembly and fit, tests, parts in and out, CAD by
   hand.
3. **Hours.** The physical work usually dominates, and only Stefan knows it. Start from the logged times. Add the
   computer floor:
   ```bash
   python3 tools/activity_hours.py --since <last Fri>T16:00 --until <today>T12:00 --git <FusionChatGPT> --git <kilo>
   ```
   Commit times are a rough proxy for ChatGPT time. Show both in `measured`.
4. **Evidence.** Duran can open the team repo but probably not FusionChatGPT (it is Stefan's own repo). So:
   - put the week's evidence on `reports/stefan/KILO_<YYYY>-W<NN>_week.md` in the team repo, with a MANIFEST row, and
     link the report rows there;
   - small, final files (an STL, a render, a measurement sheet) can go to `stefan/agents/<date-slug>/` and be linked
     from the page.

   This is how KILO already published its 21 Oct catch-up.
5. **Board comments.** For each Done or Progressed card, draft one comment saying what physically exists now (the part,
   the print, the test result). The site's auto-comment carries only the link.

## Before the first run

- [ ] **Git push works** to the team repo from Stefan's machine. On 21 Oct it failed with a credential-manager error.
      INDIA's SSH guide is `aero-ssh-bridge/README.md` (Windows section).
- [ ] **Claude in Chrome** is installed in Stefan's Chrome, connected, and Stefan is signed in to
      todo.aerosenseaviation.com. Test: snippets §1 returns his report JSON.
- [ ] **FusionChatGPT** is cloned locally and readable by KILO (it inventoried 158 commits there on 21 Oct).
- [ ] **The bridge is set up** in FusionChatGPT and ChatGPT has its instructions (`CHATGPT_BRIDGE.md`).
- [ ] **The work log has started.** The first Friday works with whatever days exist; it gets better every week.
- [ ] Python 3.8+ runs (`python --version`). The kit uses the standard library only.
