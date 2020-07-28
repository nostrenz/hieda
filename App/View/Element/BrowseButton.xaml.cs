using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;

namespace Hieda.View.Element
{
	/// <summary>
	/// Interaction logic for BrowseButton.xaml
	/// </summary>
	public partial class BrowseButton : Button
	{
		private TextBox textbox;
		private string defaultExtension; // Example: ".txt"
		private bool isFolderPicker = false;
		CommonOpenFileDialog dialog;

		public BrowseButton()
		{
			InitializeComponent();
		}

		/*
		============================================
		Public
		============================================
		*/

		public void AddFilter(string name, string extensions)
		{
			// Dialog not yet initialized
			if (this.dialog == null) {
				return;
			}

			this.dialog.Filters.Add(new CommonFileDialogFilter(name, extensions));
		}

		public void AddFilter(string filter)
		{
			string[] parts = filter.Split('|');

			if (parts.Length == 2) {
				this.AddFilter(parts[0], parts[1]);
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public TextBox LinkedTextBox
		{
			set { this.textbox = value; }
			get { return this.textbox; }
		}

		public string DefaultExtension
		{
			set { this.defaultExtension = value; }
			get { return this.defaultExtension; }
		}

		public bool IsFolderPicker
		{
			get { return this.isFolderPicker; }
			set { this.isFolderPicker = value; }
		}

		#endregion

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		private void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			this.dialog = new CommonOpenFileDialog {
				IsFolderPicker = this.isFolderPicker,
				DefaultExtension = this.defaultExtension
			};

			if (this.dialog.ShowDialog() == CommonFileDialogResult.Ok && this.textbox != null) {
				this.textbox.Text = this.dialog.FileName;
			}
		}

		#endregion
	}
}
