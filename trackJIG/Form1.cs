using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using Papouch.Communication;
using CoreScanner;

namespace trackJIG
{
    public partial class Form1 : Form
    {
        //Vytvoření komunikačního kanálu pro jednotku Quido
        private ICommunicationInterface ci;
        //Vytvoření instance mohulu Quido
        public Papouch.Spinel.Spinel97.Device.Quido.Quido MyDevice;
        //Vytvoření instance pro řízení scaneru
        public CCoreScannerClass cCoreScannerClass;
     
        string fileName;
        string result;
        string DMC;
        string QR;
        string finalResult;
        string symbol;

        Counter counter;

        public Form1()
        {
            InitializeComponent();
            counter = new Counter();
            //Vytvoření reference pro Scanner
            cCoreScannerClass = new CoreScanner.CCoreScannerClass();
            //Inicializace a připojení modulu Quido
            ci = new CiSerialPort();
            ci.ConfigString = "provider=SERIAL_PORT;PortName=COM5;BaudRate=115200;";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            short[] scannerTypes = new short[1];
            scannerTypes[0] = 1;
            short numerOfScannerTypes = 1;
            int status;

            if (!ci.Open(true))
                { 
                MessageBox.Show("Spojení s modulem QUIDO se nepodařilo otevřít! Ověřte nastavení COM portu a restartujte aplikaci.");
                checkBox1.Enabled = false;
                checkBox2.Enabled = false;
                }
            MyDevice = new Papouch.Spinel.Spinel97.Device.Quido.Quido(ci, 0xFE);
            if (MyDevice == null) MessageBox.Show("Zařízení se nepovedlo inicializovat!");

            //Přihlášení pro odběr události na modulu Quido a na Scaneru
            MyDevice.OnInputChange += new Papouch.Spinel.Spinel97.Device.Quido.Quido.EventQuidoInputChange(OnInputChange);
            cCoreScannerClass.BarcodeEvent += new _ICoreScannerEvents_BarcodeEventEventHandler(OnBarcodeEvent);

            MyDevice.StartListen();
            //Otevření připojených scanerů
            cCoreScannerClass.Open(0, scannerTypes, numerOfScannerTypes, out status);
                       
            int opcode = 1001;
            string outXML;
            string inXML = "<inArgs>"+"<cmdArgs>"+"<arg-int>1</arg-int>"+"<arg-int>1</arg-int>"+"</cmdArgs>"+"</inArgs>";
            cCoreScannerClass.ExecCommand(opcode, ref inXML, out outXML, out status);

            checkBox4.Checked = true;
            counter.updateCounts(DateTime.Now.ToString("ddMM"));
            labelPocetZaDen.Text = (counter.getPocetZaDen()).ToString();
            labelPocetCelkem.Text = (counter.getPocetCelkem()).ToString();
            ztlumBeeper();
            zakazQRcode();
            zakazDMCcode();
        }
        private void OnInputChange(Papouch.Spinel.Spinel97.Device.Quido.Quido quido, int index, bool old_stat, bool new_stat)
        {
            //Při změně stavu vstupu
            if (index == 1 && new_stat == false)
                this.Invoke((MethodInvoker)delegate { checkBox2.Checked = true; });
            if (index == 1 && new_stat == true)
                this.Invoke((MethodInvoker)delegate { checkBox2.Checked = false; });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //reset počítadla zaDen
            fileName = @"C:\TXT\Data\counters.txt";
            counter.zaDen = 0;
            string[] lines;
            lines = System.IO.File.ReadAllLines(fileName);
            lines[1] = counter.zaDen.ToString();
            System.IO.File.WriteAllLines(fileName, lines);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            //Reset počítadla Celkem
            fileName = @"C:\TXT\Data\counters.txt";
            counter.celkem = 0;
            string[] lines;
            lines = System.IO.File.ReadAllLines(fileName);
            lines[3] = counter.celkem.ToString();
            System.IO.File.WriteAllLines(fileName, lines);
        }

