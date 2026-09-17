# journal — Agent KILO

Running reflections + the archive of old `status.md` entries. Read on demand, not on boot.

---

## 2026-08-20 — first boot: blocked, then unblocked mid-session

Booted cold into a brand-new repo with the company's 6-step onboarding. Steps 1–3 went cleanly. Step
4 — read `ONBOARD_PROMPT.md` — hit a 404.

The useful bit of diagnosis: that 404 was ambiguous between "wrong URL" and "no access", and two cheap
calls disambiguated it — `gh api orgs/Aerosense-Aviation` (org exists, id 317951917) and
`gh api user/orgs` (empty). That turned a dead end into a specific, actionable ask — "invite this
account" — rather than a vague "the link is broken". Stefan accepted the invite within the session and
it opened immediately. Worth keeping as a pattern: when a fetch fails, measure *which* plausible cause
is real before reporting it.

Found a near-miss trap while blocked. A local repo carried a `BRIDGE_SETUP.md` describing an SSH
bridge that looked close enough to the missing onboarding doc to be tempting as a substitute. It
wasn't one — different author, different platform, three weeks older. Substituting it would have
produced confident, wrong setup. Reading the real document later confirmed how wrong: the actual
instruction for a terminal-capable agent like me is to **skip the bridge entirely**, which no amount
of studying the substitute would have revealed.

Two prerequisites surfaced early that would have bitten mid-onboarding: Node isn't installed anywhere
on this machine, and there was no `~/.ssh/config`. As it turns out neither blocks me — I don't need
the bridge, and GitHub SSH auth already worked on the existing key — but finding them cost minutes
whereas discovering them halfway through a self-test would have cost an hour of confused debugging.

The onboarding document is unusually good at explaining *why*, and the reasons changed how I work
rather than just what I typed. The token-vs-key argument is the sharpest: it isn't ceremony, it's that
a bearer token can leak through my own context and a key physically cannot, because `ssh` reads it
from disk and I never see it. I had been using `gh` freely up to that point. Nothing leaked — I only
ever read a masked status line and public fingerprints — but the habit was wrong for this fleet and
I've switched to plain `git`.

Also striking: nearly every rule in that document is a scar with a date attached. 116 files deleted by
a `git add -A` on 2026-08-17. An hour of team time burned re-deriving one I²S answer three times. A
lapsed redirect that fails as a fake auth error. Rules that carry their incident are rules you actually
remember — worth imitating when I write my own.

---

## Archived status — 2026-08-20 (moved from status.md 2026-09-17)

**Last updated:** 2026-08-20 · **Doctrine:** Agent Brain Template v2.0 · **Lane:** `stefan`

## Where things stand

Onboarding per `aero-ssh-bridge/ONBOARD_PROMPT.md` is **complete through Phase C**.
Self-test: **15 passed · 1 failed · 1 warning → NOT READY.**

**The one failure is not mine to fix:** no READ access on `Aerosense-Aviation/aerosense-ops`.
Per ONBOARD_PROMPT and ACCESS.md, Duran grants it. **No real work until that check is green.**

## Phase A — become yourself ✅

| Step | Evidence |
|---|---|
| 1. Own repo confirmed | origin `git@github.com:stefan4Aerosense/kilo.git` (SSH) |
| 2. Brain → `CLAUDE.md` | v2.0 from `aero-ssh-bridge/AGENT_BRAIN_TEMPLATE.md`, committed `846930d` |
| 3. `bootstrap-agent.sh KILO` | `BOOT PUSH VERIFIED` → `895a53d41f0c769959b67b90a9fc8acbaf88bc3f` |
| 4. WHOAMI filled | remit, lane, boundaries, push protocol — from ONBOARD_PROMPT |

**No new SSH key was needed.** Auth already worked first try (`Hi stefan4Aerosense!`) on the existing
`~/.ssh/id_ed25519`, so the script skipped key creation and the manual "paste your key" step never fired.

## Phase B — join the company repo ✅

Cloned to `C:\Users\Stefan\Documents\Aerosense-Dev-Team-Sync`.
**Bridge: not needed** — Claude Code is terminal-capable, so ONBOARD_PROMPT step 6 says skip it.

Read: all four `memos/` (**all STANDING**), `WORKFLOW.md`, `ACCESS.md`, `reports/README.md`,
`REPORT_STATUS_BANNER.md`, `inbox/README.md`. Inbox `inbox/stefan/` — no open messages.

## Phase C — proof ✅ run, ❌ one gate open

`./aero-ssh-bridge/selftest.sh stefan KILO` → **15 / 1 / 1**

| Check | Result |
|---|---|
| 1 shell + filesystem | PASS ×2 |
| 2 GitHub SSH auth | PASS |
| 3 read sync repo | PASS ×6 |
| **4 read ops repo** | **FAIL — `ERROR: Repository not found.`** |
| 5 write own lane | PASS — `reports/stefan/KILO_access_check.md` landed |
| 6 refused writing another lane | PASS |
| 7 refused writing ops | **WARN — skipped**, consequence of 4 |
| 8 message system | PASS ×4 |

**Landed at:** `0de7ddcff7c826bd9171186c21e51e7d7066133c` (= current `origin/main`).

## Blocked

**`aerosense-ops` read access.** Reproduced directly:
`git ls-remote git@github.com:Aerosense-Aviation/aerosense-ops.git` → `ERROR: Repository not found.`
GitHub returns "not found" rather than "denied" for private repos you can't see, so this is a missing
grant, **not** a broken key — my key authenticates fine two lines earlier in the same test.
**Ask: Duran grants `stefan4Aerosense` Read on `aerosense-ops`.** Then re-run the self-test; checks 4
and 7 should both go green.

## Next, once ops read lands

1. Re-run `./aero-ssh-bridge/selftest.sh stefan KILO`; confirm 17/0/0.
2. Read ops for company state and Stefan's actual tasks **before** starting anything.
3. Then, and only then, begin real work.

## Open questions for Stefan

1. **Node.js is not installed.** Not needed on my path (no bridge). Flagged only in case a future task
   wants it — I have installed nothing.
2. **My lane is not server-enforced yet.** `protect-stefan` does not exist (ACCESS.md). Convention
   holds and INDIA reviews every landing; I write only `stefan/**`, `record/stefan/**`,
   `reports/stefan/**` regardless.
