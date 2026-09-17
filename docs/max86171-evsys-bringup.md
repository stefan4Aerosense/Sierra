# MAX86171EVSYS# bring-up — heart rate and SpO₂ from a PC, with no vendor HR output

**Author:** KILO (lane `stefan`), with Stefan Kruger at the bench · **Date:** 2026-09-17
**Tool:** [`tools/max86171-hr-spo2/`](../tools/max86171-hr-spo2/README.md) · **Raw data:** `tools/max86171-hr-spo2/recordings/`

Provenance tags: `[measured]` = I ran it today · `[fetched]` = read at a cited source · `[repo]` = read in a
repository, credited · `[assumed]` = inference, flagged. Everything measured was measured by me (KILO) driving
the board from Stefan's PC, **on Stefan's fingertip**.

---

## 0 · The one-paragraph answer

The kit is Analog Devices' **MAX86171EVSYS#** (MAXSensorBLE# data board + MAX86171_OSB# sensor board). It
talks to a PC **only over Bluetooth LE** — its USB-C port is power plus a firmware-update drive. The vendor
GUI streams raw PPG but **never shows a heart rate**, so I reverse-engineered the GUI's BLE protocol from its
.NET assemblies and wrote a small Windows tool that configures the MAX86171 register-by-register over that
link. On a **fingertip** it gives a heart rate that agrees with the spectrum of the raw signal (73–75 BPM vs a
1.2 Hz = 72 BPM fundamental) and an **uncalibrated** SpO₂ of **~98 %** that is stable (R = 0.514 ± 0.012 over
60 s) — but only on one of the board's two red/IR paths, the other being useless on the same finger at the
same moment. **Nothing here says anything about an ear cup**, and there is no reference instrument behind
either number.

---

## 1 · Hardware

| Item | Value | Tag |
|---|---|---|
| Kit | **MAX86171EVSYS#** — "Evaluation System for the MAX86171/MAX86170A/MAX86170B". Obsolete; ADI's replacement is **MAXREFDES280#** (same two boards) | `[fetched]` ADI product page |
| Boards | **MAXSensorBLE#** (data acquisition, BLE, battery/USB-C) + **MAX86171_OSB#** (sensor daughter board, MAX86171ENI+ 28-bump WLP) | `[fetched]` ADI page; kit datasheet Rev 4 |
| Sensor-board optics | 2 × OSRAM **SFH 7013** (green 530 / red 655 / IR 940 nm, common anode) + 2 × OSRAM **CT DBLP31.12** (green 536 nm) + 4 photodiodes PD1–PD4 | `[fetched]` kit schematic p.25; OSRAM datasheets |
| SFH 7013 pins | **1 = green cathode · 2 = IR cathode · 3 = common anode · 4 = red cathode** | `[fetched]` SFH 7013 datasheet v1.2 p.9 |
| Chip-to-LED nets | D1 (SFH 7013): pin1→LED8, pin2→LED7, pin4→LED6 · D2 (SFH 7013): pin1→LED9, pin2→LED3, pin4→LED2 · D3 → LED1 · D4 → LED4 **and** LED5 tied | `[fetched]` kit schematic p.25 |
| MAX86171 ↔ board MCU | SPI nets (`SCK_SCL`, `SPI0_MISO` via R9 → SDO, `MOSI_SDA`, `SPI0_CSOPT_N` via R8A → CSB/ADDR); an `I2C_SEL` resistor option table exists. **Which option is populated, and the bus speed, were not verified** — they live inside the vendor firmware | `[fetched]` schematic p.25 / **not measured** |
| Wiring | **None.** The two boards mate on a connector; the host link is BLE. There is nothing to wire. | `[measured]` |
| Supply rails at the part | **NOT MEASURED.** No meter was used. I cannot give VDD, VLED or the LED drive voltage. | — |
| Firmware | BLE name `OS61 Demo`; GATT manufacturer string `Maxim Integrated`; firmware-version reply raw bytes `00-04-00-07-E5-09-15-00-01-00-00-01-00…` (not decoded) | `[measured]` |

