using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MonoInjector
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: MonoInjector.exe <process> <assembly.dll> [namespace.class] [method]");
                Console.WriteLine("  process  - process name or PID");
                Console.WriteLine("  assembly - path to .NET DLL to inject");
                Console.WriteLine("  class    - fully qualified class name (default: auto-detect)");
                Console.WriteLine("  method   - method to invoke (default: Load)");
                Console.WriteLine("");
                Console.WriteLine("Examples:");
                Console.WriteLine("  MonoInjector.exe \"Liar's Bar\" HyperBar.dll");
                Console.WriteLine("  MonoInjector.exe t6mp doomsday.dll");
                return;
            }

            string target = args[0];
            string dllPath = Path.GetFullPath(args[1]);
            string className = args.Length > 2 ? args[2] : null;
            string methodName = args.Length > 3 ? args[3] : "Load";

            if (!File.Exists(dllPath))
            {
                Console.WriteLine("ERROR: DLL not found: {dllPath}");
                return;
            }

            Process proc = FindProcess(target);
            if (proc == null)
            {
                Console.WriteLine("ERROR: Process not found: {target}");
                return;
            }

            Console.WriteLine("Target: " + proc.ProcessName + " (PID " + proc.Id + ")");
            Console.WriteLine("DLL: " + dllPath);

            Inject(proc, dllPath);
        }

        static Process FindProcess(string target)
        {
            int pid = 0;
            if (int.TryParse(target, out pid) && pid > 0)
                return Process.GetProcessById(pid);
            var procs = Process.GetProcessesByName(target);
            return procs.Length > 0 ? procs[0] : null;
        }

        static void Inject(Process proc, string dllPath)
        {
            IntPtr hProc = OpenProcess(0x1FFFFF, false, proc.Id);
            if (hProc == IntPtr.Zero)
            {
                Console.WriteLine("ERROR: OpenProcess failed. Run as admin?");
                return;
            }

            // Find mono.dll in the target process
            IntPtr monoModule = IntPtr.Zero;
            foreach (ProcessModule m in proc.Modules)
            {
                if (m.ModuleName.Equals("mono.dll", StringComparison.OrdinalIgnoreCase) ||
                    m.ModuleName.Equals("mono-2.0-bdwgc.dll", StringComparison.OrdinalIgnoreCase) ||
                    m.ModuleName.Equals("mono-2.0-sgen.dll", StringComparison.OrdinalIgnoreCase))
                {
                    monoModule = m.BaseAddress;
                    break;
                }
            }

            if (monoModule == IntPtr.Zero)
            {
                Console.WriteLine("WARNING: No mono.dll found in target. Trying MelonLoader injection...");
                InjectMelonLoader(hProc, proc, dllPath);
                CloseHandle(hProc);
                return;
            }

            // Get mono functions
            IntPtr localMono = LoadLibrary("mono.dll");
            if (localMono == IntPtr.Zero) localMono = LoadLibrary("mono-2.0-bdwgc.dll");
            if (localMono == IntPtr.Zero) localMono = LoadLibrary("mono-2.0-sgen.dll");
            
            if (localMono == IntPtr.Zero)
            {
                Console.WriteLine("ERROR: Cannot load mono.dll locally. Install Unity/Mono runtime.");
                CloseHandle(hProc);
                return;
            }

            // Calculate RVAs
            IntPtr fnMonoAssemblyLoadFromFull = GetProcAddress(localMono, "mono_assembly_load_from_full");
            IntPtr fnMonoImageOpenFromDataWithName = GetProcAddress(localMono, "mono_image_open_from_data_with_name");
            IntPtr fnMonoAssemblyClose = GetProcAddress(localMono, "mono_assembly_close");
            IntPtr fnMonoImageClose = GetProcAddress(localMono, "mono_image_close");

            if (fnMonoAssemblyLoadFromFull == IntPtr.Zero)
            {
                Console.WriteLine("ERROR: mono_assembly_load_from_full not found");
                FreeLibrary(localMono);
                CloseHandle(hProc);
                return;
            }

            long rvaAssemblyLoad = fnMonoAssemblyLoadFromFull.ToInt64() - localMono.ToInt64();
            IntPtr remoteMonoAssemblyLoad = (IntPtr)(monoModule.ToInt64() + rvaAssemblyLoad);

            // Write DLL path to target
            byte[] pathBytes = Encoding.UTF8.GetBytes(dllPath);
            uint pathSize = (uint)(pathBytes.Length + 1);
            IntPtr remotePath = VirtualAllocEx(hProc, IntPtr.Zero, pathSize, 0x3000, 0x04);
            if (remotePath == IntPtr.Zero)
            {
                Console.WriteLine("ERROR: VirtualAllocEx failed");
                FreeLibrary(localMono);
                CloseHandle(hProc);
                return;
            }

            byte[] fullPath = new byte[pathSize];
            Array.Copy(pathBytes, fullPath, pathBytes.Length);
            int written;
            WriteProcessMemory(hProc, remotePath, fullPath, pathSize, out written);

            // Create remote thread to call mono_assembly_load_from_full
            // mono_assembly_load_from_full(image, name, ref status, ref refonly)
            // We need shellcode that pushes the path pointer and calls the function
            uint pathAddr = (uint)(remotePath.ToInt64() & 0xFFFFFFFF);
            uint loadAddr = (uint)(remoteMonoAssemblyLoad.ToInt64() & 0xFFFFFFFF);

            // Shellcode: push path; push 0; push 0; push path; call mono_load; ret
            byte[] shell = new byte[] {
                0x68, (byte)(pathAddr & 0xFF), (byte)((pathAddr >> 8) & 0xFF), (byte)((pathAddr >> 16) & 0xFF), (byte)((pathAddr >> 24) & 0xFF),
                0x6A, 0x00,
                0x6A, 0x00,
                0x68, (byte)(pathAddr & 0xFF), (byte)((pathAddr >> 8) & 0xFF), (byte)((pathAddr >> 16) & 0xFF), (byte)((pathAddr >> 24) & 0xFF),
                0xB8, (byte)(loadAddr & 0xFF), (byte)((loadAddr >> 8) & 0xFF), (byte)((loadAddr >> 16) & 0xFF), (byte)((loadAddr >> 24) & 0xFF),
                0xFF, 0xD0,
                0xC3
            };

            IntPtr shellMem = VirtualAllocEx(hProc, IntPtr.Zero, (uint)shell.Length, 0x3000, 0x40);
            WriteProcessMemory(hProc, shellMem, shell, (uint)shell.Length, out written);

            IntPtr hThread = CreateRemoteThread(hProc, IntPtr.Zero, 0, shellMem, IntPtr.Zero, 0, IntPtr.Zero);
            if (hThread == IntPtr.Zero)
            {
                Console.WriteLine("ERROR: CreateRemoteThread failed");
            }
            else
            {
                WaitForSingleObject(hThread, 10000);
                Console.WriteLine("Mono assembly injected successfully!");
                CloseHandle(hThread);
            }

            VirtualFreeEx(hProc, shellMem, 0, 0x8000);
            VirtualFreeEx(hProc, remotePath, 0, 0x8000);
            FreeLibrary(localMono);
            CloseHandle(hProc);
        }

        static void InjectMelonLoader(IntPtr hProc, Process proc, string dllPath)
        {
            // Just copy the DLL to the Mods folder and let MelonLoader pick it up
            string modsFolder = Path.Combine(Path.GetDirectoryName(proc.MainModule.FileName), "Mods");
            if (Directory.Exists(modsFolder))
            {
                string dest = Path.Combine(modsFolder, Path.GetFileName(dllPath));
                File.Copy(dllPath, dest, true);
                Console.WriteLine("Copied to Mods folder: " + dest);
                Console.WriteLine("Restart the game to load the mod.");
            }
            else
            {
                Console.WriteLine("No Mods folder found - not a MelonLoader game.");
            }
        }

        // P/Invoke
        [DllImport("kernel32")] static extern IntPtr OpenProcess(uint a, bool b, int c);
        [DllImport("kernel32")] static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32")] static extern IntPtr VirtualAllocEx(IntPtr p, IntPtr a, uint s, uint t, uint f);
        [DllImport("kernel32")] static extern bool VirtualFreeEx(IntPtr p, IntPtr a, uint s, uint t);
        [DllImport("kernel32")] static extern bool WriteProcessMemory(IntPtr p, IntPtr a, byte[] b, uint s, out int w);
        [DllImport("kernel32")] static extern IntPtr CreateRemoteThread(IntPtr p, IntPtr t, uint s, IntPtr a, IntPtr x, uint f, IntPtr i);
        [DllImport("kernel32")] static extern uint WaitForSingleObject(IntPtr h, uint m);
        [DllImport("kernel32")] static extern IntPtr GetProcAddress(IntPtr m, string n);
        [DllImport("kernel32")] static extern IntPtr LoadLibrary(string n);
        [DllImport("kernel32")] static extern bool FreeLibrary(IntPtr m);
    }
}


