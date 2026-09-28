using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Algora.Core;
namespace Algora;
public partial class MainWindow : Window
{
 readonly Interpreter _interpreter=new(); bool _dark,_highlighting; string? _file; WindowStyle _oldStyle; WindowState _oldState;
 static readonly string[] Keywords={"ΠΡΟΓΡΑΜΜΑ","ΜΕΤΑΒΛΗΤΕΣ","ΑΚΕΡΑΙΕΣ","ΠΡΑΓΜΑΤΙΚΕΣ","ΧΑΡΑΚΤΗΡΕΣ","ΛΟΓΙΚΕΣ","ΑΡΧΗ","ΤΕΛΟΣ_ΠΡΟΓΡΑΜΜΑΤΟΣ","ΔΙΑΒΑΣΕ","ΓΡΑΨΕ","ΕΜΦΑΝΙΣΕ","ΑΝ","ΤΟΤΕ","ΑΛΛΙΩΣ","ΤΕΛΟΣ_ΑΝ","ΚΑΙ","Ή","Η","ΟΧΙ","ΑΛΗΘΗΣ","ΨΕΥΔΗΣ","ΟΣΟ","ΕΠΑΝΑΛΑΒΕ","ΓΙΑ","ΑΠΟ","ΜΕΧΡΙ","ΜΕ_ΒΗΜΑ","ΤΕΛΟΣ_ΕΠΑΝΑΛΗΨΗΣ","ΑΡΧΗ_ΕΠΑΝΑΛΗΨΗΣ","ΜΕΧΡΙΣ_ΟΤΟΥ"};
 public MainWindow(){InitializeComponent();UpdateLines();ApplyTheme();Highlight();}
 static Brush B(string h)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(h));
 string CodeText()=>new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd).Text.TrimEnd('\r','\n');
 void SetCode(string s){Editor.Document.Blocks.Clear();Editor.Document.Blocks.Add(new Paragraph(new Run(s)){Margin=new Thickness(0)});Highlight();}
 void ApplyTheme(){
  var bg=B(_dark?"#0B1220":"#F3F6FB");var card=B(_dark?"#151F32":"#FFFFFF");var header=B(_dark?"#1B2940":"#F8FAFC");var text=B(_dark?"#E8EEF8":"#172033");var muted=B(_dark?"#93A4BC":"#64748B");var border=B(_dark?"#263650":"#E2E8F0");var gutter=B(_dark?"#111A2A":"#F7F9FC");
  Background=Root.Background=bg;TopBar.Background=Toolbar.Background=card;TopBar.BorderBrush=Toolbar.BorderBrush=border;EditorCard.Background=OutputCard.Background=card;EditorCard.BorderBrush=OutputCard.BorderBrush=border;EditorHeader.Background=ConsoleHeader.Background=header;
  TitleText.Foreground=EditorHeaderText.Foreground=ConsoleHeaderText.Foreground=VariablesTitle.Foreground=text;SubtitleText.Foreground=FileText.Foreground=muted;Editor.Background=ConsoleBox.Background=VariablesPanel.Background=VariablesGrid.Background=card;Editor.Foreground=ConsoleBox.Foreground=VariablesGrid.Foreground=text;VariablesGrid.RowBackground=card;VariablesGrid.AlternatingRowBackground=gutter;VariablesGrid.HorizontalGridLinesBrush=border;VariablesGrid.VerticalGridLinesBrush=border;LineNumbers.Background=gutter;LineNumbers.Foreground=muted;VariablesPanel.BorderBrush=border;
  StatusBar.Background=B(_dark?"#101A2B":"#172033");StatusText.Foreground=VersionText.Foreground=B("#CBD5E1");StatusDot.Fill=B("#22C55E");ThemeButton.Background=B(_dark?"#253552":"#EEF2FF");ThemeButton.Foreground=B(_dark?"#DCE6FF":"#3730A3");RunButton.Background=B("#5B4FF7");RunButton.Foreground=Brushes.White;Highlight();
 }
 void Highlight(){
  if(_highlighting||Editor==null)return;_highlighting=true;
  try{
   var caretOffset=new TextRange(Editor.Document.ContentStart,Editor.CaretPosition).Text.Length;
   var all=new TextRange(Editor.Document.ContentStart,Editor.Document.ContentEnd);all.ApplyPropertyValue(TextElement.ForegroundProperty,B(_dark?"#E8EEF8":"#172033"));all.ApplyPropertyValue(TextElement.FontWeightProperty,FontWeights.Normal);
   var text=all.Text;var pattern=@"\b("+string.Join("|",Keywords.Select(Regex.Escape))+@")\b";
   foreach(Match m in Regex.Matches(text,pattern,RegexOptions.IgnoreCase)){var start=AtOffset(m.Index);var end=AtOffset(m.Index+m.Length);if(start!=null&&end!=null){var r=new TextRange(start,end);r.ApplyPropertyValue(TextElement.ForegroundProperty,B(_dark?"#8AB4FF":"#4F46E5"));r.ApplyPropertyValue(TextElement.FontWeightProperty,FontWeights.SemiBold);}}
   Editor.CaretPosition=AtOffset(Math.Min(caretOffset,text.Length))??Editor.Document.ContentEnd;
  }finally{_highlighting=false;}
 }
 TextPointer? AtOffset(int offset){var p=Editor.Document.ContentStart;int n=0;while(p!=null){if(p.GetPointerContext(LogicalDirection.Forward)==TextPointerContext.Text){var run=p.GetTextInRun(LogicalDirection.Forward);if(n+run.Length>=offset)return p.GetPositionAtOffset(offset-n);}else if(p.GetPointerContext(LogicalDirection.Forward)==TextPointerContext.ElementEnd&&p.Parent is Paragraph){if(n==offset)return p;n++;}p=p.GetNextContextPosition(LogicalDirection.Forward);}return Editor.Document.ContentEnd;}
 void Theme_Click(object s,RoutedEventArgs e){_dark=!_dark;ApplyTheme();}
 void Run_Click(object s,RoutedEventArgs e){try{var r=_interpreter.Run(CodeText(),PromptInput);ConsoleBox.Text=string.IsNullOrWhiteSpace(r)?"✓ Η εκτέλεση ολοκληρώθηκε.":r;VariablesGrid.ItemsSource=_interpreter.Variables.Select(v=>new{Name=v.Key,Value=v.Value}).ToList();StatusText.Text="Η εκτέλεση ολοκληρώθηκε";StatusDot.Fill=B("#22C55E");}catch(Exception ex){ConsoleBox.Text="Σφάλμα: "+ex.Message;StatusText.Text="Σφάλμα εκτέλεσης";StatusDot.Fill=B("#EF4444");}}
 string PromptInput(string name){var w=new Window{Title="ΔΙΑΒΑΣΕ — "+name,Width=380,Height=160,WindowStartupLocation=WindowStartupLocation.CenterOwner,Owner=this,ResizeMode=ResizeMode.NoResize};var g=new Grid{Margin=new Thickness(18)};g.RowDefinitions.Add(new RowDefinition());g.RowDefinitions.Add(new RowDefinition());g.RowDefinitions.Add(new RowDefinition());var label=new TextBlock{Text=$"Δώσε τιμή για τη μεταβλητή {name}:",Margin=new Thickness(0,0,0,8)};var box=new TextBox{Height=28};var ok=new Button{Content="OK",Width=80,Height=28,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,8,0,0),IsDefault=true};Grid.SetRow(box,1);Grid.SetRow(ok,2);g.Children.Add(label);g.Children.Add(box);g.Children.Add(ok);w.Content=g;ok.Click+=(_,__)=>w.DialogResult=true;w.Loaded+=(_,__)=>box.Focus();if(w.ShowDialog()!=true)throw new InvalidOperationException("Η εισαγωγή ακυρώθηκε.");return box.Text;}
 void Clear_Click(object s,RoutedEventArgs e){ConsoleBox.Clear();VariablesGrid.ItemsSource=null;StatusText.Text="Έτοιμο";StatusDot.Fill=B("#22C55E");}
 void New_Click(object s,RoutedEventArgs e){SetCode("");_file=null;FileText.Text="χωρίς τίτλο.glossa";StatusText.Text="Νέο αρχείο";}
 void Open_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Αρχεία Algora (*.glossa;*.algo)|*.glossa;*.algo|Αρχεία κειμένου (*.txt)|*.txt|Όλα τα αρχεία|*.*"};if(d.ShowDialog()==true){SetCode(File.ReadAllText(d.FileName));_file=d.FileName;FileText.Text=Path.GetFileName(_file);StatusText.Text="Το αρχείο άνοιξε";}}
 void Save_Click(object s,RoutedEventArgs e){if(_file==null){var d=new SaveFileDialog{Filter="Αρχείο ΓΛΩΣΣΑΣ (*.glossa)|*.glossa|Αλγόριθμος (*.algo)|*.algo|Αρχείο κειμένου (*.txt)|*.txt",DefaultExt=".glossa"};if(d.ShowDialog()!=true)return;_file=d.FileName;}File.WriteAllText(_file,CodeText());FileText.Text=Path.GetFileName(_file);StatusText.Text="Αποθηκεύτηκε";}
 void Editor_TextChanged(object s,TextChangedEventArgs e){UpdateLines();Highlight();}
 void UpdateLines(){if(LineNumbers==null||Editor==null)return;var n=Math.Max(1,CodeText().Split('\n').Length);LineNumbers.Text=string.Join(Environment.NewLine,Enumerable.Range(1,n));}
 void Editor_KeyDown(object s,KeyEventArgs e){if(e.Key==Key.F5){Run_Click(s,new RoutedEventArgs());e.Handled=true;}}
 void Window_KeyDown(object s,KeyEventArgs e){if(e.Key!=Key.F11)return;if(WindowStyle!=WindowStyle.None){_oldStyle=WindowStyle;_oldState=WindowState;WindowStyle=WindowStyle.None;WindowState=WindowState.Maximized;}else{WindowStyle=_oldStyle;WindowState=_oldState;}}
}