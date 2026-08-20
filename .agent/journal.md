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
