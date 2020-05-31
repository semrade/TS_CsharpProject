using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

//CodeProjectSerialComms program 
//23/04/2013   16:29



namespace CodeProjectSerialComms
{
    public enum PHYSICAL_VALUES
    {
        TS_DSPDRV_CURRENT,
        TS_DSPDRV_VOLTAGE,
        TS_DSPDRV_TEMP,

        TS_DSPDRV_MAX_PHYSICAL

    };

    public enum CMD
    {
        TS_CMD_3_CURRENT,
        TS_CMD_3_VOLTAGE,
        TS_CMD_TEMP,

        TS_CMD_DSP_MAX
    };

    public partial class MainForm1 : Form
    {
        public const int PHYS_SIZE = 3;
        public const UInt16 FIFO = 16;
        public const UInt16 PYLOAD = 8;

        SerialPort ComPort = new SerialPort();
        
        internal delegate void SerialDataReceivedEventHandlerDelegate(object sender, SerialDataReceivedEventArgs e);
        internal delegate void SerialPinChangedEventHandlerDelegate(object sender, SerialPinChangedEventArgs e);
        private SerialPinChangedEventHandler SerialPinChangedEventHandler1;
        delegate void SetTextCallback(string text);
        //string Rxbuffer = String.Empty;
        Byte[] Rxbuffer = new Byte[FIFO];
        Byte[] Txbuffer = new Byte[FIFO];
        UInt16[] InputData = new UInt16[PYLOAD];
        UInt16[] OutputData = new UInt16[PYLOAD];


        public float[] Currents = new float[PHYS_SIZE];
        public float[] Voltages = new float[PHYS_SIZE];

        UInt16 u16AmpCurrent = 0, u16AmpVoltage = 0, u16FreqCurrent = 1, u16FreqVoltage = 1;
        
        public float Temperature = 0;
        public bool Ready = false;

        Stopwatch TimerCounter = new Stopwatch();
        //public static readonly bool IsHighResolution; ===> look for this for more accur...
        long Delta = 0;

        public MainForm1()
        {
            InitializeComponent();
            SerialPinChangedEventHandler1 = new SerialPinChangedEventHandler(PinChanged);
            ComPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(port_DataReceived_1);
        }
     
