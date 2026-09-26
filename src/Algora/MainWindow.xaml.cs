using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Algora.Core;
namespace Algora;
public partial class MainWindow : Window
{
 private readonly Interpreter _interpreter = new();
 private bool _dark; private WindowStyle _oldStyle; private WindowState _oldState;
 public MainWindow() => InitializeComponent();
 private void Run_Click(object sender, RoutedEventArgs e)
 {
  try { var result=_interpreter.Run(Editor.Text); ConsoleBox.Text=string.IsNullOrWhiteSpace(result)?"✓ Η εκτέλεση ολοκληρώθηκε.":result; VariablesGrid.ItemsSource=_interpreter.Variables.Select(v=>new { Name=v.Key, Value=v.Value }).ToList(); StatusText.Text="Algora • Η εκτέλεση ολοκληρώθηκε"; }
  catch(Exception ex) { ConsoleBox.Text="Σφάλμα: "+ex.Message; StatusText.Text="Algora • Σφάλμα εκτέλεσης"; }
 }
 private void Theme_Click(object sender, RoutedEventArgs e)
 {
  _dark=!_dark; Background=B(_dark?"#101827":"#F4F7FB"); EditorCard.Background=OutputCard.Background=B(_dark?"#172033":"#FFFFFF"); Editor.Background=ConsoleBox.Background=B(_dark?"#172033":"#FFFFFF"); Editor.Foreground=ConsoleBox.Foreground=B(_dark?"#E5E7EB":"#172033");
 }
 private static Brush B(string h)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(h));
 private void Window_KeyDown(object sender, KeyEventArgs e)
 {
  if(e.Key!=Key.F11)return;
  if(WindowStyle!=WindowStyle.None){_oldStyle=WindowStyle;_oldState=WindowState;WindowStyle=WindowStyle.None;WindowState=WindowState.Maximized;}
  else {WindowStyle=_oldStyle;WindowState=_oldState;}
 }
}