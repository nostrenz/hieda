using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hieda.Properties;

namespace Hieda.View.Element
{
	public partial class PlaceholderTextBox : TextBox
	{
		private string placeholder;

		public PlaceholderTextBox()
		{
			InitializeComponent();

			this.Focusable = true;
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private void HidePlaceholder()
		{
			if (Settings.Default.Theme == "MayaDark") {
				this.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#D2D2D2"));
			} else {
				this.Foreground = new SolidColorBrush(Colors.Black);
			}
		}

		private void ShowPlaceholder()
		{
			if (Settings.Default.Theme == "MayaDark") {
				this.Foreground = new SolidColorBrush(Colors.DarkGray);
			} else {
				this.Foreground = new SolidColorBrush(Colors.Gray);
			}

			this.Text = this.placeholder;
		}

		#endregion

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string Placeholder
		{
			set { this.placeholder = value; }
			get { return this.placeholder; }
		}

		/// <summary>
		/// Returns text ignoring the placeholder.
		/// </summary>
		public string ActualText
		{
			get
			{
				if (this.Text == this.placeholder) {
					return "";
				}

				return this.Text.Trim();
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
		/// Called when the textbox gets the focus.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			if (this.Text == this.placeholder && this.Text.Length > this.placeholder.Length) {
				this.HidePlaceholder();
			}
		}

		/// <summary>
		/// Called when the textbox loses focus.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (this.Text == "" && this.Text.Length < 1) {
				this.ShowPlaceholder();
			}
		}

		/// <summary>
		/// Called when the textbox is loaded, meaning it's ready to be used.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_Loaded(object sender, RoutedEventArgs e)
		{
			if (Settings.Default.Theme == "MayaDark") {
				if (this.ActualText == "") {
					this.Foreground = new SolidColorBrush(Colors.DarkGray);
				} else {
					this.Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#D2D2D2"));
				}
			} else {
				if (this.ActualText == "") {
					this.Foreground = new SolidColorBrush(Colors.Gray);
				} else {
					this.Foreground = new SolidColorBrush(Colors.Black);
				}
			}
		}

		/// <summary>
		/// Called when the content of the textbox is changed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (this.ActualText.Length < 1) {
				this.ShowPlaceholder();
			}

			if (!this.IsFocused || this.placeholder == null) {
				return;
			}

			if (this.Text.Length >= this.placeholder.Length + 1) {
				string tempstr = this.Text;
				this.HidePlaceholder();
				this.Text = tempstr.Replace(this.placeholder, "");

				if (this.ActualText.Length == 1) {
					this.Select(this.Text.Length + 1, 0);
				}
			}
		}

		/// <summary>
		/// Move the cursor at the start of the text when clicking on it while the placeholder is shown.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (!this.IsReadOnly && this.Text == this.placeholder) {
				this.Select(0, 0);
			}
		}

		#endregion
	}
}
