using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Windows.Forms;
using AutoTypeSearch.Properties;
using KeePass.Forms;
using KeePass.Resources;
using KeePass.UI;
using KeePass.Util;
using KeePassLib;
using KeePassLib.Collections;

namespace AutoTypeSearch
{
	public partial class SearchWindow : Form
	{
		private const int SecondLineInset = 10;

		private readonly PwDatabase mDatabase;
		private readonly MainForm mMainForm;
		private readonly MethodInfo mSelectEntriesMethod;
		private readonly Bitmap mBannerImage;
		private readonly Searcher mSearcher;

		private int mBannerWidth = -1;
		private int mMaximumExpandHeight;
		private bool mManualSizeApplied;
		private SearchResults mCurrentSearch;
		private SearchResults mLastResultsUpdated;
		private int mLastResultsUpdatedNextAvailableIndex;

		#region Opening
		public SearchWindow()
		{
			InitializeComponent();

			GlobalWindowManager.CustomizeControl(this);
			UIUtil.SetExplorerTheme(mResults, true);
			
			SetItemHeight();

			mThrobber.Top = mSearch.Top + (mSearch.Height - mThrobber.Height) / 2;
		}

		public SearchWindow(PwDatabase database, MainForm mainForm) : this()
		{
			mDatabase = database;
			mMainForm = mainForm;

			var mainWindowType = mMainForm.GetType();
			mSelectEntriesMethod = mainWindowType.GetMethod("SelectEntries", BindingFlags.Instance | BindingFlags.NonPublic);

			mSearcher = new Searcher(mDatabase);

			Icon = mMainForm.Icon;
			using (var bannerIcon = new Icon(Icon, 48, 48))
			{
				mBannerImage = bannerIcon.ToBitmap();
			}
			UpdateBanner();

			var windowRect = Settings.Default.WindowPosition;
			var collapsedWindowRect = windowRect;
			collapsedWindowRect.Height = mSearch.Bottom + (Height - ClientSize.Height);
			if (windowRect.IsEmpty || !IsOnScreen(collapsedWindowRect))
			{
				windowRect = new Rectangle(0, 0, Width, Height);
				Height = collapsedWindowRect.Height;
				
				StartPosition = FormStartPosition.CenterScreen;
			}
			else
			{
				Location = windowRect.Location;
				Size = collapsedWindowRect.Size;
			}

			MinimumSize = new Size(MinimumSize.Width, collapsedWindowRect.Height);
			mMaximumExpandHeight = Math.Max(windowRect.Height, MinimumSize.Height + mResults.ItemHeight);

			ShowThrobber = false;

			FontUtil.AssignDefaultItalic(mNoResultsLabel);
		}

		private static bool IsOnScreen(Rectangle rectangle)
		{
			return Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(rectangle));
		}

		private void SetItemHeight()
		{
			mResults.ItemHeight = mResults.Font.Height * 2 + 2;
		}
		#endregion

