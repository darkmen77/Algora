using System.Globalization;
using System.Text.RegularExpressions;

namespace Algora.Core;

public sealed class Interpreter
{
    public sealed record VariableInfo(string Type, object? Value);
    private readonly Dictionary<string, VariableInfo> _vars = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, object?> Variables => _vars.ToDictionary(x=>x.Key,x=>x.Value.Value,StringComparer.OrdinalIgnoreCase);

    public string Run(string source, Func<string, string>? input = null)
    {
        _vars.Clear();
        var output=new List<string>();
        var lines=source.Replace("\r","").Split('\n');
        DeclareVariables(lines);
        var start=Array.FindIndex(lines,l=>l.Trim().Equals("ΑΡΧΗ",StringComparison.OrdinalIgnoreCase));
        if(start<0) throw Error(1,"Δεν βρέθηκε η λέξη ΑΡΧΗ.");
        ExecuteBlock(lines,start+1,lines.Length,output,input);
        return string.Join(Environment.NewLine,output);
    }

    void DeclareVariables(string[] lines)
    {
        foreach(var raw in lines)
        {
            var line=raw.Trim();
            var m=Regex.Match(line,@"^(ΑΚΕΡΑΙΕΣ|ΠΡΑΓΜΑΤΙΚΕΣ|ΧΑΡΑΚΤΗΡΕΣ|ΛΟΓΙΚΕΣ)\s*:\s*(.+)$",RegexOptions.IgnoreCase);
            if(!m.Success) continue;
            var type=m.Groups[1].Value.ToUpperInvariant();
            foreach(var name in m.Groups[2].Value.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries))
            {
                if(!Regex.IsMatch(name,@"^[\p{L}_][\p{L}\p{N}_]*$")) throw new InvalidOperationException($"Μη έγκυρο όνομα μεταβλητής: {name}");
                _vars[name]=new(type,null);
            }
        }
    }

    int ExecuteBlock(string[] lines,int from,int to,List<string> output,Func<string,string>? input)
    {
        for(int i=from;i<to;i++)
        {
            var line=lines[i].Trim();
            if(string.IsNullOrWhiteSpace(line)||line.StartsWith("!")) continue;
            if(Regex.IsMatch(line,@"^(ΤΕΛΟΣ_ΠΡΟΓΡΑΜΜΑΤΟΣ|ΤΕΛΟΣ\\b)",RegexOptions.IgnoreCase)) return i;
            if(Regex.IsMatch(line,@"^(ΑΛΛΙΩΣ|ΤΕΛΟΣ_ΑΝ|ΤΕΛΟΣ_ΕΠΑΝΑΛΗΨΗΣ|ΜΕΧΡΙΣ_ΟΤΟΥ)\\b",RegexOptions.IgnoreCase)) return i;

            var ifm=Regex.Match(line,@"^ΑΝ\\s+(.+?)\\s+ΤΟΤΕ$",RegexOptions.IgnoreCase);
            if(ifm.Success)
            {
                var (elseAt,endAt)=FindIfBounds(lines,i+1,to);
                if(ToBool(Eval(ifm.Groups[1].Value,i+1))) ExecuteBlock(lines,i+1,elseAt>=0?elseAt:endAt,output,input);
                else if(elseAt>=0) ExecuteBlock(lines,elseAt+1,endAt,output,input);
                i=endAt;continue;
            }

            var read=Regex.Match(line,@"^ΔΙΑΒΑΣΕ\\s+(.+)$",RegexOptions.IgnoreCase);
            if(read.Success)
            {
                if(input is null) throw Error(i+1,"Δεν είναι διαθέσιμη είσοδος δεδομένων.");
                foreach(var name in read.Groups[1].Value.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries))
                {
                    if(!_vars.TryGetValue(name,out var info)) throw Error(i+1,$"Η μεταβλητή «{name}» δεν έχει δηλωθεί.");
                    var raw=input(name);
                    try {
                        object value=info.Type switch {
                            "ΑΚΕΡΑΙΕΣ"=>int.Parse(raw,CultureInfo.CurrentCulture),
                            "ΠΡΑΓΜΑΤΙΚΕΣ"=>double.Parse(raw,CultureInfo.CurrentCulture),
                            "ΛΟΓΙΚΕΣ"=>raw.Equals("ΑΛΗΘΗΣ",StringComparison.OrdinalIgnoreCase)?true:raw.Equals("ΨΕΥΔΗΣ",StringComparison.OrdinalIgnoreCase)?false:bool.Parse(raw),
                            _=>raw };
                        Assign(name,value,i+1);
                    } catch(FormatException) { throw Error(i+1,$"Μη έγκυρη τιμή για τη μεταβλητή «{name}»."); }
                }
                continue;
            }

            var whileM=Regex.Match(line,@"^ΟΣΟ\\s+(.+?)\\s+ΕΠΑΝΑΛΑΒΕ$",RegexOptions.IgnoreCase);
            if(whileM.Success)
            {
                var loopEnd=FindMatching(lines,i+1,to,@"^ΟΣΟ\\b.*\\bΕΠΑΝΑΛΑΒΕ$",@"^ΤΕΛΟΣ_ΕΠΑΝΑΛΗΨΗΣ$");
                int guard=0;
                while(ToBool(Eval(whileM.Groups[1].Value,i+1)))
                {
                    ExecuteBlock(lines,i+1,loopEnd,output,input);
                    if(++guard>100000) throw Error(i+1,"Η επανάληψη ξεπέρασε τα 100000 βήματα.");
                }
                i=loopEnd;continue;
            }

            var forM=Regex.Match(line,@"^ΓΙΑ\\s+([\\p{L}_][\\p{L}\\p{N}_]*)\\s+ΑΠΟ\\s+(.+?)\\s+ΜΕΧΡΙ\\s+(.+?)(?:\\s+ΜΕ_ΒΗΜΑ\\s+(.+))?$",RegexOptions.IgnoreCase);
            if(forM.Success)
            {
                var loopEnd=FindMatching(lines,i+1,to,@"^ΓΙΑ\\b",@"^ΤΕΛΟΣ_ΕΠΑΝΑΛΗΨΗΣ$");
                var name=forM.Groups[1].Value;
                var first=Convert.ToDouble(Eval(forM.Groups[2].Value,i+1),CultureInfo.InvariantCulture);
                var last=Convert.ToDouble(Eval(forM.Groups[3].Value,i+1),CultureInfo.InvariantCulture);
                var step=forM.Groups[4].Success?Convert.ToDouble(Eval(forM.Groups[4].Value,i+1),CultureInfo.InvariantCulture):1d;
                if(step==0) throw Error(i+1,"Το βήμα της ΓΙΑ δεν μπορεί να είναι 0.");
                for(var v=first;step>0?v<=last:v>=last;v+=step)
                {
                    Assign(name,v,i+1);
                    ExecuteBlock(lines,i+1,loopEnd,output,input);
                }
                i=loopEnd;continue;
            }

            if(Regex.IsMatch(line,@"^ΑΡΧΗ_ΕΠΑΝΑΛΗΨΗΣ$",RegexOptions.IgnoreCase))
            {
                var loopEnd=FindMatching(lines,i+1,to,@"^ΑΡΧΗ_ΕΠΑΝΑΛΗΨΗΣ$",@"^ΜΕΧΡΙΣ_ΟΤΟΥ\\b");
                var until=Regex.Match(lines[loopEnd].Trim(),@"^ΜΕΧΡΙΣ_ΟΤΟΥ\\s+(.+)$",RegexOptions.IgnoreCase);
                if(!until.Success) throw Error(loopEnd+1,"Αναμενόταν συνθήκη μετά το ΜΕΧΡΙΣ_ΟΤΟΥ.");
                int guard=0;
                do
                {
                    ExecuteBlock(lines,i+1,loopEnd,output,input);
                    if(++guard>100000) throw Error(i+1,"Η επανάληψη ξεπέρασε τα 100000 βήματα.");
                } while(!ToBool(Eval(until.Groups[1].Value,loopEnd+1)));
                i=loopEnd;continue;
            }

            var write=Regex.Match(line,@"^(ΓΡΑΨΕ|ΕΜΦΑΝΙΣΕ)\\s+(.+)$",RegexOptions.IgnoreCase);
            if(write.Success){output.Add(Print(write.Groups[2].Value,i+1));continue;}

            var assign=Regex.Match(line,@"^([\\p{L}_][\\p{L}\\p{N}_]*)\\s*(?:<-|←)\\s*(.+)$");
            if(assign.Success){Assign(assign.Groups[1].Value,Eval(assign.Groups[2].Value,i+1),i+1);continue;}

            if(Regex.IsMatch(line,@"^(ΠΡΟΓΡΑΜΜΑ|ΑΛΓΟΡΙΘΜΟΣ|ΜΕΤΑΒΛΗΤΕΣ|ΑΚΕΡΑΙΕΣ|ΠΡΑΓΜΑΤΙΚΕΣ|ΧΑΡΑΚΤΗΡΕΣ|ΛΟΓΙΚΕΣ)\\b",RegexOptions.IgnoreCase)) continue;
            throw Error(i+1,$"Δεν αναγνωρίζεται η εντολή «{line}».");
        }
        return to;
    }

    int FindMatching(string[] lines,int from,int to,string openPattern,string closePattern)
    {
        int depth=0;
        for(int i=from;i<to;i++){var s=lines[i].Trim();if(Regex.IsMatch(s,openPattern,RegexOptions.IgnoreCase))depth++;else if(Regex.IsMatch(s,closePattern,RegexOptions.IgnoreCase)){if(depth==0)return i;depth--;}}
        throw Error(from,"Λείπει το τέλος της δομής επανάληψης.");
    }

    (int ElseAt,int EndAt) FindIfBounds(string[] lines,int from,int to)
    {
        int depth=0,elseAt=-1;
        for(int i=from;i<to;i++)
        {
            var s=lines[i].Trim();
            if(Regex.IsMatch(s,@"^ΑΝ\b.*\bΤΟΤΕ$",RegexOptions.IgnoreCase)) depth++;
            else if(Regex.IsMatch(s,@"^ΤΕΛΟΣ_ΑΝ$",RegexOptions.IgnoreCase)){if(depth==0)return(elseAt,i);depth--;}
            else if(depth==0&&Regex.IsMatch(s,@"^ΑΛΛΙΩΣ$",RegexOptions.IgnoreCase)) elseAt=i;
        }
        throw Error(from,"Λείπει ΤΕΛΟΣ_ΑΝ.");
    }

    void Assign(string name,object value,int line)
    {
        if(!_vars.TryGetValue(name,out var info)) throw Error(line,$"Η μεταβλητή «{name}» δεν έχει δηλωθεί.");
        try
        {
            object v=info.Type switch
            {
                "ΑΚΕΡΑΙΕΣ" => Convert.ToInt32(value,CultureInfo.InvariantCulture),
                "ΠΡΑΓΜΑΤΙΚΕΣ" => Convert.ToDouble(value,CultureInfo.InvariantCulture),
                "ΧΑΡΑΚΤΗΡΕΣ" => Convert.ToString(value,CultureInfo.CurrentCulture)??"",
                "ΛΟΓΙΚΕΣ" => ToBool(value),
                _ => value
            };
            _vars[name]=info with {Value=v};
        } catch { throw Error(line,$"Ασυμβατότητα τύπου στη μεταβλητή «{name}»."); }
    }

    string Print(string expr,int line)=>string.Join(" ",SplitComma(expr).Select(p=>{
        var t=p.Trim();
        if(IsString(t)) return t[1..^1];
        return Format(Eval(t,line));
    }));

    object Eval(string expr,int line)
    {
        var p=new ExpressionParser(expr,Resolve,line);
        return p.Parse();
    }
    object Resolve(string name,int line)
    {
        if(!_vars.TryGetValue(name,out var v)) throw Error(line,$"Η μεταβλητή «{name}» δεν έχει δηλωθεί.");
        if(v.Value is null) throw Error(line,$"Η μεταβλητή «{name}» δεν έχει τιμή.");
        return v.Value;
    }
    static string Format(object v)=>v switch { bool b=>b?"ΑΛΗΘΗΣ":"ΨΕΥΔΗΣ", double d=>d.ToString("G",CultureInfo.CurrentCulture), _=>Convert.ToString(v,CultureInfo.CurrentCulture)??""};
    static bool ToBool(object v)=>v is bool b?b:throw new InvalidOperationException("Αναμενόταν λογική έκφραση.");
    static bool IsString(string s)=>s.Length>=2&&((s[0]=='\''&&s[^1]=='\'')||(s[0]=='"'&&s[^1]=='"'));
    static IEnumerable<string> SplitComma(string s){var q=false;char qc='\0';var start=0;for(int i=0;i<s.Length;i++){if((s[i]=='\''||s[i]=='"')){if(!q){q=true;qc=s[i];}else if(qc==s[i])q=false;}else if(s[i]==','&&!q){yield return s[start..i];start=i+1;}}yield return s[start..];}
    static InvalidOperationException Error(int line,string msg)=>new($"Γραμμή {line}: {msg}");

    sealed class ExpressionParser
    {
        readonly List<string> t; int p; readonly Func<string,int,object> resolve; readonly int line;
        public ExpressionParser(string s,Func<string,int,object> r,int l){t=Tokenize(s);resolve=r;line=l;}
        public object Parse(){var v=Or();if(p<t.Count)throw Error(line,$"Μη αναμενόμενο «{t[p]}».");return v;}
        object Or(){var v=And();while(Match("Ή","Η"))v=ToBool(v)||ToBool(And());return v;}
        object And(){var v=Compare();while(Match("ΚΑΙ"))v=ToBool(v)&&ToBool(Compare());return v;}
        object Compare(){var a=Add();if(p<t.Count&&new[]{"=","<>",">","<",">=","<="}.Contains(t[p])){var op=t[p++];var b=Add();var c=Cmp(a,b);return op switch{"="=>c==0,"<>"=>c!=0,">"=>c>0,"<"=>c<0,">="=>c>=0,"<="=>c<=0,_=>false};}return a;}
        object Add(){var v=Mul();while(p<t.Count&&(t[p]=="+"||t[p]=="-")){var op=t[p++];var r=Mul();v=op=="+"?Num(v)+Num(r):Num(v)-Num(r);}return v;}
        object Mul(){var v=Unary();while(p<t.Count&&(t[p]=="*"||t[p]=="/")){var op=t[p++];var r=Unary();v=op=="*"?Num(v)*Num(r):Num(v)/Num(r);}return v;}
        object Unary(){if(Match("-"))return-Num(Unary());if(Match("ΟΧΙ"))return!ToBool(Unary());return Primary();}
        object Primary(){if(Match("(")){var v=Or();Need(")");return v;}if(p>=t.Count)throw Error(line,"Ελλιπής έκφραση.");var x=t[p++];if(IsString(x))return x[1..^1];if(double.TryParse(x,NumberStyles.Float,CultureInfo.InvariantCulture,out var n))return n;if(x.Equals("ΑΛΗΘΗΣ",StringComparison.OrdinalIgnoreCase))return true;if(x.Equals("ΨΕΥΔΗΣ",StringComparison.OrdinalIgnoreCase))return false;return resolve(x,line);}
        bool Match(params string[] xs){if(p<t.Count&&xs.Any(x=>x.Equals(t[p],StringComparison.OrdinalIgnoreCase))){p++;return true;}return false;}
        void Need(string x){if(!Match(x))throw Error(line,$"Αναμενόταν «{x}».");}
        static double Num(object v)=>Convert.ToDouble(v,CultureInfo.InvariantCulture);
        static int Cmp(object a,object b){if(a is string||b is string)return string.Compare(Convert.ToString(a),Convert.ToString(b),StringComparison.CurrentCulture);return Num(a).CompareTo(Num(b));}
        static List<string> Tokenize(string s){var m=Regex.Matches(s,@"\s*(>=|<=|<>|[()+\-*/=<>]|'(?:[^']*)'|""(?:[^""]*)""|\d+(?:[\.,]\d+)?|[\p{L}_][\p{L}\p{N}_]*)");var list=m.Select(x=>x.Groups[1].Value.Replace(',','.')).ToList();if(string.Concat(m.Select(x=>x.Value)).Replace(" ","")!=s.Replace(" ",""))throw new InvalidOperationException("Μη έγκυρη έκφραση.");return list;}
    }
}