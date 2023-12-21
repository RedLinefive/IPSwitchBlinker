
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ip.Form1;

namespace ip
{
    public partial class Form1 : Form
    {
        private Timer blinkTimer;
        private string[] interfaceNames = { "eth1", "eth2" };
        private int currentInterfaceIndex = 0;

        public Form1()
        {
       

            StartPosition = FormStartPosition.Manual;
            Location = new Point((Screen.PrimaryScreen.Bounds.Width - Width) / 2, 0);

            InitializeComponent();

           

            // Initialize and configure the blink timer
            blinkTimer = new Timer();
            blinkTimer.Interval = 500; // Set the interval for blinking (500 milliseconds in this example)
            blinkTimer.Tick += BlinkTimer_Tick;

  
            UpdateIPAddress();
        }
        private void StartBlinking()
        {
            blinkTimer.Start();
        }

        private void StopBlinking()
        {
            blinkTimer.Stop();
            labelIPAddress.BackColor = SystemColors.Control; // Reset the label background color
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            // Toggle the label background color to create a blinking effect
            labelIPAddress.BackColor = (labelIPAddress.BackColor == Color.Yellow) ? SystemColors.Control : Color.Yellow;
            label2.BackColor = (labelIPAddress.BackColor == Color.DarkRed) ? SystemColors.Control : Color.DarkGreen;
            label1.BackColor = (labelIPAddress.BackColor == Color.DarkGreen) ? SystemColors.Control : Color.DarkRed;
        }

        private async void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F7)
            {
                StartBlinking(); // Start the blinking effect
                try
                {
                    await ToggleNetworkInterface();
                    await DisplayIPAddress();
                 
                }
                finally
                {
                    StopBlinking(); // Stop the blinking effect
                }
            }
        }




        private async 
        Task
ToggleNetworkInterface()
        {
            string currentInterface = interfaceNames[currentInterfaceIndex];
            string nextInterface = interfaceNames[(currentInterfaceIndex + 1) % interfaceNames.Length];
            label1.Text = currentInterface + " OFF";
            label2.Text = nextInterface + " ON";
            // Release the IP for the current interface
            await ReleaseRenewIP(currentInterface, "");

            // Other code for toggling network interface

            // Renew the IP for the next interface
            await ReleaseRenewIP("", nextInterface);

            // Ensure that you update the current interface index after toggling
            currentInterfaceIndex = (currentInterfaceIndex + 1) % interfaceNames.Length;
      
        }

        private async Task ReleaseRenewIP(string releaseInterface, string renewInterface)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(releaseInterface))
                    {
                        ProcessStartInfo releasePsi = new ProcessStartInfo("ipconfig", $"/release \"{releaseInterface}\"")
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        Process releaseProcess = Process.Start(releasePsi);
                        releaseProcess.WaitForExit();
                    }

                    if (!string.IsNullOrEmpty(renewInterface))
                    {
                        ProcessStartInfo renewPsi = new ProcessStartInfo("ipconfig", $"/renew \"{renewInterface}\"")
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        Process renewProcess = Process.Start(renewPsi);
                        renewProcess.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    SetLabelIPAddress(" NO INTERNET " + ex.ToString(), Color.Chocolate); ;
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }



        private void SetLabelIPAddress(string text, Color color)
        {
            if (labelIPAddress.InvokeRequired)
            {
                labelIPAddress.Invoke(new Action(() =>
                {
                    labelIPAddress.Text = text;
                    labelIPAddress.ForeColor = color;
                }));
            }
            else
            {
                labelIPAddress.Text = text;
                labelIPAddress.ForeColor = color;
            }
        }

        private void DisplayPingStatus(IPStatus status, long roundtripTime)
        {

            // Assuming you have a label named labelPingStatus to display ping status
            if (labelPingStatus.InvokeRequired)
            {
                labelPingStatus.Invoke(new Action(() =>
                {
                    labelPingStatus.Text = $"Ping Status: {status}, Roundtrip Time: {roundtripTime} ms";
                }));
            }
            else
            {
                labelPingStatus.Text = $"Ping Status: {status}, Roundtrip Time: {roundtripTime} ms";
            }
        }
        private async Task DisplayIPAddress()
        {


            string currentInterface = interfaceNames[currentInterfaceIndex];
            string nextInterface = interfaceNames[(currentInterfaceIndex + 1) % interfaceNames.Length];
            try
            {
                Ping ping = new Ping();
                PingReply reply = await ping.SendPingAsync("google.com", 3000);
                DisplayPingStatus(reply.Status, reply.RoundtripTime);
            }
            catch (Exception ex)
            {
                SetLabelIPAddress(currentInterface.ToUpper() +" NO INTERNET " +ex.ToString(), Color.Blue); ;
                DisplayPingStatus(IPStatus.Unknown, -1);
            }

            label1.Text = currentInterface + " ON";
            label2.Text = nextInterface + " OFF";

            try
            {

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string web = await new WebClient().DownloadStringTaskAsync("https://kinsta.com/tools/what-is-my-ip/");
                int f = web.IndexOf("clipboard.writeText( ");
                string ip = web.Substring(f);
                int l = ip.IndexOf(")");
                ip = ip.Substring(0, l).Replace("clipboard.writeText(", "").Replace(")", "").Trim();
                string location = ExtractElement(web, "Location");
                 SetLabelIPAddress($"Your IP Address: {ip}\nLocation: {location}", location.Contains("Lebanon") ? Color.Green : Color.Red);
  
            }
            catch (Exception ex)
            {
                labelIPAddress.Text = $"Error: {ex.Message}";
                labelIPAddress.ForeColor = Color.Red; // Set color to red in case of an error
            }
        }



        static string ExtractElement(string html, string elementName)
        {
            try
            {
                string pattern = $"<dt>{elementName}</dt>\\s*<dd>(.*?)</dd>";
                Regex regex = new Regex(pattern);
                Match match = regex.Match(html);

                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
                else
                {
                    return "Element not found";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting element: {ex.Message}");
                return null;
            }
        }


        private async void UpdateIPAddress()
        {


            while (true)
            {
                await DisplayIPAddress();

                await Task.Delay(1000); // Update every 1 second
            }
        }
     
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
          
            // Draw the blue border
            using (Pen borderPen = new Pen(Color.Blue, 5))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
               
            }
        }
    }
}
