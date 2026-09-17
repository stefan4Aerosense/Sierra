# max86171-hr-spo2 — heart-rate + SpO₂ tester for the MAX86171EVSYS#

Windows desktop tool that talks to the **MAX86171EVSYS#** evaluation kit (MAXSensorBLE# + MAX86171_OSB#)
over **Bluetooth LE**, configures the MAX86171, and shows heart rate, an **uncalibrated** SpO₂ estimate and
the pulse waveform. Built 2026-09-17 by KILO with Stefan at the bench. **Testing tool, not a medical device.**

Full write-up of how it works and what it does and does not establish: [`docs/max86171-evsys-bringup.md`](../../docs/max86171-evsys-bringup.md).

## Build

No SDK needed — uses the C# compiler that ships with Windows (.NET Framework 4.x) and the WinRT Bluetooth API.

```bat
build.cmd  C:\some\short\output\dir
```

The output directory must be a **short path**: the Framework `csc.exe` fails with
`CS2021: File name ... is too long or invalid` on long paths `[measured]`. Produces:

- `HeartRateTester.exe` — the window (Start / Stop, BPM, SpO₂, waveform).
- `hrdiag.exe` — same code as a console tool:
  - `hrdiag --selftest` — offline tests of the HR/SpO₂ maths on synthetic signals (no hardware).
  - `hrdiag --diag 40` — connect, stream 40 s, print one line per second.
  - `hrdiag --replay2 25.3 green.txt red1.txt ir1.txt red2.txt ir2.txt` — replay recordings.

Binaries are gitignored (`bin/`). Rebuild from source.

## Before you press Start

- **Close the Maxim GUI.** The board accepts one BLE connection; while the GUI holds it, GATT
  characteristic discovery returns an empty list.
- If you get *Board not found* right after another program disconnected, wait a few seconds and retry.
- Rest a **warm fingertip lightly** across the sensor window. Pressing hard weakens the pulse.

## What is in here

| Path | What |
|---|---|
| `OS61.cs` | Minimal client for the kit's BLE protocol (reverse-engineered from the MAXREFDES280 GUI's .NET assemblies) |
| `PpgStream.cs` | Sensor configuration (all LEDs, 4 photodiodes) + FIFO frame parsing |
| `HeartRate.cs` | Band-pass + autocorrelation heart rate; multi-channel "use the clearest" |
| `SpO2.cs` | Ratio-of-ratios SpO₂ with lock-in amplitudes at the pulse frequency; multi-path |
| `App.cs`, `SelfTest.cs`, `build.cmd` | UI, tests/replay, build |
| `bench/` | The one-off instruments used to produce the findings (recorder, analysers, LED survey, IL disassembler used on the GUI). Hard-coded to board address `F1324B0CFE32`; compile with the same references as `build.cmd`. |
| `recordings/` | Raw sample recordings behind every number in the write-up — see below |

## Recordings

One integer per line = one raw 20-bit ADC sample (signed), in order. File name `rec_<session>_M<meas>_PD<n>`,
where `PD1`/`PD2` are the **first/second PPG channel of that measurement** (photodiodes PD1/PD2 for these
sessions). All recorded 2026-09-17 on Stefan's fingertip, host Windows 11 Pro 10.0.26200.

LED select codes are `driver:select`; see the write-up for the map. Currents are register codes.

| Files | Rate / averaging | Measurements |
|---|---|---|
| `raw1.txt` | 100.6 Hz measured, AVER 1×, 22 s | single green (A:00, `0x0A`), PPG1 = PD1 only |
| `sp_M*_PD*` | 25 Hz, 8×, 28 s | M1 green A:00 `0x0A` · M2 **red** B:00 `0x0A` · M3 **IR** B:01 `0x14` |
| `rec_A_*` | 10 Hz, 64×, 45 s | M1 green A:00 `0x0A` · M2 red B:00 `0x0A` · M3 IR B:01 `0x14` · M4 red B:00 `0x14` ADC-range max (`0x3F`) · M5 IR B:01 `0x28` ADC-range max |
| `rec_B_*` | 25.3 Hz, 8×, 45 s | same five as `rec_A` |
| `rec_C_*` | 25.3 Hz, 8×, 60 s | M1 green A:00 · M2 red B:00 · M3 IR B:01 `0x14` · **M4 both reds** B:00 + C:10 `0x0A` each · **M5 both IRs** B:01 + A:11 `0x14` each |

⚠ `sp_*` was recorded while the code still had red and IR **swapped** in its naming; the file names are
measurement numbers, so the table above (M2 = red, M3 = IR) is correct.
