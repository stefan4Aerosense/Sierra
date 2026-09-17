using System; using System.IO; using System.Linq; using System.Reflection; using System.Reflection.Emit; using System.Collections.Generic; using System.Text;
class ILD {
 static Dictionary<short,OpCode> ops=new Dictionary<short,OpCode>();
 static string dir;
 static void Main(string[] a){
  dir=a[0];
  AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{ var p=Path.Combine(dir,new AssemblyName(e.Name).Name+".dll"); return File.Exists(p)?Assembly.LoadFrom(p):null; };
  foreach(var f in typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static)){ var o=(OpCode)f.GetValue(null); ops[o.Value]=o; }
  var asm=Assembly.LoadFrom(Path.Combine(dir,a[1])); string filter=a.Length>2?a[2]:null; bool il=a.Length>3;
  Type[] types; try{types=asm.GetTypes();}catch(ReflectionTypeLoadException ex){types=ex.Types.Where(t=>t!=null).ToArray();}
  var BF=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly;
  foreach(var t in types.OrderBy(t=>t.FullName)){
   if(filter!=null && !System.Text.RegularExpressions.Regex.IsMatch(t.FullName,filter)) continue;
   Console.WriteLine("TYPE "+t.FullName+" : "+t.BaseType);
   try{
   if(t.IsEnum){ foreach(var f in t.GetFields(BindingFlags.Public|BindingFlags.Static)) Console.WriteLine("   "+f.Name+" = 0x"+Convert.ToInt64(f.GetRawConstantValue()).ToString("X")); continue; }
   foreach(var f in t.GetFields(BF)){ string v=""; if(f.IsLiteral) try{v=" = "+f.GetRawConstantValue();}catch{} Console.WriteLine("   F "+f.FieldType.Name+" "+f.Name+v); }
   foreach(var m in t.GetMethods(BF).Cast<MethodBase>().Concat(t.GetConstructors(BF))){
    Console.WriteLine("   M "+m.Name+"("+string.Join(", ",m.GetParameters().Select(p=>p.ParameterType.Name+" "+p.Name))+")");
    if(il){ var body=m.GetMethodBody(); if(body!=null) Dis(m,body.GetILAsByteArray()); }
   }
   }catch(Exception ex){Console.WriteLine("   !! "+ex.Message);}
  }
 }
 static void Dis(MethodBase m, byte[] b){
  int i=0; var mod=m.Module; Type[] tg=m.DeclaringType.IsGenericType?m.DeclaringType.GetGenericArguments():null; Type[] mg=m.IsGenericMethod?m.GetGenericArguments():null;
  while(i<b.Length){ int start=i; OpCode op; if(b[i]==0xFE){ op=ops[(short)(0xFE00|b[i+1])]; i+=2; } else { op=ops[b[i]]; i++; }
   string arg="";
   switch(op.OperandType){
    case OperandType.InlineNone: break;
    case OperandType.ShortInlineBrTarget: arg="IL_"+(i+1+(sbyte)b[i]).ToString("X4"); i+=1; break;
    case OperandType.ShortInlineI: arg=((sbyte)b[i]).ToString()+" (0x"+b[i].ToString("X2")+")"; i+=1; break;
    case OperandType.ShortInlineVar: arg=b[i].ToString(); i+=1; break;
    case OperandType.InlineBrTarget: arg="IL_"+(i+4+BitConverter.ToInt32(b,i)).ToString("X4"); i+=4; break;
    case OperandType.InlineI: { int v=BitConverter.ToInt32(b,i); arg=v+" (0x"+v.ToString("X")+")"; i+=4; break; }
    case OperandType.InlineI8: arg=BitConverter.ToInt64(b,i).ToString(); i+=8; break;
    case OperandType.InlineR: arg=BitConverter.ToDouble(b,i).ToString(); i+=8; break;
    case OperandType.ShortInlineR: arg=BitConverter.ToSingle(b,i).ToString(); i+=4; break;
    case OperandType.InlineVar: arg=BitConverter.ToInt16(b,i).ToString(); i+=2; break;
    case OperandType.InlineString: { int tok=BitConverter.ToInt32(b,i); i+=4; try{arg="\""+mod.ResolveString(tok)+"\"";}catch{arg="str?";} break; }
    case OperandType.InlineSwitch: { int n=BitConverter.ToInt32(b,i); i+=4; var l=new List<string>(); for(int k=0;k<n;k++){ l.Add("IL_"+(i+4*n+BitConverter.ToInt32(b,i+4*k)).ToString("X4")); } i+=4*n; arg=string.Join(",",l); break; }
    default: { int tok=BitConverter.ToInt32(b,i); i+=4; try{ var mem=mod.ResolveMember(tok,tg,mg); arg=(mem.DeclaringType!=null?mem.DeclaringType.Name+"::":"")+mem.Name; var fi=mem as FieldInfo; if(fi!=null&&fi.IsLiteral) arg+=" ="+fi.GetRawConstantValue(); }catch{ arg="tok 0x"+tok.ToString("X8"); } break; }
   }
   Console.WriteLine("      IL_"+start.ToString("X4")+" "+op.Name+" "+arg);
  }
 }
}
