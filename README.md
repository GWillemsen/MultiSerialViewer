# MultiSerialViewer
A simple application to open multiple serial ports at the same time (with coloring).

# Use
This is a simple application that allows you view the data of multiple serial ports and interact with them. Each serial port has its own 'column'. In that block there is an Open/Close button for the serial port, an exit button to remove the view completely, dropdown to select the Serial port (updates each time you open it). And a textbox to input the baud rate.
The serial ports output will be show in the textbox below these controls. This is also the place that you enter your commands. When you press enter the text that you entered before will be send to the serial port.

# Auto reconnect
When you are connected to a serial port and you lose connection (like you for example unplug the device). The background color of the block will become orange and the status text will show "Disconnected". If you then reconnect the serial port again and it has the same name, the program will try to reconnect. If it succeeds, it will resume as normal, the status will go back to "Connected" and the background color will return to the default. If it fails nothing will happen and it keep trying to reopen as soon as it can (until you of course close the block or program first).

# License
I licensed this under the MIT license. I however would appreciate that if you make changes or fix a problem that make them available to all of us using a pull request. Thanks.
