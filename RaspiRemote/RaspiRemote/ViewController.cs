using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Foundation;
using UIKit;

namespace RaspiRemote
{
    public partial class ViewController : UIViewController
    {
        const string SW_TV_ON = "#SW_TV_ON#";
        const string SW_AIRCON_ON = "#SW_AIRCON_ON#";
        const string SW_SYNC_ON = "#SW_SYNC_ON#";

        const string SW_TV_OFF = "#SW_TV_OFF#";
        const string SW_AIRCON_OFF = "#SW_AIRCON_OFF#";
        const string SW_SYNC_OFF = "#SW_SYNC_OFF#";

        const string RUNONCE_TV = "#RUNONCE_TV#";
        const string RUNONCE_AIRCON = "#RUNONCE_AIRCON#";
        const string RUNONCE_SYNC = "#RUNONCE_SYNC#";
        const string RUNONCE_PIC = "#RUNONCE_PIC#";

        const string RETURN_OK = "#RETURN_OK#";
        const string RETURN_NG = "#RETURN_NG#";

        const string DATA_FIELD = "#DATA_-_FIELD#";

        const int CHECK_SERVER_INTERVAL = 3000;

        const int TIMEOUT_CHECK_STATUS = 3000;
        const int TIMEOUT_GET_SET_SW = 3000;
        const int TIMEOUT_RUNONCE_SCRIPT = 15000;
        const int TIMEOUT_GET_PIC = 5000;

        const string SERVER_URL = "http://YOUR URL";

        const string REQUEST_HEAD_FUNC = "FUNC";
        const string REQUEST_HEAD_TARGET = "TARGET";

        static bool isServerConnected;
        static object threadLock = new object();
        static Timer checkServer;

        static class REQUEST_TYPE
        {
            public const string STATUS = "STATUS";
            public const string GET_SW = "GETSW";
            public const string SET_SW = "SETSW";
            public const string RUN_ONCE = "RUNONCE";
            public const string GET_PIC = "GETPIC";
        }

        public enum SERVER_STATUS
        {
            SERVER_CONNECTED = 0,
            SERVER_DISCONNECTED,
            SERVER_CONNECTING
        }

        public enum SWITCH_ITEMS
        {
            SWITCH_TV = 0,
            SWITCH_AIRCON,
            SWITCH_SYNC
        }

        public enum ACTION_ITEMS
        {
            ACTION_TV = 0,
            ACTION_AIRCON,
            ACTION_SYNC,
            ACTION_PIC
        }

        protected ViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.

            UpdateServerStatus(SERVER_STATUS.SERVER_CONNECTING);
            isServerConnected = false;
            statusBox.Text = string.Empty;
            statusBox.BackgroundColor = UIColor.Black;
            statusBox.TextColor = UIColor.Green;

            checkServer = new Timer(CheckServerThread, null, 50, CHECK_SERVER_INTERVAL);
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        public void CheckServerThread(object state)
        {
            bool isLocked = false;

            try
            {
                Monitor.TryEnter(threadLock, ref isLocked);
                if (!isLocked)
                {
                    return;
                }

                checkServer.Change(Timeout.Infinite, Timeout.Infinite);

                CheckServerConnection();

                GetSWData();
            }
            finally
            {
                if (isLocked)
                {
                    Monitor.Exit(threadLock);
                    checkServer.Change(CHECK_SERVER_INTERVAL, CHECK_SERVER_INTERVAL);
                }
            }
        }

        public UIImage GetImageFromURL(string inputURL)
        {
            HttpWebRequest httpReq = null;
            HttpWebResponse retResponse = null;

            if (string.IsNullOrWhiteSpace(inputURL))
            {
                return null;
            }

            try
            {
                httpReq = WebRequest.Create(inputURL) as HttpWebRequest;

                httpReq.Method = "GET";
                httpReq.ContentType = "charset=UTF-8";
                httpReq.UserAgent = "";
                httpReq.Timeout = TIMEOUT_GET_PIC;

                retResponse = httpReq.GetResponse() as HttpWebResponse;

                NSData nsImage = NSData.FromStream(retResponse.GetResponseStream());

                return UIImage.LoadFromData(nsImage);
            }
            catch (Exception ex)
            {
                PrintOnScreenLog(ex.Message);
                return null;
            }
            finally
            {
                if (retResponse != null)
                {
                    retResponse.Dispose();
                }
            }
        }

