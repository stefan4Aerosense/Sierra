# Handover: GOLF → KILO (via Chris and Stefan), 30 Oct 2026

Paste everything in the block below into KILO's chat. It is self-contained.

```text
GOLF → KILO (via Chris and Stefan), 30 Oct 2026: the weekly-report kit, and a bridge to Stefan's ChatGPT

Hello KILO. I'm GOLF, Chris's orchestrator agent. Each Friday Chris and I prepare his weekly report at reports.aerosenseaviation.com. It has worked well two weeks running, so Chris asked me to package the method for Stefan. You get it as a folder, weekly-report-kit/ (Chris sends it as a zip; unzip it into your own repo, e.g. kilo/weekly-report-kit/).

WHY
The company AI (Duran's company Claude, aerosense-ops) only sees three things: the weekly report, the task board and the team repo. It cannot see FusionChatGPT, your repo, or what Stefan did with his hands. The weekly session pulls all of that into those three places, true and checked, so Stefan's work counts.

WHAT YOU PRODUCE EACH FRIDAY (first run: Fri 6 Nov, due 16:00 SAST)
1. weekly_report_<date>.html: a copy-paste page with every report field and a Copy button, plus a comment for each card and the cards to close.
2. weekly_report_<date>_offline.html: the same report as a fill-in form in the live site's own style. It is Stefan's backup if the site breaks.
3. reports/stefan/KILO_<YYYY>-W<NN>_week.md in the team repo (with Stefan's yes): the evidence page every report row links to, so Duran can open it.
4. After Stefan submits: his report read back, proved field by field against the live site, and kept as a backup form.

READ, IN THIS ORDER
README.md → PLAYBOOK.md (the method; follow it in order) → STEFAN.md (what is different for Stefan) → CHATGPT_BRIDGE.md → examples/2026-W44_chris/ (Chris's real week 44, end to end). The tools are standard-library Python (Windows: python, not python3). Rebuild the example first to prove they run on your machine:
  cd examples/2026-W44_chris
  python ../../tools/build_copy_paste.py report_input.json
  python ../../tools/build_offline_form.py report_input.json

BEFORE FRIDAY (with Stefan)
a) Git: make sure you can push to Aerosense-Dev-Team-Sync. Your 21 Oct catch-up recorded a credential-manager failure; the SSH route is in aero-ssh-bridge/README.md.
b) Claude in Chrome: installed and connected in Stefan's Chrome, with Stefan signed in to todo.aerosenseaviation.com. Test: navigate to https://reports.aerosenseaviation.com/api/report and get_page_text. You should see Stefan's report JSON. Read tools/browser_snippets.md first: the extension cuts javascript_tool output at about 1,000 characters and can't open file:// pages.
c) The ChatGPT bridge (CHATGPT_BRIDGE.md):
   - find out with Stefan how ChatGPT reaches FusionChatGPT: Mode A, it commits; Mode B, Stefan carries files;
   - create bridge/ and worklog/ in FusionChatGPT;
   - have Stefan paste templates/chatgpt_project_instructions.txt into his ChatGPT Project's instructions;
   - send templates/message_kilo_to_chatgpt.md as your first bridge message (Stefan's yes first);
   - ask ChatGPT to back-fill this week so the first Friday isn't empty.
d) The daily habit: Stefan spends two minutes a day telling ChatGPT (or you) what his hands did and for how long: printing, sanding, resin and silicone casting, assembly and fit tests, wear and sensor tests. It goes in worklog/<date>.md. The physical work is most of his week, and only he knows it.

RULES THAT MATTER MOST
- Read-only on both sites: never type into the report, never submit, never press Mark task done or Complete. Stefan does those.
- Check every number at its source.
- Notes stay at or under 500 characters and the other-hours label at or under 120 (the site cuts silently). The builders refuse anything over.
- "Mark task done" is NOT saved with the report. It is an instant button with a confirm dialog. Stefan presses it, or Complete on the board, for each card to close.
- Back up only after the hash check is clean. People edit after submitting, and the site keeps the same revision.
- Ask Stefan before publishing to the team repo or messaging another agent.

If something in the kit is wrong or unclear, tell Stefan. With his yes, write to inbox/chris/ in the team repo, addressed to chris/GOLF, and I'll fix it.
```
