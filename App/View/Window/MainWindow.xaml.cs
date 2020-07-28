using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Hieda.Model;
using Hieda.Properties;
using Application = System.Windows.Application;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Path = Hieda.Tool.Path;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Level = Hieda.Constants.Level;

namespace Hieda.View.Window
{
	public partial class MainWindow : System.Windows.Window
	{
		private bool isFullscreen = false;
		private DispatcherTimer timer;

		public MainWindow()
		{
			try {
				InitializeComponent();
			} catch (System.Windows.Markup.XamlParseException) {
				// If the theme is already the default one, we're fucked
				if (Settings.Default.Theme == "default") {
					return;
				}

				// The XamlParseException can be caused by an external theme with missing or invalid elements,
				// preventing the app to start. To prevent that we set the default theme and restart the program.
				Settings.Default.Theme = "default";
				Settings.Default.Save();

				Tools.Restart();
			}

			this.Title += " - r" + Constants.RELEASE;
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Show a notification at the window bottom.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="title"></param>
		/// <param name="message"></param>
		/// <param name="noDuration">
		/// If true, won't automatically close the element after a certain time.
		/// </param>
		public void Notify(Constants.Notify type, string title, string message, bool noDuration=false)
		{
			this.notify.Type = type;
			this.notify.Title = title;
			this.notify.Message = message;

			// Make the notify element visible.
			if (!Settings.Default.Notify) {
				return;
			}

			(this.notify.RenderTransform = new TranslateTransform(0, -this.notify.Height))
				.BeginAnimation(
					TranslateTransform.YProperty,
					new DoubleAnimation(0, -this.notify.Height, TimeSpan.FromSeconds(0.3))
				);

			if (this.timer != null) {
				this.timer.Stop();
			}

			if (!noDuration) {
				// The longer the message is, the longer the notification will remain into view, but never under 2.5 seconds.
				int duration = message.Length * 100;

				if (duration < 2500) {
					duration = 2500;
				}

				this.timer = new DispatcherTimer();
				timer.Tick += new EventHandler(NotifyDurationElapsed);
				timer.Interval = TimeSpan.FromMilliseconds(duration);
				timer.Start();
			}
		}

		/// <summary>
		/// Hide the notification.
		/// </summary>
		public void HideNotify()
		{
			(this.notify.RenderTransform = new TranslateTransform(0, 0))
				.BeginAnimation(
					TranslateTransform.YProperty,
					new DoubleAnimation(-this.notify.Height, 0, TimeSpan.FromSeconds(0.5))
				);
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Called each time the window size change.
		/// </summary>
		private void ResizeWindowElements()
		{
			// Add some margin to the right and at the bottom to prevent the window from overflowing offscreen.
			if (this.WindowState == WindowState.Maximized) {
				this.RootWindow.Margin = new Thickness { Top = 5, Bottom = 5, Right = 5 };
			} else {
				this.RootWindow.Margin = new Thickness { Top = 5 };
			}

			if (this.IsLoaded) {
				this.Collection.ResizeUpdate();
			}
		}

		/// <summary>
		/// Look for unused covers and delete them.
		/// </summary>
		/// <param name="covers"></param>
		/// <param name="coversFromDb"></param>
		/// <returns></returns>
		private int DeleteUnusedCoverFiles()
		{
			string coverDir = Path.CoverFolder;

			if (!Directory.Exists(coverDir)) {
				return 0;
			}

			string fullDir = coverDir + @"\full\";
			string thumbDir = coverDir + @"\thumb\";

			bool fullDirExists = Directory.Exists(fullDir);
			bool thumbDirExists = Directory.Exists(thumbDir);

			if (!fullDirExists && !thumbDirExists) {
				return 0;
			}

			List<string> coversFromDb = Tools.ListCoversInDatabase();

			string[] fulls = null;
			string[] thumbs = null;

			// Retrieve the files from the cover folder
			if (fullDirExists) {
				fulls = Directory.GetFiles(coverDir + @"\full\");
			}

			if (thumbDirExists) {
				thumbs = Directory.GetFiles(coverDir + @"\thumb\");
			}

			int deleted = 0;

			// Delete full files not in db
			if (fulls != null) {
				deleted += this.CheckCoversToDelete(fulls, coversFromDb);
			}

			// Delete thumb files not in db
			if (thumbs != null) {
				deleted += this.CheckCoversToDelete(thumbs, coversFromDb);
			}

			return deleted;
		}

		/// <summary>
		/// Will check if the given covers are in use.
		/// </summary>
		/// <param name="covers"></param>
		/// <param name="coversFromDb"></param>
		/// <returns></returns>
		private int CheckCoversToDelete(string[] covers, List<string> coversFromDb)
		{
			int deleted = 0;

			foreach (string file in covers) {
				bool coverFileIsUsed = false;

				foreach (string cover in coversFromDb) {
					if (Path.LastSegment(file) == cover) {
						coverFileIsUsed = true;
					}
				}

				if (!coverFileIsUsed) {
					try {
						File.Delete(file);
						deleted++;
					} catch (Exception) { }
				}
			}

			return deleted;
		}

		/// <summary>
		/// Switch betweend maximised and normal window size.
		/// </summary>
		private void AdjustWindowSize()
		{
			if (this.WindowState == WindowState.Maximized) {
				this.WindowState = WindowState.Normal;
				this.titleBar_MaxButton.Content = "■";
			} else {
				this.WindowState = WindowState.Maximized;
				this.titleBar_MaxButton.Content = "▢";
			}
		}

		/// <summary>
		/// Go to or from fullscreen.
		/// </summary>
		private void ToggleFullscreen()
		{
			// Go fullscreen
			if (!this.isFullscreen) {
				this.ResizeMode = ResizeMode.NoResize;
				this.Width = Screen.PrimaryScreen.WorkingArea.Width;
				this.Height = Screen.PrimaryScreen.WorkingArea.Height + 48;
				this.Left = 0;
				this.Top = 0;
				this.WindowState = WindowState.Normal;
				this.isFullscreen = true;
			} else { // Disable fullscreen
				this.ResizeMode = ResizeMode.CanResize;
				this.WindowState = WindowState.Maximized;
				this.isFullscreen = false;
			}
		}

		/// <summary>
		/// Fix a bug affecting how the window is sized when maximized:
		/// the window is slightly larger than it should be, causing well placed elements on the right and bottom edge
		/// to be offscreen and prevent an autohide taskbar to be shown like normal.
		/// </summary>
		private void SizingFix()
		{
			if (Settings.Default.AutoHideTaskBarFix) {
				// If Single, WindowInitialized() cause a display bug to minimize/maximize/close buttons
				this.WindowStyle = WindowStyle.None;
				WindowSizing.WindowInitialized(this);

				// Can also fix the taskbar problem, but the program's size must be lower than the screen size
				//this.MaxHeight = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - 1;
			}
		}

		/// <summary>
		/// Check if there is an update.
		/// Open the Updater window if there is.
		/// </summary>
		/// <param name="notify"></param>
		private async void CheckForUpdate()
		{
			Tool.Update update = new Tool.Update();
			bool success = await update.RetrieveAsync();

			if (!success) {
				this.NoNewVersion();

				return;
			}

			(new Updater(update.Release, update.Changelog)).ShowDialog();

			this.Collection.Blackout(true);
			(new Updater(update.Release, update.Changelog)).ShowDialog();
			this.Collection.Blackout(false);
		}

		/// <summary>
		/// Show a notification informing that no new version were found.
		/// </summary>
		private void NoNewVersion()
		{
			this.Notify(Constants.Notify.Info, Lang.CHECK_FOR_UPDATE, Lang.NO_NEW_VERSION);
		}

		/// <summary>
		/// Show a notification after a successful update.
		/// </summary>
		private void ShowUpdated()
		{
			this.Notify(Constants.Notify.Info, Lang.UPDATED, String.Format(
				Lang.Text("updatedDescr", "Hieda updated to release {0} (build {1})"),
				Constants.RELEASE, Constants.BUILD
			));
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		/// <summary>
		/// The window title.
		/// </summary>
		public new string Title
		{
			get { return this.textBlock_Title.Text; }
			set { this.textBlock_Title.Text = value; }
		}

		/// <summary>
		/// Number of selected items in the collection.
		/// </summary>
		public int SelectedCount
		{
			get { return this.Collection.SelectedIndexes.Count; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Once the window is loaded, initialize the collection, adjust the window size and check for update.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// Initialize collection (view, model)
			Controller.Collection controller = new Controller.Collection(this.Collection, new Collection());

			// Apply window sizing fix
			this.SizingFix();

			// Check for update
			if (App.updated) {
				this.ShowUpdated();
			} else if (Settings.Default.UpdateOnStartup) {
				this.CheckForUpdate();
			}
		}

		/// <summary>
		/// Open the About window when clicking on the corresponding menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_About_Click(object sender, RoutedEventArgs e)
		{
			this.Collection.Blackout(true);

			new About().ShowDialog();

			this.Collection.Blackout(false);
		}

		/// <summary>
		/// Open the Preferences window when clicking on the corresponding menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_Edit_Preferences_Click(object sender, RoutedEventArgs e)
		{
			this.Collection.Blackout(true);

			Options options = new Options();
			options.ShowDialog();

			if (options.NeedRefresh) {
				this.Collection._controller.Refresh();
			}

			if (!Settings.Default.SidebarBackground) {
				this.Collection.Sidebar.RemoveBigCover();
			}

			this.Collection.Blackout(false);
		}

		/// <summary>
		/// Backup the DB when clicking on the corresponding menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_BackupDb_Click(object sender, RoutedEventArgs e)
		{
			try {
				App.db.Backup();

				this.Notify(Constants.Notify.Info, Lang.SUCCESS, Lang.Text("dbBackupSaved", "Database restore point saved!"));
			} catch (Exception) {
				this.Notify(Constants.Notify.Warning, Lang.ERROR, Lang.Text("dbCopyError"));
			}
		}

		/// <summary>
		/// Open the DbManager window when clicking on the corresponding menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_RestoreDb_Click(object sender, RoutedEventArgs e)
		{
			this.Collection.Blackout(true);

			View.Window.BackupManager dbManager = new View.Window.BackupManager(this);

			if (dbManager.NeedCollectionRefresh) {
				this.Collection._controller.RefreshStatus();
				this.Collection._controller.Refresh();
			}

			this.Collection.Blackout(false);
		}

		/// <summary>
		/// Open the Query window when clicking on the corresponding menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_ExecuteQuery_Click(object sender, RoutedEventArgs e)
		{
			this.Collection.Blackout(true);

			string table = null;
			int serieId = 0;
			int seasonId = 0;

			switch (this.Collection.Level) {
				case Level.Serie:
					table = Entity.Serie.TABLE;
				break;
				case Level.Season: {
					table = Entity.Season.TABLE;
					serieId = this.Collection._controller.CurrentSerie.Id;
				}
				break;
				case Level.Episode: {
					table = Entity.Episode.TABLE;
					serieId = this.Collection._controller.CurrentSerie.Id;
					seasonId = this.Collection._controller.CurrentSeason.Id;
				}
				break;
			}

			View.Window.Query window = new View.Window.Query(this, table, serieId, seasonId);
			window.ShowDialog();

			if (window.Greenlight && !String.IsNullOrWhiteSpace(window.SqlQuery)) {
				if (window.MustDoBackup) {
					App.db.Backup();
				}

				try {
					App.db.Execute(window.SqlQuery);
				} catch (Exception x) {
					MessageBox.Show(x.Message, Lang.CANT_QUERY);
				}

				this.Collection._controller.Refresh();
			}

			this.Collection.Blackout(false);
		}

		/// <summary>
		/// Keyboard controls.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MainWindow_KeyDown(object sender, KeyEventArgs e)
		{
			if (this.Collection.textbox_Search.IsFocused) {
				return;
			}

			if (e.Key == Key.Back) {
				this.Collection._controller.Back();
			} else if (e.Key == Key.F5) {
				this.Collection._controller.Refresh();
			} else if (e.Key == Key.F11) {
				this.ToggleFullscreen();
			} else if (this.Collection.SelectedIndex == -1 && (e.Key == Key.Left || e.Key == Key.Down || e.Key == Key.Right || e.Key == Key.Up)) {
				this.Collection.SelectedIndex = 0;
			} else {
				string key = e.Key.ToString();

				if (key.Length == 1) {
					this.Collection.FocusItemStartingWith(key);
				}
			}
		}

		/// <summary>
		/// Support for forward and back mouse buttons.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton.Equals(MouseButton.XButton1)) {
				this.Collection._controller.Back();
			}

			if (e.ChangedButton.Equals(MouseButton.XButton2)) {
				switch (this.Collection.Level) {
					case Level.Serie:
						if (this.Collection._controller.LastVisitedSerieIndex != -1) {
							this.Collection._controller.Forward(this.Collection._controller.LastVisitedSerieIndex);
						}
					break;
					case Level.Season:
						if (this.Collection._controller.LastVisitedSeasonIndex != -1) {
							this.Collection._controller.Forward(this.Collection._controller.LastVisitedSeasonIndex);
						}
					break;
				}
			}
		}

		/// <summary>
		/// Open or close the sidebar for the selected item when clicking on the corresponsig menu item.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_ToggleSidebar_Click(object sender, RoutedEventArgs e)
		{
			if (this.Collection.SelectedIndex != -1 && this.Collection.SelectedIndex < this.Collection.Count) {
				if (!this.Collection.Sidebar.IsOpen) {
					this.Collection.OpenSidebar();
				} else {
					this.Collection.CloseSidebar();
				}

				return;
			}

			this.Notify(
				Constants.Notify.Warning,
				Lang.NO_ITEM_SELECTED_TITLE,
				Lang.NO_ITEM_SELECTED_DESCR
			);
		}

		/// <summary>
		/// Remove images from covers/full and covers/thumb folders not referenced in DB.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_DeleteUnusedCoverFiles_Click(object sender, RoutedEventArgs e)
		{
			this.Collection.Blackout(true);

			TwoButtonsDialog dialog = new TwoButtonsDialog(
				Lang.Text("removeUnusedCovers", "This operation will remove unused covers from disk."),
				Lang.Text("deleteUnusedCover", "Delete unused cover files"), Lang.START, Lang.ABORT
			);

			dialog.Owner = this;

			if (dialog.Open()) {
				int deleted = this.DeleteUnusedCoverFiles();

				this.Notify(
					Constants.Notify.Notif,
					Lang.OPERATION_FINISHED_TITLE,
					String.Format(Lang.Content("fileRemovalSuccess"), deleted)
				);
			}

			this.Collection.Blackout(false);
		}

		/// <summary>
		/// Ask for vacuuming the database when clicking on the corresponding menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_VacuumDb_Click(object sender, RoutedEventArgs e)
		{
			this.Collection.Blackout(true);

			TwoButtonsDialog dialog = new TwoButtonsDialog(
				Lang.Text("compactDb", "This operation will compact the database to reduce its size and make it faster.\n\n(a backup will be made just in case)\n"),
				Lang.Text("vacuumDb", "Vacuum database"), Lang.START, Lang.ABORT
			);

			dialog.Owner = this;

			if (dialog.Open()) {
				App.db.Backup();
				App.db.Vacuum();

				this.Notify(
					Constants.Notify.Notif,
					Lang.OPERATION_FINISHED_TITLE,
					Lang.Text("vacuumSuccess", "Database vacuumed succesfully")
				);
			}

			this.Collection.Blackout(false);
		}

		/// <summary>
		/// Toggle between normal and maximized size when double clicking on the titlebar or allow to grad the window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left) {
				return;
			}

			if (e.ClickCount == 2) {
				AdjustWindowSize();
			} else {
				Application.Current.MainWindow.DragMove();
			}
		}

		/// <summary>
		/// Close the window (and shutdown the whole app) when clicking on the button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		/// <summary>
		/// Maximize the window when clicking on the button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MaximizeButton_Click(object sender, RoutedEventArgs e)
		{
			AdjustWindowSize();
		}

		/// <summary>
		/// Minimize the window when clicking on the button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MinimizeButton_Click(object sender, RoutedEventArgs e)
		{
			this.WindowState = WindowState.Minimized;
		}

		/// <summary>
		/// Save settings on exit.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			Settings.Default.Save();
		}

		/// <summary>
		/// Called each time the window size change.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			this.ResizeWindowElements();
		}

