using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using AutoTypeSearch.Properties;
using KeePass.Forms;
using KeePass.Resources;
using KeePass.UI;
using KeePass.Util;
using KeePassLib;
using KeePassLib.Collections;
using KeePassLib.Native;

namespace AutoTypeSearch
{
	public partial class SearchWindow : Form
	{
		private const int SecondLineInset = 10;

		// HACK to work around mono bug
		private static readonly FieldInfo sMonoListBoxTopIndex = typeof(ListBox).GetField("top_index", BindingFlags.Instance | BindingFlags.NonPublic);

		private readonly MainForm mMainForm;
		private readonly Bitmap mBannerImage;
		private readonly Searcher mSearcher;

		private readonly Stream mThrobberImageStream;

		private int? mWindowTopBorderHeight;
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

			// Mono can't load animated gifs from resx without crashing, so load it from an embedded resource instead
			try
			{
				mThrobberImageStream = GetType().Assembly.GetManifestResourceStream("AutoTypeSearch.Throbber.gif");
				if (mThrobberImageStream != null)
				{
					mThrobber.Image = Image.FromStream(mThrobberImageStream);
				}
			}
			catch (Exception ex)
			{
				Debug.Fail("Failed to load Throbber.gif from embedded resource: " + ex.Message);
			}

			GlobalWindowManager.CustomizeControl(this);
			UIUtil.SetExplorerTheme(mResults, true);
			SetItemHeight();
		}

		public SearchWindow(MainForm mainForm, string infoBanner) : this()
		{
			mMainForm = mainForm;

			mInfoBanner.Height = Math.Max(mInfoBannerImage.Height, mInfoLabel.Font.Height) + mInfoBanner.Margin.Vertical;
			mInfoLabel.Padding = new Padding(0, (mInfoBanner.Height - mInfoLabel.Font.Height) / 2, 0, 0);
			mInfoLabel.Text = infoBanner;

			if (infoBanner == null)
			{
				mInfoBanner.Visible = false;
				mInfoBanner.Height = 0;
			}
			
			mSearcher = new Searcher(mMainForm.DocumentManager.GetOpenDatabases().ToArray());

			Icon = mMainForm.Icon;
			using (var bannerIcon = new Icon(Icon, 48, 48))
			{
				mBannerImage = bannerIcon.ToBitmap();
			}
			UpdateBanner();

			ShowThrobber = false;

			FontUtil.AssignDefaultItalic(mNoResultsLabel);
		}


		protected override void OnCreateControl()
		{
			base.OnCreateControl();

			if (NativeMethods.IsWindows10())
			{
				mWindowTopBorderHeight = PointToScreen(Point.Empty).Y - this.Top;
				NativeMethods.RefreshWindowFrame(Handle);
			}

			var windowRect = Settings.Default.WindowPosition;
			var collapsedWindowRect = windowRect;
			
			collapsedWindowRect.Height = mSearch.Bottom + (Height - ClientSize.Height);

			MinimumSize = new Size(MinimumSize.Width, collapsedWindowRect.Height);

			if (windowRect.IsEmpty || !IsOnScreen(collapsedWindowRect))
			{
				windowRect = new Rectangle(0, 0, Width, Height);
				Height = collapsedWindowRect.Height;

				CenterToScreen();
			}
			else
			{
				Location = windowRect.Location;
				Size = collapsedWindowRect.Size;
			}

			mMaximumExpandHeight = Math.Max(windowRect.Height, MinimumSize.Height + mResults.ItemHeight);
		}
		

