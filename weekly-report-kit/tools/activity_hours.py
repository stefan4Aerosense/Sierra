#!/usr/bin/env python3
"""A floor for the hours worked this week, from the record: the person's prompts to Claude agents and/or commit times.

    python3 activity_hours.py --since 2026-10-23T16:00 --until 2026-10-30T23:59                # Claude Code prompts
    python3 activity_hours.py --since ... --until ... --git C:/path/FusionChatGPT --git C:/path/kilo   # + commit times
    python3 activity_hours.py ... --exclude chimp                                              # leave a project out

Claude Code keeps every session as JSONL under ~/.claude/projects/<project>/ (Windows: %USERPROFILE%\\.claude\\projects).
A "prompt" is a user message that is not a tool result. Activity is merged across all sources: events closer than
--gap minutes count as one stretch, and each stretch gets --tail minutes added. This is screen time with agents, NOT
the hours worked: bench time, printing, sanding, resin and assembly come on top and must come from the person.
"""
import argparse
import glob
import json
import os
import subprocess
from collections import defaultdict
from datetime import datetime, timedelta, timezone
from pathlib import Path


def claude_events(since, until, exclude):
    ev = []
    for d in glob.glob(str(Path.home() / ".claude" / "projects" / "*")):
        proj = os.path.basename(d)
        if any(x in proj for x in exclude):
            continue
        for f in glob.glob(os.path.join(d, "*.jsonl")):
            if os.path.getmtime(f) < since.timestamp():
                continue
            for line in open(f, encoding="utf-8", errors="ignore"):
                try:
                    e = json.loads(line)
                except ValueError:
                    continue
                if e.get("type") != "user" or e.get("isMeta") or e.get("isSidechain") or not e.get("timestamp"):
                    continue
                c = e.get("message", {}).get("content")
                if isinstance(c, list):
                    if any(isinstance(x, dict) and x.get("type") == "tool_result" for x in c):
                        continue
                    c = " ".join(x.get("text", "") for x in c if isinstance(x, dict))
                c = c or ""
                if not c.strip() or c.startswith(("<command", "<local-command", "<task-notification")) or "This session is being continued" in c[:200]:
                    continue
                t = datetime.fromisoformat(e["timestamp"].replace("Z", "+00:00"))
                if since <= t <= until:
                    ev.append((t, "claude:" + proj))
    return ev


def git_events(repo, since, until):
    out = subprocess.run(["git", "-C", repo, "log", "--all", f"--since={since.isoformat()}", f"--until={until.isoformat()}",
                          "--format=%aI"], capture_output=True, text=True, encoding="utf-8").stdout.split()
    return [(datetime.fromisoformat(s), "git:" + os.path.basename(repo.rstrip("/\\"))) for s in out]


def merged(ev, gap, tail):
    tot, start, last = timedelta(), None, None
    for t, _ in sorted(ev):
        if start is None:
            start = last = t
            continue
        if t - last > gap:
            tot += (last - start) + tail
            start = t
        last = t
    if start is not None:
        tot += (last - start) + tail
    return tot.total_seconds() / 3600


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--since", required=True)
    ap.add_argument("--until", required=True)
    ap.add_argument("--tz", type=float, default=2.0, help="local UTC offset in hours (SAST = 2)")
    ap.add_argument("--git", action="append", default=[], help="a repo whose commit times count as activity (repeatable)")
    ap.add_argument("--no-claude", action="store_true")
    ap.add_argument("--exclude", action="append", default=[], help="skip Claude projects whose folder name contains this")
    ap.add_argument("--gap", type=int, default=20)
    ap.add_argument("--tail", type=int, default=10)
    a = ap.parse_args()
    tz = timezone(timedelta(hours=a.tz))
    since = datetime.fromisoformat(a.since).replace(tzinfo=tz)
    until = datetime.fromisoformat(a.until).replace(tzinfo=tz)
    ev = [] if a.no_claude else claude_events(since, until, a.exclude)
    for r in a.git:
        ev += git_events(r, since, until)
    gap, tail = timedelta(minutes=a.gap), timedelta(minutes=a.tail)
    print(f"ALL sources merged: {merged(ev, gap, tail):.1f} h from {len(ev)} events (a floor, not the hours worked)")
    days, bysrc = defaultdict(list), defaultdict(list)
    for t, s in ev:
        days[t.astimezone(tz).strftime("%a %d %b")].append((t, s))
        bysrc[s].append((t, s))
    for d in sorted(days, key=lambda k: min(days[k])[0]):
        e = sorted(days[d])
        print(f"  {d}: {merged(e, gap, tail):.1f} h, {len(e)} events, {e[0][0].astimezone(tz):%H:%M}-{e[-1][0].astimezone(tz):%H:%M}")
    for s in sorted(bysrc, key=lambda k: -merged(bysrc[k], gap, tail)):
        print(f"  {s:40s} {merged(bysrc[s], gap, tail):5.1f} h  {len(bysrc[s])} events")


if __name__ == "__main__":
    main()
