> **Provenance:** moved into kilo 2026-09-18 from `stefan4Aerosense/Aerosense@679ce5e:docs/fusion360-operating-notes.md`. **kilo is now the canonical home** (Stefan ruled 2026-09-17 that kilo is the working repo). The Aerosense copy is frozen at that SHA — update here, append the changelog, never edit the old copy.

# Fusion 360 — operating notes (portable, project-agnostic)

> **Canonical location:** `stefan4Aerosense/Aerosense` → `docs/fusion360-operating-notes.md`
> (owned by the Aerosense orchestrator). **Version 1.0 · 2026-07-29.**
>
> **What this is.** Project-agnostic know-how for driving **Fusion 360 via its MCP tools**, distilled
> from what the Aerosense Fusion agent learned the hard way — with **no** project domain knowledge.
> Hand it to any new Fusion agent so it starts fluent instead of relearning these.
>
> **Mirroring:** other repos get a **copy with a provenance header** (this canonical path + the commit
> SHA copied from + "update by re-copying the canonical, never edit the mirror"). Append to the
> changelog; don't rewrite history.

---

## The toolset — 4 MCP tools (usually DEFERRED; load via ToolSearch before first use, batched)

```
ToolSearch "select:mcp__Autodesk_Fusion__fusion_mcp_read,mcp__Autodesk_Fusion__fusion_mcp_execute,mcp__Autodesk_Fusion__fusion_mcp_update,mcp__Autodesk_Fusion__fusion_mcp_electronics_read"
```

- **`fusion_mcp_read`** — `projects`, `document` (search / open-list / recent), `apiDocumentation`,
  `screenshot` (named camera angles), `activeCommand`. Pure read.
- **`fusion_mcp_execute`** — `script` (arbitrary Python: `adsk.core` / `adsk.fusion`) + `document`
  open/save/close. The hands.
- **`fusion_mcp_update`** — `undo` / `redo` only.
- **`fusion_mcp_electronics_read`** — read PCB/schematic (boards/nets/parts). Read-only.

**Prereq:** Fusion 360 desktop must be **running with the target document open**. With no doc open,
`execute→script` errors and `read→document` returns 0 open documents. Check `read→document` first.

## The golden workflow (every time)

1. **`read→apiDocumentation`** to look up the exact class/method/property **before** writing a script.
   Never guess an API shape.
2. **`execute→script`**: define `def run(_context):`; use `print()` for output; **do NOT catch
   exceptions** — you need the raw traceback. Keep scripts small.
3. **Verify after every run** — `read→screenshot` (iso/front/top) or re-read state. A script that "ran"
   is not a result that's right.

## Common operations (verified entry points)

- **Iterate the assembly:** `root.allOccurrences` → `occurrence.component` → `component.bRepBodies`.
  (See the entityToken scar — do **not** dedup occurrences by entityToken.)
- **Mass / CG:** `Design.physicalProperties` or `Component.getPhysicalProperties(accuracy)`; use
  `HighCalculationAccuracy` (±0.1%) for a final number, `MediumAccuracy` for quick per-part.
- **Interference / clearance:** `Design.createInterferenceInput(occurrences)` → `analyzeInterference`;
  set `areCoincidentFacesIncluded=False` or every touching face reads as a clash.
- **Backup / export:** `Design.exportManager` → `createFusionArchiveExportOptions(path)` for `.f3d`,
  `createSTEPExportOptions` for STEP; `exportManager.execute(options)`.
- **Screenshot:** `Viewport.saveAsImageFile(path, w, h)`.

## Hard-won scars — don't relearn these

- **Material assignment is NOT geometry-safe on a broken timeline.** `body.material = ...` forces a full
  timeline recompute; if a body was created by a broken/failed feature, it **raises**. The loop is **not
  transactional** — earlier changes commit before the exception. Always re-read state after a raise, and
  back up first. On a damaged model, expect material edits to partially apply.
- **Don't dedup components by `entityToken`** — it can collapse many distinct components into one
  (measured: 22→2). Component **names** also collide. Iterate `root.allOccurrences`; dedup only for
  **logging** by `(component.name, body.name)`, never for assignment (material sets are idempotent).
- **Mass is only as good as the materials.** A body on a default/placeholder material gives a garbage
  weight. **Audit every body's material before trusting a weight** — flag any left on
  "Steel"/"Stainless"/a default that shouldn't be.
- **Fusion libraries have gaps.** Some real materials don't exist (measured: 17-7 PH stainless — only
  17-4; no gel/memory-foam — only Polyurethane Foam). Create a **custom material** rather than silently
  accept a wrong proxy, and always flag a proxy in your report.

## Safety discipline (any human-owned design)

- Treat every design as live. **Back up first** — a cloud version save + a local `.f3d` snapshot to a
  gitignored `backups/` — before running scripts. Read-only analysis is free, but still on backed-up
  work, and **inspect the output after**.
- **Never change geometry/materials/parameters without the human's explicit per-task permission.** Ask
  whether the design is co-edited; if so, confirm the other party is out before changing anything, prefer
  non-destructive changes, and announce what you changed.
- Worth replicating: a `.claude/settings.json` deny-net (deny `fusion_mcp_update`; off-switch deny
  editing your own `.claude/` + `orders.md`). **Windows caveat:** absolute drive-letter path deny rules
  (`//D:/…`, `///D:/…`) proved **inert** — relative forms like `.claude/**` work. Prove any deny rule
  actually refuses from a session booted after the change — a deny you haven't seen fire is decoration.

## Deliver

Findings as committed `.md` reports in your repo (numbers + a screenshot + provenance), not just chat.
Commit → push → verify with `git ls-remote`.

---

## Changelog (append-only)

- **v1.0 · 2026-07-29** — initial capture from the Aerosense Fusion agent's H1 work (guard verification +
  material pass): the toolset, golden workflow, common ops, and the four scars (broken-timeline material
  edits, entityToken dedup, materials-drive-mass, library gaps) + safety discipline.
