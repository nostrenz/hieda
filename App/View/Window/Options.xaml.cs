using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Hieda.Properties;
using File = System.IO.File;

namespace Hieda.View.Window
{
	public partial class Options : System.Windows.Window
	{
		const string DEFAULT_LANG = "en-US";

		private bool needRefresh = false;
		private bool needRestart = false;
		private bool mustRestart = false;

		private List<CultureInfo> cultures = new List<CultureInfo>();
		private List<string> themes = new List<string>();

		public Options()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.GetCultures();
			this.GetThemes();

			this.field_VLCPath.BrowseTextBox.Placeholder = "VLC executable...";
			this.field_MPCHC_Path.BrowseTextBox.Placeholder = "MPC-HC executable...";
			this.field_mpv_Path.BrowseTextBox.Placeholder = "mpv executable...";
			this.field_VLCPath.BrowseTextBox.Text = this.field_VLCPath.BrowseTextBox.Placeholder;
			this.field_VLCPath.BrowseButton.DefaultExtension = ".exe";
			this.field_VLCPath.BrowseButton.AddFilter("VLC executable", "vlc.exe");
			this.field_General_CoverFolder.BrowseTextBox.Text = Settings.Default.CoverFolder;
			this.field_General_DatabaseFolder.BrowseTextBox.Text = Settings.Default.Dbfolder;
			this.ComboBox_StartingList.SelectedIndex = (Settings.Default.StartingLabeledList ? 1 : 0);
			this.CheckBox_ContinueOnClose.IsChecked = Settings.Default.ContinueOnClose;

			if (!String.IsNullOrWhiteSpace(Settings.Default.VLC_Path)) {
				this.field_VLCPath.Text = Settings.Default.VLC_Path;
			}

			if (!String.IsNullOrWhiteSpace(Settings.Default.mpv_Path)) {
				this.field_mpv_Path.Text = Settings.Default.mpv_Path;
			}

			this.CheckBox_OpenInFullscreen.IsChecked = Settings.Default.OpenPlayerInFullscreen;
			this.Tiles_CheckBox_Overlay.IsChecked = Settings.Default.TileOverlay;
			this.Tiles_CheckBox_Over.IsChecked = Settings.Default.TileHover;
			this.Tiles_CheckBox_Direct.IsChecked = Settings.Default.TileDirect;
			this.Tiles_CheckBox_AutoMarkAsWatched.IsChecked = Settings.Default.AutoMarkAsWatched;
			this.Tiles_CheckBox_VideoThumbs.IsChecked = Settings.Default.TileVideoThumbs;
			this.Tiles_CheckBox_VideoThumbs.IsChecked = Settings.Default.TileVideoThumbs;
			this.Tiles_CheckBox_LoadAnimation.IsChecked = Settings.Default.TileLoadAnimation;
			this.checkbox_CollectionBg.IsChecked = Settings.Default.CollectionBg;
			this.CheckBox_SidebarBackground.IsChecked = Settings.Default.SidebarBackground;
			this.checkbox_AutoHideTaskBarFix.IsChecked = Settings.Default.AutoHideTaskBarFix;
			this.checkbox_Notify.IsChecked = Settings.Default.Notify;
			this.checkbox_FakeEpisodes.IsChecked = Settings.Default.FakeEpisode;
			this.checkBox_ShowOverlay.IsChecked = Settings.Default.ShowOverlay;
			this.checkbox_RememberFilter.IsChecked = Settings.Default.RememberFilter;
			this.checkbox_WhileScrolling.IsChecked = Settings.Default.WhileScrolling;
			this.Slider_LoadSpeed.Value = Settings.Default.LoadSpeed;
			this.LoadSpeedLabelValue = Settings.Default.LoadSpeed;
			this.Checkbox_DiscordRpc.IsChecked = Settings.Default.RPC_Enabled;
			this.Checkbox_ReplaceFileByDrop.IsChecked = Settings.Default.ReplaceFileByDrop;
			this.CheckBox_SavePositionOnQuit.IsChecked = Settings.Default.SavePositionOnQuit;
			this.Checkbox_GenerateThumbOnLaunch.IsChecked = Settings.Default.GenerateThumbOnLaunch;

			if (!String.IsNullOrWhiteSpace(Settings.Default.MPCHC_Path)) {
				this.field_MPCHC_Path.Text = Settings.Default.MPCHC_Path;
			}

