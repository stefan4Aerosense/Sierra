using System; using System.IO; using System.Linq; using System.Collections.Generic;
class A2 {
  static double[] Load(string p, int skip){ return File.ReadAllLines(p).Skip(skip).Select(double.Parse).ToArray(); }
  static double[] Tone(double[] v, double f, double fs){ int n=v.Length; double sx=0,sy=0,sxx=0,sxy=0; for(int i=0;i<n;i++){sx+=i;sy+=v[i];sxx+=(double)i*i;sxy+=i*v[i];} double sl=(n*sxy-sx*sy)/(n*sxx-sx*sx), ic=(sy-sl*sx)/n; double re=0,im=0,ws=0; for(int i=0;i<n;i++){ double w=0.5-0.5*Math.Cos(2*Math.PI*i/(n-1)); double d=(v[i]-(ic+sl*i))*w, ph=2*Math.PI*f*i/fs; re+=d*Math.Cos(ph); im+=d*Math.Sin(ph); ws+=w;} return new[]{2*re/ws,2*im/ws}; }
  static double Mag(double[] c){ return Math.Sqrt(c[0]*c[0]+c[1]*c[1]); }
  // simple band-pass: subtract moving average (hp) then moving average (lp)
  static double[] Band(double[] x, double fs){ int n=x.Length, w=(int)Math.Round(fs*1.2), l=Math.Max(1,(int)Math.Round(fs*0.12)); var h=new double[n]; for(int i=0;i<n;i++){int a=Math.Max(0,i-w/2),b=Math.Min(n-1,i+w/2); double s=0; for(int k=a;k<=b;k++) s+=x[k]; h[i]=x[i]-s/(b-a+1);} var o=new double[n]; for(int i=0;i<n;i++){int a=Math.Max(0,i-l/2),b=Math.Min(n-1,i+l/2); double s=0; for(int k=a;k<=b;k++) s+=h[k]; o[i]=s/(b-a+1);} return o; }
  static void Main(string[] args){
    string s=args[0]; double fs=double.Parse(args[1]); int skip=(int)(fs*4);
    Func<string,double[]> L = k => Load("rec_"+s+"_"+k+".txt", skip);
    var g=L("M1_PD2"); double hr=0,best=0; for(double f=0.7;f<=3.0;f+=0.005){ double m=Mag(Tone(g,f,fs)); if(m>best){best=m;hr=f;} }
    Console.WriteLine("== session {0}: fs={1} HR={2:F3} Hz ({3:F0} bpm), {4:F0} s analysed", s, fs, hr, hr*60, g.Length/fs);
    foreach(var k in new[]{"M1_PD1","M1_PD2","M2_PD1","M2_PD2","M3_PD1","M3_PD2","M4_PD1","M4_PD2","M5_PD1","M5_PD2"}){
      var x=L(k); double dc=x.Average(); double ac=Mag(Tone(x,hr,fs));
      var nb=new List<double>(); for(double f=0.6; f<=Math.Min(4.0,fs/2-0.3); f+=0.05){ if(Math.Abs(f-hr)<0.15||Math.Abs(f-2*hr)<0.15||Math.Abs(f-3*hr)<0.15) continue; nb.Add(Mag(Tone(x,f,fs))); }
      nb.Sort(); double noise=nb[nb.Count/2];
      Console.WriteLine("  {0}: DC={1,7:F0} AC={2,7:F1} PI={3,6:F3}%  noise={4,6:F1}  SNR={5,5:F1}", k, dc, ac, 100*ac/dc, noise, ac/noise);
    }
    foreach(var pair in new[]{ new[]{"M2_PD1","M3_PD1","base PD1"}, new[]{"M4_PD1","M5_PD1","hi   PD1"}, new[]{"M2_PD2","M3_PD2","base PD2"}, new[]{"M4_PD2","M5_PD2","hi   PD2"} }){
      var ir=L(pair[0]); var rd=L(pair[1]); int n=(int)(fs*16), step=(int)(fs*2);
      var rl=new List<double>(); var rreg=new List<double>(); int phOk=0, tot=0;
      for(int st=0; st+n<=ir.Length; st+=step){
        var xi=ir.Skip(st).Take(n).ToArray(); var xr=rd.Skip(st).Take(n).ToArray(); double dI=xi.Average(), dR=xr.Average();
        // local HR from green
        var gg=g.Skip(st).Take(n).ToArray(); double h=hr, bm=0; for(double f=hr-0.2; f<=hr+0.2; f+=0.005){ double m=Mag(Tone(gg,f,fs)); if(m>bm){bm=m;h=f;} }
        var ci=Tone(xi,h,fs); var cr=Tone(xr,h,fs); double ph=Math.Atan2(cr[1]*ci[0]-cr[0]*ci[1], cr[0]*ci[0]+cr[1]*ci[1])*180/Math.PI; tot++; if(Math.Abs(ph)<=25) phOk++;
        rl.Add((Mag(cr)/dR)/(Mag(ci)/dI));
        var bi=Band(xi,fs); var br=Band(xr,fs); double sxy=0,sxx=0; for(int i=0;i<n;i++){ sxy+=bi[i]*br[i]; sxx+=bi[i]*bi[i]; } rreg.Add((sxy/sxx)*(dI/dR));
      }
      Func<List<double>,string> st2 = v => { double m=v.Average(), sd=Math.Sqrt(v.Select(z=>(z-m)*(z-m)).Average()); var o=v.OrderBy(z=>z).ToList(); return string.Format("median={0:F2} mean={1:F2} sd={2:F2} range={3:F2}..{4:F2}", o[o.Count/2], m, sd, o.First(), o.Last()); };
      Console.WriteLine("  R {0}: lock-in {1} | regression {2} | phase ok {3}/{4}", pair[2], st2(rl), st2(rreg), phOk, tot);
    }
  }
}
