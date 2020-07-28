using System;
using System.Windows;
using Hieda.Properties;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for CrashReport.xaml
	/// </summary>
	public partial class CrashReport : System.Windows.Window
	{
		public CrashReport(string type, string message, string source, string data, string traceback)
		{
			InitializeComponent();

			this.Title = "Please blame Nitori for her code.";
			this.label_Message.Content = string.Format("An unhandled exception occurred: {0}", message);
			this.textblock_Traceback.Text = traceback;
			this.label_Source.Content = source;
			this.label_Data.Content = data;
			this.label_Type.Content = type;
		}

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called when the gif reaches the last frame, replay it from the first one.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
		{
			this.gif.Position = new TimeSpan(0, 0, 1);
			this.gif.Play();
		}

		/// <summary>
		///  Called when ckicking on the "Copy" option in the TextBlock's context menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Copy_Click(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(this.textblock_Traceback.Text);
		}

		/// <summary>
		/// Called when clicking on the Restart button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Restart_Click(object sender, RoutedEventArgs e)
		{
			Tools.Restart();
		}

		/// <summary>
		/// Called when clicking on the Exit button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Exit_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Reset settings to default, may be usefull if one of them is causing constant crashes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Reset_Click(object sender, RoutedEventArgs e)
		{
			Settings.Default.Reset();
			Settings.Default.Save();

			Tools.Restart();
		}

		/// <summary>
		/// Re-download the latest version.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void Button_Update_Click(object sender, RoutedEventArgs e)
		{
			Tool.Update update = new Tool.Update();
			bool success = await update.RetrieveAsync();

			if (!success) {
				MessageBox.Show(Lang.NO_NEW_VERSION);
				this.Close();

				return;
			}

			(new Updater(update.Release, update.Changelog)).ShowDialog();
		}

		#endregion
	}
}
