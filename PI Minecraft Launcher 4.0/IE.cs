using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PI_Minecraft_Launcher_4._0
{
    public partial class IE : Form
    {
        [DllImport("urlmon.dll", CharSet = CharSet.Ansi)]
        private static extern int UrlMkSetSessionOption(
            int dwOption, string pBuffer, int dwBufferLength, int dwReserved);

        const int URLMON_OPTION_USERAGENT = 0x10000001;
        const int URLMON_OPTION_USERAGENT_REFRESH = 0x10000002;

        public bool logout = false;
        private MainWindow form;

        public IE(MainWindow form, bool logout)
        {
            InitializeComponent();
            this.form = form;
            this.logout = logout;
            ChangeUserAgent();

            SHDocVw.WebBrowser axBrowser = (SHDocVw.WebBrowser)this.webBrowser1.ActiveXInstance;
            axBrowser.NavigateError += new SHDocVw.DWebBrowserEvents2_NavigateErrorEventHandler(axBrowser_NavigateError);

        }

        private void IE_Load(object sender, EventArgs e)
        {
            //this.Location = new Point(5000, 5000);

            if (!logout)
            {
                webBrowser1.Navigate("https://mcbackend.power.moe/login.php");
            }
            else
            {
                webBrowser1.Navigate("https://mcbackend.power.moe/logout.php");
            }

        }

        public void ChangeUserAgent()
        {
            string ua = "Mozilla/5.0 (Windows; U; Windows CE; Mobile; PLMCLauncher Build#150802 ; zh-TW ; zh_TW ; zh-tw) AppleWebKit/533.3 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.3";

            UrlMkSetSessionOption(URLMON_OPTION_USERAGENT_REFRESH, null, 0, 0);
            UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, ua, ua.Length, 0);
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

            if (e.Url.ToString().Contains("facebook.com"))
            {
                this.CenterToScreen();
            }

            if (e.Url.ToString().StartsWith("https://mcbackend.power.moe/callLauncher.php"))
            {
                string ret = webBrowser1.DocumentText;

                if (ret.Contains("graph.facebook.com"))
                {
                    form.sync.Send(new SendOrPostCallback(form.logIn), ret);
                }

                HtmlDocument htmlDocument = this.webBrowser1.Document;
                htmlDocument.Window.Unload += new HtmlElementEventHandler(CloseWindow);

                webBrowser1.Navigate("https://mcbackend.power.moe/closeWindow.php");
            }

        }
        void axBrowser_NavigateError(object pDisp, ref object URL, ref object Frame, ref object StatusCode, ref bool Cancel)
        {

            if (StatusCode.ToString() == "403")
            {
                webBrowser1.Navigate("https://mcbackend.power.moe/callLauncher.php");
            }
        }
        void CloseWindow(object sender, HtmlElementEventArgs e)
        {
            this.Close();
        }

        private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            timer1.Enabled = true;
        }

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            this.Text = "登入視窗";
            timer1.Enabled = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            switch (this.Text.Length)
            {
                case 3:
                    {
                        this.Text = "處理中.";
                        break;
                    }
                case 4:
                    {
                        this.Text = "處理中..";
                        break;
                    }
                case 5:
                    {
                        this.Text = "處理中...";
                        break;
                    }
                default:
                    {
                        this.Text = "處理中";
                        break;
                    }
            }

        }

    }
}
