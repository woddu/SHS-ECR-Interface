using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApplication1 {
  /// <summary>
  /// Interaction logic for HighestScores.xaml
  /// </summary>
  public partial class HighestScores : UserControl {



    public ObservableCollection<string> WrittenWorks { get; set; } = new ObservableCollection<string>();

    public ObservableCollection<string> PerformanceTasks { get; set; } = new ObservableCollection<string>();

    private string _exam;

    public string Exam {
      get { return _exam; }
      set {
        txtExam.Text = _exam = value;
        btnExamItem.IsEnabled = int.TryParse(_exam, out int _out) && _out > 0;
      }
    }


    public EventHandler SaveExamClicked;
    public EventHandler SaveWrittenWorksClicked;
    public EventHandler SavePerformanceTasksClicked;
    public Action<uint, ScoreType, int> HighestScoreItemClicked;
    public HighestScores() {
      InitializeComponent();
      DataContext = this;
    }

    private void WrittenWorks_ItemCliked(object sender, System.Windows.RoutedEventArgs e) {
      if (sender is Button btn) {
        HighestScoreItemClicked?.Invoke(uint.Parse(btn.Tag.ToString()), ScoreType.WrittenWorks, int.Parse(WrittenWorks[int.Parse(btn.Tag.ToString())]));
      }
    }
    private void PerformanceTasks_ItemCliked(object sender, System.Windows.RoutedEventArgs e) {
      if (sender is Button btn) {

        HighestScoreItemClicked?.Invoke(uint.Parse(btn.Tag.ToString()), ScoreType.PerformanceTasks, int.Parse(PerformanceTasks[int.Parse(btn.Tag.ToString())]));
      }
    }
    private void Exam_ItemCliked(object sender, System.Windows.RoutedEventArgs e) {
      if (sender is Button btn) {
        HighestScoreItemClicked?.Invoke(0u, ScoreType.Exam, int.Parse(Exam));
      }
    }

    public void SetWrittenWorksPercentage(string percentage) => tbWrittenWorks.Text = "Writtent Works : " + percentage;
    public void SetPerformancePercentage(string percentage) => tbPerformanceTasks.Text = "Performance Tasks : " + percentage;
    public void SetExamPercentage(string percentage) => tbExam.Text = "Exam : " + percentage;

    private void SaveExam_Click(object sender, System.Windows.RoutedEventArgs e) {
      _exam = txtExam.Text;
      SaveExamClicked?.Invoke(this, EventArgs.Empty);
    }
    

    private void SaveWrittenWorks_Click(object sender, System.Windows.RoutedEventArgs e) {

      for (int i = 0; i < WrittenWorks.Count; i++) {
        // Get the container for this item
        var container = (ContentPresenter)itemsControlWrittenWorks
            .ItemContainerGenerator.ContainerFromIndex(i);

        if (container != null) {
          // Find the TextBox inside the DataTemplate
          var tb = FindVisualChild<TextBox>(container);
          if (tb != null) {
            WrittenWorks[i] = tb.Text; // overwrite the value in the collection
          }
        }
      }

      Debug.WriteLine("WrittenWorks saved: " + string.Join(", ", WrittenWorks));

      SaveWrittenWorksClicked?.Invoke(this, EventArgs.Empty);
      btnSaveWrittenWorks.IsEnabled = false;
    }

    private void SavePerformanceTasks_Click(object sender, System.Windows.RoutedEventArgs e) {

      for (int i = 0; i < WrittenWorks.Count; i++) {
        // Get the container for this item
        var container = (ContentPresenter)itemsControlPerformanceTasks
            .ItemContainerGenerator.ContainerFromIndex(i);

        if (container != null) {
          // Find the TextBox inside the DataTemplate
          var tb = FindVisualChild<TextBox>(container);
          if (tb != null) {
            PerformanceTasks[i] = tb.Text; // overwrite the value in the collection
          }
        }
      }

      Debug.WriteLine("PerformanceTasks saved: " + string.Join(", ", PerformanceTasks));

      SavePerformanceTasksClicked?.Invoke(this, EventArgs.Empty);
      btnSavePerformanceTasks.IsEnabled = false;
    }
    private void WrittenScoresTextChanged(object sender, TextChangedEventArgs e) {
      var tb = (TextBox)sender;
      int index = (int)tb.Tag; // index in the collection

      string newValue = tb.Text;
      string oldValue = WrittenWorks[index];

      btnSaveWrittenWorks.IsEnabled = (newValue != oldValue);
    }

    private void PerformanceScoresTextChanged(object sender, TextChangedEventArgs e) {
      var tb = (TextBox)sender;
      int index = (int)tb.Tag; // index in the collection
      
      string newValue = tb.Text;
      string oldValue = PerformanceTasks[index];

      btnSavePerformanceTasks.IsEnabled = (newValue != oldValue);
    }

    private void ExamTextChanged(object sender, TextChangedEventArgs e) {
      btnSaveExam.IsEnabled = txtExam.Text != Exam;
    }

    private void NumberOnlyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
      e.Handled = !int.TryParse(e.Text, out _);
    }

    public void SetExamBtnEnabled(bool enabled) => btnSaveExam.IsEnabled = enabled;

    public void SetWrittenWorksBtnEnabled(bool enabled) => btnSaveWrittenWorks.IsEnabled = enabled;

    public void SetPerformanceTasksBtnEnabled(bool enabled) => btnSavePerformanceTasks.IsEnabled = enabled;

    private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject {
      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
        var child = VisualTreeHelper.GetChild(parent, i);
        if (child is T tChild)
          return tChild;

        var result = FindVisualChild<T>(child);
        if (result != null)
          return result;
      }
      return null;
    }
  }

}
