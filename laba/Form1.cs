using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Net;
using System.Windows.Forms.Design;
using System.Threading;

namespace laba
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        bool resolveNames = false;
        private void button1_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            richTextBox1.Clear();
            NetworkInterface[] Interfaces = NetworkInterface.GetAllNetworkInterfaces();
            
            resolveNames = checkBox1.Checked;
            foreach (NetworkInterface Interface in Interfaces )
            {
                if (Interface.OperationalStatus != OperationalStatus.Up) continue;
                if (Interface.GetIPProperties().GatewayAddresses.Any())
                {
                    richTextBox1.AppendText(Interface.GetPhysicalAddress().ToString() + "\n");
                    if( resolveNames )
                        richTextBox1.AppendText(System.Net.Dns.GetHostName() + "\n"); 
                    richTextBox1.AppendText(Interface.Description.ToString() + "\n\n");
                    UnicastIPAddressInformationCollection UnicastIPInfoCol = Interface.GetIPProperties().UnicastAddresses;

                    foreach (UnicastIPAddressInformation UnicatIPInfo in UnicastIPInfoCol)
                    {
                        richTextBox1.AppendText("\tIP Address is" + UnicatIPInfo.Address.ToString() + "\n");
                        richTextBox1.AppendText("\tSubnet Mask is" + UnicatIPInfo.IPv4Mask.ToString() + "\n");
                        UInt32[] interval = GetInterval(UnicatIPInfo.Address.ToString(), UnicatIPInfo.IPv4Mask.ToString());
                        richTextBox1.AppendText("\tstart" + UInt32ToIPAddress(interval[0]).ToString() + "\n");
                        richTextBox1.AppendText("\tEnd" + UInt32ToIPAddress(interval[1]).ToString() + "\n");
                        IPAddress adr;
                        for (UInt32 i = interval[0]; i < interval[1]; i++) {
                            adr = UInt32ToIPAddress(i);
                            Ping p = new Ping();

                            p.PingCompleted += new PingCompletedEventHandler(p_PingCompleted);
                            p.SendAsync(adr, 2000, adr);
                        }

                   } 
                }
            }

        }
        public static IPAddress UInt32ToIPAddress(UInt32 address)
        {
            return new IPAddress(new byte[] { 
                (byte)((address>>24) & 0xFF) ,
                (byte)((address>>16) & 0xFF) , 
                (byte)((address>>8)  & 0xFF) , 
                (byte)( address & 0xFF)});
        }
        private UInt32[] GetInterval(String adr, String mas)
        {
            UInt32 address = BitConverter.ToUInt32(IPAddress.Parse(adr).GetAddressBytes(), 0);
            UInt32 mask = BitConverter.ToUInt32(IPAddress.Parse(mas).GetAddressBytes(), 0);
            UInt32 start = (address & mask); 
            byte[] star = BitConverter.GetBytes(start);
            star[3]++; 
            Array.Reverse(star);
            start = BitConverter.ToUInt32(star, 0);
            UInt32 end = (address | (~mask));
            byte[] en = BitConverter.GetBytes(end);
            en[3]--; 
            Array.Reverse(en);
            end = BitConverter.ToUInt32(en,0);
            UInt32[] res=new UInt32[]{start,end};
            return res;
        }
        public void p_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            string ip = e.UserState.ToString();
            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                
                ListViewItem item = new ListViewItem(GetMacAddress(ip));
                item.SubItems.Add(ip);
                richTextBox1.AppendText(ip+" is up\n");

                if (resolveNames)
                {
                    string name;
                    try
                    {
                        IPHostEntry hostEntry = Dns.GetHostEntry(ip);
                        name = hostEntry.HostName;
                    }
                    catch (Exception ex)
                    {
                        name = "?";
                    }
                    item.SubItems.Add(name);
                }
                else {
                    item.SubItems.Add("?");
                }
                item.SubItems.Add(Thread.CurrentThread.ManagedThreadId.ToString());
                listView1.Items.Add(item);
            }
            else if (e.Reply == null)
            {
                
            }
            ((Ping)sender).Dispose();

        }
        public static string GetMacAddress(string ipAddress)
        {
            var macAddress = string.Empty;
            var pProcess = new System.Diagnostics.Process();

            pProcess.StartInfo.FileName = "arp";
            pProcess.StartInfo.Arguments = "-a " + ipAddress;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.Start();

            string strOutput = pProcess.StandardOutput.ReadToEnd();
            string[] substrings = strOutput.Split('-');

            if (substrings.Length >= 8)
            {
                macAddress = substrings[3].Substring(Math.Max(0, substrings[3].Length - 2))
                         + "-" + substrings[4] + "-" + substrings[5] + "-" + substrings[6]
                         + "-" + substrings[7] + "-"
                         + substrings[8].Substring(0, 2);
                return macAddress;
            }
            else
            {
                return "not found";
            }
        }
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}