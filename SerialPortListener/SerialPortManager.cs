using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Reflection;
using System.ComponentModel;
using System.Threading;
using System.IO;
using System.Management;
using System.Windows.Forms;

namespace ArdDebug.Serial
{


    /// <summary>
    /// Manager for serial port data
    /// </summary>
    public class SerialPortManager : IDisposable
    {
        class USBDeviceInfo
        {
            //public USBDeviceInfo(string deviceID, string pnpDeviceID, string description, string name)
            public USBDeviceInfo(string deviceID, string name)
            {
                this.DeviceID = deviceID;
                //this.PnpDeviceID = pnpDeviceID;
                //this.Description = description;
                this.Name = name;
            }
            public string DeviceID { get; private set; }
            //public string PnpDeviceID { get; private set; }
            //public string Description { get; private set; }
            public string Name { get; private set; }
        }

        public SerialPortManager()
        {
 //           _currentSerialSettings.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_currentSerialSettings_PropertyChanged);
            ScanPorts();
            //// Finding installed serial ports on hardware
            //_currentSerialSettings.PortNameCollection = SerialPort.GetPortNames();
            

            //// If serial ports is found, we select the first found
            //if (_currentSerialSettings.PortNameCollection.Length > 0)
            //    _currentSerialSettings.PortName = _currentSerialSettings.PortNameCollection[0];
        }

        
        ~SerialPortManager()
        {
            Dispose(false);
        }


        public void ScanPorts()
        {
            // find USB devices first, if possible (may not work with all Windows machines....!)
            string ArduinoPort = null;
            try
            {
                List<USBDeviceInfo> devices = new List<USBDeviceInfo>();
                var searcher = new ManagementObjectSearcher(@"Select * From Win32_SerialPort");


                foreach (var device in searcher.Get())
                {
                    //devices.Add(new USBDeviceInfo(
                    USBDeviceInfo usbInfo = new USBDeviceInfo(
                    (string)device.GetPropertyValue("DeviceID"),
                    //(string)device.GetPropertyValue("PNPDeviceID"),
                    //(string)device.GetPropertyValue("Description"),
                    //(string)device.GetPropertyValue("Name")
                    //));
                    (string)device.GetPropertyValue("Name")
                    );
                    if (usbInfo.Name.Contains("rduino"))
                    {
                        ArduinoPort = usbInfo.DeviceID;
                    }
                }
            }
            catch
            { // well at leat we tried....
            }


            if (ArduinoPort != null)
            {
                _currentSerialSettings.PortName = ArduinoPort;
            }
            else
            {
                // Finding installed serial ports on hardware
                _currentSerialSettings.PortNameCollection = SerialPort.GetPortNames();
                // If serial ports is found, we select the first found
                if (_currentSerialSettings.PortNameCollection.Length > 0)
                    _currentSerialSettings.PortName = _currentSerialSettings.PortNameCollection[0];
            }
        }

        #region Fields
        private SerialPort _serialPort;
        private SerialSettings _currentSerialSettings = new SerialSettings();
        private string _latestRecieved = String.Empty;
       // public event EventHandler<SerialDataEventArgs> NewSerialDataRecieved; 

        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the current serial port settings
        /// </summary>
        public SerialSettings CurrentSerialSettings
        {
            get { return _currentSerialSettings; }
            set { _currentSerialSettings = value; }
        }

        #endregion

        #region Event handlers

        //void _currentSerialSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    // if serial port is changed, a new baud query is issued
        //    if (e.PropertyName.Equals("PortName"))
        //        UpdateBaudRateCollection();
        //}

        
        //void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    int dataLength = _serialPort.BytesToRead;
        //    byte[] data = new byte[dataLength];
        //    int nbrDataRead = _serialPort.Read(data, 0, dataLength);
        //    if (nbrDataRead == 0)
        //        return;
        //    lock (Arduino.expectedReply)
        //    {
        //        if (data.AsQueryable().Contains<byte>((byte)'\n'))
        //        {
        //            Arduino.waitingForRX.Set();
        //        }
        //    }
        //    // Send data to whom ever interested
        //    if (NewSerialDataRecieved != null)
        //        NewSerialDataRecieved(this, new SerialDataEventArgs(data));
        //}

        #endregion

        #region Methods

        public string ReadLine()
        {
            try
            {
                return _serialPort.ReadLine();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return string.Empty;
            }
        }
        public string ReadLine(int timeout)
        {
            string line;
            int oldTimeout = _serialPort.ReadTimeout;
            _serialPort.ReadTimeout = timeout;
            try
            {
                line = _serialPort.ReadLine();
            }
            catch
            {
                line = string.Empty;
            }
            _serialPort.ReadTimeout = oldTimeout;
            return line;
        }
        public void Send(string s)
        {
            //s += '\n';
            if (_serialPort != null && _serialPort.IsOpen && s != null)
            {
                _serialPort.Write(s);
            }
        }
        public void Send(byte[] bytes)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Write(bytes, 0, bytes.Length);
            }
        }
        /// <summary>
        /// Connects to a serial port defined through the current settings
        /// </summary>
        public void StartListening()
        {
            // Closing serial port if it is open
            if (_serialPort != null && _serialPort.IsOpen)
                    _serialPort.Close();

            // Setting serial port settings
            _serialPort = new SerialPort(
                _currentSerialSettings.PortName,
                _currentSerialSettings.BaudRate);
           // _serialPort.Handshake = Handshake.RequestToSend;
            _serialPort.RtsEnable = true;
            _serialPort.DtrEnable = true;
            _serialPort.ReadTimeout = 500;
            // Subscribe to event and open serial port for data
            //_serialPort.DataReceived += new SerialDataReceivedEventHandler(_serialPort_DataReceived);
            try
            {
                _serialPort.Open();
            }
            catch
            {
                MessageBox.Show("No device found, please check ports");
            }
        }

        /// <summary>
        /// Closes the serial port
        /// </summary>
        public void StopListening()
        {
            if (_serialPort != null)
              _serialPort.Close();
        }


        /// <summary>
        /// Retrieves the current selected device's COMMPROP structure, and extracts the dwSettableBaud property
        /// </summary>
        //private void UpdateBaudRateCollection()
        //{
        //    try
        //    {
        //        _serialPort = new SerialPort(_currentSerialSettings.PortName);
        //        _serialPort.Open();
        //        object p = _serialPort.BaseStream.GetType().GetField("commProp", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_serialPort.BaseStream);
        //        Int32 dwSettableBaud = (Int32)p.GetType().GetField("dwSettableBaud", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(p);

        //        _serialPort.Close();
        //        _currentSerialSettings.UpdateBaudRateCollection(dwSettableBaud);
        //    }
        //    catch { }
        //}

        // Call to release serial port
        public void Dispose()
        {
            Dispose(true);
        }

        // Part of basic design pattern for implementing Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
             //   _serialPort.DataReceived -= new SerialDataReceivedEventHandler(_serialPort_DataReceived);
            }
            // Releasing serial port (and other unmanaged objects)
            if (_serialPort != null)
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();

                _serialPort.Dispose();
            }
        }


        #endregion

    }

    /// <summary>
    /// EventArgs used to send bytes recieved on serial port
    /// </summary>
    public class SerialDataEventArgs : EventArgs
    {
        public SerialDataEventArgs(byte[] dataInByteArray)
        {
            Data = dataInByteArray;
        }

        /// <summary>
        /// Byte array containing data from serial port
        /// </summary>
        public byte[] Data;
    }
}