			this.SetLanguagesCombo();
			this.SetThemesCombo();

			switch (Settings.Default.Theme) {
				case "default":
					this.combo_Theme.SelectedIndex = 0;
				break;
				case "MayaDark":
					this.combo_Theme.SelectedIndex = 1;
				break;
			}

			this.Tiles_CheckBox_NumberOnTile.IsChecked = Settings.Default.NumberOnTile;
			this.checkbox_BackupDbOnExit.IsChecked = Settings.Default.BackupDbOnExit;
			this.checkbox_UpdateOnStartup.IsChecked = Settings.Default.UpdateOnStartup;
			this.CheckMPCHCPath();
			this.CheckVLCPath();
			this.CheckmpvPath();

			switch (Settings.Default.TileOrderBy) {
				case "title":
					this.Tiles_ComboBox_OrderBy.SelectedIndex = 0;
				break;
				case "status":
					this.Tiles_ComboBox_OrderBy.SelectedIndex = 1;
				break;
				case "id":
					this.Tiles_ComboBox_OrderBy.SelectedIndex = 2;
				break;
			}

			switch (Settings.Default.TileOrderByDirection) {
				case "ASC":
					this.Tiles_RadioButton_OrderBy_ASC.IsChecked = true;
				break;
				case "DESC":
					this.Tiles_RadioButton_OrderBy_DESC.IsChecked = true;
				break;
			}

			switch (Settings.Default.PreferedPlayer) {
				case "VLC":
					this.combobox_PreferedPlayer.SelectedIndex = 1;
				break;
				case "MPC-HC":
					this.combobox_PreferedPlayer.SelectedIndex = 2;
				break;
				case "mpv":
					this.combobox_PreferedPlayer.SelectedIndex = 3;
				break;
				default: {
					this.combobox_PreferedPlayer.SelectedIndex = 0;
					this.CheckBox_OpenInFullscreen.IsEnabled = false;
				}
				break;
			}
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Get a list of languages files in the lang/ folder and show them in the languages combo.
		/// </summary>
		private void SetLanguagesCombo()
		{
			this.combo_Language.Items.Clear();

			foreach (CultureInfo culture in this.cultures) {
				this.combo_Language.Items.Add(culture.DisplayName);

				// Set selected value
				if (culture.ToString() == Settings.Default.Language) {
					this.combo_Language.SelectedValue = culture.DisplayName;
				}
			}
		}

		/// <summary>
		/// Get a list of themes files in the theme/ folder and show them in the themes combo.
		/// </summary>
		private void SetThemesCombo()
		{
			this.combo_Theme.Items.Clear();

			foreach (string theme in this.themes) {
				this.combo_Theme.Items.Add(theme);

				// Set selected value
				if (theme == Settings.Default.Theme) {
					this.combo_Theme.SelectedValue = theme;
				}
			}
		}

		private void CheckVLCPath()
		{
			if (File.Exists(this.field_VLCPath.Text)) {
				this.image_VLC_Test.Source = new BitmapImage(new Uri(@"pack://siteoforigin:,,,/res/check.png"));
			}
		}

		private void CheckMPCHCPath()
		{
			if (File.Exists(this.field_MPCHC_Path.Text)) {
				this.image_MPCHC_Test.Source = new BitmapImage(new Uri(@"pack://siteoforigin:,,,/res/check.png"));
			}
		}

		private void CheckmpvPath()
		{
			if (File.Exists(this.field_mpv_Path.Text)) {
				this.image_mpv_Test.Source = new BitmapImage(new Uri(@"pack://siteoforigin:,,,/res/check.png"));
			}
		}

