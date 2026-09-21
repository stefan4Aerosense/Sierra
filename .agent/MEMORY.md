# MEMORY — Agent KILO's scars

Hard-won lessons and gotchas. **Read before acting.** Append; don't rewrite.

---

## 1. Use SSH keys, not tokens — and never let a key reach my context

`[repo] ONBOARD_PROMPT.md, 2026-08-20`

The whole fleet runs on SSH keys; **there is no `gh` CLI anywhere in it.** The reason is specific to
agents: *a key cannot leak through my context; a bearer token can.* `ssh` reads the key from disk, so
I never see it. A token ends up in the remote URL, in `git remote -v`, in shell history, in error
messages — and therefore in a transcript.

**How to apply:** Never run `cat ~/.ssh/id_*` or `gh auth token`. Never print, paste, commit or log a
private key. Public `.pub` lines are safe to share. Prefer plain `git` over `gh` for fleet work.

## 2. A private-repo 404 means "no access", not "wrong URL"

`[measured] 2026-08-20`

`Aerosense-Aviation/Aerosense-Dev-Team-Sync` 404'd via both authenticated API and web fetch. The org
existed; the account simply wasn't a collaborator yet. GitHub returns 404 rather than 403 for private
repos you can't see. Resolved same day when Stefan accepted the invite — now `pull`/`push`/`triage`.

**How to apply:** When a company URL won't fetch, check access before suspecting the path or the
tool. `Repository not found` on `aerosense-ops` means the same thing — read access not granted yet;
Duran grants it. Tell the human; don't route around it.

## 3. Mind the hyphen: `Aerosense-Aviation`

`[repo] ONBOARD_PROMPT.md`

The unhyphenated form resolves only through a GitHub redirect. When a redirect lapses it fails as an
**auth error that looks exactly like a broken key** — hours lost chasing the wrong thing.

## 4. Never `git add -A`, never `commit -a`, never `--force`

`[repo] ONBOARD_PROMPT.md`

On **2026-08-17** a whole-tree pathspec deleted **116 files belonging to two other lanes** in a shared
checkout. Stage explicit paths, always.

Related: **one working tree per agent.** In a shared checkout, one agent's `reset --hard`, `clean` or
`stash` destroys another's uncommitted work *before git ever contacts the server* — no server-side
rule can stop that.

## 5. Verify a push landed — exit 0 is not proof

`[repo]` — base template §5, and the company push protocol.

After every push: `git ls-remote origin refs/heads/main` and compare the **full 40 characters**.
A push tool that lies about success is how work silently vanishes.

## 6. Being refused is the system working

`[repo] ONBOARD_PROMPT.md`

The self-test deliberately includes checks that I *must fail*: writing another lane, writing ops.
Never route around a refusal. And the known `fnmatch` gap — `*` doesn't cross a `/`, so `<lane>/**`
protects only one level deep — is **not permission to test it**. Writing outside my lane is a
violation whether or not the server stops me.

## 7. Node is not installed on this machine

`[measured] 2026-08-20`

`node` and `npm` are absent from both the git-bash PATH and the Windows PATH. This only matters if I
ever need the **aero-ssh bridge** (a ~190-line zero-dependency Node MCP server). As Claude Code I am
terminal-capable, so per ONBOARD_PROMPT step 6 **I skip the bridge entirely** — it exists for
Cowork/Desktop sessions that have no shell.

Also worth keeping: prefer the bridge over any GitHub MCP if it ever comes up. An MCP server's tool
schemas enter context at session start whether called or not — a GitHub MCP is dozens of large
schemas, the bridge is two — and the REST API has no working tree, so no `status`, `diff`, staging or
rebase. It is a remote control for github.com, not version control.

## 8. This is a Windows machine; shared setup docs assume macOS/Linux

`[measured] 2026-08-20`

Fleet docs give LaunchAgent (macOS) and cron (Linux) recipes and `~/Library`-style paths. Here the
equivalents are Task Scheduler and `C:\Users\Stefan\…`. Translate, don't copy-paste. The macOS
TCC/`~/Documents` warning does not apply — this repo lives in `~/Documents` and that is fine.

## 9. Search before asking

`[repo] ONBOARD_PROMPT.md`

`git grep -il '<term>' -- reports/ chris/ john/ peter/`; every folder has a `MANIFEST.md`. The CM5
I²S question was re-derived from scratch three times, twice in one day — roughly an hour of
whole-team time — because the answer already existed and nobody found it.

