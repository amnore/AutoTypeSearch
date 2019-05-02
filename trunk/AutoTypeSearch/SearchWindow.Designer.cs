using System.Windows.Forms;

namespace AutoTypeSearch
{
	partial class SearchWindow
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.mSearch = new System.Windows.Forms.TextBox();
			this.mResults = new System.Windows.Forms.ListBox();
			this.mLayout = new System.Windows.Forms.TableLayoutPanel();
			this.mBanner = new System.Windows.Forms.PictureBox();
			this.mInfoBanner = new System.Windows.Forms.Panel();
			this.mInfoLabel = new System.Windows.Forms.Label();
			this.mInfoBannerImage = new System.Windows.Forms.PictureBox();
			this.mThrobber = new System.Windows.Forms.PictureBox();
			this.mResultsUpdater = new System.Windows.Forms.Timer(this.components);
			this.mNoResultsLabel = new System.Windows.Forms.Label();
			this.mLayout.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.mBanner)).BeginInit();
			this.mInfoBanner.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.mInfoBannerImage)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.mThrobber)).BeginInit();
			this.SuspendLayout();
			// 
			// mSearch
			// 
			this.mSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mSearch.Location = new System.Drawing.Point(1, 78);
			this.mSearch.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
			this.mSearch.Name = "mSearch";
			this.mSearch.Size = new System.Drawing.Size(521, 20);
			this.mSearch.TabIndex = 0;
			this.mSearch.LocationChanged += new System.EventHandler(this.mSearch_LocationChanged);
			this.mSearch.TextChanged += new System.EventHandler(this.mSearch_TextChanged);
			// 
			// mResults
			// 
			this.mResults.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.mResults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mResults.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.mResults.FormattingEnabled = true;
			this.mResults.IntegralHeight = false;
			this.mResults.Location = new System.Drawing.Point(0, 98);
			this.mResults.Margin = new System.Windows.Forms.Padding(0);
			this.mResults.Name = "mResults";
			this.mResults.Size = new System.Drawing.Size(523, 176);
			this.mResults.TabIndex = 1;
			this.mResults.TabStop = false;
			this.mResults.MouseClick += new System.Windows.Forms.MouseEventHandler(this.mResults_MouseClick);
			this.mResults.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.mResults_DrawItem);
			this.mResults.LocationChanged += new System.EventHandler(this.mResults_LocationChanged);
			this.mResults.MouseEnter += new System.EventHandler(this.mResults_MouseEnter);
			this.mResults.MouseMove += new System.Windows.Forms.MouseEventHandler(this.mResults_MouseMove);
			// 
			// mLayout
			// 
			this.mLayout.ColumnCount = 1;
			this.mLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.mLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.mLayout.Controls.Add(this.mSearch, 0, 2);
			this.mLayout.Controls.Add(this.mResults, 0, 3);
			this.mLayout.Controls.Add(this.mBanner, 0, 0);
			this.mLayout.Controls.Add(this.mInfoBanner, 0, 1);
			this.mLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mLayout.Location = new System.Drawing.Point(0, 0);
			this.mLayout.Name = "mLayout";
			this.mLayout.RowCount = 4;
			this.mLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.mLayout.Size = new System.Drawing.Size(523, 274);
			this.mLayout.TabIndex = 2;
			// 
			// mBanner
			// 
			this.mBanner.Dock = System.Windows.Forms.DockStyle.Top;
			this.mBanner.Location = new System.Drawing.Point(0, 0);
			this.mBanner.Margin = new System.Windows.Forms.Padding(0);
			this.mBanner.Name = "mBanner";
			this.mBanner.Size = new System.Drawing.Size(523, 60);
			this.mBanner.TabIndex = 3;
			this.mBanner.TabStop = false;
			this.mBanner.MouseDown += new System.Windows.Forms.MouseEventHandler(this.mBannerImage_MouseDown);
			// 
			// mInfoBanner
			// 
			this.mInfoBanner.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.mInfoBanner.BackColor = System.Drawing.SystemColors.Info;
			this.mInfoBanner.Controls.Add(this.mInfoLabel);
			this.mInfoBanner.Controls.Add(this.mInfoBannerImage);
			this.mInfoBanner.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mInfoBanner.Location = new System.Drawing.Point(2, 61);
			this.mInfoBanner.Margin = new System.Windows.Forms.Padding(2, 1, 1, 1);
			this.mInfoBanner.Name = "mInfoBanner";
			this.mInfoBanner.Size = new System.Drawing.Size(520, 16);
			this.mInfoBanner.TabIndex = 8;
			// 
			// mInfoLabel
			// 
			this.mInfoLabel.AutoEllipsis = true;
			this.mInfoLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mInfoLabel.ForeColor = System.Drawing.SystemColors.InfoText;
			this.mInfoLabel.Location = new System.Drawing.Point(16, 0);
			this.mInfoLabel.Name = "mInfoLabel";
			this.mInfoLabel.Size = new System.Drawing.Size(504, 16);
			this.mInfoLabel.TabIndex = 6;
			this.mInfoLabel.Text = "AutoType failed to find";
			// 
			// mInfoBannerImage
			// 
			this.mInfoBannerImage.Dock = System.Windows.Forms.DockStyle.Left;
			this.mInfoBannerImage.Image = global::AutoTypeSearch.Properties.Resources.Info;
			this.mInfoBannerImage.Location = new System.Drawing.Point(0, 0);
			this.mInfoBannerImage.Margin = new System.Windows.Forms.Padding(0);
			this.mInfoBannerImage.Name = "mInfoBannerImage";
			this.mInfoBannerImage.Size = new System.Drawing.Size(16, 16);
			this.mInfoBannerImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.mInfoBannerImage.TabIndex = 7;
			this.mInfoBannerImage.TabStop = false;
			// 
			// mThrobber
			// 
			this.mThrobber.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.mThrobber.BackColor = System.Drawing.SystemColors.Window;
			this.mThrobber.Location = new System.Drawing.Point(503, 81);
			this.mThrobber.Name = "mThrobber";
			this.mThrobber.Size = new System.Drawing.Size(16, 16);
			this.mThrobber.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.mThrobber.TabIndex = 4;
			this.mThrobber.TabStop = false;
			this.mThrobber.Visible = false;
			// 
			// mResultsUpdater
			// 
			this.mResultsUpdater.Interval = 250;
			this.mResultsUpdater.Tick += new System.EventHandler(this.mResultsUpdater_Tick);
			// 
			// mNoResultsLabel
			// 
			this.mNoResultsLabel.AutoSize = true;
			this.mNoResultsLabel.Location = new System.Drawing.Point(5, 103);
			this.mNoResultsLabel.Name = "mNoResultsLabel";
			this.mNoResultsLabel.Size = new System.Drawing.Size(84, 13);
			this.mNoResultsLabel.TabIndex = 5;
			this.mNoResultsLabel.Text = "No results found";
			// 
			// SearchWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.ClientSize = new System.Drawing.Size(523, 274);
			this.ControlBox = false;
			this.Controls.Add(this.mNoResultsLabel);
			this.Controls.Add(this.mThrobber);
			this.Controls.Add(this.mLayout);
			this.MinimumSize = new System.Drawing.Size(160, 96);
			this.Name = "SearchWindow";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.TopMost = true;
			this.mLayout.ResumeLayout(false);
			this.mLayout.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.mBanner)).EndInit();
			this.mInfoBanner.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.mInfoBannerImage)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.mThrobber)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox mSearch;
		private System.Windows.Forms.ListBox mResults;
		private System.Windows.Forms.TableLayoutPanel mLayout;
		private System.Windows.Forms.PictureBox mBanner;
		private PictureBox mThrobber;
		private Timer mResultsUpdater;
		private Label mNoResultsLabel;
		private Label mInfoLabel;
		private Panel mInfoBanner;
		private PictureBox mInfoBannerImage;
	}
}