using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoMineSweeper
{
    class MemoryUtils
    {
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, UInt32 dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UInt32 nSize, out UInt32 lpNumberOfBytesWritten);

        [DllImport("Kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UInt32 nSize, out UInt32 lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern Int32 CloseHandle(IntPtr hProcess);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out UInt32 dwProcessId);

        public static byte[] ReadProcessMemory(IntPtr hWnd, IntPtr lpBaseAddress, UInt32 size)
        {
            GetWindowThreadProcessId(hWnd, out var dwProcessId);

            byte[] buffer = new byte[size];

            var hProcess = OpenProcess(ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VMOperation | ProcessAccessFlags.VMRead, false, dwProcessId);

            ReadProcessMemory(hProcess, lpBaseAddress, buffer, size, out var numberOfBytesRead);

            CloseHandle(hProcess);

            return buffer;
        }
    }
}
