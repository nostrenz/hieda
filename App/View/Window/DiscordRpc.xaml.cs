using System;
using System.Windows;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using Hieda.Properties;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for DiscordRpc.xaml
	/// </summary>
	public partial class DiscordRpc : System.Windows.Window
	{
		const string BASE_URL = "https://cdn.discordapp.com/app-assets/378515022260731904/";

		public DiscordRpc(string serieTitle, string largeImage, List<string> largeImages)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
			this.ComboBox_LargeImage.Text = largeImage;

			if (serieTitle != null) {
				this.Label_Details.Content = serieTitle;
			}

			foreach (string lI in largeImages) {
				ComboBoxItem comboBoxItem = new ComboBoxItem();
				comboBoxItem.Content = lI;
				this.ComboBox_LargeImage.Items.Add(comboBoxItem);
			}

			this.TextBox_ClientId.Text = Settings.Default.RPC_ClientId;
			this.ComboBox_LargeImage.SelectedValue = largeImage;
			this.CheckBox_Enable.IsChecked = Settings.Default.RPC_Enabled;

			this.SetImageFromSelectedKey();
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Show the large image.
		/// </summary>
		/// <param name="value"></param>
		private void ShowImage(string value)
		{
			this.Image_Large.Source = new BitmapImage(new Uri(BASE_URL + value));
			this.Image_Large.Visibility = Visibility.Visible;
			this.Grid_Labels.Margin = new Thickness() { Left = 100, Top = 34, Right = 0, Bottom = 10 };
		}

		/// <summary>
		/// Hide the large image.
		/// </summary>
		private void HideImage()
		{
			this.Image_Large.Source = null;
			this.Image_Large.Visibility = Visibility.Hidden;
			this.Grid_Labels.Margin = new Thickness() { Left = 10, Top = 34, Right = 0, Bottom = 10 };
		}

		private void SetImageFromSelectedKey()
		{
			string tag = this.SelectedLargeImageTag;

			// It's a custom large image key, no image to be shown
			if (String.IsNullOrEmpty(tag)) {
				this.HideImage();

				return;
			}

			// It's an embeded image key, show the image and use Hieda's default client ID
			this.ShowImage(tag);

			if (this.TextBox_ClientId.ActualText != Settings.Default.RPC_ClientId) {
				this.TextBox_ClientId.Text = null;
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		/// <summary>
		/// Get the large image key.
		/// </summary>
		public string LargeImage
		{
			get
			{
				if (this.ComboBox_LargeImage.Text == null) {
					return "";
				}

				return this.ComboBox_LargeImage.Text.Trim();
			}
		}

		public bool Saved
		{
			get; set;
		}

		private string SelectedLargeImageTag
		{
			get
			{
				if (!(this.ComboBox_LargeImage.SelectedItem is ComboBoxItem)) {
					return null;
				}

				ComboBoxItem item = (ComboBoxItem)this.ComboBox_LargeImage.SelectedItem;

				if (item == null || item.Tag == null) {
					return null;
				}

				return item.Tag.ToString();
			}
		}

		private string ClientId
		{
			get { return this.TextBox_ClientId.ActualText.Trim(); }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Open an URL when clicking on an HYperlink.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			try {
				Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
				e.Handled = true;
			} catch (Exception x) {
				Console.WriteLine(x.Message);
			}
		}

		/// <summary>
		/// Called when another large image key in selected in the ComboBox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_LargeImage_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.SetImageFromSelectedKey();
		}

		/// <summary>
		/// Called by clicking on the Save button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Save_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			string clientId = this.ClientId;
			string largeImage = this.LargeImage;
			clientId = (String.IsNullOrEmpty(clientId) ? null : clientId);

			// We have a custom client ID
			if (clientId != null) {
				// If given the client ID must be a 18-digits value
				if ((clientId.Length < 18 || !Tools.IsNumeric(clientId))) {
					MessageBox.Show(Lang.Text("clientId16Digits", "The client ID must be a 18 digits value."));

					return;
				}
			} else if (!String.IsNullOrEmpty(largeImage) && String.IsNullOrEmpty(this.SelectedLargeImageTag)) {
				// The user entered a custom large image key but not a client ID
				MessageBox.Show(String.Format(Lang.Text(
					"missingClientId",
					"You need to enter your own client ID for using '{0}' as an image key."
				), largeImage));

				return;
			}

			Settings.Default.RPC_LargeText = this.TextBox_Tooltip.ActualText;
			Settings.Default.RPC_Enabled = (bool)this.CheckBox_Enable.IsChecked;

			// No change to the client ID, just start or shutdown the RPC depending on the checkbox
			if (clientId == Settings.Default.RPC_ClientId) {
				Tool.DiscordRpc.ToggleRpc(Settings.Default.RPC_Enabled);
			} else { // The client ID is different, we need to restart the RPC
				Tool.DiscordRpc.RestartRpc();

				Settings.Default.RPC_ClientId = clientId;
			}

			// Used outside the class to know if the window was closed from the Save button
			this.Saved = true;

			Settings.Default.Save();
			this.Close();
		}

		#endregion Event
	}
}
