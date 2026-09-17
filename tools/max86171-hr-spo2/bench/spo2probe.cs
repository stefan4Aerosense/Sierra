using System; using System.Collections.Generic; using System.Linq; using System.Threading;
class P {
  static void Main(string[] a){ var t=new Thread(()=>Run(a)); t.SetApartmentState(ApartmentState.MTA); t.Start(); t.Join(); }
  static void Run(string[] a){
    byte aver=Convert.ToByte(a[0]); byte iG=Convert.ToByte(a[1],16), iIR=Convert.ToByte(a[2],16), iR=Convert.ToByte(a[3],16); int secs=int.Parse(a[4]);
    using(var b=new OS61(0xF1324B0CFE32)){
      b.EnableSensors(false); Thread.Sleep(300); b.UseAccel(false);
      b.WriteBits(0x16,7,0,0x05); b.WriteBits(0x17,7,0,0x00);             // 25 Hz
      // (selectsByte, drvA, drvB, drvC) for green=L1 (A sel0), IR=L2 (B sel0), red=L3 (B sel1)
      var meas=new[]{ new byte[]{0x00,iG,0,0}, new byte[]{0x00,0,iIR,0}, new byte[]{0x04,0,iR,0} };
      for(int m=0;m<3;m++){ byte bs=(byte)(0x18+8*m);
        b.WriteBits(bs,7,0,meas[m][0]);
        b.WriteBits((byte)(bs+1),7,0,(byte)(0x18|aver|0x00));  // TINT=11, AVER, PD1 on PPG1, PD... (PDSEL bits 0)
        b.WriteBits((byte)(bs+2),7,0,0x3A); b.WriteBits((byte)(bs+3),7,0,0x50);
        for(int d=0;d<3;d++) b.WriteBits((byte)(bs+4+d),7,0,meas[m][1+d]); }
      for(int m=3;m<6;m++){ byte bs=(byte)(0x18+8*m); for(int d=4;d<7;d++) b.WriteBits((byte)(bs+d),7,0,0); }
      b.WriteBits(0x0C,7,7,0); b.WriteBits(0x0C,3,3,0); b.WriteBits(0x0C,2,2,0); b.WriteBits(0x0C,1,1,0); // both PPG channels on
      b.WriteBits(0x0D,7,0,0x07);
      Thread.Sleep(200); Console.WriteLine("sys1=0x{0:X2} en=0x{1:X2} m1cfg1=0x{2:X2} status2=0x{3:X2}", b.ReadReg(0x0C), b.ReadReg(0x0D), b.ReadReg(0x19), b.ReadReg(0x01));
      var data=new Dictionary<string,List<int>>(); var frame=new List<byte>(); object lk=new object(); int badTag=0;
      b.Packet+=p=>{ lock(lk){ int ty=p[1];
        if(ty==0&&frame.Count>0){ var f=frame.ToArray(); frame.Clear(); var seen=new int[16];
          for(int i=0;i+3<=f.Length;i+=3){ int tag=f[i]>>4; int v=((f[i]<<16)|(f[i+1]<<8)|f[i+2])&0xFFFFF; if((v&0x80000)!=0) v-=0x100000; if(tag<1||tag>9){ badTag++; continue; }
            string key="M"+tag+"_PD"+(++seen[tag]); if(!data.ContainsKey(key)) data[key]=new List<int>(); data[key].Add(v);} }
        if(ty==0||ty==1||ty==2||ty==10) for(int i=2;i<20;i++) frame.Add(p[i]); } };
      b.WriteBits(0x0A,4,4,1); b.EnableNotifications(true); b.EnableSensors(true);
      Thread.Sleep(secs*1000); b.EnableSensors(false); Thread.Sleep(300);
      lock(lk){
        Console.WriteLine("bad tags: {0}", badTag);
        foreach(var kv in data.OrderBy(k=>k.Key)){ System.IO.File.WriteAllLines("sp_"+kv.Key+".txt", kv.Value.Select(v=>v.ToString())); }
        // find HR from green PD1 spectrum, then report amplitude at HR for all channels
        Func<double[],double,double> amp=(x,f)=>{ int n=x.Length; double m=x.Average(), re=0, im=0; for(int i=0;i<n;i++){ double ph=2*Math.PI*f*i/25.0; re+=(x[i]-m)*Math.Cos(ph); im+=(x[i]-m)*Math.Sin(ph);} return 2*Math.Sqrt(re*re+im*im)/n; };
        var g=data.ContainsKey("M1_PD1")? data["M1_PD1"].Skip(50).Select(v=>(double)v).ToArray() : null;
        double hrF=0, best=0; if(g!=null) for(double f=0.8; f<=3.0; f+=0.02){ double am=amp(g,f); if(am>best){best=am; hrF=f;} }
        Console.WriteLine("HR from green: {0:F2} Hz = {1:F0} bpm", hrF, hrF*60);
        foreach(var kv in data.OrderBy(k=>k.Key)){
          var x=kv.Value.Skip(50).Select(v=>(double)v).ToArray(); if(x.Length<100) continue;
          double dc=x.Average(), ac=0; for(double f=hrF-0.06; f<=hrF+0.061; f+=0.02) ac=Math.Max(ac,amp(x,f));
          double noise=Math.Sqrt(Enumerable.Range(1,x.Length-1).Select(i=>(x[i]-x[i-1])*(x[i]-x[i-1])).Average()/2);
          Console.WriteLine("{0}: n={1} DC={2,8:F0} ({3,4:F1}% FS)  AC@HR={4,7:F1}  PI={5,6:F3}%  noise={6,6:F1}  max={7}", kv.Key, kv.Value.Count, dc, 100*dc/524287, ac, 100*ac/dc, noise, x.Max());
        }
      }
    }
  }
}
