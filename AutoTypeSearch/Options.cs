using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using AutoTypeSearch.Properties;
using KeePass.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePassLib;
using KeePassLib.Native;

namespace AutoTypeSearch
{
	internal partial class Options : UserControl
	{
		private const string OptionsConfigRoot = "AutoTypeSearchExt.";

		private readonly HotKeyControlEx mShowHotKeyControl;
		private static int sRegisteredHotkeyId;

		// ReSharper disable once MemberCanBePrivate.Global - Public for forms designer
		public Options()
		{
			InitializeComponent();

			// Must mach order and values of Actions enum
			var actions = new object[] { Resources.PerformAutoType, Resources.EditEntry, Resources.ShowEntry, Resources.OpenEntryUrl };
			mDefaultAction.Items.AddRange(actions);
			mAlternativeAction.Items.AddRange(actions);

			mShowHotKeyControl = HotKeyControlEx.ReplaceTextBox(mShowSearchGroup, mShowHotKeyTextBox, false);

			// Read options
			mShowOnFailedSearch.Checked = Settings.Default.ShowOnFailedAutoType;
			
			if (NativeLib.IsUnix())
			{
				mShowOnHotKey.Enabled = false;
				mShowOnHotKey.Checked = false;

				mShowHotKeyControl.Clear();
				mShowHotKeyControl.RenderHotKey();
			}
			else
			{
				mShowOnHotKey.Checked = Settings.Default.ShowOnHotKey;
				ShowHotKey = Settings.Default.ShowHotKey;
			}
			mShowOnHotKey_CheckedChanged(null, EventArgs.Empty);

			mShowOnIPC.Checked = Settings.Default.ShowOnIPC;
			mSearchInTitle.Checked = Settings.Default.SearchTitle;
			mSearchInUserName.Checked = Settings.Default.SearchUserName;
			mSearchInUrl.Checked = Settings.Default.SearchUrl;
			mSearchInNotes.Checked = Settings.Default.SearchNotes;
			mSearchInTags.Checked = Settings.Default.SearchTags;
			mSearchInOtherFields.Checked = Settings.Default.SearchCustomFields;
			
			mCaseSensitive.Checked = Settings.Default.CaseSensitive;
			mExcludeExpired.Checked = Settings.Default.ExcludeExpired;
			mResolveReferences.Checked = Settings.Default.ResolveReferences;

			mDefaultAction.SelectedIndex = (int)Settings.Default.DefaultAction;
			mAlternativeAction.SelectedIndex = (int)Settings.Default.AlternativeAction;
		}

		private Keys ShowHotKey
		{
			get { return mShowHotKeyControl.HotKey | mShowHotKeyControl.HotKeyModifiers; }
			set
			{
				mShowHotKeyControl.HotKey = value & Keys.KeyCode;
				mShowHotKeyControl.HotKeyModifiers = value & Keys.Modifiers;

				mShowHotKeyControl.RenderHotKey();
			}
		}

		private void mShowOnHotKey_CheckedChanged(object sender, EventArgs e)
		{
			mShowHotKeyControl.Enabled = mShowOnHotKey.Checked;
		}

		protected override void OnValidating(CancelEventArgs e)
		{
			mShowHotKeyControl.ResetIfModifierOnly();
			base.OnValidating(e);
		}

		private void ApplySettings()
		{
			// Apply settings
			Settings.Default.ShowOnFailedAutoType = mShowOnFailedSearch.Checked;
			Settings.Default.ShowOnHotKey = mShowOnHotKey.Checked;
			Settings.Default.ShowOnIPC = mShowOnIPC.Checked;
			Settings.Default.SearchTitle = mSearchInTitle.Checked;
			Settings.Default.SearchUserName = mSearchInUserName.Checked;
			Settings.Default.SearchUrl = mSearchInUrl.Checked;
			Settings.Default.SearchNotes = mSearchInNotes.Checked;
			Settings.Default.SearchTags = mSearchInTags.Checked;
			Settings.Default.SearchCustomFields = mSearchInOtherFields.Checked;
			Settings.Default.CaseSensitive = mCaseSensitive.Checked;
			Settings.Default.ExcludeExpired = mExcludeExpired.Checked;
			Settings.Default.ResolveReferences = mResolveReferences.Checked;
			Settings.Default.DefaultAction = (Actions)mDefaultAction.SelectedIndex;
			Settings.Default.AlternativeAction = (Actions)mAlternativeAction.SelectedIndex;
			Settings.Default.ShowHotKey = ShowHotKey;

			ApplyHotKey();
		}

		#region Settings persistence
		public static void SaveSettings(IPluginHost host)
		{
			if (host != null)
			{
				foreach (SettingsPropertyValue property in Settings.Default.PropertyValues)
				{
					if (property.IsDirty)
					{
						var value = property.SerializedValue as String;
						if (value != null)
						{
							host.CustomConfig.SetString(OptionsConfigRoot + property.Name, value);
						}
						else
						{
							Debug.Fail("Non-string serialized settings property");
						}
					}
				}
			}
		}

		public static void LoadSettings(IPluginHost host)
		{
			if (host != null)
			{
				// ReSharper disable once UnusedVariable
				var ignored = Settings.Default.ShowOnFailedAutoType; //Access any property just to make it load settings.

				foreach (SettingsPropertyValue property in Settings.Default.PropertyValues)
				{
					var value = host.CustomConfig.GetString(OptionsConfigRoot + property.Name);
					if (value != null)
					{
						property.SerializedValue = value;
						property.Deserialized = false;
						property.IsDirty = false;
					}
				}

				ApplyHotKey();
			}
		}
		#endregion

		#region Hotkey
		private static void ApplyHotKey()
		{
			UnregisterHotKey();

			if (Settings.Default.ShowOnHotKey && Settings.Default.ShowHotKey != Keys.None)
			{
				sRegisteredHotkeyId = HotKeyManager.RegisterHotKey(Settings.Default.ShowHotKey);
			}
		}

		public static void UnregisterHotKey()
		{
			if (sRegisteredHotkeyId != 0)
			{
				var result = HotKeyManager.UnregisterHotKey(sRegisteredHotkeyId);
				Debug.Assert(result);
				sRegisteredHotkeyId = 0;
			}
		}
		#endregion

		public static void AddToWindow(OptionsForm optionsForm)
		{
			var tabControl = optionsForm.Controls.Find("m_tabMain", false).FirstOrDefault() as TabControl;
			var okButton = optionsForm.Controls.Find("m_btnOK", false).FirstOrDefault() as Button;

			if (tabControl == null || okButton == null)
			{
				Debug.Fail("Could not integrate with options form");
			}

			var tabPage = new TabPage(Resources.AutoTypeSearch)
			{
				UseVisualStyleBackColor = true,
				AutoScroll = true,
				ImageIndex = (int)PwIcon.EMailSearch
			};
			var options = new Options { Dock = DockStyle.Fill };
			tabPage.Controls.Add(options);

			tabControl.TabPages.Add(tabPage);

			okButton.Click += delegate
			{
				options.ApplySettings();
			};
		}
	}
}
