# The Claude ↔ ChatGPT bridge (KILO ↔ Stefan's ChatGPT)

**The problem.** Stefan does most of his design work in ChatGPT, which saves into the `FusionChatGPT` repo. KILO, his
Claude agent, has a local clone and can read every commit. But a commit says *what* changed, not *why*, what was
tried, or what Stefan then did with his hands. And there is no agreed place for KILO to ask ChatGPT a question.

**The fix:** three folders in FusionChatGPT that both sides use, and instructions that make ChatGPT keep them
up to date in every session.

## The layout (inside FusionChatGPT)

```
FusionChatGPT/
  bridge/
    README.md                              this protocol, short (copy §2 below)
    to_chatgpt/YYYY-MM-DD-NNN_slug.md      KILO → ChatGPT: questions, requests   (only KILO writes here)
    from_chatgpt/YYYY-MM-DD-NNN_slug.md    ChatGPT → KILO: answers, the weekly   (only ChatGPT writes here)
  worklog/
    YYYY-MM-DD.md                          one per working day: ChatGPT's sessions + Stefan's hands-on work
    weekly/YYYY-Www.md                     the week in a fixed shape, ready by Thursday evening
  outputs/…                                unchanged: ChatGPT's design outputs as today
```

It is one file per message and one per day, never a shared running file, so nobody's edits collide. This is the same
rule the team repo's `inbox/` uses.

### Message format (both directions)

```markdown
---
id: 2026-11-02-001
from: KILO            # or ChatGPT
to: ChatGPT           # or KILO
re: —                 # the id being answered, if a reply
status: open          # open | answered | closed
needs: an answer | a file | a decision
---
## Ask
One clear question or request.
## Context
Paths, commit SHAs, the board task it belongs to.
```

When ChatGPT answers, it writes `from_chatgpt/<new id>` with `re:` set. KILO then sets the request's `status:` to
`answered`.

## 1 · How ChatGPT gets files into the repo: pick the mode that works

KILO's first job is to find out how ChatGPT actually reaches FusionChatGPT today. Look at the commit authors and ask
Stefan.

| Mode | When | How a ChatGPT file lands |
|---|---|---|
| **A: ChatGPT writes to the repo** | ChatGPT works in a local checkout with commit rights (a coding agent or Codex-style tool) | ChatGPT writes the file and commits it, prefix `[CHATGPT]` |
| **B: Stefan carries it** | ChatGPT can't write to git (plain chat, read-only GitHub connector) | ChatGPT ends the session with one fenced block headed `FILE: worklog/2026-11-02.md`. Stefan saves it into the clone, or pastes it to KILO, which saves and commits it as `[KILO] log: carried from ChatGPT` |

Either way, KILO reads the same files. Mode B costs Stefan about 30 seconds a day.

## 2 · Instructions for ChatGPT (paste into the ChatGPT Project's instructions)

Paste `templates/chatgpt_project_instructions.txt` into the instructions of the ChatGPT Project Stefan works in, so
**every chat follows it without being told**. In short:
- start each session by checking `bridge/to_chatgpt/` for open items;
- end each session by appending to today's `worklog/`;
- ask Stefan what he did with his hands;
- on Thursday write the weekly summary;
- never invent hours or results; mark what is unverified.

## 3 · The first message from KILO to ChatGPT

Send `templates/message_kilo_to_chatgpt.md` once:
- Mode A: commit it as `bridge/to_chatgpt/<date>-001_weekly-logging.md`;
- Mode B: Stefan pastes it into ChatGPT.

It explains who KILO is, why it needs the log, the formats, and asks ChatGPT to confirm with its first day's
`worklog/` entry.

## 4 · The daily rhythm

| When | Who | What |
|---|---|---|
| End of each ChatGPT session | ChatGPT | Appends its session to `worklog/<today>.md`: what was asked, what was made (output folder, commit SHA), what is verified, what isn't |
| End of each day (2 min) | Stefan → ChatGPT (or KILO) | Says what his hands did; the agent adds it to the same day file under **Stefan's hands** with times. Voice notes are fine; the agent writes them up |
| Any time | KILO | A question for ChatGPT → `bridge/to_chatgpt/`. Stefan opens ChatGPT with "check the bridge" when KILO flags one |
| Thursday | ChatGPT (KILO asks via `to_chatgpt` in the morning) | `worklog/weekly/<YYYY-Www>.md` from `templates/worklog_week.TEMPLATE.md` |
| Friday | KILO | Runs the playbook (`STEFAN.md`) and reads the whole week |

## 5 · Rules both sides keep

- **True and scoped.** "Printed 4, 2 warped" beats "printing went well". Numbers carry their source: `[measured]` by a
  tool or meter, `[repo]` in a file or commit, `[Stefan]` his own account, `[assumed]` flagged loudly.
- **Never invent** hours, results or fit. If Stefan didn't say, write "not reported".
- **Cite** every design output by folder and commit SHA.
- **Own your folder.** ChatGPT writes `from_chatgpt/` and `worklog/`; KILO writes `to_chatgpt/` and marks requests
  answered. Neither edits the other's files.
- **Nothing secret** in the log: no passwords, keys or Wi-Fi details.
- **Physical work counts as work.** Printing, sanding, casting, curing, assembly and testing go in the log with times,
  even when nothing was committed.