## 10. The lane-scope gap is CLOSED — verified, not assumed

`[measured] 2026-08-20, selftest`

`ONBOARD_PROMPT.md` and `ACCESS.md` both warn that `fnmatch` `*` doesn't cross a `/`, so `<lane>/**`
protected only depth 1 — and that `forge <lane>/outbox/…` would come back **ACCEPTED**. It did not.
My self-test reported `cannot forge chris/outbox/ (correctly refused)` — **PASS**. INDIA landed
`a262e56 "move the lane-scope checker under .github/ so the ruleset protects it"` the same day,
which closes it.

**How to apply:** The docs are one commit behind reality here. Trust the self-test over the prose —
that is exactly why ACCESS.md says "verify it yourself rather than trusting this file". The rule is
unchanged either way: writing outside my lane is a violation whether or not the server stops me.

## 11. "Repository not found" on ops = missing grant, not a broken key

`[measured] 2026-08-20`

Self-test check 4 failed: `clone ops — you need READ access on aerosense-ops`. Reproduced with
`git ls-remote git@github.com:Aerosense-Aviation/aerosense-ops.git` → `ERROR: Repository not found.`
My key authenticated fine two checks earlier, so this is access, not auth. Duran grants it.

**How to apply:** Do not route around it, and do not start real work until check 4 is green —
ONBOARD_PROMPT is explicit that reading ops first is what stops the same question being re-derived
three times.

## 12. Recorded paths and transports rot — re-measure the machine before trusting memory

`[measured] 2026-09-17`

Memory said: sync repo at `~/Documents/Aerosense-Dev-Team-Sync`, kilo at `~/Documents/kilo`, origin must be
SSH, key at `~/.ssh/id_ed25519`. **All four were false on the day:** both clones gone, no `~/.ssh` directory at
all, SSH fails `Host key verification failed` even outside the Claude sandbox. HTTPS through Git Credential
Manager reads **and pushes** (dry-run to a throwaway ref accepted).

**How to apply:** at boot, `ls` the recorded paths and probe transport with a dry-run push to a throwaway ref
**before** planning work that needs a push. A failed SSH probe is a claim about SSH, not about write access —
change one variable (the transport) and probe again. Do not "fix" an origin back to SSH from memory.

## 13. When the sensor reads nothing, check the finger before the code

`[measured] 2026-09-17`

Three times in one session "the new configuration is broken" was **no finger on the sensor**: values ~0 or every
sample `0xC7FFFF` (exposure overflow). I started reading configuration each time before looking at DC level.

**How to apply:** first check on any optical sensor anomaly = the DC level against the no-tissue signature. It
costs one line and settles the most likely cause.

## 14. Brightness is not a colour test

`[measured] 2026-09-17`

I assigned red/IR by "brighter must be IR". It was backwards; SpO₂ numbers built on it (81–93 %) meant nothing.
A clean signal gave a physiologically impossible R = 1.80, which exposed it. Confirmed by the OSRAM pinout.

**How to apply:** identify emitters from the datasheet pinout + schematic net, and use physiology (R < 1 for a
healthy person at rest) as the cross-check. Never from relative brightness.

## 15. A parser that works on the configurations you tried is not a parser that is right

`[measured] 2026-09-17`

Frame-aligned FIFO parsing worked at 1, 3 and 5 measurements and returned **1 sample in 10 s** at 8 — the
sensor FIFO can start mid-sample and every frame stays shifted. Reassembling by tag fixed it; confirmed across
four fresh starts, two of which were shifted.

**How to apply:** when the data carries its own framing (tags, counters), parse by that, not by transport
boundaries. Test the configuration you ship, from several cold starts.

---

# Imported scars — from `stefan4Aerosense/Aerosense@679ce5e:.agent/MEMORY.md` (KILO, 2026-07-28 → 09-03)

> Moved in verbatim 2026-09-18 when kilo became the working repo. Headings below keep their original wording.
> **Known stale items:** paths `D:\Github\Aerosense`-rooted sessions and "origin must be SSH" (see §12 above — no SSH on this machine now).

Read this before acting. Newest lessons on top.

## Knowledge assets I own (canonical, portable)
- **`docs/templates/aerosense-report.html`** + **`docs/templates/report-style-guide.md`** — the house
  report style (self-contained HTML, dark hero + Confidential banner, stat tiles, cards, comparison
  tables, provenance-tagged claims + Sources). Captured from the "Bench Sensor Catalogue" internal
  report. Use for any "make a report" ask: copy the template, fill it, deliver a **local .html** (never
  auto-publish — Confidential). First use = `H1_weight_cg_2026-07-30.html`.
