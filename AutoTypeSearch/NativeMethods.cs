using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
	}
}
