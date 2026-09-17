using System; using System.IO; using System.Linq; using System.Collections.Generic;
class A3 {
  static double fs=25.3;
  static double[] L(string k){ return File.ReadAllLines("rec_C_"+k+".txt").Skip(100).Select(double.Parse).ToArray(); }
  static double[] Tone(double[] v,double f){ int n=v.Length; double sx=0,sy=0,sxx=0,sxy=0; for(int i=0;i<n;i++){sx+=i;sy+=v[i];sxx+=(double)i*i;sxy+=i*v[i];} double sl=(n*sxy-sx*sy)/(n*sxx-sx*sx), ic=(sy-sl*sx)/n; double re=0,im=0,ws=0; for(int i=0;i<n;i++){ double w=0.5-0.5*Math.Cos(2*Math.PI*i/(n-1)); double d=(v[i]-(ic+sl*i))*w, ph=2*Math.PI*f*i/fs; re+=d*Math.Cos(ph); im+=d*Math.Sin(ph); ws+=w;} return new[]{2*re/ws,2*im/ws}; }
  static double Mag(double[] c){ return Math.Sqrt(c[0]*c[0]+c[1]*c[1]); }
  static double Curve(double r){ return Math.Max(70,Math.Min(100,-45.060*r*r+30.354*r+94.845)); }
  static string Stats(List<double> v, string fmt){ double m=v.Average(), sd=Math.Sqrt(v.Select(z=>(z-m)*(z-m)).Average()); return string.Format("mean="+fmt.Replace("0","0")+" sd="+fmt.Replace("0","1")+" range="+fmt.Replace("0","2")+".."+fmt.Replace("0","3"), m, sd, v.Min(), v.Max()); }
  static void Main(){
    var g=L("M1_PD2"); double hr=0,best=0; for(double f=0.7;f<=3;f+=0.005){ double m=Mag(Tone(g,f)); if(m>best){best=m;hr=f;} }
    Console.WriteLine("HR {0:F0} bpm, {1:F0} s analysed", hr*60, g.Length/fs);
    foreach(var k in new[]{"M2_PD1","M3_PD1","M4_PD1","M5_PD1","M4_PD2","M5_PD2"}){
      var x=L(k); double dc=x.Average(), ac=Mag(Tone(x,hr)); var nb=new List<double>();
      for(double f=0.6; f<=4.0; f+=0.05){ if(Math.Abs(f-hr)<0.15||Math.Abs(f-2*hr)<0.15||Math.Abs(f-3*hr)<0.15) continue; nb.Add(Mag(Tone(x,f))); } nb.Sort();
      Console.WriteLine("  {0}: DC={1,7:F0} PI={2,5:F3}% SNR={3,5:F1}", k, dc, 100*ac/dc, ac/nb[nb.Count/2]);
    }
    var paths=new[]{ new[]{"M2_PD1","M3_PD1","single (app now)   "}, new[]{"M4_PD1","M5_PD1","all LEDs, detector1"}, new[]{"M4_PD2","M5_PD2","all LEDs, detector2"} };
    var rs=new List<double>[3]; int n=(int)(fs*16), step=(int)(fs*2);
    for(int p=0;p<3;p++){ rs[p]=new List<double>(); var rd=L(paths[p][0]); var ir=L(paths[p][1]);
      for(int st=0; st+n<=ir.Length; st+=step){ var xr=rd.Skip(st).Take(n).ToArray(); var xi=ir.Skip(st).Take(n).ToArray(); var gg=g.Skip(st).Take(n).ToArray();
        double h=hr,bm=0; for(double f=hr-0.2;f<=hr+0.2;f+=0.005){ double m=Mag(Tone(gg,f)); if(m>bm){bm=m;h=f;} }
        rs[p].Add((Mag(Tone(xr,h))/xr.Average())/(Mag(Tone(xi,h))/xi.Average())); } }
    var comb=Enumerable.Range(0,rs[1].Count).Select(i=>(rs[1][i]+rs[2][i])/2).ToList();
    Console.WriteLine("\nR and SpO2 over {0} sliding 16 s windows:", rs[0].Count);
    for(int p=0;p<3;p++) Console.WriteLine("  {0}: R {1} | SpO2 {2}", paths[p][2], Stats(rs[p],"{0:F3}"), Stats(rs[p].Select(Curve).ToList(),"{0:F1}"));
    Console.WriteLine("  all LEDs, both avg  : R {0} | SpO2 {1}", Stats(comb,"{0:F3}"), Stats(comb.Select(Curve).ToList(),"{0:F1}"));
  }
}
