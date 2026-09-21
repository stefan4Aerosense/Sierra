# Phone version of the heart-rate tester

A web page that talks to the MAX86171 board over Bluetooth straight from **Chrome on Android**, so a phone can
take readings with no PC involved. Same measurement logic as the desktop tool.

| File | What it is |
|---|---|
| `app.html` | The page itself: UI, connect, live readings, recording |
| `os61.js` | Bluetooth client and sensor setup — port of `OS61.cs` + `PpgStream.cs` |
| `ppg.js` | Heart rate and SpO2 maths — port of `HeartRate.cs` + `SpO2.cs` |
| `test.html` + `recordings.js` | Checks the ported maths against real 21 Sep ear recordings |
| `serve.ps1` | Tiny local server (`http://localhost:8123`), nothing to install |

## State `[measured]` / `[assumed]`

- **The maths is verified.** `test.html` replays four of the 21 Sep ear recordings through the JavaScript port
  and compares with the C# results: EAR4 72 vs 73, QUIET 72 vs 72, TALK 70 vs 69, EAR1 78 vs 78 BPM, no wrong
  sub-50 readings, SpO2 curve matches. `[measured]`
- **The Bluetooth half has not been run against the board yet** — it is a line-by-line port of the working C#
  client, but until it has driven the real hardware it is `[assumed]`. First run on a phone is the test.
- PDF reports are not ported; the page saves a CSV. The desktop tool still does PDFs.

## Running it

**Bluetooth in a browser only works on a secure origin** — HTTPS, or `localhost`. Plain
`http://192.168.x.x` will not work, so serving it over the office network is not enough on its own.

### On this PC (layout and maths only; the pane cannot pair with the board)
```
powershell -ExecutionPolicy Bypass -File web\serve.ps1
```
then open `http://localhost:8123/app.html`, or `/test.html` for the maths check.

### On the phone, without hosting anything (USB)
1. On the phone: Settings → Developer options → **USB debugging** on, and plug it into the PC.
2. On the PC in Chrome: `chrome://inspect/#devices` → **Port forwarding** → add `8123` → `localhost:8123`.
3. Run `serve.ps1` on the PC, then on the phone open `http://localhost:8123/app.html`.
   The phone treats it as localhost, so Bluetooth is allowed.

### On the phone, properly (hosting — undecided as of 21 Sep 2026)
Put these files behind any HTTPS host and open the link. GitHub Pages works but is public; an internal host
keeps it private. Nothing here holds credentials or company data, but the sensor protocol would be visible.

## Notes

- The page never touches the board's firmware-update service.
- Chrome asks the user to pick the device every session; that prompt is the browser's permission model and
  cannot be skipped.
- Screen-off or switching apps will suspend the page and stop the stream — a normal app would be needed for
  long unattended recordings.