		/// <summary>
		/// Save options.
		/// </summary>
		private void SaveValues()
		{
			this.CheckRestart();

			// Check if Discord RPC need to be initialized or shutdown
			Tool.DiscordRpc.ToggleRpc((bool)this.Checkbox_DiscordRpc.IsChecked);

			if (!String.IsNullOrWhiteSpace(this.field_VLCPath.BrowseTextBox.ActualText)) {
				Settings.Default.VLC_Path = this.field_VLCPath.BrowseTextBox.Text;
			}

			if (this.field_General_DatabaseFolder.BrowseTextBox.ActualText != "" && this.field_General_DatabaseFolder.BrowseTextBox.ActualText != null) {
				Settings.Default.Dbfolder = this.field_General_DatabaseFolder.BrowseTextBox.Text.Replace(AppDomain.CurrentDomain.BaseDirectory, ":AppFolder:");
			}

			if (this.field_General_CoverFolder.BrowseTextBox.ActualText != "" && this.field_General_CoverFolder.BrowseTextBox.ActualText != null) {
				Settings.Default.CoverFolder = this.field_General_CoverFolder.BrowseTextBox.Text.Replace(AppDomain.CurrentDomain.BaseDirectory, ":AppFolder:");
			}

			Settings.Default.OpenPlayerInFullscreen = (bool)this.CheckBox_OpenInFullscreen.IsChecked;
			Settings.Default.TileOverlay = (bool)this.Tiles_CheckBox_Overlay.IsChecked;
			Settings.Default.TileDirect = (bool)this.Tiles_CheckBox_Direct.IsChecked;
			Settings.Default.AutoMarkAsWatched = (bool)this.Tiles_CheckBox_AutoMarkAsWatched.IsChecked;
			Settings.Default.TileVideoThumbs = (bool)this.Tiles_CheckBox_VideoThumbs.IsChecked;
			Settings.Default.PreferedPlayer = this.combobox_PreferedPlayer.SelectedValue.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "");
			Settings.Default.StartingLabeledList = (this.ComboBox_StartingList.SelectedIndex == 1);
			Settings.Default.ContinueOnClose = (bool)this.CheckBox_ContinueOnClose.IsChecked;
			Settings.Default.SavePositionOnQuit = (bool)this.CheckBox_SavePositionOnQuit.IsChecked;

			if (!String.IsNullOrWhiteSpace(this.field_MPCHC_Path.BrowseTextBox.ActualText)) {
				Settings.Default.MPCHC_Path = this.field_MPCHC_Path.Text;
			}

			if (!String.IsNullOrWhiteSpace(this.field_mpv_Path.BrowseTextBox.ActualText)) {
				Settings.Default.mpv_Path = this.field_mpv_Path.Text;
			}
			
			Settings.Default.TileLoadAnimation = (bool)this.Tiles_CheckBox_LoadAnimation.IsChecked;
			Settings.Default.NumberOnTile = (bool)this.Tiles_CheckBox_NumberOnTile.IsChecked;
			Settings.Default.BackupDbOnExit = (bool)this.checkbox_BackupDbOnExit.IsChecked;
			Settings.Default.UpdateOnStartup = (bool)this.checkbox_UpdateOnStartup.IsChecked;
			Settings.Default.CollectionBg = (bool)this.checkbox_CollectionBg.IsChecked;
			Settings.Default.SidebarBackground = (bool)this.CheckBox_SidebarBackground.IsChecked;
			Settings.Default.AutoHideTaskBarFix = (bool)this.checkbox_AutoHideTaskBarFix.IsChecked;
			Settings.Default.Notify = (bool)this.checkbox_Notify.IsChecked;
			Settings.Default.FakeEpisode = (bool)this.checkbox_FakeEpisodes.IsChecked;
			Settings.Default.ShowOverlay = (bool)this.checkBox_ShowOverlay.IsChecked;
			Settings.Default.RememberFilter = (bool)this.checkbox_RememberFilter.IsChecked;
			Settings.Default.WhileScrolling = (bool)this.checkbox_WhileScrolling.IsChecked;
			Settings.Default.LoadSpeed = (int)this.Slider_LoadSpeed.Value;
			Settings.Default.RPC_Enabled = (bool)this.Checkbox_DiscordRpc.IsChecked;
			Settings.Default.ReplaceFileByDrop = (bool)this.Checkbox_ReplaceFileByDrop.IsChecked;
			Settings.Default.GenerateThumbOnLaunch = (bool)this.Checkbox_GenerateThumbOnLaunch.IsChecked;

			if ((bool)this.Tiles_CheckBox_Overlay.IsChecked && (bool)this.Tiles_CheckBox_Over.IsChecked) {
				Settings.Default.TileHover = true;
			} else {
				Settings.Default.TileHover = false;
			}

			// Order by
			if ((bool)this.Tiles_RadioButton_OrderBy_ASC.IsChecked) {
				Settings.Default.TileOrderByDirection = "ASC";
			} else if ((bool)this.Tiles_RadioButton_OrderBy_DESC.IsChecked) {
				Settings.Default.TileOrderByDirection = "DESC";
			}

