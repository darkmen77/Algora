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
  var bg=B(_dark?"#0B1220":"#F3F6FB");var card=B(_dark?"#151F32":"#FFFFFF");var header=B(_dark?"#1B2940":"#F8FAFC");var text=B(_dark?"#E8EEF8":"#172033");var muted=B(_dark?"#93A4BC":"#64748B");var border=B(_dark?"#263650":"#E2E8F0");var gutter=B(_dark?"#111A2A":"#F7F9FC");
  Background=Root.Background=bg;TopBar.Background=Toolbar.Background=card;TopBar.BorderBrush=Toolbar.BorderBrush=border;EditorCard.Background=OutputCard.Background=card;EditorCard.BorderBrush=OutputCard.BorderBrush=border;EditorHeader.Background=ConsoleHeader.Background=header;TitleText.Foreground=EditorHeaderText.Foreground=ConsoleHeaderText.Foreground=VariablesTitle.Foreground=text;SubtitleText.Foreground=FileText.Foreground=muted;
  Editor.Background=ConsoleBox.Background=VariablesPanel.Background=VariablesGrid.Background=card;Editor.Foreground=ConsoleBox.Foreground=VariablesGrid.Foreground=text;Editor.LineNumbersForeground=muted;Editor.TextArea.TextView.CurrentLineBackground=gutter;Editor.TextArea.TextView.CurrentLineBorder=new Pen(border,1);
  TraceTitle.Foreground=text;TracePanel.BorderBrush=border;TraceGrid.Background=card;TraceGrid.Foreground=text;TraceGrid.RowBackground=card;TraceGrid.AlternatingRowBackground=gutter;TraceGrid.HorizontalGridLinesBrush=border;TraceGrid.VerticalGridLinesBrush=border;VariablesGrid.RowBackground=card;VariablesGrid.AlternatingRowBackground=gutter;VariablesGrid.HorizontalGridLinesBrush=border;VariablesGrid.VerticalGridLinesBrush=border;VariablesPanel.BorderBrush=border;StatusBar.Background=B(_dark?"#101A2B":"#172033");StatusText.Foreground=VersionText.Foreground=B("#CBD5E1");StatusDot.Fill=B("#22C55E");ThemeButton.Background=B(_dark?"#253552":"#EEF2FF");ThemeButton.Foreground=B(_dark?"#DCE6FF":"#3730A3");RunButton.Background=B("#5B4FF7");RunButton.Foreground=Brushes.White;InstallHighlighting();
 }
 void Theme_Click(object s,RoutedEventArgs e){_dark=!_dark;ApplyTheme();if(WorkTabs.SelectedIndex==1)BuildFlowchart();}
 void Flow_Click(object s,RoutedEventArgs e){BuildFlowchart();WorkTabs.SelectedIndex=1;}
 void WorkTabs_SelectionChanged(object s,SelectionChangedEventArgs e){if(IsLoaded&&WorkTabs.SelectedIndex==1)BuildFlowchart();}
 void BuildFlowchart(){
  FlowCanvas.Children.Clear();_flowShapes.Clear();var lines=CodeText().Replace("\r","").Split('\n');double y=28;const double cx=380,w=360,h=58;
  AddFlowNode(0,"ΑΡΧΗ",cx,y,w,h,"start");y+=92;
  for(int i=0;i<lines.Length;i++){var t=lines[i].Trim();if(string.IsNullOrWhiteSpace(t)||t.StartsWith("!")||t.StartsWith("ΠΡΟΓΡΑΜΜΑ",StringComparison.OrdinalIgnoreCase)||t=="ΜΕΤΑΒΛΗΤΕΣ"||t.StartsWith("ΑΚΕΡΑΙΕΣ:")||t.StartsWith("ΠΡΑΓΜΑΤΙΚΕΣ:")||t.StartsWith("ΧΑΡΑΚΤΗΡΕΣ:")||t.StartsWith("ΛΟΓΙΚΕΣ:")||t=="ΑΡΧΗ"||t=="ΤΕΛΟΣ_ΑΝ"||t=="ΤΕΛΟΣ_ΕΠΑΝΑΛΗΨΗΣ"||t=="ΑΛΛΙΩΣ")continue;if(t.StartsWith("ΤΕΛΟΣ_ΠΡΟΓΡΑΜΜΑΤΟΣ")||t=="ΤΕΛΟΣ")continue;var kind=t.StartsWith("ΑΝ ")||t.StartsWith("ΟΣΟ ")||t.StartsWith("ΓΙΑ ")||t.StartsWith("ΜΕΧΡΙΣ_ΟΤΟΥ ")?"decision":t.StartsWith("ΔΙΑΒΑΣΕ ")||t.StartsWith("ΓΡΑΨΕ ")||t.StartsWith("ΕΜΦΑΝΙΣΕ ")?"io":"process";AddArrow(cx,y-30,cx,y);AddFlowNode(i+1,t,cx,y,w,h,kind);y+=96;}
  AddArrow(cx,y-30,cx,y);AddFlowNode(-1,"ΤΕΛΟΣ",cx,y,w,h,"start");FlowCanvas.Height=Math.Max(900,y+100);
 }
 void AddArrow(double x1,double y1,double x2,double y2){var line=new Line{X1=x1,Y1=y1,X2=x2,Y2=y2,Stroke=B(_dark?"#64748B":"#94A3B8"),StrokeThickness=2};FlowCanvas.Children.Add(line);var head=new Polygon{Points=new PointCollection{new(x2-6,y2-8),new(x2+6,y2-8),new(x2,y2)},Fill=line.Stroke};FlowCanvas.Children.Add(head);}
 void AddFlowNode(int line,string text,double cx,double y,double w,double h,string kind){Shape shape;if(kind=="decision"){var p=new Polygon{Points=new PointCollection{new(cx,y),new(cx+w/2,y+h/2),new(cx,y+h),new(cx-w/2,y+h/2)}};shape=p;}else if(kind=="io"){var skew=22;shape=new Polygon{Points=new PointCollection{new(cx-w/2+skew,y),new(cx+w/2,y),new(cx+w/2-skew,y+h),new(cx-w/2,y+h)}};}else shape=new Rectangle{RadiusX=kind=="start"?22:8,RadiusY=kind=="start"?22:8,Width=w,Height=h};shape.Fill=B(_dark?"#18243A":"#FFFFFF");shape.Stroke=B(kind=="decision"?"#F59E0B":kind=="io"?"#06B6D4":"#6366F1");shape.StrokeThickness=2;Canvas.SetLeft(shape,cx-w/2);Canvas.SetTop(shape,y);FlowCanvas.Children.Add(shape);var label=new TextBlock{Text=text,Foreground=B(_dark?"#E8EEF8":"#172033"),FontFamily=new FontFamily("Cascadia Mono,Consolas"),FontSize=12,TextWrapping=TextWrapping.Wrap,TextAlignment=TextAlignment.Center,Width=w-38};Canvas.SetLeft(label,cx-w/2+19);Canvas.SetTop(label,y+18);FlowCanvas.Children.Add(label);if(line>0)_flowShapes[line]=shape;}
 void HighlightFlowLine(int line){foreach(var s in _flowShapes.Values)s.StrokeThickness=2;if(_flowShapes.TryGetValue(line,out var active)){active.Stroke=B("#22C55E");active.StrokeThickness=5;}}
 void PrepareTrace(){var r=_interpreter.Run(CodeText(),PromptInput);_stepIndex=-1;ConsoleBox.Text="Βηματική εκτέλεση έτοιμη — πάτησε ξανά Βήμα (F10).";TraceGrid.ItemsSource=null;VariablesGrid.ItemsSource=null;StatusText.Text=$"Έτοιμα {_interpreter.Trace.Count} βήματα";}
 void Step_Click(object s,RoutedEventArgs e){try{if(_stepIndex<0){PrepareTrace();_stepIndex=0;}else _stepIndex++;if(_interpreter.Trace.Count==0){StatusText.Text="Δεν υπάρχουν εκτελέσιμες εντολές";return;}if(_stepIndex>=_interpreter.Trace.Count){StatusText.Text="Τέλος βηματικής εκτέλεσης";StatusDot.Fill=B("#22C55E");_stepIndex=-1;return;}var step=_interpreter.Trace[_stepIndex];Editor.ScrollToLine(step.Line);var dl=Editor.Document.GetLineByNumber(step.Line);Editor.Select(dl.Offset,dl.Length);VariablesGrid.ItemsSource=step.Variables.Select(v=>new{Name=v.Key,Value=v.Value}).ToList();ConsoleBox.Text=string.IsNullOrWhiteSpace(step.Output)?$"Γραμμή {step.Line}: {step.Statement}":step.Output;TraceGrid.ItemsSource=_interpreter.Trace.Take(_stepIndex+1).Select((t,n)=>new{Βήμα=n+1,Γραμμή=t.Line,Εντολή=t.Statement,Τιμές=string.Join(", ",t.Variables.Where(v=>v.Value!=null).Select(v=>$"{v.Key}={v.Value}"))}).ToList();HighlightFlowLine(step.Line);StatusText.Text=$"Βήμα {_stepIndex+1}/{_interpreter.Trace.Count} — γραμμή {step.Line}";StatusDot.Fill=B("#F59E0B");}catch(Exception ex){ConsoleBox.Text="Σφάλμα: "+ex.Message;StatusText.Text="Σφάλμα βηματικής εκτέλεσης";StatusDot.Fill=B("#EF4444");_stepIndex=-1;}}
 void Run_Click(object s,RoutedEventArgs e){try{var r=_interpreter.Run(CodeText(),PromptInput);ConsoleBox.Text=string.IsNullOrWhiteSpace(r)?"✓ Η εκτέλεση ολοκληρώθηκε.":r;VariablesGrid.ItemsSource=_interpreter.Variables.Select(v=>new{Name=v.Key,Value=v.Value}).ToList();StatusText.Text="Η εκτέλεση ολοκληρώθηκε";StatusDot.Fill=B("#22C55E");}catch(Exception ex){ConsoleBox.Text="Σφάλμα: "+ex.Message;StatusText.Text="Σφάλμα εκτέλεσης";StatusDot.Fill=B("#EF4444");}}
 string PromptInput(string name){var w=new Window{Title="ΔΙΑΒΑΣΕ — "+name,Width=380,Height=160,WindowStartupLocation=WindowStartupLocation.CenterOwner,Owner=this,ResizeMode=ResizeMode.NoResize};var g=new Grid{Margin=new Thickness(18)};g.RowDefinitions.Add(new RowDefinition());g.RowDefinitions.Add(new RowDefinition());g.RowDefinitions.Add(new RowDefinition());var label=new TextBlock{Text=$"Δώσε τιμή για τη μεταβλητή {name}:",Margin=new Thickness(0,0,0,8)};var box=new TextBox{Height=28};var ok=new Button{Content="OK",Width=80,Height=28,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,8,0,0),IsDefault=true};Grid.SetRow(box,1);Grid.SetRow(ok,2);g.Children.Add(label);g.Children.Add(box);g.Children.Add(ok);w.Content=g;ok.Click+=(_,__)=>w.DialogResult=true;w.Loaded+=(_,__)=>box.Focus();if(w.ShowDialog()!=true)throw new InvalidOperationException("Η εισαγωγή ακυρώθηκε.");return box.Text;}
 void Clear_Click(object s,RoutedEventArgs e){_stepIndex=-1;ConsoleBox.Clear();VariablesGrid.ItemsSource=null;TraceGrid.ItemsSource=null;StatusText.Text="Έτοιμο";StatusDot.Fill=B("#22C55E");}
 void New_Click(object s,RoutedEventArgs e){SetCode("");_file=null;FileText.Text="χωρίς τίτλο.glossa";StatusText.Text="Νέο αρχείο";}
 void Open_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Αρχεία Algora (*.glossa;*.algo)|*.glossa;*.algo|Αρχεία κειμένου (*.txt)|*.txt|Όλα τα αρχεία|*.*"};if(d.ShowDialog()==true){SetCode(File.ReadAllText(d.FileName));_file=d.FileName;FileText.Text=Path.GetFileName(_file);StatusText.Text="Το αρχείο άνοιξε";}}
 void Save_Click(object s,RoutedEventArgs e){if(_file==null){var d=new SaveFileDialog{Filter="Αρχείο ΓΛΩΣΣΑΣ (*.glossa)|*.glossa|Αλγόριθμος (*.algo)|*.algo|Αρχείο κειμένου (*.txt)|*.txt",DefaultExt=".glossa"};if(d.ShowDialog()!=true)return;_file=d.FileName;}File.WriteAllText(_file,CodeText());FileText.Text=Path.GetFileName(_file);StatusText.Text="Αποθηκεύτηκε";}
 void Editor_TextChanged(object? s,EventArgs e){StatusText.Text="Επεξεργασία";if(IsLoaded&&WorkTabs.SelectedIndex==1)BuildFlowchart();}
 void Editor_KeyDown(object s,KeyEventArgs e){if(e.Key==Key.F5){Run_Click(s,new RoutedEventArgs());e.Handled=true;return;}if(e.Key==Key.F10){Step_Click(s,new RoutedEventArgs());e.Handled=true;return;}if(e.Key==Key.Enter){var line=Editor.Document.GetLineByOffset(Editor.CaretOffset);var text=Editor.Document.GetText(line.Offset,Math.Max(0,Editor.CaretOffset-line.Offset));var indent=new string(text.TakeWhile(char.IsWhiteSpace).ToArray());var trimmed=text.TrimStart();if(trimmed.StartsWith("ΑΝ ",StringComparison.OrdinalIgnoreCase)||trimmed.StartsWith("ΓΙΑ ",StringComparison.OrdinalIgnoreCase)||trimmed.StartsWith("ΟΣΟ ",StringComparison.OrdinalIgnoreCase)||trimmed.Equals("ΑΡΧΗ_ΕΠΑΝΑΛΗΨΗΣ",StringComparison.OrdinalIgnoreCase))indent+="  ";Editor.Document.Insert(Editor.CaretOffset,Environment.NewLine+indent);Editor.CaretOffset+=Environment.NewLine.Length+indent.Length;e.Handled=true;}}
 void Window_KeyDown(object s,KeyEventArgs e){if(e.Key!=Key.F11)return;if(WindowStyle!=WindowStyle.None){_oldStyle=WindowStyle;_oldState=WindowState;WindowStyle=WindowStyle.None;WindowState=WindowState.Maximized;}else{WindowStyle=_oldStyle;WindowState=_oldState;}}
}