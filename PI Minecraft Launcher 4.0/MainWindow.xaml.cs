using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Timers;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Net;
using Microsoft.VisualBasic;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Imaging;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace PI_Minecraft_Launcher_4._0
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        //啟動器版本
        string launcherVersion = "4.1.2 (20150929.1)";
        string mcMainVersion = "1.7.10";

        //UI變數
        private const int WM_SYSCOMMAND = 0x112;
        private HwndSource hwndSource;
        private int time = 3;

        //狀態設定
        bool loggedIn = false; bool serverOn = true;
        string playerId = ""; int downloadProg;
        System.Timers.Timer aTimer = null, waitLaunchGame = null;
        delegate void TimerDispatcherDelegate();

        //環境設定
        bool os64Bit = false; bool foundJava = true; bool need64BitJava = false; bool customJava = false;
        string javaPath = ""; string root = ""; double locationx = 0; int WaitCount = 0;
        string maxMem = "1024"; //最大記憶體限制

        //更新或安裝設定
        bool needUpdate = false; bool needInstall = false; bool needUpdateLauncher = false;
        private Thread downloader, initializer;
        private SynchronizationContext dwsync, initsync;
        public SynchronizationContext sync;
        string downloadStatus = "";
        string itemName = "";
        string status = "";
        string board = "公告內容，用\\n換行，像這樣\n看到了嗎?";
        string updateInfo = "更新資訊存入此變數，用法同上";
        string oldTxt = "目前版本";
        string newTxt = "新版本";
        string updateName = "";
        string NextVersion;
        double version = 0;
        string versionText;
        double updateVersion = 0;
        String[] playerOnline = new String[50];   //線上有誰
        int PlayersOnline = 0; //線上玩家數
        String serverAddress = "mcbackend.power.moe";
        String javaVersion = "";
        String[] clientSide;
        string UpdateURL;
        string startUpdir;
        string UpdateFileName;
        System.Timers.Timer MCRunning = null, ServerChecker = null;
        int updateServerStatus = 30;
        bool customScreen = false;
        int width = 854, height = 480;
        private string token = "";
        private string fbName;
        //string UpdateFile;

        private bool formClosing = false;

        WebClient webclient = new WebClient();

        private IE fbLogin;

        //IM功能
        //txtIMessenger：聊天室內容
        //edtIM：訊息輸入框
        //btnIMSubmit：送出鈕

        void submitMessage()
        {
            //送出訊息
            edtIM.Clear();
        }

        void getVersion()
        {
            //取得版本編號，存入version變數
            txtTitleVersion.Content = "Minecraft " + mcMainVersion.ToString() + " | 版本：" + version.ToString();
            txtAboutVersion.Content = "Minecraft " + mcMainVersion.ToString() + " | 版本：" + version.ToString() + "\n啟動器版本：" + launcherVersion;

        }

        void getPlugin()
        {
            //取得plugin列表
            //格式為二維陣列
            /*
             * plugin[0][0] = "名稱"; plugin[0][1] = "網址";
             * 
            */
        }

        void UI_putPlugin()
        {
            lsvPlugin.Items.Clear();
            //將plugin清單加入listview中(取得plugin[0]，也就是名稱欄位即可)
        }
        void InstallPlugin()
        {
            //計算勾選數量
            int n = 1;
            for (int i = 0; i < n; ++i)
            {
                string name = "";//取得勾選的plugin[0]，downloadUrl存入對應網址;
                Download(name);
            }
        }

        void checkServer() //set 方法
        {

            try
            {
                if (GetServerString("status.php") == "ServerOnline")
                {
                    serverOn = true;
                    PlayersOnline = int.Parse(GetServerString("status.php?q=Players"));
                    if (PlayersOnline > 0)
                        playerOnline = GetServerString("Player.php").Split('\n');
                }
                else
                    serverOn = false;

            }
            catch
            {
                serverOn = false;
            }
        }

        bool serverIsRunning() //get方法
        {
            return (serverOn) ? true : false;
        }
        private void SetUpdateInfo() //更新動作SetUpdateInfo()
        {
            try
            {
                string Updatelist = GetServerString("UpdateList.txt");
                foreach (string a in Strings.Split(Updatelist, Microsoft.VisualBasic.Constants.vbCrLf))
                {
                    string PreVersion = Microsoft.VisualBasic.Strings.Split(a, "->")[0];
                    NextVersion = Microsoft.VisualBasic.Strings.Split(a, "->")[1];
                    //MessageBox.Show(PreVersion+"->"+NextVersion);
                    if (PreVersion == versionText)
                    {

                        UpdateURL = "https://mcbackend.power.moe/Update/" + NextVersion + ".exe";
                        UpdateFileName = "Update" + NextVersion + ".exe";
                        updateName = "更新(" + NextVersion + ")";
                        getUpdateInfo();

                    }

                }
            }
            catch
            { }
        }
        void checkUpdate() //set方法
        {

            try
            {
                String[] serverSide = GetServerString("getversion.php").Split('.');
                if (int.Parse(serverSide[0]) > int.Parse(clientSide[0]))
                {
                    needUpdate = true;
                    updateVersion = double.Parse(serverSide[0] + "." + serverSide[1]);
                }
                else
                {
                    if (int.Parse(serverSide[1]) > int.Parse(clientSide[1]))
                    {
                        needUpdate = true;
                        updateVersion = double.Parse(serverSide[0] + "." + serverSide[1]);
                    }
                    else
                    {
                        needUpdate = false;
                    }
                }
            }
            catch
            {

            }
            SetUpdateInfo();

            //thread會自動更新UI，請勿變更UI上的屬性值，否則thread會出錯;
        }
        bool isNeedUpdate() //get方法
        {
            return needUpdate;
        }

        void getBoard()
        {
            try
            {
                //取得公告，將取得的公告字串存入board變數即可，thread會自動更新UI，請勿變更UI上的屬性值，否則thread會出錯;
                //公告內容請使用\n來換行
                board = GetServerString("Board.txt");
            }
            catch
            {
                board = "無法取得公告資訊，請聯絡系統管理員。";
            }
        }

        void getUpdateInfo()
        {
            try
            {
                //取得更新資訊，將取得的公告字串存入updateInfo變數即可，thread會自動更新UI，請勿變更UI上的屬性值，否則thread會出錯;
                //更新資訊請使用\n來換行
                //MessageBox.Show(NextVersion);
                updateInfo = GetServerString("ChangeLog." + NextVersion + ".txt");

                oldTxt = "目前版本：" + versionText;
                newTxt = "新版本：" + NextVersion;

            }
            catch
            {
                updateInfo = "無法取得更新資訊，請聯絡系統管理員。";
                oldTxt = "目前版本：" + versionText;
                newTxt = "新版本：" + NextVersion;
            }
        }

        void varifyAccount()
        {
            UI_VarifyAccount();
            /*string response = System.Text.UnicodeEncoding.UTF8.GetString(System.Convert.FromBase64String(GetServerString("verify.php?user=" + edtUsername.Text + "&pwd=" + GetMD5(pwbPassword.Password))));
            string orgtime = Microsoft.VisualBasic.Strings.Mid(response, 33);
            string timemd5 = Microsoft.VisualBasic.Strings.Mid(response, 1, 32);*/
            fbLogin = new IE(this, false);
            fbLogin.FormClosed += loginClosed;

            fbLogin.Show();
            fbLogin.Location = new System.Drawing.Point(5000, 5000);

        }

        private void loginClosed(object o, FormClosedEventArgs e)
        {

            if (formClosing)
            {
                System.Windows.Application.Current.Shutdown();
            }

            if (!fbLogin.logout)
            {

                if (!loggedIn)
                    UI_LoginFailed();
            }

            fbLogin = null;
        }

        private void UploadHW()
        {
            try
            {
                String macaddr = "";
                String cpuname = "";
                String ramsize = "";
                String vgacard = "";
                String winstr = "";
                String hddmodel = "";
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                        nic.OperationalStatus == OperationalStatus.Up)
                        if (!(nic.Description.Contains("irtual")))
                            macaddr = nic.GetPhysicalAddress().ToString();
                ManagementObjectSearcher mos = new ManagementObjectSearcher("root\\CIMV2",
                    "SELECT * FROM Win32_Processor");
                foreach (ManagementObject mo in mos.Get())
                {
                    cpuname = mo["Name"].ToString();
                }
                ramsize =
                    ((new Microsoft.VisualBasic.Devices.ComputerInfo()).TotalPhysicalMemory/1024/1024).ToString("F0");
                mos = new ManagementObjectSearcher("SELECT * FROM Win32_DisplayConfiguration");


                foreach (ManagementObject mo in mos.Get())
                {
                    foreach (PropertyData property in mo.Properties)
                    {
                        if (property.Name == "Description")
                        {
                            vgacard += property.Value.ToString() + "/";
                        }
                    }
                }
                var disks = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

                hddmodel = disks.Get()
                    .Cast<ManagementObject>()
                    .Aggregate("", (current, disk) => current + (disk["Model"].ToString() + "/"));

                mos = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
                foreach (ManagementObject os in mos.Get())
                {
                    winstr = os["Caption"].ToString();
                    break;
                }
                JObject data = new JObject();
                data.Add("macaddr", macaddr);
                data.Add("name", playerId);
                data.Add("cpuname", cpuname);
                data.Add("ramsize", ramsize);
                data.Add("vgacard", vgacard);
                data.Add("hddmodel", hddmodel);
                data.Add("winstr", winstr);
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                string url;
                url = "https://mcbackend.power.moe/hardware.php";
                string res = wc.UploadString(url,
                    String.Format("data={0}", Uri.UnescapeDataString(System.Convert.ToBase64String(Encoding.UTF8.GetBytes(data.ToString())))));
            }
            catch (Exception e)
            {
            }

        }

        private string getPlayerName(string token)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            string url;
            url = "https://mcbackend.power.moe/userID.php?q=query";
            string res = wc.UploadString(url, String.Format("session={0}", token));
            JObject ret = JObject.Parse((string)res);
            return ret["PlayerName"].ToString();
        }

        public void logIn(Object response)
        {

            try
            {
                JObject ret = JObject.Parse((string)response);
                if (ret["Name"] != null)
                {
                    string pname = getPlayerName(ret["Token"].ToString());
                    token = ret["Token"].ToString();

                    fbName = ret["Name"].ToString();

                    loggedIn = true;
                    if (pname != "")
                    {

                        playerId = pname;
                        UI_Login();
                    }
                    else
                    {
                        edtJoinUsername.Text = ret["Name"].ToString();
                        edtJoinEmail.Text = ret["ID"].ToString() + "/" + ret["Name"].ToString();
                        tabJoin.IsSelected = true;
                    }

                }
                else
                {
                    UI_LoginFailed();
                }

            }
            catch (Exception x)
            {
                UI_ShowStatus("登入系統發生問題，請與管理員聯絡。", "Red", 30);
            }

        }

        bool isLoggedIn() //get 方法
        {
            return (loggedIn) ? true : false;
        }



        void loadSettings()
        {
            if (!Properties.Settings.Default.FirstRun)
            {
                if (Properties.Settings.Default.customJava)
                {
                    javaPath = Properties.Settings.Default.javaPath;
                    customJava = true;
                    rdbBrowseJava.IsChecked = true;
                }
                else
                {
                    customJava = false;
                }
                if (Properties.Settings.Default.customScreen)
                {
                    width = Properties.Settings.Default.width;
                    height = Properties.Settings.Default.height;

                    customScreen = true;
                    rdbCustomResolution.IsChecked = true;
                }
                else
                {
                    customScreen = false;
                }
                /*if (Properties.Settings.Default.RemeberPWD)
                {
                    pwbPassword.Password = System.Text.UnicodeEncoding.UTF8.GetString(System.Convert.FromBase64String(Properties.Settings.Default.Password));
                    cbxSavePasswd.IsChecked = true;
                    if (Properties.Settings.Default.AutoLogin)
                    {
                        cbxAutoLogin.IsChecked = true;
                    }
                }
                edtUsername.Text = Properties.Settings.Default.Username;*/
                edtMem.Text = Properties.Settings.Default.maxMem.ToString();
                cbxAutoLogin.IsChecked = Properties.Settings.Default.AutoLogin;
            }
        }
        void saveSettings()
        {
            try
            {
                if ((bool)cbxAutoLogin.IsChecked)
                    Properties.Settings.Default.AutoLogin = true;
                else
                    Properties.Settings.Default.AutoLogin = false;
                Properties.Settings.Default.maxMem = int.Parse(edtMem.Text);
                /*if ((bool)cbxSavePasswd.IsChecked)
                {
                    Properties.Settings.Default.Password = System.Convert.ToBase64String(System.Text.UnicodeEncoding.UTF8.GetBytes(pwbPassword.Password));
                    Properties.Settings.Default.Username = edtUsername.Text;
                    Properties.Settings.Default.RemeberPWD = true;
                }
                else
                {
                    Properties.Settings.Default.Password = "";
                    Properties.Settings.Default.Username = edtUsername.Text;
                    Properties.Settings.Default.RemeberPWD = false;
                }
                */
                Properties.Settings.Default.javaPath = javaPath;
                Properties.Settings.Default.customJava = customJava;
                Properties.Settings.Default.customScreen = customScreen;
                Properties.Settings.Default.width = width;
                Properties.Settings.Default.height = height;
                Properties.Settings.Default.FirstRun = false;
                Properties.Settings.Default.Save();
            }
            catch
            { }


        }

        bool FBVerify(String fbstr)
        {
            try
            {
                fbstr = fbstr.Replace("https", "http");

                if (fbstr.Contains("profile.php?id="))
                {
                    fbstr = fbstr.Replace("www.facebook.com/profile.php?id=", "graph.facebook.com/");
                    if ((new WebClient()).DownloadString(new Uri(fbstr)).Contains("name"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
                else
                {
                    fbstr = fbstr.Replace("www.facebook.com", "graph.facebook.com");
                    if ((new WebClient()).DownloadString(new Uri(fbstr)).Contains("name"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private JObject setPlayerName(string token)
        {
            string url;
            string res;

            //url = "https://mcbackend.power.moe/userID.php?q=query";
            System.Collections.Specialized.NameValueCollection reqparm = new System.Collections.Specialized.NameValueCollection();
            reqparm.Add("session", token);

            //res = (new System.Text.UTF8Encoding()).GetString((new WebClient()).UploadValues(url, "POST", reqparm));

            reqparm.Add("name", edtJoinId.Text);

            url = "https://mcbackend.power.moe/userID.php?q=update";

            res = (new System.Text.UTF8Encoding()).GetString((new WebClient()).UploadValues(url, "POST", reqparm));
            return JObject.Parse(res);

        }

        void createNewAccount()
        {
            JObject ret = setPlayerName(token);

            switch (int.Parse(ret["Status"].ToString()))
            {
                case 0:
                    {
                        UI_ShowStatus("帳戶已建立完成！", "Green", 5);
                        playerId = edtJoinId.Text;
                        UI_Login();
                        break;
                    }

                case 1:
                    {
                        UI_ShowStatus("遊戲ID已被使用過，請修改ID。", "Red", 30);
                        edtJoinId.Clear();
                        break;
                    }
                case 10:
                    {
                        UI_ShowStatus("帳戶建立失敗，請與管理員聯絡。", "Red", 30);
                        break;
                    }
                case 200:
                    {
                        UI_ShowStatus("遊戲ID有誤，請重新輸入...", "Red", 30);
                        break;
                    }
                case 1000:
                    {
                        UI_ShowStatus("帳戶建立失敗，請與管理員聯絡。", "Red", 30);
                        break;
                    }
            }
            CleanBox();

        }

        void editAccount()
        {

            /*UI_ShowStatus("帳戶變更已完成，請重新登入帳戶。", "Green", 5);
            tabUserSettings.IsSelected = true;
            UI_Logout();*/

            //bool changePWD = false;
            bool changeID = false;
            //bool changedPWD = false;
            bool changedID = false;

            /*if (verifyReturn(GetServerString("verify.php?user=" + edtUsername.Text + "&pwd=" + GetMD5(pwdChangeOldPasswd.Password))))
            {*/
            if (edtChangeNewId.Text != "")
            {
                changeID = true;
                if (GetServerString("checkid.php?id=" + edtChangeNewId.Text) == "OK")
                {
                    //if (verifyReturn(GetServerString("updateid.php?user=" + edtUsername.Text + "&pwd=" + GetMD5(pwdChangeOldPasswd.Password) + "&id=" + edtChangeNewId.Text)))
                    changedID = true;
                }
                else
                {
                    UI_ShowStatus("遊戲 ID 已被使用過，請修改 ID 名稱。", "Red", 30);
                    edtChangeNewId.Clear();
                }
            }
            /*if (pwdChangeNewPasswd.Password != "" || pwdChangeNewConfirmPasswd.Password != "") //新密碼與確認密碼不完全空白
            {
                changePWD = true;
                if (pwdChangeNewPasswd.Password == pwdChangeNewConfirmPasswd.Password)
                {
                    if (verifyReturn(GetServerString("updatepwd.php?user=" + edtUsername.Text + "&pwd=" + GetMD5(pwdChangeNewPasswd.Password) + "&1=" + GetMD5(pwdChangeOldPasswd.Password))))
                        changedPWD = true;
                }
                else
                {
                    UI_ShowStatus("兩次密碼輸入不一致，請重新輸入。", "Red", 30);
                    pwdChangeNewPasswd.Clear(); pwdChangeNewConfirmPasswd.Clear();
                }
            }*/
            if ((changeID && changedID))
            {

                UI_ShowStatus("帳戶變更已完成，請重新登入帳戶。", "Green", 5);
                tabUserSettings.IsSelected = true;
                UI_Logout();
            }
            /*}
            else
            {
                UI_ShowStatus("密碼輸入錯誤！", "Red", 30);
                pwdChangeOldPasswd.Password = "";
            }*/
        }

        void RemoveAccount()
        {
            /*UI_ShowStatus("正在驗證帳戶...", "Yellow");
            string response = System.Text.UnicodeEncoding.UTF8.GetString(System.Convert.FromBase64String(GetServerString("verify.php?user=" + edtUsername.Text + "&pwd=" + GetMD5(pwdRemovePasswd.Password))));
            string orgtime = Microsoft.VisualBasic.Strings.Mid(response, 33);
            string timemd5 = Microsoft.VisualBasic.Strings.Mid(response, 1, 32);

            if (GetMD5((int.Parse(orgtime) + 811526).ToString()) == timemd5)
            {
                UI_ShowStatus("正在刪除帳戶...", "Yellow");
                long timenow = timestamp();
                GetServerString("deleteacc.php?user=" + edtUsername.Text + "&1=" + GetMD5(pwdRemovePasswd.Password) + "&a=" + (timenow + 811526).ToString() + "&b=" + GetMD5(timenow.ToString()));
                UI_ShowStatus("帳戶已刪除完成！", "Green", 3);
                
                

                CleanBox();

                UI_Logout();
                saveSettings();
            }
            else
            {
                UI_ShowStatus("密碼輸入錯誤！", "Red", 30);
            }*/
        }

        void launchGame(bool online)
        {
            saveSettings();
            if (foundJava)
            {
                WaitCount = 0; pbrStatus.Value = 0;
                imgSettings.IsEnabled = false;
                imgProfile.IsEnabled = false;
                imgAbout.IsEnabled = false;
                imgRefresh.IsEnabled = false;
                maxMem = edtMem.Text; //為最大記憶體限制
                if (maxMem == "") { maxMem = "1024"; }
                if (online)
                {
                    UI_ShowStatus("正在以線上模式啟動 Minecraft...", "Green");

                    //locationx = Application.Current.MainWindow.Left;
                    //Application.Current.MainWindow.Left = SystemParameters.PrimaryScreenWidth - this.Width - 3; //將視窗擺放到螢幕右側

                    btnLaunchGame.IsEnabled = false;
                    //執行線上模式，不要結束啟動器
                    waitLaunchGame.Enabled = true; //檢測是否正常啟動
                    cbxStatusDownloading.IsChecked = true;
                    //Minecraft 關閉時
                    //UI_gameClosed(true);
                }
                else
                {
                    UI_ShowStatus("正在以離線模式啟動 Minrecraft...", "Green");
                    btnOfflineLaunch.IsEnabled = false;
                    btnLogin.IsEnabled = false;
                    /*edtUsername.IsEnabled = false;
                    pwbPassword.IsEnabled = false;*/
                    cbxAutoLogin.IsEnabled = false;
                    //cbxSavePasswd.IsEnabled = false;
                    cbxStatusDownloading.IsChecked = true;
                    playerId = "Offline_Player";
                    //執行離線模式，不要結束啟動器
                    waitLaunchGame.Enabled = true; //檢測是否正常啟動

                    //Minecraft 關閉時
                    //
                }
                new Thread(new ThreadStart(UploadHW)).Start();
                try
                {
                    Directory.Delete("minecraft\\assets\\skins", true);
                }
                catch (Exception e)
                {
                }
                string[] startup = new string[4];
                startup[0] = "cd minecraft";
                startup[1] = "SET APPDATA=%CD%\\";
                startup[2] = "del .\\logs\\*.gz";
                startup[3] = "start \"MC17A\" /D \"%appdata%\" \"" + javaPath + "\" -Xmx" + maxMem + "M -XX:PermSize=128M -XX:MaxPermSize=256m -XX:+CMSClassUnloadingEnabled -XX:+UseConcMarkSweepGC -Djava.library.path=%appdata%\\versions\\natives -Dfml.ignorePatchDiscrepancies=true -Dfml.ignoreInvalidMinecraftCertificates=true -cp %appdata%\\libraries\\*;%appdata%\\versions\\1.7.10\\1.7.10.jar net.minecraft.launchwrapper.Launch --username " + playerId + " --version 1.7.10-PowerLiMC --gameDir %appdata% --assetsDir %appdata%\\assets --assetIndex 1.7.10 --uuid " + GetMD5(GetMD5(playerId)) + " --accessToken " + token + " --userProperties {} --userType mojang --tweakClass cpw.mods.fml.common.launcher.FMLTweaker";

                if (customScreen)
                    startup[3] += " --width " + width.ToString() + " --height " + height.ToString();
                File.WriteAllLines("start.cmd", startup, System.Text.Encoding.ASCII);
                Microsoft.VisualBasic.Interaction.Shell("start.cmd", AppWinStyle.Hide);
                UI_gameClosed(false);
            }
            else
            {

                cbxShowNoJava.IsChecked = true;
            }

        }

        void UI_gameClosed(bool online)
        {
            cbxStatusDownloading.IsChecked = false;
            //this.WindowState = WindowState.Normal;
            imgAbout.IsEnabled = true;
            imgSettings.IsEnabled = true;
            imgProfile.IsEnabled = true;
            btnLaunchGame.IsEnabled = true;
            btnOfflineLaunch.IsEnabled = true;
            btnLogin.IsEnabled = true;
            /*edtUsername.IsEnabled = true;
            pwbPassword.IsEnabled = true;*/
            cbxAutoLogin.IsEnabled = true;
            //cbxSavePasswd.IsEnabled = true;
            imgRefresh.IsEnabled = true;
            //if(online)
            //{
            //    Application.Current.MainWindow.Left = locationx;
            //}
            // UI_ShowStatus("Minecraft 已關閉。", "Green",5);
            //waitLaunchGame.Enabled = false;
            pbrStatus.Value = 0;
        }

        private void OnTimedEventLaunch(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new TimerDispatcherDelegate(UI_NoLaunch));
        }



        [DllImport("user32.dll")]
        public static extern int FindWindow(string strclassName, string strWindowName);

        private void OnTimedServerChecker(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new TimerDispatcherDelegate(WatchingServer));
        }

        void WatchingServer()
        {
            txtServerStatus_checking.Visibility = Visibility.Visible;
            txtServerStatus_Online.Content = "伺服器服務中 (" + updateServerStatus + " 秒後重新整理)";
            txtServerStatus_Offline.Content = "無法連線至伺服器 (" + updateServerStatus + " 秒後重新整理)";
            titleOnlinePlayers.Content = "線上玩家名單 (" + PlayersOnline + ")";
            if ((updateServerStatus--) == 0)
            {
                checkServer();
                if (serverIsRunning())
                {
                    UI_ServerOnline();
                    //txtServerStatus_Online.Content = "伺服器服務中 (目前線上人數：" + PlayersOnline.ToString() + " 人)";
                    txtOnlinePlayers.Content = PlayersOnline.ToString();
                    lstOnlinePlayers.Items.Clear();
                    foreach (String s in playerOnline)
                    {
                        lstOnlinePlayers.Items.Add(s);
                    }
                    PlayersOnline = lstOnlinePlayers.Items.Count;

                }
                else
                {
                    UI_ServerOffline();

                    UI_ShowStatus("歡迎使用 PowerLi Minecraft Server！無法連線到伺服器。", "Red");
                }
                updateServerStatus = 120;
            }
        }

        private void OnTimedMCrunning(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new TimerDispatcherDelegate(WatchingMC));
        }
        void WatchingMC()
        {
            if (!(FindWindow(null, "Minecraft 1.7.10") > 0))
            {
                btnChangeIdOrPasswd.IsEnabled = true;
                btnChangeSkin.IsEnabled = true;
                btnRemoveAccount.IsEnabled = true;
                btnLogout.IsEnabled = true;
                imgSettings.IsEnabled = true;
                //this.WindowState = WindowState.Normal;
                UI_ShowStatus("Minecraft 主程式已關閉。", "Green", 5);
                btnLaunchGame.IsEnabled = true;
                MCRunning.Enabled = false;
                cbxShowOnlinePlayers.IsChecked = false;
                btnCloseOnlinePlayers.Visibility = Visibility.Visible;
            }
            else
            {
                if (loggedIn)
                {
                    txtMainStatus1.Content = " Minecraft 線上模式正在執行中...";
                    txtMainStatus2.Content = " Minecraft 線上模式正在執行中...";
                    //System.Windows.Application.Current.Shutdown();

                }
                else
                {
                    txtMainStatus1.Content = " Minecraft 離線模式正在執行中...";
                    txtMainStatus2.Content = " Minecraft 離線模式正在執行中...";
                }
                try
                {
                    File.Delete("start.cmd");
                }
                catch { }
            }
        }

        void UI_NoLaunch()
        {
            WaitCount += 1;
            if (FindWindow(null, "Minecraft 1.7.10") > 0)
            {
                //Minecraft 正常執行
                pbrStatus.Value = 0;
                if (!loggedIn)
                {
                    txtMainStatus1.Content = " 準備啟動 Minecraft 離線模式...";
                    txtMainStatus2.Content = " 準備啟動 Minecraft 離線模式...";
                }
                else
                {
                    txtMainStatus1.Content = " 準備啟動 Minecraft 線上模式...";
                    txtMainStatus2.Content = " 準備啟動 Minecraft 線上模式...";
                }

                btnLaunchGame.IsEnabled = false;
                MCRunning.Enabled = true;

                //this.WindowState = WindowState.Minimized;
                saveSettings();
                cbxStatusDownloading.IsChecked = false;
                waitLaunchGame.Enabled = false;

            }
            else
            {
                txtMainStatus2.Content = "正在等候 Minecraft 主程式啟動 (" + (60 - WaitCount) + ")...";
                btnLaunchGame.IsEnabled = false;
                imgSettings.IsEnabled = false;
                pbrStatus.Value = ((double)WaitCount / 60) * 100;
                btnLogout.IsEnabled = false;
                btnChangeIdOrPasswd.IsEnabled = false;
                btnChangeSkin.IsEnabled = false;
                btnRemoveAccount.IsEnabled = false;
                
            }

            if (WaitCount == 60)
            {
                //Minecraft 於 60 秒後仍未啟動
                btnLogout.IsEnabled = true;
                btnChangeIdOrPasswd.IsEnabled = true;
                btnChangeSkin.IsEnabled = true;
                btnRemoveAccount.IsEnabled = true;
                btnLaunchGame.IsEnabled = true;
                UI_ShowStatus("Minecraft 啟動失敗。", "Red", 5);
                pbrStatus.Value = 0;
                cbxShowNoLaunch.IsChecked = true;
                if (loggedIn)
                {
                    tabUserSettings.IsSelected = true;
                }
                UI_gameClosed(false);
                waitLaunchGame.Enabled = false;
            }
        }

        void DownloadJava(bool is64bit)
        {
            if (is64bit)
            {
                UpdateURL = "https://dl.dropboxusercontent.com/u/24581738/Javax64.exe"; //64位元Java下載點
                UpdateFileName = "Javax64.exe";
                Download("Java 執行環境(64 位元)");

            }
            else
            {
                UpdateURL = "https://dl.dropboxusercontent.com/u/24581738/Javax86.exe"; //32位元Java下載點
                UpdateFileName = "Javax86.exe";
                Download("Java 執行環境(32 位元)");
            }
        }


        void Download(string item)
        {
            btnPluginInstall.IsEnabled = false;
            btnLaunchGame.IsEnabled = false;
            btnOfflineLaunch.IsEnabled = false;
            UI_Download(item);
            itemName = item;
            downloader = null;
            downloader = new Thread(new ThreadStart(DownloadWorker));
            downloader.Start();

        }

        void UpdateGOGO()
        {
            //UI_ShowStatus("正在安裝更新程式...", "Yellow");
            Thread.Sleep(500);
            Interaction.Shell("cmd.exe /c start /wait " + UpdateFileName, AppWinStyle.Hide, true);
            Thread.Sleep(1000);
            try
            {
                File.Delete(UpdateFileName);
            }
            catch { }

            if (File.Exists("Launcher_New.exe"))
            {
                Interaction.Shell("Launcher_New.exe", AppWinStyle.Hide, false);
                Environment.Exit(0);
            }
            else
            {
                Interaction.Shell(Environment.GetCommandLineArgs()[0].Split()[Environment.GetCommandLineArgs()[0].Split().Length - 1]);
                Environment.Exit(0);
            }

        }


        void DownloadWorker()
        {

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(UpdateURL);
            httpRequest.Method = WebRequestMethods.Http.Get;
            HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            double totalLength = Convert.ToDouble(httpResponse.ContentLength);
            double total = Convert.ToDouble(totalLength);
            Stream httpResponseStream = httpResponse.GetResponseStream();
            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            int bytesRead = 0;

            double byteReaded = 0;
            FileStream fileStream = File.Create(UpdateFileName);
            while ((bytesRead = httpResponseStream.Read(buffer, 0, bufferSize)) != 0)
            {
                byteReaded += Convert.ToDouble(bytesRead);
                downloadProg = Convert.ToInt32((byteReaded * 100 / totalLength).ToString("F0"));
                dwsync.Send(new SendOrPostCallback(UI_Thread_Downloading), downloadProg);
                fileStream.Write(buffer, 0, bytesRead);
            }
            fileStream.Close();

            dwsync.Send(new SendOrPostCallback(UI_Thread_DownloadFinished), downloadProg);//下載完成
        }


        void UI_Thread_DownloadFinished(object finish)
        {
            UI_ShowStatus("正在安裝 " + itemName + " ...", "Yellow");
            pbrStatus.Value = 0;
            Thread.Sleep(500);
            Thread update = new Thread(UpdateGOGO);
            update.Start();
            //以下交由 updater.exe 負責更新
            //string updater = "Updater.exe " + UpdateFileName + " " + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName; 


            /*btnLaunchGame.IsEnabled = true;
            btnOfflineLaunch.IsEnabled = true;
            Thread.Sleep(1000);
            //MessageBox.Show(updater);
            //Microsoft.VisualBasic.Interaction.Shell(updater, AppWinStyle.Hide,true);

            //Process.Start("Updater.exe", UpdateFileName + " " + System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(".vshost", ""));
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "Updater.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = "\""+UpdateFileName + "\" " +"\""+ System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName+"\"";
            try { Process.Start(startInfo);this.Close(); }
            catch { UI_ShowStatus("更新失敗：找不到更新程式。", "Red", 10); }*/





        }
        /*
		void UI_Thread_DownloadFinished(object finish)
        {
            UI_ShowStatus("正在準備安裝 " + itemName + " ...", "Yellow");
            pbrStatus.Value = 0;
            if (needUpdateLauncher)
            {

            }
            else 
            {
                UI_ShowStatus("正在安裝 " + itemName + " ...", "Yellow");
                Microsoft.VisualBasic.Interaction.Shell(UpdateFileName, AppWinStyle.Hide,true);
                if (itemName.Contains("Java"))
                {
                    Process.Start(Environment.GetCommandLineArgs()[0].Replace(".vshost", ""));
                    UI_ShowStatus(itemName + " 已安裝完成，正在準備重新啟動...", "Green");
                    Thread.Sleep(1000);
                    Process.GetCurrentProcess().Kill();
                }
                UI_ShowStatus(itemName + " 已安裝完成。", "Green",5);                
            }                        
        }
		*/
        void UI_Download(string itemName)
        {
            cbxStatusDownloading.IsChecked = true;
            downloadStatus = "正在下載 " + itemName;
        }
        void UI_Thread_Downloading(object progress)
        {
            pbrStatus.Value = Int32.Parse(progress.ToString());
            txtMainStatus2.Content = downloadStatus + " (" + progress + "%)...";
        }

        void UI_LoginFailed()
        {
            /*edtUsername.IsEnabled = true;
            pwbPassword.IsEnabled = true;*/
            btnLogin.IsEnabled = true;
            //btnJoin.IsEnabled = true;
            btnOfflineLaunch.IsEnabled = true;
            cbxAutoLogin.IsEnabled = true;
            //cbxSavePasswd.IsEnabled = true;
            UI_ShowStatus("登入失敗...", "Red");

        }

        void searchJava() //set 方法
        {

            if (!customJava)
            {
                if (Is64BitOS())
                {
                    if (Directory.Exists("C:\\Program Files\\Java"))
                    {
                        string[] dirs = Directory.GetDirectories("C:\\Program Files\\Java");
                        for (int i = dirs.Length - 1; i >= 0; i--)
                        {
                            string dir = dirs[i];
                            if (dir.Contains("jre1"))
                            {
                                if (File.Exists(dir + "\\bin\\javaw.exe"))
                                {
                                    FileVersionInfo javaexe = FileVersionInfo.GetVersionInfo(dir + "\\bin\\javaw.exe");
                                    if (javaexe.FileVersion.Contains("8.0"))
                                    {
                                        foundJava = true;
                                        need64BitJava = false;
                                        javaVersion = "8";
                                        javaPath = dir + "\\bin\\javaw.exe";
                                    }

                                }
                            }
                        }
                    }
                    else if (Directory.Exists("C:\\Program Files (x86)\\Java"))
                    {
                        string[] dirs = Directory.GetDirectories("C:\\Program Files (x86)\\Java");
                        for (int i = dirs.Length - 1; i >= 0; i--)
                        {
                            string dir = dirs[i];
                            if (dir.Contains("jre1"))
                            {
                                if (File.Exists(dir + "\\bin\\javaw.exe"))
                                {
                                    FileVersionInfo javaexe = FileVersionInfo.GetVersionInfo(dir + "\\bin\\javaw.exe");
                                    if (javaexe.FileVersion.Contains("8.0"))
                                    {
                                        foundJava = true;
                                        need64BitJava = true;
                                        javaVersion = "8";
                                        javaPath = dir + "\\bin\\javaw.exe";
                                    }

                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Directory.Exists("C:\\Program Files\\Java"))
                    {
                        string[] dirs = Directory.GetDirectories("C:\\Program Files\\Java");
                        for (int i = dirs.Length - 1; i >= 0; i--)
                        {
                            string dir = dirs[i];
                            if (dir.Contains("jre1"))
                            {
                                if (File.Exists(dir + "\\bin\\javaw.exe"))
                                {
                                    FileVersionInfo javaexe = FileVersionInfo.GetVersionInfo(dir + "\\bin\\javaw.exe");
                                    if (javaexe.FileVersion.Contains("8.0"))
                                    {
                                        foundJava = true;
                                        need64BitJava = false;
                                        javaVersion = "8";
                                        javaPath = dir + "\\bin\\javaw.exe";
                                    }

                                }
                            }
                        }
                    }
                }
                if (File.Exists(".\\Java\\bin\\javaw.exe"))
                {
                    need64BitJava = false;
                    foundJava = true;
                    javaVersion = "8";
                    javaPath = ".\\Java\\bin\\javaw.exe";
                }
            }
        }

        string getJavaPath() //get方法
        {
            return javaPath;
        }

        void checkOS64Bit() //set方法
        {
            if (File.Exists(root + "\\windows\\syswow64\\ntdll.dll"))
                os64Bit = true;
            else
                os64Bit = false;
        }

        bool Is64BitOS() //get方法
        {
            return (os64Bit) ? true : false;
        }


        void varifyJoinForm()//只是檢查申請表單內容對不對而已
        {
            if (edtJoinId.Text == "")
            {
                UI_ShowStatus("帳戶資訊不完整，請檢查後再提交。", "Red", 8);
            }
            else
            {
                cbxShowEULA.IsChecked = true;

            }
        }



        public MainWindow()
        {
            startUpdir = Environment.CurrentDirectory;
            if (Environment.GetCommandLineArgs()[0].Split()[Environment.GetCommandLineArgs()[0].Split().Length - 1].Contains("Launcher_New.exe"))
            {
                Interaction.Shell("taskkill.exe /f /im 啟動Minecraft.exe", AppWinStyle.Hide, true);
                Thread.Sleep(500);
                File.Copy("Launcher_New.exe", "啟動Minecraft.exe", true);
                Interaction.Shell("啟動Minecraft.exe", AppWinStyle.Hide, false);
                Environment.Exit(0);
            }
            else if (File.Exists("Launcher_New.exe"))
            {

                Thread.Sleep(500);
                Interaction.Shell("taskkill.exe /f /im Launcher_New.exe", AppWinStyle.Hide, true);
                File.Delete("Launcher_New.exe");
            }
            InitializeComponent();
            versionText = "20140628.1";// ;
            if (!File.Exists(".\\minecraft\\version.dat"))
            {
                File.WriteAllText(".\\minecraft\\version.dat", "20140628.1");
            }
            versionText = File.ReadAllText(".\\minecraft\\version.dat");
            clientSide = versionText.Split('.');
            version = double.Parse(clientSide[0] + "." + clientSide[1]);
            getVersion();

            dwsync = SynchronizationContext.Current;
            sync = SynchronizationContext.Current;
            initsync = SynchronizationContext.Current;
            aTimer = new System.Timers.Timer(1000);
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 1000;
            waitLaunchGame = new System.Timers.Timer(1000);
            waitLaunchGame.Elapsed += new ElapsedEventHandler(OnTimedEventLaunch);
            waitLaunchGame.Interval = 1000;
            MCRunning = new System.Timers.Timer(1000);
            MCRunning.Elapsed += new ElapsedEventHandler(OnTimedMCrunning);
            MCRunning.Interval = 1000;
            ServerChecker = new System.Timers.Timer(1000);
            ServerChecker.Elapsed += new ElapsedEventHandler(OnTimedServerChecker);
            ServerChecker.Interval = 1000;
            this.Closing += new CancelEventHandler(WinClosing);



            UI_init();
        }

        private void WinClosing(object sender, CancelEventArgs e)
        {
            try
            {
                if(!(cbxAutoLogin.IsChecked==true))
                {
                    e.Cancel = true;
                    formClosing = true;
                fbLogin=new IE(this,true);
                fbLogin.FormClosed += loginClosed;
                fbLogin.Show();
                fbLogin.Location = new System.Drawing.Point(5000,5000);
                    
                }
            }
            catch (Exception)
            {
                
                
            }
        }

        void UI_init()
        {
            txtServerStatus_checking.Visibility = Visibility.Visible;
            imgAbout.IsEnabled = false;
            imgProfile.IsEnabled = false;
            imgSettings.IsEnabled = false;
            imgRefresh.IsEnabled = false;
            root = System.Environment.GetEnvironmentVariable("systemdrive");
            cbxAniCheckingServer.IsChecked = true;
            cbxAniCheckingServer.IsChecked = false;
            cbxStatusBarBlink.IsChecked = true;
            cbxStatusBarBlink.IsChecked = false;
            cbxStatusDownloading.IsChecked = true;

            initializer = new Thread(new ThreadStart(Thread_InitDo));
            initializer.Start();

        }

        void Thread_InitDo()
        {

            int prog = 33;
            status = "正在檢查環境...";
            checkOS64Bit();
            dwsync.Send(new SendOrPostCallback(UI_ThreadInit), prog);
            searchJava();
            Thread.Sleep(700);
            prog = 66;
            status = "正在檢查伺服器狀態...";
            dwsync.Send(new SendOrPostCallback(UI_ThreadInit), prog);
            checkServer();
            Thread.Sleep(700);
            prog = 90;
            status = "正在檢查更新...";
            dwsync.Send(new SendOrPostCallback(UI_ThreadInit), prog);
            checkUpdate();
            Thread.Sleep(700);
            prog = 99;
            status = "正在取得資訊...";
            dwsync.Send(new SendOrPostCallback(UI_ThreadInit), prog);
            getBoard();

            getPlugin();

            Thread.Sleep(700);
            status = "正在準備登入頁面...";
            dwsync.Send(new SendOrPostCallback(Thread_InitFinished), prog);

        }

        void UI_ThreadInit(object prog)
        {
            loadSettings();

            UI_ShowStatus(status, "None");
            pbrStatus.Value = Int32.Parse(prog.ToString());
        }

        void Thread_InitFinished(object prog)
        {

            if (!customJava)
                rdbAutoScanJava.IsChecked = true;
            else
                rdbBrowseJava.IsChecked = true;

            if (customScreen)
                rdbCustomResolution.IsChecked = true;
            else
                rdbDefaultResolution.IsChecked = true;

            cbxStatusDownloading.IsChecked = false;
            imgAbout.IsEnabled = true;
            imgProfile.IsEnabled = true;
            imgSettings.IsEnabled = true;
            imgRefresh.IsEnabled = true;
            edtJavaPath.Text = getJavaPath();

            if (needUpdate)
            {
                txtUpdateInfo.Text = updateInfo;

                txtOldVersion.Content = "目前版本：" + versionText;
                txtNewVersion.Content = newTxt;

                cbxShowUpdateInfo.IsChecked = true;
                Download(updateName);
            }
            else
            {
                if (serverIsRunning())
                {

                    UI_ServerOnline();

                    /*else
                    {*/
                    if (!loggedIn)
                    {
                        cbxAniLogin.IsChecked = true;
                        cbxAniLogin.IsChecked = false;
                        tabLogin.IsSelected = true;
                    }
                    UI_putPlugin();

                    UI_ShowStatus("歡迎使用 PowerLi Minecraft Server!", "Green");
                    //txtServerStatus_Online.Content="伺服器服務中 (目前線上人數："+PlayersOnline.ToString()+" 人)";
                    txtOnlinePlayers.Content = PlayersOnline.ToString();
                    lstOnlinePlayers.Items.Clear();
                    foreach (String s in playerOnline)
                    {
                        lstOnlinePlayers.Items.Add(s);
                    }



                    if (!foundJava)
                    {
                        cbxShowNoJava.IsChecked = true;
                    }
                    else
                    {
                        if (need64BitJava)
                        {
                            if (javaVersion == "7")
                            {
                                titleNeed64BitJava.Content = "有新的 Java 版本";
                                txtNeed64BitJava.Text = "系統偵測到您正在使用舊的 Java 環境，\n請點選「升級 Java」來獲得更好的體驗。";
                                btnNeed64JavaInstall.Content = "升級 _Java";
                            }
                            cbxShow64JavaInfo.IsChecked = true;
                        }
                    }
                    //}
                    if ((bool)cbxAutoLogin.IsChecked)
                        varifyAccount();
                }
                else
                {
                    UI_ServerOffline();

                    UI_ShowStatus("歡迎使用 PowerLi Minecraft Server！ 無法連線到伺服器。", "Red");
                }
            }
            pbrStatus.Value = 0;
            ServerChecker.Enabled = true;
        }


        void UI_ShowMsg(string content, string title)
        {
            txtMsgTitle.Content = title;
            txtMsgContent.Text = content;
            cbxShowMsg.IsChecked = true;
        }

        void UI_VarifyAccount()
        {
            btnLogin.IsEnabled = false;
            //btnJoin.IsEnabled = false;
            imgSettings.IsEnabled = false;
            btnOfflineLaunch.IsEnabled = false;
            cbxAutoLogin.IsEnabled = false;
            UI_ShowStatus("正在登入...", "Blue");

        }
        void UI_Login()
        {
            UI_ShowMsg(board, "PowerLi Minecraft Server 公告");
            UI_ShowStatus("登入成功！歡迎 " + fbName, "Green", 3);
            imgProfile.IsEnabled = true;
            tabIMessage.IsEnabled = true;
            tabUserSettings.IsEnabled = true;
            tabLogin.IsEnabled = false;
            txtWelcome.Text = "歡迎您回來，" + fbName;
            txtCurrentId.Text = "您的遊戲 ID 為：" + playerId;
            tabUserSettings.IsSelected = true;
            imgSettings.IsEnabled = true;
            saveSettings();
        }



        private void OnTimedEvent(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new TimerDispatcherDelegate(UI_StatusWelcome));
        }
        void UI_StatusWelcome()
        {
            if (time >= 0)
            {
                time--;
            }
            else
            {
                UI_ShowStatus("歡迎使用 PowerLi Minecraft Server!", "None");
                aTimer.Enabled = false;
            }
        }



        void UI_Logout()
        {
            loggedIn = false;
            imgProfile.IsEnabled = false;

            btnLogin.IsEnabled = true;
            //btnJoin.IsEnabled = true;
            btnOfflineLaunch.IsEnabled = true;
            cbxAutoLogin.IsEnabled = true;

            tabIMessage.IsEnabled = false;
            tabUserSettings.IsEnabled = false;
            tabLogin.IsEnabled = true;
            tabLogin.IsSelected = true;
            UI_ShowStatus("您已順利登出 PowerLi Minecraft Server!", "Green", 3);

        }

        void UI_ServerOnline()
        {
            cbxAniCheckingServer.IsChecked = true;
            cbxAniCheckingServer.IsChecked = false;
            cbxAniOnline.IsChecked = true;
            cbxAniOnline.IsChecked = false;
            btnLogin.IsEnabled = true;
            cbxAutoLogin.IsEnabled = true;
            tabPlugin.IsEnabled = true;
            tabJoin.IsEnabled = true;
            //btnJoin.IsEnabled = true;
            cbxNoConnection.IsChecked = false;
        }
        void UI_ServerOffline()
        {
            UI_ShowStatus("無法連線到伺服器。", "Red", 3);
            /*UI_Logout();
            btnCloseOnlinePlayers.Visibility = Visibility.Visible;
            cbxShowOnlinePlayers.IsChecked = false;
            
            txtOnlinePlayers.Content = "--";
            lstOnlinePlayers.Items.Clear();
            btnOfflineLaunch.IsEnabled = true;
            cbxAniCheckingServer.IsChecked = true;
			cbxAniCheckingServer.IsChecked = false;
            cbxAniOffline.IsChecked = true;
            cbxAniOffline.IsChecked = false;
            btnLogin.IsEnabled = false;
			//btnJoin.IsEnabled = false;
            cbxAutoLogin.IsEnabled = false;
            tabPlugin.IsEnabled = false;
            tabJoin.IsEnabled = false;
            tabLogin.IsSelected = true;
            cbxNoConnection.IsChecked = true;*/
        }

        void UI_ShowStatus(string msg, string color, int timer)
        {
            UI_ShowStatus(msg, color);
            time = timer;
            aTimer.Enabled = true;
        }


        void UI_ShowStatus(string msg, string color)
        {
            string s = txtMainStatus2.Content.ToString();
            txtMainStatus1.Content = s;
            txtMainStatus2.Content = msg;
            cbxAniShowTitleStatus.IsChecked = true;
            cbxAniShowTitleStatus.IsChecked = false;
            switch (color)
            {
                case "Green":
                    cbxStatusGreen.IsChecked = true;
                    cbxStatusGreen.IsChecked = false;
                    break;
                case "Yellow":
                    cbxStatusYellow.IsChecked = true;
                    cbxStatusYellow.IsChecked = false;
                    break;
                case "Red":
                    cbxStatusRed.IsChecked = true;
                    cbxStatusRed.IsChecked = false;
                    break;
                case "None":

                    break;
            }

        }


        /// 此處開始為頁面切換程式碼
        private void imgProfile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isLoggedIn())
            {
                tabUserSettings.IsSelected = true;
            }
            else
            {
                tabLogin.IsSelected = true;
            }
        }

        private void imgMessenger_MouseDown(object sender, MouseButtonEventArgs e)
        {
            tabIMessage.IsSelected = true;
        }

        private void imgAbout_MouseDown(object sender, MouseButtonEventArgs e)
        {
            tabAbout.IsSelected = true;
        }

        private void imgSettings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            tabSetting.IsSelected = true;
        }

        private void btnJoin_Click(object sender, RoutedEventArgs e)
        {
            tabJoin.IsSelected = true;
        }

        private void btnChangeIdOrPasswd_Click(object sender, RoutedEventArgs e)
        {
            pwdChangeNewConfirmPasswd.Clear();
            pwdChangeNewPasswd.Clear();
            pwdChangeOldPasswd.Clear();
            edtChangeNewId.Text = "";
            tabChange.IsSelected = true;
        }

        private void btnCancelChange_Click(object sender, RoutedEventArgs e)
        {
            tabUserSettings.IsSelected = true;
            pwdChangeNewConfirmPasswd.Clear();
            pwdChangeNewPasswd.Clear();
            pwdChangeOldPasswd.Clear();
            edtChangeNewId.Text = "";
        }

        private void btnChangeSave_Click(object sender, RoutedEventArgs e)
        {
            editAccount();

        }
        /// 此處為頁面切換程式碼的結尾

        /// 此處開始為視窗可被滑鼠左鍵移動的程式碼
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            hwndSource = PresentationSource.FromVisual((Visual)sender) as HwndSource;
            hwndSource.AddHook(new HwndSourceHook(WndProc));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //Debug.WriteLine("WndProc messages: " + msg.ToString());

            if (msg == WM_SYSCOMMAND)
            {
                //Debug.WriteLine("WndProc messages: " + msg.ToString());
            }

            return IntPtr.Zero;
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private void ResetCursor(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (System.Windows.Input.Mouse.LeftButton != MouseButtonState.Pressed)
            {
                this.Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        /// 此處為視窗可被滑鼠左鍵移動的程式碼的結尾

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            fbLogin = new IE(this, true);
            fbLogin.FormClosed += loginClosed;
            fbLogin.Show();
            fbLogin.Location = new System.Drawing.Point(5000, 5000);
            UI_Logout();
        }



        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            varifyAccount();
        }

        private void btnJoinSubmit_Click(object sender, RoutedEventArgs e)
        {
            varifyJoinForm();

        }

        private void btnCancelJoin_Click(object sender, RoutedEventArgs e)
        {
            tabLogin.IsSelected = true;
        }

        private void btnPlugin_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            tabPlugin.IsSelected = true;
        }

        private void btnTest_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            tabTest.IsSelected = true;
        }

        private void btnTestTitleStatus_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            UI_ShowStatus(edtStatusTitle.Text.ToString(), "Green");
        }

        private void btnTestOnline_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            serverOn = true;
            UI_init();
        }

        private void btnTestOffline_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            serverOn = false;
            UI_init();
        }

        private void btnTestCheckServer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            cbxAniCheckingServer.IsChecked = true;
            cbxAniCheckingServer.IsChecked = false;
            cbxStatusBarBlink.IsChecked = true;
            cbxStatusBarBlink.IsChecked = false;
        }

        private void btnTestDownload_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            updateName = "測試更新功能";
            needUpdate = true;
            serverOn = true;
            UI_init();
        }

        private void imgRefresh_Click(object sender, MouseButtonEventArgs e)
        {
            txtServerStatus_checking.Visibility = Visibility.Visible;
            UI_init();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            cbxNoConnection.IsChecked = false;
            UI_init();
        }

        private void btnTestNoJava_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            foundJava = false;
            cbxNoConnection.IsChecked = false;
            UI_init();
        }

        private void btnTestNeed64Java_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            cbxNoConnection.IsChecked = false;
            foundJava = true;
            need64BitJava = true;
            UI_init();
        }

        private void btnRemoveConf_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RemoveAccount();
        }
        private void btnRemoveAccount_Click(object sender, RoutedEventArgs e)
        {
            tabRemove.IsSelected = true;
        }

        private void btnTestLoginFailed_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            UI_LoginFailed();
        }

        private void btnPluginInstall_Click(object sender, System.Windows.RoutedEventArgs e)
        {

            InstallPlugin();

        }

        private void edtJoinId_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            edtJoinId.Text = edtJoinId.Text.Replace(" ", "");
        }

        private void edtChangeNewId_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            edtChangeNewId.Text = edtChangeNewId.Text.Replace(" ", "");
        }

        private void btnJoinAgree_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            cbxShowEULA.IsChecked = false;
            createNewAccount();
        }

        private void btnJoinDisagree_Agree(object sender, System.Windows.RoutedEventArgs e)
        {
            cbxShowEULA.IsChecked = false;
        }

        private void btnChangeSkin_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.RestoreDirectory = true;
            ofd.DefaultExt = ".png";
            ofd.Filter = "Minecraft 角色 Skin (*.png)|*.png";
            if ((bool)ofd.ShowDialog())
            {
                
                FileInfo fi = new FileInfo(ofd.FileName);
                if (fi.Length <= (6 * 1024 * 1024))
                {
                    try
                    {
                        string skinpath = ofd.FileName;
                        Bitmap bm = new Bitmap(skinpath);
                        if ((bm.Height == 32 || bm.Height==64) && bm.Width == 64)
                        {
                            if (bm.Height == 64)
                            {
                                Bitmap image = new Bitmap(64, 32);
                                using (Graphics graphics = Graphics.FromImage(image))
                                {
                                    graphics.DrawImage(bm, new System.Drawing.Rectangle(0, 0, 64, 64));
                                    graphics.Dispose();
                                }
                                image.Save("new.png",ImageFormat.Png);
                                fi=new FileInfo("new.png");
                                skinpath = "new.png";

                            }

                            string b64img = System.Convert.ToBase64String(File.ReadAllBytes(skinpath));
                            System.Collections.Specialized.NameValueCollection reqparm = new System.Collections.Specialized.NameValueCollection();
                            reqparm.Add("img", b64img);
                            byte[] responsebytes = webclient.UploadValues("https://mcbackend.power.moe/skin.php?id=" + playerId + "&l=" + fi.Length.ToString(), "POST", reqparm);
                            string result = (new System.Text.UTF8Encoding()).GetString(responsebytes);
                            if (result == "OK")
                            {
                                UI_ShowStatus("角色外觀上傳成功，請重新啟動遊戲來套用變更", "Green", 5);
                            }
                            else
                            {
                                UI_ShowStatus("圖片格式錯誤(請聯絡管理員)", "Red", 8);
                            }
                        }
                        else
                        {
                            UI_ShowStatus("圖片大小錯誤(長寬須為64x32)", "Red", 8);
                        }
                    }
                    catch
                    {
                        UI_ShowStatus("圖片格式錯誤", "Red", 8);
                    }
                }
                else
                {
                    UI_ShowStatus("圖片大小錯誤(檔案必須小於6KB)", "Red", 8);
                }
                try
                {
                    File.Delete("new.png");
                }
                catch (Exception x)
                {
                }
            }
        }

        private void imgFacebook_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Interaction.Shell("cmd.exe /c start https://www.facebook.com/groups/139970582874999/");
        }

        private void btnBrowseJava_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.RestoreDirectory = true;
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".exe";
            dlg.Filter = "Java 執行檔 (javaw.exe)|javaw.exe";
            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                javaPath = dlg.FileName;
                edtJavaPath.Text = javaPath;
                customJava = true;
                rdbBrowseJava.IsChecked = true;
                saveSettings();
            }
        }

        private void btnOfflineLaunch_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            launchGame(false);
        }

        private void btnLaunchGame_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            launchGame(true);
        }

        private void btnNeed64JavaInstall_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DownloadJava(true);
            cbxShow64JavaInfo.IsChecked = false;
        }

        private void btnNeed64JavaIgnore_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            cbxShow64JavaInfo.IsChecked = false;
        }

        private void btnNoJavaInstall_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Is64BitOS()) DownloadJava(true);
            else DownloadJava(false);
            cbxShowNoJava.IsChecked = false;
        }

        private void btnNoJavaSelectPath_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".exe";
            dlg.Filter = "Java 執行檔 (javaw.exe)|javaw.exe";
            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                javaPath = dlg.FileName;
                edtJavaPath.Text = javaPath;
                customJava = true;
                cbxShowNoJava.IsChecked = false;
                rdbBrowseJava.IsChecked = true;
                saveSettings();
                MessageBox.Show("Java路徑已設定\n請重新打開啟動器!!");
                Environment.Exit(0);
            }
        }

        private void btnNoJavaCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            cbxShowNoJava.IsChecked = false;
        }

        private void btnNoLaunchGameSetting_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            tabSetting.IsSelected = true;
            cbxShowNoLaunch.IsChecked = false;
        }

        private void btnNoLaunchGameOK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            cbxShowNoLaunch.IsChecked = false;
        }

        private void edtMem_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            saveSettings();
        }

        private void rdbAutoScanJava_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            saveSettings();
        }

        private void rdbBrowseJava_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (javaPath == "" || edtJavaPath.Text == "")
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                // Set filter for file extension and default file extension 
                dlg.DefaultExt = ".exe";
                dlg.Filter = "Java 執行檔 (javaw.exe)|javaw.exe";
                // Display OpenFileDialog by calling ShowDialog method 
                Nullable<bool> result = dlg.ShowDialog();

                // Get the selected file name and display in a TextBox 
                if (result == true)
                {
                    // Open document 
                    javaPath = dlg.FileName;
                    edtJavaPath.Text = javaPath;
                    customJava = true;
                    cbxShowNoJava.IsChecked = false;
                    rdbBrowseJava.IsChecked = true;
                    saveSettings();
                    MessageBox.Show("Java路徑已設定\n請重新打開啟動器!!");
                    Environment.Exit(0);
                }

            }
        }

        private void edtJavaPath_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            saveSettings();
        }

        private void rdbDefaultResolution_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            saveSettings();
        }

        private void rdbCustomResolution_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            saveSettings();
        }

        private void edtResolutionX_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            saveSettings();
        }

        private void edtResolutionY_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            saveSettings();
        }

        private void btnIMSubmit_Click(object sender, RoutedEventArgs e)
        {
            submitMessage();
        }

        private void txtIMessenger_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            edtIM.Focus();
        }

        string GetServerString(string q)
        {
            try
            {
                return webclient.DownloadString("https://" + serverAddress + "/" + q);
            }
            catch
            {
                return "";
            }

        }

        string GetMD5(string input)
        {
            byte[] data = System.Security.Cryptography.MD5.Create().ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i <= data.Length - 1; i++)
                sBuilder.Append(data[i].ToString("x2"));

            return sBuilder.ToString();
        }
        long timestamp()
        {
            return (DateTime.UtcNow.Ticks - DateTime.Parse("01/01/1970 00:00:00").Ticks) / 10000000;
        }
        void CleanBox()
        {
            edtChangeNewId.Text = "";
            edtJoinEmail.Text = "";
            edtJoinId.Text = "";
            edtJoinUsername.Text = "";
            pwdChangeNewConfirmPasswd.Password = "";
            pwdChangeNewPasswd.Password = "";
            pwdChangeOldPasswd.Password = "";
            pwdJoinPasswd.Password = "";
            pwdJoinPasswdConf.Password = "";
            pwdRemovePasswd.Password = "";
            cbxAutoLogin.IsChecked = false;
        }

        private void btnMsgOK_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            cbxHideMsg.IsChecked = true;
            btnLaunchGame.IsDefault = true;
        }

        bool verifyReturn(string input)
        {
            string response = System.Text.UnicodeEncoding.UTF8.GetString(System.Convert.FromBase64String(input));
            string orgtime = Microsoft.VisualBasic.Strings.Mid(response, 33);
            string timemd5 = Microsoft.VisualBasic.Strings.Mid(response, 1, 32);

            if (GetMD5((int.Parse(orgtime) + 811526).ToString()) == timemd5)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void btnCloseOnlinePlayers_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            cbxShowOnlinePlayers.IsChecked = false;
        }

        private void imgOnline_Click(object sender, MouseButtonEventArgs e)
        {
            cbxShowOnlinePlayers.IsChecked = true;
        }

        private void txtOnlinePlayers_Click(object sender, MouseButtonEventArgs e)
        {
            cbxShowOnlinePlayers.IsChecked = true;
        }


    }
}
