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
    public partial class TrainerForm : Form
    {
        public TrainerForm()
        {
            InitializeComponent();
        }
        [DllImport("kernel32")]  
        public static extern IntPtr CreateRemoteThread(  
          IntPtr hProcess,  
          IntPtr lpThreadAttributes,  
          uint dwStackSize,  
          UIntPtr lpStartAddress, // raw Pointer into remote process  
          IntPtr lpParameter,  
          uint dwCreationFlags,  
          out IntPtr lpThreadId  
        );
  
        [DllImport("kernel32.dll")]  
        public static extern IntPtr OpenProcess(  
            UInt32 dwDesiredAccess,  
            Int32 bInheritHandle,  
            Int32 dwProcessId  
            );  
  
        [DllImport("kernel32.dll")]  
        public static extern Int32 CloseHandle(  
        IntPtr hObject  
        );  
  
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]  
        static extern bool VirtualFreeEx(  
            IntPtr hProcess,  
            IntPtr lpAddress,  
            UIntPtr dwSize,  
            uint dwFreeType  
            );

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool FreeLibrary([In] IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern UIntPtr GetProcAddress(
            IntPtr hModule,
            string procName
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(
            string lpModuleName
            );  
  
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]  
        static extern IntPtr VirtualAllocEx(  
            IntPtr hProcess,  
            IntPtr lpAddress,  
            uint dwSize,  
            uint flAllocationType,  
            uint flProtect  
            );

        [DllImport("user32.dll")]
        static extern byte VkKeyScan(char ch);

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        //[DllImport("User32.Dll", EntryPoint = "PostMessageA")]
       // static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        [DllImport("kernel32.dll")]  
        static extern bool WriteProcessMemory(  
            IntPtr hProcess,  
            IntPtr lpBaseAddress,  
            string lpBuffer,  
            UIntPtr nSize,  
            out IntPtr lpNumberOfBytesWritten  
        );
  
        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]  
        internal static extern Int32 WaitForSingleObject(  
            IntPtr handle,  
            Int32 milliseconds  
            );

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);

        public static int proccID;
        public static IntPtr pHandle;
        private ProcessModule mainModule;
        //ProcessMemoryReader pReader = new ProcessMemoryReader();
        //ProcessMemoryReaderLib.ProcessMemoryReader pReader = new ProcessMemoryReaderLib.ProcessMemoryReader();

        #region DllImports
        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        private static extern bool _CloseHandle(IntPtr hObject);


        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);
        #endregion

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern uint GetPrivateProfileString(
           string lpAppName,
           string lpKeyName,
           string lpDefault,
           StringBuilder lpReturnedString,
           uint nSize,
           string lpFileName);

        private static Boolean Follow = false;
        //private static Boolean UltraVision = false;
        protected ProcessModule myProcessModule;
        public static string eqgameID;


        public void InjectDLL(IntPtr hProcess, String strDLLName)
        {
            IntPtr bytesout;

            // Length of string containing the DLL file name +1 byte padding  
            Int32 LenWrite = strDLLName.Length + 1;
            // Allocate memory within the virtual address space of the target process  
            IntPtr AllocMem = (IntPtr)VirtualAllocEx(hProcess, (IntPtr)null, (uint)LenWrite, 0x1000, 0x40); //allocation pour WriteProcessMemory  

            // Write DLL file name to allocated memory in target process  
            WriteProcessMemory(hProcess, AllocMem, strDLLName, (UIntPtr)LenWrite, out bytesout);
            // Function pointer "Injector"  
            UIntPtr Injector = (UIntPtr)GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (Injector == null)
            {
                MessageBox.Show(" Injector Error! \n ");
                // return failed  
                return;
            }

            // Create thread in target process, and store handle in hThread  
            IntPtr hThread = (IntPtr)CreateRemoteThread(hProcess, (IntPtr)null, 0, Injector, AllocMem, 0, out bytesout);
            // Make sure thread handle is valid  
            if (hThread == null)
            {
                //incorrect thread handle ... return failed  
                MessageBox.Show(" hThread [ 1 ] Error! \n ");
                return;
            }
            // Time-out is 10 seconds...  
            int Result = WaitForSingleObject(hThread, 10 * 1000);
            // Check whether thread timed out...  
            if (Result == 0x00000080L || Result == 0x00000102L)
            {
                /* Thread timed out... */
                MessageBox.Show(" hThread [ 2 ] Error! \n ");
                // Make sure thread handle is valid before closing... prevents crashes.  
                if (hThread != null)
                {
                    //Close thread in target process  
                    CloseHandle(hThread);
                }
                return;
            }
            // Sleep thread for 1 second  
            //Thread.Sleep(1000);
            // Clear up allocated space ( Allocmem )  
            VirtualFreeEx(hProcess, AllocMem, (UIntPtr)0, 0x8000);
            // Make sure thread handle is valid before closing... prevents crashes.

            if (hThread != null)
            {
                //Close thread in target process
                CloseHandle(hThread);
            }
            // return succeeded
            
            return;
        }
        
        public static float readFloat(uint Address)
        {
            byte[] buffer = new byte[sizeof(float)];
            ReadProcessMemory(pHandle, (UIntPtr)Address, buffer, (UIntPtr)4, IntPtr.Zero);
            float hexaddress = BitConverter.ToSingle(buffer, 0);
            return (float)Math.Round(hexaddress, 2);
        }

        public static int readInt(uint Address)
        {
            byte[] buffer = new byte[sizeof(int)];
            ReadProcessMemory(pHandle, (UIntPtr)Address, buffer, (UIntPtr)4, IntPtr.Zero);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static string readString(uint Address)
        {
            byte[] buffer = new byte[255];
            ReadProcessMemory(pHandle, (UIntPtr)Address, buffer, (UIntPtr)255, IntPtr.Zero);
            return System.Text.Encoding.UTF8.GetString(buffer);
        }

        public static void WriteFloat(uint Address, float value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteProcessMemory(pHandle, (UIntPtr)Address, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
        }

        public static void WriteInt(long Address, int value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteProcessMemory(pHandle, (UIntPtr)Address, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
        }

        protected override void WndProc(ref Message m) //hotbuttons
        {
            if (m.Msg == 0x0312)
            {
                int id = m.WParam.ToInt32();
                if (id == 1)
                {
                    button1.PerformClick();
                } 
                else if (id == 2)
                {
                    button2.PerformClick();
                }
                else if (id == 3)
                {
                    button3.PerformClick();
                }
                else if (id == 4)
                {
                    button4.PerformClick();
                }
                else if (id == 5)
                {
                    Form MapForm = new MapForm();
                    MapForm.Show();
                }
                else if (id == 6)
                {
                    button23.PerformClick();
                }
                else if (id == 7)
                {
                    button24.PerformClick();
                }
            }
            base.WndProc(ref m);
        }

        private void TrainerForm_LoadScripts()
        {
            Debug.WriteLine("Application.StartupPath: " + Application.StartupPath);

            string[] scripts = Directory.GetFiles(Application.StartupPath + @"\scripts", "*.ini");

            foreach (string script in scripts)
            {
                Debug.WriteLine("loaded script: " + script);

                uint read_ini_result = 0;

                StringBuilder script_name = new StringBuilder(1024);
                read_ini_result = GetPrivateProfileString("Script", "Name", "", script_name, (uint)script_name.Capacity, script);

                StringBuilder script_description = new StringBuilder(1024);
                read_ini_result = GetPrivateProfileString("Script", "Description", "", script_description, (uint)script_description.Capacity, script);

                System.IO.StreamReader script_file = new System.IO.StreamReader(script);

                bool script_found_enable  = false;
                bool script_found_disable = false;

                string script_instruction_enable  = "";
                string script_instruction_disable = "";

                string script_line;
                while ((script_line = script_file.ReadLine()) != null)
                {
                    if (script_line.Length == 0)
                    {
                        continue;
                    }

                    if (script_line.Contains("#"))
                    {
                        continue;
                    }

                    if (script_line.Contains("//"))
                    {
                        continue;
                    }

                    if (script_line.Contains("[Enable]"))
                    {
                        script_found_enable  = true;
                        script_found_disable = false;

                        continue;
                    }

                    if (script_line.Contains("[Disable]"))
                    {
                        script_found_enable  = false;
                        script_found_disable = true;

                        continue;
                    }

                    if (script_found_enable == true)
                    {
                        script_instruction_enable += script_line + "^";
                    }

                    if (script_found_disable == true)
                    {
                        script_instruction_disable += script_line + "^";
                    }
                }

                script_file.Close();

                script_instruction_enable  = script_instruction_enable.TrimEnd('^',' ');
                script_instruction_disable = script_instruction_disable.TrimEnd('^',' ');

                string[] listviewScriptsRow = { script_name.ToString(), script_description.ToString(), script_instruction_enable, script_instruction_disable };
                var listviewScriptsItem = new ListViewItem(listviewScriptsRow);
                listViewScripts.Items.Add(listviewScriptsItem);
            }
        }

        private void TrainerForm_RefreshSpawnList()
        {
            byte[] buffer = new byte[4];
            ReadProcessMemory(pHandle, (UIntPtr)0x007F94CC, buffer, (UIntPtr)4, IntPtr.Zero);

            int player_spawn_info = BitConverter.ToInt32(buffer, 0);

            int spawn_info_address = player_spawn_info;

            byte[] buffer2 = new byte[4];
            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x78, buffer2, (UIntPtr)4, IntPtr.Zero);

            int spawn_next_spawn_info = BitConverter.ToInt32(buffer2, 0);

            spawn_info_address = spawn_next_spawn_info;

            for (int i = 0; i < 4096; i++)
            {
                byte[] buffer3 = new byte[4];
                ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x78, buffer3, (UIntPtr)4, IntPtr.Zero);

                spawn_next_spawn_info = BitConverter.ToInt32(buffer3, 0);

                if (spawn_next_spawn_info == 0x00000000)
                {
                    break;
                }

                byte[] buffer4 = new byte[64];
                ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x01, buffer4, (UIntPtr)64, IntPtr.Zero);

                string spawn_info_name = System.Text.Encoding.ASCII.GetString(buffer4);

                byte[] buffer5 = new byte[4];
                ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x48, buffer5, (UIntPtr)4, IntPtr.Zero);

                float spawn_info_y = BitConverter.ToSingle(buffer5, 0);
                spawn_info_y = (float)Math.Round(spawn_info_y, 2);

                byte[] buffer6 = new byte[4];
                ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x4C, buffer6, (UIntPtr)4, IntPtr.Zero);

                float spawn_info_x = BitConverter.ToSingle(buffer6, 0);
                spawn_info_x = (float)Math.Round(spawn_info_x, 2);

                byte[] buffer7 = new byte[4];
                ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x50, buffer7, (UIntPtr)4, IntPtr.Zero);

                float spawn_info_z = BitConverter.ToSingle(buffer7, 0);
                spawn_info_z = (float)Math.Round(spawn_info_z, 2);

                byte[] buffer8 = new byte[4];
                ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x54, buffer8, (UIntPtr)4, IntPtr.Zero);

                float spawn_info_heading = BitConverter.ToSingle(buffer8, 0);
                spawn_info_heading = (float)Math.Round(spawn_info_heading, 2);

                byte[] buffer9 = new byte[4];
                ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0xad, buffer9, (UIntPtr)4, IntPtr.Zero);

                int spawn_info_level = BitConverter.ToInt32(buffer9, 0);
                spawn_info_level = (byte)spawn_info_level;

                if (textBoxSpawnListFilter.TextLength > 0)
                {
                    if (spawn_info_name.ToLower().Contains(textBoxSpawnListFilter.Text.ToLower()) == false)
                    {
                        spawn_info_address = spawn_next_spawn_info;
                        continue;
                    }
                }

                string[] listViewSpawnListRow = { spawn_info_name, spawn_info_address.ToString("X8"), spawn_info_x.ToString(), spawn_info_y.ToString(), spawn_info_z.ToString(), spawn_info_heading.ToString(), spawn_info_level.ToString() };
                var listViewSpawnListItem = new ListViewItem(listViewSpawnListRow);
                listViewSpawnList.Items.Add(listViewSpawnListItem);

                spawn_info_address = spawn_next_spawn_info;
            }
        }

        private void TrainerForm_WarpToSpawn()
        {
            string x_text = listViewSpawnList.SelectedItems[0].SubItems[2].Text;
            string y_text = listViewSpawnList.SelectedItems[0].SubItems[3].Text;
            string z_text = listViewSpawnList.SelectedItems[0].SubItems[4].Text;

            string heading_text = listViewSpawnList.SelectedItems[0].SubItems[5].Text;

            Teleport
            (
                float.Parse(y_text), // x and y NEED to be backwards like this
                float.Parse(x_text), // x and y NEED to be backwards like this
                float.Parse(z_text),

                float.Parse(heading_text)
            );
        }

        private void RunScript()
        {
            
        }

        public void OpenProcess()
        {
            Int32 ProcID = Convert.ToInt32(eqgameID);
            Process procs = Process.GetProcessById(ProcID);
            IntPtr hProcess = (IntPtr)OpenProcess(0x1F0FFF, 1, ProcID);

            pHandle = OpenProcess(0x1F0FFF, true, ProcID);
            //ProcessModuleCollection modules = procs.Modules;
            mainModule = procs.MainModule;
            //pReader.ReadProcess = procs;
            //pReader.OpenProcess();

        }

        private void TrainerForm_Load(object sender, EventArgs e)
        {
            ToolTip tt = new ToolTip();
            tt.SetToolTip(this.pictureBox1, "Bank Money");
            tt.SetToolTip(this.button1, "CTRL+1");
            tt.SetToolTip(this.button2, "CTRL+2");
            tt.SetToolTip(this.button3, "CTRL+3");
            tt.SetToolTip(this.button4, "CTRL+4");
            tt.SetToolTip(this.button24, "CTRL+F");
            tt.SetToolTip(this.button23, "CTRL+G");
            tt.SetToolTip(this.x_label, "forward and backwards");
            tt.SetToolTip(this.y_label, "left and right");
            tt.SetToolTip(this.z_label, "up and down");
            tt.SetToolTip(this.map_label, "CTRL+M");
            tt.SetToolTip(this.button5, "Set current X Y Z (set 1)");
            tt.SetToolTip(this.button6, "Set current X Y Z (set 2)");
            tt.SetToolTip(this.button7, "Set current X Y Z (set 3)");
            tt.SetToolTip(this.button8, "Set current X Y Z (set 4)");
            tt.SetToolTip(this.button9, "Erase X Y Z (set 1)");
            tt.SetToolTip(this.button10, "Erase X Y Z (set 2)");
            tt.SetToolTip(this.button11, "Erase X Y Z (set 3)");
            tt.SetToolTip(this.button12, "Erase X Y Z (set 4)");
            tt.SetToolTip(this.button13, "Save X Y Z to file (set 1)");
            tt.SetToolTip(this.button14, "Save X Y Z to file (set 2)");
            tt.SetToolTip(this.button15, "Save X Y Z to file (set 3)");
            tt.SetToolTip(this.button16, "Save X Y Z to file (set 4)");
            tt.SetToolTip(this.button17, "Load X Y Z from file (set 1)");
            tt.SetToolTip(this.button18, "Load X Y Z from file (set 2)");
            tt.SetToolTip(this.button19, "Load X Y Z from file (set 3)");
            tt.SetToolTip(this.button20, "Load X Y Z from file (set 4)");

            sd1.InitialDirectory = Path.Combine(Application.StartupPath, @"saves");
            sd2.InitialDirectory = Path.Combine(Application.StartupPath, @"saves");
            sd3.InitialDirectory = Path.Combine(Application.StartupPath, @"saves");
            sd4.InitialDirectory = Path.Combine(Application.StartupPath, @"saves");
            RegisterHotKey(this.Handle, 1, 2, (int)'1');
            RegisterHotKey(this.Handle, 2, 2, (int)'2');
            RegisterHotKey(this.Handle, 3, 2, (int)'3');
            RegisterHotKey(this.Handle, 4, 2, (int)'4');
            RegisterHotKey(this.Handle, 5, 2, (int)'M'); //must be uppercase?
            RegisterHotKey(this.Handle, 6, 2, (int)'G');
            RegisterHotKey(this.Handle, 7, 2, (int)'F');

            TrainerForm_LoadScripts();
        }

        public static byte[] StrToByteArray(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }

        private void charClass(int t_class)
        {
            t_class = (byte)t_class;
            if (t_class >= 1)
                if (t_class == 1)
                    label12.Text = "Class: Warrior";
                else if (t_class == 2)
                    label12.Text = "Class: Cleric";
                else if (t_class == 3)
                    label12.Text = "Class: Paladin";
                else if (t_class == 4)
                    label12.Text = "Class: Ranger";
                else if (t_class == 5)
                    label12.Text = "Class: Shadow Knight";
                else if (t_class == 6)
                    label12.Text = "Class: Druid";
                else if (t_class == 7)
                    label12.Text = "Class: Monk";
                else if (t_class == 8)
                    label12.Text = "Class: Bard";
                else if (t_class == 9)
                    label12.Text = "Class: Rogue";
                else if (t_class == 10)
                    label12.Text = "Class: Shaman";
                else if (t_class == 11)
                    label12.Text = "Class: Necromancer";
                else if (t_class == 12)
                    label12.Text = "Class: Wizard";
                else if (t_class == 13)
                    label12.Text = "Class: Magician";
                else if (t_class == 14)
                    label12.Text = "Class: Enchanter";
                else if (t_class == 15)
                    label12.Text = "Class: Beastlord";
                else if (t_class == 16)
                    label12.Text = "Class: Banker";
                else if (t_class == 17)
                    label12.Text = "Class: Warrior Trainer";
                else if (t_class == 18)
                    label12.Text = "Class: Cleric Trainer";
                else if (t_class == 19)
                    label12.Text = "Class: Paladin Trainer";
                else if (t_class == 20)
                    label12.Text = "Class: Ranger Trainer";
                else if (t_class == 21)
                    label12.Text = "Class: Shadow Knight Trainer";
                else if (t_class == 22)
                    label12.Text = "Class: Druid Trainer";
                else if (t_class == 23)
                    label12.Text = "Class: Monk Trainer";
                else if (t_class == 24)
                    label12.Text = "Class: Bard Trainer";
                else if (t_class == 25)
                    label12.Text = "Class: Rogue Trainer";
                else if (t_class == 26)
                    label12.Text = "Class: Shaman Trainer";
                else if (t_class == 27)
                    label12.Text = "Class: Necromancer Trainer";
                else if (t_class == 28)
                    label12.Text = "Class: Wizard Trainer";
                else if (t_class == 29)
                    label12.Text = "Class: Magician Trainer";
                else if (t_class == 30)
                    label12.Text = "Class: Enchanter Trainer";
                else if (t_class == 31)
                    label12.Text = "Class: Beastlord Trainer";
                else if (t_class == 32)
                    label12.Text = "Class: Merchant";
                else
                    label12.Text = "Class: " + t_class;
            else
                label12.Text = "Class: ";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (listView2.Items.Count == 0)
            {
                Process[] processlist = Process.GetProcesses();

                foreach (Process theprocess in processlist)
                {
                    if (theprocess.ProcessName == "eqgame")
                    {
                        string[] listView2Rows = { theprocess.ProcessName, theprocess.Id.ToString() };
                        var listView2Items = new ListViewItem(listView2Rows);
                        listView2.Items.Add(listView2Items);
                    }
                }
                if (listView2.Items.Count > 0) //if we didnt have items before, but now we do
                {
                    if (listView2.SelectedItems.Count == 0) //select the first item we see and start the processes
                    {
                        listView2.Items[0].Selected = true;
                        listView2.Select();
                        eqgameID = listView2.SelectedItems[0].SubItems[1].Text;
                        OpenProcess();
                        backgroundWorker1.RunWorkerAsync();
                    }
                }
            }

            if (listView2.SelectedItems.Count > 0)
            {
                eqgameID = listView2.SelectedItems[0].SubItems[1].Text; //keep maps up to date
                if (backgroundWorker1.IsBusy == false)
                {
                    backgroundWorker1.RunWorkerAsync();
                }
            }

            /*if (UltraVision == true)
            {
                int ultravision_address = 0x004C0D57;
                byte[] ultravision = new byte[1];
                byte[] buffer_read = new byte[4];

                int[] UltraVisionoffsets = { 0x3F94E8, 0xC8A };
                ReadProcessMemory(pHandle, (UIntPtr)getPointer(UltraVisionoffsets), buffer_read, (UIntPtr)1, IntPtr.Zero);
                int visioncheck = BitConverter.ToInt32(buffer_read, 0);
                //MessageBox.Show(visioncheck.ToString());
                if (visioncheck == 255)
                {
                    WriteProcessMemory(pHandle, (UIntPtr)ultravision_address, ultravision, (UIntPtr)1, IntPtr.Zero);

                    //removing the code below will make ultravision not work.
                    WriteProcessMemory(pHandle, (UIntPtr)getPointer(UltraVisionoffsets), ultravision, (UIntPtr)1, IntPtr.Zero);
                    //MessageBox.Show("changing vision");
                }
            }*/

            if (Follow == true)
            {
                try
                {
                    int[] offsets = { 0x3F94CC, 0x4c };
                    int[] offsets2 = { 0x3F94CC, 0x48 };
                    int[] offsets3 = { 0x3F94CC, 0x50 };
                    int[] offsets19 = { 0x3F94EC, 0x50 };
                    UIntPtr base1 = (UIntPtr)0;
                    byte[] pre_t_z_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets19), pre_t_z_address, (UIntPtr)4, IntPtr.Zero);
                    float t_z_address = BitConverter.ToSingle(pre_t_z_address, 0);
                    t_z_address = (float)Math.Round(t_z_address, 2);

                    int[] offsets20 = { 0x3F94EC, 0x4c };
                    byte[] pre_t_y_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets20), pre_t_y_address, (UIntPtr)4, IntPtr.Zero);
                    float t_y_address = BitConverter.ToSingle(pre_t_y_address, 0);
                    t_y_address = (float)Math.Round(t_y_address, 2);

                    int[] offsets21 = { 0x3F94EC, 0x48 };
                    byte[] pre_t_x_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets21), pre_t_x_address, (UIntPtr)4, IntPtr.Zero);
                    float t_x_address = BitConverter.ToSingle(pre_t_x_address, 0);
                    t_x_address = (float)Math.Round(t_x_address, 2);

                    int[] offsets22 = { 0x3F94EC, 0x54 };
                    byte[] pre_t_h_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets22), pre_t_h_address, (UIntPtr)4, IntPtr.Zero);
                    float t_h_address = BitConverter.ToSingle(pre_t_h_address, 0);
                    t_h_address = (float)Math.Round(t_h_address, 2);

                    int[] offsets14 = { 0x003F94CC, 0x54 };
                    byte[] pre_heading = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets14), pre_heading, (UIntPtr)4, IntPtr.Zero);
                    uint heading_write = BitConverter.ToUInt32(pre_heading, 0);
                    float heading = BitConverter.ToSingle(pre_heading, 0);
                    heading = (float)Math.Round(heading, 2);

                    double angleInDegrees = t_h_address / 1.42;
                    double cos = Math.Cos(angleInDegrees * (Math.PI / 180.0));
                    double sin = Math.Sin(angleInDegrees * (Math.PI / 180.0));
                    double reverse_x = t_x_address - Convert.ToInt32(distance.Text) * cos;
                    double reverse_y = t_y_address - Convert.ToInt32(distance.Text) * sin;

                    int[] offsets16 = { 0x3F94eC, 0x9c };
                    byte[] pre_thealth = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets16), pre_thealth, (UIntPtr)4, IntPtr.Zero);
                    int thealth = BitConverter.ToInt32(pre_thealth, 0);

                    if (thealth <= 0) //targets dead
                    {
                        button24.Text = "Follow Target";
                        Follow = false;
                        return;
                    }

                    byte[] buffer = new byte[4];
                    buffer = BitConverter.GetBytes((float)reverse_y);
                    WriteProcessMemory(pHandle, (UIntPtr)getPointer(offsets), buffer, (UIntPtr)buffer.Length, IntPtr.Zero);

                    byte[] buffer2 = new byte[4];
                    buffer2 = BitConverter.GetBytes((float)reverse_x);
                    WriteProcessMemory(pHandle, (UIntPtr)getPointer(offsets2), buffer2, (UIntPtr)buffer2.Length, IntPtr.Zero);

                    byte[] buffer3 = new byte[4];
                    buffer3 = BitConverter.GetBytes((float)t_h_address);
                    WriteProcessMemory(pHandle, (UIntPtr)getPointer(offsets14), buffer3, (UIntPtr)buffer3.Length, IntPtr.Zero);

                    byte[] buffer4 = new byte[4];
                    buffer4 = BitConverter.GetBytes((float)t_z_address);
                    WriteProcessMemory(pHandle, (UIntPtr)getPointer(offsets3), buffer4, (UIntPtr)buffer4.Length, IntPtr.Zero);
                    //WriteProcessMemory(pHandle, (UIntPtr)0x00798974, write_z, (UIntPtr)write_z.Length, IntPtr.Zero);
                    button24.Text = "Unfollow";
                }
                catch
                {
                    button24.Text = "Follow Target";
                    Follow = false;
                }
            } 
            else
            {
                button24.Text = "Follow Target";
                Follow = false;
            }
        }

        public int DllImageAddress(string dllname)
        {
            Int32 ProcID = Convert.ToInt32(eqgameID);
            Process MyProcess = Process.GetProcessById(ProcID);
            ProcessModuleCollection modules = MyProcess.Modules;

            foreach (ProcessModule procmodule in modules)
            {
                if (dllname == procmodule.ModuleName)
                {
                    return (int)procmodule.BaseAddress;
                }
            }
            return -1;

        }

        public UIntPtr getPointer(int[] offsets)
        {
            byte[] pre_t_z_address = new byte[4];
            ReadProcessMemory(pHandle, (UIntPtr)((int)mainModule.BaseAddress + offsets[0]), pre_t_z_address, (UIntPtr)4, IntPtr.Zero);
            uint num1 = BitConverter.ToUInt32(pre_t_z_address, 0);

            UIntPtr base1 = (UIntPtr)0;

            for (int i = 1; i < offsets.Length; i++)
            {
                base1 = new UIntPtr(num1 + Convert.ToUInt32(offsets[i]));
                ReadProcessMemory(pHandle, base1, pre_t_z_address, (UIntPtr)4, IntPtr.Zero);
                num1 = BitConverter.ToUInt32(pre_t_z_address, 0);
            }

            return base1;
        }

        void Teleport(float value_x, float value_y, float value_z, float value_h)
        {
            byte[] write_y = BitConverter.GetBytes(value_y);
            byte[] write_x = BitConverter.GetBytes(value_x);
            byte[] write_z = BitConverter.GetBytes(value_z);

            WriteProcessMemory(pHandle, (UIntPtr)0x00798970, write_y, (UIntPtr)write_y.Length, IntPtr.Zero);
            WriteProcessMemory(pHandle, (UIntPtr)0x0079896C, write_x, (UIntPtr)write_x.Length, IntPtr.Zero);
            WriteProcessMemory(pHandle, (UIntPtr)0x00798974, write_z, (UIntPtr)write_z.Length, IntPtr.Zero);

            byte[] value = BitConverter.GetBytes(value_h);
            int[] offsets1 = { 0x003F94CC, 0x54 };
            WriteProcessMemory(pHandle, (UIntPtr)getPointer(offsets1), value, (UIntPtr)value.Length, IntPtr.Zero);

            Int32 ProcID = Convert.ToInt32(eqgameID);
            Process procs = Process.GetProcessById(ProcID);
            IntPtr hProcess = (IntPtr)OpenProcess(0x1F0FFF, 1, ProcID);

            String strDLLName = Environment.CurrentDirectory + "\\injectdll2.dll";
            InjectDLL(hProcess, strDLLName);
            
            //MessageBox.Show("value_x:" + value_x + " value_y:" + value_y + " value_z:" + value_z + " new_y:" + BitConverter.ToSingle(result_y, 0).ToString() + " new_x:" + BitConverter.ToSingle(result_x, 0).ToString() + " new_z:" + BitConverter.ToSingle(result_z, 0).ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (x_tele.Text == "" || y_tele.Text == "" || z_tele.Text == "")
            {
                MessageBox.Show("ERROR: You need X Y and Z coordinates!");
            }
            else
            {
                if (h_tele1.Text == "")
                {
                    h_tele1.Text = "0";
                }
                Teleport(float.Parse(x_tele.Text), float.Parse(y_tele.Text), float.Parse(z_tele.Text), float.Parse(h_tele1.Text));
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (x_tele2.Text == "" || y_tele2.Text == "" || z_tele2.Text == "")
            {
                MessageBox.Show("ERROR: You need X Y and Z coordinates!");
            }
            else
            {
                if (h_tele2.Text == "")
                {
                    h_tele2.Text = "0";
                }
                Teleport(float.Parse(x_tele2.Text), float.Parse(y_tele2.Text), float.Parse(z_tele2.Text), float.Parse(h_tele2.Text));
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (x_tele3.Text == "" || y_tele3.Text == "" || z_tele3.Text == "")
            {
                MessageBox.Show("ERROR: You need X Y and Z coordinates!");
            }
            else
            {
                if (h_tele3.Text == "")
                {
                    h_tele3.Text = "0";
                }
                Teleport(float.Parse(x_tele3.Text), float.Parse(y_tele3.Text), float.Parse(z_tele3.Text), float.Parse(h_tele3.Text));
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (x_tele4.Text == "" || y_tele4.Text == "" || z_tele4.Text == "")
            {
                MessageBox.Show("ERROR: You need X Y and Z coordinates!");
            }
            else
            {
                if (h_tele4.Text == "")
                {
                    h_tele4.Text = "0";
                }
                Teleport(float.Parse(x_tele4.Text), float.Parse(y_tele4.Text), float.Parse(z_tele4.Text), float.Parse(h_tele4.Text));
            }
        }

        private void map_label_Click(object sender, EventArgs e)
        {
            //System.Diagnostics.Process.Start(@".\maps\" + map_label.Text + ".jpg");
            Form MapForm = new MapForm();
            MapForm.Show();
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            x_tele.Text = x_label.Text;
            y_tele.Text = y_label.Text;
            z_tele.Text = z_label.Text;
            h_tele1.Text = heading_label.Text;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            x_tele2.Text = x_label.Text;
            y_tele2.Text = y_label.Text;
            z_tele2.Text = z_label.Text;
            h_tele2.Text = heading_label.Text;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            x_tele3.Text = x_label.Text;
            y_tele3.Text = y_label.Text;
            z_tele3.Text = z_label.Text;
            h_tele3.Text = heading_label.Text;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            x_tele4.Text = x_label.Text;
            y_tele4.Text = y_label.Text;
            z_tele4.Text = z_label.Text;
            h_tele4.Text = heading_label.Text;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            tele_label1.Text = "";
            x_tele.Text = "";
            y_tele.Text = "";
            z_tele.Text = "";
            h_tele1.Text = "";
        }

        private void button10_Click(object sender, EventArgs e)
        {
            tele_label2.Text = "";
            x_tele2.Text = "";
            y_tele2.Text = "";
            z_tele2.Text = "";
            h_tele2.Text = "";
        }

        private void button11_Click(object sender, EventArgs e)
        {
            tele_label3.Text = "";
            x_tele3.Text = "";
            y_tele3.Text = "";
            z_tele3.Text = "";
            h_tele3.Text = "";
        }

        private void button12_Click(object sender, EventArgs e)
        {
            tele_label4.Text = "";
            x_tele4.Text = "";
            y_tele4.Text = "";
            z_tele4.Text = "";
            h_tele4.Text = "";
        }

        private void button13_Click(object sender, EventArgs e)
        {
            sd1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string[] lines = { tele_label1.Text, x_tele.Text, y_tele.Text, z_tele.Text, h_tele1.Text };
                Stream s = sd1.OpenFile();
                StreamWriter sw = new StreamWriter(s, Encoding.Unicode);
                foreach (string line in lines)
                    sw.WriteLine(line);
                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not write file. Please try again later. Error message: " + ex.Message, "Error Writing File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            sd2.ShowDialog();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            sd3.ShowDialog();
        }

        private void button16_Click(object sender, EventArgs e)
        {
            sd4.ShowDialog();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            od1.ShowDialog();
        }

        private void sd2_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string[] lines = { tele_label2.Text, x_tele2.Text, y_tele2.Text, z_tele2.Text, h_tele2.Text };
                Stream s = sd2.OpenFile();
                StreamWriter sw = new StreamWriter(s, Encoding.Unicode);
                foreach (string line in lines)
                    sw.WriteLine(line);
                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not write file. Please try again later. Error message: " + ex.Message, "Error Writing File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void sd3_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string[] lines = { tele_label3.Text, x_tele3.Text, y_tele3.Text, z_tele3.Text, h_tele3.Text };
                Stream s = sd3.OpenFile();
                StreamWriter sw = new StreamWriter(s, Encoding.Unicode);
                foreach (string line in lines)
                    sw.WriteLine(line);
                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not write file. Please try again later. Error message: " + ex.Message, "Error Writing File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void sd4_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string[] lines = { tele_label4.Text, x_tele4.Text, y_tele4.Text, z_tele4.Text, h_tele4.Text };
                Stream s = sd4.OpenFile();
                StreamWriter sw = new StreamWriter(s, Encoding.Unicode);
                foreach (string line in lines)
                    sw.WriteLine(line);
                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not write file. Please try again later. Error message: " + ex.Message, "Error Writing File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void od1_FileOk(object sender, CancelEventArgs e)
        {
            using (StreamReader sr = new StreamReader(od1.FileName))
            {
                tele_label1.Text = sr.ReadLine();
                x_tele.Text = sr.ReadLine();
                y_tele.Text = sr.ReadLine();
                z_tele.Text = sr.ReadLine();
                h_tele1.Text = sr.ReadLine();
            }
        }

        private void autoLoad_FileOk(object sender, CancelEventArgs e)
        {
            string loop = "dont";
            if (checkBox1.Checked == true)
            {
                loop = "loop";
            }

            System.Diagnostics.Process.Start("AutoBot.exe", listView2.SelectedItems[0].SubItems[1].Text + " " + loop + " " + autoLoad.FileName);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            od2.ShowDialog();
        }

        private void button19_Click(object sender, EventArgs e)
        {
            od3.ShowDialog();
        }

        private void button20_Click(object sender, EventArgs e)
        {
            od4.ShowDialog();
        }

        private void od2_FileOk(object sender, CancelEventArgs e)
        {
            using (StreamReader sr = new StreamReader(od2.FileName))
            {
                tele_label2.Text = sr.ReadLine();
                x_tele2.Text = sr.ReadLine();
                y_tele2.Text = sr.ReadLine();
                z_tele2.Text = sr.ReadLine();
                h_tele2.Text = sr.ReadLine();
            }
        }

        private void od3_FileOk(object sender, CancelEventArgs e)
        {
            using (StreamReader sr = new StreamReader(od3.FileName))
            {
                tele_label3.Text = sr.ReadLine();
                x_tele3.Text = sr.ReadLine();
                y_tele3.Text = sr.ReadLine();
                z_tele3.Text = sr.ReadLine();
                h_tele3.Text = sr.ReadLine();
            }
        }

        private void od4_FileOk(object sender, CancelEventArgs e)
        {
            using (StreamReader sr = new StreamReader(od4.FileName))
            {
                tele_label4.Text = sr.ReadLine();
                x_tele4.Text = sr.ReadLine();
                y_tele4.Text = sr.ReadLine();
                z_tele4.Text = sr.ReadLine();
                h_tele4.Text = sr.ReadLine();
            }
        }

        private void sdall_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                string[] lines = { tele_label1.Text, x_tele.Text, y_tele.Text, z_tele.Text, h_tele1.Text, tele_label2.Text, x_tele2.Text, y_tele2.Text, z_tele2.Text, h_tele2.Text, tele_label3.Text, x_tele3.Text, y_tele3.Text, z_tele3.Text, h_tele3.Text, tele_label4.Text, x_tele4.Text, y_tele4.Text, z_tele4.Text, h_tele4.Text };
                Stream s = sdall.OpenFile();
                StreamWriter sw = new StreamWriter(s, Encoding.Unicode);
                foreach (string line in lines)
                    sw.WriteLine(line);
                sw.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: Could not write file. Please try again later. Error message: " + ex.Message, "Error Writing File", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void button21_Click(object sender, EventArgs e)
        {
            sdall.ShowDialog();
        }

        private void button22_Click(object sender, EventArgs e)
        {
            odall.ShowDialog();
        }

        private void odall_FileOk(object sender, CancelEventArgs e)
        {
            using (StreamReader sr = new StreamReader(odall.FileName))
            {
                tele_label1.Text = sr.ReadLine();
                x_tele.Text = sr.ReadLine();
                y_tele.Text = sr.ReadLine();
                z_tele.Text = sr.ReadLine();
                h_tele1.Text = sr.ReadLine();
                tele_label2.Text = sr.ReadLine();
                x_tele2.Text = sr.ReadLine();
                y_tele2.Text = sr.ReadLine();
                z_tele2.Text = sr.ReadLine();
                h_tele2.Text = sr.ReadLine();
                tele_label3.Text = sr.ReadLine();
                x_tele3.Text = sr.ReadLine();
                y_tele3.Text = sr.ReadLine();
                z_tele3.Text = sr.ReadLine();
                h_tele3.Text = sr.ReadLine();
                tele_label4.Text = sr.ReadLine();
                x_tele4.Text = sr.ReadLine();
                y_tele4.Text = sr.ReadLine();
                z_tele4.Text = sr.ReadLine();
                h_tele4.Text = sr.ReadLine();
            }
        }



        private void button23_Click(object sender, EventArgs e)
        {
            //byte[] memory;
            //int bytesWrote;
            //int bytesRead;
            byte[] memory = new byte[4];
            byte[] buffer = BitConverter.GetBytes(2);
            //memory = pReader.ReadProcessMemory((IntPtr)0x7f94e0, 4, out bytesRead);
            ReadProcessMemory(pHandle, (UIntPtr)0x7f94e0, memory, (UIntPtr)memory.Length, IntPtr.Zero);
            int pointerbase = BitConverter.ToInt32(memory, 0);
            pointerbase += 0xa8;
            //memory = BitConverter.GetBytes(2);
            //pReader.WriteProcessMemory((IntPtr)pointerbase, memory, out bytesWrote);
            WriteProcessMemory(pHandle, (UIntPtr)pointerbase, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
        }

        private void button24_Click(object sender, EventArgs e)
        {
            if (Follow == true)
            {
                Follow = false;
            }
            else
            {
                Follow = true;
            }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            runBox.Text = "0.6999999881";
        }

        private void buttonAllScriptsEnabled_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem listViewItem in listViewScripts.Items)
            {
                listViewItem.Checked = true;
            }
        }

        private void buttonAllScriptsDisabled_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem listViewItem in listViewScripts.Items)
            {
                listViewItem.Checked = false;
            }
        }

        private void timerScripts_Tick(object sender, EventArgs e)
        {
            
        }

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.newagesoldier.com");
        }

        private void x64CDependenciesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://go.microsoft.com/?linkid=9709949");
        }

        private void buttonRefreshSpawnList_Click(object sender, EventArgs e)
        {
            listViewSpawnList.Items.Clear();

            TrainerForm_RefreshSpawnList();
        }

        private void buttonTargetSpawn_Click(object sender, EventArgs e)
        {
            if (listViewSpawnList.SelectedItems.Count == 0)
            {
                return;
            }

            string address_text = listViewSpawnList.SelectedItems[0].SubItems[1].Text;

            int address_value = Convert.ToInt32(address_text, 16);

            byte[] buffer = BitConverter.GetBytes(address_value);
            WriteProcessMemory(pHandle, (UIntPtr)0x007F94EC, buffer, (UIntPtr)buffer.Length, IntPtr.Zero);
        }

        private void buttonWarpToSpawn_Click(object sender, EventArgs e)
        {
            TrainerForm_WarpToSpawn();
        }

        private void listViewSpawnList_DoubleClick(object sender, EventArgs e)
        {
            if (listViewSpawnList.SelectedItems.Count == 1)
            {
                TrainerForm_WarpToSpawn();
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
            autoLoad.ShowDialog();
        }

        int old_buffspell1 = 0;
        int old_buffspell2 = 0;
        int old_buffspell3 = 0;
        int old_buffspell4 = 0;
        int old_buffspell5 = 0;
        int old_buffspell6 = 0;
        int old_buffspell7 = 0;
        int old_buffspell8 = 0;
        int old_buffspell9 = 0;
        int old_buffspell10 = 0;
        int old_buffspell11 = 0;
        int old_buffspell12 = 0;

        double old_bufftimer1 = 0;
        double old_bufftimer2 = 0;
        double old_bufftimer3 = 0;
        double old_bufftimer4 = 0;
        double old_bufftimer5 = 0;
        double old_bufftimer6 = 0;
        double old_bufftimer7 = 0;
        double old_bufftimer8 = 0;
        double old_bufftimer9 = 0;
        double old_bufftimer10 = 0;
        double old_bufftimer11 = 0;
        double old_bufftimer12 = 0;

        static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++)
            {
                if (!comparer.Equals(a1[i], a2[i])) return false;
            }
            return true;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            byte[] memory;
            bool printit = false;

            while (true)
            {
                OpenProcess();
                    int[] offsets = { 0x3F94CC, 0x4c };
                    byte[] pre_y_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets), pre_y_address, (UIntPtr)4, IntPtr.Zero);
                    float y_address = BitConverter.ToSingle(pre_y_address, 0);
                    y_address = (float)Math.Round(y_address, 2);

                    int[] offsets2 = { 0x3F94CC, 0x48 };
                    byte[] pre_x_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets2), pre_x_address, (UIntPtr)4, IntPtr.Zero);
                    float x_address = BitConverter.ToSingle(pre_x_address, 0);
                    x_address = (float)Math.Round(x_address, 2);

                    int[] offsets3 = { 0x3F94CC, 0x50 };
                    byte[] pre_z_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets3), pre_z_address, (UIntPtr)4, IntPtr.Zero);
                    float z_address = BitConverter.ToSingle(pre_z_address, 0);
                    z_address = (float)Math.Round(z_address, 2);

                    int[] offsets14 = { 0x003F94CC, 0x54 };
                    byte[] pre_heading = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets14), pre_heading, (UIntPtr)4, IntPtr.Zero);
                    uint heading_write = BitConverter.ToUInt32(pre_heading, 0);
                    float heading = BitConverter.ToSingle(pre_heading, 0);
                    heading = (float)Math.Round(heading, 2);

                    int[] offsets4 = { 0x23DEA8, 0x8cfc };
                    byte[] pre_map_address = new byte[255];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets4), pre_map_address, (UIntPtr)255, IntPtr.Zero);
                    string map_address = System.Text.Encoding.UTF8.GetString(pre_map_address);

                    byte[] pre_map_address_long = new byte[255];
                    ReadProcessMemory(pHandle, (UIntPtr)0x007987E4, pre_map_address_long, (UIntPtr)255, IntPtr.Zero);
                    map_label.Text = System.Text.Encoding.UTF8.GetString(pre_map_address_long);

                    int[] offsets5 = { 0x3F94E8, 0xb78 };
                    byte[] pre_bank_plat_int = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets5), pre_bank_plat_int, (UIntPtr)4, IntPtr.Zero);
                    int bank_plat_int = BitConverter.ToInt32(pre_bank_plat_int, 0);
                    
                    int[] offsets6 = { 0x3F94E8, 0xb7c };
                    byte[] pre_bank_gold_int = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets6), pre_bank_gold_int, (UIntPtr)4, IntPtr.Zero);
                    int bank_gold_int = BitConverter.ToInt32(pre_bank_gold_int, 0);

                    int[] offsets7 = { 0x3F94E8, 0xb80 };
                    byte[] pre_bank_silver_int = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets7), pre_bank_silver_int, (UIntPtr)4, IntPtr.Zero);
                    int bank_silver_int = BitConverter.ToInt32(pre_bank_silver_int, 0);

                    int[] offsets8 = { 0x3F94E8, 0xb84 };
                    byte[] pre_bank_copper_int = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets8), pre_bank_copper_int, (UIntPtr)4, IntPtr.Zero);
                    int bank_copper_int = BitConverter.ToInt32(pre_bank_copper_int, 0);

                    y_label.Text = y_address.ToString();
                    x_label.Text = x_address.ToString();
                    z_label.Text = z_address.ToString();
                    //map_label.Text = mem.ReadString(map_address);

                    bank_plat.Text = bank_plat_int.ToString();
                    bank_gold.Text = bank_gold_int.ToString();
                    bank_silver.Text = bank_silver_int.ToString();
                    bank_copper.Text = bank_copper_int.ToString();
                    heading_label.Text = heading.ToString();

                    //good codes, just never used them.
                    //uint player_plat_int = (uint)mem.ReadPointer(base_address + 0x3F94E8) + 0xb68;
                    //uint player_gold_int = (uint)mem.ReadPointer(base_address + 0x3F94E8) + 0xb6c;
                    //uint player_silver_int = (uint)mem.ReadPointer(base_address + 0x3F94E8) + 0xb70;
                    //uint player_copper_int = (uint)mem.ReadPointer(base_address + 0x3F94E8) + 0xb74;

                    int[] offsets24 = { 0x3F94E8, 0x02 };
                    byte[] pre_char_name = new byte[255];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets24), pre_char_name, (UIntPtr)255, IntPtr.Zero);
                    string char_name = System.Text.Encoding.UTF8.GetString(pre_char_name);

                    int[] offsets9 = { 0x3F94E8, 0x9c };
                    byte[] pre_current_hp = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets9), pre_current_hp, (UIntPtr)4, IntPtr.Zero);
                    int current_hp = BitConverter.ToInt32(pre_current_hp, 0);

                    int[] offsets10 = { 0x3F94CC, 0x98 };
                    byte[] pre_max_hp = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets10), pre_max_hp, (UIntPtr)4, IntPtr.Zero);
                    int max_hp = BitConverter.ToInt32(pre_max_hp, 0);

                    int[] offsets11 = { 0x3F94E8, 0x9a };
                    byte[] pre_current_mp = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets11), pre_current_mp, (UIntPtr)4, IntPtr.Zero);
                    int current_mp = BitConverter.ToInt32(pre_current_mp, 0);

                    int[] offsets12 = { 0x0023B920, 0x30, 0xbc, 0x204, 0x310, 0x796 };
                    byte[] pre_max_mp = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets12), pre_max_mp, (UIntPtr)4, IntPtr.Zero);
                    int max_mp = BitConverter.ToInt32(pre_max_mp, 0);

                    int[] offsets13 = { 0x3F94E8, 0x94 };
                    byte[] pre_current_xp = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets13), pre_current_xp, (UIntPtr)4, IntPtr.Zero);
                    int current_xp = BitConverter.ToInt32(pre_current_xp, 0);

                    byte[] buffer = BitConverter.GetBytes(2);
                    ReadProcessMemory(pHandle, (UIntPtr)0x7f94e0, buffer, (UIntPtr)4, IntPtr.Zero);
                    int pointerbase = BitConverter.ToInt32(buffer, 0);
                    pointerbase += 0x104;
                    ReadProcessMemory(pHandle, (UIntPtr)pointerbase, buffer, (UIntPtr)4, IntPtr.Zero);
                    float run_speed = BitConverter.ToSingle(buffer, 0);

                    byte[] mousebuff = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)0x8092E8, mousebuff, (UIntPtr)4, IntPtr.Zero);
                    mousex.Text = BitConverter.ToInt32(mousebuff, 0).ToString();

                    byte[] mousebuff2 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)0x8092EC, mousebuff2, (UIntPtr)4, IntPtr.Zero);
                    mousey.Text = BitConverter.ToInt32(mousebuff2, 0).ToString();

                    int[] offsets17 = { 0x3F94EC, 0xad };
                    byte[] t_level_buffer = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets17), t_level_buffer, (UIntPtr)4, IntPtr.Zero);
                    int t_level = BitConverter.ToInt32(t_level_buffer, 0);

                    if (t_level >= 1)
                    {
                        label11.Text = "Level: " + t_level.ToString();
                        button24.Enabled = true;
                    }
                    else
                    {
                        label11.Text = "Level: ";
                        button24.Enabled = false;
                    }

                    int[] offsets15 = { 0x3F94EC, 0x1 };
                    byte[] pre_t_name = new byte[255];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets15), pre_t_name, (UIntPtr)255, IntPtr.Zero);
                    string t_name = System.Text.Encoding.UTF8.GetString(pre_t_name);

                    if (t_level >= 1)
                    {
                        label14.Text = "Name: " + t_name;
                        button24.Enabled = true;
                    }
                    else
                    {
                        label14.Text = "Name: ";
                        button24.Enabled = false;
                    }

                    int[] offsets18 = { 0x3F94EC, 0xa9 };
                    byte[] pre_t_class = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets18), pre_t_class, (UIntPtr)4, IntPtr.Zero);
                    int t_class = BitConverter.ToInt32(pre_t_class, 0);

                    charClass(t_class);
                    name_label.Text = char_name;
                    hp_stats.Text = current_hp + " / " + max_hp;
                    mp_stats.Text = (byte)current_mp + " / " + (byte)max_mp;
                    int cur_xp;
                    if (current_xp > 330)
                    {
                        cur_xp = 0;
                    }
                    else
                    {
                        cur_xp = current_xp;
                    }
                    xp_stats.Text = cur_xp + " / 330";

                    if ((runBox.Text != "") && (run_speed != float.Parse(runBox.Text)))
                    {
                        byte[] runbuffer = new byte[4];
                        ReadProcessMemory(pHandle, (UIntPtr)0x7f94e0, runbuffer, (UIntPtr)4, IntPtr.Zero);
                        int pointerbase2 = BitConverter.ToInt32(runbuffer, 0);
                        pointerbase2 += 0x104;
                        memory = BitConverter.GetBytes(Convert.ToSingle(runBox.Text));
                        WriteProcessMemory(pHandle, (UIntPtr)pointerbase2, memory, (UIntPtr)memory.Length, IntPtr.Zero);
                    }

                    int[] offsets19 = { 0x3F94EC, 0x50 };
                    byte[] pre_t_z_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets19), pre_t_z_address, (UIntPtr)4, IntPtr.Zero);
                    float t_z_address = BitConverter.ToSingle(pre_t_z_address, 0);
                    t_z_address = (float)Math.Round(t_z_address, 2);

                    int[] offsets20 = { 0x3F94EC, 0x4c };
                    byte[] pre_t_y_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets20), pre_t_y_address, (UIntPtr)4, IntPtr.Zero);
                    float t_y_address = BitConverter.ToSingle(pre_t_y_address, 0);
                    t_y_address = (float)Math.Round(t_y_address, 2);

                    int[] offsets21 = { 0x3F94EC, 0x48 };
                    byte[] pre_t_x_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets21), pre_t_x_address, (UIntPtr)4, IntPtr.Zero);
                    float t_x_address = BitConverter.ToSingle(pre_t_x_address, 0);
                    t_x_address = (float)Math.Round(t_x_address, 2);

                    int[] offsets22 = { 0x3F94EC, 0x54 };
                    byte[] pre_t_h_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets22), pre_t_h_address, (UIntPtr)4, IntPtr.Zero);
                    float t_h_address = BitConverter.ToSingle(pre_t_h_address, 0);
                    t_h_address = (float)Math.Round(t_h_address, 2);

                    int[] offsets16 = { 0x3F94eC, 0x9c };
                    byte[] pre_thealth = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets16), pre_thealth, (UIntPtr)4, IntPtr.Zero);
                    int thealth = BitConverter.ToInt32(pre_thealth, 0);
                    int[] offsets23 = { 0x3F94eC, 0x98 };
                    byte[] pre_thealth_max = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets23), pre_thealth_max, (UIntPtr)4, IntPtr.Zero);
                    int thealth_max = BitConverter.ToInt32(pre_thealth_max, 0);

                    string display_thealth = "";
                    if (thealth_max == 100) //FIX: Need to compare this to max HP. If max HP is not 100, then its you.
                    {
                        display_thealth = thealth.ToString() + "%";
                    }
                    else
                    {
                        display_thealth = thealth.ToString() + " / " + thealth_max.ToString();
                    }
                    
                    t_health.Text = "Health: " + display_thealth;
                    target_y.Text = "Y: " + t_y_address.ToString();
                    target_x.Text = "X: " + t_x_address.ToString();
                    target_z.Text = "Z: " + t_z_address.ToString();
                    target_h.Text = "H: " + t_h_address.ToString();

                    int[] offsets25 = { 0x003F94E8, 0x268 };
                    byte[] pre_buffspell1 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets25), pre_buffspell1, (UIntPtr)4, IntPtr.Zero);
                    int buffspell1 = BitConverter.ToInt16(pre_buffspell1, 0);

                    int[] offsets26 = { 0x003F94E8, 0x269 };
                    byte[] pre_bufftimer1 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets26), pre_bufftimer1, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer1 = BitConverter.ToInt32(pre_bufftimer1, 0);

                    int[] offsets27 = { 0x003F94E8, 0x272 };
                    byte[] pre_buffspell2 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets27), pre_buffspell2, (UIntPtr)4, IntPtr.Zero);
                    int buffspell2 = BitConverter.ToInt16(pre_buffspell2, 0);

                    int[] offsets28 = { 0x003F94E8, 0x273 };
                    byte[] pre_bufftimer2 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets28), pre_bufftimer2, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer2 = BitConverter.ToInt32(pre_bufftimer2, 0);

                    int[] offsets29 = { 0x003F94E8, 0x27c };
                    byte[] pre_buffspell3 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets29), pre_buffspell3, (UIntPtr)4, IntPtr.Zero);
                    int buffspell3 = BitConverter.ToInt16(pre_buffspell3, 0);

                    int[] offsets30 = { 0x003F94E8, 0x27d };
                    byte[] pre_bufftimer3 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets30), pre_bufftimer3, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer3 = BitConverter.ToInt32(pre_bufftimer3, 0);

                    int[] offsets31 = { 0x003F94E8, 0x286 };
                    byte[] pre_buffspell4 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets31), pre_buffspell4, (UIntPtr)4, IntPtr.Zero);
                    int buffspell4 = BitConverter.ToInt16(pre_buffspell4, 0);

                    int[] offsets32 = { 0x003F94E8, 0x287 };
                    byte[] pre_bufftimer4 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets32), pre_bufftimer4, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer4 = BitConverter.ToInt32(pre_bufftimer4, 0);

                    int[] offsets33 = { 0x003F94E8, 0x290 };
                    byte[] pre_buffspell5 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets33), pre_buffspell5, (UIntPtr)4, IntPtr.Zero);
                    int buffspell5 = BitConverter.ToInt16(pre_buffspell5, 0);

                    int[] offsets34 = { 0x003F94E8, 0x291 };
                    byte[] pre_bufftimer5 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets34), pre_bufftimer5, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer5 = BitConverter.ToInt32(pre_bufftimer5, 0);

                    int[] offsets35 = { 0x003F94E8, 0x29a };
                    byte[] pre_buffspell6 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets35), pre_buffspell6, (UIntPtr)4, IntPtr.Zero);
                    int buffspell6 = BitConverter.ToInt16(pre_buffspell6, 0);

                    int[] offsets36 = { 0x003F94E8, 0x29b };
                    byte[] pre_bufftimer6 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets36), pre_bufftimer6, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer6 = BitConverter.ToInt32(pre_bufftimer6, 0);

                    int[] offsets37 = { 0x003F94E8, 0x2a4 };
                    byte[] pre_buffspell7 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets37), pre_buffspell7, (UIntPtr)4, IntPtr.Zero);
                    int buffspell7 = BitConverter.ToInt16(pre_buffspell7, 0);

                    int[] offsets38 = { 0x003F94E8, 0x2a5 };
                    byte[] pre_bufftimer7 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets38), pre_bufftimer7, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer7 = BitConverter.ToInt32(pre_bufftimer7, 0);

                    int[] offsets39 = { 0x003F94E8, 0x2ae };
                    byte[] pre_buffspell8 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets39), pre_buffspell8, (UIntPtr)4, IntPtr.Zero);
                    int buffspell8 = BitConverter.ToInt16(pre_buffspell8, 0);

                    int[] offsets40 = { 0x003F94E8, 0x2af };
                    byte[] pre_bufftimer8 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets40), pre_bufftimer8, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer8 = BitConverter.ToInt32(pre_bufftimer8, 0);

                    int[] offsets41 = { 0x003F94E8, 0x2b8 };
                    byte[] pre_buffspell9 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets41), pre_buffspell9, (UIntPtr)4, IntPtr.Zero);
                    int buffspell9 = BitConverter.ToInt16(pre_buffspell9, 0);

                    int[] offsets42 = { 0x003F94E8, 0x2b9 };
                    byte[] pre_bufftimer9 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets42), pre_bufftimer9, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer9 = BitConverter.ToInt32(pre_bufftimer9, 0);

                    int[] offsets43 = { 0x003F94E8, 0x2c2 };
                    byte[] pre_buffspell10 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets43), pre_buffspell10, (UIntPtr)4, IntPtr.Zero);
                    int buffspell10 = BitConverter.ToInt16(pre_buffspell10, 0);

                    int[] offsets44 = { 0x003F94E8, 0x2c3 };
                    byte[] pre_bufftimer10 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets44), pre_bufftimer10, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer10 = BitConverter.ToInt32(pre_bufftimer10, 0);

                    int[] offsets45 = { 0x003F94E8, 0x2cc };
                    byte[] pre_buffspell11 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets45), pre_buffspell11, (UIntPtr)4, IntPtr.Zero);
                    int buffspell11 = BitConverter.ToInt16(pre_buffspell11, 0);

                    int[] offsets46 = { 0x003F94E8, 0x2cd };
                    byte[] pre_bufftimer11 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets46), pre_bufftimer11, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer11 = BitConverter.ToInt32(pre_bufftimer11, 0);

                    int[] offsets47 = { 0x003F94E8, 0x2d6 };
                    byte[] pre_buffspell12 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets47), pre_buffspell12, (UIntPtr)4, IntPtr.Zero);
                    int buffspell12 = BitConverter.ToInt16(pre_buffspell12, 0);

                    int[] offsets48 = { 0x003F94E8, 0x2d7 };
                    byte[] pre_bufftimer12 = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets48), pre_bufftimer12, (UIntPtr)4, IntPtr.Zero);
                    int bufftimer12 = BitConverter.ToInt32(pre_bufftimer12, 0);

                    int[] buffids = { buffspell1, buffspell2, buffspell3, 
                                                 buffspell4, buffspell5, buffspell6, 
                                                 buffspell7, buffspell8, buffspell9, 
                                                 buffspell10, buffspell11, buffspell12 };

                    int[] old_buffids = { old_buffspell1, old_buffspell2, old_buffspell3, 
                                                 old_buffspell4, old_buffspell5, old_buffspell6, 
                                                 old_buffspell7, old_buffspell8, old_buffspell9, 
                                                 old_buffspell10, old_buffspell11, old_buffspell12 };

                    double[] bufftimers = { ((bufftimer1 / 255) * 0.1), ((bufftimer2 / 255) * 0.1), ((bufftimer3 / 255) * 0.1), 
                                                 ((bufftimer4 / 255) * 0.1), ((bufftimer5 / 255) * 0.1), ((bufftimer6 / 255) * 0.1), 
                                                 ((bufftimer7 / 255) * 0.1), ((bufftimer8 / 255) * 0.1), ((bufftimer9 / 255) * 0.1), 
                                                 ((bufftimer10 / 255) * 0.1), ((bufftimer11 / 255) * 0.1), ((bufftimer12 / 255) * 0.1) };

                    double[] old_bufftimers = { old_bufftimer1, old_bufftimer2, old_bufftimer3, 
                                                 old_bufftimer4, old_bufftimer5, old_bufftimer6, 
                                                 old_bufftimer7, old_bufftimer8, old_bufftimer9, 
                                                 old_bufftimer10, old_bufftimer11, old_bufftimer12 };

                    if (ArraysEqual(buffids, old_buffids) == false)
                    {
                        printit = true;
                    }
                    if (ArraysEqual(bufftimers, old_bufftimers) == false)
                    {
                        printit = true;
                    }

                    old_buffspell1 = buffspell1;
                    old_buffspell2 = buffspell2;
                    old_buffspell3 = buffspell3;
                    old_buffspell4 = buffspell4;
                    old_buffspell5 = buffspell5;
                    old_buffspell6 = buffspell6;
                    old_buffspell7 = buffspell7;
                    old_buffspell8 = buffspell8;
                    old_buffspell9 = buffspell9;
                    old_buffspell10 = buffspell10;
                    old_buffspell11 = buffspell11;
                    old_buffspell12 = buffspell12;

                    old_bufftimer1 = bufftimers[0];
                    old_bufftimer2 = bufftimers[1];
                    old_bufftimer3 = bufftimers[2];
                    old_bufftimer4 = bufftimers[3];
                    old_bufftimer5 = bufftimers[4];
                    old_bufftimer6 = bufftimers[5];
                    old_bufftimer7 = bufftimers[6];
                    old_bufftimer8 = bufftimers[7];
                    old_bufftimer9 = bufftimers[8];
                    old_bufftimer10 = bufftimers[9];
                    old_bufftimer11 = bufftimers[10];
                    old_bufftimer12 = bufftimers[11];

                    if (printit == true)
                    {

                        listView1.Items.Clear();

                        string[] buffnames = new string[12];

                        string line;

                        for (int i = 0; i < 12; i++)
                        {
                            if (buffids[i] == 0)
                            {
                                continue;
                            }

                            System.IO.StreamReader file = new System.IO.StreamReader("buffs.txt");

                            while ((line = file.ReadLine()) != null)
                            {
                                if (line.Contains("#"))
                                {
                                    continue;
                                }

                                string[] words = line.Split('^');

                                int wordsid = Int32.Parse(words[0]);

                                if (wordsid == buffids[i])
                                {
                                    buffnames[i] = words[1];
                                    break;
                                }
                            }

                            file.Close();
                        }

                        for (int i = 0; i < 12; i++)
                        {
                            if ((bufftimers[i].ToString() != "0.1") && (bufftimers[i].ToString() != "0"))
                            {
                                string[] row = { bufftimers[i].ToString() + " minutes", /*buffids[i].ToString() + ":" +*/ buffnames[i] };

                                var listViewItem = new ListViewItem(row);
                                listView1.Items.Add(listViewItem);
                            }
                        }
                    }
                    printit = false;

                    if (checkBoxScripts.Checked == false)
                    {
                        return;
                    }

                    foreach (ListViewItem listViewItem in listViewScripts.Items)
                    {
                        int script_instructions_index = 3; // disabled column

                        if (listViewItem.Checked == true)
                        {
                            script_instructions_index = 2; // enabled column
                        }

                        string script_instructions = listViewItem.SubItems[script_instructions_index].Text;

                        string[] script_instructions_split = script_instructions.Split('^');

                        foreach (string script_instruction in script_instructions_split)
                        {
                            if (script_instruction.Length == 0)
                            {
                                continue;
                            }

                            string[] script_instruction_split = script_instruction.Split(':');

                            byte[] script_instruction_address = new byte[4];

                            string script_instruction_type = "";
                            string script_instruction_value = "";

                            int script_instruction_address_int = 0;
                            int script_instruction_value_int = 0;

                            byte[] script_instruction_value_bytes;

                            if (script_instruction_split[0] == "pointer" && script_instruction_split[2] == "offsets")
                            {
                                int script_instruction_pointer = Int32.Parse(script_instruction_split[1], System.Globalization.NumberStyles.AllowHexSpecifier);

                                ReadProcessMemory(pHandle, (UIntPtr)script_instruction_pointer, script_instruction_address, (UIntPtr)4, IntPtr.Zero);

                                string script_instruction_offsets = script_instruction_split[3];

                                string[] script_instruction_offsets_split = script_instruction_offsets.Split(',');

                                int current_offset = 1;

                                int num_offsets = script_instruction_offsets_split.Length;

                                foreach (string script_instruction_offset in script_instruction_offsets_split)
                                {
                                    int script_instruction_offset_int = Int32.Parse(script_instruction_offset, System.Globalization.NumberStyles.AllowHexSpecifier);

                                    script_instruction_address_int = BitConverter.ToInt32(script_instruction_address, 0);

                                    script_instruction_address_int += script_instruction_offset_int;

                                    if (current_offset == num_offsets)
                                    {
                                        break;
                                    }

                                    byte[] script_instruction_address_after_offset = new byte[4];
                                    ReadProcessMemory(pHandle, (UIntPtr)script_instruction_address_int, script_instruction_address_after_offset, (UIntPtr)4, IntPtr.Zero);

                                    script_instruction_address = script_instruction_address_after_offset;

                                    script_instruction_address_int = BitConverter.ToInt32(script_instruction_address, 0);
                                }

                                script_instruction_type = script_instruction_split[4];
                                script_instruction_value = script_instruction_split[5];
                            }
                            else
                            {
                                script_instruction_address_int = Int32.Parse(script_instruction_split[0], System.Globalization.NumberStyles.AllowHexSpecifier);

                                script_instruction_address = BitConverter.GetBytes(script_instruction_address_int);

                                script_instruction_type = script_instruction_split[1];
                                script_instruction_value = script_instruction_split[2];
                            }

                            int i = 0;

                            switch (script_instruction_type)
                            {
                                case "nops":
                                    int num_nops = Int32.Parse(script_instruction_value, System.Globalization.NumberStyles.AllowHexSpecifier);

                                    byte[] nops = new byte[num_nops];

                                    for (i = 0; i < nops.Length; i++)
                                    {
                                        nops[i] = 0x90;
                                    }

                                    WriteProcessMemory(pHandle, (UIntPtr)script_instruction_address_int, nops, (UIntPtr)num_nops, IntPtr.Zero);
                                    break;

                                case "bytes":
                                    string[] script_instruction_value_split = script_instruction_value.Split(',');

                                    int num_bytes = script_instruction_value_split.Length;

                                    byte[] write_bytes = new byte[num_bytes];

                                    for (i = 0; i < num_bytes; i++)
                                    {
                                        script_instruction_value_int = Int32.Parse(script_instruction_value_split[i], System.Globalization.NumberStyles.AllowHexSpecifier);
                                        script_instruction_value_bytes = BitConverter.GetBytes(script_instruction_value_int);

                                        write_bytes[i] = script_instruction_value_bytes[0];
                                    }

                                    WriteProcessMemory(pHandle, (UIntPtr)script_instruction_address_int, write_bytes, (UIntPtr)num_bytes, IntPtr.Zero);
                                    break;

                                case "byte":
                                    script_instruction_value_int = Int32.Parse(script_instruction_value, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    script_instruction_value_bytes = BitConverter.GetBytes(script_instruction_value_int);

                                    WriteProcessMemory(pHandle, (UIntPtr)script_instruction_address_int, script_instruction_value_bytes, (UIntPtr)1, IntPtr.Zero);
                                    break;

                                case "word":
                                    script_instruction_value_int = Int32.Parse(script_instruction_value, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    script_instruction_value_bytes = BitConverter.GetBytes(script_instruction_value_int);

                                    WriteProcessMemory(pHandle, (UIntPtr)script_instruction_address_int, script_instruction_value_bytes, (UIntPtr)2, IntPtr.Zero);
                                    break;

                                case "dword":
                                    script_instruction_value_int = Int32.Parse(script_instruction_value, System.Globalization.NumberStyles.AllowHexSpecifier);
                                    script_instruction_value_bytes = BitConverter.GetBytes(script_instruction_value_int);

                                    WriteProcessMemory(pHandle, (UIntPtr)script_instruction_address_int, script_instruction_value_bytes, (UIntPtr)4, IntPtr.Zero);
                                    break;

                                case "float":
                                    float script_instruction_value_float = float.Parse(script_instruction_value); //, System.Globalization.NumberStyles.Float);
                                    script_instruction_value_bytes = BitConverter.GetBytes(script_instruction_value_float);

                                    WriteProcessMemory(pHandle, (UIntPtr)script_instruction_address_int, script_instruction_value_bytes, (UIntPtr)1, IntPtr.Zero);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }

                    Thread.Sleep(100);
            }
        }

        private void button27_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();
            Process[] processlist = Process.GetProcesses();

            foreach (Process theprocess in processlist)
            {
                if (theprocess.ProcessName == "eqgame")
                {
                    string[] listView2Rows = { theprocess.ProcessName, theprocess.Id.ToString() };
                    var listView2Items = new ListViewItem(listView2Rows);
                    listView2.Items.Add(listView2Items);
                }
            }
            if (listView2.Items.Count > 0)
            {
                if (listView2.SelectedItems.Count == 0)
                {
                    listView2.Items[0].Selected = true;
                    listView2.Select();
                }
            }
        }        

        private void textBoxSpawnListFilter_GotFocus(object sender, EventArgs e)
        {
            this.AcceptButton = buttonRefreshSpawnList;
        }

        /*private void button28_Click(object sender, EventArgs e)
        {
            if (UltraVision == true)
            {
                UltraVision = false;
                int ultravision_address = 0x004C0D57;
                byte[] ultravision = new byte[1];
                ultravision[0] = 0xFF;

                WriteProcessMemory(pHandle, (UIntPtr)ultravision_address, ultravision, (UIntPtr)1, IntPtr.Zero);

                //removing the code below will make ultravision not work.
                int[] offsets = { 0x3F94E8, 0xC8A };
                WriteProcessMemory(pHandle, (UIntPtr)getPointer(offsets), ultravision, (UIntPtr)1, IntPtr.Zero);
            }

            if (UltraVision == false)
                UltraVision = true;

        }*/

        private void buttonResetCamera_Click(object sender, EventArgs e)
        {
            byte[] buffer = new byte[4];
            ReadProcessMemory(pHandle, (UIntPtr)0x7F94CC, buffer, (UIntPtr)4, IntPtr.Zero);

            int spawn_info = BitConverter.ToInt32(buffer, 0);

            byte[] buffer2 = new byte[4];
            ReadProcessMemory(pHandle, (UIntPtr)spawn_info + 0x84, buffer2, (UIntPtr)4, IntPtr.Zero);

            int actor_info = BitConverter.ToInt32(buffer2, 0);

            byte[] buffer3 = new byte[4];
            ReadProcessMemory(pHandle, (UIntPtr)actor_info + 0x00, buffer3, (UIntPtr)4, IntPtr.Zero);

            //int view_actor = BitConverter.ToInt32(buffer3, 0);
            //MessageBox.Show("view_actor: " + view_actor);

            WriteProcessMemory(pHandle, (UIntPtr)0x0063D6C0, buffer3, (UIntPtr)4, IntPtr.Zero);
        }

        private void buttonCameraOnSpawn_Click(object sender, EventArgs e)
        {
            if (listViewSpawnList.SelectedItems.Count == 0)
            {
                return;
            }

            string address_text = listViewSpawnList.SelectedItems[0].SubItems[1].Text;

            int address_value = Convert.ToInt32(address_text, 16);

            int spawn_info = address_value;

            byte[] buffer2 = new byte[4];
            ReadProcessMemory(pHandle, (UIntPtr)spawn_info + 0x84, buffer2, (UIntPtr)4, IntPtr.Zero);

            int actor_info = BitConverter.ToInt32(buffer2, 0);

            byte[] buffer3 = new byte[4];
            ReadProcessMemory(pHandle, (UIntPtr)actor_info + 0x00, buffer3, (UIntPtr)4, IntPtr.Zero);

            //int view_actor = BitConverter.ToInt32(buffer3, 0);
            //MessageBox.Show("view_actor: " + view_actor);

            WriteProcessMemory(pHandle, (UIntPtr)0x0063D6C0, buffer3, (UIntPtr)4, IntPtr.Zero);
        }

    }
}
