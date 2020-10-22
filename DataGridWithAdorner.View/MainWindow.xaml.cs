using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DataGridWithAdorner.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ListView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            var dc = DataContext;

            var r = MessageBox.Show("I would like to start the adorner from here but can't cast the SelectedItem!");

        }
    }
}