        public KeyValuePair<bool, ArrayList> SendRequest(Dictionary<string, string> inputDic, int inputTimeout)
        {
            KeyValuePair<bool, ArrayList> ret = new KeyValuePair<bool, ArrayList>();
            HttpWebRequest httpReq = null;
            HttpWebResponse retResponse = null;
            StreamReader retStream = null;

            try
            {
                string paramRaw = string.Empty;

                foreach (KeyValuePair<string, string> eachParam in inputDic)
                {
                    paramRaw += eachParam.Key + "=" + eachParam.Value + "&";
                }
                paramRaw = paramRaw.TrimEnd('&');
                string connectString = SERVER_URL + EncodeMe(paramRaw);

                httpReq = WebRequest.Create(connectString) as HttpWebRequest;

                httpReq.Method = "GET";
                httpReq.ContentType = "charset=UTF-8";
                httpReq.UserAgent = "";
                httpReq.Timeout = inputTimeout;

                retResponse = httpReq.GetResponse() as HttpWebResponse;
                retStream = new StreamReader(retResponse.GetResponseStream());

                string retRawData = retStream.ReadToEnd();
                bool retStatus = false;
                ArrayList retText = new ArrayList();

                foreach (string eachLine in retRawData.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    if (eachLine.Trim().Equals(RETURN_OK))
                    {
                        retStatus = true;
                    }
                    else if (eachLine.Trim().Equals(RETURN_NG))
                    {
                        retStatus = false;
                    }
                    else
                    {
                        retText.Add(eachLine.Trim());
                    }
                }

                ret = new KeyValuePair<bool, ArrayList>(retStatus, retText);
            }
            catch (Exception ex)
            {
                ret = new KeyValuePair<bool, ArrayList>(false, new ArrayList(){ ex.Message });
            }
            finally
            {
                if (retStream != null)
                {
                    retStream.Close();
                    retStream = null;
                }

                if (retResponse != null)
                {
                    retResponse.Dispose();
                }
            }

            return ret;
        }

        public ArrayList HandleServerRet(string inputReq, ArrayList inputArray)
        {
            string emptyString = string.Empty;

            return HandleServerRet(inputReq, inputArray, out emptyString);
        }

        public ArrayList HandleServerRet(string inputReq, ArrayList inputArray, out string outputDataField)
        {
            outputDataField = string.Empty;
            ArrayList outputArray = new ArrayList();

            foreach (string eachLine in inputArray)
            {
                if (inputReq.Equals(REQUEST_TYPE.STATUS))
                {
                    outputArray.Add(eachLine);
                }
                else if (inputReq.Equals(REQUEST_TYPE.GET_SW))
                {
                    if (eachLine.Trim().Equals(SW_TV_ON))
                    {
                        ChangeSW(SWITCH_ITEMS.SWITCH_TV, true);
                    }
                    else if (eachLine.Trim().Equals(SW_AIRCON_ON))
                    {
                        ChangeSW(SWITCH_ITEMS.SWITCH_AIRCON, true);
                    }
                    else if (eachLine.Trim().Equals(SW_SYNC_ON))
                    {
                        ChangeSW(SWITCH_ITEMS.SWITCH_SYNC, true);
                    }
                    else if (eachLine.Trim().Equals(SW_TV_OFF))
                    {
                        ChangeSW(SWITCH_ITEMS.SWITCH_TV, false);
                    }
                    else if (eachLine.Trim().Equals(SW_AIRCON_OFF))
                    {
                        ChangeSW(SWITCH_ITEMS.SWITCH_AIRCON, false);
                    }
                    else if (eachLine.Trim().Equals(SW_SYNC_OFF))
                    {
                        ChangeSW(SWITCH_ITEMS.SWITCH_SYNC, false);
                    }
                    else
                    {
                        outputArray.Add(eachLine);
                    }
                }
                else if (inputReq.Equals(REQUEST_TYPE.SET_SW))
                {
                    outputArray.Add(eachLine);
                }
                else if (inputReq.Equals(REQUEST_TYPE.RUN_ONCE))
                {
                    if (eachLine.StartsWith(DATA_FIELD, StringComparison.Ordinal) && eachLine.EndsWith(DATA_FIELD, StringComparison.Ordinal))
                    {
                        outputDataField = eachLine.Replace(DATA_FIELD, string.Empty);
                    }
                    else
                    {
                        outputArray.Add(eachLine);
                    }
                }
                else if (inputReq.Equals(REQUEST_TYPE.GET_PIC))
                {
                    outputArray.Add(eachLine);
                }
            }

            return outputArray;
        }

