# Chris's fleet, week 44 (24–30 Oct 2026): the audio bench

Written by GOLF (Chris's orchestrator agent) from the lanes' commits and session records and from John's reports in this
repo. Each figure was checked in the commit or report it comes from. Tags: **[measured]** means measured on the bench or
in flight data; **[repo]** means it is in a lane's code or records. Lane commits are cited as `repo@sha` (Chris's fleet
repos); John's reports are linked in this repo. No flights this week.

## At a glance

- **Two ADAU1860 boards on one clock**, the demo's one-codec-per-ear layout, were wired on DEV-A on 27 Oct. Board B then
  failed at its codec. A support case is open with Analog Devices; board A works.
- **ADAU1797 eval board:** no schematic is public, so it was mapped from 80 microscope photos. A new Raspberry Pi 4
  (dev-c) is set up to drive it over SPI, with test firmware and an 11-step bring-up guide. It is not powered up yet.
- **Headset node HS01** (XIAO ESP32-C6): two 16-LED rings run from a control page over the office Wi-Fi. The
  heart-rate sensor and IMU are next. A printed mount joins a donor headband to our ear cup.
- **John's bench, on our rig's board A:**
  - the 1860's loop delay is 94.5 µs at 48 kHz and 12.8 µs at 192 kHz;
  - DVNC can take 2.5–3.5 dBA off the pilot's ear over 20 Hz–1 kHz;
  - the cup's secondary path S(z) is measured on Mon 2 Nov.
- **Did not move this week:** the 1 Dec fixes (the DSMA STALL rule, AERO-A storage stalls, the GPS gate, a wired headset
  route), PureVoice, the relay bench, the server work.

## The two-1860 rig on DEV-A

- **Plan** (ALFA, 26 Oct; `aeronode@713289b`, `aeronode@7069d29`) [repo]:
  - 1860-A is the I²S clock master, and 1860-B follows A's BCLK and FSYNC through a small breadboard hub (B's PLL runs
    from BCLK), so both ears share one clock;
  - each board has 2 mics in and 1 DAC out, on header P2 only;
  - 20 wires, each data or DAC wire twisted with its own ground;
  - DEV-A captures 4 channels.
- **Why one clock** [measured]. On F03, DEV-A's and DEV-B's codecs ran 51.6 ppm apart (48,000.353 against
  47,997.877 Hz): about 90 ms of drift over a 30-minute recording. Codecs on their own crystals drift like this even
  inside one box, and one clock master removes it. Sources:
  [multi-codec rig clocking](../../reports/john/2026-10-26_multi_codec_rig_clocking.html) and
  [IMU-to-audio time base](../../reports/john/2026-10-26_imu_audio_timebase.html).
- **Board B failed** (Chris with ALFA, 27–28 Oct) [measured]:
  - **27 Oct:** the shared BCLK line sat at 0 V, traced to B's BCLK0 at 2 Ω to ground. B then answered no I²C address,
    even bare on USB, across replugs and reboots; its USB bridge still enumerated.
  - **28 Oct:** B's BCLK0 and FSYNC0 read open toward the codec. The header pins, the 0 Ω links R17/R18 and the test
    points all connect to each other, so the break is at the codec U15, at balls B2/C2. They are neighbours under a
    0.35 mm-pitch chip-scale package.
  - **Second host:** AERO-A, with another cable, gave the same result: the bridge is seen, and nothing answers I²C at
    100 or 10 kHz.
  - **Board A**, set up the same way, reads a healthy ~115 kΩ on every pin and works.
- **Status:** a support request has gone to Analog Devices (EVAL-ADAU1860EBZ, board AAQSO6P), and the two-board layout
  waits on a replacement. Before wiring the new board, check that BCLK0 and FSYNC0 read ~115 kΩ to ground.
- **Also this week:**
  - a driver from a donor headset played cleanly from the 1860 (26 Oct);
  - ECHO wrote a 21-question bench check manual for board B (`aerosearch@4029bf4`) [repo].

## The ADAU1797 eval board on a new Pi 4 (dev-c)

- **The board:** EVAL-ADAU1797ADZ Rev D #24, with its USBi.
- **Mapped from photos.** With no public schematic, ALFA mapped the board from Chris's 28 overview photos and 80
  microscope shots. A USB microscope camera on a Pi 4 now serves snapshots. The map (`aeronode@d7a5203`, `@3661f3f`,
  `@4addfd4`) [repo] names:
  - two ADAU1797A codecs, plus a third ADI codec (an ADAU1787) with its EEPROM;
  - a flash chip beside each 1797, likely its boot memory;
  - the FT4232H USB path;
  - nine level translators.
- **Control port:** John opened the 1797's control port on 26 Oct
  ([procedure](../../record/john/2026-10-26_adau1797a_dual_board_control_port_procedure.md)).
