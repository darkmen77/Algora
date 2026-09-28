using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Algora.Core;
namespace Algora;
public partial class MainWindow : Window
{
 readonly Interpreter _interpreter=new(); bool _dark; string? _file; WindowStyle _oldStyle; WindowState _oldState;
 public MainWindow(){InitializeComponent();UpdateLines();ApplyTheme();}
 static Brush B(string h)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(h));
 void ApplyTheme(){
  var bg=B(_dark?"#0B1220":"#F3F6FB");
  var card=B(_dark?"#151F32":"#FFFFFF");
  var header=B(_dark?"#1B2940":"#F8FAFC");
  var text=B(_dark?"#E8EEF8":"#172033");
  var muted=B(_dark?"#93A4BC":"#64748B");
  var border=B(_dark?"#263650":"#E2E8F0");
  var gutter=B(_dark?"#111A2A":"#F7F9FC");
  Background=Root.Background=bg; TopBar.Background=Toolbar.Background=card; TopBar.BorderBrush=Toolbar.BorderBrush=border; EditorCard.Background=OutputCard.Background=card; EditorCard.BorderBrush=OutputCard.BorderBrush=border; EditorHeader.Background=ConsoleHeader.Background=header;
  TitleText.Foreground=EditorHeaderText.Foreground=ConsoleHeaderText.Foreground=VariablesTitle.Foreground=text; SubtitleText.Foreground=FileText.Foreground=muted; Editor.Background=ConsoleBox.Background=VariablesPanel.Background=VariablesGrid.Background=card; Editor.Foreground=ConsoleBox.Foreground=VariablesGrid.Foreground=text; VariablesGrid.RowBackground=card; VariablesGrid.AlternatingRowBackground=gutter; VariablesGrid.HorizontalGridLinesBrush=border; VariablesGrid.VerticalGridLinesBrush=border; LineNumbers.Background=gutter;LineNumbers.Foreground=muted; VariablesPanel.BorderBrush=border;
  StatusBar.Background=B(_dark?"#101A2B":"#172033");StatusText.Foreground=VersionText.Foreground=B("#CBD5E1");StatusDot.Fill=B("#22C55E"); ThemeButton.Background=B(_dark?"#253552":"#EEF2FF");ThemeButton.Foreground=B(_dark?"#DCE6FF":"#3730A3");RunButton.Background=B("#5B4FF7");RunButton.Foreground=Brushes.White;
 }
 void Theme_Click(object s,RoutedEventArgs e){_dark=!_dark;ApplyTheme();}
 void Run_Click(object s,RoutedEventArgs e){try{var r=_interpreter.Run(Editor.Text);ConsoleBox.Text=string.IsNullOrWhiteSpace(r)?"✓ Η εκτέλεση ολοκληρώθηκε.":r;VariablesGrid.ItemsSource=_interpreter.Variables.Select(v=>new{Name=v.Key,Value=v.Value}).ToList();StatusText.Text="Η εκτέλεση ολοκληρώθηκε";StatusDot.Fill=B("#22C55E");}catch(Exception ex){ConsoleBox.Text="Σφάλμα: "+ex.Message;StatusText.Text="Σφάλμα εκτέλεσης";StatusDot.Fill=B("#EF4444");}}
 void Clear_Click(object s,RoutedEventArgs e){ConsoleBox.Clear();VariablesGrid.ItemsSource=null;StatusText.Text="Έτοιμο";StatusDot.Fill=B("#22C55E");}
 void New_Click(object s,RoutedEventArgs e){Editor.Clear();_file=null;FileText.Text="χωρίς τίτλο.glossa";StatusText.Text="Νέο αρχείο";}
 void Open_Click(object s,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Αρχεία Algora (*.glossa;*.algo)|*.glossa;*.algo|Αρχεία κειμένου (*.txt)|*.txt|Όλα τα αρχεία|*.*"};if(d.ShowDialog()==true){Editor.Text=File.ReadAllText(d.FileName);_file=d.FileName;FileText.Text=Path.GetFileName(_file);StatusText.Text="Το αρχείο άνοιξε";}}
 void Save_Click(object s,RoutedEventArgs e){if(_file==null){var d=new SaveFileDialog{Filter="Αρχείο ΓΛΩΣΣΑΣ (*.glossa)|*.glossa|Αλγόριθμος (*.algo)|*.algo|Αρχείο κειμένου (*.txt)|*.txt",DefaultExt=".glossa"};if(d.ShowDialog()!=true)return;_file=d.FileName;}File.WriteAllText(_file,Editor.Text);FileText.Text=Path.GetFileName(_file);StatusText.Text="Αποθηκεύτηκε";}
 void Editor_TextChanged(object s,System.Windows.Controls.TextChangedEventArgs e)=>UpdateLines();
 void UpdateLines(){if(LineNumbers==null||Editor==null)return;var n=Math.Max(1,Editor.LineCount);LineNumbers.Text=string.Join(Environment.NewLine,Enumerable.Range(1,n));}
 void Editor_KeyDown(object s,KeyEventArgs e){if(e.Key==Key.F5){Run_Click(s,new RoutedEventArgs());e.Handled=true;}}
 void Window_KeyDown(object s,KeyEventArgs e){if(e.Key!=Key.F11)return;if(WindowStyle!=WindowStyle.None){_oldStyle=WindowStyle;_oldState=WindowState;WindowStyle=WindowStyle.None;WindowState=WindowState.Maximized;}else{WindowStyle=_oldStyle;WindowState=_oldState;}}
}