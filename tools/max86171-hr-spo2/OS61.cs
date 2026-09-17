using System; using System.Linq; using System.Threading; using Windows.Foundation;
using Windows.Devices.Bluetooth; using Windows.Devices.Bluetooth.GenericAttributeProfile; using Windows.Storage.Streams;
// Minimal client for the Maxim OS61 (MAX86171) BLE protocol, as used by the MAXREFDES280 GUI.
public class OS61 : IDisposable {
  public const byte DevNrf = 0, DevOptical = 3;
  BluetoothLEDevice dev; GattCharacteristic cfg, noti; readonly object ioLock = new object();
  public event Action<byte[]> Packet; // 20-byte notify packets
  static T W<T>(IAsyncOperation<T> op){ var t0=DateTime.Now; while(op.Status==AsyncStatus.Started){ if((DateTime.Now-t0).TotalSeconds>15) throw new TimeoutException("BLE operation timed out"); Thread.Sleep(5);} if(op.Status!=AsyncStatus.Completed) throw new Exception("BLE operation "+op.Status+" (0x"+op.ErrorCode.HResult.ToString("X8")+")"); return op.GetResults(); }
  public OS61(ulong address){
    dev=W(BluetoothLEDevice.FromBluetoothAddressAsync(address)); if(dev==null) throw new Exception("Board not found. Is it switched on and not connected to the Maxim GUI?");
    var sr=W(dev.GetGattServicesForUuidAsync(new Guid("6e400000-b5a3-f393-e0a9-e50e24dcca9e"), BluetoothCacheMode.Uncached));
    if(sr.Status!=GattCommunicationStatus.Success||sr.Services.Count==0) throw new Exception("Could not open the sensor service ("+sr.Status+"). Close the Maxim GUI and try again.");
    var s=sr.Services[0];
    var cr=W(s.GetCharacteristicsForUuidAsync(new Guid("6e400002-b5a3-f393-e0a9-e50e24dcca9e"), BluetoothCacheMode.Uncached));
    var nr=W(s.GetCharacteristicsForUuidAsync(new Guid("6e400001-b5a3-f393-e0a9-e50e24dcca9e"), BluetoothCacheMode.Uncached));
    if(cr.Characteristics.Count==0||nr.Characteristics.Count==0) throw new Exception("The board is busy (probably still connected to the Maxim GUI). Close the Maxim GUI and try again.");
    cfg=cr.Characteristics[0]; noti=nr.Characteristics[0];
    noti.ValueChanged+=(c,e)=>{ var b=Bytes(e.CharacteristicValue); var h=Packet; if(h==null) return; for(int i=0;i+20<=b.Length;i+=20){ var p=new byte[20]; Array.Copy(b,i,p,0,20); h(p);} };
  }
  public string Name { get { return dev.Name; } }
  static byte[] Bytes(IBuffer buf){ var b=new byte[buf.Length]; DataReader.FromBuffer(buf).ReadBytes(b); return b; }
  public void Write(params byte[] data){
    lock(ioLock){ var w=new DataWriter(); w.WriteBytes(data); var st=W(cfg.WriteValueAsync(w.DetachBuffer(), GattWriteOption.WriteWithResponse)); if(st!=GattCommunicationStatus.Success) throw new Exception("Write failed: "+st); }
  }
  public byte[] WriteRead(params byte[] data){
    lock(ioLock){ Write(data); var r=W(cfg.ReadValueAsync(BluetoothCacheMode.Uncached)); if(r.Status!=GattCommunicationStatus.Success) throw new Exception("Read failed: "+r.Status); return Bytes(r.Value); }
  }
  // TargetDevice.ReadCommand: [dev, cmd, 1] -> response[0] == message id
  public byte[] ReadCommand(byte devId, byte cmd, byte expectMsg){ var r=WriteRead(devId,cmd,1); if(r.Length==0||r[0]!=expectMsg) throw new Exception("Unexpected response "+BitConverter.ToString(r)); return r; }
  // TargetDevice.WriteCommand: [dev, cmd, 0, data...]
  public void WriteCommand(byte devId, byte cmd, params byte[] data){ Write(new byte[]{devId,cmd,0}.Concat(data).ToArray()); }
  // Sensor.ReadRegister: [dev, 0, reg, n] -> [4, b1..b4]; a single-byte value is right-aligned in b4
  public byte ReadReg(byte reg){ var r=WriteRead(DevOptical,0,reg,1); if(r.Length<5||r[0]!=4) throw new Exception("Bad register read "+BitConverter.ToString(r)); return r[4]; }
  // Sensor.ReadModifyWrite: [dev, 1, reg, msb, lsb, value]
  public void WriteBits(byte reg, int msb, int lsb, int value){
    if(reg>=0xD0) throw new InvalidOperationException("Refusing to write OTP/ID registers");
    Write(DevOptical,1,reg,(byte)msb,(byte)lsb,(byte)value);
  }
  public void EnableNotifications(bool on){ var st=W(noti.WriteClientCharacteristicConfigurationDescriptorAsync(on?GattClientCharacteristicConfigurationDescriptorValue.Notify:GattClientCharacteristicConfigurationDescriptorValue.None)); if(st!=GattCommunicationStatus.Success) throw new Exception("Notify setup failed: "+st); }
  public void EnableSensors(bool on){ WriteCommand(DevNrf,1,(byte)(on?1:0)); }
  public bool ReadUseAccel(){ return ReadCommand(DevNrf,5,8)[1]==1; }
  public void UseAccel(bool on){ WriteCommand(DevNrf,5,(byte)(on?1:0),0); }
  public void Dispose(){ try{ if(noti!=null) EnableNotifications(false);}catch{} if(dev!=null) dev.Dispose(); }
}