        public void CheckServerConnection()
        {
            if (!isServerConnected)
            {
                UpdateServerStatus(SERVER_STATUS.SERVER_CONNECTING);
            }

            Dictionary<string, string> paramDic = new Dictionary<string, string>
            {
                { REQUEST_HEAD_FUNC, REQUEST_TYPE.STATUS }
            };

            KeyValuePair<bool, ArrayList> retKeyPair = SendRequest(paramDic, TIMEOUT_CHECK_STATUS);

            if (!retKeyPair.Key && isServerConnected)
            {
                isServerConnected = false;
                UpdateServerStatus(SERVER_STATUS.SERVER_DISCONNECTED);
            }
            if (!retKeyPair.Key && !isServerConnected)
            {
                isServerConnected = false;
                UpdateServerStatus(SERVER_STATUS.SERVER_CONNECTING);
            }
            else if (retKeyPair.Key && !isServerConnected)
            {
                isServerConnected = true;
                UpdateServerStatus(SERVER_STATUS.SERVER_CONNECTED);
            }
            else
            {
                return;
            }

            PrintOnScreenLog(HandleServerRet(REQUEST_TYPE.STATUS, retKeyPair.Value ));
        }

        public void GetSWData()
        {
            if (!isServerConnected)
            {
                return;
            }

            Dictionary<string, string> paramDic = new Dictionary<string, string>
            {
                { REQUEST_HEAD_FUNC, REQUEST_TYPE.GET_SW }
            };

            KeyValuePair<bool, ArrayList> retKeyPair = SendRequest(paramDic, TIMEOUT_GET_SET_SW);
            if (retKeyPair.Key)
            {
                HandleServerRet(REQUEST_TYPE.GET_SW, retKeyPair.Value);
            }
            else
            {
                PrintOnScreenLog("Failed To Get SW Info.");
            }
        }