        private void btnGetSerialPorts_Click(object sender, EventArgs e)
        {
            string[] ArrayComPortsNames = null;
            int index = -1;
            string ComPortName = null;
           
            //Com Ports
            ArrayComPortsNames = SerialPort.GetPortNames();
            do
            {
                index += 1;
                cboPorts.Items.Add(ArrayComPortsNames[index]);             
            } while (!((ArrayComPortsNames[index] == ComPortName) || (index == ArrayComPortsNames.GetUpperBound(0))));
            Array.Sort(ArrayComPortsNames);
           
            if (index == ArrayComPortsNames.GetUpperBound(0))
            {
                ComPortName = ArrayComPortsNames[0];
            }
            //get first item print in text
            cboPorts.Text = ArrayComPortsNames[0];

            //Baud Rate
            cboBaudRate.Items.Add(300);
            cboBaudRate.Items.Add(600);
            cboBaudRate.Items.Add(1200);
            cboBaudRate.Items.Add(2400);
            cboBaudRate.Items.Add(9600);
            cboBaudRate.Items.Add(14400);
            cboBaudRate.Items.Add(19200);
            cboBaudRate.Items.Add(38400);
            cboBaudRate.Items.Add(57600);
            cboBaudRate.Items.Add(115200);
            cboBaudRate.Items.Add(460800);
            cboBaudRate.Items.ToString();
            //get first item print in text
            cboBaudRate.Text = cboBaudRate.Items[0].ToString(); 

            //Data Bits
            cboDataBits.Items.Add(7);
            cboDataBits.Items.Add(8);
            //get the first item print it in the text 
            cboDataBits.Text = cboDataBits.Items[0].ToString();
           

            //Stop Bits
            cboStopBits.Items.Add("One");
            cboStopBits.Items.Add("OnePointFive");
            cboStopBits.Items.Add("Two");
            //get the first item print in the text
            cboStopBits.Text = cboStopBits.Items[0].ToString();

            //Parity 
            cboParity.Items.Add("None");
            cboParity.Items.Add("Even");
            cboParity.Items.Add("Mark");
            cboParity.Items.Add("Odd");
            cboParity.Items.Add("Space");
            //get the first item print in the text
            cboParity.Text = cboParity.Items[0].ToString();

            //Handshake
            cboHandShaking.Items.Add("None");
            cboHandShaking.Items.Add("XOnXOff");
            cboHandShaking.Items.Add("RequestToSend");
            cboHandShaking.Items.Add("RequestToSendXOnXOff");
            //get the first item print it in the text 
            cboHandShaking.Text = cboHandShaking.Items[0].ToString();

        }
        private void port_DataReceived_1(object sender, SerialDataReceivedEventArgs e)
        {
            //Read Port buffer
            ComPort.Read(Rxbuffer, 0, 16);

            //I received data
            Ready = true;

            //Stop counter
            TimerCounter.Stop();

            //Read time ilapsed time
            Delta = TimerCounter.ElapsedMilliseconds;
            
            //Format data to Int16
            for (int i = 0; i < InputData.Length; i++)
            {
                UInt16 var1 = (UInt16)(Rxbuffer[2 * i] << 8);
                UInt16 var2 = (UInt16)(Rxbuffer[(2 * i) + 1]);
                InputData[i] |= var1;
                InputData[i] |= var2;

            }
            // Test button for 16 FIFO
            if (InputData[0] == 3)
            {
                //test if data is the same data has been sended
                if (Equality(Rxbuffer, Txbuffer))
                {
                    //rtbIncoming.AppendText("Test Communication has been successfully\n");
                    rtbIncoming.Invoke(new MethodInvoker(delegate
                    {
                        //message 
                        rtbIncoming.Text = "Test Communication has been successfully\n";

                        //Time of sending and receiving data
                        rtbIncoming.AppendText("Time Taken for --><--" + Delta.ToString() + " ms" + "\n");

                        //sending and receiving combination in hex
                        rtbIncoming.AppendText("TxBuffer => " + BytesToStringConverted(Txbuffer) + "\n");
                        rtbIncoming.AppendText("RxBuffer <= " + BytesToStringConverted(Rxbuffer) + "\n");

                    }));
                }
                else
                {
                    //rtbIncoming.Text = "Test comunicatdion Error\n";
                    rtbIncoming.Invoke(new MethodInvoker(delegate
                    {
                        rtbIncoming.Text = "Test comunicatdion Error\n";
                    }));

                }
            }

            //Rxbuffer = ComPort.ReadExisting();
            //if (Rxbuffer != String.Empty)
            //{
            //  this.BeginInvoke(new SetTextCallback(SetText), new object[] { Rxbuffer });
            //}

        }
        public string BytesToStringConverted(byte[] TxTable)
        {
            string bitString = BitConverter.ToString(TxTable);
            return bitString;
        }
        /*        private void tmrPollForRecivedData_Tick(object sender, EventArgs e)
                {
                    String RecievedData;
                    RecievedData = ComPort.ReadExisting();
                    if (!(RecievedData == ""))
                    {
                        rtbIncoming.Text += RecievedData;
                    }
                }*/
        private void SetText(string text)
        {
            this.rtbIncoming.Text += text;
        }
        internal void PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            SerialPinChange SerialPinChange1 = 0;
            bool signalState = false;

            SerialPinChange1 = e.EventType;
            lblCTSStatus.BackColor = Color.Green;
            lblDSRStatus.BackColor = Color.Green;
            lblRIStatus.BackColor = Color.Green;
            lblBreakStatus.BackColor = Color.Green;
            switch (SerialPinChange1)
            {
                case SerialPinChange.Break:
                    lblBreakStatus.BackColor = Color.Red;
                    MessageBox.Show("Break is Set");
                    break;
                case SerialPinChange.CDChanged:
                    signalState = ComPort.CtsHolding;
                    MessageBox.Show("CD = " + signalState.ToString());
                    break;
                case SerialPinChange.CtsChanged:
                    signalState = ComPort.CDHolding;
                    lblCTSStatus.BackColor = Color.Red;
                    MessageBox.Show("CTS = " + signalState.ToString());
                    break;
                case SerialPinChange.DsrChanged:
                    signalState = ComPort.DsrHolding;
                    lblDSRStatus.BackColor = Color.Red;
                    MessageBox.Show("DSR = " + signalState.ToString());
                    break;
                case SerialPinChange.Ring:
                    lblRIStatus.BackColor = Color.Red;
                    MessageBox.Show("Ring Detected");
                    break;
            }
        }
        private void btnTest_Click(object sender, EventArgs e)
        {
            SerialPinChangedEventHandler1 = new SerialPinChangedEventHandler(PinChanged);
            ComPort.PinChanged += SerialPinChangedEventHandler1;
            ComPort.Open();

            ComPort.RtsEnable = true;
            ComPort.DtrEnable = true;
            btnTest.Enabled = false;

        }
        private void btnPortState_Click(object sender, EventArgs e)
        {
          
            if (btnPortState.Text == "Closed")
            {
                btnPortState.Text = "Open";
                btnPortState.BackColor = Color.Red;
                ComPort.PortName = Convert.ToString(cboPorts.Text);
                ComPort.BaudRate = Convert.ToInt32(cboBaudRate.Text);
                ComPort.DataBits = Convert.ToInt16(cboDataBits.Text);
                ComPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cboStopBits.Text);
                ComPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), cboHandShaking.Text);
                ComPort.Parity = (Parity)Enum.Parse(typeof(Parity), cboParity.Text);
