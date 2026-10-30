# Browser snippets: reading the report site and the task board with Claude in Chrome

All of these were run against the live sites on 30 Oct 2026. They are **read-only**: each does `GET` requests with the
signed-in person's own cookies and changes nothing on the server. Run them with the `javascript_tool` of Claude in
Chrome in **a tab you opened yourself** (`tabs_create_mcp`), never in the person's own working tab.

## 0 · Load the tools once (one ToolSearch call)

```
ToolSearch  query: "select:mcp__claude-in-chrome__tabs_context_mcp,mcp__claude-in-chrome__tabs_create_mcp,mcp__claude-in-chrome__tabs_close_mcp,mcp__claude-in-chrome__navigate,mcp__claude-in-chrome__get_page_text,mcp__claude-in-chrome__javascript_tool,mcp__claude-in-chrome__computer,mcp__claude-in-chrome__find"
```

Then `tabs_context_mcp` with `createIfEmpty: true`. If a tool says the group no longer exists, call it again: tab ids
from an earlier session are never valid.

## The three quirks that shape every snippet

1. **`javascript_tool` returns only about 1,000 characters.** Anything longer is cut with `[TRUNCATED]`. So snippets
   either return a short answer, or write their result into the page and you read it with `get_page_text` (about 50,000
   characters per call).
2. **A result containing a URL query string or cookie-like text is refused** with `[BLOCKED: Cookie/query string data]`.
   Strip `?…` from anything you return.
3. **The extension cannot open `file://` pages.** It rewrites them to `https://file///…`. Check local HTML in the
   Claude desktop app's built-in browser instead. Note that it blocks `localStorage` on those pages, so autosave can't be
   tested there.

The two ways around quirk 1:

- **A JSON endpoint:** `navigate` the tab straight to it, then `get_page_text`. The raw JSON comes back as text.
- **A big dump:** run a snippet that writes the text into the page body, then `get_page_text`. Do this only in **your
  own scratch tab**: it replaces that tab's page.

## 1 · The person's report (reports.aerosenseaviation.com)

```
navigate        https://reports.aerosenseaviation.com/api/report
get_page_text   -> the raw JSON: {"report": {...}, "week": {...}, "outcomes": [...], "days": [...]}
```

Save that text **verbatim** with the Write tool as `report_api_<week_ending>_<draft|submitted>.json`. Then prove the
copy is exact with `tools/report_hash.py` (§6).

What is in it:

- `report.items[]`: the person's rows, in form order, with `id`, `title`, `status`, `technology`, `due_date`,
  `priority`, `outcome`, `evidence_urls`, `hours` and `note`.
- `report.previous.commitments[]`: last week's commitments, with `text`, `done_means`, `task_id` (null if not on the
  board), `due_day` and `met`.
- `report.commitments[]`: this week's, filled in by the person.
- `report.status`, `submitted_at`, `revision`, `timeliness`, `hours_total`, `updated_at` and `drive_file_id`.
- `week`: `opens_at`, `due_at` and `missing_at` (SAST), and `now`.

## 2 · The person's cards and every comment on them (todo.aerosenseaviation.com)

Open a scratch tab on `https://todo.aerosenseaviation.com/preflight`, then:

