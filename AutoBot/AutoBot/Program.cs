using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;

namespace ConsoleApplication1
{
    class Program
    {
        public static IntPtr pHandle;
        private static ProcessModule mainModule;

        #region DllImports
        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesWritten);

        [DllImportAttribute("User32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
        private static extern bool _CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);

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

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType
            );

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

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        internal static extern Int32 WaitForSingleObject(
            IntPtr handle,
            Int32 milliseconds
            );

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            string lpBuffer,
            UIntPtr nSize,
            out IntPtr lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(
        IntPtr hObject
        );

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            UInt32 dwDesiredAccess,
            Int32 bInheritHandle,
            Int32 dwProcessId
            ); 
        #endregion

        private static string eqgameID;

        static public bool InjectDLL(IntPtr hProcess, String strDLLName)
        {
            IntPtr bytesout;
 
            Int32 LenWrite = strDLLName.Length + 1; 
            IntPtr AllocMem = (IntPtr)VirtualAllocEx(hProcess, (IntPtr)null, (uint)LenWrite, 0x1000, 0x40); //allocation pour WriteProcessMemory  

            WriteProcessMemory(hProcess, AllocMem, strDLLName, (UIntPtr)LenWrite, out bytesout);
            UIntPtr Injector = (UIntPtr)GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (Injector == null)
            {
                return false;
            }

            IntPtr hThread = (IntPtr)CreateRemoteThread(hProcess, (IntPtr)null, 0, Injector, AllocMem, 0, out bytesout);
            if (hThread == null)
            {
                return false;
            }

            int Result = WaitForSingleObject(hThread, 10 * 1000);
            if (Result == 0x00000080L || Result == 0x00000102L)
            {
                if (hThread != null)
                {
                    CloseHandle(hThread);
                }
                return false;
            }
            VirtualFreeEx(hProcess, AllocMem, (UIntPtr)0, 0x8000);

            if (hThread != null)
            {
                CloseHandle(hThread);
            }

            return true;
        }

        public static void OpenProcess(string processID)
        {
            Int32 ProcID = Convert.ToInt32(processID);
            Process procs = Process.GetProcessById(ProcID);
            if (procs == null)
            {
                Console.WriteLine("ERROR: OpenProcess() Proc: NULL");
                return;
            }

            pHandle = OpenProcess(0x1F0FFF, 1, ProcID);
            IntPtr hProcess = (IntPtr)OpenProcess(0x1F0FFF, 1, ProcID);
            mainModule = procs.MainModule;
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

        public static void WriteString(long Address, string str)
        {
            str += '\0';
            byte[] bytes = System.Text.ASCIIEncoding.Default.GetBytes(str);
            WriteProcessMemory(pHandle, (UIntPtr)Address, bytes, (UIntPtr)bytes.Length, IntPtr.Zero);
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

        public static void WriteLog(string dialog)
        {
            StreamWriter log;
            string logfile = "logfile.txt";

            if (!File.Exists(logfile))
            {
                log = new StreamWriter(logfile);
            }
            else
            {
                log = File.AppendText(logfile);
            }

            log.WriteLine(dialog);

            log.Close();
        }

        static void TargetPlayer (string name){
            OpenProcess(eqgameID);
            byte[] buffer = new byte[4];
            ReadProcessMemory(pHandle, (UIntPtr)0x007F94CC, buffer, (UIntPtr)4, IntPtr.Zero);

            int player_spawn_info = BitConverter.ToInt32(buffer, 0);

            int spawn_info_address = player_spawn_info;

            byte[] buffer2 = new byte[4];
            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x78, buffer2, (UIntPtr)4, IntPtr.Zero);

            int spawn_next_spawn_info = BitConverter.ToInt32(buffer2, 0);

            spawn_info_address = spawn_next_spawn_info;

            bool tryAgain = true;
            while (tryAgain == true)
            {
                try
                {
                    for (int i = 0; i < 4096; i++) //we could make this a seperate function if we need to.
                    {
                        byte[] buffer3 = new byte[4];
                        ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x78, buffer3, (UIntPtr)4, IntPtr.Zero);

                        spawn_next_spawn_info = BitConverter.ToInt32(buffer3, 0);

                        if (spawn_next_spawn_info == 0x00000000)
                        {
                            tryAgain = false;
                            continue; //reached the end I guess
                        }

                        byte[] buffer4 = new byte[64];
                        try
                        {
                            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x01, buffer4, (UIntPtr)64, IntPtr.Zero);
                        }
                        catch
                        {
                            Console.WriteLine("ERROR: CANT FIND SPAWN INFO ADDRESS 0X01");
                            System.Threading.Thread.Sleep(30000);
                            break;
                        }

                        string spawn_info_name = Encoding.UTF8.GetString(buffer4);

                        byte[] buffer5 = new byte[4];
                        try
                        {
                            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x48, buffer5, (UIntPtr)4, IntPtr.Zero);
                        }
                        catch
                        {
                            Console.WriteLine("ERROR: CANT FIND SPAWN INFO ADDRESS 0X48");
                            System.Threading.Thread.Sleep(30000);
                            break;
                        }

                        float spawn_info_y = BitConverter.ToSingle(buffer5, 0);
                        spawn_info_y = (float)Math.Round(spawn_info_y, 2);

                        byte[] buffer6 = new byte[4];
                        try
                        {
                            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x4C, buffer6, (UIntPtr)4, IntPtr.Zero);
                        }
                        catch
                        {
                            Console.WriteLine("ERROR: CANT FIND SPAWN INFO ADDRESS 0X4C");
                            System.Threading.Thread.Sleep(30000);
                            break;
                        }

                        float spawn_info_x = BitConverter.ToSingle(buffer6, 0);
                        spawn_info_x = (float)Math.Round(spawn_info_x, 2);

                        byte[] buffer7 = new byte[4];
                        try
                        {
                            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x50, buffer7, (UIntPtr)4, IntPtr.Zero);
                        }
                        catch
                        {
                            Console.WriteLine("ERROR: CANT FIND SPAWN INFO ADDRESS 0X50");
                            System.Threading.Thread.Sleep(30000);
                            break;
                        }
                        
                        float spawn_info_z = BitConverter.ToSingle(buffer7, 0);
                        spawn_info_z = (float)Math.Round(spawn_info_z, 2);

                        byte[] buffer8 = new byte[4];
                        try
                        {
                            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x54, buffer8, (UIntPtr)4, IntPtr.Zero);
                        }
                        catch
                        {
                            Console.WriteLine("ERROR: CANT FIND SPAWN INFO ADDRESS 0X54");
                            System.Threading.Thread.Sleep(30000);
                            break;
                        }
                        
                        float spawn_info_heading = BitConverter.ToSingle(buffer8, 0);
                        spawn_info_heading = (float)Math.Round(spawn_info_heading, 2);

                        if (spawn_info_name.Contains(name) == false) //find our NPC's name
                        {
                            //Console.WriteLine("Cant find NPC name... " + "Name: " + spawn_info_name + " Address: " + spawn_info_address.ToString("X8") + " X: " + spawn_info_x.ToString() + " Y: " + spawn_info_y.ToString() + " Z: " + spawn_info_z.ToString() + " H: " + spawn_info_heading.ToString()); //DEBUG
                            spawn_info_address = spawn_next_spawn_info;
                            continue;
                        }
                        
                        byte[] buffer9 = BitConverter.GetBytes(spawn_info_address);
                        WriteProcessMemory(pHandle, (UIntPtr)0x007F94EC, buffer9, (UIntPtr)buffer9.Length, IntPtr.Zero);

                        System.Threading.Thread.Sleep(350);

                        double angleInDegrees = spawn_info_heading / 1.42;
                        double cos = Math.Cos(angleInDegrees * (Math.PI / 180.0));
                        double sin = Math.Sin(angleInDegrees * (Math.PI / 180.0));
                        double reverse_x = spawn_info_x - Convert.ToInt32(20) * cos;
                        double reverse_y = spawn_info_y - Convert.ToInt32(20) * sin;

                        int[] offsets16 = { 0x3F94eC, 0x9c };
                        byte[] pre_thealth = new byte[4];
                        ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets16), pre_thealth, (UIntPtr)4, IntPtr.Zero);
                        int thealth = BitConverter.ToInt32(pre_thealth, 0);

                        if (thealth <= 0) //targets dead
                        {
                            return;
                        }
                        spawn_info_address = spawn_next_spawn_info;
                    }
                }
                catch
                {
                    Console.WriteLine("ERROR: OVERFLOW!");
                    System.Threading.Thread.Sleep(30000);
                    break;
                }
            }
            
        }

        static void HealCheck(string name, int percent)
        {
            int[] offsets16 = { 0x3F94eC, 0x9c };
            byte[] pre_thealth = new byte[4];

            while (true)
            {
                try
                {
                    TargetPlayer(name);
                    System.Threading.Thread.Sleep(1000);
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets16), pre_thealth, (UIntPtr)4, IntPtr.Zero);
                    int thealth = BitConverter.ToInt32(pre_thealth, 0);
                    if (percent > thealth)
                    {
                        break; // break the loop check and continue with the script
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(5000);
                        continue; //keep checking
                    }
                }
                catch
                {
                    Console.WriteLine("ERROR: HealCheck Try/Catch return");
                    WriteLog("[ERROR] HealCheck Try/Catch return");
                    break;
                }
            }
        }

        static void FollowTarget(string name)
        {
            TargetPlayer(name);
            //To Do
        }

        static void CheckTrade(string lastcommand)
        {
            Console.WriteLine("Checking for trade window.");
            int i = 0;
            int t = 0;
            bool tryAgain = true;
            byte[] memory = new byte[4];
            //int bytesRead;

            while (tryAgain == true)
            {
                try
                {
                            byte[] buffer = BitConverter.GetBytes(2);
                            ReadProcessMemory(pHandle, (UIntPtr)0x007F9574, memory, (UIntPtr)memory.Length, IntPtr.Zero);
                            int pointerbase = BitConverter.ToInt32(memory, 0);
                            pointerbase += 0x5f314;
                            ReadProcessMemory(pHandle, (UIntPtr)pointerbase, memory, (UIntPtr)memory.Length, IntPtr.Zero);
                            int tradewindow = BitConverter.ToInt32(memory, 0);
                            tradewindow = (byte)tradewindow;

                            if (tradewindow == 1)
                            {
                                //WriteLog("[CHECKTRADE] Trade window found!");
                                tryAgain = false;
                                break;
                            }
                            else
                            {
                                System.Threading.Thread.Sleep(200);
                                if (i >= 5)
                                {
                                    if (t >= 5)
                                    {
                                        Console.WriteLine("CheckTrade giving up.");
                                        break;
                                    }
                                    Console.WriteLine("Trying last command: " + lastcommand);
                                    //WriteLog("[CHECKTRADE] Trying last command: " + lastcommand);
                                    t++;
                                    i = 0;
                                    ParseReader(lastcommand);
                                }
                                else
                                {
                                    i++;
                                }
                                continue;
                            }
                }
                catch
                {
                    Console.WriteLine("ERROR: CheckTrade Try/Catch return");
                    WriteLog("[ERROR] CheckTrade Try/Catch return");
                    break;
                }
            }
        }

        static void CheckCursor(string lastcommand)
        {
            Console.WriteLine("Checking for cursor items.");
            int i = 0;
            int t = 0;
            bool tryAgain = true;
            byte[] memory = new byte[4];
            //int bytesRead;

            while (tryAgain == true)
            {
                try
                {
                            byte[] buffer = BitConverter.GetBytes(2);
                            ReadProcessMemory(pHandle, (UIntPtr)0x007F9510, memory, (UIntPtr)memory.Length, IntPtr.Zero);
                            int pointerbase = BitConverter.ToInt32(memory, 0);
                            pointerbase += 0x40;
                            ReadProcessMemory(pHandle, (UIntPtr)pointerbase, memory, (UIntPtr)memory.Length, IntPtr.Zero);
                            int cursor = BitConverter.ToInt32(memory, 0);
                            cursor = (byte)cursor;

                            if (cursor == 1)
                            {
                                //WriteLog("[CHECKCURSOR] Cursor item found!");
                                tryAgain = false;
                                break;
                            }
                            else
                            {
                                System.Threading.Thread.Sleep(200);
                                if (i >= 5)
                                {
                                    if (t >= 5)
                                    {
                                        Console.WriteLine("CheckCursor giving up.");
                                        break;
                                    }
                                    ParseReader(lastcommand);
                                    Console.WriteLine("Trying last command: " + lastcommand);
                                    //WriteLog("[CHECKCURSOR] Trying last command: " + lastcommand);
                                    i = 0;
                                    t++;
                                }
                                else
                                {
                                    i++;
                                }
                                continue;
                            }
                }
                catch
                {
                    Console.WriteLine("ERROR: CheckCursor Try/Catch return");
                    WriteLog("[ERROR] CheckCursor Try/Catch return (" + DateTime.Now + ")");
                    break;
                }
            }
        }

        static public UIntPtr getPointer(int[] offsets)
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

        //static bool firsttime = true;

        //static float old_x = 0;
        //static float old_y = 0;
        //static float old_z = 0;

        static int o = 0;

        static void Teleport(float value_x, float value_y, float value_z, float value_h)
        {
            byte[] write_y = BitConverter.GetBytes(value_y);
            byte[] write_x = BitConverter.GetBytes(value_x);
            byte[] write_z = BitConverter.GetBytes(value_z);

            WriteProcessMemory(pHandle, (UIntPtr)0x00798970, write_y, (UIntPtr)write_y.Length, IntPtr.Zero);
            WriteProcessMemory(pHandle, (UIntPtr)0x0079896C, write_x, (UIntPtr)write_x.Length, IntPtr.Zero);
            WriteProcessMemory(pHandle, (UIntPtr)0x00798974, write_z, (UIntPtr)write_z.Length, IntPtr.Zero);

            byte[] value2 = BitConverter.GetBytes(value_h);
            int[] offsets2 = { 0x003F94CC, 0x54 };
            WriteProcessMemory(pHandle, (UIntPtr)getPointer(offsets2), value2, (UIntPtr)value2.Length, IntPtr.Zero);

            /*byte[] buffer6 = pReader.ReadProcessMemory((IntPtr)0x00798970, 4, out store);
            float check_y = BitConverter.ToSingle(buffer6, 0);
            check_y = (float)Math.Round(check_y, 2);

            byte[] buffer7 = pReader.ReadProcessMemory((IntPtr)0x0079896C, 4, out store);
            float check_x = BitConverter.ToSingle(buffer7, 0);
            check_x = (float)Math.Round(check_x, 2);

            byte[] buffer8 = pReader.ReadProcessMemory((IntPtr)0x00798974, 4, out store);
            float check_z = BitConverter.ToSingle(buffer8, 0);
            check_z = (float)Math.Round(check_z, 2);

            if ((check_y == old_y || check_x == old_x || check_z == old_z) && firsttime == false)
            {
                Console.WriteLine("ERROR: XYZ safe value did not change before injection! Please try again.");
                //WriteLog("[ERROR] Found Error in Y and stopped the teleport.");
                return;
            }*/

                //inject our DLL
                String strDLLName = Environment.CurrentDirectory + "\\injectdll2.dll";
                System.Threading.Thread.Sleep(250);
                if (InjectDLL(pHandle, strDLLName) == true)
                {
                    System.Threading.Thread.Sleep(50);
                    int[] offsets3 = { 0x3F94CC, 0x4c };
                    byte[] pre_y_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets3), pre_y_address, (UIntPtr)4, IntPtr.Zero);
                    float y_address = BitConverter.ToSingle(pre_y_address, 0);
                    y_address = (float)Math.Round(y_address, 2);

                    int[] offsets4 = { 0x3F94CC, 0x48 };
                    byte[] pre_x_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets4), pre_x_address, (UIntPtr)4, IntPtr.Zero);
                    float x_address = BitConverter.ToSingle(pre_x_address, 0);
                    x_address = (float)Math.Round(x_address, 2);

                    int[] offsets5 = { 0x3F94CC, 0x50 };
                    byte[] pre_z_address = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets5), pre_z_address, (UIntPtr)4, IntPtr.Zero);
                    float z_address = BitConverter.ToSingle(pre_z_address, 0);
                    z_address = (float)Math.Round(z_address, 2);

                    int[] offsets6 = { 0x3F94CC, 0x54 };
                    byte[] pre_heading = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets6), pre_heading, (UIntPtr)4, IntPtr.Zero);
                    float heading = BitConverter.ToSingle(pre_heading, 0);
                    heading = (float)Math.Round(heading, 2);

                    int[] offsets7 = { 0x3F94E8, 0x9c };
                    byte[] pre_current_hp = new byte[4];
                    ReadProcessMemory(pHandle, (UIntPtr)getPointer(offsets7), pre_current_hp, (UIntPtr)4, IntPtr.Zero);
                    int current_hp = BitConverter.ToInt32(pre_current_hp, 0);

                    if (o >= 50)
                    {
                        Console.WriteLine("ERROR: Teleport Overflow!");
                        WriteLog("[ERROR] Teleport Overflow! (" + DateTime.Now + ")");
                        Environment.Exit(0);
                    } else if (current_hp <= 0)
                    {
                        Console.WriteLine("ERROR: Your character has died!");
                        WriteLog("[ERROR] Your character has died! (" + DateTime.Now + ")");
                        Environment.Exit(0);
                    }
                    else if (Math.Ceiling(x_address) != Math.Ceiling(value_x))
                    {
                        Console.WriteLine("ERROR: Caught X mis-match! Attempting to correct!");
                        //WriteLog("[ERROR] Caught X mis-match! Attempting to correct!");
                        Teleport(value_x, value_y, value_z, value_h); //GET ME OUTA HERE!
                        o++;
                    }
                    else if (Math.Ceiling(y_address) != Math.Ceiling(value_y))
                    {
                        Console.WriteLine("ERROR: Caught Y mis-match! Attempting to correct!");
                        //WriteLog("[ERROR] Caught X mis-match! Attempting to correct!");
                        Teleport(value_x, value_y, value_z, value_h); //GET ME OUTA HERE!
                        o++;
                    }
                    else if (Math.Ceiling(z_address) != Math.Ceiling(value_z))
                    {
                        Console.WriteLine("ERROR: Caught Z mis-match! Attempting to correct!");
                        //WriteLog("[ERROR] Caught X mis-match! Attempting to correct!");
                        Teleport(value_x, value_y, value_z, value_h); //GET ME OUTA HERE!
                        o++;
                    }
                    else
                    {
                        o = 0;
                    }

                    //old_x = value_x;
                    //old_y = value_y;
                    //old_z = value_z;
                    //firsttime = false;
                }
        }

        static void CheckDistance(string NPC, float x, float y, float z, int dist)
        {
                        byte[] buffer = new byte[4];
                        ReadProcessMemory(pHandle, (UIntPtr)0x007F94CC, buffer, (UIntPtr)4, IntPtr.Zero);

                        int player_spawn_info = BitConverter.ToInt32(buffer, 0);

                        int spawn_info_address = player_spawn_info;

                        byte[] buffer2 = new byte[4];
                        ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x78, buffer2, (UIntPtr)4, IntPtr.Zero);

                        int spawn_next_spawn_info = BitConverter.ToInt32(buffer2, 0);

                        spawn_info_address = spawn_next_spawn_info;

                            bool tryAgain = true;
                            while (tryAgain == true)
                            {
                                try
                                {
                                    for (int i = 0; i < 4096; i++) //we could make this a seperate function if we need to.
                                    {
                                        byte[] buffer3 = new byte[4];
                                        ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x78, buffer3, (UIntPtr)4, IntPtr.Zero);

                                        spawn_next_spawn_info = BitConverter.ToInt32(buffer3, 0);

                                        if (spawn_next_spawn_info == 0x00000000)
                                        {
                                            tryAgain = false;
                                            continue; //reached the end I guess
                                        }

                                        byte[] buffer4 = new byte[64];
                                        try
                                        {
                                            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x01, buffer4, (UIntPtr)64, IntPtr.Zero);
                                        }
                                        catch
                                        {
                                            Console.WriteLine("ERROR: CANT FIND SPAWN INFO ADDRESS 0X01");
                                            System.Threading.Thread.Sleep(30000);
                                            break;
                                        }

                                        string spawn_info_name = Encoding.UTF8.GetString(buffer4);

                                        byte[] buffer5 = new byte[4];
                                        try
                                        {
                                            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x48, buffer5, (UIntPtr)4, IntPtr.Zero);
                                        }
                                        catch
                                        {
                                            Console.WriteLine("ERROR: CANT FIND SPAWN INFO ADDRESS 0X48");
                                            System.Threading.Thread.Sleep(30000);
                                            break;
                                        }

                                        float spawn_info_y = BitConverter.ToSingle(buffer5, 0);
                                        spawn_info_y = (float)Math.Round(spawn_info_y, 2);

                                        byte[] buffer6 = new byte[4];
                                        try
                                        {
                                            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x4C, buffer6, (UIntPtr)4, IntPtr.Zero);
                                        }
                                        catch
                                        {
                                            Console.WriteLine("ERROR: CANT FIND SPAWN INFO ADDRESS 0X4C");
                                            System.Threading.Thread.Sleep(30000);
                                            break;
                                        }

                                        float spawn_info_x = BitConverter.ToSingle(buffer6, 0);
                                        spawn_info_x = (float)Math.Round(spawn_info_x, 2);

                                        byte[] buffer7 = new byte[4];
                                        try
                                        {
                                            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x50, buffer7, (UIntPtr)4, IntPtr.Zero);
                                        }
                                        catch
                                        {
                                            Console.WriteLine("ERROR: CANT FIND SPAWN INFO ADDRESS 0X50");
                                            System.Threading.Thread.Sleep(30000);
                                            break;
                                        }

                                        float spawn_info_z = BitConverter.ToSingle(buffer7, 0);
                                        spawn_info_z = (float)Math.Round(spawn_info_z, 2);

                                        byte[] buffer8 = new byte[4];
                                        try
                                        {
                                            ReadProcessMemory(pHandle, (UIntPtr)spawn_info_address + 0x54, buffer8, (UIntPtr)4, IntPtr.Zero);
                                        }
                                        catch
                                        {
                                            Console.WriteLine("ERROR: CANT FIND SPAWN INFO ADDRESS 0X54");
                                            System.Threading.Thread.Sleep(30000);
                                            break;
                                        }

                                        float spawn_info_heading = BitConverter.ToSingle(buffer8, 0);
                                        spawn_info_heading = (float)Math.Round(spawn_info_heading, 2);

                                        float difference = Math.Abs(spawn_info_x - x);
                                        float difference2 = Math.Abs(spawn_info_y - y);
                                        float difference3 = Math.Abs(spawn_info_z - z);
                                        if (spawn_info_name.Contains(NPC) == false) //find our NPC's name
                                        {
                                            //Console.WriteLine("Cant find NPC name... " + "Name: " + spawn_info_name + " Address: " + spawn_info_address.ToString("X8") + " X: " + spawn_info_x.ToString() + " Y: " + spawn_info_y.ToString() + " Z: " + spawn_info_z.ToString() + " H: " + spawn_info_heading.ToString()); //DEBUG
                                            spawn_info_address = spawn_next_spawn_info;
                                            continue;
                                        }

                                        //Console.WriteLine("checking difference X:" + difference + " Y:" + difference2 + " Z:" + difference3); //DEBUG
                                        if (difference <= dist && difference2 <= dist && difference3 <= (float)10)
                                        {
                                            //Console.WriteLine("WARNING: NPC is within distance! X:" + x + " tX:" + spawn_info_x.ToString() + " dX:" + difference + " | Y:" + y + " tY:" + spawn_info_y + " dY:" + difference2 + " | tZ:" + spawn_info_z + " dZ:" + difference3); //DEBUG
                                            Console.WriteLine("WARNING: NPC is in distance! Pausing for 30 seconds. Will recheck after.");
                                            System.Threading.Thread.Sleep(30000);
                                            continue;

                                        }
                                        else
                                        {
                                            tryAgain = false;
                                        }

                                        spawn_info_address = spawn_next_spawn_info;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("ERROR: OVERFLOW!");
                                    System.Threading.Thread.Sleep(30000);
                                    break;
                                }
            }
        }

        static void Mouse(int x, int y, string click)
        {
            try
            {
                uint mouse_x = (uint)0x8092E8;
                uint mouse_y = (uint)0x8092Ec;
                uint mouse_click = (uint)0x798614;
                WriteInt(mouse_x, x);
                WriteInt(mouse_y, y);
                if (click == "left")
                {
                    WriteInt(mouse_click, 1);
                }
                else if (click == "right")
                {
                    WriteInt(mouse_click, 1677612);
                }
                else if (click == "hold")
                {
                    WriteInt(mouse_click, 16777217);
                }
            }
            catch
            {
                Console.WriteLine("ERROR: Mouse Try/Catch return");
                WriteLog("[ERROR] Mouse Try/Catch return (" + DateTime.Now + ")");
                return;
            }
        }

        static void SayMessage(string message)
        {
                        uint openMessage = (uint)0x79856C;
                        //uint writeMessage = (uint)mem.ReadPointer(0x00809478) + 0x175FC; //re-write this later.
                        WriteInt(openMessage, 65792);
                        //WriteString(writeMessage, message); //re-write this later.
        }

        static string LastCommand = "";

        static private void ParseReader(string line)
        {
            try
            {
                OpenProcess(eqgameID);
                if (line.Contains("teleport") == true && line.Contains('"') == false)
                {
                    string[] words = line.Split(' ');
                    Console.WriteLine("teleporting to " + "X:" + words[1] + " Y:" + words[2] + " Z:" + words[3] + " H:" + words[4]);
                    Teleport(float.Parse(words[1]), float.Parse(words[2]), float.Parse(words[3]), float.Parse(words[4]));
                }
                else if (line.Contains("target") == true && line.Contains('"') == false)
                {
                    string[] words = line.Split(' ');
                    Console.WriteLine("Targeting " + words[1]);
                    TargetPlayer(words[1]);
                }
                else if (line.Contains("pause") == true && line.Contains('"') == false)
                {
                    string[] words = line.Split(' ');
                    Console.WriteLine("pausing for " + words[1] + " milliseconds");
                    int timer = Convert.ToInt32(words[1]);
                    System.Threading.Thread.Sleep(timer);
                }
                else if (line.Contains("CheckCursor") == true && line.Contains('"') == false)
                {
                    CheckCursor(LastCommand);
                }
                else if (line.Contains("CheckTrade") == true && line.Contains('"') == false)
                {
                    CheckTrade(LastCommand);
                }
                else if (line.Contains("say") == true && line.Contains('"') == true)
                {
                    string[] words = line.Split('"');
                    SayMessage(words[1]);
                    Console.WriteLine("sending message");
                }
                else if (line.Contains("checkNPCdistance") == true && line.Contains('"') == false)
                {
                    string[] words = line.Split(' ');
                    Console.WriteLine("checking NPC distance for " + words[1]);
                    CheckDistance(words[1], float.Parse(words[2]), float.Parse(words[3]), float.Parse(words[4]), Convert.ToInt32(words[5]));
                }
                else if (line.Contains("mouse") == true && line.Contains('"') == false)
                {
                    string[] words = line.Split(' ');
                    Mouse(Convert.ToInt32(words[1]), Convert.ToInt32(words[2]), words[3]);
                    LastCommand = "mouse " + Convert.ToInt32(words[1]).ToString() + " " + Convert.ToInt32(words[2]).ToString() + " " + words[3];
                    Console.WriteLine("moving mouse " + "X:" + words[1] + " Y:" + words[2] + " click:" + words[3]);
                }
                else if (line.Contains("heal") == true && line.Contains('"') == false)
                {
                    string[] words = line.Split(' ');
                    Console.WriteLine("checking heal conditions for " + words[1]);
                    HealCheck(words[1], Convert.ToInt32(words[2]));
                }
                else
                {
                    Console.WriteLine("ERROR: Break in ParseReader. Game Crashed.");
                    WriteLog("[ERROR] Break in ParseReader. Game Crashed. (" + DateTime.Now + ")");
                    return;
                }
            }
            catch
            {
                Console.WriteLine("ERROR: ParseReader Try/Catch return. Game Crashed.");
                WriteLog("[ERROR] ParseReader Try/Catch return. Game Crashed. (" + DateTime.Now + ")");
                Environment.Exit(0);
            }
        }

        static void Main(string[] args)
        {
            bool runonce = true;
            if(args != null && args.Length > 0)
            {
                if (File.Exists(args[2]))
                {
                    using (StreamReader sr = new StreamReader(args[2]))
                    {
                        
                            eqgameID = args[0];
                            string[] lines = File.ReadAllLines(args[2]);
                            while (args[1] == "loop" || runonce == true)
                            {
                                try
                                {
                                    foreach (string line in lines)
                                    {
                                        ParseReader(line);
                                        System.Threading.Thread.Sleep(100); //it's too fast otherwise
                                        runonce = false;
                                    }
                                    if (runonce == false)
                                    {
                                        Console.WriteLine("- end of script -");
                                        System.Threading.Thread.Sleep(100);
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("ERROR: Main StreamReader loop stopped");
                                    WriteLog("[ERROR] Main StreamReader loop stopped (" + DateTime.Now + ")");
                                }
                            }
                        
                    }
                }
            }
        }
    }
}
