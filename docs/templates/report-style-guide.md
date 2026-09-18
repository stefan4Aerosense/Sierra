> **Provenance:** moved into kilo 2026-09-18 from `stefan4Aerosense/Aerosense@679ce5e:docs/templates/report-style-guide.md`. **kilo is now the canonical home** (Stefan ruled 2026-09-17 that kilo is the working repo). The Aerosense copy is frozen at that SHA — update here, append the changelog, never edit the old copy.

# Aerosense report — style guide (house style)

> **Canonical:** `stefan4Aerosense/Aerosense` → `docs/templates/report-style-guide.md` · **v1.0 · 2026-07-30.**
> Pairs with the skeleton `aerosense-report.html`. Distilled from the internal "Bench Sensor Catalogue"
> report. Follow this whenever Stefan asks for a report "in the house style."

## What a good Aerosense report is
A **self-contained HTML file** that a reader learns from top-to-bottom: broad accurate overview first →
progressively more detail → and an honest close about what's unverified or didn't work. Its credibility
comes from **provenance** — every number is traceable to a named source and tagged by how sure we are.

## Structure (order matters)
1. **Confidential banner** (`.cbar`) + **hero** (badge with Rev + date, title, 1–3 sentence subtitle,
   "Prepared for" + a **Sources** line naming the inputs).
2. **Sticky table of contents** (`nav.toc`).
3. **§1 Executive summary** — the whole story in one paragraph, *including the main caveat*, then a row
   of **stat tiles** (`.tiles`) for the at-a-glance numbers, then a "how to read this" callout.
4. **Detail** — a table (highlight the answer row with `.best`/`.win`), and/or a **card grid**
   (`.ledger`+`.chipcard`) for per-item detail (name, meta, optional photo, specs, verification, source).
5. **Comparison / deep-dive** — diagrams (`figure.diagfig`), collapsible detail (`details.vid`).
6. **Open items & honest caveats** — what's unverified/assumed and who owns it; what you tried that failed.
7. **§Sources** — mandatory. Every report ends here.

The components are a **menu, not a mandate.** A simple report can be just hero + summary + one table +
Sources. Use cards/figures/comparisons only when the content earns them.

## Provenance discipline (the whole point)
- **Tag every claim.** Map our worklog tags to the visual pills:
  - `[measured]` (we ran it / read it in hand) → `<span class="tag tag-ok">MEASURED</span>`
  - `[fetched]`/spec-sheet (datasheet, vendor page, doc) → `<span class="tag tag-med">SPEC / FETCHED</span>`
  - `[assumed]`/unverified → `<span class="tag tag-un">ASSUMED / UNVERIFIED</span>`
- **State verification status honestly and separately from usefulness** — e.g. "1 of 13 verified in
  hand, the rest are spec-sheet only." A red UNVERIFIED item can still be the right answer.
- **Flag "do not assume — verify against the source"** wherever a value is second-hand or a register/
  spec must be read directly. Never launder an assumption into a fact (see the grommet lesson — a small
  wrong assumption presented confidently is the expensive kind).
- **Sources section is not optional.** Name inputs precisely enough to re-find them.
- **Never fabricate data.** No invented test results/measurements — only report what was measured or
  cited. If something can't be verified from the desk, say "Unknown / route to a live test," don't guess.

## Callouts
`.callout` (info/sky) · `.callout.warn` (amber caution) · `.callout.real` (green confirmed) ·
`.callout.ask` (indigo — an open question only the human can answer).

## Hard rules (self-contained + confidential)
- **Fully self-contained:** all CSS inline in `<style>`; **any image base64-embedded** (`data:` URI);
  **zero external references** (no CDN, web fonts, remote images) — it must open offline.
- **Confidential:** carries the `Aerosense Internal · Confidential` banner + footer.
- **Delivery = a local `.html` file** handed over via SendUserFile (`display:"render"`). **Do NOT
  auto-publish** to any cloud/Artifact — these are internal/Confidential. Publish an Artifact link only
  if Stefan explicitly asks for a shareable one.

## How I generate one
1. Gather + **verify** the inputs; decide each claim's tag. Never guess a number.
2. Copy `aerosense-report.html`, fill placeholders, keep only the components the content needs.
3. Base64-embed any images; confirm no external refs remain.
4. Deliver the local file; offer to keep a copy in `docs/reports/`.

## Two report modes (learned from more examples)
Same visual foundation (dark hero, panel sections, tiles, callouts) — two audiences:

- **Internal · Confidential** (default for engineering) — carries the `.cbar` banner. **Banner colour
  signals sensitivity:** *blue* stripes (`#0b3a5c/#0e4a75`) = normal internal; ***red* stripes
  (`#7f1d1d/#991b1b`) + red badge = higher sensitivity / competitor material** (e.g. a teardown of
  another vendor's product, like the Bose A30 review). Provenance-tagged, engineering detail, cards/tables.
- **External / investor** (AeroSense-branded) — **no** Confidential banner; the badge is brand/context,
  not "Internal". Leads with a **technical-credibility narrative** ("the science, on real flight data"),
  per-topic deep-dive sections, and **charts on real data**. Polished, prose-forward, less jargon. Same
  honesty rules apply, **harder** — investor material must be defensible; never over-claim or fabricate.

## Extra components (pull in only when the content needs them)
- **Annotated photo hotspots** (teardown/hardware): `.board-wrap` (`position:relative`) with absolutely
  positioned `.hs` boxes over a board photo, each with a hover `.tip` tooltip (ref / marking / role),
  plus a `.hslegend`. Great for chip-ID on a board photo.
- **Galleries:** `.gallery` (3-col) / `.gallery4` (4-col) responsive figure grids for photo sets.
- **Chip labels / colour chips:** `.chiplbl` (coloured pill), `.bchip` (swatch) for legends/mapping.
- **Collapsible roster:** `details.roster` for a long part/chip table kept folded.
- **Diagram figures:** `figure.vdiag`; use a `.diagblocked` note when a diagram couldn't be produced.
(CSS for these lives in the Bose A30 review; copy the class blocks when needed rather than reinventing.)

## Charts / plots (machine reality — important)
This machine has **no local charting engine** (no general-purpose Python/Node). So data charts must be:
- **pre-rendered PNGs embedded base64** (the investor report's approach — `data:image/png` `<img>`), or
- **hand-authored inline SVG** (small/simple charts written directly).
Every chart **cites its data source in the caption**, and never depicts data it doesn't have.

## Changelog
- **v1.1 · 2026-07-30** — added the two modes (internal-confidential vs external/investor), the
  banner-colour sensitivity signal (blue normal / red competitor), teardown components (photo hotspots,
  galleries, chip labels, rosters), and the charts-as-PNG/SVG reality. Learned from `aerosense_investor.html`
  + the Bose A30 teardown review.
- **v1.0 · 2026-07-30** — initial capture (design system + editorial rules) from the Bench Sensor
  Catalogue internal report; first use = H1 weight/CG report.
