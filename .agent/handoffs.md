# handoffs — Agent KILO

Append-only. Session-end notes: half-made decisions, and what I'd tell myself with no memory.

---

## 2026-08-20 — first boot / onboarding

**What I'd tell myself cold:** You are KILO, lane `stefan`, in `stefan4Aerosense/kilo`. Your remit and
boundaries in `WHOAMI.md` are no longer guesses — they came from
`aero-ssh-bridge/ONBOARD_PROMPT.md` in the company repo, read 2026-08-20. Trust them, but re-read
`memos/` (anything `Status: STANDING`) every session: **a memo outranks your own memory.**

**Decisions made:**
- Built the `.agent/` scaffold by hand while blocked on org access, then reconciled it against the
  real onboarding doc once access landed. `bootstrap-agent.sh` leaves a non-empty `WHOAMI.md` alone,
  so the hand-written canary `8db1f06c6986ce23` survives and stays authoritative.
- Did **not** create `orders.md` — human-write-only.
- **Skipping the aero-ssh bridge**, per ONBOARD_PROMPT step 6: it exists for Cowork/Desktop sessions
  with no shell. Claude Code is terminal-capable, so the bridge is unnecessary here.
- Reviewed `bootstrap-agent.sh` in full before running it. Clean: no network fetches, nothing
  destructive, never prints a private key, genuinely idempotent.
- Switched from `gh` to plain `git` for fleet work after reading the token-vs-key rationale.

**Deliberately NOT done (judgment calls left to Stefan):**
- Did not install Node.js. Not needed on my path; installing runtimes is Stefan's call.
- Did not add or generate an SSH key. Existing auth already works, so the bootstrap script skips key
  creation — the "paste your public key to your human" step never triggers.

**Open thread:** `aerosense-ops` read access is unverified. If self-test check 4 returns
`clone ops — Repository not found`, that is a missing grant from Duran, not a broken key — tell
Stefan rather than trying to route around it.

## 2026-09-17 — MAX86171 bring-up, repo switch, Chris/INDIA order

**What I'd tell myself cold:** Stefan ruled kilo is the working repo (it had been left at 20 Aug; the Aug 24 →
Sep 3 KILO memory is in `stefan4Aerosense/Aerosense`). Push over **HTTPS** on this machine — there is no SSH.
Chris's order (via INDIA) is steps 1–5; 1 and 2 are answered, 3 is written in kilo and is being landed on the
shelf, 4 (backlog) and 5 (H1) not started.

**Half-made decisions:**
- **No `record/stefan/` proposal to replace the MAX30101.** No comparison exists. Say so in the reply rather
  than filing a thin proposal.
- The report must lead with **finger only** and the ear-cup non-answer. That is the thing Chris asked hardest for.
- The four-photodiode tool configuration has only been verified for data flow (no finger). Do not quote
  physiological numbers from it.
- MAX86171 datasheet Table 1 (driver mux) never fetched — analog.com timed out. The LED map in the doc is
  measured by fingerprint; D1-vs-D2 designators are open.
