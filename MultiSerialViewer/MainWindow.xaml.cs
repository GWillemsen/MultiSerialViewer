using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MultiSerialViewer
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

        private void mniAdd_Click(object sender, RoutedEventArgs e)
        {
            var serialConnection = new SerialConnection();
            serialConnection.Width = 700;
            serialConnection.VerticalAlignment = VerticalAlignment.Stretch;
            serialConnection.WantsClosing += (s, e) =>
            {
                (s as SerialConnection)?.DisposeReader();
                stkBlocks.Children.Remove(serialConnection);
            };
            
            stkBlocks.Children.Add(serialConnection);
        }

        private void stkBlocks_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach(var connection in (sender as StackPanel).Children.Cast<SerialConnection>())
            {
                connection.Height = (sender as StackPanel).ActualHeight - 20;
            }
        }

        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var stackPanel = (sender as ScrollViewer).Content as StackPanel;
            stackPanel.Height = (sender as ScrollViewer).ActualHeight - 20;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var children = stkBlocks.Children.Cast<SerialConnection>().ToArray();
            foreach (var child in children)
            {
                child.DisposeReader();
                stkBlocks.Children.Remove(child);
            }
        }
    }
}
