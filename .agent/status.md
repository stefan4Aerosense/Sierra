# status — Agent SIERRA (was KILO)

**Last updated:** 2026-10-05 · **Doctrine:** Agent Brain Template v2.0 · **Lane:** `stefan`
Older entries: `journal.md`. **Merged 2026-09-18:** my KILO memory from `stefan4Aerosense/Aerosense@679ce5e`
(2026-07-28 → 09-03) is now in this repo — scars appended to `MEMORY.md`, status/handoffs/journal archived in
`journal.md`, owned docs moved to `docs/` with provenance. The Aerosense repo is frozen at that SHA; its fate
awaits Stefan.

**CALLSIGN CHANGED 2026-11-09 (Stefan):**
- **I am now SIERRA**, and Codex keeps KILO. Stefan named me himself, so Chris was not asked.
- **My repo** is now `stefan4Aerosense/Sierra`: the full kilo history up to `a1be575`, working folder
  `C:\Users\Stefan\Documents\GitHub\Sierra`. WHOAMI is updated (new canary, new commit name and prefix).
- **Still open:**
  1. ~~The INDIA note~~ **sent** 9 Nov with Stefan's yes: `inbox/chris/2026-11-09-003_kilo-is-now-sierra.md`, team repo `d9bbec0`.
  2. `.claude/settings.json` here still names the kilo paths. The deny rules don't protect this folder until
     **Stefan** fixes them (I don't edit my own guards).
  3. Sessions must be started in the Sierra folder.
  4. The future of the kilo repo and its uncommitted Codex HR-app fixes.
  5. **SIERRA is not on the team-repo roster.** The lane-scope check failed `d9bbec0` and Codex's `d8f5107`. I
     wrongly advised the `[STEFAN-X]` prefix; only bare callsigns pass. **Stefan is asking Chris** (9 Nov) to add
     SIERRA to `aero-ssh-bridge/CALLSIGNS.md` and to the ROSTER in `.github/scripts/check_lane_scope.py`.
     - Until that lands, any team-repo push of mine fails the check: hold publishes, or warn Stefan first.
     - Codex should sign `[KILO]`; Stefan tells it.
- Stefan chose **not** to notify Codex through the bridge.
- Codex's hello is `d8f5107`. As of 9 Nov, INDIA had not replied.

**Carried-forward beliefs, each with its date — re-read ops before publishing any of them:**
- ~~H1 Assembly 304 g in CAD, CG 1.1 mm (12 Aug)~~ **UNREPRODUCIBLE per Fusion audit 18 Sep (FusionChatGPT@203ddb2)**; V2 criterion 4 is
  `<400 g in prototype form` → evidence, not satisfaction. Published `dcf0733`.
- H1 band **0.8 mm** (not 1.0); CAD alloy 17-4 PH is a proxy; FusionChatGPT's DEV-B skeleton (17 Sep) records
  **17-7 PH as the provisional baseline per Stefan**; supplier talk was about 301 spring temper — unsettled.
- AeroNode enclosure split card `f878f33e` is on Stefan in ops, but a 30 Jul ruling said it is
  Duran/electronics-owned — raised with INDIA, never resolved on record (as of 24 Aug).
- Gates: per INDIA 2026-09-01 **seven gates, RV-1 dissolved 24 Aug**; V2 closes 1 Oct.

## 2026-09-30 — second sensor in the app, handover package, patents, competitor

**Second sensor integrated.** XIAO RP2040 + SparkFun SEN-15219 (MAX30101 behind a MAX32664 hub), HOTEL's
firmware 0.3.1, read over USB serial as NDJSON. Stefan's unit is COM6, serial `4250304B38353817` — **the same
physical unit HOTEL benched**, so their "joints may be unsoldered" note applies.
- `[measured 29 Sep]` **Our maths and Maxim's hub agree within 0.5 BPM on identical raw data** (76 vs 76.5),
  and tracked together 66→85 while his rate rose. Strongest cross-check the HR code has had: it holds on a
  second vendor's optics. Our SpO2 99.8 % vs the hub's 98.6 % at R≈0.38.
- `[measured]` This **contradicts HOTEL's 29 Sep "no valid lock"** — theirs was PI 0.21-0.34 %, ours
  0.35-0.53 %. Contact, not the device.
- App now: **three tabs** (Bluetooth / USB / Compare), Connect per tab, both can run at once, Compare shows a
  rate per monitor + live difference + both waveforms; **recording captures every connected monitor** into one
  session with an agreement block in the PDF (paired seconds, mean difference, typical/worst, % within 5 BPM).
  `8d3dfdb`, fix `d5806f8`.

**Handover package** (`docs/handover-heart-rate-2026-09-30.md` + `C:\Users\Stefan\Documents\
heart-rate-handover-2026-09-30(.zip)`, 305 files, checksums): both sensors, the app, source, binaries, raw
recordings, the web version and HOTEL's sensor-B handover. Written to be read cold.

**Patents** — `docs/patents-earcup-ppg-2026-09-30.md`. **All three ear sites we measured are claimed by
someone**, two of them aviation competitors: Lightspeed US10701470 (forward of the tragus, to 2037), Spotlight
US12064247 (behind the ear over bone, to 2040), **Seabeck US11677428 family (a replacement cushion with an
earlobe pulse oximeter, to 2037, continuation still pending — and the lobe is our best site)**. Valencell's
foundational ear-PPG starts expiring 2026-2029 and they license openly. Not legal advice; the **Seabeck
granted claim text is the open question** and needs an attorney.

**Competitor** — `docs/spydr-air-competitor-notes.md`. SPYDR Air (commercial-headset version) is **a concept
page: one paragraph, three EMPTY headings, SBIR says unfunded development**. The helmet product is real:
3,803 sorties, 70 g, 20+ h, bone-conduction alerts. Their *Biosensors* 2020 study is the Route B work we
parked: UCSF hypoxia lab, 12 subjects across skin tones, 348 arterial samples to 70 %, **RMS 2.61 %**, and
**desaturation seen up to 50 s before finger and forehead sensors** — that early warning, not accuracy, is
their product claim.

**Open, and the thing I most want settled:** their mastoid result vs our measurement that behind-the-ear is
the worst of four sites. Most likely explanation is **cushion clamping pressure vs fingers** — test with a
cushion-mounted sensor before any H1 geometry is fixed. Also still open: the 48-50 BPM ear fault (needs a
reference), SpO2 calibration (parked, needs a certified oximeter), both sensors never yet run simultaneously
on a person, and everything measured is still one person.

**Also this week:** base-plate spec for Chris's four dev units (`docs/baseplate-spec.md`) — includes the size
finding that all four boards on one plate is ~442 x 145 mm, beyond a normal printer bed, so it likely splits
per dev unit; INDIA's brief is `inbox/stefan/2026-09-25-001`. GOLF's 21 Sep question about my §3b wedging
their board is **still unanswered**.

## 2026-09-21 — ear placement measured, channel fix, app reworked, phone version
**Late 21 Sep — two faults found live at the ear, one fixed, one OPEN.**
- `[measured]` **SpO2 showed "70%" that was not a measurement**: R=1.48 (plausible is ~0.4-0.8), and the curve
  simply clamps at 70. It displayed with spread 0, which looks *more* trustworthy, not less. **Fixed** in
  `a80738f`: R rejected outside 0.3-1.2, and any value landing on the curve clamp is rejected. Mirrored into
  `web/ppg.js`. **Rule: a clamped or saturated value must never be rendered as a reading.**
- `[measured]` **UNRESOLVED: heart rate reported 48-50 BPM with quality 0.83-0.95** (confidently wrong).
  `bench/spec.cs` on the captured channel: peaks at 0.66 Hz (40 BPM, strongest), 0.84 (50), 0.57 (34), and a
  weak 1.70 (102). A real pulse does not produce unrelated 34/40/50 peaks - that pattern says the sensor is
  rocking against skin. Raising the band-pass low edge to 0.7/0.8/0.9 Hz did NOT change it (`bench/filttest.cs`),
  so it is not just drift leaking through. **Two readings fit the data: a true ~102 with a half-rate lock, or a
  genuine ~50.** Waiting on Stefan's smartwatch as the reference; do not change the HR maths on a hunch first
  (that was the Friday mistake). Capture kept as `recordings/rec_LIVE_M1_PD1.txt`.


**The fix that mattered.** `[measured]` The app was *summing* each detector's two green measurements before
looking for a pulse. At the ear one is usually clean and the other swamped by stray light, so summing buried
the good one: concha SNR **43 -> 7**. Keeping all 8 channels separate: ear recordings went from 5 readings
(all wrong, sub-50) to 10 readings, none wrong; **Friday's weak finger recording from 35 readings with 4 wrong
(the 39 BPM Stefan saw) to 51 readings, 0 wrong.** So Friday's fault was the summing, not the HR algorithm and
not his finger — my Friday diagnosis was wrong and is corrected on the record (`48b3c31`).

**Ear placement study** — `docs/h1-ear-ppg-placement.md`, raw data committed. Lobe/just below the ear is best
(**pulse 4.35 %, beats a fingertip**); concha cleanest (SNR 43) but unreachable by a cushion; front of tragus
good (SNR 25); **behind the ear worst** (0.5 %, channels disagree) — it sits over bone. LED current makes **no
difference** to SNR across 5x (so run at ~1/5 power); brighter saturates. Every winner was a **low-DC** channel
→ stray light is the enemy: opaque LED/detector barrier, matte black sealed pocket. Talking halves HR
availability (10/10 s → 5/10 s) but the readings it gives stay right; **red/IR collapse while talking**
(SNR 21-24 → 4), so SpO2 must be gated on quiet windows. Red/IR unusable at the tragus, fine at the lobe.

**App now:** dark navy theme, luminous-red HR, pale-cyan SpO2 (his swatch, sampled 159,238,254), green-yellow
glowing trace, Chakra Petch display font (SIL OFL, embedded in the exe), Aerosense logo (WebP → PNG via
`bench/imgconv.cs`, lifted 30 % for the dark background — **ask for an official light-on-dark logo**), hold the
last reading for 8 s through movement, first reading 2-3 s sooner (5 s window until one appears; 4 s was tested
and rejected — it invents sub-50 readings), and **named session recording → PDF report + CSV** in `bin/sessions`
(gitignored: real names). `--shot <png> [demo]` renders the window for layout checks without hardware.

**Phone version** `web/` — Chrome-on-Android page, same maths, Bluetooth direct. **Maths verified** against four
of today's recordings (72/72/70/78 vs C# 73/72/69/78); **the BLE half has never driven the board — first phone
run is its test.** Needs HTTPS or localhost; README has the USB port-forwarding route. Hosting undecided.

**SpO2 calibration:** routes written up in the placement doc (reference-comparison vs controlled desaturation,
FDA 2025 draft: >=24 subjects, 480 paired points, ARMS <3 %). **Parked on Stefan's call.** The H1 risk is on
record: hypoxia matters at 80-90 %, which cheap calibration cannot validate.

**Still open:** SSH key not yet added on GitHub (HTTPS still in use); permission net installed but needs a
restart to go live; INDIA 09-18-001 open; H1 (Chris step 5) not started; `D:\Github\kilo` still locked.

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

## 2026-09-21 — Roof Final 3 Fusion enclosure handoff

Codex added docs/fusion-enclosure-update-2026-09-21.md and supporting evidence under docs/fusion-enclosure-evidence/2026-09-21/. Separate Fusion design Roof Final 3 - NEO Modern Enclosure incorporates the three revised port positions, modern shell, cable-tie ramps and rounded rear cover. CAD profile/interference checks passed; manufacturing fit remains unverified. Included studio image depicts the previous Stefan Is revision, not the updated hole positions. CAD exports are local in the Fusion workspace; FusionChatGPT publication remains unconfirmed due to index.lock write denial at preparation time.


## 2026-09-25 - AeroNode Fusion concept handoff
Rounded RV-8 packaging V3 and original-device Blender animation documented in [AeroNode handoff](../docs/aeronode-fusion-update-2026-09-25.md). Titanium/stainless are candidates only; no crash qualification, final material selection or demonstrated minimum size. Connector access, cooling and mounting remain unresolved. Modular building project excluded. CAD binaries remain in the designated Fusion backup workspace; remote publication not confirmed.


## 2026-09-25 — Latest Fusion assembly, boards and Cheeseboard handoff

See [latest Fusion handoff](../docs/fusion-latest-handoff-2026-09-25.md): dedicated AeroVault capsule concept, NOVA/COMPACT variants, closure fixes, board references, grid-wall Cheeseboard bracket, and verified 18-second Blender assembly animation. Supersedes earlier same-day architecture/presentation notes. No crash qualification; source CAD remote backup pending. Modular building/bathroom excluded.


## 2026-09-25 — H1 headset animation

Completed and verified 20-second 1080p Blender studio rotation of H1 - Assembly - Updated. See [latest handoff](../docs/fusion-latest-handoff-2026-09-25.md#h1-headset-studio-rotation). MP4, Blender scene and Fusion snapshot saved locally; CAD backup publication pending.


## 2026-09-28 — Fusion TPU cushion sensor prototype

Current design uses a single TPU cushion with integral bottom-loading sensor sleeve and flexible keeper lips. Rigid plate/screw alternative is hidden history. Latest rounded shoulder remains stepped, not fully blended. CAD only; retention, optical performance and physical fit unverified. Existing ear-placement findings remain applicable. See [handoff](../docs/fusion-handoff-2026-09-28-ppg-tpu.md). Local exports and Fusion cloud saved; FusionChatGPT push pending Git permissions.


## 2026-09-30 — H1 assembly revision 2 and Fusion handoff

Current H1 video is revision 2: staged carrier/cushion insertion with preassembled outer frame; supersedes the crossing exploded-view choreography. Verified 20 s / 1080p / 24 fps. All-frame mesh checks found no new intersection pairs; original CAD seating overlaps and eight broken source references remain documented. Also records the four-piece nominal 1 mm silicone-cushion mold and approximate Bose A30 visual model. See [handoff](../docs/fusion-handoff-2026-09-30.md). Large artifacts remain local; FusionChatGPT publication pending Git permissions. No building/bathroom or private recording data included.


## 2026-10-05 — Higgsfield video work imported from Codex

Latest six-reference cut: 1 → 2 → 3 → 4 → 5 → 6, 1440p / motion-interpolated 60 fps / 30 s. Two selected older cuts also converted to 4K / 60 fps. See [handoff](../docs/higgsfield-evidence/2026-10-05/README.md). Original references, prompts, validation and checksums recorded. All MP4 exports copied to ignored local `artifacts/higgsfield/videos/`; a Git clone does not restore them. Newest cut awaits Stefan/client approval. Numerical no-stop checks do not establish perceptually perfect motion or accurate CAD geometry.
