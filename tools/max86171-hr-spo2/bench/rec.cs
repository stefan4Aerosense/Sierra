using System; using System.Collections.Generic; using System.Linq; using System.Threading;
// rec <name> <frDivHex> <aver 0-7> <secs> <meas spec>...   spec = selects:drvA:drvB:drvC:cfg2 (hex)
class Rec {
  static void Main(string[] a){ var t=new Thread(()=>Run(a)); t.SetApartmentState(ApartmentState.MTA); t.Start(); t.Join(); }
  static byte H(string s){ return Convert.ToByte(s,16); }
  static void Run(string[] a){
    string name=a[0]; int div=Convert.ToInt32(a[1],16); int aver=int.Parse(a[2]); int secs=int.Parse(a[3]); var specs=a.Skip(4).ToArray();
    using(var b=new OS61(0xF1324B0CFE32)){
      b.EnableSensors(false); Thread.Sleep(300); b.UseAccel(false);
      b.WriteBits(0x0D,7,0,0); b.WriteBits(0x15,5,5,0); b.WriteBits(0x16,7,0,(byte)(div>>8)); b.WriteBits(0x17,7,0,(byte)div);
      for(int m=1;m<=8;m++){ byte bs=(byte)(0x18+8*(m-1));
        if(m<=specs.Length){ var p=specs[m-1].Split(':');
          b.WriteBits(bs,7,0,H(p[0])); b.WriteBits((byte)(bs+1),7,0,(byte)(p.Length>5 ? (H(p[5])|aver) : (0x18|aver))); b.WriteBits((byte)(bs+2),7,0,H(p[4])); b.WriteBits((byte)(bs+3),7,0,0x50);
          b.WriteBits((byte)(bs+4),7,0,H(p[1])); b.WriteBits((byte)(bs+5),7,0,H(p[2])); b.WriteBits((byte)(bs+6),7,0,H(p[3])); }
        else for(int d=4;d<7;d++) b.WriteBits((byte)(bs+d),7,0,0); }
      b.WriteBits(0x0C,7,7,0); b.WriteBits(0x0C,3,2,0); b.WriteBits(0x0C,1,1,0);
      b.WriteBits(0x0D,7,0,(byte)((1<<specs.Length)-1));
      Thread.Sleep(300);
      Console.WriteLine("[{0}] div=0x{1:X2}{2:X2} en=0x{3:X2} status1=0x{4:X2} status2=0x{5:X2} (INVALID_CFG=bit2)", name, b.ReadReg(0x16), b.ReadReg(0x17), b.ReadReg(0x0D), b.ReadReg(0x00), b.ReadReg(0x01));
      var data=new Dictionary<string,List<int>>(); var frame=new List<byte>(); object lk=new object(); int bad=0; var badTags=new int[16];
      b.Packet+=p=>{ lock(lk){ int ty=p[1];
        if(ty==0&&frame.Count>0){ var f=frame.ToArray(); frame.Clear(); var seen=new int[16];
          for(int i=0;i+3<=f.Length;i+=3){ int tag=f[i]>>4; int v=((f[i]<<16)|(f[i+1]<<8)|f[i+2])&0xFFFFF; if((v&0x80000)!=0) v-=0x100000;
            if(tag<1||tag>9){ bad++; badTags[tag]++; continue; }
            string key="M"+tag+"_PD"+(++seen[tag]); List<int> l; if(!data.TryGetValue(key,out l)) data[key]=l=new List<int>(); l.Add(v); if(seen[tag]==2) seen[tag]=0; } }
        if(ty==0||ty==1||ty==2||ty==10) for(int i=2;i<20;i++) frame.Add(p[i]); } };
      b.WriteBits(0x0A,4,4,1); b.EnableNotifications(true); var t0=DateTime.Now; b.EnableSensors(true);
      Thread.Sleep(secs*1000); b.EnableSensors(false); double el=(DateTime.Now-t0).TotalSeconds; Thread.Sleep(300);
      lock(lk){
        Console.WriteLine("[{0}] bad tags={1} ({2})", name, bad, string.Join(",", Enumerable.Range(0,16).Where(i=>badTags[i]>0).Select(i=>i+":"+badTags[i])));
        foreach(var kv in data.OrderBy(k=>k.Key)){
          System.IO.File.WriteAllLines("rec_"+name+"_"+kv.Key+".txt", kv.Value.Select(v=>v.ToString()));
          Console.WriteLine("[{0}] {1}: n={2} fs={3:F1} mean={4:F0} ({5:F0}% FS) max={6}", name, kv.Key, kv.Value.Count, kv.Value.Count/el, kv.Value.Average(), 100*kv.Value.Average()/524287, kv.Value.Max());
        }
      }
    }
  }
}
