using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Hieda.Properties;
using System.Reflection;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using File = System.IO.File;
using Path = System.IO.Path;

namespace Hieda.View.Element.List
{
	public partial class Row : Item, IItem
	{
		public event EventHandler Edited;
		public event EventHandler DoubleClick;
		public event EventHandler PlayClick;
		public event EventHandler Selected;
		public event EventHandler MiddleClick;
		public event EventHandler ContextMenuOpeningRequested;
		public event EventHandler Dropped;

		private bool selected = false;

		public Row(ushort index)
		{
			InitializeComponent();

			this.Button_Continue.IsHitTestVisible = true;
			this.Button_Continue.Focusable = true;

			this.Image_Cover.Focusable = true;
			this.Grid_CoverContainer.Focusable = false;
			this.Grid_CoverOverlay.Focusable = false;
			this.index = index;
			this.Label_Status_Small.Background.Opacity = 0;
			this.Border_CoverBorder.BorderBrush = null;
			this.Image_EpisodePlay.Opacity = 0;

			if (Settings.Default.TileLoadAnimation) {
				this.Opacity = 0;
				this.FadeIn();
			}

			// Set an empty context to prevent the one from the collection to be opened instead
			this.Image_Cover.ContextMenu = new ContextMenu();
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		new public void Focus()
		{
			this.Image_Cover.Focus();
		}

		public void RemoveEpisodesValuesOverlay()
		{
			this.Label_EpisodesValuesDescr.Opacity = 0;
			this.Label_EpisodesValues.Opacity = 0;
			this.Label_EpisodesValues.Background.Opacity = 0;
			this.Button_Continue.Opacity = 0;
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

		/// <summary>
		/// Reset the cover and set the default one.
		/// </summary>
		public void SetNoCover()
		{
			this.CoverUri = "pack://application:,,,/res/no.jpg";
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
		/// Update the row from an serie.
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
		/// Update the row from a season.
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

			this.SetEpisodesValues(season.EpisodesViewed, season.EpisodesOwned, season.EpisodesTotal);
		}

		/// <summary>
		/// Update the row from an episode.
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
			double fontSize = 50 / (length * 0.15);

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

		/// <summary>
		/// Set or remove the big overlay.
		/// </summary>
		private string BigOverlay
		{
			set
			{
				if (value != null) {
					this.Grid_CoverOverlay.Background = (Brush)new BrushConverter().ConvertFromString("#000000");
					this.Grid_CoverOverlay.Background.Opacity = 0.4;
					this.Label_Status_Big.Content = value;
					this.Label_Status_Big.Opacity = 0.7;

					// Value depending on the text's length
					this.Label_Status_Big.FontSize = this.GetFontSize(value.Length, 70);

					this.SmallOverlay = null;
				} else {
					this.Label_Status_Big.Content = null;
					this.Grid_CoverOverlay.Background = null;
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
				if (value != null) {
					this.Label_Status_Small.Content = value;
					this.Label_Status_Small.Opacity = 0.8;
					this.Label_Status_Small.Background.Opacity = 0.7;
					this.Label_Status_Small.FontSize = this.GetFontSize(value.Length, 30);

					this.BigOverlay = null;
				} else {
					this.Label_Status_Small.Content = null;
					this.Label_Status_Small.Background.Opacity = 0;
				}
			}
		}

		private Entity.DefaultStatus GetDefaultStatusFromString(string content)
		{
			return (Entity.DefaultStatus)Enum.Parse(typeof(Entity.DefaultStatus), content);
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

		public bool MarkItemAsWatched
		{
			set
			{
				this.Watched = value;
				this.watched = value;
				this.CreateEpisodeContextMenu();
			}
		}

		public Entity.DefaultStatus Status
		{
			get
			{
				if (this.Label_Status_Big.Content != null) {
					return this.GetDefaultStatusFromString(this.Label_Status_Big.Content.ToString());
				}

				return this.GetDefaultStatusFromString(this.Label_Status_Small.Content.ToString());
			}
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

		/// <summary>
		/// To know if the tile is being edited.
		/// Hardcoded false because editable field are not yet implemented for rows.
		/// </summary>
		public bool IsBeingEdited
		{
			get { return false; }
		}

		public string Title
		{
			get { return (string)this.Label_Title.Content; }
			set { this.Label_Title.Content = value; }
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

		public string Number
		{
			get { return this.Label_Number.Content.ToString(); }
			set
			{
				if (value != null) {
					this.Label_Number.Opacity = 0.8;
					this.Label_Number.Content = value;
				} else {
					this.Label_Number.Opacity = 0;
					this.Label_Number.Content = value;
				}
			}
		}

		public ushort NumberOfSeasons
		{
			set { this.Label_SeasonsCounter.Content = value.ToString() + " " + Lang.LowerFirst(Lang.Plural("season")); }
		}

		public ushort NumberOfEpisodes
		{
			set { this.Label_EpisodesCounter.Content = value.ToString() + " " + Lang.LowerFirst(Lang.Plural("episode")); }
		}

		public string EpisodesValues
		{
			get { return this.Label_EpisodesValues.Content.ToString(); }
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
			set
			{
				this.Border_CoverBorder.Width = value;
				this.Label_Title.Margin = new Thickness { Left = value };
			}
		}

		/// <summary>
		/// Set the height for the cover.
		/// </summary>
		public int CoverHeight
		{
			set
			{
				this.Grid_Root.Height = value;
			}
		}

		private string CoverUri
		{
			set
			{
				BitmapImage bitmap = new BitmapImage();

				// Specifying those options does not lock the file on disk (meaning it can be deleted or overwritten)
				bitmap.BeginInit();
				bitmap.UriSource = new Uri(value);
				bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.EndInit();

				this.Image_Cover.Source = bitmap;
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

		public string GetEpisodesValues
		{
			get { return this.Label_EpisodesValues.Content.ToString(); }
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

		private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			// Don't do forward when holding the control key
			if (Keyboard.IsKeyDown(Key.LeftCtrl)) {
				return;
			}

			if (DoubleClick != null) DoubleClick(this.Index, e);
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
			if (Selected != null) Selected(this.Index, e);

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
			|| ((Hieda.View.Window.MainWindow)System.Windows.Application.Current.MainWindow).SelectedCount > 0) {
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
				if (PlayClick != null) PlayClick(this.Index, e);
			}
		}

		private void cover_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			if (ContextMenuOpeningRequested != null) {
				ContextMenuOpeningRequested(this.Index, e);
			}
		}

		private void row_DragEnter(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

			// Don't go to the next level when dragging an image
			if (files == null || files.Length != 1) {
				return;
			}

			DispatcherTimer timer = new DispatcherTimer();

			timer.Tick += new EventHandler(DragTime);
			timer.Interval = TimeSpan.FromMilliseconds(500);
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

			if (DoubleClick != null) DoubleClick(this.Index, e);
		}

		/// <summary>
		/// Called when dropping a file onto the row.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Row_Drop(object sender, DragEventArgs e)
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
