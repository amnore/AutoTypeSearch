using System;
using System.Linq;
using System.Windows.Forms;
using AutoTypeSearch.Properties;
using KeePass;
using KeePass.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePass.Util;
using KeePassLib;
using KeePassLib.Security;

namespace AutoTypeSearch
{
// ReSharper disable once ClassNeverInstantiated.Global - Plugin instantiated by KeePass
	public sealed class AutoTypeSearchExt : Plugin
    {
		private const string IpcEventName = "AutoTypeSearch";
		private const int UnixAutoTypeWaitTime = 500; // Milliseconds
		internal const string TagsVirtualFieldName = "***TAGS***";

		private IPluginHost mHost;
		private bool mAutoTypeSuccessful;
		private string mLastAutoTypeWindowTitle;

		public override string UpdateUrl
		{
			get { return "sourceforge-version://AutoTypeSearch/autotypesearch?-v(%5B%5Cd.%5D%2B)%5C.zip"; }
		}

		public override bool Initialize(IPluginHost host)
		{
			mHost = host;

			IpcUtilEx.IpcEvent += OnIpcEvent;
			GlobalWindowManager.WindowAdded += OnWindowAdded;
			if (!KeePassLib.Native.NativeLib.IsUnix())
			{
				HotKeyManager.HotKeyPressed += HotKeyManager_HotKeyPressed;
			}
			AutoType.SequenceQueriesEnd += OnAutoTypeSequenceQueriesEnd;

			Options.LoadSettings(host);

			return true;
		}

		#region Unsuccessful AutoType Detection
		private void OnAutoTypeSequenceQueriesEnd(object sender, SequenceQueriesEventArgs e)
		{
			// An auto-type has completed. Was it successful? Watch for an auto-type event, and for the UI thread unblocking. If the UI thread unblocks before the auto-type event, it wasn't successful.
			// (hacky, yes, but no other means possible to detect failed auto-types at the time of writing)

			if (Settings.Default.ShowOnFailedAutoType)
			{
				mAutoTypeSuccessful = false;
				mLastAutoTypeWindowTitle = e.TargetWindowTitle;
				AutoType.FilterCompilePre += OnAutoType;

				if (KeePassLib.Native.NativeLib.IsUnix())
				{
					// If Unix, can't rely on waiting for UI thread unblocking as the XDoTool mechanism calls DoEvents (in NativeMethods.TryXDoTool) before anything else.
					// Instead, just wait half a second and hope for the best.
					var timer = new Timer { Interval = UnixAutoTypeWaitTime };
					timer.Tick += delegate
					{
						timer.Stop();
						timer.Dispose();
						OnAutoTypeEnd();
					};
					timer.Start();
				}
				else
				{
					mHost.MainWindow.BeginInvoke((Action)OnAutoTypeEnd);
				}
			}
		}

		private void OnAutoType(object sender, AutoTypeEventArgs autoTypeEventArgs)
		{
			// Detach event, we are only interested in a single invocation.
			AutoType.FilterCompilePre -= OnAutoType;

			mAutoTypeSuccessful = true;
		}

		private void OnAutoTypeEnd()
		{
			// Detach event, the auto-type failed, it won't be received now.
			AutoType.FilterCompilePre -= OnAutoType;

			if (!mAutoTypeSuccessful)
			{
				ShowSearch(String.Format(Resources.AutoTypeFailedMessage, mLastAutoTypeWindowTitle));
			}
		}
		#endregion

		#region Options
		private void OnWindowAdded(object sender, GwmWindowEventArgs e)
		{
			var optionsForm = e.Form as OptionsForm;
			if (optionsForm != null)
			{
				Options.AddToWindow(optionsForm);
				return;
			}

			if (Settings.Default.ShowOnFailedAutoType)
			{
				var autoTypeCtxForm = e.Form as AutoTypeCtxForm;
				if (autoTypeCtxForm != null)
				{
					mAutoTypeSuccessful = true; // Don't show the search if the picker box is shown
					autoTypeCtxForm.Closed += OnAutoTypeCtxFormClosed;
				}
			}
		}

		private void OnAutoTypeCtxFormClosed(object sender, EventArgs e)
		{
			var autoTypeCtxForm = (AutoTypeCtxForm)sender;
			autoTypeCtxForm.Closed -= OnAutoTypeCtxFormClosed;

			if (autoTypeCtxForm.DialogResult == DialogResult.Cancel)
			{
				ShowSearch();
			}
		}
		#endregion

		public override void Terminate()
		{
			IpcUtilEx.IpcEvent -= OnIpcEvent;
			GlobalWindowManager.WindowAdded -= OnWindowAdded;

			if (!KeePassLib.Native.NativeLib.IsUnix())
			{
				HotKeyManager.HotKeyPressed -= HotKeyManager_HotKeyPressed;
				Options.UnregisterHotKey();
			}

			Options.SaveSettings(mHost);
			
			base.Terminate();
		}

		#region Search Initiation
		private void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e)
		{
			/*
			var testGroup = mHost.Database.RootGroup.FindCreateGroup("Test", true);
			for (int i = 0; i < 10000; i++)
			{
				var pwEntry = new PwEntry(true, true);
				pwEntry.Strings.Set(PwDefs.TitleField, new ProtectedString(false, "Title " + i));
				pwEntry.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, "User " + i));
				pwEntry.Strings.Set(PwDefs.UrlField, new ProtectedString(false, "http://website/" + i));
				pwEntry.Strings.Set(PwDefs.NotesField, new ProtectedString(false, "Notes " + i + "\nLine 2\n\nLine 3\nLine 4\nLine 5\n Line 6\n Line 7\nLine 8\nLine 9\nLine 10"));
				testGroup.AddEntry(pwEntry, true);
			}*/

			ShowSearch();
		}

		private void OnIpcEvent(object sender, IpcEventArgs ipcEventArgs)
		{
			if (Settings.Default.ShowOnIPC && ipcEventArgs.Name.Equals(IpcEventName, StringComparison.InvariantCultureIgnoreCase))
			{
				ShowSearch();
			}
		}

		private void ShowSearch(string infoText = null)
		{
			// Unlock, if required
			mHost.MainWindow.ProcessAppMessage((IntPtr)Program.AppMessage.Unlock, IntPtr.Zero);


			if (mHost.MainWindow.IsAtLeastOneFileOpen())
			{
				var searchWindow = new SearchWindow(mHost.MainWindow, infoText);
				searchWindow.Show();
				searchWindow.Activate();
			}
		}
		#endregion
	}
}
