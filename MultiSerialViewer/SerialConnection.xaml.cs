using System;
using System.Collections.Generic;
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
    /// Interaction logic for SerialConnection.xaml
    /// </summary>
    public partial class SerialConnection : UserControl
    {
        public event EventHandler WantsClosing;
        private SerialPortReader reader;
        private string toSendData = "";
        private bool capturing = false;
        private Point dragStartPoint;
        private double startWidth;

        public SerialConnection()
        {
            InitializeComponent();
        }

        private void SerialConnection_Loaded(object sender, RoutedEventArgs e)
        {
            cboPorts.DropDownOpened += (s, e) => EnumerateSerialPorts();
        }

        private void EnumerateSerialPorts()
        {
            string[] portNames = System.IO.Ports.SerialPort.GetPortNames();
            Dispatcher.Invoke(() =>
            {
                string selectedName = cboPorts.Text;
                cboPorts.ItemsSource = portNames;
                for (int pIndex = 0; pIndex < portNames.Length; pIndex++)
                {
                    if (cboPorts.Items[pIndex] as string == selectedName)
                        cboPorts.SelectedIndex = pIndex;
                }
            });
        }

        private void mniOpen_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem from)
            {
                if (from.Header.ToString() == "Open")
                {
                    if (int.TryParse(txbBaud.Text, out int baud))
                    {
                        System.IO.Ports.SerialPort port = new System.IO.Ports.SerialPort(cboPorts.Text, baud);
                        try
                        {
                            if (!port.IsOpen)
                                port.Open();
                            reader = new SerialPortReader(port);
                            reader.NewTextAvaible += HandleNewData;
                            reader.Connected += HandleConnectEvent;
                            reader.Disconnected += HandleDisconnectEvent;
                            txbBaud.IsEnabled = false;
                            from.Header = "Close";
                            cboPorts.IsEnabled = false;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error trying to open the serial port." + Environment.NewLine + "Error message: " + ex.Message, "Error opening serial port.", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid baud-rate entered.", "Parsing error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    if (reader != default)
                    {
                        reader.Close();
                    }
                    txbBaud.IsEnabled = true;
                    cboPorts.IsEnabled = true;
                    from.Header = "Open";
                    Background = null;
                }
            }
        }

        private void cboPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbo)
            {
                Dispatcher.Invoke(() =>
                {
                    mniOpen.IsEnabled = cboPorts.SelectedIndex >= 0;
                });
            }
        }

        private void HandleNewData(object sender, NewTextAvaibleEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    Paragraph data = rtbData.Document.Blocks.LastBlock as Paragraph;
                    data.Inlines.Remove(data.Inlines.LastInline);

                    if (data.Inlines.LastInline is Run lastLine &&
                        (lastLine.Foreground as SolidColorBrush).Color == e.Color &&
                        (lastLine.Background  as SolidColorBrush).Color == e.Background)
                    {
                        lastLine.Text += e.Text;
                    }
                    else
                    {
                        Run textBlock = new Run()
                        {
                            Foreground = new SolidColorBrush(e.Color),
                            Background = new SolidColorBrush(e.Background),
                            Text = e.Text,
                        };
                        data.Inlines.Add(textBlock);
                    }
                    data.Inlines.Add(new Run()
                    {
                        Foreground = new SolidColorBrush(Colors.White),
                        Background = new SolidColorBrush(Colors.Black),
                        Text = toSendData
                    });

                    rtbData.ScrollToEnd();
                    rtbData.Selection.Select(rtbData.Document.ContentEnd, rtbData.Document.ContentEnd);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private void txbBaud_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!ushort.TryParse(e.Text, out _))
                e.Handled = true;
        }

        private void rtbData_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            toSendData += e.Text;
        }

        private void rtbData_KeyDown(object sender, KeyEventArgs e)
        {
            if (reader != default && reader.IsConnected)
            {
                if (e.Key == Key.Enter)
                {
                    reader?.SendString(toSendData + "\n");
                    Dispatcher.Invoke(() =>
                    {
                        Paragraph a = new Paragraph
                        {
                            Padding = new Thickness(0),
                            Margin = new Thickness(0),
                            KeepTogether = true,
                            KeepWithNext = true,
                        };
                        (rtbData.Document.Blocks.LastBlock as Paragraph).KeepWithNext = true;
                        (rtbData.Document.Blocks.LastBlock as Paragraph).KeepTogether = true;
                        rtbData.Document.Blocks.Add(a);
                        rtbData.Document.LineStackingStrategy = LineStackingStrategy.MaxHeight;

                    });
                    toSendData = string.Empty;
                    e.Handled = true;
                }
            }
            else
                e.Handled = true;
            Dispatcher.Invoke(() =>
            {
                rtbData.Selection.Select(rtbData.Document.ContentEnd, rtbData.Document.ContentEnd);
            });
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (reader != default)
                reader.NewTextAvaible -= HandleNewData;
            reader?.Dispose();
        }

        private void griSizeDragger_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(griSizeDragger);
            dragStartPoint = e.GetPosition(this);
            startWidth = Width;
            capturing = true;
        }

        private void griSizeDragger_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(default);
            capturing = false;
        }

        private void griSizeDragger_MouseMove(object sender, MouseEventArgs e)
        {
            if (!capturing)
                return;
            Vector difference = dragStartPoint - e.GetPosition(this);

            double newWidth = startWidth - difference.X;
            if (newWidth < 50)
                newWidth = 50;

            Width = newWidth;            
        }

        private void mniExit_Click(object sender, RoutedEventArgs e)
        {
            reader?.Close();
            WantsClosing?.Invoke(this, new EventArgs());
        }

        private void HandleConnectEvent(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                lblConnected.Text = "Connected";
                Background = null;
            });
        }

        private void HandleDisconnectEvent(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                lblConnected.Text = "Disconnected";
                Background = new SolidColorBrush(Colors.DarkOrange);
            });
        }

        public void DisposeReader()
        {
            if (reader != default)
                reader.NewTextAvaible -= HandleNewData;
            reader?.Dispose();
        }
    }
}
