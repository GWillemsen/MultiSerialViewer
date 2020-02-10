using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.IO.Ports;
using System.Text;

using System.Timers;
using System.Windows.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MultiSerialViewer
{
    /// <summary>
    /// A class that reads continuously from a serial port.
    /// </summary>
    internal class SerialPortReader : IDisposable
    {
        public Color CurrrentColor { get; private set; } = Colors.White;
        public Color BackgroundColor { get; private set; } = Colors.Black;
        public bool IsConnected { get; private set; } = false;
        public event EventHandler<NewTextAvaibleEventArgs> NewTextAvaible;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        private SerialPort serialPort;
        private Timer timer;
        private StringBuilder textBuilder = new StringBuilder();
        private StringBuilder commandBuilder = new StringBuilder(5);
        private bool fromEsc = false;
        private bool serialWasOpen = false;
        private string comPortName = "";
       
        public SerialPortReader(SerialPort serialPort) : base()
        {
            this.serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
            serialPort.DataReceived += HandleDataReceived;
            comPortName = serialPort.PortName;
            textBuilder.EnsureCapacity(20);
            timer = new Timer(1000);
            timer.Elapsed += IsOpenCheck_Elapsed;
            timer.Start();
        }

        private void IsOpenCheck_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            try
            {
                if (serialPort != default && serialPort.IsOpen)
                {
                    if (serialWasOpen)
                        return;
                    else
                    {
                        serialWasOpen = true;
                        Connected?.Invoke(this, new EventArgs());
                        IsConnected = true;
                    }
                }
                else
                {
                    if (serialWasOpen)
                    {
                        serialWasOpen = false;
                        IsConnected = false;
                        Disconnected?.Invoke(this, new EventArgs());
                    }

                    if (SerialPort.GetPortNames().Where(s => s == comPortName).Any())
                    {
                        if (serialPort == default)
                        {
                            serialPort = new SerialPort(comPortName);
                            serialPort.DataReceived += HandleDataReceived;
                        }
                        serialPort.Open();
                    }
                }
            }
            finally
            {
                timer.Start();
            }
        }

        /// <summary>
        /// Stops the reading from the serial port and closes the serial port it self.
        /// </summary>
        public void Close()
        {
            serialPort.DataReceived -= HandleDataReceived;
            serialPort.Close();
            timer.Stop();
            timer.Close();
            IsConnected = false;
            serialWasOpen = false;
            Disconnected?.Invoke(this, new EventArgs());
        }

        private void HandleDataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            int minBufferSize = serialPort.BytesToRead;
            if (minBufferSize > 0)
            {
                byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(minBufferSize);
                int bytesRead = serialPort.Read(buffer, 0, minBufferSize);
                Task.Run(() => HandleFinalValueParsing(buffer, bytesRead));
            }
        }

        private void HandleFinalValueParsing(byte[] buffer, int bytesRead)
        {
            for (int bIndex = 0; bIndex < bytesRead; bIndex++)
            {
                if (fromEsc)
                {
                    commandBuilder.Append((char)buffer[bIndex]);
                    string command = commandBuilder.ToString();
                    if (command[0] == '[')
                    {
                        char lastChar = command[^1];
                        switch (lastChar)
                        {
                            case 'm':
                                (var newBack, var newFore) = HandleAttributeSetting(command);
                                NewTextAvaible?.Invoke(this, new NewTextAvaibleEventArgs(textBuilder.ToString(), CurrrentColor, BackgroundColor));
                                textBuilder.Clear();
                                BackgroundColor = newBack ?? BackgroundColor;
                                CurrrentColor = newFore ?? CurrrentColor;
                                fromEsc = false;
                                break;

                            case 'c':
                                fromEsc = false;
                                break;
                            case 'n':
                                fromEsc = false;
                                break;
                            case 'R':
                                fromEsc = false;
                                break;
                            case 'h':
                                fromEsc = false;
                                break;
                            case 'H':
                                fromEsc = false;
                                break;
                            case 'A':
                                fromEsc = false;
                                break;
                            case 'B':
                                fromEsc = false;
                                break;
                            case 'D':
                                fromEsc = false;
                                break;
                            case 'f':
                                fromEsc = false;
                                break;
                            case 's':
                                fromEsc = false;
                                break;
                            case 'u':
                                fromEsc = false;
                                break;
                            case 'r':
                                fromEsc = false;
                                break;
                            case 'M':
                                fromEsc = false;
                                break;
                            case 'g':
                                fromEsc = false;
                                break;
                            case 'K':
                                fromEsc = false;
                                break;
                            case 'J':
                                fromEsc = false;
                                break;
                            case 'i':
                                fromEsc = false;
                                break;
                            case 'p':
                                fromEsc = false;
                                break;
                        }
                        if (!fromEsc)
                        {
                            commandBuilder.Clear();
                        }
                    }
                    if (command[0] == '(' || command[0] == ')' || command[0] == 'H' || command[0] == 'M' ||
                        command[0] == 'D' || command[0] == '8' || command[0] == '7' || command[0] == 'c')
                    {
                        fromEsc = false;
                        commandBuilder.Clear();
                    }

                }
                else
                {
                    if (buffer[bIndex] == 0x1B) // 0x1B => ASCII 'ESC' char
                    {
                        fromEsc = true;
                    }
                    else
                    {
                        textBuilder.Append(serialPort.Encoding.GetString(buffer, bIndex, 1));
                    }
                }
            }
            NewTextAvaible?.Invoke(this, new NewTextAvaibleEventArgs(textBuilder.ToString(), CurrrentColor, BackgroundColor));
            textBuilder.Clear();
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer, true);
        }
       
        private (Color? background, Color? foreground) HandleAttributeSetting(string command)
        {
            Color? newBackgroundColor = default;
            Color? newForegroundColor = default;
            string[] attribs = command[1..^1].Split(';'); // skip the [ in front and dont include the m in the back.
            foreach (var attribute in attribs)
            {
                if (int.TryParse(attribute, out int number))
                {
                    switch(number)
                    {
                        case 0:
                            newForegroundColor = Colors.White;
                            newBackgroundColor = Colors.Black;
                            break;
                        case 30:
                            newForegroundColor = Colors.Black;
                            break;
                        case 31:
                            newForegroundColor = Colors.Red;
                            break;
                        case 32:
                            newForegroundColor = Colors.Green;
                            break;
                        case 33:
                            newForegroundColor = Colors.Yellow;
                            break;
                        case 34:
                            newForegroundColor = Colors.Blue;
                            break;
                        case 35:
                            newForegroundColor = Colors.Magenta;
                            break;
                        case 36:
                            newForegroundColor = Colors.Cyan;
                            break;
                        case 37:
                            newForegroundColor = Colors.White;
                            break;

                        case 40:
                            newBackgroundColor = Colors.Black;
                            break;
                        case 41:
                            newBackgroundColor = Colors.Red;
                            break;
                        case 42:
                            newBackgroundColor = Colors.Green;
                            break;
                        case 43:
                            newBackgroundColor = Colors.Yellow;
                            break;
                        case 44:
                            newBackgroundColor = Colors.Blue;
                            break;
                        case 45:
                            newBackgroundColor = Colors.Magenta;
                            break;
                        case 46:
                            newBackgroundColor = Colors.Cyan;
                            break;
                        case 47:
                            newBackgroundColor = Colors.White;
                            break;

                        default:
                            break;
                    }
                }
            }
            return (newBackgroundColor, newForegroundColor);
        }

        /// <summary>
        /// Writes the string to the serial port.
        /// </summary>
        /// <param name="command">The string to write.</param>
        public void SendString(string command)
        {
            if (serialPort == default || !serialPort.IsOpen)
                throw new InvalidOperationException();
            try
            {
                serialPort.Write(command);
            }
            catch (TimeoutException ex)
            {
                MessageBox.Show("Write timeout occurred. Please re-enter the command.");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                    timer.Dispose();
                    serialPort.Dispose();
                    commandBuilder.Clear();
                    textBuilder.Clear();

                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SerialPortReader()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