			// Save selected language
			string selectedLanguage = this.cultures[this.combo_Language.SelectedIndex].ToString();
			string selectedTheme = this.themes[this.combo_Theme.SelectedIndex].ToString();

			// If we have changed the language
			if (selectedLanguage != Settings.Default.Language) {
				Settings.Default.Language = selectedLanguage;
				Tools.ChangeLanguage(selectedLanguage);
				this.needRestart = true;
			}

			// If we have changed the theme
			if (selectedTheme != Settings.Default.Theme) {
				Settings.Default.Theme = selectedTheme;
				Tools.ChangeTheme(selectedTheme);
				this.needRestart = true;
			}

			Settings.Default.Save();

			if (this.mustRestart) {
				MessageBox.Show(Lang.Text("mustRestart", "Hieda must restart for applying those changes."));
				Tools.Restart();
			} else if (this.needRestart) {
				Tools.AskRestart();
			}
		}

		private void GetCultures()
		{
			// Add default culture
			this.cultures.Add(CultureInfo.GetCultureInfo(DEFAULT_LANG));

			string folder = @"lang/";

			if (!System.IO.Directory.Exists(folder)) {
				return;
			}

			string[] langs = System.IO.Directory.GetFiles(folder, "*.tsl");

			foreach (string f in langs) {
				string lang = f.Replace("lang/", "").Replace(".tsl", "");

				if (lang != DEFAULT_LANG) {
					try {
						CultureInfo culture = CultureInfo.GetCultureInfo(lang);
						this.cultures.Add(culture);
					} catch {}
				}
			}
		}

		private void GetThemes()
		{
			// Add default themes
			foreach (string theme in Constants.embededThemes) {
				this.themes.Add(theme);
			}

			string folder = @"theme/";

			if (!System.IO.Directory.Exists(folder)) {
				return;
			}

			string[] themes = System.IO.Directory.GetFiles(folder, "*.xaml");

			foreach (string t in themes) {
				string theme = t.Replace("theme/", "").Replace(".xaml", "");
				this.themes.Add(theme);
			}
		}

		/// <summary>
		/// Check some of the value to see if a restart will be needed.
		/// Works by settting this.needRestart (will ask for a restart) and this.mustRestart (will restart without possiblity to cancel).
		/// </summary>
		private void CheckRestart()
		{
			this.needRestart = (bool)this.checkbox_AutoHideTaskBarFix.IsChecked != Settings.Default.AutoHideTaskBarFix;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		/// <summary>
		/// True if the user click on the Save button.
		/// </summary>
		public bool NeedRefresh
		{
			get { return this.needRefresh; }
		}

		/// <summary>
		/// Set the value of the loadspeed slider and its associated label.
		/// </summary>
		private int LoadSpeedLabelValue
		{
			set { this.Label_LoadSpeed.Content = String.Format(Lang.Content("prefs_LoadingSpeed", "Tile loading speed ({0} milliseconds)"), value); }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called by clicking on the cancel button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Called by clicking on the ok button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Ok_Click(object sender, RoutedEventArgs e)
		{
			this.needRefresh = true;

			this.SaveValues();
			this.Close();
		}

		/// <summary>
		/// Called by selecting a different item in the OrderBy combobox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_OrderBy_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Settings.Default.TileOrderBy = this.Tiles_ComboBox_OrderBy.SelectedItem.ToString().Replace("System.Windows.Controls.ComboBoxItem: ", "").ToLower();
		}

		/// <summary>
		/// Called by clicking on the overlay checkbox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CheckBox_Overlay_Click(object sender, RoutedEventArgs e)
		{
			this.Tiles_CheckBox_Over.IsEnabled = (bool)this.Tiles_CheckBox_Overlay.IsChecked;
		}

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
				MessageBox.Show(x.Message);
			}
		}

		/// <summary>
		/// Called when the value of the load speed slider is changed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Slider_LoadSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (this.IsLoaded) {
				this.LoadSpeedLabelValue = (int)this.Slider_LoadSpeed.Value;
			}
		}

		/// <summary>
		/// Enable or disable certain options when the "default" player is selected.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_PreferedPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.IsLoaded) {
				this.CheckBox_OpenInFullscreen.IsEnabled = (this.combobox_PreferedPlayer.SelectedIndex != 0);
			}
		}

		#endregion Event
	}
}
