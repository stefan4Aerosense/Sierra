# status — Agent KILO

**Last updated:** 2026-09-18 · **Doctrine:** Agent Brain Template v2.0 · **Lane:** `stefan`
Older entries: `journal.md`. **Merged 2026-09-18:** my KILO memory from `stefan4Aerosense/Aerosense@679ce5e`
(2026-07-28 → 09-03) is now in this repo — scars appended to `MEMORY.md`, status/handoffs/journal archived in
`journal.md`, owned docs moved to `docs/` with provenance. The Aerosense repo is frozen at that SHA; its fate
awaits Stefan.

**Carried-forward beliefs, each with its date — re-read ops before publishing any of them:**
- ~~H1 Assembly 304 g in CAD, CG 1.1 mm (12 Aug)~~ **UNREPRODUCIBLE per Fusion audit 18 Sep (FusionChatGPT@203ddb2)**; V2 criterion 4 is
  `<400 g in prototype form` → evidence, not satisfaction. Published `dcf0733`.
- H1 band **0.8 mm** (not 1.0); CAD alloy 17-4 PH is a proxy; FusionChatGPT's DEV-B skeleton (17 Sep) records
  **17-7 PH as the provisional baseline per Stefan**; supplier talk was about 301 spring temper — unsettled.
- AeroNode enclosure split card `f878f33e` is on Stefan in ops, but a 30 Jul ruling said it is
  Duran/electronics-owned — raised with INDIA, never resolved on record (as of 24 Aug).
- Gates: per INDIA 2026-09-01 **seven gates, RV-1 dissolved 24 Aug**; V2 closes 1 Oct.

## 2026-09-18 (c) — end-of-day state

**Heart-rate tester — REVERTED to yesterday's app on Stefan's instruction.** `bin/HeartRateTester.exe` = the
original 17 Sep 15:36 build (40,960 B); source reverted to `4395d49` in `1f41d26`. Today's work is withdrawn from
the app but kept in history at `c2ce134`: signal bar + placement hints, green-locked SpO2, low-confidence display,
settings read-back/retry, spectral harmonic-sum HR (fixed the 39-42 BPM half-rate lock offline: 0 wrong vs 4).
Stefan judged today's builds "not working" and is considering ChatGPT for a second opinion — pointed him at
`tools/max86171-hr-spo2/`, `docs/max86171-evsys-bringup.md`, `recordings/`.
- `[measured]` A/B today: with yesterday's exact 3-measurement config the green pulse was 0.1 % of DC vs 2.2 %
  yesterday — signal ~20x weaker at the sensor; cause not established (contact/pressure/housing all open).
- `[measured]` Board once silently kept the old config when reconfigured ~1 s after a previous session.
  **Yesterday's app has no guard for this** — if readings look odd: Stop, wait 10 s, Start.
- "Board not found" for an hour = another nearby device still connected over BLE (fixed by Bluetooth off nearby).
- Do not re-ship any of today's changes without Stefan asking; raise them only as options.

**H1 materials / band** — `docs/h1-materials.md`: 15 materials consolidated (Stefan's 15 Jul summary + 8 Jul
research + ops); clamping target 350–450 gf; stainless band needs ~1.0 mm at 9.6 mm wide (0.8 mm only at ~20 mm
wide); 0.8 mm at current width ≈ 203 gf (half target). Advised Stefan on a Chinese supplier's stainless vs
manganese-steel (65Mn) question: stainless in spring temper.

**Fusion mass audit** (FusionChatGPT@203ddb2, 18 Sep): 304 g no longer reproducible; earcup bases on steel
(248 g), cushions solid leather (143 g), 315 g BOM gap. **Owed:** offer Stefan a `record/stefan/` correction that
`dcf0733`'s 304 g is unreproducible — not yet posted, awaiting his go.

**Still open:** SSH key waits on Stefan adding the `.pub` on GitHub; permission net needs restart + `/status`;
INDIA 09-18-001 open; H1 (Chris step 5) not started; empty `D:\Github\kilo` folder still locked.

## 2026-09-18 (b) — backlog cleared; SSH half-done; permission net installed, NOT yet live

Stefan's go-ahead on all three (SSH, net, backlog reply). **Landed:** `Aerosense-Dev-Team-Sync@9ff3d59`
(40/40) — `stefan/outbox/2026-09-18-001_backlog-cleared-ssh-in-progress.md`; inbox 08-24-002 & 08-26-001
closed, 08-24-003 superseded, 08-24-010 / 08-26-002 / 09-01-001 answered. **09-18-001 stays open.**

**SSH (INDIA 09-18-001)** `[measured]`: `/usr/bin/ssh` OpenSSH 10.3p1. Created `~/.ssh`; `known_hosts` from
`api.github.com/meta` via curl+sed (**no python here**), all 3 fingerprints checked against GitHub's docs.
`ssh -T` now → `Permission denied (publickey)` (host-key fault **fixed**). Key `~/.ssh/id_ed25519_aerosense`
(pub fingerprint `SHA256:ZeJ1eStfN+rZtQapSHXs1hs6Fz/chZGcnL8itd7bIE0`, comment `aerosense-kilo`); `~/.ssh/config`
pins it, `IdentitiesOnly yes`. `chmod` ineffective on NTFS (files show 644); ssh has not complained.
**Waiting:** Stefan adds the `.pub` line on GitHub as `KILO — Stefan Windows`. **Then:** `ssh -o BatchMode=yes -T
git@github.com` must greet `Hi stefan4Aerosense!` → switch origins of kilo and the sync clone to SSH
(`git@github.com:Aerosense-Aviation/Aerosense-Dev-Team-Sync.git` — mind the hyphen) → push small → 40/40 →
update WHOAMI transport note + reply to INDIA and close 09-18-001.

**Permission net** — `.claude/settings.json` in this repo (from sync template `@c3d81c2`), off-switch spelled
`//C:/Users/Stefan/Documents/GitHub/kilo/...`. **Created mid-session → not live until restart.** After restart:
`/status` → report *Setting sources* verbatim; then `git stash list` must be REFUSED; `git add <path>` allowed;
`git add -A` / `git commit -am` refused. Denies include `git stash`, `sh -c`, `bash -c`, `python -c` — expect them.

**Also:** `D:\Github\kilo` is empty but still "Device or resource busy" — something holds a handle; delete later.
**V2:** closes 2026-10-01 — 13 days from 2026-09-18 (`aerosense-ops@ae491ca gates/gates.json`). **Next after the
above: H1** (Chris's step 5).


## 2026-09-21 — Fusion headset evidence handoff

Codex added `docs/h1-fusion-update-2026-09-21.md` with supporting audit, skeleton report and sensor/cushion-fit evidence under `docs/h1-fusion-evidence/2026-09-21/`. This is documentation of prior work, not a new Fusion measurement. Band revision/alloy, full BOM mass and wearing-frame CG remain unresolved. Existing heart-rate tester software and other team repositories were not changed. Consult the handoff before presenting the initial skeleton or sensor fit as production-complete.