```js
const PERSON = 'Stefan';                  // the board's display name (see /api/users)
const SINCE  = '2026-10-23T14:00';        // UTC; usually last Friday's submit time
const g = async p => (await fetch('/api/' + p, {credentials: 'include'})).json();
const tasks = await g('tasks');           // open cards only; completed ones are in /api/archive/tasks
const mine = tasks.filter(t => (t.assigneeNames || []).includes(PERSON) || t.taskLeadName === PERSON);
let out = `OPEN CARDS ${tasks.length}; ${PERSON}'s ${mine.length}\n`;
for (const t of mine) {
  const cs = await g('tasks/' + t.id + '/comments');
  out += `\n=== [${t.id}] ${t.title}\n status=${t.status} cat=${t.category} prio=${t.priority} gate=${t.gateId} due=${(t.dueDate || '').slice(0, 10)} lead=${t.taskLeadName} assignees=${(t.assigneeNames || []).join('/')} updated=${(t.updatedAt || '').slice(0, 10)} comments=${t.commentCount}\n desc: ${(t.description || '').replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').slice(0, 400)}\n`;
  for (const c of cs) out += `  - ${c.createdAt.slice(0, 16)} ${c.creatorName}${c.createdAt >= SINCE ? ' [NEW]' : ''}: ${(c.content || '').replace(/\s+/g, ' ').slice(0, 600)}\n`;
}
window._full = out;
document.body.innerHTML = '<main><pre id="dump"></pre></main>';
document.getElementById('dump').textContent = out; out.length
```

Then `get_page_text`. If the length is over about 48,000, show the rest with
`document.getElementById('dump').textContent = window._full.slice(45000)` and call `get_page_text` again.

## 3 · What moved on the board since last Friday (activity, completed cards, chat)

```js
const SINCE = '2026-10-23T14:00';
const g = async p => (await fetch('/api/' + p, {credentials: 'include'})).json();
let out = '';
const act = await g('activity');          // the latest 100 events
out += `ACTIVITY (oldest shown ${act[act.length - 1]?.createdAt})\n`;
for (const a of act.filter(a => a.createdAt >= SINCE)) out += `${a.createdAt.slice(0, 16)} ${a.userName} ${a.action} ${a.entityType} "${(a.entityTitle || '').slice(0, 90)}"\n`;
const arch = await g('archive/tasks');
out += `\nCOMPLETED since ${SINCE}\n`;
for (const t of arch.filter(t => (t.updatedAt || '') >= SINCE)) out += `${t.updatedAt.slice(0, 16)} ${t.title} | lead=${t.taskLeadName} as=${(t.assigneeNames || []).join('/')}\n`;
const ch = await g('chat/channels');
for (const c of ch) {
  const m = await g('chat/channels/' + c.id + '/messages');     // the latest 50 per channel
  const recent = (Array.isArray(m) ? m : []).filter(x => x.createdAt >= SINCE);
  out += `\n## #${c.name}: ${recent.length} since\n`;
  for (const x of recent) out += `${x.createdAt.slice(0, 16)} ${x.senderName}: ${(x.content || '').replace(/\s+/g, ' ').slice(0, 900)}\n`;
}
document.body.innerHTML = '<main><pre id="dump"></pre></main>';
document.getElementById('dump').textContent = out; out.length
```

## 4 · The Preflight page (the company AI's daily read-out)

```
navigate        https://todo.aerosenseaviation.com/preflight
get_page_text   (if it comes back empty, the page is still rendering: call get_page_text again)
```

Look for the person's name in **"Who's on what"**, **"Watch"** and **"Needs a call"**. Those are the items the company
AI is waiting on from them.

## 5 · After the person submits: the board comments, and which cards really closed

```js
const PERSON = 'Stefan', SUBMIT = '2026-11-06T07:00';   // UTC, from report.submitted_at
const g = async p => (await fetch('/api/' + p, {credentials: 'include'})).json();
const isMine = t => (t.assigneeNames || []).includes(PERSON) || t.taskLeadName === PERSON;
const open = (await g('tasks')).filter(isMine), done = (await g('archive/tasks')).filter(isMine);
let out = '';
for (const [state, list] of [['OPEN', open], ['COMPLETED', done]]) for (const t of list) {
  const cs = (await g('tasks/' + t.id + '/comments')).filter(c => c.createdAt >= SUBMIT && c.creatorName === 'Weekly report');
  if (cs.length || state === 'COMPLETED' && t.updatedAt >= SUBMIT) out += `${state} ${t.title.slice(0, 60)} | ${cs.map(c => c.content.slice(0, 80)).join(' || ')}\n`;
}
document.body.innerHTML = '<main><pre id="dump"></pre></main>';
document.getElementById('dump').textContent = out; out.length
```

## 6 · Prove a saved copy matches the live report

```
python3 tools/report_hash.py report_api_<week>_submitted.json > check.js
```

Paste the whole of `check.js` into `javascript_tool` on a `https://reports.aerosenseaviation.com/` tab.

- `{"fields": 113, "mismatches": []}` means the copy is exact.
- Any mismatch names the field, with both values.

On 30 Oct this caught an edit Chris made **after** submitting: the site saves edits into a submitted report without a
new revision. Re-save the JSON and check again until it is clean.

## 7 · Finding other endpoints (only if you need one that isn't listed)

```js
const srcs = [...document.querySelectorAll('script[src]')].map(s => s.src); const found = new Set();
for (const s of srcs) { const t = await (await fetch(s)).text();
  for (const m of t.matchAll(/["'`]\/api\/([A-Za-z0-9_\-\/\$\{\}\.]*)/g)) found.add(m[1].replace(/\$\{[^}]*\}/g, 'X')); }
[...found].join(' | ')
```

Use only the `GET` ones. **Never call `POST`, `PUT` or `DELETE`.** Writes to the board (comments, Complete, "Mark
task done") are done by the person, or by you only after their explicit yes for that one action.
