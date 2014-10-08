using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//using memory_control;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
//using ProcessMemoryReaderLib;

namespace EQTrainer
{
    public partial class MapForm : Form
    {
        public static int proccID;
        public static IntPtr pHandle;
        public static int base_address;
        #region DllImports
        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesWritten);

        [DllImportAttribute("User32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            UInt32 dwDesiredAccess,
            Int32 bInheritHandle,
            Int32 dwProcessId
            );  

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        private static extern bool _CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);
        #endregion
        public MapForm()
        {
            InitializeComponent();
        }
        private static ProcessModule mainModule;
        //static ProcessMemoryReader pReader = new ProcessMemoryReader();

        private static void NewOpenProcess()
        {
            Int32 ProcID = Convert.ToInt32(TrainerForm.eqgameID);
            //if (ProcID == 0)
            //    return;

            Process procs = Process.GetProcessById(ProcID);
            //if (procs == null)
            //    return;

            pHandle = OpenProcess(0x1F0FFF, false, ProcID);
            IntPtr hProcess = (IntPtr)OpenProcess(0x1F0FFF, 1, ProcID);
            ProcessModuleCollection modules = procs.Modules;
            mainModule = procs.MainModule;
            //pReader.ReadProcess = procs;
            //pReader.OpenProcess();
        }

        public static float readFloat(uint Address)
        {
            byte[] buffer = new byte[sizeof(float)];
            ReadProcessMemory(pHandle, (UIntPtr)Address, buffer, (UIntPtr)4, IntPtr.Zero);
            float hexaddress = BitConverter.ToSingle(buffer, 0);
            return (float)Math.Round(hexaddress, 2);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            backgroundWorker4.RunWorkerAsync();
        }

        public static string RemoveSpecialCharacters(string str)
        {
            //memory string keeps char width from longest string
            //causes issues, only fix i know...
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                }
                else
                {
                    break;
                }
            }
            return sb.ToString();
        }

        public UIntPtr getPointer(int[] offsets)
        {
            //int bytesRead;
            byte[] pre_t_z_address = new byte[4];
            //byte[] mem1 = pReader.ReadProcessMemory((IntPtr)((int)mainModule.BaseAddress + offsets[0]), 4, out bytesRead);
            ReadProcessMemory(pHandle, (UIntPtr)((int)mainModule.BaseAddress + offsets[0]), pre_t_z_address, (UIntPtr)4, IntPtr.Zero);
            uint num1 = BitConverter.ToUInt32(pre_t_z_address, 0);

            UIntPtr base1 = (UIntPtr)0;

            for (int i = 1; i < offsets.Length; i++)
            {
                base1 = new UIntPtr(num1 + Convert.ToUInt32(offsets[i]));
                //mem1 = pReader.ReadProcessMemory(base1, 4, out bytesRead);
                ReadProcessMemory(pHandle, base1, pre_t_z_address, (UIntPtr)4, IntPtr.Zero);
                num1 = BitConverter.ToUInt32(pre_t_z_address, 0);
            }

            return base1;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //int bytesRead;
            while (true)
            {
                try {
                    NewOpenProcess();
                    //uint map_address = (uint)mem.ReadPointer(base_address + 0x23DEA8) + 0x8cfc;
                    int[] offsets = { 0x23DEA8, 0x8cfc };
                    //byte[] pre_map_address = pReader.ReadProcessMemory(getPointer(offsets), 255, out bytesRead);
                    byte[] pre_map_address = new byte[255];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets), pre_map_address, (UIntPtr)255, IntPtr.Zero);
                    string map_name = System.Text.Encoding.UTF8.GetString(pre_map_address);

                    //string map_name = mem.ReadString(map_address).ToString();
                    string locfile = @"./maps/" + RemoveSpecialCharacters(map_name) + ".loc";
                    FileInfo fi = new FileInfo(locfile);
                    display2.Location = new Point(0, 0);

                    //uint y_address = (uint)mem.ReadPointer(base_address + 0x3F94CC) + 0x4c;
                    int[] offsets2 = { 0x3F94CC, 0x4c };
                    //byte[] pre_y_address = pReader.ReadProcessMemory(getPointer(offsets2), 4, out bytesRead);
                    byte[] pre_y_address = new byte[255];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets2), pre_y_address, (UIntPtr)255, IntPtr.Zero);
                    float y_address = BitConverter.ToSingle(pre_y_address, 0);
                    y_address = (float)Math.Round(y_address, 2);

                    //uint x_address = (uint)mem.ReadPointer(base_address + 0x3F94CC) + 0x48;
                    int[] offsets3 = { 0x3F94CC, 0x48 };
                    //byte[] pre_x_address = pReader.ReadProcessMemory(getPointer(offsets3), 4, out bytesRead);
                    byte[] pre_x_address = new byte[255];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets3), pre_x_address, (UIntPtr)255, IntPtr.Zero);
                    float x_address = BitConverter.ToSingle(pre_x_address, 0);
                    x_address = (float)Math.Round(x_address, 2);

                    //uint z_address = (uint)mem.ReadPointer(base_address + 0x3F94CC) + 0x50;
                    int[] offsets4 = { 0x3F94CC, 0x50 };
                    //byte[] pre_z_address = pReader.ReadProcessMemory(getPointer(offsets4), 4, out bytesRead);
                    byte[] pre_z_address = new byte[255];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets4), pre_z_address, (UIntPtr)255, IntPtr.Zero);
                    float z_address = BitConverter.ToSingle(pre_z_address, 0);
                    z_address = (float)Math.Round(z_address, 2);

                    //MessageBox.Show("Y:" + y_address.ToString() + " X:" + x_address.ToString() + " Z:" + z_address.ToString());// DEBUG

                    string path = @"./maps/" + RemoveSpecialCharacters(map_name) + ".jpg";
                
                    Bitmap bmp = new Bitmap(path);
                    Graphics g = Graphics.FromImage(bmp);
                    float scale_x = bmp.Width;
                    float scale_y = bmp.Height;
                    g.RotateTransform(180.0F);
                    int x = 0; //offsets
                    int y = 0;
                    int actualx = bmp.Width + 346;
                    int actualy = bmp.Height + 38;
                    if (fi.Exists)
                    {

                        int[] s = new int[4];
                        using (StreamReader sr = fi.OpenText())
                        {
                            int i;
                            for (i = 0; i < 4; i++)
                            {
                                s[i] = Convert.ToInt32(sr.ReadLine());
                            }
                        }

                        scale_x = s[2] / bmp.Width;
                        scale_y = s[3] / bmp.Height;
                        x = s[0]; //offsets
                        y = s[1];
                    }
                    g.DrawString("X", new Font("Calibri", 12), new SolidBrush(Color.Red), (y_address / scale_y) + y, (x_address / scale_x) + x);
                    
                    string old_map_name = "";
                    if (map_name != old_map_name) //cant keep pulling images this fast. Causes a crash.
                    {
                        display2.Image = bmp;
                        this.Width = actualx;
                        this.Height = actualy;
                        display2.Width = bmp.Width;
                        display2.Height = bmp.Height;
                        this.Controls.Add(display2);

                        textBox1.Location = new Point(bmp.Width, 0);
                        textBox1.Height = bmp.Height;
                        string txt = @"./maps/" + RemoveSpecialCharacters(map_name) + ".txt";
                        fi = new FileInfo(txt);
                        if (fi.Exists)
                        {
                            string text = File.ReadAllText(txt);
                            textBox1.Text = text;
                        }
                    }
                    else
                    {
                        old_map_name = map_name;
                    }

                    Thread.Sleep(200);
                } catch 
                {
                }
            }
        }
    }
}