		/// <summary>
		/// Toggle fullscreen mode when clicking on the corresponding menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_ToggleFullscreen_Click(object sender, RoutedEventArgs e)
		{
			this.ToggleFullscreen();
		}

		/// <summary>
		/// Reconstruct the DB when clicking on the corresponding menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_EmptyDb_Click(object sender, RoutedEventArgs e)
		{
			this.Collection.Blackout(true);

			TwoButtonsDialog dialog = new TwoButtonsDialog(
				Lang.Text("emptyDbDescr"),
				Lang.WARNING, Lang.EXECUTE, Lang.ABORT
			);

			dialog.Owner = this;

			if (dialog.Open()) {
				App.db.Backup();
				App.db.Recreate();

				Collection.Level = Level.Serie;
				Collection.Breadcrumb = null;

				this.Collection._controller.RefreshStatus();
				this.Collection._controller.Refresh();
			}

			this.Collection.Blackout(false);
		}

		/// <summary>
		/// Automaticaly called x seconds after ShowNotify().
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NotifyDurationElapsed(object sender, EventArgs e)
		{
			this.HideNotify();

			// Stop the timer
			((DispatcherTimer)sender).Stop();
		}

		/// <summary>
		/// Check for update when clicking on the corresponding menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_Update_Click(object sender, RoutedEventArgs e)
		{
			this.CheckForUpdate();
		}

