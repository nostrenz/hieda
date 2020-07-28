using System.Diagnostics;
using System.Windows.Input;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for About.xaml
	/// </summary>
	public partial class About : System.Windows.Window
	{
		public About()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.Release = Constants.RELEASE;
			this.Build = Constants.BUILD;
			this.DbVersion = Constants.DB_VERSION;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public int Release
		{
			set { this.label_programVersion.Content = "Release: " + value; }
		}

		public int DbVersion
		{
			set { this.label_dbVersion.Content = "Database version: " + value; }
		}

		public long Build
		{
			set { this.label_build.Content = "build: " + value; }
		}

		#endregion

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Surpise!
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Picture_MouseUp(object sender, MouseButtonEventArgs e)
		{
			ProcessStartInfo processInfo = new ProcessStartInfo(@"https://www.youtube.com/watch?v=05uWWAXo55c");
			Process myProcess = Process.Start(processInfo);
		}

		#endregion
	}
}
