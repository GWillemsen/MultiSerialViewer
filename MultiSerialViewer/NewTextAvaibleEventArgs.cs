using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace MultiSerialViewer
{
    class NewTextAvaibleEventArgs : EventArgs
    {
        public string Text { get; private set; }
        public Color Color { get; private set; }
        public Color Background { get; private set; }
        public NewTextAvaibleEventArgs(string text, Color color, Color background) : base()
        {
            Text = text;
            Color = color;
            Background = background;
        }
    }
}