/*                ComPort.ReadBufferSize = 16;
                ComPort.WriteBufferSize = 16;*/
                ComPort.Open();
            }
            else if (btnPortState.Text == "Open")
            {
                btnPortState.Text = "Closed";
                btnPortState.BackColor = Color.Green;
                ComPort.Close();
               
            }
        }
        private void rtbOutgoing_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13) // enter key  
            {
                ComPort.Write("\r\n");
                
            }
            else if (e.KeyChar < 32 || e.KeyChar > 126)
            {
                e.Handled = true; // ignores anything else outside printable ASCII range  
            }
            else
            {
                ComPort.Write(e.KeyChar.ToString());
            }
        }
        private void btnHello_Click(object sender, EventArgs e)
        {
            //Test com cmd
            OutputData[0] = 3;

            for (int i = 1; i < OutputData.Length; i++)
            {
                //store data
                OutputData[i] = 0xCAFE;
            }

            //dispatch data to Bytes 
            Dataformating();

            //Clear the buffer
            TimerCounter.Reset();

            //Send buffer char
            ComPort.Write(Txbuffer, 0, Txbuffer.Length);

            //start time measurement
            TimerCounter.Start();
        }
        private void btnHyperTerm_Click(object sender, EventArgs e)
        {
            string Command1 = txtCommand.Text;
            string CommandSent;
            int Length, j = 0;

            Length = Command1.Length;

            for (int i = 0; i < Length; i++)
            {
                CommandSent = Command1.Substring(j, 1);
                ComPort.Write(CommandSent);
                j++;
            }

        }
        private void rtbIncoming_TextChanged(object sender, EventArgs e)
        {

        }
        public bool Equality(Byte[] a1, Byte[] b1)
        {
            if (a1.Length != b1.Length)
            {
                return false;
            }

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != b1[i])
                {
                    return false;
                }
            }
            return true;
        }
        public void ReadPhysical(PHYSICAL_VALUES Physical)
        {
            switch (Physical)
            {
                case PHYSICAL_VALUES.TS_DSPDRV_CURRENT:
                    {
                        Currents[0] = (Int16)InputData[1] / 100;
                        Currents[1] = (Int16)InputData[2] / 100;
                        Currents[2] = (Int16)InputData[3] / 100;
                        break;
                    }
                case PHYSICAL_VALUES.TS_DSPDRV_VOLTAGE:
                    {
                        Voltages[0] = (Int16)InputData[1] / 100;
                        Voltages[1] = (Int16)InputData[2] / 100;
                        Voltages[2] = (Int16)InputData[3] / 100;
                        break;
                    }
                case PHYSICAL_VALUES.TS_DSPDRV_TEMP:
                    {
                        Temperature = (Int16)InputData[1] / 100;
                        break;
                    }
            }


        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        public void Dataformating()
        {
            for (int i = 0; i < OutputData.Length; i++)
            {
                Txbuffer[2 * i] = (byte)((OutputData[i] & 0xFF00)>>8);
                Txbuffer[(2 * i) + 1] = (byte)(OutputData[i] & 0x00FF);
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            //update the buffer 
            ReadPhysical((PHYSICAL_VALUES)InputData[0]);

            foreach (string item in PhcheckListBox.CheckedItems)
            {
                if (item == "Current")
                {
                    //change cmd
                    OutputData[0] = (UInt16)CMD.TS_CMD_3_CURRENT;

                    //Amd and frequency for current wave
                    OutputData[1] = u16AmpCurrent;
                    OutputData[2] = u16FreqCurrent;

                    //dispatch data to Bytes 
                    Dataformating();

                    try
                    {
                        //ask for currents send 16 bytes to the DSP...
                        ComPort.Write(Txbuffer, 0, Txbuffer.Length);
                    }
                    catch (Exception ex)
                    {
                        rtbIncoming.Invoke(new MethodInvoker(delegate
                        {
                            rtbIncoming.Text = ex.Message + "\n";
                        }));
                    }

                    // wait for information
                    while (false == Ready);

                    // clear flag
                    Ready = false;

                    // Read physical values and converd uint16 to int16
                    ReadPhysical((PHYSICAL_VALUES)InputData[0]);

                    if (ComPort.IsOpen)
                    {
                        // Display data to the GUI
                        rtbIncoming.Invoke(new MethodInvoker(delegate
                        {
                            rtbIncoming.Text =  "Current amplitude  =  " + u16AmpCurrent.ToString() + " V     "
                            +"Current Frequency = "+ u16FreqCurrent.ToString()+" Hz"+"\n";
                            rtbIncoming.AppendText("Current I1: " + Currents[0].ToString() + "\n");
                            rtbIncoming.AppendText("Current I2: " + Currents[1].ToString() + "\n");
                            rtbIncoming.AppendText("Current I3: " + Currents[2].ToString() + "\n");
                        }));
                    }
 
                }
                else if (item == "Voltage")
                {
                    //change cmd
                    OutputData[0] = (UInt16)CMD.TS_CMD_3_CURRENT;

                    //Amd and frequency for current wave
                    OutputData[1] = u16AmpVoltage;
                    OutputData[2] = u16FreqVoltage;

                    //dispatch data to Bytes 
                    Dataformating();

                    try
                    {
                        //ask for currents send 16 bytes to the DSP...
                        ComPort.Write(Txbuffer, 0, Txbuffer.Length);
                    }
                    catch (Exception ex)
                    {
                        rtbIncoming.Invoke(new MethodInvoker(delegate
                        {
                            rtbIncoming.Text = ex.Message + "\n";
                        }));
                    }

                    // wait for information
                    while (false == Ready);

                    // clear flag
                    Ready = false;

                    // Read physical values and converd uint16 to int16
                    ReadPhysical((PHYSICAL_VALUES)InputData[0]);

                    // Display data to the GUI
                    if (ComPort.IsOpen)
                    {
                        rtbIncoming.Invoke(new MethodInvoker(delegate
                        {
                            rtbIncoming.AppendText("Voltage V1: " + Voltages[0].ToString() + "\n");
                            rtbIncoming.AppendText("Voltage V2: " + Voltages[1].ToString() + "\n");
                            rtbIncoming.AppendText("Voltage V3: " + Voltages[2].ToString() + "\n");
                        }));
                    }
                }
                else if (item == "Temperature")
                {
                    //change cmd
                    OutputData[0] = (UInt16)CMD.TS_CMD_TEMP;

                    //Amd and frequency for current wave
                    OutputData[1] = u16AmpVoltage;

                    //dispatch data to Bytes 
                    Dataformating();

                    try
                    {
                        //ask for currents send 16 bytes to the DSP...
                        ComPort.Write(Txbuffer, 0, Txbuffer.Length);
                    }
                    catch (Exception ex)
                    {
                        rtbIncoming.Invoke(new MethodInvoker(delegate
                        {
                            rtbIncoming.Text = ex.Message + "\n";
                        }));
                    }

                    // wait for information
                    while (false == Ready) ;

                    // clear flag
                    Ready = false;

                    // Read physical values and converd uint16 to int16
                    ReadPhysical((PHYSICAL_VALUES)InputData[0]);

                    // Display data to the GUI
                    if (ComPort.IsOpen)
                    {
                        rtbIncoming.Invoke(new MethodInvoker(delegate
                        {
                            rtbIncoming.AppendText("Temperature : " + Temperature.ToString() + "\n");

                        }));
                    }
                }

            }
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.facebook.com/groups/Tsemrade/?ref=bookmarks");
        }
        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/semrade?tab=repositories");
        }
        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
             System.Diagnostics.Process.Start("https://www.linkedin.com/in/tarik-semrade-7bb702191?lipi=urn%3Ali%3Apage%3Ad_flagship3_profile_view_base_contact_details%3BG%2FfsZ5cUQni%2FZ1%2FXKgl8yg%3D%3D");
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void CurrentAmp_Scroll(object sender, EventArgs e)
        {
            u16AmpCurrent = (UInt16)CurrentAmp.Value;
        }

        private void AmpVoltage_Scroll(object sender, EventArgs e)
        {
            u16AmpVoltage = (UInt16)AmpVoltage.Value;
        }

        private void Frquency_Scroll(object sender, EventArgs e)
        {
            u16FreqCurrent = (UInt16)Frquency.Value;
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.youtube.com/channel/UCWjnD8fxmyqbP5-LvxcsnMQ");
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (rtbIncoming.Text != "")
            {
                rtbIncoming.Text = "";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //this.Hide();
            Form1 graphi = new Form1();
            graphi.Show();


            //display
        }
    }
}
