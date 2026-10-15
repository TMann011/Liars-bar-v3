using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MonoInjector
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new MainForm());
        }
    }

    class MainForm : Form
    {
        TextBox processBox, dllBox;
        ComboBox methodBox;
        Button browseBtn, injectBtn;

        public MainForm()
        {
            Text = "Mono Injector"; Size = new System.Drawing.Size(500, 200); StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;

            var pl = new Label { Text = "Process (name or PID):", Location = new Point(15, 20), AutoSize = true };
            Controls.Add(pl);
            processBox = new TextBox { Location = new Point(160, 17), Size = new System.Drawing.Size(310, 23) };
            Controls.Add(processBox);

            var dl = new Label { Text = "DLL Path:", Location = new Point(15, 55), AutoSize = true };
            Controls.Add(dl);
            dllBox = new TextBox { Location = new Point(160, 52), Size = new System.Drawing.Size(200, 23) };
            Controls.Add(dllBox);

            browseBtn = new Button { Text = "Browse", Location = new Point(370, 50), Size = new System.Drawing.Size(100, 25) };
            browseBtn.Click += (s, e) => { var d = new OpenFileDialog { Filter = "DLL|*.dll" }; if (d.ShowDialog() == DialogResult.OK) dllBox.Text = d.FileName; };
            Controls.Add(browseBtn);

            var ml = new Label { Text = "Method:", Location = new Point(15, 90), AutoSize = true };
            Controls.Add(ml);
            methodBox = new ComboBox { Location = new Point(160, 87), Size = new System.Drawing.Size(200, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            methodBox.Items.AddRange(new object[] { "MelonLoader (copy to Mods)", "mono_assembly_load" });
            methodBox.SelectedIndex = 0;
            Controls.Add(methodBox);

            injectBtn = new Button { Text = "Inject", Location = new Point(370, 85), Size = new System.Drawing.Size(100, 30) };
            injectBtn.Click += InjectClick;
            Controls.Add(injectBtn);
        }

        void InjectClick(object s, EventArgs e)
        {
            string procName = processBox.Text.Trim();
            string dllPath = dllBox.Text.Trim();
            if (string.IsNullOrEmpty(procName) || string.IsNullOrEmpty(dllPath))
            {
                MessageBox.Show("Fill in process name and DLL path.");
                return;
            }

            if (!File.Exists(dllPath))
            {
                MessageBox.Show("DLL not found: " + dllPath);
                return;
            }

            Process proc = null;
            int pid = 0;
            if (int.TryParse(procName, out pid) && pid > 0)
                proc = Process.GetProcessById(pid);
            else
            {
                var procs = Process.GetProcessesByName(procName);
                if (procs.Length > 0) proc = procs[0];
            }

            if (proc == null)
            {
                MessageBox.Show("Process not found: " + procName);
                return;
            }

            if (methodBox.SelectedIndex == 0)
            {
                // MelonLoader mode - copy to Mods folder
                string modsDir = Path.Combine(Path.GetDirectoryName(proc.MainModule.FileName), "Mods");
                string dest = Path.Combine(modsDir, Path.GetFileName(dllPath));
                try
                {
                    File.Copy(dllPath, dest, true);
                    MessageBox.Show("Copied to Mods folder!\n" + dest + "\n\nRestart the game to load.");
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
            else
            {
                // Direct injection mode
                IntPtr hProc = OpenProcess(0x1FFFFF, false, proc.Id);
                if (hProc == IntPtr.Zero) { MessageBox.Show("OpenProcess failed. Run as admin?"); return; }

                // Find mono.dll in target
                IntPtr monoBase = IntPtr.Zero;
                foreach (ProcessModule m in proc.Modules)
                {
                    if (m.ModuleName.StartsWith("mono", StringComparison.OrdinalIgnoreCase))
                    { monoBase = m.BaseAddress; break; }
                }

                if (monoBase == IntPtr.Zero)
                { CloseHandle(hProc); MessageBox.Show("No mono.dll found - not a Unity Mono game."); return; }

                IntPtr localMono = LoadLibrary("mono.dll");
                if (localMono == IntPtr.Zero) localMono = LoadLibrary("mono-2.0-bdwgc.dll");
                if (localMono == IntPtr.Zero) { CloseHandle(hProc); MessageBox.Show("mono.dll not found locally."); return; }

                IntPtr fnLoad = GetProcAddress(localMono, "mono_assembly_load_from_full");
                if (fnLoad == IntPtr.Zero) { FreeLibrary(localMono); CloseHandle(hProc); MessageBox.Show("mono_assembly_load_from_full not found."); return; }

                long rva = fnLoad.ToInt64() - localMono.ToInt64();
                IntPtr remoteFn = (IntPtr)(monoBase.ToInt64() + rva);

                byte[] pathBytes = System.Text.Encoding.UTF8.GetBytes(dllPath);
                uint sz = (uint)(pathBytes.Length + 1);
                IntPtr remotePath = VirtualAllocEx(hProc, IntPtr.Zero, sz, 0x3000, 0x04);
                byte[] fullPath = new byte[sz]; Array.Copy(pathBytes, fullPath, pathBytes.Length);
                int written = 0;
                WriteProcessMemory(hProc, remotePath, fullPath, sz, out written);

                uint pa = (uint)(remotePath.ToInt64() & 0xFFFFFFFF);
                uint la = (uint)(remoteFn.ToInt64() & 0xFFFFFFFF);
                byte[] sc = { 0x68, (byte)(pa&0xFF),(byte)((pa>>8)&0xFF),(byte)((pa>>16)&0xFF),(byte)((pa>>24)&0xFF),
                              0x6A,0, 0x6A,0, 0x6A,0,
                              0xB8, (byte)(la&0xFF),(byte)((la>>8)&0xFF),(byte)((la>>16)&0xFF),(byte)((la>>24)&0xFF),
                              0xFF,0xD0, 0xC3 };
                IntPtr sm = VirtualAllocEx(hProc, IntPtr.Zero, (uint)sc.Length, 0x3000, 0x40);
                WriteProcessMemory(hProc, sm, sc, (uint)sc.Length, out written);
                IntPtr th = CreateRemoteThread(hProc, IntPtr.Zero, 0, sm, IntPtr.Zero, 0, IntPtr.Zero);
                if (th == IntPtr.Zero) MessageBox.Show("CreateRemoteThread failed.");
                else { WaitForSingleObject(th, 5000); CloseHandle(th); MessageBox.Show("Injected!"); }
                VirtualFreeEx(hProc, sm, 0, 0x8000);
                VirtualFreeEx(hProc, remotePath, 0, 0x8000);
                FreeLibrary(localMono);
                CloseHandle(hProc);
            }
        }

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

