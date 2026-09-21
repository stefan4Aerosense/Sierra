// Web Bluetooth client for the MAX86171 board (OS61 firmware), and the measurement setup.
// Protocol ported from tools/max86171-hr-spo2/{OS61,PpgStream}.cs - keep the two in step.

import { MultiHeartRate, MultiPathOximeter } from './ppg.js';

const SERVICE = '6e400000-b5a3-f393-e0a9-e50e24dcca9e';
const CHAR_CONFIG = '6e400002-b5a3-f393-e0a9-e50e24dcca9e';
const CHAR_NOTIFY = '6e400001-b5a3-f393-e0a9-e50e24dcca9e';
// 00001530-... is the firmware-update service. Never touch it.

const DEV_NRF = 0, DEV_OPTICAL = 3;
const CURRENT_GREEN = 0x0A, CURRENT_RED = 0x0A, CURRENT_IR = 0x10;
const PD_PAIR_12 = 0x18, PD_PAIR_34 = 0x78, AVER_8 = 0x03;

const DETECTORS = ['detector 1', 'detector 2', 'detector 3', 'detector 4'];
// The two green measurements per detector are kept apart, never summed (measured 21 Sep: summing them
// destroyed the best ear channel, SNR 43 -> 7).
const GREEN_CHANNELS = [
  'detector 1 (LED group)', 'detector 1 (single LED)', 'detector 2 (LED group)', 'detector 2 (single LED)',
  'detector 3 (LED group)', 'detector 3 (single LED)', 'detector 4 (LED group)', 'detector 4 (single LED)'];

const sleep = ms => new Promise(r => setTimeout(r, ms));

export class Board {
  constructor(log) {
    this.log = log || (() => {});
    this.samples = 0; this.frames = 0; this.dropped = 0;
    this.lastCounter = -1;
    this.frame = [];
    this.pending = null; this.pendingSeen = null;
    this.resetPending();
    this.lastGreen = [0, 0, 0, 0]; this.lastRed = [0, 0, 0, 0]; this.lastIr = [0, 0, 0, 0];
    this.onSample = null;
  }

  // Must be called from a user gesture (a tap), which is how the browser asks permission.
  async connect() {
    if (!navigator.bluetooth) throw new Error('This browser has no Bluetooth support. Use Chrome on Android.');
    this.log('Choose "OS61 Demo" in the list...');
    this.device = await navigator.bluetooth.requestDevice({
      filters: [{ namePrefix: 'OS61' }],
      optionalServices: [SERVICE]
    });
    this.device.addEventListener('gattserverdisconnected', () => this.log('Board disconnected.'));
    this.log('Connecting to ' + (this.device.name || 'board') + '...');
    const server = await this.device.gatt.connect();
    const service = await server.getPrimaryService(SERVICE);
    this.cfg = await service.getCharacteristic(CHAR_CONFIG);
    this.noti = await service.getCharacteristic(CHAR_NOTIFY);
  }

  async write(...bytes) { await this.cfg.writeValueWithResponse(new Uint8Array(bytes)); }

  async writeRead(...bytes) {
    await this.write(...bytes);
    const v = await this.cfg.readValue();
    return new Uint8Array(v.buffer);
  }

  // The value of a single register sits in byte 4 of the reply, not byte 1.
  async readReg(reg) {
    const r = await this.writeRead(DEV_OPTICAL, 0, reg, 1);
    if (r.length < 5 || r[0] !== 4) throw new Error('Bad register read: ' + [...r].join(','));
    return r[4];
  }

  async writeBits(reg, msb, lsb, value) {
    if (reg >= 0xD0) throw new Error('Refusing to write OTP/ID registers');
    await this.write(DEV_OPTICAL, 1, reg, msb, lsb, value);
  }

  async enableSensors(on) { await this.write(DEV_NRF, 1, 0, on ? 1 : 0); }
  async useAccel(on) { await this.write(DEV_NRF, 5, 0, on ? 1 : 0, 0); }

  async configureMeas(m, selects, pdSelect, drvA, drvB, drvC) {
    const bs = 0x18 + 8 * (m - 1);
    await this.writeBits(bs, 7, 0, selects);
    await this.writeBits(bs + 1, 7, 0, pdSelect | AVER_8);
    await this.writeBits(bs + 2, 7, 0, 0x3A); // filter/LED/ADC ranges, as the vendor GUI leaves them
    await this.writeBits(bs + 3, 7, 0, 0x50); // settling times
    await this.writeBits(bs + 4, 7, 0, drvA);
    await this.writeBits(bs + 5, 7, 0, drvB);
    await this.writeBits(bs + 6, 7, 0, drvC);
  }

