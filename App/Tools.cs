using System;
using System.Windows;
using Tomers.WPF.Localization;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Hieda
{
	static class Tools
	{
		/*
		============================================
		Public
		============================================
		*/

		#region Public

		public static string GetTranslatedStatus(Entity.DefaultStatus status)
		{
			return Lang.Text("status_" + status);
		}

		public static string GetLangFromStatus(Entity.DefaultStatus status)
		{
			switch (status) {
				case Entity.DefaultStatus.All:
					return Lang.ALL;

				case Entity.DefaultStatus.ToWatch:
					return Lang.TO_WATCH;

				case Entity.DefaultStatus.Current:
					return Lang.CURRENT;

				case Entity.DefaultStatus.StandBy:
					return Lang.STANDBY;

				case Entity.DefaultStatus.Finished:
					return Lang.FINISHED;

				case Entity.DefaultStatus.Dropped:
					return Lang.DROPPED;

				default:
					return status.ToString();
			}
		}

		/// <summary>
		/// "MyString" -> "myString"
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string LowerFirst(string text)
		{
			return text[0].ToString().ToLower() + text.Substring(1);
		}

		/// <summary>
		/// "myString" -> "MyString"
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string UpperFirst(string text)
		{
			return text[0].ToString().ToUpper() + text.Substring(1);
		}

		/// <summary>
		/// Change the app theme.
		/// </summary>
		/// <param name="theme"></param>
		public static void ChangeTheme(string theme)
		{
			string absolute = App.appFolder + @"\theme\" + theme + ".xaml";
			string relative = "pack://siteoforigin:,,,/theme/" + theme + ".xaml";

			Uri uri = null;

			// Embeded themes
			if (!File.Exists(absolute)) {
				// Use default theme
				if (!Constants.embededThemes.Contains(theme)) {
					theme = Constants.embededThemes[0];
				}

				uri = new Uri("theme/" + theme + ".xaml", UriKind.Relative);
			} else { // Themes from the theme/ folder
				uri = new Uri(relative, UriKind.RelativeOrAbsolute);
			}

			// Set the theme
			Application.Current.Resources.Clear();
			Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = uri });
		}

		/// <summary>
		/// Change de app language.
		/// </summary>
		/// <param name="lang"></param>
		public static void ChangeLanguage(string lang)
		{
			string file = "lang/" + lang + ".tsl";

			if (!File.Exists(file)) {
				return;
			}

			try {
				CultureInfo culture = CultureInfo.GetCultureInfo(lang);

				LanguageDictionary.RegisterDictionary(culture, new Hieda.Tool.XmlLanguageDictionary(file));
				LanguageContext.Instance.Culture = culture;

				Lang.Initialize();
			} catch {
				// If error (invalid culture), default hard-coded texts will be used
			}
		}

		/// <summary>
		/// Restart the program.
		/// </summary>
		public static void Restart()
		{
			Process.Start(Application.ResourceAssembly.Location);
			Application.Current.Shutdown();
			Process.GetCurrentProcess().Kill();
		}

		/// <summary>
		/// Ask the user for restarting the program.
		/// </summary>
		public static void AskRestart()
		{
			View.Window.TwoButtonsDialog dialog = new View.Window.TwoButtonsDialog(
				Lang.NEED_RESTART, Lang.RESTART + "?", Lang.RESTART_NOW, Lang.CONTINUE_NO_RESTART
			);

			if (dialog.Open()) {
				Tools.Restart();
			}
		}

		/// <summary>
		/// To know if a string represent a number.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static bool IsNumeric(string str)
		{
			long result;

			return long.TryParse(str, out result);
		}

		/// <summary>
		/// Build a string of intergers each separated by a comma.
		/// </summary>
		/// <param name="integers"></param>
		/// <returns></returns>
		public static string InClause(List<int> integers)
		{
			string clause = "";
			int count = integers.Count;

			for (int i = 0; i < count; i++) {
				clause += integers[i].ToString();

				if (i < count - 1) {
					clause += ", ";
				}
			}

			return clause;
		}

		/// <summary>
		/// Run a method periodically.
		/// </summary>
		/// <param name="onTick"></param>
		/// <param name="dueTime"></param>
		/// <param name="interval"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public static async Task RunPeriodicAsync(Action onTick, TimeSpan interval, CancellationToken token)
		{
			// Repeat this loop until cancelled.
			while (!token.IsCancellationRequested) {
				// Call our onTick function.
				onTick.Invoke();

				// Wait to repeat again.
				if (interval > TimeSpan.Zero) {
					await Task.Delay(interval, token);
				}
			}
		}

		/// <summary>
		/// Create an open file dialog with a custom filter and default extension.
		/// </summary>
		/// <param name="filter"></param>
		/// <param name="defaultExt"></param>
		/// <returns></returns>
		public static OpenFileDialog CreateOpenFileDialog(string filter=null, string defaultExtension="")
		{
			OpenFileDialog dlg = new OpenFileDialog();

			dlg.DefaultExt = defaultExtension;
			dlg.Filter = filter;

			return dlg;
		}

		/// <summary>
		/// Create an open image dialog for images.
		/// </summary>
		/// <returns></returns>
		public static OpenFileDialog CreateOpenImageDialog()
		{
			return CreateOpenFileDialog(Constants.IMAGE_FILTER);
		}

		/// <summary>
		/// Check if the given filename has one of the given extensions.
		/// </summary>
		public static bool HasExtensions(string[] extensions, string filename)
		{
			return Array.Exists(
				extensions,
				element => element.Equals(
					Tool.Path.Extension(filename)
				)
			);
		}

		/// <summary>
		/// Replace accented characters by non accented ones in the given text.
		/// Borrowed from https://stackoverflow.com/a/2086575
		///
		/// Example: "Tést Tèst àéçîï" -> "Test Test aecii"
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string ReplaceDiacritics(string text)
		{
			if (text == null) {
				return null;
			}

			byte[] tempBytes = System.Text.Encoding.GetEncoding("ISO-8859-8").GetBytes(text);

			return System.Text.Encoding.UTF8.GetString(tempBytes);
		}

		/// <summary>
		/// Get a list of all the covers in the database.
		/// </summary>
		public static List<string> ListCoversInDatabase()
		{
			List<string> covers = App.db.FetchSingleColumn("cover", "SELECT DISTINCT cover FROM serie UNION SELECT DISTINCT cover FROM season UNION SELECT DISTINCT cover FROM episode");

			// Remove duplicates
			return covers.Distinct().ToList();
		}

		#endregion Public
	}
}
