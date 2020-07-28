using System;
using System.Windows.Controls;

namespace Hieda.View.Element
{
	public partial class ImportRow : UserControl
	{
		private string path; // Full file path
		private string oldNumberContent = "";
		private string defaultTitle = "";

		public ImportRow()
		{
			InitializeComponent();
		}

		public ImportRow(string filepath)
		{
			InitializeComponent();

			this.textbox_Filepath.IsReadOnly = true;
			this.path = filepath;
			this.textbox_Filepath.Text = Tool.Path.LastSegment(filepath);
			this.textbox_Filepath.ToolTip = filepath;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string Filename
		{
			get { return this.textbox_Filepath.Text.ToString(); }
			set { this.textbox_Filepath.Text = value; }
		}

		public string Title
		{
			get
			{
				if (string.IsNullOrEmpty(this.textbox_Name.ActualText)) {
					return Tool.Path.LastSegment(this.path);
				} else {
					return this.textbox_Name.Text;
				}
			}
			set { this.textbox_Name.Text = value; }
		}

		public string Number
		{
			get
			{
				if (!String.IsNullOrWhiteSpace(this.textbox_Number.Text)) {
					return this.textbox_Number.Text;
				} else {
					return "0";
				}
			}
			set { this.textbox_Number.Text = value; }
		}

		public bool Watched
		{
			get { return (bool)this.checkbox_Viewed.IsChecked; }
			set { this.checkbox_Viewed.IsChecked = value; }
		}

		public string Path
		{
			get { return this.path; }
			set { this.path = value; }
		}

		/// <summary>
		/// True if all fields have correct values.
		/// </summary>
		public bool Greenlight
		{
			get { return !String.IsNullOrWhiteSpace(this.Number); }
		}

		public string DefaultTitle
		{
			set
			{
				this.textbox_Name.Text = value;
				this.defaultTitle = value;
			}
		}

		#endregion

		/*
		============================================
		Event
		============================================
		*/

		#region Event
		
		/// <summary>
		/// Called when the content of the number field is changed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_Number_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (Tools.IsNumeric(this.textbox_Number.Text)) {
				if (this.textbox_Name.Text.StartsWith(this.defaultTitle)) {
					this.textbox_Name.Text = this.defaultTitle + " " + this.textbox_Number.Text;
				}

				this.oldNumberContent = this.textbox_Number.Text;
			} else {
				this.textbox_Number.Text = this.oldNumberContent;
			}
		}

		#endregion
	}
}