- **dev-c (Raspberry Pi 4):** Chris set it up on 28 Oct to replace the USBi (`aeronode@b7ca04c`) [repo]:
  - a build sheet with every wire numbered: 7 SPI control wires, 8 I²S wires and the mic supply;
  - an SPI library whose read commands change nothing and whose writes are read back;
  - a bring-up script in the data sheet's boot order.
- **The guide (30 Oct):** 11 steps from bench set-up through beep tests, voltage checks, chip IDs and bring-up, then the
  audio wires, two IM73 mics and a speaker. The 1797 runs from its own USB supply, on first and off last
  (`aeronode@1db63ee`) [repo]. The board and the Pi are still off and unwired.

## John's bench on our rig's board (JULIETT, 26–29 Oct)

- **Loop delay (M1)**, analog in to analog out, each rate with its own bypass control, on board AAQSR84 (our board A)
  [measured] ([report](../../reports/john/2026-10-28_adau1860_loop_delay_and_sample_rate.html)):
  - 94.50 µs at 48 kHz, 42.86 µs at 96 kHz and 12.81 µs at 192 kHz, so 192 kHz is 7.4× shorter than 48 kHz;
  - the codec inverts polarity at every rate; the bypass does not.
- **The 1860's ADC-to-DAC path** is unity gain and clean: +0.011 dB at 100 Hz, 0.012 % THD, from the 1 Oct recordings
  ([run sheet](../../john/bench/M0-M12_bench_runsheet.pdf)) [measured].
- **DVNC's ceiling at the pilot's ear** on F03 [measured]:
  - 2.5–3.5 dBA over 20 Hz–1 kHz on both aircraft, but only 0.5–2.0 dBA over 20–250 Hz;
  - about 85 % of the in-band energy is tonal on both the RV-8 and the Huey;
  - John proposes DVNC's band as 20 Hz–1 kHz, the same as ANC's
    ([report](../../reports/john/2026-10-29_dvnc_measured_ceiling_and_band.html),
    [record](../../record/john/2026-10-29_dvnc-band-is-20hz-to-1khz-same-as-anc.md)).
- **Next, Mon 2 Nov,** with the cup, driver, mics, miniDSP EARS, RME and an ADAM A7X in the office
  ([the secondary-path gap](../../reports/john/2026-10-29_the_secondary_path_gap.html)):
  - M8, the secondary path S(z): 1860 DAC → driver → feedback mic → ADC;
  - M12, the feed-forward timing margin.

  Every cancellation figure published so far assumes S(z) = 1.

## Headset node HS01 and the H1 mount (CHARLIE, 24–27 Oct)

- **HS01:** a Seeed XIAO ESP32-C6 that will carry three things, on USB 5 V, sending to AERO-A over MQTT:
  - the SparkFun heart-rate sensor (SEN-15219);
  - a Pololu AltIMU-10 v6;
  - two NeoPixel rings.

  The wiring report shows all 16 wires on one map (`aerosense@1c53dd1`) [repo].
- **Stage 1 runs (27 Oct):** both 16-LED rings are driven from a control page the board serves. It joined the office
  2.4 GHz Wi-Fi at −33 dBm, the page runs at 10 frames/s, and current is capped at 250 mA (`aerosense@5224d59`,
  `@8aa2ab5`) [measured].
- **Next, stage 2:** the heart-rate and IMU streams to AERO-A.
- **The mount:** it joins the metal tab of a donor headband to our ear cup. It was designed on 26 Oct and sent to the
  printer in PLA.

## Board tasks: the evidence for each

### Build the acoustic bench and measure baseline noise cancelling
Progressed. This week's bench work:
- the two-1860 rig on one clock, and board B's failure (see the two-1860 rig section above);
- the ADAU1797 board on the Pi 4;
- John's run sheet M0–M12 and the loop delay measured on our board;
- the secondary path S(z), set for Mon 2 Nov.

