using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Hieda.Properties;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using File = System.IO.File;
using Path = System.IO.Path;
using Point = System.Windows.Point;

namespace Hieda.View.Element.List
{
	public partial class Tile : Item, IItem
	{
		public event EventHandler DoubleClick;
		public event EventHandler PlayClick;
		public event EventHandler Edited;
		public event EventHandler MiddleClick;
		public event EventHandler Selected;
		public event EventHandler ContextMenuOpeningRequested;
		public event EventHandler Dropped;

		private bool selected = false;

		public Tile(ushort index)
		{
			InitializeComponent();

			this.Button_Continue.IsHitTestVisible = true;
			this.Button_Continue.Focusable = true;

			this.Image_Cover.Focusable = true;
			this.index = index;
			this.Label_Status_Small.Background.Opacity = 0;
			this.Border_CoverBorder.BorderBrush = null;

			this.Image_EpisodePlay.Opacity = 0;
			this.Field_Number.Enabled = false;
			this.Field_Number.OnlyNumeric = true;
			this.Field_Title.CanBeNull = false;
			this.Field_Number.CanBeNull = false;
			this.Field_Number.MaxChars = 3;

			if (Settings.Default.TileLoadAnimation) {
				this.Opacity = 0;
				this.FadeIn();
			}

			this.Field_Title.Edited += new EventHandler(this.ValuesEdited);
			this.Field_Number.Edited += new EventHandler(this.ValuesEdited);
			this.Field_Title.Selected += new EventHandler(this.FieldSelected);
			this.Field_Number.Selected += new EventHandler(this.FieldSelected);

			this.Label_EpisodesValuesDescr.Content = Lang.WATCHED + '/' + Lang.OWNED + '/' + Lang.TOTAL;

			Brush color = (Brush)new BrushConverter().ConvertFromString("#CC91EE91");
			this.Label_EpisodesValues.Foreground = color;
			this.Label_EpisodesValuesDescr.Foreground = color;

			// Set an empty context to prevent the one from the collection to be opened instead
			this.Image_Cover.ContextMenu = new ContextMenu();
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		public void CreateSerieAndSeasonContextMenu()
		{
			this.Image_Cover.ContextMenu = this.GetSerieAndSeasonContextMenu(this.CoverSource.ToString());
		}

		public void CreateEpisodeContextMenu()
		{
			this.Image_Cover.ContextMenu = this.GetEpisodeContextMenu(this.CoverSource.ToString());
		}

		public void CreateMultipleSeriesAndSeasonsContextMenu()
		{
			this.Image_Cover.ContextMenu = this.GetMultipleSeriesAndSeasonsContextMenu();
		}

		public void CreateMultipleEpisodesContextMenu()
		{
			this.Image_Cover.ContextMenu = this.GetMultipleEpisodesContextMenu();
		}

		public void RemoveEpisodesValuesOverlay()
		{
			this.Label_EpisodesValuesDescr.Opacity = 0;
			this.Label_EpisodesValues.Opacity = 0;
			this.Label_EpisodesValues.Background.Opacity = 0;
			this.Button_Continue.Opacity = 0;
		}

		/// <summary>
		/// Focus this tile.
		/// </summary>
		new public void Focus()
		{
			this.Image_Cover.Focus();
		}

		/// <summary>
		/// Reset cover, put the default one instead.
		/// </summary>
		public void SetNoCover()
		{
			this.CoverUri = "pack://application:,,,/res/no.jpg";
		}

		/// <summary>
		/// Display watched/owned/total episodes number.
		/// </summary>
		/// <param name="viewed"></param>
		/// <param name="owned"></param>
		/// <param name="total"></param>
		public void SetEpisodesValues(ushort viewed, ushort owned, ushort total)
		{
			this.Label_EpisodesValues.Content = viewed + "/" + owned + "/" + (total != 0 ? total.ToString() : "?");

			// 8 for "99/99/99"
			if (((string)this.Label_EpisodesValues.Content).Length > 8) {
				this.Label_EpisodesValues.FontSize = 33;
			}
		}

		public void OpenContext()
		{
			this.Image_Cover.ContextMenu.IsOpen = true;
		}

		public void Select()
		{
			this.selected = true;

			this.ShowBorder();
		}

		public void Unselect()
		{
			this.selected = false;

			this.RemoveBorder();
		}

		public void ToggleSelect()
		{
			this.selected = !this.selected;

			if (this.selected) {
				this.ShowBorder();
			} else {
				this.RemoveBorder();
			}
		}

		/// <summary>
		/// Update the tile from a serie.
		/// </summary>
		/// <param name="episode"></param>
		public void Update(Entity.Serie serie)
		{
			this.Title = serie.Title;
			this.Type = Constants.Type.None;

			this.SetCover(serie.Cover);

			if (serie.UserStatus != null) {
				if (serie.UserStatus.Type == 0) {
					this.StringSmallStatus = serie.UserStatus.Text;
				} else {
					this.StringBigStatus = serie.UserStatus.Text;
				}
			} else if (serie.StatusId < 0) {
				this.Status = (Entity.DefaultStatus)serie.StatusId;
			}

			this.NumberOfSeasons = serie.NumberOfSeasons;
			this.NumberOfEpisodes = serie.EpisodesOwned;
			this.SetEpisodesValues(serie.EpisodesViewed, serie.EpisodesOwned, serie.EpisodesTotal);
			this.Number = null;
		}

		/// <summary>
		/// Update the tile from a season.
		/// </summary>
		/// <param name="episode"></param>
		public void Update(Entity.Season season)
		{
			this.Title = season.Title;
			this.Type = season.Type;

			this.SetCover(season.DisplayCover);

			// Set Status
			if (season.UserStatus != null) {
				if (season.UserStatus.Type == 0) {
					this.StringSmallStatus = season.UserStatus.Text;
				} else {
					this.StringBigStatus = season.UserStatus.Text;
				}
			} else {
				this.Status = (Entity.DefaultStatus)season.StatusId;
			}

			this.NumberOfSeasons = 0;
			this.NumberOfEpisodes = season.EpisodesOwned;
			this.Number = season.Number.ToString();
			this.Field_Number.Enabled = true;

			this.SetEpisodesValues(season.EpisodesViewed, season.EpisodesOwned, season.EpisodesTotal);
		}

		/// <summary>
		/// Update the tile from an episode.
		/// </summary>
		/// <param name="episode"></param>
		public void Update(Entity.Episode episode)
		{
			this.Title = episode.Title;
			this.Type = Constants.Type.None;
			this.Watched = episode.Watched;

			this.SetCover(episode.DisplayCover);

			if (String.IsNullOrWhiteSpace(episode.Uri) || episode.IsFile && !File.Exists(episode.Uri)) {
				this.NoLinkedFile = true;
			}
			
			if (Settings.Default.NumberOnTile) {
				if (episode.Number != 0) {
					this.Number = episode.Number.ToString();
				} else {
					this.Number = "?";
				}
			} else {
				this.Number = null;
			}

			this.Field_Number.Enabled = true;
			this.Label_SeasonsCounter.Opacity = 0;
			this.Label_EpisodesCounter.Opacity = 0;
		}

		/// <summary>
		/// Set the cover from an image file.
		/// </summary>
		/// <param name="imageFilePath"></param>
		public void SetCover(string imageFilePath)
		{
			if (imageFilePath == null) {
				this.SetNoCover();

				return;
			}

			string coverPath = Settings.Default.CoverFolder.Replace(":AppFolder:", App.appFolder) + @"\thumb\" + imageFilePath;

			if (!String.IsNullOrEmpty(imageFilePath) && File.Exists(coverPath)) {
				this.CoverUri = coverPath;
			}
		}

		/// <summary>
		/// Set the cover from a bitmap object.
		/// </summary>
		/// <param name="bitmap"></param>
		public void SetCover(System.Drawing.Bitmap bitmap)
		{
			if (bitmap == null) {
				this.SetNoCover();
			} else {
				this.Image_Cover.Source = this.LoadBitmap(bitmap);
			}
		}

		#endregion

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// So much fun.
		/// </summary>
		/// <param name="seconds"></param>
		private void DoABarrelRoll(ushort seconds = 3)
		{
			DoubleAnimation da = new DoubleAnimation(360, 0, new Duration(TimeSpan.FromSeconds(seconds)));
			RotateTransform rt = new RotateTransform();

			this.RenderTransform = rt;
			this.RenderTransformOrigin = new Point(0.5, 0.5);

			// Rotate forever if seconds set to 0
			if (seconds == 0) {
				da.RepeatBehavior = RepeatBehavior.Forever;
			}

			rt.BeginAnimation(RotateTransform.AngleProperty, da);
		}

		private void DisplayEpisodesValuesOverlay()
		{
			this.Label_EpisodesValuesDescr.Opacity = 0.75;
			this.Label_EpisodesValues.Opacity = 0.8;
			this.Label_EpisodesValues.Background.Opacity = 1;

			this.Button_Continue.Opacity = 0;
			this.Button_Continue.IsHitTestVisible = true;
			this.Button_Continue.Focusable = true;
		}

		private void DisplayBigStatusOverlay()
		{
			this.Label_Status_Big.Opacity = 0.7;
		}

		private void RemoveBigStatusOverlay()
		{
			this.Label_Status_Big.Opacity = 0;
		}

		private void DisplaySmallStatusOverlay()
		{
			this.Label_Status_Small.Opacity = 0.8;
		}

		private void RemoveSmallStatusOverlay()
		{
			this.Label_Status_Small.Opacity = 0;
		}

		/// <summary>
		/// Calculate font size depending on text lenght.
		/// </summary>
		/// <param name="length"></param>
		/// <param name="max"></param>
		/// <returns></returns>
		private double GetFontSize(int length, byte max)
		{
			double fontSize = 50 / (length * 0.16);

			if (fontSize < max) {
				return fontSize;
			}

			return max;
		}

		private void ShowOverlay()
		{
			if (!Settings.Default.TileHover || !this.CanBeHovered) {
				return;
			}

			this.RemoveSmallStatusOverlay();
			this.RemoveBigStatusOverlay();

			DoubleAnimation animation = new DoubleAnimation(0.8, TimeSpan.FromSeconds(0.2));

			if (Collection.ItemLevel != Constants.Level.Episode) {
				this.DisplayEpisodesValuesOverlay();

				this.Label_EpisodesValuesDescr.BeginAnimation(OpacityProperty, animation);
				this.Label_EpisodesValues.BeginAnimation(OpacityProperty, animation);
				this.Button_Continue.BeginAnimation(OpacityProperty, animation);
			} else {
				this.RemoveEpisodesValuesOverlay();

				this.Image_EpisodePlay.BeginAnimation(OpacityProperty, animation);
			}
		}

		private void HideOverlay()
		{
			if (!Settings.Default.TileHover || !this.CanBeHovered) {
				return;
			}

			this.DisplaySmallStatusOverlay();
			this.DisplayBigStatusOverlay();

			DoubleAnimation animation = new DoubleAnimation(0, TimeSpan.FromSeconds(0.2));
			this.Label_EpisodesValuesDescr.BeginAnimation(OpacityProperty, animation);
			this.Label_EpisodesValues.BeginAnimation(OpacityProperty, animation);

			if (Collection.ItemLevel != Constants.Level.Episode) {
				this.Button_Continue.BeginAnimation(OpacityProperty, animation);
			} else {
				this.Image_EpisodePlay.BeginAnimation(OpacityProperty, animation);
			}
		}

		private void ShowBorder()
		{
			this.Border_CoverBorder.BorderBrush = (Brush)new BrushConverter().ConvertFromString(this.BorderColor);
		}

		private void RemoveBorder()
		{
			this.Border_CoverBorder.BorderBrush = null;
		}

		#endregion

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		/// <summary>
		/// Set or remove the gif overlay.
		/// </summary>
		private string BigOverlay
		{
			set
			{
				if (!Settings.Default.TileOverlay || value == null) {
					this.Label_Status_Big.Content = null;
					this.Grid_CoverOverlay.Background = null;
				} else {
					this.Grid_CoverOverlay.Background = (Brush)new BrushConverter().ConvertFromString("#000000");
					this.Grid_CoverOverlay.Background.Opacity = 0.4;
					this.Label_Status_Big.Content = value;
					this.Label_Status_Big.Opacity = 0.7;
					this.Label_Status_Big.FontSize = this.GetFontSize(value.Length, 70);

					if (!this.error) {
						this.SmallOverlay = null;
					}
				}
			}
		}

		/// <summary>
		/// Set or remove the small overlay.
		/// </summary>
		private string SmallOverlay
		{
			set
			{
				if (!Settings.Default.TileOverlay || value == null) {
					this.Label_Status_Small.Content = null;
					this.Label_Status_Small.Background.Opacity = 0;
				} else {
					this.Label_Status_Small.Content = value;
					this.Label_Status_Small.Opacity = 0.8;
					this.Label_Status_Small.Background.Opacity = 0.7;
					this.Label_Status_Small.FontSize = this.GetFontSize(value.Length, 30);

					this.BigOverlay = null;
				}
			}
		}

		public string Number
		{
			get
			{
				if (Tools.IsNumeric(this.Field_Number.Text)) {
					return this.Field_Number.Text;
				}

				return "0";
			}
			set
			{
				if (!Settings.Default.TileOverlay || value == null) {
					this.Field_Number.Opacity = 0;
					this.Field_Number.Text = value;
				} else {
					this.Field_Number.Opacity = 0.8;
					this.Field_Number.Text = value;
				}
			}
		}

		public string Title
		{
			get { return this.Field_Title.Text; }
			set { this.Field_Title.Text = value; }
		}

		public bool Watched
		{
			get { return this.watched; }
			set
			{
				this.watched = value;
				this.BigOverlay = value ? Lang.WATCHED : null;
			}
		}

		public Entity.DefaultStatus Status
		{
			set
			{
				if (!Settings.Default.TileOverlay) {
					return;
				}

				if (value == Entity.DefaultStatus.None) {
					this.BigOverlay = null;
					this.SmallOverlay = null;

					return;
				}

				if (value == Entity.DefaultStatus.Finished) {
					this.BigOverlay = Lang.FINISHED;
				} else if (value == Entity.DefaultStatus.Dropped) {
					this.BigOverlay = Lang.DROPPED;
				} else {
					this.SmallOverlay = Tools.GetLangFromStatus(value);
				}
			}
		}

		public string StringSmallStatus
		{
			set
			{
				if (Settings.Default.TileOverlay) {
					this.SmallOverlay = value;
				}
			}
		}

		public string StringBigStatus
		{
			set
			{
				if (Settings.Default.TileOverlay) {
					this.BigOverlay = value;
				}
			}
		}

		/// <summary>
		/// To know if the tile is being edited.
		/// </summary>
		public bool IsBeingEdited
		{
			get { return this.Field_Number.Edition || this.Field_Title.Edition; }
		}

		/// <summary>
		/// Change the Watched status.
		/// </summary>
		public bool MarkItemAsWatched
		{
			set
			{
				this.Watched = value;
				this.CreateEpisodeContextMenu();
			}
		}

		public string FullCoverPath
		{
			get
			{
				string coverSource = this.CoverSource.ToString();

				return Settings.Default.CoverFolder.Replace(
					":AppFolder:",
					AppDomain.CurrentDomain.BaseDirectory) + @"\full\" + coverSource.Substring(coverSource.LastIndexOf(@"/") + 1,
					coverSource.Length - coverSource.LastIndexOf(@"/") - 1
				);
			}
		}

