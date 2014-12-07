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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchWindow));
			this.mSearch = new System.Windows.Forms.TextBox();
			this.mResults = new System.Windows.Forms.ListBox();
			this.mLayout = new System.Windows.Forms.TableLayoutPanel();
			this.mBanner = new System.Windows.Forms.PictureBox();
			this.mThrobber = new System.Windows.Forms.PictureBox();
			this.mResultsUpdater = new System.Windows.Forms.Timer(this.components);
			this.mNoResultsLabel = new System.Windows.Forms.Label();
			this.mLayout.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.mBanner)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.mThrobber)).BeginInit();
			this.SuspendLayout();
			// 
			// mSearch
			// 
			this.mSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.mSearch.Location = new System.Drawing.Point(1, 60);
			this.mSearch.Margin = new System.Windows.Forms.Padding(1, 0, 0, 0);
			this.mSearch.Name = "mSearch";
			this.mSearch.Size = new System.Drawing.Size(522, 20);
			this.mSearch.TabIndex = 0;
			this.mSearch.TextChanged += new System.EventHandler(this.mSearch_TextChanged);
			// 
			// mResults
			// 
			this.mResults.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.mResults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mResults.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.mResults.FormattingEnabled = true;
			this.mResults.IntegralHeight = false;
			this.mResults.Location = new System.Drawing.Point(0, 80);
			this.mResults.Margin = new System.Windows.Forms.Padding(0);
			this.mResults.Name = "mResults";
			this.mResults.Size = new System.Drawing.Size(523, 194);
			this.mResults.TabIndex = 1;
			this.mResults.TabStop = false;
			this.mResults.MouseClick += new System.Windows.Forms.MouseEventHandler(this.mResults_MouseClick);
			this.mResults.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.mResults_DrawItem);
			this.mResults.MouseEnter += new System.EventHandler(this.mResults_MouseEnter);
			this.mResults.MouseMove += new System.Windows.Forms.MouseEventHandler(this.mResults_MouseMove);
			// 
			// mLayout
			// 
			this.mLayout.ColumnCount = 1;
			this.mLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.mLayout.Controls.Add(this.mSearch, 0, 1);
			this.mLayout.Controls.Add(this.mResults, 0, 2);
			this.mLayout.Controls.Add(this.mBanner, 0, 0);
			this.mLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mLayout.Location = new System.Drawing.Point(0, 0);
			this.mLayout.Name = "mLayout";
			this.mLayout.RowCount = 3;
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
			// mThrobber
			// 
			this.mThrobber.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.mThrobber.BackColor = System.Drawing.SystemColors.Window;
			this.mThrobber.Image = ((System.Drawing.Image)(resources.GetObject("mThrobber.Image")));
			this.mThrobber.Location = new System.Drawing.Point(503, 61);
			this.mThrobber.Name = "mThrobber";
			this.mThrobber.Size = new System.Drawing.Size(18, 18);
			this.mThrobber.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
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
			this.mNoResultsLabel.Location = new System.Drawing.Point(5, 85);
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
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MinimumSize = new System.Drawing.Size(160, 96);
			this.Name = "SearchWindow";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.TopMost = true;
			this.mLayout.ResumeLayout(false);
			this.mLayout.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.mBanner)).EndInit();
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
	}
}