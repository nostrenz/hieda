using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Hieda.View.Window;
using System.Diagnostics;
using Hieda.Service.Database;
using Hieda.Properties;
using Path = System.IO.Path;

namespace Hieda
{
	public partial class App : Application
	{
		public static string appFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
		public static SQLite db = new SQLite();

		public static bool updated = false;

		public App() : base()
		{
			#if !DEBUG
			this.Dispatcher.UnhandledException += OnDispatcherUnhandledException;
			#endif

			// Load resources
			Tools.ChangeTheme(Settings.Default.Theme);
			Tools.ChangeLanguage(Settings.Default.Language);
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// To know if the program was updated.
		/// When launched from the updated it has the "/updated" command line argument.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		private bool WasUpdated(string[] args)
		{
			foreach (string arg in args) {
				if (arg == "/updated") {
					return true;
				}
			}

			return false;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public new MainWindow MainWindow
		{
			get { return ((MainWindow)System.Windows.Application.Current.MainWindow); }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Triggered when the program is started.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void App_Startup(object sender, StartupEventArgs e)
		{
			App.updated = this.WasUpdated(e.Args);
		}

		/// <summary>
		/// Triggered when the program is closed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void App_OnExit(object sender, ExitEventArgs e)
		{
			db.Disconnect();

			// Backup DB if enabled
			if (Settings.Default.BackupDbOnExit) {
				db.Backup();
			}

			// Shutdown Discord RPC
			if (Settings.Default.RPC_Enabled) {
				Tool.DiscordRpc.Shutdown();
			}
		}

		/// <summary>
		/// http://stackoverflow.com/questions/1472498/wpf-global-exception-handler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			this.MainWindow.Collection.Blackout(true);

			View.Window.CrashReport report = new View.Window.CrashReport(
				e.ToString(),
				e.Exception.Message,
				e.Exception.Source,
				e.Exception.Data.ToString(),
				e.Exception.ToString()
			);

			report.ShowDialog();

			this.Shutdown();

			// Prevent from having a Windows messagebox about the crash
			Process process = Process.GetCurrentProcess();
			process.Kill();
			process.Dispose();
		}

		#endregion Event
	}
}