		public ushort NumberOfSeasons
		{
			set
			{
				if (!Settings.Default.TileOverlay || value == 0) {
					this.Label_SeasonsCounter.Opacity = 0;
					this.Label_SeasonsCounter.Content = null;
				} else {
					this.Label_SeasonsCounter.Opacity = 1;
					this.Label_SeasonsCounter.Content = value.ToString() + " " + Lang.LowerFirst(Lang.Plural("season"));
				}
			}
		}

		public string GetEpisodesValues
		{
			get { return this.Label_EpisodesValues.Content.ToString(); }
		}

		public ushort NumberOfEpisodes
		{
			set
			{
				if (!Settings.Default.TileOverlay || value == 0) {
					this.Label_EpisodesCounter.Opacity = 0;
					this.Label_EpisodesCounter.Content = null;
				} else {
					this.Label_EpisodesCounter.Opacity = 1;
					this.Label_EpisodesCounter.Content = value.ToString() + " " + Lang.LowerFirst(Lang.Plural("episode"));
				}
			}
		}

		public string EpisodesValues
		{
			get { return this.Label_EpisodesValues.Content.ToString(); }
		}

		/// <summary>
		/// Check if this item is selected, meaning the user clicked on it.
		/// </summary>
		public bool IsSelected
		{
			get { return this.selected; }
		}

