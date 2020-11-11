using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using InTheHand;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using InTheHand.Net.Ports;
using System.IO;
using InTheHand.Windows.Forms;
using InTheHand.Net;

namespace Bluetooth
{
    public partial class Form1 : Form
    {
        List<string> items;
        public Form1()
        {
            items = new List<string>();
            InitializeComponent();
        }

        private void bGo_Click(object sender, EventArgs e)
        {

            if (scanStarted)
            {
                updateUI("Already started");
                return;
            }
            
            startScan();
           
        }

        private void startScan()
        {
            listBox1.DataSource = null;
            listBox1.Items.Clear();
            items.Clear();
            
            Thread bluetoothScanThread = new Thread(new ThreadStart(scan));
            bluetoothScanThread.Start();
        }

        BluetoothDeviceInfo[] devices;
        bool scanStarted = false;
        private void scan()
        {

            device = null;
            if (!BluetoothRadio.IsSupported)
                updateUI("No Bluetooth device detected.");
            if (BluetoothRadio.PrimaryRadio.Mode == RadioMode.PowerOff)
                BluetoothRadio.PrimaryRadio.Mode = RadioMode.Connectable;
            updateUI("Bluetooth device name: "+BluetoothRadio.PrimaryRadio.Name.ToString());
            updateUI("Bluetooth device mode: "+BluetoothRadio.PrimaryRadio.Mode.ToString());
            scanStarted = true;
            updateUI("Starting Scan...");

            BluetoothClient client = new BluetoothClient();
            devices = client.DiscoverDevices(10);

            updateUI("Scan complete");
            updateUI(devices.Length.ToString() + " devices discovered, choose one to connect");

            foreach(BluetoothDeviceInfo d in devices)
            {
                items.Add(d.DeviceName);
                
            }

           updateDeviceList();
           scanStarted = false;
           
        }



        private void updateUI(string message)
        {
            Func<int> del = delegate ()
            {
                tbOutput.AppendText(message + System.Environment.NewLine);
                return 0;
            };
            Invoke(del);
        }

        private void updateDeviceList()
        {
            Func<int> del = delegate ()
            {
                listBox1.DataSource = items;
                return 0;
            };
            Invoke(del);
        }

        BluetoothDeviceInfo device = null;
        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            
            device = devices.ElementAt(listBox1.SelectedIndex);
            updateUI(device.DeviceName + "was slected, attempting connect");
            
            if (!device.Authenticated)
            {
                
                if (!BluetoothSecurity.PairRequest(device.DeviceAddress, "0000"))
                {
                    MessageBox.Show("Request failed");
                }
                else
                {
                    updateUI(device.DeviceName + " is ready");
                }
            }
            else
            {
                updateUI(device.DeviceName + " already connected");
            }
            tbAdress.Clear();
            tbAdress.AppendText("Adress of selected device: " + device.DeviceAddress);
        }

        private void sendFileButton_Click(object sender, EventArgs e)
        {
            if (device == null)
            {
                MessageBox.Show("Nie wybrano urządzenia!!!");
                return;
            }
            
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                device.Update();
                device.Refresh();
                device.SetServiceState(BluetoothService.ObexObjectPush, true);

                var file = ofd.FileName;
                var uri = new Uri("obex://" + device.DeviceAddress + "/" + file);
                var request = new ObexWebRequest(uri);
                request.ReadFile(file);

                ObexWebResponse response = null;
                try
                {
                    response = (ObexWebResponse)request.GetResponse();
                    MessageBox.Show(response.StatusCode.ToString());
                    response.Close();
                }catch(Exception)
                {   
                    // check response.StatusCode
                    MessageBox.Show("Urządzenie nie odpowiedziło");
                }
                
            }

        }
    }
}
