
using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace WpfApplication1 {
  /// <summary>
  /// Interaction logic for StudentDetails.xaml
  /// </summary>
  public partial class StudentDetails : UserControl {

    public List<string> OriginalWrittenWork { get; set; } = new List<string>();

    public List<string> OriginalPerformanceTask { get; set; } = new List<string>();

    public ObservableCollection<FieldDefinition> WrittenWorks { get; set; } = new ObservableCollection<FieldDefinition>();

    public ObservableCollection<FieldDefinition> PerformanceTasks { get; set; } = new ObservableCollection<FieldDefinition>();

    private string _exam;

    public string Exam {
      get { return _exam; }
      set {
        txtExam.Text = _exam = value;
      }
    }

    public string StudentName {
      get { return tbName.Text; }
      set { 
        tbName.Text = value;
        btnDeleteStudent.ToolTip = "Delete " + value;
      }
    }

    public uint StudentIndex { get; set; }

    public bool IsMale { get; set; }

    public EventHandler SaveExamClicked;
    public EventHandler SaveWrittenWorksClicked;
    public EventHandler SavePerformanceTasksClicked;
    public EventHandler DeleteStudent;

    public StudentDetails() {
      InitializeComponent();
      DataContext = this;
    }

    private void ShowGrade_Checked(object sender, RoutedEventArgs e) {
        tbExam.Visibility = Visibility.Visible;      
    }

    private void ShowGrade_Unchecked(object sender, RoutedEventArgs e) {
        tbExam.Visibility = Visibility.Collapsed;     
    }

    private void SaveExam_Click(object sender, RoutedEventArgs e) {
      _exam = txtExam.Text;
      SaveExamClicked?.Invoke(this, EventArgs.Empty);
    }

    private void SaveWrittenWorks_Click(object sender, RoutedEventArgs e) =>
      SaveWrittenWorksClicked?.Invoke(this, EventArgs.Empty);
    

    private void SavePerformanceTasks_Click(object sender, RoutedEventArgs e) =>
      SavePerformanceTasksClicked?.Invoke(this, EventArgs.Empty);

    private void WrittenScoresTextChanged(object sender, TextChangedEventArgs e) {
      var tb = (TextBox)sender;
      int index = (int)tb.Tag; // index in the collection

      string newValue = tb.Text;
      string oldValue = OriginalWrittenWork[index];
      btnSaveWrittenWorks.IsEnabled = (newValue != oldValue);
      List<string> valuesList = [.. WrittenWorks.Select(w => w.Value)];
    }

    private void PerformanceScoresTextChanged(object sender, TextChangedEventArgs e) {
      var tb = (TextBox)sender;
      int index = (int)tb.Tag; // index in the collection

      string newValue = tb.Text;
      string oldValue = OriginalPerformanceTask[index];
      btnSavePerformanceTasks.IsEnabled = (newValue != oldValue);
    }

    private void DeleteStudent_Click(object sender, RoutedEventArgs e) {
      DeleteStudent?.Invoke(this, EventArgs.Empty);
    }

    private void ExamTextChanged(object sender, TextChangedEventArgs e) {
      btnSaveExam.IsEnabled = txtExam.Text != Exam;
    }

    private void NumberOnlyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
      e.Handled = !int.TryParse(e.Text, out _);
    }

    public void SetSaveExamBtnEnabled(bool enabled) => btnSaveExam.IsEnabled = enabled;
    public void SetSaveWrittenWorksBtnEnabled(bool enabled) => btnSaveWrittenWorks.IsEnabled = enabled;
    public void SetSavePerformanceTasksBtnEnabled(bool enabled) => btnSavePerformanceTasks.IsEnabled = enabled;    
    
    public void SetGrade(string grade) => tbExam.Text = grade;

  }
  public class EmptyToVisibilityConverter : IMultiValueConverter {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
      string value = values[0] as string;
      string label = values[1] as string;

      return string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(label)
          ? Visibility.Collapsed
          : Visibility.Visible;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
  }
}