		/// <summary>
		/// Set the width for the cover.
		/// For example, use 516 for 16/9 aspect ratio.
		/// </summary>
		public int CoverWidth
		{
			set { this.Grid_Root.Width = value; }
		}

		/// <summary>
		/// Set the height for the cover.
		/// </summary>
		public int CoverHeight
		{
			set { this.Grid_Root.Height = value; }
		}

		/// <summary>
		/// Set the cover image using a path to an image file.
		/// </summary>
		private string CoverUri
		{
			set
			{
				BitmapImage bitmap = new BitmapImage();

				// Specifying those options does not lock the file on disk (meaning it can be deleted or overwritten) and allow the file to be reloaded in cache when we change the URI
				bitmap.BeginInit();
				bitmap.UriCachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);
				bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
				bitmap.UriSource = new Uri(value, UriKind.RelativeOrAbsolute);
				bitmap.EndInit();

				this.Image_Cover.Source = bitmap;
			}
		}

		/// <summary>
		/// Set the type element.
		/// </summary>
		public Constants.Type Type
		{
			set
			{
				if (value == Constants.Type.None) {
					this.Label_Type.Content = null;
					this.Label_Type.Background.Opacity = 0;

					return;
				}

				this.Label_Type.Content = Lang.Content(value.ToString().ToLower());
				this.Label_Type.Opacity = 0.6;
				this.Label_Type.Background.Opacity = 0.5;
			}
		}

