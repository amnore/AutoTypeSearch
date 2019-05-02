using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using KeePassLib.Native;
using Microsoft.Win32;

namespace AutoTypeSearch
{
	internal static class NativeMethods
	{
		private const int EM_SETMARGINS = 0x00D3;
		private const int EC_RIGHTMARGIN = 0x2;

		private const int WM_NCLBUTTONDOWN = 0xA1;
		private const int HTCAPTION = 0x2;
		[DllImport("User32.dll")]
		private static extern bool ReleaseCapture();
		[DllImport("User32.dll")]
		private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

		private const int SWP_NOSIZE = 0x0001;
		private const int SWP_NOMOVE = 0x0002;
		private const int SWP_NOZORDER = 0x0004;
		private const int SWP_FRAMECHANGED = 0x0020;
		[DllImport("user32.dll", SetLastError=true)]
		private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);

		private const int WM_NCCALCSIZE = 0x83;

		private struct RECT
		{
			public int Left, Top, Right, Bottom;
		}
		private struct WINDOWPOS
		{
			public IntPtr hwnd;
			public IntPtr hwndinsertafter;
			public int x, y, cx, cy;
			public int flags;
		}

		struct NCCALCSIZE_PARAMS
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public RECT[] rgrc;
			public WINDOWPOS lppos;
		}

		public static void SetTextBoxRightMargin(TextBox control, int rightMargin)
		{
			SendMessage(control.Handle, EM_SETMARGINS, EC_RIGHTMARGIN, rightMargin << 16);
		}

		public static void StartFormDrag(Form form)
		{
			Debug.Assert(Control.MouseButtons == MouseButtons.Left);
			ReleaseCapture();
			SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
		}

		public static void RefreshWindowFrame(IntPtr hWnd)
		{
			NativeMethods.SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
		}

		public static void RemoveWindowFrameTopBorder(ref Message m, int borderHeight)
		{
			if (m.Msg == WM_NCCALCSIZE)
			{
				var csp = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(NCCALCSIZE_PARAMS));
				csp.rgrc[0].Top -= borderHeight;
				Marshal.StructureToPtr(csp, m.LParam, false);
			}
		}

		public static bool IsWindows10()
		{
			return NativeLib.GetPlatformID() == PlatformID.Win32NT &&
			    // Can't just use OS Version because Windows 10 lies if you don't have specific support declared in the manifest.
				(int)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentMajorVersionNumber", -1) == 10;
		}
	}
}
