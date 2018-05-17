using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using Winuser.h;
using System.Runtime.InteropServices;
//using Microsoft.FSharp.Linq.RuntimeHelpers;
using System.Diagnostics;
using System.IO;

namespace WinMgr
{
    using HWND = IntPtr;

    public partial class Form1 : Form
    {
        private delegate bool EnumWindowsProc(HWND hWnd, int lParam);

        [DllImport("USER32.DLL")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowTextLength(HWND hWnd);

        [DllImport("USER32.DLL")]
        private static extern bool IsWindowVisible(HWND hWnd);

        [DllImport("USER32.DLL")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("USER32.DLL")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, ref Rectangle rect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int W, int H, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        const UInt32 SW_HIDE = 0;
        const UInt32 SW_SHOWNORMAL = 1;
        const UInt32 SW_NORMAL = 1;
        const UInt32 SW_SHOWMINIMIZED = 2;
        const UInt32 SW_SHOWMAXIMIZED = 3;
        const UInt32 SW_MAXIMIZE = 3;
        const UInt32 SW_SHOWNOACTIVATE = 4;
        const UInt32 SW_SHOW = 5;
        const UInt32 SW_MINIMIZE = 6;
        const UInt32 SW_SHOWMINNOACTIVE = 7;
        const UInt32 SW_SHOWNA = 8;
        const UInt32 SW_RESTORE = 9;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

        IntPtr shellWindow = GetShellWindow();

        public class InfoWindow
        {
            public IntPtr Handle = IntPtr.Zero;
            public string Title;
            public Rectangle Rect;
        }

        Dictionary<IntPtr, InfoWindow> openWindows = new Dictionary<HWND, InfoWindow>();

        private int walk_count = 0;
        private bool walk_up = true;

        public Form1()
        {
            InitializeComponent();
            SaveWindowLayout();
        }

        // Save window layout
        private void button1_Click(object sender, EventArgs e)
        {
            SaveWindowLayout();
        }

        private void SaveWindowLayout()
        {
            openWindows.Clear();
            IntPtr found = IntPtr.Zero;
            HWND shellWindow = GetShellWindow();
                            
            EnumWindows(new EnumWindowsProc(delegate (IntPtr hWnd, int lParam)
            {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;
                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;
                StringBuilder builder = new StringBuilder(length);
                GetWindowText(hWnd, builder, length + 1);
                String title = builder.ToString();

                //if (!String.Equals(title, "testWindow - Notepad"))
                //    return true; // for debugging only

                var info = new InfoWindow();
                info.Handle = hWnd;
                info.Title = title;
                Rectangle rect = new Rectangle();

                //GetWindowRect(hWnd, ref r);

                WINDOWPLACEMENT wpl = new WINDOWPLACEMENT();
                if (!GetWindowPlacement(hWnd, ref wpl)) return true;
                rect = wpl.rcNormalPosition;

                info.Rect = rect;
                openWindows[hWnd] = info;
                return true;
            }), 0);
            walk_up = true;
            walk_count = 0;
            ShowWalkCount();
        }

        private void WalkWindowLayout()
        {
            //openWindows.Clear();
            //IntPtr found = IntPtr.Zero;
            HWND shellWindow = GetShellWindow();

            EnumWindows(new EnumWindowsProc(delegate (IntPtr hWnd, int lParam)
            {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;
                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;
                StringBuilder builder = new StringBuilder(length);
                GetWindowText(hWnd, builder, length + 1);
                //String title = builder.ToString();

                //if (!String.Equals(title, "testWindow - Notepad"))
                //    return true; // for debugging only

                //var info = new InfoWindow();
                //info.Handle = hWnd;
                //info.Title = title;
                Rectangle rect = new Rectangle();

                //GetWindowRect(hWnd, ref r);

                WINDOWPLACEMENT wpl = new WINDOWPLACEMENT();
                //wpl.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                if (!GetWindowPlacement(hWnd, ref wpl)) return true;

                rect = wpl.rcNormalPosition;
                int i = walk_up ? 1 : -1;
                Point delta = new Point(i, i);
                rect.Offset(delta);
                rect.Height += i;
                rect.Width += i;
                wpl.rcNormalPosition = rect;
                SetWindowPlacement(hWnd, ref wpl);
                return true;
            }), 0);
        }

        // Restore windows to saved layout
        private void button2_Click(object sender, EventArgs e)
        {
            RestoreWindowLayout();
        }

        private void RestoreWindowLayout()
        {
            foreach (KeyValuePair<HWND, InfoWindow> entry in openWindows)
            {
                IntPtr hWnd = entry.Key;
                Rectangle rect = entry.Value.Rect;

                WINDOWPLACEMENT wpl = new WINDOWPLACEMENT();
                if (!GetWindowPlacement(hWnd, ref wpl)) continue;

                wpl.rcNormalPosition = rect;
                SetWindowPlacement(hWnd, ref wpl);
            }
            walk_up = true;
            walk_count = 0;
            ShowWalkCount();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            RestoreWindowLayout();

            if (this.WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.MyLoc = this.Location;
            }
            else
            {
                Properties.Settings.Default.MyLoc = this.RestoreBounds.Location;
            }
            Properties.Settings.Default.Save();
        }

        private void ShowWalkCount()
        {
            String increments = String.Format("{0}", walk_count);
            textBox1.Text = increments;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ShowWalkCount();
            WalkWindowLayout();

            if (walk_up)
            {
                if (++walk_count > 5)
                {
                    walk_up = false;
                    walk_count = 4;
                }
            }
            else
            {
                if (--walk_count < 0)
                {
                    walk_up = true;
                    walk_count = 1;
                }
            }
            /*
            if (x_walk++ == 5)
            {
                x_walk = 0;
                if (y_walk++ == 5)
                {
                    y_walk = 0;
                }
            }
            */
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Location = Properties.Settings.Default.MyLoc;
        }
    }
}