		public ImageSource CoverSource
		{
			get { return this.Image_Cover.Source; }
		}

		public bool NoLinkedFile
		{
			set { this.Image_NoLinkedFile.Opacity = (value ? 1 : 0); }
		}

		#endregion

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Double click on the tile.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tile_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			// Don't do forward when holding the control key
			if (Keyboard.IsKeyDown(Key.LeftCtrl)) {
				return;
			}

			// The event is itercepeted in MainWindow, giving the order to the TileList to go to the next level
			if (e.ChangedButton == MouseButton.Left) {
				DoubleClick?.Invoke(this.Index, e);
			}
		}

		/// <summary>
		/// Callend when the item receive a mouse click.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void cover_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ButtonState != MouseButtonState.Pressed) {
				return;
			}

			// Selected event
			Selected?.Invoke(this.Index, e);

			if (!this.Image_Cover.IsFocused)
				this.Image_Cover.Focus();

			if (Keyboard.IsKeyDown(Key.LeftCtrl)) {
				this.ToggleSelect();
			} else {
				this.Select();
			}

			// Middle clicked
			if (e.ChangedButton == MouseButton.Middle && MiddleClick != null) {
				MiddleClick(this.Index, e);
			}
		}

		/// <summary>
		/// Used when navigating the collection using the arrow keys.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void cover_GotFocus(object sender, RoutedEventArgs e)
		{
			this.ShowBorder();
		}

		private void cover_LostFocus(object sender, RoutedEventArgs e)
		{
			// Don't unselect when holding left Ctrl or there is multiple elements selected
			if (Keyboard.IsKeyDown(Key.LeftCtrl)
			|| ((Window.MainWindow)Application.Current.MainWindow).SelectedCount > 1) {
				return;
			}

			this.Unselect();
		}

		private void cover_MouseEnter(object sender, MouseEventArgs e)
		{
			this.ShowOverlay();
		}

		private void cover_MouseLeave(object sender, MouseEventArgs e)
		{
			this.HideOverlay();
		}

		private void button_Continue_MouseEnter(object sender, MouseEventArgs e)
		{
			this.ShowOverlay();
		}

		private void button_Continue_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left && Settings.Default.TileHover && this.CanBeHovered) {
				PlayClick?.Invoke(this.Index, e);
			}
		}

		private void cover_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			ContextMenuOpeningRequested?.Invoke(this.Index, e);
		}

		/// <summary>
		/// Raised when a value is edited on the tile.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ValuesEdited(object sender, EventArgs e)
		{
			Edited?.Invoke(this.Index, e);
		}

		/// <summary>
		/// When an editable field is selected, the tile must also be set as selected.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FieldSelected(object sender, EventArgs e)
		{
			Selected?.Invoke(this.Index, e);
		}

		/// <summary>
		/// Go forward after a moment when dragging videos on a tile.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void tile_DragEnter(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

			// Don't go to the next level when no files or when dragging an image
			if (files == null || (files.Length == 1 && Tools.HasExtensions(Constants.ImageFilesExtensions, files[0]))) {
				return;
			}

			DispatcherTimer timer = new DispatcherTimer();

			timer.Tick += new EventHandler(DragTime);
			timer.Interval = TimeSpan.FromMilliseconds(750);
			timer.Start();
		}

		/// <summary>
		/// Raise event to go forward.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DragTime(object sender, EventArgs e)
		{
			// Stop the timer
			((DispatcherTimer)sender).Stop();

			if (!this.CursorIsOver() || Collection.level == Constants.Level.Episode) {
				return;
			}

			DoubleClick?.Invoke(this.Index, e);
		}

		/// <summary>
		/// Called when dropping a file onto the tile.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tile_Drop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
			
			if (files == null || files.Length != 1) {
				return;
			}

			System.Collections.Generic.KeyValuePair<int, string> pair = new System.Collections.Generic.KeyValuePair<int, string>(this.Index, files[0]);

			Dropped?.Invoke(pair, e);
		}

		#endregion
	}
}
