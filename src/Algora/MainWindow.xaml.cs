using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using Algora.Core;
namespace Algora;
public partial class MainWindow : Window
{
 readonly Interpreter _interpreter=new(); readonly Dictionary<int,Shape> _flowShapes=new(); bool _dark; int _stepIndex=-1; string? _file; WindowStyle _oldStyle; WindowState _oldState;
 public MainWindow(){InitializeComponent();Loaded+=(_,__)=>{InstallHighlighting();ApplyTheme();};}
 static Brush B(string h)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(h));
 string CodeText()=>Editor.Text.TrimEnd('\r','\n');
 void SetCode(string s)=>Editor.Text=s;
 void InstallHighlighting(){
  var xshd=$@"<SyntaxDefinition name='Algora' extensions='.glossa;.algo' xmlns='http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008'>
<Color name='Keyword' foreground='{(_dark?"#8AB4FF":"#4F46E5")}' fontWeight='bold'/>
<Color name='String' foreground='{(_dark?"#F6C177":"#A16207")}'/>
<Color name='Comment' foreground='{(_dark?"#7F8EA3":"#64748B")}' fontStyle='italic'/>
<RuleSet>
<Span color='String' begin='&apos;' end='&apos;'/>
<Span color='String' begin='&quot;' end='&quot;'/>
<Span color='Comment' begin='!' end='\n'/>
<Keywords color='Keyword'>
<Word>ΠΡΟΓΡΑΜΜΑ</Word><Word>ΑΛΓΟΡΙΘΜΟΣ</Word><Word>ΜΕΤΑΒΛΗΤΕΣ</Word><Word>ΑΚΕΡΑΙΕΣ</Word><Word>ΠΡΑΓΜΑΤΙΚΕΣ</Word><Word>ΧΑΡΑΚΤΗΡΕΣ</Word><Word>ΛΟΓΙΚΕΣ</Word><Word>ΑΡΧΗ</Word><Word>ΤΕΛΟΣ_ΠΡΟΓΡΑΜΜΑΤΟΣ</Word><Word>ΔΙΑΒΑΣΕ</Word><Word>ΓΡΑΨΕ</Word><Word>ΕΜΦΑΝΙΣΕ</Word><Word>ΑΝ</Word><Word>ΤΟΤΕ</Word><Word>ΑΛΛΙΩΣ</Word><Word>ΤΕΛΟΣ_ΑΝ</Word><Word>ΚΑΙ</Word><Word>Ή</Word><Word>Η</Word><Word>ΟΧΙ</Word><Word>ΑΛΗΘΗΣ</Word><Word>ΨΕΥΔΗΣ</Word><Word>ΟΣΟ</Word><Word>ΕΠΑΝΑΛΑΒΕ</Word><Word>ΓΙΑ</Word><Word>ΑΠΟ</Word><Word>ΜΕΧΡΙ</Word><Word>ΜΕ_ΒΗΜΑ</Word><Word>ΤΕΛΟΣ_ΕΠΑΝΑΛΗΨΗΣ</Word><Word>ΑΡΧΗ_ΕΠΑΝΑΛΗΨΗΣ</Word><Word>ΜΕΧΡΙΣ_ΟΤΟΥ</Word>
</Keywords></RuleSet></SyntaxDefinition>";
  using var sr=new StringReader(xshd);using var xr=XmlReader.Create(sr);Editor.SyntaxHighlighting=HighlightingLoader.Load(xr,HighlightingManager.Instance);
 }
 void ApplyTheme(){
  var bg=B(_dark?"#172033":"#F3F6FB");var card=B(_dark?"#202B40":"#FFFFFF");var header=B(_dark?"#28364E":"#F8FAFC");var text=B(_dark?"#F1F5FB":"#172033");var muted=B(_dark?"#AAB8CC":"#64748B");var border=B(_dark?"#35455F":"#E2E8F0");var gutter=B(_dark?"#26344B":"#F7F9FC");
  Background=Root.Background=bg;TopBar.Background=Toolbar.Background=card;TopBar.BorderBrush=Toolbar.BorderBrush=border;EditorCard.Background=OutputCard.Background=card;EditorCard.BorderBrush=OutputCard.BorderBrush=border;EditorHeader.Background=ConsoleHeader.Background=header;TitleText.Foreground=EditorHeaderText.Foreground=ConsoleHeaderText.Foreground=VariablesTitle.Foreground=text;SubtitleText.Foreground=FileText.Foreground=muted;
  Editor.Background=ConsoleBox.Background=VariablesPanel.Background=VariablesGrid.Background=card;Editor.Foreground=ConsoleBox.Foreground=VariablesGrid.Foreground=text;Editor.LineNumbersForeground=muted;Editor.TextArea.TextView.CurrentLineBackground=gutter;Editor.TextArea.TextView.CurrentLineBorder=new Pen(border,1);
  TraceTitle.Foreground=text;TracePanel.BorderBrush=border;TraceGrid.Background=card;TraceGrid.Foreground=text;TraceGrid.RowBackground=card;TraceGrid.AlternatingRowBackground=gutter;TraceGrid.HorizontalGridLinesBrush=border;TraceGrid.VerticalGridLinesBrush=border;VariablesGrid.RowBackground=card;VariablesGrid.AlternatingRowBackground=gutter;VariablesGrid.HorizontalGridLinesBrush=border;VariablesGrid.VerticalGridLinesBrush=border;VariablesPanel.BorderBrush=border;StatusBar.Background=B(_dark?"#111A2A":"#172033");StatusText.Foreground=VersionText.Foreground=B("#CBD5E1");StatusDot.Fill=B("#22C55E");ThemeButton.Background=B(_dark?"#253552":"#EEF2FF");ThemeButton.Foreground=B(_dark?"#DCE6FF":"#3730A3");NewButton.Background=B(_dark?"#334155":"#E2E8F0");NewButton.Foreground=text;OpenButton.Background=B(_dark?"#334155":"#E2E8F0");OpenButton.Foreground=text;SaveButton.Background=B(_dark?"#166534":"#DCFCE7");SaveButton.Foreground=B(_dark?"#DCFCE7":"#166534");RunButton.Background=B("#6D5DFB");RunButton.Foreground=Brushes.White;StepButton.Background=B(_dark?"#9A5B13":"#FEF3C7");StepButton.Foreground=B(_dark?"#FFF3D0":"#92400E");FlowButton.Background=B(_dark?"#075985":"#E0F2FE");FlowButton.Foreground=B(_dark?"#D8F3FF":"#075985");ClearButton.Background=B(_dark?"#7F1D1D":"#FEE2E2");ClearButton.Foreground=B(_dark?"#FFE4E6":"#991B1B");InstallHighlighting();
 }
 void Theme_Click(object s,RoutedEventArgs e){_dark=!_dark;ApplyTheme();if(WorkTabs.SelectedIndex==1)BuildFlowchart();}
 void Flow_Click(object s,RoutedEventArgs e){BuildFlowchart();WorkTabs.SelectedIndex=1;}
 void WorkTabs_SelectionChanged(object s,SelectionChangedEventArgs e){if(IsLoaded&&WorkTabs.SelectedIndex==1)BuildFlowchart();}
 void BuildFlowchart(){
  FlowCanvas.Children.Clear();_flowShapes.Clear();FlowCanvas.Background=B(_dark?"#0F192B":"#F8FAFC");var lines=CodeText().Replace("\r","").Split('\n');const double cx=380,w=300,h=64;double y=28;AddFlowNode(0,"ΑΡΧΗ",cx,y,w,h,"start");y+=100;
  for(int i=0;i<lines.Length;i++){var t=lines[i].Trim();if(SkipFlow(t))continue;if(t.StartsWith("ΑΝ ",StringComparison.OrdinalIgnoreCase)){int end=FindFlowEnd(lines,i+1,"ΤΕΛΟΣ_ΑΝ"),els=FindFlowElse(lines,i+1,end);var cond=t[3..];if(cond.EndsWith(" ΤΟΤΕ",StringComparison.OrdinalIgnoreCase))cond=cond[..^5].Trim();AddArrow(cx,y-34,cx,y);AddFlowNode(i+1,cond,cx,y,w,h,"decision");double by=y+115,lx=cx-210,rx=cx+210;AddBranch(cx-150,y+h/2,lx,by,"ΑΛΗΘΗΣ");AddBranch(cx+150,y+h/2,rx,by,"ΨΕΥΔΗΣ");var ly=DrawFlowRange(lines,i+1,els>=0?els:end,lx,by);var ry=DrawFlowRange(lines,els>=0?els+1:end, end,rx,by);y=Math.Max(ly,ry)+45;AddArrow(lx,ly-25,cx,y);AddArrow(rx,ry-25,cx,y);i=end;continue;}if(t=="ΑΛΛΙΩΣ"||t=="ΤΕΛΟΣ_ΑΝ")continue;var display=FlowDisplay(t);var kind=FlowKind(t);AddArrow(cx,y-34,cx,y);AddFlowNode(i+1,display,cx,y,w,h,kind);y+=98;}
  AddArrow(cx,y-34,cx,y);AddFlowNode(-1,"ΤΕΛΟΣ",cx,y,w,h,"start");FlowCanvas.Height=Math.Max(900,y+120);FlowCanvas.Width=760;
 }
 bool SkipFlow(string t)=>string.IsNullOrWhiteSpace(t)||t.StartsWith("!")||t.StartsWith("ΠΡΟΓΡΑΜΜΑ",StringComparison.OrdinalIgnoreCase)||t=="ΜΕΤΑΒΛΗΤΕΣ"||t.StartsWith("ΑΚΕΡΑΙΕΣ:")||t.StartsWith("ΠΡΑΓΜΑΤΙΚΕΣ:")||t.StartsWith("ΧΑΡΑΚΤΗΡΕΣ:")||t.StartsWith("ΛΟΓΙΚΕΣ:")||t=="ΑΡΧΗ"||t.StartsWith("ΤΕΛΟΣ_ΠΡΟΓΡΑΜΜΑΤΟΣ")||t=="ΤΕΛΟΣ"||t=="ΤΕΛΟΣ_ΕΠΑΝΑΛΗΨΗΣ";
 int FindFlowEnd(string[] a,int from,string token){for(int i=from,depth=0;i<a.Length;i++){var t=a[i].Trim();if(t.StartsWith("ΑΝ "))depth++;if(t==token){if(depth==0)return i;depth--;}}return a.Length-1;}
 int FindFlowElse(string[] a,int from,int end){for(int i=from,depth=0;i<end;i++){var t=a[i].Trim();if(t.StartsWith("ΑΝ "))depth++;else if(t=="ΤΕΛΟΣ_ΑΝ")depth--;else if(t=="ΑΛΛΙΩΣ"&&depth==0)return i;}return -1;}
 double DrawFlowRange(string[] lines,int from,int to,double x,double y){const double w=280,h=58;var start=y;for(int i=from;i<to;i++){var t=lines[i].Trim();if(SkipFlow(t)||t=="ΑΛΛΙΩΣ"||t=="ΤΕΛΟΣ_ΑΝ")continue;if(y>start)AddArrow(x,y-32,x,y);AddFlowNode(i+1,FlowDisplay(t),x,y,w,h,FlowKind(t));y+=88;}if(y==start){AddFlowNode(0,"—",x,y,w,h,"process");y+=88;}return y;}
 string FlowDisplay(string t){if(t.StartsWith("ΑΝ ")&&t.EndsWith(" ΤΟΤΕ",StringComparison.OrdinalIgnoreCase))return t[3..^5].Trim();if(t.StartsWith("ΟΣΟ ")&&t.EndsWith(" ΕΠΑΝΑΛΑΒΕ",StringComparison.OrdinalIgnoreCase))return t[4..^10].Trim();if(t.StartsWith("ΜΕΧΡΙΣ_ΟΤΟΥ "))return t[12..].Trim();return t;}
 string FlowKind(string t)=>t.StartsWith("ΑΝ ")||t.StartsWith("ΟΣΟ ")||t.StartsWith("ΓΙΑ ")||t.StartsWith("ΜΕΧΡΙΣ_ΟΤΟΥ ")?"decision":t.StartsWith("ΔΙΑΒΑΣΕ ")||t.StartsWith("ΓΡΑΨΕ ")||t.StartsWith("ΕΜΦΑΝΙΣΕ ")?"io":"process";
 void AddBranch(double x1,double y1,double x2,double y2,string caption){AddArrow(x1,y1,x2,y2);var l=new TextBlock{Text=caption,Foreground=B(_dark?"#FBBF24":"#92400E"),FontWeight=FontWeights.Bold,FontSize=11};Canvas.SetLeft(l,(x1+x2)/2-25);Canvas.SetTop(l,(y1+y2)/2-20);FlowCanvas.Children.Add(l);}
 void AddArrow(double x1,double y1,double x2,double y2){var stroke=B(_dark?"#64748B":"#64748B");var line=new Line{X1=x1,Y1=y1,X2=x2,Y2=y2,Stroke=stroke,StrokeThickness=2};FlowCanvas.Children.Add(line);var head=new Polygon{Points=new PointCollection{new(x2-6,y2-8),new(x2+6,y2-8),new(x2,y2)},Fill=stroke};FlowCanvas.Children.Add(head);}
 void AddFlowNode(int line,string text,double cx,double y,double w,double h,string kind){Shape shape;if(kind=="decision")shape=new Polygon{Points=new PointCollection{new(cx,y),new(cx+w/2,y+h/2),new(cx,y+h),new(cx-w/2,y+h/2)}};else if(kind=="io"){var skew=22;shape=new Polygon{Points=new PointCollection{new(cx-w/2+skew,y),new(cx+w/2,y),new(cx+w/2-skew,y+h),new(cx-w/2,y+h)}};}else{shape=new Rectangle{RadiusX=kind=="start"?22:8,RadiusY=kind=="start"?22:8,Width=w,Height=h};Canvas.SetLeft(shape,cx-w/2);Canvas.SetTop(shape,y);}shape.Fill=B(_dark?"#18243A":"#FFFFFF");shape.Stroke=B(kind=="decision"?"#F59E0B":kind=="io"?"#06B6D4":"#6366F1");shape.StrokeThickness=2;FlowCanvas.Children.Add(shape);var label=new TextBlock{Text=text,Foreground=B(_dark?"#E8EEF8":"#172033"),FontFamily=new FontFamily("Cascadia Mono,Consolas"),FontSize=12,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap,TextAlignment=TextAlignment.Center,Width=w-46,Height=h,IsHitTestVisible=false};label.Padding=new Thickness(4,(h-24)/2,4,0);Canvas.SetLeft(label,cx-w/2+23);Canvas.SetTop(label,y);FlowCanvas.Children.Add(label);if(line>0)_flowShapes[line]=shape;}
 void HighlightFlowLine(int line){foreach(var s in _flowShapes.Values)s.StrokeThickness=2;if(_flowShapes.TryGetValue(line,out var active)){active.Stroke=B("#22C55E");active.StrokeThickness=5;}}
 void PrepareTrace(){var r=_interpreter.Run(CodeText(),PromptInput);_stepIndex=-1;ConsoleBox.Text="Βηματική εκτέλεση έτοιμη — πάτησε ξανά Βήμα (F10).";TraceGrid.ItemsSource=null;VariablesGrid.ItemsSource=null;StatusText.Text=$"Έτοιμα {_interpreter.Trace.Count} βήματα";}
 void Step_Click(object s,RoutedEventArgs e){try{if(_stepIndex<0){PrepareTrace();_stepIndex=0;}else _stepIndex++;if(_interpreter.Trace.Count==0){StatusText.Text="Δεν υπάρχουν εκτελέσιμες εντολές";return;}if(_stepIndex>=_interpreter.Trace.Count){StatusText.Text="Τέλος βηματικής εκτέλεσης";StatusDot.Fill=B("#22C55E");_stepIndex=-1;return;}var step=_interpreter.Trace[_stepIndex];Editor.ScrollToLine(step.Line);var dl=Editor.Document.GetLineByNumber(step.Line);Editor.Select(dl.Offset,dl.Length);VariablesGrid.ItemsSource=step.Variables.Select(v=>new{Name=v.Key,Value=v.Value}).ToList();ConsoleBox.Text=string.IsNullOrWhiteSpace(step.Output)?$"Γραμμή {step.Line}: {step.Statement}":step.Output;TraceGrid.ItemsSource=_interpreter.Trace.Take(_stepIndex+1).Select((t,n)=>new{Βήμα=n+1,Γραμμή=t.Line,Εντολή=t.Statement,Τιμές=string.Join(", ",t.Variables.Where(v=>v.Value!=null).Select(v=>$"{v.Key}={v.Value}"))}).ToList();HighlightFlowLine(step.Line);StatusText.Text=$"Βήμα {_stepIndex+1}/{_interpreter.Trace.Count} — γραμμή {step.Line}";StatusDot.Fill=B("#F59E0B");}catch(Exception ex){ConsoleBox.Text="Σφάλμα: "+ex.Message;StatusText.Text="Σφάλμα βηματικής εκτέλεσης";StatusDot.Fill=B("#EF4444");_stepIndex=-1;}}
 void FormatCode(){
  var lines=CodeText().Replace("\r","").Split('\n');var result=new List<string>();int indent=0;bool vars=false;
  foreach(var raw in lines){var t=raw.Trim();if(t.Length==0){result.Add("");continue;}var u=t.ToUpperInvariant();
   if(u=="ΑΡΧΗ"){vars=false;indent=0;result.Add(t);indent=1;continue;}
   if(u=="ΜΕΤΑΒΛΗΤΕΣ"){vars=true;indent=0;result.Add(t);continue;}
   if(u.StartsWith("ΤΕΛΟΣ_ΠΡΟΓΡΑΜΜΑΤΟΣ")||u=="ΤΕΛΟΣ"){indent=0;result.Add(t);continue;}
   bool close=u=="ΑΛΛΙΩΣ"||u=="ΤΕΛΟΣ_ΑΝ"||u=="ΤΕΛΟΣ_ΕΠΑΝΑΛΗΨΗΣ"||u.StartsWith("ΜΕΧΡΙΣ_ΟΤΟΥ ");
   if(close)indent=Math.Max(1,indent-1);
   var level=vars&&u!="ΜΕΤΑΒΛΗΤΕΣ"?1:indent;result.Add(new string(' ',Math.Max(0,level)*2)+t);
   bool open=(u.StartsWith("ΑΝ ")&&u.EndsWith(" ΤΟΤΕ"))||(u.StartsWith("ΟΣΟ ")&&u.EndsWith(" ΕΠΑΝΑΛΑΒΕ"))||u.StartsWith("ΓΙΑ ")||u=="ΑΡΧΗ_ΕΠΑΝΑΛΗΨΗΣ"||u=="ΑΛΛΙΩΣ";
   if(open)indent++;
  }
  var caret=Editor.CaretOffset;SetCode(string.Join(Environment.NewLine,result));Editor.CaretOffset=Math.Min(caret,Editor.Text.Length);
 }
 void Run_Click(object s,RoutedEventArgs e){try{FormatCode();var r=_interpreter.Run(CodeText(),PromptInput);ConsoleBox.Text=string.IsNullOrWhiteSpace(r)?"✓ Η εκτέλεση ολοκληρώθηκε.":r;VariablesGrid.ItemsSource=_interpreter.Variables.Select(v=>new{Name=v.Key,Value=v.Value}).ToList();StatusText.Text="Η εκτέλεση ολοκληρώθηκε";StatusDot.Fill=B("#22C55E");}catch(Exception ex){ConsoleBox.Text="Σφάλμα: "+ex.Message;StatusText.Text="Σφάλμα εκτέλεσης";StatusDot.Fill=B("#EF4444");}}
 string PromptInput(string name){var w=new Window{Title="ΔΙΑΒΑΣΕ — "+name,Width=380,Height=160,WindowStartupLocation=WindowStartupLocation.CenterOwner,Owner=this,ResizeMode=ResizeMode.NoResize};var g=new Grid{Margin=new Thickness(18)};g.RowDefinitions.Add(new RowDefinition());g.RowDefinitions.Add(new RowDefinition());g.RowDefinitions.Add(new RowDefinition());var label=new TextBlock{Text=$"Δώσε τιμή για τη μεταβλητή {name}:",Margin=new Thickness(0,0,0,8)};var box=new TextBox{Height=28};var ok=new Button{Content="OK",Width=80,Height=28,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,8,0,0),IsDefault=true};Grid.SetRow(box,1);Grid.SetRow(ok,2);g.Children.Add(label);g.Children.Add(box);g.Children.Add(ok);w.Content=g;ok.Click+=(_,__)=>w.DialogResult=true;w.Loaded+=(_,__)=>box.Focus();if(w.ShowDialog()!=true)throw new InvalidOperationException("Η εισαγωγή ακυρώθηκε.");return box.Text;}
 void Clear_Click(object s,RoutedEventArgs e){_stepIndex=-1;ConsoleBox.Clear();VariablesGrid.ItemsSource=null;TraceGrid.ItemsSource=null;StatusText.Text="Έτοιμο";StatusDot.Fill=B("#22C55E");}
 void New_Click(object s,RoutedEventArgs e){SetCode("");_file=null;FileText.Text="χωρίς τίτλο.glossa";StatusText.Text="Νέο αρχείο";}
 void Open_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Αρχεία Algora (*.glossa;*.algo)|*.glossa;*.algo|Αρχεία κειμένου (*.txt)|*.txt|Όλα τα αρχεία|*.*"};if(d.ShowDialog()==true){SetCode(File.ReadAllText(d.FileName));_file=d.FileName;FileText.Text=System.IO.Path.GetFileName(_file);StatusText.Text="Το αρχείο άνοιξε";}}
 void Save_Click(object s,RoutedEventArgs e){if(_file==null){var d=new SaveFileDialog{Filter="Αρχείο ΓΛΩΣΣΑΣ (*.glossa)|*.glossa|Αλγόριθμος (*.algo)|*.algo|Αρχείο κειμένου (*.txt)|*.txt",DefaultExt=".glossa"};if(d.ShowDialog()!=true)return;_file=d.FileName;}File.WriteAllText(_file,CodeText());FileText.Text=System.IO.Path.GetFileName(_file);StatusText.Text="Αποθηκεύτηκε";}
 void Editor_TextChanged(object? s,EventArgs e){StatusText.Text="Επεξεργασία";if(IsLoaded&&WorkTabs.SelectedIndex==1)BuildFlowchart();}
 void Editor_KeyDown(object s,KeyEventArgs e){if(e.Key==Key.F5){Run_Click(s,new RoutedEventArgs());e.Handled=true;return;}if(e.Key==Key.F10){Step_Click(s,new RoutedEventArgs());e.Handled=true;return;}if(e.Key==Key.Enter){var line=Editor.Document.GetLineByOffset(Editor.CaretOffset);var text=Editor.Document.GetText(line.Offset,Math.Max(0,Editor.CaretOffset-line.Offset));var indent=new string(text.TakeWhile(char.IsWhiteSpace).ToArray());var trimmed=text.TrimStart();if(trimmed.StartsWith("ΑΝ ",StringComparison.OrdinalIgnoreCase)||trimmed.StartsWith("ΓΙΑ ",StringComparison.OrdinalIgnoreCase)||trimmed.StartsWith("ΟΣΟ ",StringComparison.OrdinalIgnoreCase)||trimmed.Equals("ΑΡΧΗ_ΕΠΑΝΑΛΗΨΗΣ",StringComparison.OrdinalIgnoreCase))indent+="  ";Editor.Document.Insert(Editor.CaretOffset,Environment.NewLine+indent);Editor.CaretOffset+=Environment.NewLine.Length+indent.Length;e.Handled=true;}}
 void Window_KeyDown(object s,KeyEventArgs e){if(e.Key!=Key.F11)return;if(WindowStyle!=WindowStyle.None){_oldStyle=WindowStyle;_oldState=WindowState;WindowStyle=WindowStyle.None;WindowState=WindowState.Maximized;}else{WindowStyle=_oldStyle;WindowState=_oldState;}}
}