### Host
Windows 11 Pro 10.0.26200 laptop, Realtek USB Bluetooth adapter, .NET Framework 4.x `csc.exe` + WinRT
`Windows.Devices.Bluetooth`. **No Linux, no kernel involved.** `[measured]`

### Driver check
Mainline Linux `drivers/iio/health` contains `afe4403`, `afe4404`, `max30100`, `max30102` — **no MAX86171**
`[fetched]` github.com/torvalds/linux. Not needed for this route anyway: the MAX86171 is driven by the kit's
own BLE firmware; the PC only sends it register commands.

---

## 2 · The route: vendor GUI → protocol → own tool

1. **The vendor GUI** is "MAXREFDES280# Reference Design Software 1.3.0" (ADI `SFW0018960A`, needs a myAnalog
   login). It installs to `C:\Program Files (x86)\Maxim Integrated\MAXREFDES280 PPG Reference Design\`; its
   exe namespace is `MAX86171EVKitGui`. It streams raw PPG and shows PSNR / AC-DC — **it has no heart-rate
   display** (its only algorithm is `PSnrACDC.dll`) `[measured]`.
2. **The protocol** was read out of `BleWristband.dll` and `MAXREFDES280Gui.exe` with a reflection-based IL
   disassembler (`bench/ild.cs`) — types under `Maxim.Sensor.BleWristband.Devices.OS61` `[repo]` (vendor binaries).
   The register map ships in the install as `RegistersData/max86171_reg.csv` `[repo]`.

### BLE protocol `[repo]` from the GUI assemblies, every element `[measured]` working

| Thing | Value |
|---|---|
| Service | `6e400000-b5a3-f393-e0a9-e50e24dcca9e` |
| Command/response characteristic (write, read) | `6e400002-…` |
| Data characteristic (notify) | `6e400001-…` |
| **Do not touch** | service `00001530-1212-efde-1523-785feabcd123` — Nordic legacy DFU (firmware update) |
| Target byte | `0` = NRF (board), `3` = MAX86171 (optical) |
| Write command | `[target, cmd, 0x00, data…]` |
| Read command | write `[target, cmd, 0x01]`, then read the characteristic → `[message-id, data…]` |
| Read register | write `[3, 0x00, reg, 1]`, read → `[0x04, b1, b2, b3, **value**]` — **value is byte 4** |
| Write register bits | `[3, 0x01, reg, msb, lsb, value]` (read-modify-write of bits msb..lsb) |
| Sensors on/off | `[0, 0x01, 0x00, 1/0]` |
| Accelerometer on/off | `[0, 0x05, 0x00, enable, freeRun]` |
| Notification | one or more 20-byte packets: `[counter, type, 18 payload bytes]`. Types `0,1,2,10` = sensor data (type 0 starts a frame), `3` = periodic status |
| Sample value | 3 bytes: top 4 bits = measurement tag (1–9 = MEAS1–9, 0 = padding), low 20 bits = signed ADC count |

With both PPG channels enabled, each measurement appears **twice per sample** (PPG1 value, then PPG2 value),
measurements in order MEAS1 … MEASn `[measured]`.

---

## 3 · Register sequence, power-up to first valid sample

All writes use the read-modify-write command above. Register names from the vendor CSV `[repo]`.

### 3a · Minimal: one green LED, heart rate — `[measured]` 73–75 BPM live

State found after the vendor GUI had run: MEAS1 selects `0x18=0x00`, config `0x19=0x18 0x1A=0x3A 0x1B=0x50`,
LEDA current `0x1C=0x0A`, frame-rate divider `0x16/0x17=0x01/0x40` (32000/320 = **100 Hz**), and the chip in
**shutdown** (`0x0C=0x02`) `[measured]`.

```
sensors off                         [0,1,0,0]
accelerometer off                   [0,5,0,0,0]
0x0C bit7 = 0        MEAS9 off
0x0C bit3 = 1        PPG2 powered down
0x0C bit2 = 0        PPG1 on
0x0C bit1 = 0        SHDN cleared   ← the chip is in shutdown otherwise
0x0D      = 0x01     MEAS1 only
0x19 bit5 = 0        MEAS1 PPG1 reads PD1
0x0A bit4 = 1        flush FIFO
enable notifications on 6e400001
sensors on                          [0,1,0,1]
```
Result: 100.6 samples/s measured, 0 dropped packets `[measured]`.

### 3b · Full: every LED, all four photodiodes — what the tool now runs

```
sensors off; accelerometer off
0x0D = 0x00 ; 0x0C bit7 = 0          disable ALL measurements BEFORE reconfiguring (see trap 7)
0x15 bit5 = 0 ; 0x16 = 0x05 ; 0x17 = 0x00      32000/1280 = 25 Hz
for PD pair in (PD1/PD2 → cfg1 0x18, PD3/PD4 → cfg1 0x78):     cfg1 |= 0x03 (8× averaging)
  MEAS n+1  selects 0x38  currents A=B=C=0x0A   green: A:00 (D3) + B:10 (D4) + C:11 (package-1 green)
  MEAS n+2  selects 0x0C  current  B=0x0A       green: B:11 (package-2 green)
  MEAS n+3  selects 0x20  currents B=C=0x0A     red:   B:00 + C:10 (both packages)
  MEAS n+4  selects 0x07  currents A=B=0x10     IR:    B:01 + A:11 (both packages)
  each MEAS: cfg2 = 0x3A, cfg3 = 0x50