        public void SetSWData(SWITCH_ITEMS inputSW, bool inputStatus)
        {
            if (!isServerConnected)
            {
                return;
            }

            Dictionary<string, string> paramDic = new Dictionary<string, string>
            {
                { REQUEST_HEAD_FUNC, REQUEST_TYPE.SET_SW }
            };

            if (inputStatus)
            {
                switch (inputSW)
                {
                    case SWITCH_ITEMS.SWITCH_TV:
                        paramDic.Add(REQUEST_HEAD_TARGET, SW_TV_ON);
                        break;
                    case SWITCH_ITEMS.SWITCH_AIRCON:
                        paramDic.Add(REQUEST_HEAD_TARGET, SW_AIRCON_ON);
                        break;
                    case SWITCH_ITEMS.SWITCH_SYNC:
                        paramDic.Add(REQUEST_HEAD_TARGET, SW_SYNC_ON);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (inputSW)
                {
                    case SWITCH_ITEMS.SWITCH_TV:
                        paramDic.Add(REQUEST_HEAD_TARGET, SW_TV_OFF);
                        break;
                    case SWITCH_ITEMS.SWITCH_AIRCON:
                        paramDic.Add(REQUEST_HEAD_TARGET, SW_AIRCON_OFF);
                        break;
                    case SWITCH_ITEMS.SWITCH_SYNC:
                        paramDic.Add(REQUEST_HEAD_TARGET, SW_SYNC_OFF);
                        break;
                    default:
                        break;
                }
            }

            KeyValuePair<bool, ArrayList> retKeyPair = SendRequest(paramDic, TIMEOUT_GET_SET_SW);
            if (retKeyPair.Key)
            {
                PrintOnScreenLog(HandleServerRet(REQUEST_TYPE.SET_SW, retKeyPair.Value));
            }
            else
            {
                PrintOnScreenLog("Failed To Set SW Info.");
            }
        }

        partial void swSync_Changed(UISwitch sender)
        {
            SetSWData(SWITCH_ITEMS.SWITCH_SYNC, ((UISwitch)sender).On);
        }

        partial void swAircon_Changed(UISwitch sender)
        {
            SetSWData(SWITCH_ITEMS.SWITCH_AIRCON, ((UISwitch)sender).On);
        }

        partial void swTV_Changed(UISwitch sender)
        {
            SetSWData(SWITCH_ITEMS.SWITCH_TV, ((UISwitch)sender).On);
        }

        public void UpdateServerStatus(SERVER_STATUS inputStatus)
        {
            InvokeOnMainThread(() =>
            {
                labelTitle.Text = "Status: ";
                switch (inputStatus)
                {
                    case SERVER_STATUS.SERVER_CONNECTED:
                        labelTitle.Text += "Connected";
                        break;
                    case SERVER_STATUS.SERVER_DISCONNECTED:
                        labelTitle.Text += "Disconnected";
                        break;
                    case SERVER_STATUS.SERVER_CONNECTING:
                        labelTitle.Text += "Connecting";
                        break;
                    default:
                        labelTitle.Text += "Unknown";
                        break;
                }
            });
        }

        public void ChangeSW(SWITCH_ITEMS inputSW, bool inputStatus)
        {
            InvokeOnMainThread(() =>
            {
                switch (inputSW)
                {
                    case SWITCH_ITEMS.SWITCH_TV:
                        swTV.SetState(inputStatus, true);
                        break;
                    case SWITCH_ITEMS.SWITCH_AIRCON:
                        swAircon.SetState(inputStatus, true);
                        break;
                    case SWITCH_ITEMS.SWITCH_SYNC:
                        swSync.SetState(inputStatus, true);
                        break;
                    default:
                        break;
                }
            });
        }

        public void PrintOnScreenLog(string inputText)
        {
            PrintOnScreenLog( new ArrayList{ inputText } );
        }

        public void PrintOnScreenLog(ArrayList inputArray)
        {
            InvokeOnMainThread(() => {
                statusBox.Text += DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss]") + Environment.NewLine;
                foreach (string eachLine in inputArray)
                {
                    if (string.IsNullOrWhiteSpace(eachLine)) continue;

                    statusBox.Text += (eachLine.Trim() + Environment.NewLine);
                }

                statusBox.Text += Environment.NewLine;
                statusBox.ScrollRangeToVisible(new Foundation.NSRange(statusBox.Text.Length, 0));
            });
        }

        partial void clickPic(UIButton sender)
        {
            var uiAlert = UIAlertController.Create("RaspiRemote", "Take a picture from camera, confirm?", UIAlertControllerStyle.Alert);

            uiAlert.AddAction(UIAlertAction.Create("Yes", UIAlertActionStyle.Default, alert => SendRunOnce(ACTION_ITEMS.ACTION_PIC)));
            uiAlert.AddAction(UIAlertAction.Create("No", UIAlertActionStyle.Cancel, null));

            PresentViewController(uiAlert, true, null);
        }

        partial void clickSync(UIButton sender)
        {
            var uiAlert = UIAlertController.Create("RaspiRemote", "Run sync script, confirm?", UIAlertControllerStyle.Alert);

            uiAlert.AddAction(UIAlertAction.Create("Yes", UIAlertActionStyle.Default, alert => SendRunOnce(ACTION_ITEMS.ACTION_SYNC)));
            uiAlert.AddAction(UIAlertAction.Create("No", UIAlertActionStyle.Cancel, null));

            PresentViewController(uiAlert, true, null);
        }

