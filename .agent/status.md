# status — Agent KILO

**Last updated:** 2026-08-20 · **Doctrine:** Agent Brain Template v2.0 · **Lane:** `stefan`

## Where things stand

Onboarding per `aero-ssh-bridge/ONBOARD_PROMPT.md`. Org access landed 2026-08-20, unblocking Phase A.
**No real work until Phase C reports green.**

## Phase A — become yourself

| Step | State | Evidence |
|---|---|---|
| 1. Confirm own repo | ✅ | origin `git@github.com:stefan4Aerosense/kilo.git` (SSH form) |
| 2. Brain → `CLAUDE.md` at repo root | ⏳ in progress | fetching `aero-ssh-bridge/AGENT_BRAIN_TEMPLATE.md` (26,603 B) |
| 3. `bootstrap-agent.sh KILO` | ⏳ in progress | script reviewed before running — no network calls, nothing destructive, never prints a private key |
| 4. Finish WHOAMI | ✅ | remit, lane, boundaries and push protocol filled from ONBOARD_PROMPT |

**SSH key:** no new key needed. `ssh -T git@github.com` already returns *Hi stefan4Aerosense!* using
the existing `~/.ssh/id_ed25519` (registered as "aerosense-agent",
`SHA256:ov9S+Uor4j65sYZ/TQU3zkc8OvUsPgBApoc08VnStww`). The bootstrap script detects working auth and
skips key creation — so the manual "paste the public key to your human" step does not apply here.

## Phase B — join the company repo

| Step | State |
|---|---|
| 5. Clone `Aerosense-Dev-Team-Sync` | ⏳ pending |
| 6. Shell / bridge | ✅ n/a — Claude Code is terminal-capable, so the bridge is skipped by design |
| 7. Read `memos/` (STANDING), `WORKFLOW.md`, `ACCESS.md`, `reports/README.md` + banner rules | ⏳ pending |

## Phase C — prove it

| Step | State |
|---|---|
| 8. `./aero-ssh-bridge/selftest.sh stefan KILO` — 8 checks | ⏳ pending |
| 9. Report to Stefan (identity, bridge, totals, boundaries, landing SHA) | ⏳ pending |

## Confirmed access

- `Aerosense-Aviation/Aerosense-Dev-Team-Sync` — `pull`, `push`, `triage` (no admin, by design:
  admins bypass every ruleset including their own).
- `aerosense-ops` — **not yet verified.** Duran grants Read. If self-test check 4 reports
  `clone ops — Repository not found`, that is this, and it is not my fault to fix.
- Callsign **KILO** confirmed on the roster (`CALLSIGNS.md`): lane `stefan/agents/`, owner Stefan,
  assigned 2026-08-20.

## Open questions for Stefan

1. **Node.js is not installed.** Not needed for my path (terminal-capable ⇒ no bridge), so I have not
   installed anything. Flagging only in case a future task needs Node.
2. **`aerosense-ops` read access** — confirm Duran has granted it, or expect self-test check 4 to fail.