  async start() {
    await this.enableSensors(false);
    await sleep(300);
    try { await this.useAccel(false); } catch (e) { this.log('Note: could not turn the accelerometer off.'); }

    // Everything off before reconfiguring: changing settings while measurements run can leave the old
    // configuration in place, which looks like a dead sensor.
    await this.writeBits(0x0D, 7, 0, 0x00);
    await this.writeBits(0x0C, 7, 7, 0);

    const part = await this.readReg(0xFF);
    if (part !== 0x2C) this.log('Warning: unexpected part ID 0x' + part.toString(16));

    // 25 Hz frame rate (32000 / 1280).
    await this.writeBits(0x15, 5, 5, 0);
    await this.writeBits(0x16, 7, 0, 0x05);
    await this.writeBits(0x17, 7, 0, 0x00);

    for (let pair = 0; pair < 2; pair++) {
      const pds = pair === 0 ? PD_PAIR_12 : PD_PAIR_34;
      const m0 = 1 + 4 * pair;
      await this.configureMeas(m0,     0x38, pds, CURRENT_GREEN, CURRENT_GREEN, CURRENT_GREEN);
      await this.configureMeas(m0 + 1, 0x0C, pds, 0, CURRENT_GREEN, 0);
      await this.configureMeas(m0 + 2, 0x20, pds, 0, CURRENT_RED, CURRENT_RED);
      await this.configureMeas(m0 + 3, 0x07, pds, CURRENT_IR, CURRENT_IR, 0);
    }

    await this.writeBits(0x0C, 3, 2, 0);
    await this.writeBits(0x0C, 1, 1, 0);
    await this.writeBits(0x0D, 7, 0, 0xFF);

    const sys1 = await this.readReg(0x0C), sys2 = await this.readReg(0x0D), st2 = await this.readReg(0x01);
    if ((sys1 & 0x8E) !== 0 || sys2 !== 0xFF)
      this.log('Warning: settings did not stick (0x0C=0x' + sys1.toString(16) + ', 0x0D=0x' + sys2.toString(16) + '). Stop, wait 10 s, start again.');
    if ((st2 & 0x04) !== 0) this.log('Warning: the sensor reports an invalid configuration.');

    this.fs = await this.readFrameRate();
    this.estimator = new MultiHeartRate(this.fs, GREEN_CHANNELS);
    this.oximeter = new MultiPathOximeter(this.fs, DETECTORS);

    await this.writeBits(0x0A, 4, 4, 1); // flush the FIFO
    this.noti.addEventListener('characteristicvaluechanged', e => this.onPacketValue(e.target.value));
    await this.noti.startNotifications();
    await this.enableSensors(true);
    this.log('Streaming. Rest the sensor lightly against the skin and keep still.');
  }

  async readFrameRate() {
    const sel = await this.readReg(0x15), hi = await this.readReg(0x16), lo = await this.readReg(0x17);
    const clk = (sel & 0x20) !== 0 ? 32768.0 : 32000.0;
    const div = ((hi & 0x7F) << 8) | lo;
    let fs = div > 0 ? clk / div : 0;
    if (fs < 5 || fs > 4096) { this.log('Frame-rate registers look odd; assuming 25 Hz.'); fs = 25; }
    return fs;
  }

  async stop() {
    try { await this.enableSensors(false); } catch (e) {}
    try { await this.noti.stopNotifications(); } catch (e) {}
    try { this.device.gatt.disconnect(); } catch (e) {}
  }

  onPacketValue(dataView) {
    const b = new Uint8Array(dataView.buffer);
    for (let i = 0; i + 20 <= b.length; i += 20) this.onPacket(b.subarray(i, i + 20));
  }

  onPacket(p) {
    if (this.lastCounter >= 0 && p[0] !== ((this.lastCounter + 1) & 0xFF)) {
      this.dropped += (p[0] - this.lastCounter - 1) & 0xFF;
    }
    this.lastCounter = p[0];
    const type = p[1];
    if (type === 0 && this.frame.length > 0) { this.processFrame(this.frame); this.frame = []; }
    if (type === 0 || type === 1 || type === 2 || type === 10) {
      for (let i = 2; i < 20; i++) this.frame.push(p[i]);
    }
  }

  resetPending() {
    this.pending = Array.from({ length: 9 }, () => [0, 0]);
    this.pendingSeen = new Array(9).fill(0);
  }

  // Each 3-byte value: top 4 bits = measurement tag (1-8), low 20 bits = signed reading. Samples are
  // reassembled across frames because the FIFO can start mid-sample.
  processFrame(b) {
    this.frames++;
    for (let i = 0; i + 3 <= b.length; i += 3) {
      const tag = b[i] >> 4;
      let x = ((b[i] << 16) | (b[i + 1] << 8) | b[i + 2]) & 0xFFFFF;
      if (x & 0x80000) x -= 0x100000;
      if (tag < 1 || tag > 8) continue;
      if (tag === 1 && this.pendingSeen[1] === 2) this.resetPending();
      const occ = this.pendingSeen[tag]++;
      if (occ > 1) { this.resetPending(); continue; }
      this.pending[tag][occ] = x;
      if (tag === 8 && occ === 1) {
        if (this.pendingSeen.slice(1).every(c => c === 2)) this.emitSample(this.pending);
        this.resetPending();
      }
    }
  }

  emitSample(v) {
    this.samples++;
    for (let pd = 0; pd < 4; pd++) {
      const m0 = pd < 2 ? 1 : 5, occ = pd % 2;
      const greenGroup = v[m0][occ], greenSingle = v[m0 + 1][occ];
      const red = v[m0 + 2][occ], ir = v[m0 + 3][occ];
      this.lastGreen[pd] = greenGroup + greenSingle; this.lastRed[pd] = red; this.lastIr[pd] = ir;
      this.estimator.add(2 * pd, greenGroup, Math.abs(greenGroup) >= 523287);
      this.estimator.add(2 * pd + 1, greenSingle, Math.abs(greenSingle) >= 523287);
      this.oximeter.add(pd, red, ir);
    }
    if (this.onSample) this.onSample();
  }
}