		/// <summary>
		/// Open the DataManager window when clicking on the corresponding menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_Edit_DataManager_Click(object sender, RoutedEventArgs e)
		{
			this.Collection.Blackout(true);

			DataManager dm = new DataManager();
			dm.ShowDialog();

			if (dm.NeedStatusReload) {
				this.Collection._controller.RefreshStatus();
			}

			this.Collection.Blackout(false);
		}

		/// <summary>
		/// Reload the current theme when clicking on the corresponding menuitem.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void menuitem_ReloadTheme_Click(object sender, RoutedEventArgs e)
		{
			Tools.ChangeTheme(Settings.Default.Theme);

			Tools.AskRestart();
		}

		/// <summary>
		/// Remove all the files kept in the cache folder.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_EmptyCache_(object sender, RoutedEventArgs e)
		{
			string cacheDir = Hieda.App.appFolder + @"\cache\";
			int deleted = 0;

			if (Directory.Exists(cacheDir)) {
				string[] files = Directory.GetFiles(cacheDir);

				foreach (string file in files) {
					try {
						File.Delete(file);

						deleted++;
					} catch { }
				}
			}

			this.Notify(
				Constants.Notify.Notif,
				Lang.OPERATION_FINISHED_TITLE,
				String.Format(Lang.Content("fileRemovalSuccess"), deleted)
			);
		}

		/// <summary>
		/// Called when clicking on the Discord RPC, open the Discord RPC preview window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_Edit_DiscordRpc_Click(object sender, RoutedEventArgs e)
		{
			bool opened = this.Collection._controller.ShowRpcWindow();

			// The window can't be opened
			if (!opened) {
				this.Notify(Constants.Notify.Notif,
					Lang.SELECT_ITEM_FIRST,
					Lang.Text("selectSerieOrSeason", "First select a serie, a season or go to episodes level.")
				);
			}
		}

		/// <summary>
		/// Open the Dropbox control window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItem_Dropbox_Click(object sender, RoutedEventArgs e)
		{
			View.Window.Dropbox dropbox = new View.Window.Dropbox();

			if (dropbox.NeedReload) {
				this.Collection._controller.RefreshStatus();
				this.Collection._controller.Refresh();
			}
		}

		#endregion Event
	}
}
