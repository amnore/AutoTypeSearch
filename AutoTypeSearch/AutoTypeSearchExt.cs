﻿using System;
using System.Linq;
using System.Windows.Forms;
using AutoTypeSearch.Properties;
using KeePass;
using KeePass.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePass.Util;

namespace AutoTypeSearch
{
	// ReSharper disable once UnusedMember.Global : Plugin class instantiated by KeePass
	public sealed class AutoTypeSearchExt : Plugin
    {
		private const string IpcEventName = "AutoTypeSearch";
		internal const string TagsVirtualFieldName = "***TAGS***";

		private IPluginHost mHost;
		private bool mAutoTypeSuccessful;

		public override string UpdateUrl
		{
			get { return "sourceforge-version://AutoTypeSearch/autotypesearch?-v(%5B%5Cd.%5D%2B)%5C.zip"; }
		}

		public override bool Initialize(IPluginHost host)
		{
			mHost = host;

			IpcUtilEx.IpcEvent += OnIpcEvent;
			GlobalWindowManager.WindowAdded += OnWindowAdded;
			HotKeyManager.HotKeyPressed += HotKeyManager_HotKeyPressed;
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
				AutoType.FilterCompilePre += OnAutoType;

				mHost.MainWindow.BeginInvoke((Action)OnAutoTypeEnd);
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
				ShowSearch();
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
			HotKeyManager.HotKeyPressed -= HotKeyManager_HotKeyPressed;

			Options.UnregisterHotKey();

			Options.SaveSettings(mHost);
			
			base.Terminate();
		}

		#region Search Initiation
		private void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e)
		{
			ShowSearch();
		}

		private void OnIpcEvent(object sender, IpcEventArgs ipcEventArgs)
		{
			if (Settings.Default.ShowOnIPC && ipcEventArgs.Name.Equals(IpcEventName, StringComparison.InvariantCultureIgnoreCase))
			{
				ShowSearch();
			}
		}

		private void ShowSearch()
		{
			// Unlock, if required
			mHost.MainWindow.ProcessAppMessage((IntPtr)Program.AppMessage.Unlock, IntPtr.Zero);

			if (mHost.MainWindow.IsAtLeastOneFileOpen())
			{
				var searchWindow = new SearchWindow(mHost.Database, mHost.MainWindow);
				searchWindow.Show();
				searchWindow.Activate();
			}
		}
		#endregion
	}
}