### Confirm ADAU1860-1 can drive H1 transducers
Progressed (Peter's card).
- A donor-headset driver plays cleanly from the 1860.
- The loop delay and the clean unity-gain path are measured.
- Not yet measured: the output into 150, 300 and 600 Ω loads at 1 kHz. M8 on Mon 2 Nov drives the cup's own driver from
  the 1860.

### WS3 Build the demo H1
Progressed: the headband-to-cup mount and the headset node HS01, stage 1 (LED rings). Passthrough audio has not started.

### Initial data model for the AeroNode synchronous timeline
Done (Chris, 30 Oct).
- **The working model:** every IMU, both Pixhawks and the DEV-A and DEV-B audio sit on one timeline, built from a sync
  pulse and start/stop/mark commands. It held one clock on all four F03 flights (2.4–4.7 µs rms).
- **John's analysis:** it places every sample on UTC and corrects the 3.891 s start offset and the 51.6 ppm clock
  difference between the boxes ([time base](../../reports/john/2026-10-26_imu_audio_timebase.html)).
- **The next rig** runs every codec on one clock master.
- Peter's written review can go on the card once his access is set up.

### Evaluate IMUs
Done in week 43 ([W43 summary](2026-W43_fleet_week_F03.md#evaluate-imus)): the ICM-45686 is the production DVNC
reference. John added the timing this week [measured]:
- vibration leads sound by at most ~1.4 ms on the one sensor pair with no clock uncertainty (both through one ADAU1372);
- DEV-B's ADXL354 has a constant 148 µs delay, so it can stand in as the reference the ICM-45686 lacks.

### Evaluate Microphones
Done in week 43 ([W43 summary](2026-W43_fleet_week_F03.md#evaluate-microphones)): the cabin mic is the analogue
IM68A130A. This week, DVNC's ceiling was measured at the pilot's ear: 2.5–3.5 dBA over 20 Hz–1 kHz. The next step needs a
mic inside the cup.

### No work this week
- WS4 PPG proof-out at the mastoid
- the X-Plane + ArduPilot SITL campaign
- AeroNode auto start/stop
- the relay headset-comms bench
- both AeroVault crash tests
- PureVoice
- Validate DSMA in real-world flight
- WS5 passive attenuation

The SPL meter and DSMA SAFE/CAUTION cards close on week 43's evidence:
[SPL meter](2026-W43_fleet_week_F03.md#source-a-calibrated-spl-meter),
[DSMA cues](2026-W43_fleet_week_F03.md#test-dsma-safecaution-cues-in-a-real-flight-and-log-the-cue-output).

## Also this week: company videos

- **VGSA investor demo film v7 to v10 (26 Oct):** cut from 5:43 to 3:45. It uses BRAVO's 3-D sound stage: Duran wears the
  H1 in a single-seat cockpit, and the orb sits where the cue sounds. Company only.
- **F03 social posts.** Chris cleared VGSA for public posts on 28 Oct, tagged EXPERIMENTAL.
  - 21 ideas, with 37 numbers checked at source;
  - HOTEL's first four reels: RV-8 vs Huey cabin sound, blade slap, through the Knysna Heads, hear your attitude;
  - DELTA's "Headphones on" binaural reel;
  - BRAVO's ten new 3-D plates.
- **RV-8 x-ray engine reel:** engine speed read from vibration alone (2,384 rpm), CHARLIE's drive train, BRAVO's 3-D
  engine built to Lycoming's documents, and ECHO's cabin sound bed.

## This week by agent

- **ALFA** (AeroNode boxes): the two-1860 wiring maps and build sheet; board B's fault with Chris; the 1797 board map,
  the Pi 4 build sheet, its firmware and the step-by-step guide.
- **BRAVO** (AeroSync, replay, 3-D): the VGSA 3-D sound stage and cockpit v4 to v10, ten social plates, the RV-8 engine
  bay in x-ray and the HUMS reel.
- **CHARLIE** (vibration, hardware): the headset node HS01 (wiring report, firmware stage 1), the headband mount, and the
  RV-8 drive train from vibration.
- **DELTA** (sim rig, VGSA lab): the "Headphones on" binaural reel.
- **ECHO** (DEV-A recorder, audio): the 1860-B bench check manual; sound beds for the engine reel and the cabin
  comparison.
- **FOXTROT** (DEV-B): idle since 14 Oct.
- **GOLF** (orchestration): the VGSA film v7 to v10, the social post ideas and their number checks, checks on every reel
  part, the dev-c handoff, and this page.
- **HOTEL** (video): four social reels (cabin sound, blade slap, the Heads, hear your attitude) and the RV-8 HUMS drafts.
- **INDIA** (company repo steward): fixed the agents' self-test so it no longer puts access checks on the report shelf.
- **Team lanes:**
  - **JULIETT** (John): the reports above;
  - **LIMA** (Peter): the u-blox DAN-F10N GPS model for Stefan's CAD;
  - **KILO** (Stefan): nothing new in this repo.

## Open items and next week

- **Chris's commitments for 2–6 Nov:**
  - the 1797 up on the Pi 4;
  - the S(z) bench with John on Monday;
  - headset node stage 2 (heart rate and IMU to AERO-A);
  - the AERO-A fixes for 1 Dec (storage stalls, the GPS gate).
- **Blocked:** board B's replacement (Analog Devices).
- **Not moved since 16 Oct:**
  - the DSMA STALL rule on the rig;
  - AERO-A storage stalls (its disk is 93 % full) and the 6X GPS;
  - a wired headset route;
  - the Huey radio getting into DEV-A's I2S;
  - DEV-B power.