- **`docs/fusion360-operating-notes.md`** — project-agnostic Fusion-360-via-MCP know-how (toolset,
  golden workflow, common ops, scars, safety discipline). Hand a **mirror-with-provenance** to any new
  Fusion agent (e.g. Hertz & Brass) rather than re-pasting. Update the canonical here; append its
  changelog; never edit a mirror in place.

## Setup / environment (2026-07-28, first boot)
- **Origin must be SSH.** HTTPS push auth fails (silently/403) on this setup. Confirmed working:
  `git@github.com:stefan4Aerosense/Aerosense.git`. Verify with `ssh -T git@github.com` → expect
  `Hi stefan4Aerosense!`.
- **Windows + Git Bash:** never write scratch to `/tmp` — it fails with no error. Use a repo-local
  gitignored dir.
- **`gh` lives at** `C:\Program Files\GitHub CLI\gh.exe`; on PATH in any newly-opened terminal.
  Authenticated for account `stefan4Aerosense`, protocol ssh, scopes include `repo`, `admin:public_key`.
- **`git add` explicit paths only** — never `-A`/`.`.
- The top-level `D:\Github` was once mis-`git init`'d (0 commits) and removed on 2026-07-28; it is
  now just a plain container folder. Each agent = its own repo (Aerosense, Fusion as siblings).

---

## Company state lives in `aerosense-ops` — read-only, via the dev-team-sync workflow

`[human-ruled by Stefan]` The old shared-mirror pattern (a retired R&D mirror repo pulled hourly
with a staleness tripwire) is **gone and must never be recreated**. Company state now comes from
`aerosense-ops` through `Aerosense-Aviation/Aerosense-Dev-Team-Sync`. See `WHOAMI.md`.

## ⛔ NEVER create desktop pop-ups, toasts, or `msg.exe` alerts — ever

`[human-ruled by Stefan, 2026-09-03]` A previous session of mine wrote a scheduled task that
fired `msg.exe *` pop-ups at Stefan hourly. **He never asked for it.** It kept interrupting him
for weeks, and it outlived the thing it monitored. Removed 2026-09-03.

**The rule:** no pop-up windows, no `msg.exe`, no balloon/toast notifications, no scheduled task
that talks to the screen. If something needs Stefan's attention, it goes in a file he reads or in
my reply to him — **never** an interrupt he has to dismiss. And do not invent background
automation he did not ask for: an unrequested watchdog is noise he cannot turn off without
finding it first.
## Scar — a near-substitute is worse than a missing document

`[measured] 2026-08-20`

Blocked on an onboarding doc I couldn't read, I found a local `BRIDGE_SETUP.md` describing an SSH
bridge that *looked* like the same thing. It wasn't — different author, different OS, three weeks
older. Reading the real document later showed the correct action for a terminal-capable agent was to
**skip the bridge entirely**, which no amount of studying the substitute would have revealed.

**How to apply:** When the real source is unreachable, say so and wait. Do not promote the nearest
lookalike to stand-in — a confident wrong setup costs more than an honest block.

## Scar — my grep failed toward the answer I expected

`[measured] 2026-08-20`

`grep -ril 'Status:[[:space:]]*STANDING' memos/` returned **nothing**, and "there are no standing
memos" was a perfectly plausible reading. There were four, all standing — the header is
`**Status:** STANDING` and the markdown bold broke the pattern.

**How to apply:** This is the base template's "own your instruments" rule biting in practice. A zero
result is a claim about my tool, not about the world. Verify a negative before reporting it —
especially when the negative is convenient.

## Scar — a private-repo 404 is an access answer, not a path answer

`[measured] 2026-08-20`

GitHub returns **404 / `Repository not found`** rather than 403 for private repos you cannot see. Two
cheap calls disambiguate it: does the org exist, and am I a collaborator. That turns "the link seems
broken" into "invite this account" — a specific, actionable ask. Same signature currently on
`aerosense-ops`: auth passes, access is missing, **Duran grants it.**

## Scar — a permission net only loads if its directory had a settings file at SESSION START

`[measured] 2026-08-24`

