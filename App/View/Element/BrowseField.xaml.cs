using System.Windows.Controls;

namespace Hieda.View.Element
{
	public partial class BrowseField : UserControl
	{
		public BrowseField()
		{
			InitializeComponent();

			this.BrowseButton.LinkedTextBox = this.BrowseTextBox;
		}

		/*
		============================================
		Public
		============================================
		*/

		public void AddFilter(string name, string extensions)
		{
			this.BrowseButton.AddFilter(name, extensions);
		}

		public void AddFilter(string filter)
		{
			this.BrowseButton.AddFilter(filter);
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public int TextWidth
		{
			set { this.BrowseTextBox.Width = value; }
			get { return (int)this.BrowseTextBox.Width; }
		}
		public int ButtonWidth
		{
			set
			{
				this.BrowseButton.Width = value;
				this.BrowseTextBox.Margin = new System.Windows.Thickness() {
					Top = 0,
					Left = 0,
					Right = value + 5,
					Bottom = 0
				};
			}
			get { return (int)this.BrowseButton.Width; }
		}

		public string Text
		{
			get { return this.BrowseTextBox.Text; }
			set { this.BrowseTextBox.Text = value; }
		}

		public string ActualText
		{
			get { return this.BrowseTextBox.ActualText.Trim(); }
		}

		public string Placeholder
		{
			get { return this.BrowseTextBox.Placeholder; }
			set { this.BrowseTextBox.Placeholder = value; }
		}

		public bool IsFolderPicker
		{
			get { return this.BrowseButton.IsFolderPicker; }
			set { this.BrowseButton.IsFolderPicker = value; }
		}

		public string DefaultExtension
		{
			set { this.BrowseButton.DefaultExtension = value; }
			get { return this.BrowseButton.DefaultExtension; }
		}

		#endregion
	}
}