		private static bool IsOnScreen(Rectangle rectangle)
		{
			return Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(rectangle));
		}

		private void SetItemHeight()
		{
			mResults.ItemHeight = mResults.Font.Height * 2 + 2;
		}

		protected override void WndProc(ref Message m)
		{
			if (mWindowTopBorderHeight.HasValue)
			{
				NativeMethods.RemoveWindowFrameTopBorder(ref m, mWindowTopBorderHeight.Value);
			}
			base.WndProc(ref m);
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
				if (mThrobber.Image != null)
				{
					mThrobber.Image.Dispose();
					mThrobber.Image = null;
					mThrobberImageStream.Dispose();
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
// ReSharper disable once HeuristicUnreachableCode - Not unreachable
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

			var image = GetImage(searchResult.Database, searchResult.Entry.CustomIconUuid, searchResult.Entry.IconId);
			var imageMargin = (drawingArea.Height - image.Height) / 2;
			e.Graphics.DrawImage(image, drawingArea.Left + imageMargin, drawingArea.Top + imageMargin, image.Width, image.Height);

			var textLeftMargin = drawingArea.Left + imageMargin * 2 + image.Width;
			var textBounds = new Rectangle(textLeftMargin, drawingArea.Top + 1, drawingArea.Width - textLeftMargin - 1, drawingArea.Height - 2);

			var line1Bounds = textBounds;
			line1Bounds.Height = e.Font.Height;
			var line2Bounds = line1Bounds;
			line2Bounds.Y += line2Bounds.Height - 1;
			line2Bounds.X += SecondLineInset;
			line2Bounds.Width -= SecondLineInset;

			var resultInTitleField = searchResult.FieldName == PwDefs.TitleField;

			var title = (resultInTitleField ? searchResult.FieldValue : searchResult.Title).Replace('\n', ' '); // The FieldValue may have references resolved, whereas the title is always read directly.

			var uniqueTitlePartWidth = 0;
			if (!String.IsNullOrEmpty(searchResult.UniqueTitlePart))
			{
				var uniqueTitlePart = searchResult.UniqueTitlePart.Replace('\n', ' ');

				var titleWidth = TextRenderer.MeasureText(e.Graphics, title, e.Font, line1Bounds.Size, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis).Width;

				var availableWidthForUniqueTitlePart = line1Bounds.Width - titleWidth;
				if (availableWidthForUniqueTitlePart > 20) // Don't bother including a unique part if there's no room for it
				{
					var uniqueTitlePartReversed = ReverseString(uniqueTitlePart);

					uniqueTitlePartWidth = TextRenderer.MeasureText(e.Graphics, uniqueTitlePartReversed, e.Font, new Size(availableWidthForUniqueTitlePart, line1Bounds.Height), TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.ModifyString).Width;

					uniqueTitlePart = ReverseString(uniqueTitlePartReversed);

					TextRenderer.DrawText(e.Graphics, uniqueTitlePart, e.Font, new Rectangle(line1Bounds.X, line1Bounds.Y, uniqueTitlePartWidth, line1Bounds.Height), SystemColors.GrayText, TextFormatFlags.NoPadding);
				}
			}

			var titleBounds = new Rectangle(line1Bounds.X + uniqueTitlePartWidth, line1Bounds.Y, line1Bounds.Width - uniqueTitlePartWidth, line1Bounds.Height);

			if (resultInTitleField)
			{
				// Found the result in the title field. Highlight title in first line.
				DrawHighlight(e, titleBounds, title, searchResult.Start, searchResult.Length);
			}

			TextRenderer.DrawText(e.Graphics, searchResult.Title, e.Font, titleBounds, SystemColors.WindowText, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

			if (resultInTitleField)
			{
				// Found the result in the title field. Use Username for second line.
				TextRenderer.DrawText(e.Graphics, KPRes.UserName + ": " + searchResult.Entry.Strings.ReadSafeEx(PwDefs.UserNameField), e.Font, line2Bounds, SystemColors.GrayText, TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
			}
			else
			{
				// Found the result in not title field. Show the matching result on second line
				
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
						var leftContextReversed = ReverseString(leftContext);
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
						leftContext = ReverseString(leftContextReversed);
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

		private static string ReverseString(string value)
		{
			return new String(value.ToCharArray().TakeWhile(c => c != '\0').Reverse().ToArray());
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

		private Image GetImage(PwDatabase database, PwUuid customIconId, PwIcon iconId)
		{
			Image image = null;
			if (!customIconId.Equals(PwUuid.Zero))
			{
				image = database.GetCustomIcon(customIconId, DpiUtil.ScaleIntX(16), DpiUtil.ScaleIntY(16));
			}
			if (image == null)
			{
				try { image = mMainForm.ClientIcons.Images[(int)iconId]; }
				catch (Exception) { Debug.Assert(false); }
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

			if (Height > MinimumSize.Height && Height != mMaximumExpandHeight)
			{
				mMaximumExpandHeight = Math.Max(Height, MinimumSize.Height + mResults.ItemHeight);
			}
			else
			{
				mManualSizeApplied = false;
			}

			Settings.Default.WindowPosition = new Rectangle(Left, Top, Width, mMaximumExpandHeight);
		}

		private void UpdateBanner()
		{
			if (mBannerImage != null)
			{
				BannerFactory.UpdateBanner(this, mBanner, mBannerImage, PwDefs.ProductName, Resources.BannerText, ref mBannerWidth);
			}
		}

		private void mSearch_LocationChanged(object sender, EventArgs e)
		{
			mThrobber.Location = new Point(mSearch.Right - mThrobber.Width - mThrobber.Margin.Right, mSearch.Top + (mSearch.Height - mThrobber.Height) / 2);
		}

		private void mResults_LocationChanged(object sender, EventArgs e)
		{
			mNoResultsLabel.Top = mResults.Top + (mResults.ItemHeight - mNoResultsLabel.Height) / 2;
		}
		#endregion

		#region Searching
		private static readonly SearchResultPrecedence SearchResultPrecedenceComparer = new SearchResultPrecedence();
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

		[SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Object arrays for Listbox.Items, known to be of correct type")]
		private void mResultsUpdater_Tick(object sender, EventArgs e)
		{
			if (mLastResultsUpdated != mCurrentSearch)
			{
				// Clear out old results and replace with new ones
				mResults.Items.Clear();
				mLastResultsUpdated = mCurrentSearch;
				mLastResultsUpdatedNextAvailableIndex = 0;
			}
			var existingResultsCount = mResults.Items.Count;
			
			bool complete;
			var newResults = mLastResultsUpdated.GetAvailableResults(ref mLastResultsUpdatedNextAvailableIndex, out complete);
			if (newResults.Length > 0)
			{
				mResults.BeginUpdate();
				
				SearchResult[] allResults;
				if (existingResultsCount > 0)
				{
					allResults = new SearchResult[existingResultsCount + newResults.Length];
					mResults.Items.CopyTo(allResults, 0);
					newResults.CopyTo(allResults, existingResultsCount);

					mResults.Items.Clear();
				}
				else
				{
					allResults = newResults;
				}

				CalculateUniqueTitles(allResults);

				Array.Sort(allResults, SearchResultPrecedenceComparer);
				mResults.Items.AddRange(allResults);
				
				mResults.EndUpdate();

				if (allResults.Length > 0)
				{
					if (mResults.SelectedIndex == -1)
					{
						try
						{
							// HACK to work around mono bug
							if (sMonoListBoxTopIndex != null)
							{
								sMonoListBoxTopIndex.SetValue(mResults, 1); // Set the top_index to 1 so that when selected index is set to 0, and calls EnsureVisible(0), it follows the index < top_index pass and not the broken index >= top_index + rows path. 
							}

							mResults.SelectedIndex = 0;
							mResults.TopIndex = 0;
						}
						catch (Exception ex)
						{
							Debug.Fail("Failed to set selection on count of " + allResults.Length + ": " + ex.Message);
						}
					}

					if (!mManualSizeApplied)
					{
						Height = Math.Min(mMaximumExpandHeight, MinimumSize.Height + (allResults.Length * mResults.ItemHeight));
					}
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

		private void CalculateUniqueTitles(IEnumerable<SearchResult> results, int depth = 0)
		{
			// Where results have identical titles, include group titles to make them unique
			depth += 1;

			// First create a lookup by title
			var titles = new Dictionary<string, List<SearchResult>>();
			foreach (var searchResult in results)
			{
				List<SearchResult> resultsWithSameTitle;
				if (titles.TryGetValue(searchResult.UniqueTitle, out resultsWithSameTitle))
				{
					resultsWithSameTitle.Add(searchResult);
				}
				else
				{
					titles.Add(searchResult.UniqueTitle, new List<SearchResult> { searchResult });
				}
			}

			// Attempt to unique-ify any non-unique titles
			foreach (var resultsSharingTitle in titles.Values)
			{
				if (resultsSharingTitle.Count > 1)
				{
					var titlesModified = false;
					foreach (var searchResult in resultsSharingTitle)
					{
						titlesModified |= searchResult.SetUniqueTitleDepth(depth);
					}

					if (titlesModified)
					{
						// Recurse in case of continuing non-uniqueness
						CalculateUniqueTitles(resultsSharingTitle, depth);
					}
				}
			}
		}

		private class SearchResultPrecedence : IComparer<SearchResult>
		{
			public int Compare(SearchResult x, SearchResult y)
			{
				// First precedence is that if the result is the start of the field value, it's higher precedence than if it doesn't.
				var result = -(x.Start == 0).CompareTo(y.Start == 0);

				// Second precedence is that the start of the title field is higher precedence than the start of any other field
				if (result == 0)
				{
					result = -(x.FieldName == PwDefs.TitleField).CompareTo(y.FieldName == PwDefs.TitleField);
				}

				// Both start the title field, so both equal. Have to have consistent ordering, so return final precedence based search index
				if (result == 0)
				{
					result = x.ResultIndex.CompareTo(y.ResultIndex);
				}
				
				return result;
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
						NativeMethods.SetTextBoxRightMargin(mSearch, mThrobber.Width + mThrobber.Margin.Right);
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

					Settings.Default.WindowPosition = new Rectangle(Left, Top, Width, mMaximumExpandHeight);
				}
				else if (!NativeLib.IsUnix())
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
					PerformAction(Settings.Default.DefaultAction, mResults.SelectedItem as SearchResult);
					break;
				case Keys.Enter | Keys.Shift:
					PerformAction(Settings.Default.AlternativeAction, mResults.SelectedItem as SearchResult);
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
			if (mResults.Items.Count > 0)
			{
				mResults.SelectedIndex = Math.Max(Math.Min(mResults.Items.Count - 1, mResults.SelectedIndex + delta), 0);
			}
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
					PerformAction((ModifierKeys & Keys.Shift) == Keys.Shift ? Settings.Default.AlternativeAction : Settings.Default.DefaultAction, clickedResult);
				}
			}
		}

		private void PerformAction(Actions action, SearchResult searchResult)
		{
			Close();

			if (searchResult != null)
			{
				switch (action)
				{
					case Actions.PerformAutoType:
						AutoTypeEntry(searchResult);
						break;
					case Actions.EditEntry:
						EditEntry(searchResult);
						break;
					case Actions.ShowEntry:
						ShowEntry(searchResult);
						break;
					case Actions.OpenEntryUrl:
						OpenEntryUrl(searchResult);
						break;
					case Actions.CopyPassword:
						CopyPassword(searchResult);
						break;
					default:
						throw new ArgumentOutOfRangeException("action");
				}
			}
		}

		private void AutoTypeEntry(SearchResult searchResult)
		{
			bool result;
			if (ActiveForm != null)
			{
				result = AutoType.PerformIntoPreviousWindow(mMainForm, searchResult.Entry, searchResult.Database);
			}
			else
			{
				result = AutoType.PerformIntoCurrentWindow(searchResult.Entry, searchResult.Database);
			}
			if (!result)
			{
				SystemSounds.Beep.Play();

				if (Settings.Default.AlternativeAction != Actions.PerformAutoType)
				{
					PerformAction(Settings.Default.AlternativeAction, searchResult);
				}
			}
		}

		private void EditEntry(SearchResult searchResult)
		{
			using (var entryForm = new PwEntryForm())
			{
				mMainForm.MakeDocumentActive(mMainForm.DocumentManager.FindDocument(searchResult.Database));
				
				entryForm.InitEx(searchResult.Entry, PwEditMode.EditExistingEntry, searchResult.Database, mMainForm.ClientIcons, false, false);

				ShowForegroundDialog(entryForm);

				mMainForm.UpdateUI(false, null, searchResult.Database.UINeedsIconUpdate, null, true, null, entryForm.HasModifiedEntry);
			}
		}

// ReSharper disable once UnusedMethodReturnValue.Local - Generic helper, result may be used in future
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

		private void ShowEntry(SearchResult searchResult)
		{
			// Show this entry
			mMainForm.UpdateUI(false, mMainForm.DocumentManager.FindDocument(searchResult.Database), true, searchResult.Entry.ParentGroup, true, null, false, null);
			mMainForm.SelectEntries(new PwObjectList<PwEntry> { searchResult.Entry }, true, true);
			mMainForm.EnsureVisibleEntry(searchResult.Entry.Uuid);
			mMainForm.UpdateUI(false, null, false, null, false, null, false);
			mMainForm.EnsureVisibleForegroundWindow(true, true);
		}

		private void OpenEntryUrl(SearchResult searchResult)
		{
			WinUtil.OpenEntryUrl(searchResult.Entry);
		}

		private void CopyPassword(SearchResult searchResult)
		{
			if (ClipboardUtil.Copy(searchResult.Entry.Strings.ReadSafe(PwDefs.PasswordField), true, true, searchResult.Entry,
									mMainForm.DocumentManager.SafeFindContainerOf(searchResult.Entry),
									IntPtr.Zero))
			{
				mMainForm.StartClipboardCountdown();
			}
		}
		
		#endregion
	}
}
