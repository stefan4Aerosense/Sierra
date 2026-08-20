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