        partial void clickTV(UIButton sender)
        {
            var uiAlert = UIAlertController.Create("RaspiRemote", "Run TV script, confirm?", UIAlertControllerStyle.Alert);

            uiAlert.AddAction(UIAlertAction.Create("Yes", UIAlertActionStyle.Default, alert => SendRunOnce(ACTION_ITEMS.ACTION_TV)));
            uiAlert.AddAction(UIAlertAction.Create("No",  UIAlertActionStyle.Cancel, null ));

            PresentViewController(uiAlert, true, null);
        }

        partial void clickAircon(UIButton sender)
        {
            var uiAlert = UIAlertController.Create("RaspiRemote", "Run aircon script, confirm?", UIAlertControllerStyle.Alert);

            uiAlert.AddAction(UIAlertAction.Create("Yes", UIAlertActionStyle.Default, alert => SendRunOnce(ACTION_ITEMS.ACTION_AIRCON)));
            uiAlert.AddAction(UIAlertAction.Create("No", UIAlertActionStyle.Cancel, null));

            PresentViewController(uiAlert, true, null);
        }

        public void SendRunOnce(ACTION_ITEMS inputAction)
        {
            if (!isServerConnected)
            {
                return;
            }

            Dictionary<string, string> paramDic = new Dictionary<string, string>
            {
                { REQUEST_HEAD_FUNC, REQUEST_TYPE.RUN_ONCE }
            };

            switch (inputAction)
            {
                case ACTION_ITEMS.ACTION_TV:
                    paramDic.Add(REQUEST_HEAD_TARGET, RUNONCE_TV);
                    break;
                case ACTION_ITEMS.ACTION_AIRCON:
                    paramDic.Add(REQUEST_HEAD_TARGET, RUNONCE_AIRCON);
                    break;
                case ACTION_ITEMS.ACTION_SYNC:
                    paramDic.Add(REQUEST_HEAD_TARGET, RUNONCE_SYNC);
                    break;
                case ACTION_ITEMS.ACTION_PIC:
                    paramDic.Add(REQUEST_HEAD_TARGET, RUNONCE_PIC);
                    break;
                default:
                    break;
            }

            KeyValuePair<bool, ArrayList> retKeyPair = SendRequest(paramDic, TIMEOUT_RUNONCE_SCRIPT);

            if (inputAction == ACTION_ITEMS.ACTION_PIC)
            {
                string dataField = string.Empty;
                PrintOnScreenLog(HandleServerRet(REQUEST_TYPE.RUN_ONCE, retKeyPair.Value, out dataField));

                string paramRaw = REQUEST_HEAD_FUNC + "=" + REQUEST_TYPE.GET_PIC + "&";
                paramRaw += REQUEST_HEAD_TARGET + "=" + dataField;
                string connectString = SERVER_URL + EncodeMe(paramRaw);

                UIImage webcamImage = GetImageFromURL(connectString);

                if (webcamImage != null)
                {
                    webcamImage.SaveToPhotosAlbum((image, error) => { 
                        if (error != null)
                        {
                            PrintOnScreenLog("Failed Saving Picture.");
                        }
                        else
                        {
                            PrintOnScreenLog("Saved to Photo Album.");

                            var uiAlert = UIAlertController.Create("RaspiRemote", "Image Saved to Photos Album.", UIAlertControllerStyle.Alert);

                            uiAlert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));

                            PresentViewController(uiAlert, true, null);
                        }
                    });
                }
                else
                {
                    PrintOnScreenLog("Failed To Get Picture.");
                }
            }
            else
            {
                PrintOnScreenLog(retKeyPair.Value);
            }
        }

        public string EncodeMe(string inputString)
        {
            string originalBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(inputString));

            return Convert.ToBase64String(Encoding.UTF8.GetBytes("0" + originalBase64));
        }

        public string DecodeMe(string inputString)
        {
            string step1Base64 = Encoding.UTF8.GetString(Convert.FromBase64String(inputString));
            return Encoding.UTF8.GetString(Convert.FromBase64String(step1Base64.Substring(1)));
        }

    }
}