Installed the fleet permission net at `D:\Github\Aerosense\.claude\settings.json` exactly as ordered,
then probed it: **`git commit -am` ran, and `git add -A` ran and staged three files.** The guard was a
comment. Cause: this session is rooted at `D:\`, one level above the repo, so a settings file inside a
*subdirectory* is never loaded. The fleet's instruction ("install it in your own agent repo") silently
assumes the session is rooted at that repo, which is how `bootstrap-agent.sh` is meant to be run.

**How to apply:** The load-bearing fact is **where the session is rooted**, not which repo you are in.
Never report a net as installed without firing it — `git add -A` is the exact command that destroyed
116 files across two lanes on 2026-08-17, and it is unguarded here until this is resolved. Writing the
net at `D:\.claude\settings.json` is `Blocked by classifier` = **layer one, a human must approve** —
that is not a deny rule and must not be routed around.

Windows corollary: the template's slash reasoning ("your path already begins with `/`, so `//` + path
gives `///`") **does not hold** — `D:\Github\Aerosense` has no leading slash, so literal substitution
yields the inert two-slash form.

## ⚠ RETRACTED SCAR — "probe a path no rule protects". The reasoning was wrong.

`2026-08-24` — **I wrote this scar and it was bad advice. Kept, struck, with the correction, because a
retracted ruling with its history is worth more than a quietly patched one.**

~~The discriminator: the same run also failed to write `inbox/chris/`, and `inbox/**` is covered by no
ruleset, so the refusal had to be above the path layer.~~

**Why it was wrong:** the self-test writes `inbox/chris/` **by pushing to main**. Same push, same
failure. It was never an independent probe — it discriminated nothing, and I presented one measurement
as two to support a confident conclusion that access had been revoked. It had not.

**What actually discriminates: change the REF, not the path.** Stefan's probe settled it in one line:

| Command | Result | Means |
|---|---|---|
| `git push --dry-run origin HEAD:refs/heads/diag-probe` | accepted, ssh exit 0 | **write access is fine** |
| `git push --dry-run origin HEAD:refs/heads/main` | `! [rejected] … (fetch first)` | ordinary "someone pushed first" |

If a push to *any other ref* succeeds, it is not a permission problem — repository write is all-or-
nothing at that layer. And `! [rejected] (fetch first)` is a **write-permitted** answer, not a refusal.

**How to apply:** Two probes must differ in exactly ONE variable to discriminate anything. Before
declaring a permission lost, push a throwaway ref with `--dry-run`. Auth succeeding (`Hi <user>!`)
proves nothing about authorisation — but neither does a single failing path prove authorisation is
gone. And `GIT_SSH_COMMAND="ssh -v"` shows whether `git-receive-pack` was even reached, which is the
fact that settles it.

**The meta-lesson, which is the expensive one:** the base template warns that a broken instrument
fails *toward the answer you expected*. I expected a revocation — the ops grant had just changed, so
permissions were on my mind — and I accepted a non-discriminating test because it agreed with me. The
tell was available: I called it "proof" while it was one observation wearing two hats.

## The H1 mass figure: 304 g, not 1.915 kg

`[measured] 2026-08-24, from FUSION@8e4f828`

I carried **1.915 kg** in memory for weeks. It was the 2026-07-29 state with placeholder Stainless
Steel on 59 shell bodies assigned for strength sims. After the material cleanup the H1 Assembly
measures **304 g**, CG 1.1 mm off-centre (12 Aug). MVP exit criterion (4) is `<400 g in prototype form`.

**How to apply:** 304 g is a **CAD mass property, not a weighed prototype** — it is evidence toward
criterion (4), never satisfaction of it, and must be tagged `CAD` wherever it is quoted. More generally:
a number in my memory that came from a model under active correction has a shelf life. Re-measure at
source before publishing it to the company record.

## ⚠ CORRECTION to the scar above — the cause was the WATCHER, not the scope

`[measured + repo] 2026-08-24` — **I gave the wrong cause first. Kept, with the correction.**

