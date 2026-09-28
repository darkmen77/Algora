using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using Algora.Core;
namespace Algora;
public partial class MainWindow : Window
{
 readonly Interpreter _interpreter=new(); bool _dark; string? _file; WindowStyle _oldStyle; WindowState _oldState;
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
  Editor.Background=ConsoleBox.Background=VariablesPanel.Background=VariablesGrid.Background=card;Editor.Foreground=ConsoleBox.Foreground=VariablesGrid.Foreground=text;Editor.LineNumbersForeground=muted;Editor.TextArea.TextView.CurrentLineBackground=gutter;Editor.TextArea.TextView.CurrentLineBorder=border;
  VariablesGrid.RowBackground=card;VariablesGrid.AlternatingRowBackground=gutter;VariablesGrid.HorizontalGridLinesBrush=border;VariablesGrid.VerticalGridLinesBrush=border;VariablesPanel.BorderBrush=border;StatusBar.Background=B(_dark?"#101A2B":"#172033");StatusText.Foreground=VersionText.Foreground=B("#CBD5E1");StatusDot.Fill=B("#22C55E");ThemeButton.Background=B(_dark?"#253552":"#EEF2FF");ThemeButton.Foreground=B(_dark?"#DCE6FF":"#3730A3");RunButton.Background=B("#5B4FF7");RunButton.Foreground=Brushes.White;InstallHighlighting();
 }
 void Theme_Click(object s,RoutedEventArgs e){_dark=!_dark;ApplyTheme();}
 void Run_Click(object s,RoutedEventArgs e){try{var r=_interpreter.Run(CodeText(),PromptInput);ConsoleBox.Text=string.IsNullOrWhiteSpace(r)?"✓ Η εκτέλεση ολοκληρώθηκε.":r;VariablesGrid.ItemsSource=_interpreter.Variables.Select(v=>new{Name=v.Key,Value=v.Value}).ToList();StatusText.Text="Η εκτέλεση ολοκληρώθηκε";StatusDot.Fill=B("#22C55E");}catch(Exception ex){ConsoleBox.Text="Σφάλμα: "+ex.Message;StatusText.Text="Σφάλμα εκτέλεσης";StatusDot.Fill=B("#EF4444");}}
 string PromptInput(string name){var w=new Window{Title="ΔΙΑΒΑΣΕ — "+name,Width=380,Height=160,WindowStartupLocation=WindowStartupLocation.CenterOwner,Owner=this,ResizeMode=ResizeMode.NoResize};var g=new Grid{Margin=new Thickness(18)};g.RowDefinitions.Add(new RowDefinition());g.RowDefinitions.Add(new RowDefinition());g.RowDefinitions.Add(new RowDefinition());var label=new TextBlock{Text=$"Δώσε τιμή για τη μεταβλητή {name}:",Margin=new Thickness(0,0,0,8)};var box=new TextBox{Height=28};var ok=new Button{Content="OK",Width=80,Height=28,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,8,0,0),IsDefault=true};Grid.SetRow(box,1);Grid.SetRow(ok,2);g.Children.Add(label);g.Children.Add(box);g.Children.Add(ok);w.Content=g;ok.Click+=(_,__)=>w.DialogResult=true;w.Loaded+=(_,__)=>box.Focus();if(w.ShowDialog()!=true)throw new InvalidOperationException("Η εισαγωγή ακυρώθηκε.");return box.Text;}
 void Clear_Click(object s,RoutedEventArgs e){ConsoleBox.Clear();VariablesGrid.ItemsSource=null;StatusText.Text="Έτοιμο";StatusDot.Fill=B("#22C55E");}
 void New_Click(object s,RoutedEventArgs e){SetCode("");_file=null;FileText.Text="χωρίς τίτλο.glossa";StatusText.Text="Νέο αρχείο";}
 void Open_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Αρχεία Algora (*.glossa;*.algo)|*.glossa;*.algo|Αρχεία κειμένου (*.txt)|*.txt|Όλα τα αρχεία|*.*"};if(d.ShowDialog()==true){SetCode(File.ReadAllText(d.FileName));_file=d.FileName;FileText.Text=Path.GetFileName(_file);StatusText.Text="Το αρχείο άνοιξε";}}
 void Save_Click(object s,RoutedEventArgs e){if(_file==null){var d=new SaveFileDialog{Filter="Αρχείο ΓΛΩΣΣΑΣ (*.glossa)|*.glossa|Αλγόριθμος (*.algo)|*.algo|Αρχείο κειμένου (*.txt)|*.txt",DefaultExt=".glossa"};if(d.ShowDialog()!=true)return;_file=d.FileName;}File.WriteAllText(_file,CodeText());FileText.Text=Path.GetFileName(_file);StatusText.Text="Αποθηκεύτηκε";}
 void Editor_TextChanged(object? s,EventArgs e){StatusText.Text="Επεξεργασία";}
 void Editor_KeyDown(object s,KeyEventArgs e){if(e.Key==Key.F5){Run_Click(s,new RoutedEventArgs());e.Handled=true;return;}if(e.Key==Key.Enter){var line=Editor.Document.GetLineByOffset(Editor.CaretOffset);var text=Editor.Document.GetText(line.Offset,Math.Max(0,Editor.CaretOffset-line.Offset));var indent=new string(text.TakeWhile(char.IsWhiteSpace).ToArray());var trimmed=text.TrimStart();if(trimmed.StartsWith("ΑΝ ",StringComparison.OrdinalIgnoreCase)||trimmed.StartsWith("ΓΙΑ ",StringComparison.OrdinalIgnoreCase)||trimmed.StartsWith("ΟΣΟ ",StringComparison.OrdinalIgnoreCase)||trimmed.Equals("ΑΡΧΗ_ΕΠΑΝΑΛΗΨΗΣ",StringComparison.OrdinalIgnoreCase))indent+="  ";Editor.Document.Insert(Editor.CaretOffset,Environment.NewLine+indent);Editor.CaretOffset+=Environment.NewLine.Length+indent.Length;e.Handled=true;}}
 void Window_KeyDown(object s,KeyEventArgs e){if(e.Key!=Key.F11)return;if(WindowStyle!=WindowStyle.None){_oldStyle=WindowStyle;_oldState=WindowState;WindowStyle=WindowStyle.None;WindowState=WindowState.Maximized;}else{WindowStyle=_oldStyle;WindowState=_oldState;}}
}