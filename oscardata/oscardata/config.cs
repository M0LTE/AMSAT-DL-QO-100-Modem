﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace oscardata
{
    // static variables for use anywhere in the program
    public static class statics
    {
        // frame types
        // the values 0..15 are sent over the air
        public static Byte noTX = 0;
        public static Byte BERtest = 1;
        public static Byte Image = 2;
        public static Byte AsciiFile = 3;
        public static Byte HTMLFile = 4;
        public static Byte BinaryFile = 5;
        public static Byte Audio = 6;
        public static Byte Userinfo = 7;
        public static Byte ExternalDate = 8; // data from ext. application

        // the upper values are for internal use
        public static Byte BulletinFile = 16;
        public static Byte AutosendFile = 17;
        public static Byte AutosendFolder = 18;
        public static Byte Modem_shutdown = 19;
        public static Byte ResetModem = 20;
        public static Byte SetPBvolume = 21;
        public static Byte SetCAPvolume = 22;
        public static Byte SetLSvolume = 23;
        public static Byte SetMICvolume = 24;
        public static Byte SetVoiceMode = 25;
        public static Byte terminate = 26;
        public static Byte tuning = 27;
        public static Byte marker = 28;
        public static Byte setfreq = 29;
        public static Byte rttykey = 30;
        public static Byte rttystring = 31;
        public static Byte txonoff = 32;
        public static Byte rtty_stopTX = 33;

        // frame sequence, modem needs that for i.e. sending a preamble
        public static Byte FirstFrame = 0;
        public static Byte NextFrame = 1;
        public static Byte LastFrame = 2;
        public static Byte SingleFrame = 3;

        // udp messages from modem
        public static Byte udp_payload = 1;
        public static Byte udp_bc = 3;
        public static Byte udp_fft = 4;
        public static Byte udp_iq = 5;
        public static Byte udp_rtty_rx = 6;
        public static Byte udp_donesending = 99;

        // global static variables
        public static bool running = true;
        public static String ModemIP = "1.2.3.4";
        public static String MyIP = "1.2.3.4";
        public static int UdpBCport_AppToModem = 40131;          // broadcast port for modem search
        public static int UdpTXport = 40132;                     // to modem
        public static int UdpRXport = 40133;                     // from modem

        public static int FecLen = 32;                          // must be same value as in qo100modem software !!!
        public static int UdpBlocklen = 258;                         // length of a data block (UDP)
        public static int PayloadLen = UdpBlocklen - FecLen - 3 - 2 - 2;
        public static int real_datarate = 6000;                 // speed in bit/s
        public static String jpg_tempfilename = "rxdata.jpg";
        public static String zip_TXtempfilename = "TXtemp.zip";
        public static String zip_RXtempfilename = "RXtemp.zip";
        public static String DataStorage = "oscardata";
        public static String RXimageStorage = "RXimages";
        public static String OSversion = "";
        public static int ostype = 0; // 0=Windows, 1=Linux
        public static int GotAudioDevices = 0;
        public static String[] AudioPBdevs;
        public static String[] AudioCAPdevs;
        public static int PBfifousage = 0;
        public static int CAPfifousage = 0;
        public static int initAudioStatus;
        public static int initVoiceStatus;
        public static int RXlevelDetected = 0;
        public static int RXinSync = 0;
        public static int maxRXlevel = 0;
        public static int maxTXlevel = 0;
        public static Color WindowBackColor;
        public static String[] langstr;
        public static int tuning_active = 0;
        public static int tune_frequency = 1500;
        public static int rtty_txon = 0;
        public static Byte extData = 0;
        public static String lastRXedHTMLfile = "";

        public static String[] getOwnIPs()
        {
            try
            {
                IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress[] addr = ipEntry.AddressList;
                //Console.WriteLine("1:len: " + addr.Length.ToString());
                int anz = 0;
                for (int i = 0; i < addr.Length; i++)
                {
                    if (addr[i].AddressFamily == AddressFamily.InterNetwork)
                        anz++;
                }

                if (anz > 0)
                {
                    String[] iparr = new String[anz];
                    int num = 0;
                    for (int i = 0; i < addr.Length; i++)
                    {
                        if (addr[i].AddressFamily == AddressFamily.InterNetwork)
                        {
                            iparr[num] = addr[i].ToString();
                            //Console.WriteLine("1:found my ip " + iparr[num]);
                            num++;
                        }
                    }

                    return iparr;
                }
            }
            catch
            { }

            try
            {
                List<IPAddress> ipList = new List<IPAddress>();
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            //Console.WriteLine("2:found my ip " + ua.Address.ToString());
                            ipList.Add(ua.Address);
                        }
                    }
                }

                if (ipList.Count > 0)
                {
                    String[] iparr = new String[ipList.Count];
                    int i = 0;
                    foreach (IPAddress v in ipList)
                    {
                        iparr[i++] = v.ToString();
                    }
                    return iparr;
                }
            }
            catch { }
            Console.WriteLine("not found");
            return null;
        }

        public static byte[] StringToByteArray(string str)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetBytes(str);
        }

        public static byte[] StringToByteArrayUtf8(string str)
        {
            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            return enc.GetBytes(str);
        }

        public static string ByteArrayToString(byte[] arr, int offset = 0)
        {
            Byte[] ba = new byte[arr.Length];
            int dst = 0;
            for (int i = offset; i < arr.Length; i++)
            {
                if (arr[i] != 0) ba[dst++] = arr[i];
            }
            Byte[] ban = new byte[dst];
            Array.Copy(ba, ban, dst);

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            
            return enc.GetString(ban);
        }

        public static string ByteArrayToString(byte[] arr, int offset, int length)
        {
            Byte[] ba = new byte[arr.Length];
            int dst = 0;
            for (int i = 0; i < length; i++)
            {
                if (i >= arr.Length) break;
                if (arr[i+ offset] != 0) ba[dst++] = arr[i+ offset];
            }
            Byte[] ban = new byte[dst];
            Array.Copy(ba, ban, dst);

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

            return enc.GetString(ban);
        }

        public static string ByteArrayToStringUtf8(byte[] arr, int offset = 0)
        {
            Byte[] ba = new byte[arr.Length];
            int dst = 0;
            for (int i = offset; i < arr.Length; i++)
            {
                if (arr[i] != 0) ba[dst++] = arr[i];
            }
            Byte[] ban = new byte[dst];
            Array.Copy(ba, ban, dst);

            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();

            return enc.GetString(ban);
        }

        public static String addTmpPath(String fn)
        {
            if (statics.ostype == 0)
            {
                // Windows
                return Application.UserAppDataPath + "\\" + fn;
            }
            else
            {
                // Linux
                return "/tmp/" + fn;
            }
        }

        public static String getHomePath(String subpath, String filename)
        {
            String deli = "/";
            if (statics.ostype == 0)deli = "\\";

            //String home = Application.UserAppDataPath;
            String home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            home = home + deli + DataStorage + deli;
            try { Directory.CreateDirectory(home); } catch { }

            // if not exists, create subfolder "oscardata"
            if (subpath.Length > 0)
                try {
                    home += subpath + deli;
                    Directory.CreateDirectory(home); 
                } catch { }

            return home + filename;
        }

        public static void CreateAllDirs()
        {
            String home = Application.UserAppDataPath;
            String deli = "/";

            if (statics.ostype == 0)
                deli = "\\";

            // create home directory
            try
            {
                Directory.CreateDirectory(home);
            }
            catch { }

            // create application path
            home += deli + DataStorage;
            try
            {
                Directory.CreateDirectory(home);
            }
            catch { }

            // create image path
            home += deli + RXimageStorage;
            try
            {
                Directory.CreateDirectory(home);
            }
            catch { }
        }

        // Returns the file's size.
        public static long GetFileSize(string file_name)
        {
            return new FileInfo(file_name).Length;
        }

        // returns the filename of a path+filename string
        public static String pureFilename(String fullfn)
        {
            // extract just the filename without a path
            String fn;
            int idx = fullfn.LastIndexOf('/');
            if (idx == -1) idx = fullfn.LastIndexOf('\\');
            if (idx == -1)
            {
                // fullfn does not contain a path
                return fullfn;
            }
            else
            {
                // just the filename
                try { fn = fullfn.Substring(idx + 1); }
                catch { fn = fullfn; }
                return fn;
            }
        }

        // returns only the path of a Path+Filename string
        public static String purePath(String fullfn)
        {
            int idx = fullfn.LastIndexOf('/');
            if (idx == -1) idx = fullfn.LastIndexOf('\\');
            if (idx == -1)
                return ".";
            else
                return fullfn.Substring(0, idx);
        }

        // check if an image is a valid image
        public static bool checkImage(String imgfn)
        {
            try
            {
                using (Image dummy = new Bitmap(imgfn))
                {
                    // valid image
                }
                return true;
            }
            catch
            {
                // invalid image
                return false;
            }
        }

        // add a file extension or replace an existing one
        public static String AddReplaceFileExtension(String fn, String ext)
        {
            int idx = fn.IndexOf('.');
            if(idx == -1)
            {
                // filename has no '.'
                return fn + "." + ext;
            }

            // filename has a '.'
            // if the '.' is the first char, then the filename is invalid
            if (idx == 0) return fn;

            return fn.Substring(0, idx) + "." + ext;
        }

        static Process cmd = null;
        public static bool StartHSmodem(bool start = true)
        {
            // kill old processes already running
            killall("hsmodem");
            killall("hsmodem.exe");

            if (start == true)
            {
                // starte Prozesse
                try
                {
                    if (ostype == 0)
                    {
                        if (!File.Exists("hsmodem.exe")) return false;
                        cmd = new Process();
                        cmd.StartInfo.FileName = "hsmodem.exe";
                    }
                    else
                    {
                        if (!File.Exists("hsmodem")) return false;
                        cmd = new Process();
                        cmd.StartInfo.FileName = "hsmodem";
                    }

                    if (cmd != null)
                    {
                        cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        cmd.StartInfo.Arguments = "";
                        cmd.Start();
                        Console.WriteLine("hsmodem started");
                    }
                }
                catch { return false; }
            }
            return true;
        }

        // checks if a process is running
        static public bool isProcRunning(String s)
        {
            bool running = false;

            if (ostype == 0)
            {
                foreach (var process in Process.GetProcessesByName(s))
                {
                    running = true;
                    break;
                }
            }

            return running;
        }

        static public void killall(String s)
        {
            if (ostype == 0)
            {
                // kill a Windows process
                try
                {
                    foreach (var process in Process.GetProcessesByName(s))
                        process.Kill();
                }
                catch { }
            }
            else
            {
                // kill a Linux process
                try
                {
                    if (cmd != null)
                        cmd.Kill();
                    cmd = null;

                    Process proc = new Process();
                    proc.EnableRaisingEvents = false;
                    proc.StartInfo.FileName = "killall";
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.OutputDataReceived += (sender, args) => { };   // schreibe Output ins nichts
                    proc.StartInfo.RedirectStandardError = true;
                    proc.ErrorDataReceived += (sender, args) => { };   // schreibe Output ins nichts
                    proc.StartInfo.Arguments = s;
                    proc.Start();
                    proc.WaitForExit();
                }
                catch { }
            }
        }

        public static void FileDelete(String fn)
        {
            try
            {
                File.Delete(fn);
            }
            catch { }
        }

        // Culture invariant conversion

        public static double MyToDouble(String s)

        {
            double r = 0;

            try
            {
                s = s.Replace(',', '.');
                r = Convert.ToDouble(s, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
            }
            return r;

        }
    }
}