        void OnBarcodeEvent(short eventType, ref string pscanData)
        {
            result = ShowBarcodeLabel(pscanData);
            symbol = CheckSymbology(pscanData);
        }
        
        // Metoda pro určení typu skenováného barCodu
        private string CheckSymbology(string strXML)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(strXML);

            string strData = String.Empty;
            string symbol = xmlDoc.DocumentElement.GetElementsByTagName("datatype").Item(0).InnerText;
            
            return symbol;
        }
        // Metoda pro převod čistých dat ze skeneru na číselný výstup
        private string ShowBarcodeLabel(string strXML)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(strXML);

            string strData = String.Empty;
            string barcode = xmlDoc.DocumentElement.GetElementsByTagName("datalabel").Item(0).InnerText;
            string[] numbers = barcode.Split(' ');

            foreach (string number in numbers)
            {
                if (String.IsNullOrEmpty(number))
                {
                    break;
                }
                strData += ((char)Convert.ToInt32(number, 16)).ToString();
            }
            return strData;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (checkBox3.Checked && checkBox3.Enabled == false)
            {
                string text = checkBox3.Text;
                if (text == "Tracking...") checkBox3.Text = "Tracking   ";
                if (text == "Tracking   ") checkBox3.Text = "Tracking.  ";
                if (text == "Tracking.  ") checkBox3.Text = "Tracking.. ";
                if (text == "Tracking.. ") checkBox3.Text = "Tracking...";
            }
            string dateTxt = DateTime.Now.ToString("ddMM");
            fileName = @"C:\TXT\" + dateTxt + ".txt";
            aktualDateLabel.Text = DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString();
            counter.updateCounts(DateTime.Now.ToString("ddMM"));
            labelPocetZaDen.Text = (counter.getPocetZaDen()).ToString();
            labelPocetCelkem.Text = (counter.getPocetCelkem()).ToString();
        }

        private void ztlumBeeper()
        {
            int status;
            int opcode = 5004;
            string outXML;
            string inXML = "<inArgs>" + "<scannerID>"+numericUpDown1.Value+"</scannerID>" +
                                "<cmdArgs>" +
                                    "<arg-xml>" +
                                        "<attrib_list>" +
                                            "<attribute>" +
                                                //ztlumení Beeperu
                                                "<id>140</id>" +
                                                "<datatype>B</datatype>" +
                                                "<value>2</value>" +
                                            "</attribute>" +
                                        "</attrib_list>" +
                                    "</arg-xml>" +
                                "</cmdArgs>" +
                            "</inArgs>";
            cCoreScannerClass.ExecCommand(opcode, ref inXML, out outXML, out status);
        }
        private void zakazQRcode()
        {
            int status;
            int opcode = 5004;
            string outXML;
            string inXML = "<inArgs>" + "<scannerID>" + numericUpDown1.Value + "</scannerID>" +
                                "<cmdArgs>" +
                                    "<arg-xml>" +
                                        "<attrib_list>" +
                                            "<attribute>" +
                                                //čislo Attributu z ZEBRA SCANNER SDK ATTRIBUTE DATA DICTIONARY
                                                //Enable a Disable čtení DataMatrix (292 a 293)
                                                "<id>293</id>" +
                                                "<datatype>F</datatype>" +
                                                "<value>False</value>" +
                                            "</attribute>" +
                                        "</attrib_list>" +
                                    "</arg-xml>" +
                                "</cmdArgs>" +
                            "</inArgs>";
            cCoreScannerClass.ExecCommand(opcode, ref inXML, out outXML, out status);
        }
        private void povolQRcode()
        {
            int status;
            int opcode = 5004;
            string outXML;
            string inXML = "<inArgs>" + "<scannerID>" + numericUpDown1.Value + "</scannerID>" +
                                "<cmdArgs>" +
                                    "<arg-xml>" +
                                        "<attrib_list>" +
                                            "<attribute>" +
                                                //čislo Attributu z ZEBRA SCANNER SDK ATTRIBUTE DATA DICTIONARY
                                                //Enable a Disable čtení DataMatrix (292 a 293)
                                                "<id>293</id>" +
                                                "<datatype>F</datatype>" +
                                                "<value>True</value>" +
                                            "</attribute>" +
                                        "</attrib_list>" +
                                    "</arg-xml>" +
                                "</cmdArgs>" +
                            "</inArgs>";
            cCoreScannerClass.ExecCommand(opcode, ref inXML, out outXML, out status);
        }
        private void zakazDMCcode()
        {
            int status;
            int opcode = 5004;
            string outXML;
            string inXML = "<inArgs>" + "<scannerID>" + numericUpDown1.Value + "</scannerID>" +
                                "<cmdArgs>" +
                                    "<arg-xml>" +
                                        "<attrib_list>" +
                                            "<attribute>" +
                                                //čislo Attributu z ZEBRA SCANNER SDK ATTRIBUTE DATA DICTIONARY
                                                //Enable a Disable čtení DataMatrix (292 a 293)
                                                "<id>292</id>" +
                                                "<datatype>F</datatype>" +
                                                "<value>False</value>" +
                                            "</attribute>" +
                                        "</attrib_list>" +
                                    "</arg-xml>" +
                                "</cmdArgs>" +
                            "</inArgs>";
            cCoreScannerClass.ExecCommand(opcode, ref inXML, out outXML, out status);
        }
        private void povolDMCcode()
        {
            int status;
            int opcode = 5004;
            string outXML;
            string inXML = "<inArgs>" + "<scannerID>" + numericUpDown1.Value + "</scannerID>" +
                                "<cmdArgs>" +
                                    "<arg-xml>" +
                                        "<attrib_list>" +
                                            "<attribute>" +
                                                //čislo Attributu z ZEBRA SCANNER SDK ATTRIBUTE DATA DICTIONARY
                                                //Enable a Disable čtení DataMatrix (292 a 293)
                                                "<id>292</id>" +
                                                "<datatype>F</datatype>" +
                                                "<value>True</value>" +
                                            "</attribute>" +
                                        "</attrib_list>" +
                                    "</arg-xml>" +
                                "</cmdArgs>" +
                            "</inArgs>";
            cCoreScannerClass.ExecCommand(opcode, ref inXML, out outXML, out status);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked) 
            {
                groupBox1.Enabled = true;
                numericUpDown1.Enabled = true;
                numericUpDown2.Enabled = true;
                numericUpDown3.Enabled = true;
            }
            if (checkBox1.Checked)
            {
                groupBox1.Enabled = false;
                numericUpDown1.Enabled = false;
                numericUpDown2.Enabled = false;
                numericUpDown3.Enabled = false;
            }
        }

        public void ZapisText (string fileName, string finalResult) 
        {
            FileStream fs = new FileStream(fileName, FileMode.Append);

            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(finalResult);
                if (listBox1.Items.Count>3) listBox1.Items.RemoveAt(3); 
                listBox1.Items.Insert(0,"\t\t"+finalResult);
            }
        }

        private async void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked && !checkBox2.Checked)
            {
                zakazQRcode();
                zakazDMCcode();
            }

            if (checkBox1.Checked && checkBox2.Checked)
            {
                SkenujDMC(cCoreScannerClass);
                await Task.Delay(Decimal.ToInt32(numericUpDown2.Value));
                if (result == null) result = "-noDMCread-";
                if (symbol == null) symbol = "noSymbology";
                if (symbol == "27") symbol = "DMC";
                
                labelDMC.Text = symbol;
                DMC = result;
                DMCcode.Text = result;
                result = null;
                symbol = null;

                SkenujQR(cCoreScannerClass);
                await Task.Delay(Decimal.ToInt32(numericUpDown3.Value));
                if (result == null) result = "QRErr";
                if (symbol == null) symbol = "noSymbology";
                if (symbol == "28") symbol = "QR";

                labelQR.Text = symbol;
                QR = result;
                QRcode.Text = result;
                result = null;
                symbol= null;

                finalResult = DateTime.Now.ToString("dd.MM.yy-HH:mm:ss\t") + checkSmena() + QR +"\t\t" + "DMC: " + DMC + "\t\t" + checkOperator();
                label4.Text = finalResult;
                ZapisText(fileName, finalResult);
                zastavSken(cCoreScannerClass);
                counter.increseCounts(DateTime.Now.ToString("ddMM"));
                labelPocetZaDen.Text = (counter.getPocetZaDen()).ToString();
                labelPocetCelkem.Text = (counter.getPocetCelkem()).ToString();
            }
        }
        private string checkSmena()
        {
            string smena = "";
            if (radioButton1.Checked) smena = " <A> ";
            if (radioButton2.Checked) smena = " <B> ";
            if (radioButton3.Checked) smena = " <C> ";
            if (radioButton4.Checked) smena = " <-> ";
            return smena;
        }

        private string  checkOperator()
        {
            string operat = string.Empty;
            if (textBox1.Text == string.Empty) operat = "-Operator";
            else operat = textBox1.Text;
            return operat;
        }

        private void SkenujDMC(CCoreScanner cs)
        {
            zakazQRcode();
            povolDMCcode();
            int status;
            int opcode = 2011; // Method for Trigger ON
            string outXML; // Output
            string inXML = "<inArgs>" + "<scannerID>" + numericUpDown1.Value + "</scannerID>" + "</inArgs>";
            cs.ExecCommand(opcode, ref inXML, out outXML, out status);
        }
        private void SkenujQR(CCoreScanner cs)
        {
            zakazDMCcode();
            povolQRcode();
            int status;
            int opcode = 2011; // Method for Trigger ON
            string outXML; // Output
            string inXML = "<inArgs>" + "<scannerID>" + numericUpDown1.Value + "</scannerID>" + "</inArgs>";
            cs.ExecCommand(opcode, ref inXML, out outXML, out status);
        }
        private void zastavSken(CCoreScanner cs)
        {
            zakazDMCcode();
            zakazQRcode();
            int status;
            int opcode = 2012; // Method for Trigger ON
            string outXML; // Output
            string inXML = "<inArgs>" + "<scannerID>" + numericUpDown1.Value + "</scannerID>" + "</inArgs>";
            cs.ExecCommand(opcode, ref inXML, out outXML, out status);
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                if (radioButton4.Checked) MessageBox.Show("Nebyla vybrána žádná směna!");
                checkBox3.Enabled = false;
                checkBox3.Text = "Tracking...";
                checkBox4.Checked = false;
                checkBox4.Text = "STOP Track";
                checkBox4.Enabled = true;
                checkBox1.Checked = true;
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                checkBox4.Enabled = false;
                checkBox4.Text = "Track is OFF";
                checkBox3.Checked = false;
                checkBox3.Text = "START Track";
                checkBox3.Enabled = true;
                checkBox1.Checked = false;
            }
        }
    }
    public class Counter
    {
        string fileName = @"C:\TXT\Data\counters.txt";
        string[] lines;
        
        public int zaDen { get; set; }
        public int celkem { get; set; }
        public string date { get; set; }

        public Counter()
        {
            lines = System.IO.File.ReadAllLines(fileName);
            zaDen = Int32.Parse(lines[1]);
            celkem = Int32.Parse(lines[3]);
        }

        public void updateCounts(string dt)
        {
            date = File.GetLastWriteTime(fileName).ToString("ddMM");
            if (date != dt) zaDen = 0;
        }
        public void increseCounts(string dt)
        {
            if (date != dt)
            {
                date = dt;
                zaDen = 0;
            }
            zaDen++;
            celkem++;
            lines = System.IO.File.ReadAllLines(fileName);
            lines[1] = zaDen.ToString();
            lines[3] = celkem.ToString();
            System.IO.File.WriteAllLines(fileName, lines);
        }
        public int getPocetZaDen()
        {
            return zaDen;
        }
        public int getPocetCelkem()
        { 
            return celkem;
        }
    }
}