0x0C bits3:2 = 0 ; bit1 = 0          both PPG channels on, SHDN cleared
0x0D = 0xFF                          MEAS1–8
0x0A bit4 = 1 ; notifications on ; sensors on
```
Verified: no `INVALID_CFG` (status 2 bit 2), 25.3 samples/s, 0 dropped, across four consecutive starts
`[measured]` — **with no finger on the sensor**. The four-photodiode configuration has **not** been run
with a finger. The two-path configuration it extends has (`rec_C`, §5).

### LED driver-select map for this board `[measured]` + schematic

Each measurement has three current drivers A/B/C; `MEASx_DRVx[1:0]` picks which LED pin a driver reaches.
Measured by lighting one driver/select at a time and fingerprinting the light at PD1 vs PD2:

| Select | Light PD1 / PD2 (code 0x0A) | LED |
|---|---|---|
| A:00 = C:00 | 125k / 80k | single green (between the photodiodes) |
| A:10 = B:10 | 128k / 84k | the other single green |
| C:11 | 68k / ~0 | green, SFH 7013 package near PD1 |
| B:11 | ~0 / 27k | green, SFH 7013 package near PD2 |
| B:00 = A:01 | 322k / 6k | **red**, package near PD1 |
| C:10 | 6k / 210k | **red**, package near PD2 |
| B:01 = C:01 | 195k / 3.5k | **IR**, package near PD1 |
| A:11 | 3.7k / 149k | **IR**, package near PD2 |

Pairs with identical readings (A:00 = C:00 etc.) are one physical LED reachable from two drivers. **Which
package is D1 and which is D2 was not established** — ADI's MAX86171 datasheet Table 1 (the mux table) was
unreachable (analog.com timed out repeatedly). `[assumed]`-free statement: colours per select code are
measured; package designators are not.

---

## 4 · Defaults and behaviour that look like faults

1. **USB-C shows only an empty mass-storage drive** (`MAXIM MSD X`, USB VID 0703 PID 0104, no COM/HID).
   Looks like a board with no firmware. It is normal — the data path is BLE only. `[measured]`
2. **The vendor GUI crashes on Start** — *"Unable to load DLL 'PSnrACDC.dll' (0x8007007E)"*. The DLL is
   present; its dependency `MSVCR120.dll` is the **32-bit Visual C++ 2013** runtime, absent even with the
   2015–2022 runtimes installed. Fix: `winget install Microsoft.VCRedist.2013.x86`. `[measured]`
3. **The GUI shows no heart rate.** Not a fault — it has no HR algorithm. `[measured]`
4. **The chip is left in shutdown** (`0x0C = 0x02`). Clear SHDN or you get nothing. `[measured]`
5. **Register reads return all zeros** if you take byte 1 of the reply. The value is **byte 4**. `[measured]`
6. **No finger = every sample `0xC7FFFF`** (tag 12, positive full scale) with status 1 showing exposure
   overflow and status 2 `INVALID_CFG` **clear**. Looks like a broken configuration; it is ambient light
   saturating the photodiodes. Put a finger on it. `[measured]`
7. **Reconfiguring while other measurements are still enabled** (e.g. straight after the GUI or another
   configuration) left the stream delivering **1 sample in 10 s**. Writing `0x0D = 0x00` first fixed it,
   reproduced from the bad state. `[measured]`
8. **FIFO frames can start mid-sample** — tags arrive `6,6,7,7,8,8,1,1,…,5,5` and stay shifted, and in one
   run shifted again mid-stream. A parser that assumes one frame = one sample silently gets **nothing**.
   Assemble samples by tag across frames. `[measured]` (seen on 2 of 4 starts)
9. **Two select codes light the same LED** (table in §3). Looks like duplicate LEDs; it is the driver mux.
10. **"Only two LEDs are on"** — the IR LEDs are invisible. You see green and red. `[measured]` (Stefan's photo)
11. **Brightness does not tell you the colour.** My first assignment (brighter = IR) was **wrong**; see §7.
12. **Host-side:** WinRT Bluetooth async calls **hang forever on an STA thread** without a message pump
    (a WinForms `[STAThread]` Main). Run BLE work on an MTA thread. `[measured]`
13. **Only one BLE connection.** With the GUI connected, characteristic discovery returns an empty list. `[measured]`

---

## 5 · Results

**Tissue and site, first because it matters most: a fingertip, Stefan's, resting on the sensor window with the
board lying on a desk, at room temperature, seated.** Placement was per my spoken instructions; I could not see
it. **No wrist, no ear, no ear cup, no hair, no beard, no motion.**

### Heart rate `[measured]`
- **100 Hz, one green LED, PD1** (`raw1.txt`, 22 s): tool reads **73–75 BPM**, autocorrelation quality 0.89–0.92.
  Independent check on the same samples — spectrum fundamental **1.2 Hz (72 BPM)**, 2nd harmonic 2.4 Hz,
  respiration ~0.4 Hz. That is **self-consistency, not accuracy**: no ECG, chest strap or clinical reference.
- **25 Hz, three-LED configuration, live 50 s:** 66–76 BPM, quality 0.68–0.96.
- Self-test on synthetic PPG: 45–180 BPM recovered within ~1 BPM at 25/50/100 Hz; noise alone gives no reading.

### Signal quality by LED `[measured]`, session B (25.3 Hz, 8× averaging, 45 s, simultaneous)

| Channel | DC (counts) | Pulse / DC | SNR at pulse |
|---|---|---|---|
| green, PD1 | 122k | 2.25 % | 59 |
| green, PD2 | 81k | 1.80 % | 50 |
| red (B:00), PD1 | 227k | 0.43 % | 45 |
| IR (B:01), PD1 | 318k | 0.77 % | 44 |

Session A (10 Hz, **64×** averaging) and the double-current / double-range variants gave **no SNR
improvement** (SNR 35–44) `[measured]`. **Finger placement mattered far more than any setting:** earlier
sessions on the same day had red/IR pulse/DC of ~0.1 % and SNR ~3–6. `[measured]`

### SpO₂ `[measured]` — uncalibrated
Ratio of ratios, amplitudes by lock-in at the pulse frequency, curve `SpO₂ = −45.060R² + 30.354R + 94.845`
(the Maxim reference-design curve for a *different* sensor — **not fitted to this board**).

| Recording | Path | R (mean ± sd over 16 s windows) | SpO₂ |
|---|---|---|---|
| B, 45 s | red B:00 / IR B:01, PD1 | 0.55 (sd 0.02–0.03) | ~98 % |
| C, 60 s | single red/IR (B:00/B:01) → PD1 | **1.18 ± 1.04** (range 0.29–4.6), SNR 3–6 | **70–100 %, unusable** |
| C, 60 s | both-LED measurement → PD1 | 1.02 ± 0.86 (range 0.22–3.96) | 70–100 %, unusable |
| C, 60 s | both-LED measurement → PD2 | **0.514 ± 0.012** | **98.5 ± 0.2 %** |
| C, 60 s | both paths averaged | 0.77 ± 0.43 | 70–100 % (the bad path spoils it) |

Recording C was one finger, one minute, both paths sampled simultaneously. **The two red/IR paths on this board
can differ by an order of magnitude in quality at the same instant**, so the tool measures every path
separately and uses the cleanest; averaging is worse than choosing. Replaying C through the tool: 98–99 % ± 0
from 21 s onward, via the PD2 path.

---

## 6 · What this does NOT establish

- **Anything about ear-cup PPG.** Finger only. The company's open risk — PPG in an ear cup through hair, beard
  and a cup seal — is untouched by this. A clean fingertip is the easiest optical case there is.
- **Accuracy.** No ECG, chest strap or reference oximeter was used. HR agrees with the signal's own spectrum;
  that is consistency, not correctness. SpO₂ is on an uncalibrated generic curve; 98 % is *plausible*, not
  *measured against truth*.
- **Generality.** One subject, one day, seated, still, room temperature. No skin-tone, perfusion, temperature
  or motion variation.
- **The MAX86171 vs the MAX30101.** No MAX30101 was tested. There is no comparison here, so there is no
  evidence for or against replacing the ratified part.
- **Bare-chip integration.** Everything went through the kit's own BLE firmware. SPI/I²C timing to a cup MCU,
  bus speed, FIFO interrupt handling and power were not exercised.
- **Rails and power.** Not measured.
- **The four-photodiode configuration with a finger.** Data flow verified; physiological readings not yet.
- **Which SFH 7013 package is D1 vs D2.** Colours per select code measured; designators not.

---

## 7 · Mistakes made on the way (kept, corrected)

1. **Red and IR were swapped** in my first SpO₂ version: I called the brighter LED infrared. With a weak signal
   R came out ~0.77 and "SpO₂ 81–93 %" — numbers that meant nothing. A clean session gave R = 1.80, which is
   physiologically impossible for a person at a desk; inverting it gave 0.55 (~98 %), confirmed later by the
   OSRAM pinout plus schematic. **Brightness is not a colour test.**
2. **Register reads decoded from the wrong byte** → every register "was zero". The build carrying that bug
   was written to overwrite MEAS1's LED current whenever all three currents read zero, so any run of it
   would have replaced the real `0x0A` with `0x30`; whether Stefan's one run of that build did so was not
   checked.
3. **"Nothing works" three times was the finger, not the code** — twice I started investigating configuration
   before checking the obvious. Now: check DC level first; ~0 counts or `0xC7FFFF` = no finger / ambient.
4. **Frame-aligned parsing** worked at one-, three- and five-measurement configurations and silently died at
   eight (trap 8). A parser that works is not a parser that is right.

---

## Sources
- ADI MAX86171EVSYS product page and datasheet Rev 4 (`MAX86171EVSYS (1).pdf`, schematic pp. 21–25)
- OSRAM SFH 7013 datasheet v1.2 2020-07-31 (docs.rs-online.com/43db/A700000007960604.pdf)
- OSRAM CT DBLP31.12 product page (ams-osram.com)
- github.com/torvalds/linux/tree/master/drivers/iio/health
- MAXREFDES280 GUI 1.3.0 install: `BleWristband.dll`, `MAXREFDES280Gui.exe`, `RegistersData/max86171_reg.csv`
