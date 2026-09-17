using System; using System.Collections.Generic; using System.Linq; using System.Threading;
class Leds {
  static void Main(string[] a){ var t=new Thread(Run); t.SetApartmentState(ApartmentState.MTA); t.Start(); t.Join(); }
  static void Run(){
    ulong addr=PpgStreamFind(); var b=new OS61(addr);
    try{
    b.EnableSensors(false); Thread.Sleep(300); b.UseAccel(false);
    byte c1=b.ReadReg(0x19), c2=b.ReadReg(0x1A), c3=b.ReadReg(0x1B);
    Console.WriteLine("MEAS1 cfg: 0x{0:X2} 0x{1:X2} 0x{2:X2}", c1,c2,c3);
    int nMeas=int.Parse(Environment.GetCommandLineArgs()[1]); byte I=Convert.ToByte(Environment.GetCommandLineArgs()[2],16); int runSec=int.Parse(Environment.GetCommandLineArgs()[3]);
    // MEAS n (1..6): driver A/B/C cycling, pin select 0 then 1 -> LED1..LED6 (per MAX86171 driver muxing)
    for(int m=1;m<=6;m++){
      byte bs=(byte)(0x18+8*(m-1)); int drv=(m-1)%3; int sel=(m-1)/3;
      byte selects=(byte)(sel<<(2*drv));
      b.WriteBits(bs,7,0,selects);
      b.WriteBits((byte)(bs+1),7,0,(byte)(c1&0xDF)); b.WriteBits((byte)(bs+2),7,0,c2); b.WriteBits((byte)(bs+3),7,0,c3);
      for(int d=0;d<3;d++) b.WriteBits((byte)(bs+4+d),7,0,(byte)(d==drv?I:0));
    }
    b.WriteBits(0x0C,7,7,0); b.WriteBits(0x0C,3,3,1); b.WriteBits(0x0C,2,2,0); b.WriteBits(0x0C,1,1,0);
    b.WriteBits(0x16,7,0,0x05); b.WriteBits(0x17,7,0,0x00);
    b.WriteBits(0x0D,7,0,(byte)((1<<nMeas)-1)); Thread.Sleep(200); Console.WriteLine("div=0x{0:X2}{1:X2} status2=0x{2:X2} (bit2=INVALID_CFG)", b.ReadReg(0x16), b.ReadReg(0x17), b.ReadReg(0x01));
    for(int m=1;m<=6;m++){ byte bs=(byte)(0x18+8*(m-1)); if(m<=nMeas) Console.WriteLine("MEAS{0}: sel=0x{1:X2} A={2:X2} B={3:X2} C={4:X2}", m, b.ReadReg(bs), b.ReadReg((byte)(bs+4)), b.ReadReg((byte)(bs+5)), b.ReadReg((byte)(bs+6))); }
    Console.WriteLine("0x0C=0x{0:X2} 0x0D=0x{1:X2}", b.ReadReg(0x0C), b.ReadReg(0x0D));
    var data=new Dictionary<int,List<int>>(); var frame=new List<byte>(); object lk=new object(); int pk=0;
    var types=new int[256]; var samples=new List<string>(); b.Packet+=p=>{ lock(lk){ pk++; types[p[1]]++; if(samples.Count<4) samples.Add(BitConverter.ToString(p)); int ty=p[1]; if(ty==0&&frame.Count>0){ var f=frame.ToArray(); frame.Clear(); for(int i=0;i+3<=f.Length;i+=3){ int tag=f[i]>>4; int v=((f[i]<<16)|(f[i+1]<<8)|f[i+2])&0xFFFFF; if((v&0x80000)!=0) v-=0x100000; if(tag<1||tag>9) continue; if(!data.ContainsKey(tag)) data[tag]=new List<int>(); data[tag].Add(v);} }
      if(ty==0||ty==1||ty==2||ty==10) for(int i=2;i<20;i++) frame.Add(p[i]); } };
    Console.WriteLine("Place finger now... waiting 10 s"); Thread.Sleep(1000);
    b.WriteBits(0x0A,4,4,1); b.EnableNotifications(true); b.EnableSensors(true);
    Thread.Sleep(runSec*1000/2); Console.WriteLine("status1 during stream=0x{0:X2} status2=0x{1:X2}", b.ReadReg(0x00), b.ReadReg(0x01)); Thread.Sleep(runSec*1000/2); b.EnableSensors(false); Thread.Sleep(300);
    lock(lk){
      Console.WriteLine("packets={0}", pk); for(int i=0;i<256;i++) if(types[i]>0) Console.WriteLine("type {0}: {1}", i, types[i]); foreach(var s in samples) Console.WriteLine(s);
      foreach(var kv in data.OrderBy(k=>k.Key)){
        var x=kv.Value.Skip(kv.Value.Count/5).Select(v=>(double)v).ToArray(); int n=x.Length; if(n<50){ Console.WriteLine("tag {0}: only {1} samples",kv.Key,n); continue; }
        double fs=n/(runSec*0.8); /* approx */ double dc=x.Average();
        int w=Math.Max(3,(int)(fs*1.0)); var hp=new double[n]; for(int i=0;i<n;i++){ int lo=Math.Max(0,i-w/2),hi=Math.Min(n-1,i+w/2); double s=0; for(int k=lo;k<=hi;k++) s+=x[k]; hp[i]=x[i]-s/(hi-lo+1);} 
        double ac=Math.Sqrt(hp.Select(v=>v*v).Average());
        System.IO.File.WriteAllLines("led_tag"+kv.Key+".txt", kv.Value.Select(v=>v.ToString()));
        Console.WriteLine("tag {0}: n={1,5} fs~{2,5:F1}  DC={3,8:F0} ({4,5:F1}% FS)  AC rms={5,7:F1}  PI={6,6:F3}%  min={7} max={8}", kv.Key, kv.Value.Count, fs, dc, 100*dc/524287, ac, 100*ac/Math.Abs(dc), x.Min(), x.Max());
      }
    }
    } finally { try{b.EnableSensors(false);}catch{} b.Dispose(); }
  }
  static ulong PpgStreamFind(){ return 0xF1324B0CFE32; }
}