		#region Closing
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			Deactivate += OnDeactivate;
		}

		private void OnDeactivate(object sender, EventArgs eventArgs)
		{
			Close();
		}

		protected override void OnClosed(EventArgs e)
		{
			Deactivate -= OnDeactivate;
			base.OnClosed(e);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				if (mBannerImage != null)
				{
					mBannerImage.Dispose();
				}
				components.Dispose();
			}
			base.Dispose(disposing);
		}
		#endregion

		#region Item Drawing
		private void mResults_DrawItem(object sender, DrawItemEventArgs e)
		{
			var searchResult = mResults.Items[e.Index] as SearchResult;
			if (searchResult == null)
			{
				Debug.Fail("Unexpected item in mResults");
				return;
			}
			var drawingArea = e.Bounds;
			drawingArea.Height--; // Leave room for a dividing line at the bottom
			
			if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
			{
				DrawBorderedRectangle(e.Graphics, drawingArea, SystemColors.Highlight);
			}
			else
			{
				e.Graphics.FillRectangle(SystemBrushes.Window, drawingArea);
			}

			var image = GetImage(searchResult.Entry.CustomIconUuid, searchResult.Entry.IconId);
			var imageMargin = (drawingArea.Height - image.Height) / 2;
			e.Graphics.DrawImageUnscaled(image, drawingArea.Left + imageMargin, drawingArea.Top + imageMargin);

			var textLeftMargin = drawingArea.Left + imageMargin * 2 + image.Width;
			var textBounds = new Rectangle(textLeftMargin, drawingArea.Top + 1, drawingArea.Width - textLeftMargin - 1, drawingArea.Height - 2);

			var line1Bounds = textBounds;
			line1Bounds.Height = e.Font.Height;
			var line2Bounds = line1Bounds;
			line2Bounds.Y += line2Bounds.Height - 1;
			line2Bounds.X += SecondLineInset;
			line2Bounds.Width -= SecondLineInset;

			
			if (searchResult.FieldName == PwDefs.TitleField)
			{
				var title = searchResult.FieldValue;

				// Found the result in the title field. Highlight title in first line, use Username for second line.
				DrawHighlight(e, line1Bounds, title, searchResult.Start, searchResult.Length);
				TextRenderer.DrawText(e.Graphics, title, e.Font, line1Bounds.Location, SystemColors.WindowText, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
				TextRenderer.DrawText(e.Graphics, KPRes.UserName + ": " + searchResult.Entry.Strings.ReadSafeEx(PwDefs.UserNameField), e.Font, line2Bounds, SystemColors.GrayText, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
			}
			else
			{
				var title = searchResult.Entry.Strings.ReadSafe(PwDefs.TitleField);

				// Found the result in not title field. Use title on first line, and show the matching result on second line
				TextRenderer.DrawText(e.Graphics, title, e.Font, line1Bounds.Location, SystemColors.WindowText, TextFormatFlags.NoPadding);

				var fieldValue = searchResult.FieldValue.Replace('\n',' ');
				var fieldNamePrefix = GetDisplayFieldName(searchResult.FieldName) + ": ";

				var remainingSpace = line2Bounds.Width;
				var fieldNamePrefixWidth = TextRenderer.MeasureText(e.Graphics, fieldNamePrefix, e.Font, new Size(remainingSpace, line2Bounds.Height), TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis).Width;
				remainingSpace -= fieldNamePrefixWidth;

				int fieldValueHighlightWidth = 0, fieldValueLeftContextWidth = 0, fieldValueRightContextWidth = 0;

				var leftContext = fieldValue.Substring(0, searchResult.Start);
				var highlight = fieldValue.Substring(searchResult.Start, searchResult.Length);
				var rightContext = fieldValue.Substring(searchResult.Start + searchResult.Length);

				if (searchResult.Length == 0)
				{
					fieldValueHighlightWidth = remainingSpace;
				}
				else
				{
					if (remainingSpace > 0)
					{
						var availableSpace = remainingSpace;
						fieldValueHighlightWidth = TextRenderer.MeasureText(e.Graphics, highlight, e.Font, new Size(availableSpace, line2Bounds.Height), TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis).Width;
						remainingSpace -= fieldValueHighlightWidth;
					}

					// Of the space remaining, divide it equally between that which comes before, and that which comes after
					if (!String.IsNullOrEmpty(leftContext))
					{
						var leftContextReversed = new String(leftContext.ToCharArray().Reverse().ToArray());
						fieldValueLeftContextWidth = TextRenderer.MeasureText(e.Graphics, leftContextReversed, e.Font, new Size(remainingSpace / 2, line2Bounds.Height), TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.ModifyString).Width;

						if (fieldValueLeftContextWidth > remainingSpace)
						{
							// Always allow space for the minimal left context
							fieldValueHighlightWidth -= (fieldValueLeftContextWidth - remainingSpace);
							remainingSpace = 0;
						}
						else
						{
							remainingSpace -= fieldValueLeftContextWidth;							
						}
						
						// Replace left context with the truncated reversed left context.
						leftContext = new String(leftContextReversed.ToCharArray().TakeWhile(c => c != '\0').Reverse().ToArray());
					}

					if (remainingSpace > 0 && !String.IsNullOrEmpty(rightContext))
					{
						fieldValueRightContextWidth = TextRenderer.MeasureText(e.Graphics, rightContext, e.Font, new Size(remainingSpace, line2Bounds.Height), TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis).Width;
						if (fieldValueRightContextWidth > remainingSpace)
						{
							fieldValueRightContextWidth = 0;
						}
					}
				}

				// Now draw it all
				var bounds = line2Bounds;
				bounds.Width = fieldNamePrefixWidth;
				TextRenderer.DrawText(e.Graphics, fieldNamePrefix, e.Font, bounds, SystemColors.GrayText, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
				if (fieldValueLeftContextWidth > 0)
				{
					bounds.X += bounds.Width;
					bounds.Width = fieldValueLeftContextWidth;
					TextRenderer.DrawText(e.Graphics, leftContext, e.Font, bounds, SystemColors.GrayText, TextFormatFlags.NoPadding); // No ellipsis as the leftContext string has already been truncated appropriately
				}
				if (fieldValueHighlightWidth > 0)
				{
					bounds.X += bounds.Width;
					bounds.Width = fieldValueHighlightWidth;

					if (searchResult.Length > 0)
					{
						DrawHighlightRectangle(e, bounds);
					}
					TextRenderer.DrawText(e.Graphics, highlight, e.Font, bounds, SystemColors.GrayText, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
				}
				if (fieldValueRightContextWidth > 0)
				{
					bounds.X += bounds.Width;
					bounds.Width = fieldValueRightContextWidth;
					TextRenderer.DrawText(e.Graphics, rightContext, e.Font, bounds, SystemColors.GrayText, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
				}
			}

			e.Graphics.DrawLine(SystemPens.ButtonFace, drawingArea.Left, drawingArea.Bottom, drawingArea.Right, drawingArea.Bottom);
		}

		private static void DrawHighlight(DrawItemEventArgs e, Rectangle lineBounds, string text, int highlightFrom, int highlightLength)
		{
			var highlightX = TextRenderer.MeasureText(e.Graphics, text.Substring(0, highlightFrom), e.Font, Size.Empty, TextFormatFlags.NoPadding).Width;
			var highlightWidth = TextRenderer.MeasureText(e.Graphics, text.Substring(0, highlightFrom + highlightLength), e.Font, Size.Empty, TextFormatFlags.NoPadding).Width - highlightX;

			DrawHighlightRectangle(e, new Rectangle(lineBounds.Left + highlightX, lineBounds.Top, highlightWidth, lineBounds.Height));
		}

		private static void DrawHighlightRectangle(DrawItemEventArgs e, Rectangle rectangle)
		{
			DrawBorderedRectangle(e.Graphics, rectangle, Color.PaleTurquoise);
		}

		private static void DrawBorderedRectangle(Graphics graphics, Rectangle rectangle, Color colour)
		{
			var border = rectangle;
			border.Width--;
			border.Height--;

			using (var brush = new SolidBrush(MergeColors(colour, SystemColors.Window, 0.2)))
			{
				graphics.FillRectangle(brush, rectangle);
			}
			using (var pen = new Pen(colour, 1f))
			{
				graphics.DrawRectangle(pen, border);
			}
		}

		private Image GetImage(PwUuid customIconId, PwIcon iconId)
		{
			Image image = null;
			if (mDatabase != null)
			{
				if (!customIconId.Equals(PwUuid.Zero))
				{
					image = DpiUtil.ScaleImage(mDatabase.GetCustomIcon(customIconId), false);
				}
				if (image == null)
				{
					try { image = mMainForm.ClientIcons.Images[(int)iconId]; }
					catch (Exception) { Debug.Assert(false); }
				}
			}

			return image;
		}

		private static string GetDisplayFieldName(string fieldName)
		{
			switch (fieldName)
			{
				case PwDefs.TitleField:
					return KPRes.Title;
				case PwDefs.UserNameField:
					return KPRes.UserName;
				case PwDefs.PasswordField:
					return KPRes.Password;
				case PwDefs.UrlField:
					return KPRes.Url;
				case PwDefs.NotesField:
					return KPRes.Notes;
				case AutoTypeSearchExt.TagsVirtualFieldName:
					return KPRes.Tags;
				default:
					return fieldName;
			}
		}

		public static Color MergeColors(Color from, Color to, double amount)
		{
			var r = (byte)((from.R * amount) + to.R * (1 - amount));
			var g = (byte)((from.G * amount) + to.G * (1 - amount));
			var b = (byte)((from.B * amount) + to.B * (1 - amount));
			return Color.FromArgb(r, g, b);
		}
		#endregion

		#region Mouse tracking
		private Point mMouseEntryPosition;

		private void mResults_MouseEnter(object sender, EventArgs e)
		{
			mMouseEntryPosition = MousePosition;
		}

		private void mResults_MouseMove(object sender, MouseEventArgs e)
		{
			// Discard the location the mouse has on entering the control (as it may be that the control has just moved under the mouse, not the other way around)
			if (MousePosition == mMouseEntryPosition)
			{
				return;
			}

			// Hot tracking
			var hoverIndex = mResults.IndexFromPoint(e.X, e.Y);
			if (hoverIndex >= 0 && mResults.SelectedIndex != hoverIndex)
			{
				if (mResults.GetItemRectangle(hoverIndex).Bottom <= mResults.ClientRectangle.Bottom)
				{
					mResults.SelectedIndex = hoverIndex;
				}
				else
				{
					// Avoid the control scrolling
					mResults.BeginUpdate();
					var topIndex = mResults.TopIndex;
					mResults.SelectedIndex = hoverIndex;
					mResults.TopIndex = topIndex;
					mResults.EndUpdate();
				}
			}
		}
		#endregion

		#region Resizing
		protected override void OnResizeBegin(EventArgs e)
		{
			// Stop automatically sizing - the user is picking a size they want.
			mManualSizeApplied = true;
			base.OnResizeBegin(e);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			UpdateBanner();

			mResults.Invalidate();
		}

		protected override void OnResizeEnd(EventArgs e)
		{
			base.OnResizeEnd(e);

			mMaximumExpandHeight = Math.Max(Height, MinimumSize.Height + mResults.ItemHeight);

			Settings.Default.WindowPosition = new Rectangle(Left, Top, Width, mMaximumExpandHeight);
		}

		private void UpdateBanner()
		{
			if (mBannerImage != null)
			{
				BannerFactory.UpdateBanner(this, mBanner, mBannerImage, PwDefs.ProductName, Resources.BannerText, ref mBannerWidth);
			}
		}
		#endregion

		#region Searching
		private void mSearch_TextChanged(object sender, EventArgs e)
		{
			if (mSearch.Text.Length < 2)
			{
				// Stop searching
				mResultsUpdater.Enabled = false;
				ShowThrobber = false;
				Height = MinimumSize.Height;
				mManualSizeApplied = false;
				mResults.Items.Clear();
				mLastResultsUpdated = null;
				mLastResultsUpdatedNextAvailableIndex = 0;
			}
			else
			{
				// Start searching
				mNoResultsLabel.Visible = false;
				mCurrentSearch = mSearcher.Search(mSearch.Text);
				mResultsUpdater.Enabled = true;
				ShowThrobber = true;
				mResultsUpdater_Tick(null, EventArgs.Empty); // Quick poke just in case the results are already done.
			}
		}

		private void mResultsUpdater_Tick(object sender, EventArgs e)
		{
			if (mLastResultsUpdated != mCurrentSearch)
			{
				// Clear out old results and replace with new ones
				mResults.Items.Clear();
				mLastResultsUpdated = mCurrentSearch;
				mLastResultsUpdatedNextAvailableIndex = 0;
			}

			bool complete;
			mResults.Items.AddRange(mLastResultsUpdated.GetAvailableResults(ref mLastResultsUpdatedNextAvailableIndex, out complete).ToArray());

			if (mResults.Items.Count > 0)
			{
				if (mResults.SelectedIndex == -1)
				{
					mResults.SelectedIndex = 0;
					mResults.TopIndex = 0;
				}

				if (!mManualSizeApplied)
				{
					Height = Math.Min(mMaximumExpandHeight, MinimumSize.Height + (mResults.Items.Count * mResults.ItemHeight));
				}
			}

			if (complete)
			{
				ShowThrobber = false;
				mResultsUpdater.Enabled = false;

				if (mResults.Items.Count == 0)
				{
					mNoResultsLabel.Visible = true;
					Height = MinimumSize.Height + mResults.ItemHeight;
					mManualSizeApplied = false;
				}
			}
		}

		private bool ShowThrobber
		{
			get { return mThrobber.Visible; }
			set
			{
				if (value != ShowThrobber)
				{
					if (value)
					{
						mThrobber.Visible = true;

						// Set the margin on the textbox to allow room for the throbber
						NativeMethods.SetTextBoxRightMargin(mSearch, mThrobber.Width);
					}
					else
					{
						mThrobber.Visible = false;

						NativeMethods.SetTextBoxRightMargin(mSearch, 0);
					}
				}
			}
		}
		#endregion

		private void mBannerImage_MouseDown(object sender, MouseEventArgs e)
		{
			// Allow drag by banner image
			if (e.Button == MouseButtons.Left)
			{
				if (e.Clicks == 2)
				{
					// Re-center the form on double-click
					CenterToScreen();
				}
				else
				{
					NativeMethods.StartFormDrag(this);
				}
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Escape:
					Close();
					return true;
				case Keys.Up:
					TryChangeSelection(-1);
					return true;
				case Keys.Down:
					TryChangeSelection(1);
					return true;
				case Keys.PageUp:
					TryChangeSelection(-mResults.ClientSize.Height / mResults.ItemHeight);
					return true;
				case Keys.PageDown:
					TryChangeSelection(mResults.ClientSize.Height / mResults.ItemHeight);
					return true;
				case Keys.Home | Keys.Control:
					mResults.SelectedIndex = 0;
					return true;
				case Keys.End | Keys.Control:
					mResults.SelectedIndex = mResults.Items.Count - 1;
					return true;
				case Keys.Enter:
					PerformAction(Settings.Default.DefaultAction, GetSelectedEntry());
					break;
				case Keys.Enter | Keys.Shift:
					PerformAction(Settings.Default.AlternativeAction, GetSelectedEntry());
					break;
			}
			
			return base.ProcessCmdKey(ref msg, keyData);
		}

		#region Selection Changing

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			mResults.TopIndex -= (e.Delta / Math.Abs(e.Delta));
		}

		private void TryChangeSelection(int delta)
		{
			mResults.SelectedIndex = Math.Max(Math.Min(mResults.Items.Count - 1, mResults.SelectedIndex + delta), 0);
		}
		#endregion

		#region Actions

		private void mResults_MouseClick(object sender, MouseEventArgs e)
		{
			var clickIndex = mResults.IndexFromPoint(e.X, e.Y);
			if (clickIndex >= 0)
			{
				var clickedResult = mResults.Items[clickIndex] as SearchResult;
				if (clickedResult != null)
				{
					PerformAction((ModifierKeys & Keys.Shift) == Keys.Shift ? Settings.Default.AlternativeAction : Settings.Default.DefaultAction, clickedResult.Entry);
				}
			}
		}

		private void PerformAction(Actions action, PwEntry entry)
		{
			Close();

			if (entry != null)
			{
				switch (action)
				{
					case Actions.PerformAutoType:
						AutoTypeEntry(entry);
						break;
					case Actions.EditEntry:
						EditEntry(entry);
						break;
					case Actions.ShowEntry:
						ShowEntry(entry);
						break;
					default:
						throw new ArgumentOutOfRangeException("action");
				}
			}
		}

		private PwEntry GetSelectedEntry()
		{
			var selection = mResults.SelectedItem as SearchResult;
			if (selection != null)
			{
				return selection.Entry;
			}
			return null;
		}

		private void AutoTypeEntry(PwEntry entry)
		{
			bool result;
			if (ActiveForm != null)
			{
				result = AutoType.PerformIntoPreviousWindow(mMainForm, entry, mDatabase);
			}
			else
			{
				result = AutoType.PerformIntoCurrentWindow(entry, mDatabase);
			}
			if (!result)
			{
				SystemSounds.Beep.Play();

				if (Settings.Default.AlternativeAction != Actions.PerformAutoType)
				{
					PerformAction(Settings.Default.AlternativeAction, entry);
				}
			}
		}

		private void EditEntry(PwEntry entry)
		{
			using (var entryForm = new PwEntryForm())
			{
				entryForm.InitEx(entry, PwEditMode.EditExistingEntry, mDatabase, mMainForm.ClientIcons, false, false);

				ShowForegroundDialog(entryForm);
				
				mMainForm.UpdateUI(false, null, mDatabase.UINeedsIconUpdate, null, true, null, entryForm.HasModifiedEntry);
			}
		}

		private DialogResult ShowForegroundDialog(Form form)
		{
			mMainForm.EnsureVisibleForegroundWindow(false, false);
			form.StartPosition = FormStartPosition.CenterScreen;
			if (mMainForm.IsTrayed())
			{
				form.ShowInTaskbar = true;
			}

			form.Shown += ActivateFormOnShown;
			return form.ShowDialog(mMainForm);
		}

		private static void ActivateFormOnShown(object sender, EventArgs eventArgs)
		{
			var form = (Form)sender;
			form.Shown -= ActivateFormOnShown;
			form.Activate();
		}

		private void ShowEntry(PwEntry entry)
		{
			// Show this entry
			mMainForm.UpdateUI(false, null, true, entry.ParentGroup, true, null, false, null);
			SelectEntries(new PwObjectList<PwEntry> { entry }, true, true);
			mMainForm.EnsureVisibleEntry(entry.Uuid);
			mMainForm.UpdateUI(false, null, false, null, false, null, false);
			mMainForm.EnsureVisibleForegroundWindow(true, true);
		}

		/// <summary>
		/// Perform MainWindow.SelectEntries (through reflection)
		/// </summary>
		private void SelectEntries(PwObjectList<PwEntry> lEntries, bool bDeselectOthers, bool bFocusFirst)
		{
			if (mSelectEntriesMethod != null)
			{
				mSelectEntriesMethod.Invoke(mMainForm, new object[] { lEntries, bDeselectOthers, bFocusFirst });
			}
			else
			{
				Debug.Fail("Could not select the auto-typed entry, method not found");
			}
		}
		#endregion
	}
}