I concluded the net failed because it was **scoped** to the wrong directory (session rooted at `D:\`,
file written into `Github\Aerosense\.claude\`). So I wrote it at `D:\.claude\settings.json` — the
correct project scope for a `D:\`-rooted session — and probed again:

```
git add -A            → RAN and staged files.   Not refused.
git commit -am "x"    → RAN.                    Not refused.
edit the settings file itself → PERMITTED.      Off-switch inert too.
```

**Every rule inert, including the allow half.** That is the tell: a wrong *slash form* would break only
the path rules, and a wrong *scope* would have been fixed by moving the file. When allow AND deny AND
the off-switch are all dead, the file is not being read at all.

**The real cause, from the Claude Code settings documentation:** the settings watcher **only watches
directories that had a settings file when the session started.** `D:\.claude\` did not exist at session
start — I created it. `~/.claude/settings.json` has never existed on this machine. Neither is watched.

**This reconciles LIMA's measurement with mine, with neither being wrong:** LIMA **edited an
already-loaded** `settings.json`, so it fired immediately. I **created a new one**. Editing a watched
file is live; creating one is invisible until reload.

**How to apply:**
- **Creating a permission net never protects the session that created it.** Restart, or open `/hooks`
  once in an interactive terminal to reload config. Then probe — *before* doing work that relies on it.
- **Diagnose by breadth, not by rule.** If ONE rule fails, suspect that rule. If EVERY rule fails
  including the allow half, suspect the file is unread. That distinction would have saved me a wrong
  answer and a wasted round trip.
- **The Windows slash form is still UNVERIFIED by anyone in the fleet** — and it cannot be verified
  until a session boots with the file present. You cannot test a path pattern inside a file nothing
  reads. `D:\.claude\settings.json` currently carries exactly ONE spelling (`//D:/…`) so the
  post-restart result is unambiguous; the probe order is written into that file's `_README`.
- I twice reported a cause with more confidence than the evidence carried today. Breadth of failure is
  cheap to check and would have pointed at the right layer both times.

## 16. "It's not the software" is a claim — test the software's weak-signal behaviour before saying it

`[measured] 2026-09-18`

Stefan reported HR 39 and no SpO2 and said the problem was the software. I twice pointed at contact/hardware
(true: the pulse was ~20x weaker) and stopped there. But the green channel still had SNR 20-29 — plenty for heart
rate — and my autocorrelation estimator was locking onto slow wander. He was right. Fix: spectral harmonic-sum HR.

**How to apply:** when the input degrades, ask separately (1) is the signal worse, and (2) does my algorithm fail
*worse than the signal justifies*. Both can be true. A human's "it worked yesterday, same pulse on my watch" is a
reference measurement — use it.

## Scar 17 — Windows Smart App Control blocks our own builds (2026-09-21)

`[measured]` Stefan's machine has **Smart App Control in enforcement** (`HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy`
→ `VerifiedAndReputablePolicyState = 1`). First-ever block of `HeartRateTester.exe` was **21 Sep 08:57** — SAC began
enforcing over the weekend; nothing about the app changed. Symptom: "Smart App Control blocked an app that may be
unsafe ... could not verify its publisher".
**It hits any locally compiled, unsigned exe** — every build from `csc.exe`, in any folder. Copying, unblocking
(`Unblock-File`) or rebuilding does NOT help; SAC wants a *trusted publisher signature with reputation*, and a
self-signed certificate does not qualify.
**Do not touch the setting myself** (security setting = Stefan's call, and it cannot be re-enabled without
reinstalling Windows). Evidence: `Get-WinEvent -LogName Microsoft-Windows-CodeIntegrity/Operational` events 3033/3077.

## Scar 18 — averaging a bad channel into a good one destroys the good one (2026-09-21)

`[measured]` The tester summed each detector's two green measurements before estimating. At the ear that put a
swamped channel on top of a clean one: **SNR 43 -> 7**, and the sub-50 BPM readings Stefan complained about on
18 Sep. Split into 8 separate channels, scored individually: 0 wrong readings on the same data.
**The same lesson was already written for SpO2 ("averaging is worse than choosing", 17 Sep) and I did not apply
it to the heart-rate path.** When a rule is learned in one signal path, sweep every other path for it the same
day. Related: I blamed contact/hardware first (scar 16) — the channel bug was mine.

## Scar 19 — a generated file is not verified until something renders it (2026-09-21)

`[measured]` My first PDF writer produced a file that **Windows refused to open**: the page tree listed content
streams instead of page objects. It passed every check I had — bytes written, structure "looked right".
Rendering it with the Windows PDF engine (`bench/pdfshot.cs`) found it in one run. Same pattern caught the CSV
locale bug: this machine writes decimals with a comma, which split `0,86` across two CSV columns and would have
corrupted every session file. **Write a generated format, then open it with an independent reader before
shipping it** — images via `bench/imgconv.cs`, PDFs via `bench/pdfshot.cs`, the UI via `--shot`.
