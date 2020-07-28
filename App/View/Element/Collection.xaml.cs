using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Hieda.Properties;
using Hieda.Tool;
using Hieda.View.Element.List;
using Hieda.View.Window;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;
using Level = Hieda.Constants.Level;
using System.IO.Compression;

namespace Hieda.View.Element
{
	public partial class Collection : UserControl
	{
		public Controller.Collection _controller;
		public static Level level;

		private int selectedIndex = -1;
		private int currentSerieIndex;
		private int currentSeasonIndex;
		private DispatcherTimer timer;
		private bool statusSelectorEventEnabled = true;
		private bool tileDropped = false;
		private bool listIsSeasonalView = false; // use an accessor from this.List instead of declaring another variable?
		private bool isRowList = true;
		private int multiselectStart = 0;

		// Used to raise the LineChanged event only when doing to in the list, not when goinh up.
		private int currentLine = 0;
		private int currentLineNumber = 0;

		private int NumerOfDisplayedTiles = 0;

		// Index of selected items
		private List<int> selectedIndexes = new List<int>();

		// Events
		public event EventHandler ContextRequested;

		// Delegates: for calling UI functions in a threaded function.
		public delegate void PerformDelegate();
		public delegate void UpdateEpisodeThumbnailDelegate(Entity.Episode episode, int index);

		private BackgroundWorker backgroundWorker = null;
		private CancellationTokenSource cancel;

		public Collection()
		{
			InitializeComponent();

			this.Sidebar.Opacity = 0;
			this.CloseSidebar();
			this.Sidebar.CloseRequested += new EventHandler(this.Sidebar_CloseRequested);
			this.ContextRequested += new EventHandler(this.Collection_ContextRequested);
			this.label_Breadcrumb.Content = "";
			this.DisplayNothingMessage(false);
			this.textbox_Search.Text = this.textbox_Search.Placeholder = Lang.SEARCH_SERIES;
			this.Sidebar.Margin = new Thickness { Right = SystemParameters.PrimaryScreenWidth / 2 };
			this.PreviewMouseWheel += List_PreviewMouseWheel;

			// Add types
			foreach (int i in Enum.GetValues(typeof(Constants.Type))) {
				this.ComboBox_Types.Items.Add(new CheckBox() {
					Content   = Lang.Content(Enum.GetName(typeof(Constants.Type), i).ToLower()),
					IsChecked = true
				});
			}

			// Set the starting view mode
			if (Settings.Default.StartingLabeledList) {
				this.SetSeasonsViewMode();
			} else {
				this.SetSeriesViewMode();
			}

			this.TurnIntoTileList();
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Set the controller for this view.
		/// </summary>
		/// <param name="controller"></param>
		public void SetController(Controller.Collection controller)
		{
			this._controller = controller;
		}

		/// <summary>
		/// Update the series list.
		/// Used in a thread.
		/// </summary>
		/// <param name="series"></param>
		public void UpdateSeries(object series)
		{
			this.StopBackGroundWorker();
			this.RotateRefreshButton();

			List<Entity.Serie> seriesList = (List<Entity.Serie>)series;
			int count = seriesList.Count;

			// Clear view
			this.List.Clear();
			this.List.UseWideTile(false);
			this.List.IsLabeled = false;

			this.selectedIndexes.Clear();
			this.selectedIndex = -1;

			if (Settings.Default.WhileScrolling) {
				this.HandleScollLoading(ref count);
			}

			this.SetAddButtonEnabled(true);
			this.DisplayNothingMessage(count == 0);
			this.InitBackgroundWorker();

			this.backgroundWorker.DoWork += delegate (object sender, DoWorkEventArgs e) {
				BackgroundWorker worker = sender as BackgroundWorker;

				for (int i = 0; i < count; i++) {
					if (worker.CancellationPending == true) {
						e.Cancel = true;

						break;
					} else {
						Thread.Sleep(Settings.Default.LoadSpeed);

						// Create the item then report it as progress
						Dispatcher.Invoke((Action)(() => {
							worker.ReportProgress(i, this.List.CreateItemFromSerie((ushort)i, seriesList[i]));
						}));
					}
				}
			};

			this.backgroundWorker.ProgressChanged += delegate (object sender, ProgressChangedEventArgs e) {
				BackgroundWorker worker = sender as BackgroundWorker;

				if (worker.CancellationPending == true) {
					return;
				}

				// The tile is built in the DoWork function and we can then retrieve it here
				IItem item = (IItem)e.UserState;

				this.List.Add(item);
				this.AssignItemEvents(ref item);
			};

			this.AttachRunWorkerCompleted();

			this.backgroundWorker.RunWorkerAsync();
			this.SetBackgroundCover(null);
		}

		/// <summary>
		/// Update the seasons list.
		/// </summary>
		/// <param name="seasons"></param>
		public void UpdateSeasons(object seasons)
		{
			this.StopBackGroundWorker();
			this.RotateRefreshButton();

			List<Entity.Season> seasonsList = (List<Entity.Season>)seasons;
			int count = seasonsList.Count;

			// Clear view
			this.List.Clear();
			this.List.UseWideTile(false);
			this.selectedIndexes.Clear();
			this.selectedIndex = -1;

			this.List.IsLabeled = true;
			this.List.IsSeasonalView = this.listIsSeasonalView;

			if (Settings.Default.WhileScrolling) {
				this.HandleScollLoading(ref count);
			}

			this.SetAddButtonEnabled(!this.listIsSeasonalView);
			this.DisplayNothingMessage(count == 0);
			this.InitBackgroundWorker();

			this.backgroundWorker.DoWork += delegate (object sender, DoWorkEventArgs e) {
				BackgroundWorker worker = sender as BackgroundWorker;

				for (int i = 0; i < count; i++) {
					if (worker.CancellationPending == true) {
						e.Cancel = true;

						break;
					} else {
						Thread.Sleep(Settings.Default.LoadSpeed);

						// Create the item then report it as progress
						Dispatcher.Invoke((Action)(() => {
							worker.ReportProgress(i, this.List.CreateItemFromSeason((ushort)i, seasonsList[i], true));
						}));
					}
				}
			};

			this.backgroundWorker.ProgressChanged += delegate (object sender, ProgressChangedEventArgs e) {
				BackgroundWorker worker = sender as BackgroundWorker;

				if (worker.CancellationPending == true) {
					return;
				}

				// The tile is built in the DoWork function and we can then retrieve it here
				IItem item = (IItem)e.UserState;

				this.List.Add(item);
				this.AssignItemEvents(ref item);
			};

			this.AttachRunWorkerCompleted();

			this.backgroundWorker.RunWorkerAsync();

			// No current serie in the Label list as we're directly loading seasons
			if (!this.listIsSeasonalView) {
				this.SetBackgroundCover(_controller.CurrentSerie.Cover);
			} else {
				this.SetBackgroundCover(null);
			}
		}

		/// <summary>
		/// Update the episodes list.
		/// Called by Controler.Collection.DisplayEpisodes().
		/// </summary>
		/// <param name="episodes"></param>
		public void UpdateEpisodes(object episodes)
		{
			this.StopBackGroundWorker();
			this.RotateRefreshButton();

			List<Entity.Episode> episodesList = (List<Entity.Episode>)episodes;
			int count = episodesList.Count;
			Entity.Season currentSeason = _controller.CurrentSeason;

			// Handle fake episodes
			if (Settings.Default.FakeEpisode && currentSeason.EpisodesTotal > count) {
				count = currentSeason.EpisodesTotal;
				List<ushort> realNumbers = new List<ushort>();

				foreach (Entity.Episode ep in episodesList) {
					realNumbers.Add(ep.Number);
				}

				for (ushort i = 1; i <= count; i++) {
					if (realNumbers.Contains(i)) {
						continue;
					}

					Entity.Episode fake = new Entity.Episode();
					fake.Title = Lang.UpperFirst(Lang.EPISODE) + " " + i;
					fake.Number = i;
					fake.SeasonId = currentSeason.Id;
					fake.SerieId = _controller.CurrentSerie.Id;
					fake.Fake = true;

					if (currentSeason.StatusId == (int)Entity.DefaultStatus.Finished) {
						fake.Watched = true;
					}

					episodesList.Add(fake);
				}

				// Reorder the list
				_controller.CurrentEpisodes = episodesList.OrderBy(e => e.Number).ToList();
				count = episodesList.Count;
			}

			// Clear view
			this.List.Clear();
			this.List.UseWideTile(currentSeason.WideEpisode);
			this.selectedIndexes.Clear();
			this.selectedIndex = -1;
			this.List.IsLabeled = false;

			// Disable progressive display at episodes level for now as it isn't working well
			// Crashes when Tiles_LoadThumbsAfter is enabled
			// Probably because it tries to set them for all tiles even those that aren't displayed yet
			if (Settings.Default.WhileScrolling) {
				this.HandleScollLoading(ref count);
			}

			this.SetAddButtonEnabled(true);
			this.DisplayNothingMessage(count == 0);
			this.InitBackgroundWorker();

			this.backgroundWorker.DoWork += delegate (object sender, DoWorkEventArgs e) {
				BackgroundWorker worker = sender as BackgroundWorker;

				for (int i = 0; i < count; i++) {
					if (worker.CancellationPending == true) {
						e.Cancel = true;

						break;
					} else {
						Thread.Sleep(Settings.Default.LoadSpeed);

						// Create the item then report it as progress
						Dispatcher.Invoke((Action)(() => {
							worker.ReportProgress(i, this.List.CreateItemFromEpisode((ushort)i, episodesList[i]));
						}));
					}
				}
			};

			this.backgroundWorker.ProgressChanged += delegate (object sender, ProgressChangedEventArgs e) {
				BackgroundWorker worker = sender as BackgroundWorker;

				if (worker.CancellationPending == true) {
					return;
				}

				// The tile is built in the DoWork function and we can then retrieve it here
				IItem item = (IItem)e.UserState;

				this.List.Add(item);
				this.AssignItemEvents(ref item);
			};

			this.AttachRunWorkerCompleted();

			// If enabled, loads thumbs after episodes tiles are displayed
			if (Settings.Default.TileVideoThumbs) {
				this.backgroundWorker.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) {
					BackgroundWorker worker = sender as BackgroundWorker;

					if (worker.CancellationPending == true) {
						return;
					}

					this.UpdateEpisodesCovers();
				};
			}

			this.backgroundWorker.RunWorkerAsync();
			
			if (count > 0) {
				this.SetBackgroundCover(episodesList[0].ParentCover);
			}
		}

		/// <summary>
		/// This fires after all items in list are displayed to load their video thumbnails covers.
		///
		/// The system works, but there is a strange bug: instead of showing the video file thmbnail, it show the icon of the default
		/// program used to play it, as if the thumbnail were not loaded a this time.
		/// </summary>
		/// <param name="season"></param>
		public void UpdateEpisodesCovers()
		{
			int counter = -1;

			foreach (Entity.Episode episode in _controller.CurrentEpisodes) {
				counter++;

				// Already has a cover
				if (episode.Cover != null) {
					continue;
				}

				Dispatcher.BeginInvoke(
					(UpdateEpisodeThumbnailDelegate)this.UpdateEpisodeThumbnail,
					DispatcherPriority.Background,
					episode,
					counter
				);
			}
		}

		private void UpdateEpisodeThumbnail(Entity.Episode episode, int index)
		{
			Tool.File episodeFile = episode.File;

			if (episodeFile.IsCompatibleWithThumb) {
				System.Drawing.Bitmap bitmap = episode.Thumbnail;

				// Update the episode cover in database
				try {
					App.db.Update("episode", episode.Id, "cover", episode.Cover);
				} catch (Exception) { }

				// Update the view
				IItem item = this.GetItem(index);

				if (item != null) {
					item.SetCover(bitmap);
				}
			} else if (episodeFile.IsBook) {
				try {
					using (ZipArchive archive = ZipFile.OpenRead(episode.AbsoluteUri)) {
						if (archive.Entries.Count > 0) {
							IOrderedEnumerable<ZipArchiveEntry> entries = archive.Entries.OrderBy(entry => entry.Name, StringComparer.Ordinal);

							if (!System.IO.Directory.Exists("temp")) {
								System.IO.Directory.CreateDirectory("temp");
							}

							ZipArchiveEntry firstEnty = entries.First();
							string filepath = @"temp\" + firstEnty.Name;

							firstEnty.ExtractToFile(filepath, true);

							using (Tool.Cover coverTool = new Tool.Cover()) {
								episode.Cover = coverTool.Create(filepath, true, false);
							}

							// Update the view
							IItem item = this.GetItem(index);

							if (item != null) {
								item.SetCover(episode.Cover);
							}

							// Update the episode cover in database
							try {
								App.db.Update("episode", episode.Id, "cover", episode.Cover);
							} catch (Exception) { }

							// Cleanup
							System.IO.File.Delete(filepath);
						}
					}
				} catch (Exception) { }
			}
		}

		/// <summary>
		/// Calculate numbers of items to display when using the WhileScrolling option.
		/// </summary>
		/// <param name="count"></param>
		private void HandleScollLoading(ref int count)
		{
			this.NumerOfDisplayedTiles = 0;

			/*if (this.cancel != null) this.cancel.Cancel();

			this.cancel = new CancellationTokenSource();
			Tools.RunPeriodicAsync(DisplayMoreItems, TimeSpan.FromSeconds(0.05), cancel.Token);*/

			Application.Current.Dispatcher.BeginInvoke((Action)(() => {
				this.NumerOfDisplayedTiles = this.List.GetNumberOfItemsFittingInScreen + this.List.TilesPerLines;
			}));

			Dispatcher.Invoke((PerformDelegate)this.List.Clear);

			// NumerOfDisplayedTiles can't be bigger than the number of items
			// (happens when there's less items than what can fit in the screen)
			if (NumerOfDisplayedTiles > count) {
				NumerOfDisplayedTiles = count;
				if (this.cancel != null) this.cancel.Cancel();
			}

			count = NumerOfDisplayedTiles;
		}

		/// <summary>
		/// Add a serie, season or episode tile depending on the view level.
		/// The given index is used to get an entity from the model.
		/// </summary>
		/// <param name="index"></param>
		public void AddIndex(int index)
		{
			switch (this.Level) {
				case Level.Serie:
					this.Add(_controller.Model.Series[index]);
				break;
				case Level.Season:
					this.Add(_controller.Model.Seasons[index]);
				break;
				case Level.Episode:
					this.Add(_controller.Model.Episodes[index]);
				break;
			}
		}

		/// <summary>
		/// Add a serie entity to the list.
		/// </summary>
		/// <param name="serie"></param>
		public void Add(Entity.Serie serie)
		{
			IItem item = this.List.Add(serie);

			// Assign events to the last added tile.
			this.AssignItemEvents(ref item);
		}

		/// <summary>
		/// Add a season entity to the list.
		/// </summary>
		/// <param name="season"></param>
		/// <param name="parentCover"></param>
		public void Add(Entity.Season season)
		{
			IItem item = this.List.Add(season, true);

			// Assign events to the last added tile.
			this.AssignItemEvents(ref item);

			// Scroll to the new tile
			this.ScrollValue = item.Margin.Top - TileList.LABEL_TOP_MARGIN;
		}

		/// <summary>
		/// Add an episode entity to the list, and set its events.
		/// </summary>
		/// <param name="episode"></param>
		public void Add(Entity.Episode episode)
		{
			IItem item = this.List.Add(episode);

			// Assign events to the last added tile.
			this.AssignItemEvents(ref item);
		}

		/// <summary>
		/// Remove the currently selected tile.
		/// </summary>
		public void RemoveSelectedItem()
		{
			this.List.RemoveAt(this.selectedIndex);
		}

		/// <summary>
		/// Assign events to the last added tile in list.
		/// </summary>
		public void AssignItemEvents(ref IItem item)
		{
			item.Selected += new EventHandler(this.Selected);
			item.DoubleClick += new EventHandler(this.Tile_DoubleClick);
			item.PlayClick += new EventHandler(this.Tile_PlayClick);
			item.Edited += new EventHandler(this.Tile_Edited);
			item.ContextRequested += new EventHandler(this.Tile_ContextRequested);
			item.MiddleClick += new EventHandler(this.Tile_MiddleClick);
			item.ContextMenuOpeningRequested += new EventHandler(this.Tile_ContextMenuOpeningRequested);
			item.Dropped += new EventHandler(this.Tile_Dropped);
		}

		/// <summary>
		/// Update the collection element when the window is resized.
		/// Called by a resize event from MainWindow.
		/// </summary>
		/// <param name="containerWidth"></param>
		/// <param name="containerHeight"></param>
		public void ResizeUpdate()
		{
			// Resize list
			if (this.IsLoaded) {
				this.List.ResizeUpdate();
			}

			// Define the breadcrumb's max width
			int breadCrumbWidth = this.GetElementPositionX(this.label_Counter) - this.GetElementPositionX(this.label_Breadcrumb);

			if (breadCrumbWidth > 0) {
				this.label_Breadcrumb.Width = breadCrumbWidth;
			}

			// Cheap way of hidding that the sidebar isn't totaly closed when maximizing the window
			if (!this.Sidebar.IsOpen) {
				this.Sidebar.Opacity = 0;
			}
		}

		/// <summary>
		/// Apply a semi-transparent black overlay to darken the collection.
		/// </summary>
		/// <param name="toggle"></param>
		public void Blackout(bool toggle)
		{
			this.grid_ListBlackout.IsHitTestVisible = toggle;
			this.grid_ListBlackout.BeginAnimation(OpacityProperty, new DoubleAnimation(toggle ? 1 : 0, TimeSpan.FromSeconds(0.7)));
		}

		/// <summary>
		/// Set the status selector combo for the serie and season levels.
		/// </summary>
		/// <param name="selected"></param>
		public void SetSeriesAndSeasonsCombo(int selectedStatusId)
		{
			this.combobox_Selector.Items.Clear();

			this.AddStatusInCombo((int)Entity.DefaultStatus.All, Lang.ALL);
			this.AddStatusInCombo((int)Entity.DefaultStatus.None, Lang.NONE);
			this.AddStatusInCombo((int)Entity.DefaultStatus.ToWatch, Lang.TO_WATCH);
			this.AddStatusInCombo((int)Entity.DefaultStatus.Current, Lang.CURRENT);
			this.AddStatusInCombo((int)Entity.DefaultStatus.StandBy, Lang.STANDBY);
			this.AddStatusInCombo((int)Entity.DefaultStatus.Finished, Lang.FINISHED);
			this.AddStatusInCombo((int)Entity.DefaultStatus.Dropped, Lang.DROPPED);

			this.combobox_Selector.Items.Add(new Separator());

			if (_controller == null) {
				return;
			}

			// Add user status into the combobox
			foreach (Entity.UserStatus userStatus in _controller.Model.Status) {
				this.AddStatusInCombo(userStatus.Id, userStatus.Text);
			}

			this.SelectedStatusId = selectedStatusId;
		}

		/// <summary>
		/// Set the status selector combo for the episode level.
		/// </summary>
		/// <param name="selected"></param>
		public void SetEpisodesCombo(int selectedStatusId=(int)Entity.DefaultStatus.All)
		{
			this.combobox_Selector.Items.Clear();

			this.AddStatusInCombo((int)Entity.DefaultStatus.All, Lang.ALL);
			this.AddStatusInCombo((int)Entity.DefaultStatus.Watched, Lang.WATCHED);
			this.AddStatusInCombo((int)Entity.DefaultStatus.NotWatched, Lang.NOTWATCHED);

			this.SelectedStatusId = selectedStatusId;
		}

		/// <summary>
		/// Add a new status in the combobox.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="text"></param>
		private void AddStatusInCombo(int id, string text)
		{
			this.combobox_Selector.Items.Add(new ComboBoxItem() { Tag=id, Content=text });
		}

		/// <summary>
		/// Just makes the refresh button rotate.
		/// </summary>
		public void RotateRefreshButton()
		{
			RotateTransform rt = new RotateTransform();

			this.button_Refresh.RenderTransform = rt;
			this.button_Refresh.RenderTransformOrigin = new Point(0.5, 0.5);

			DoubleAnimation da = new DoubleAnimation(-360, 0, new Duration(TimeSpan.FromSeconds(0.7)));
			da.RepeatBehavior = RepeatBehavior.Forever;

			rt.BeginAnimation(RotateTransform.AngleProperty, da);
		}

		/// <summary>
		/// Turn the list into a TileList.
		/// </summary>
		private void TurnIntoTileList()
		{
			this.List = new TileList();
			this.ScrollValue = 0;
			this.Breadcrumb = null;
			this.isRowList = false;
			this.SetBackgroundCover(null);

			// We need to wait until the list is fully loaded before place items into
			// it otherwise we may encounter 0 Width values causing divisions by zero
			this.List.OnLoaded += new EventHandler(this.List_Loaded);
		}

		/// <summary>
		/// Turn the list into a RowList.
		/// </summary>
		private void TurnIntoRowList()
		{
			this.List = new RowList();
			this.ScrollValue = 0;
			this.isRowList = true;

			// We need to wait until the list is fully loaded before place items into
			// it otherwise we may encounter 0 Width values causing divisions by zero
			this.List.OnLoaded += new EventHandler(this.List_Loaded);
		}

		/// <summary>
		/// Switch between the tile and row lists.
		/// </summary>
		public void SwitchList()
		{
			if (this.isRowList) {
				this.TurnIntoTileList();
			} else {
				this.TurnIntoRowList();
			}
		}

		/// <summary>
		/// Focus the first element in list starting with the given char.
		/// </summary>
		/// <param name="c"></param>
		public void FocusItemStartingWith(string c)
		{
			if (this.Sidebar.IsOpen || !this.HasAnItemSelected() || this.SelectedItem.IsBeingEdited) {
				return;
			}

			int index = this.List.FocusItemStartingWith(c);

			if (index >= 0) {
				this.SelectedIndex = index;
			}
		}

		/// <summary>
		/// Open the left sidebar.
		/// </summary>
		public void OpenSidebar()
		{
			this.textbox_Search.IsEnabled = false;
			this.combobox_Selector.IsEnabled = false;
			this.Sidebar.Opacity = 1;
			this.Sidebar.IsOpen = true;
			this.Sidebar.IsHitTestVisible = true;
			this.Sidebar.Studio = null;

			if (this.SelectedItem != null) {
				this.Sidebar.Title = this.SelectedItem.Title;
				this.Sidebar.CoverSource = this.SelectedItem.CoverSource;
				this.Sidebar.EpisodesValues = this.SelectedItem.EpisodesValues;
			}

			if (this.Level == Level.Serie) {
				this.Sidebar.SetSerieAndSeasonsCombo();
				this.Sidebar.Status = (Entity.DefaultStatus)_controller.SelectedSerie.StatusId;
				this.Sidebar.Synopsis = _controller.SelectedSerie.Synopsis;
				this.Sidebar.Genres = Repository.Genre.Instance.GetAllBySerie(_controller.SelectedSerie.Id);
			} else if (this.Level == Level.Season) {
				this.Sidebar.SetSerieAndSeasonsCombo();
				this.Sidebar.Synopsis = _controller.SelectedSeason.Synopsis;
				this.Sidebar.Status = (Entity.DefaultStatus)_controller.SelectedSeason.StatusId;
				this.Sidebar.Genres = Repository.Genre.Instance.GetAllBySerie(_controller.SelectedSeason.SerieId);
				this.Sidebar.Premiered = _controller.SelectedSeason.Premiered;

				Entity.Studio studio = Repository.Studio.Instance.Find(_controller.SelectedSeason.StudioId);

				if (studio != null && !String.IsNullOrEmpty(studio.Name)) {
					this.Sidebar.Studio = studio.Name;
				} else {
					this.Sidebar.Studio = Lang.UNKNOWN;
				}
			} else {
				this.Sidebar.IsEpisode = true;
				this.Sidebar.SetEpisodesCombo();

				if (_controller.SelectedEpisode.Watched) {
					this.Sidebar.Status = Entity.DefaultStatus.Watched;
				} else {
					this.Sidebar.Status = Entity.DefaultStatus.NotWatched;
				}

				this.Sidebar.Synopsis = _controller.CurrentSeason.Synopsis;
				this.Sidebar.Genres = Repository.Genre.Instance.GetAllBySerie(_controller.SelectedEpisode.SerieId);
				this.Sidebar.Premiered = _controller.CurrentSeason.Premiered;

				Entity.Studio studio = Repository.Studio.Instance.Find(_controller.CurrentSeason.StudioId);

				if (studio != null && !String.IsNullOrEmpty(studio.Name)) {
					this.Sidebar.Studio = studio.Name;
				} else {
					this.Sidebar.Studio = Lang.UNKNOWN;
				}
			}

			this.Sidebar.SetEpisode(this.Level == Level.Episode ? _controller.SelectedEpisode : null);

			this.Sidebar.Type = this.Level;

			this.Blackout(true);

			(this.Sidebar.RenderTransform = new TranslateTransform(-this.Sidebar.ActualWidth, 0)).BeginAnimation(
				TranslateTransform.XProperty,
				new DoubleAnimation(0, new Duration(TimeSpan.FromSeconds(this.SidebarAnimationTime)))
			);
		}

		/// <summary>
		/// Create the context menu for the collection, will appear by right-clicking the list outide of items.
		/// </summary>
		public void CreateCollectionContextMenu()
		{
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			// Select all
			item = new MenuItem();
			item.Header = Lang.Text("selectAll");
			item.Tag = "selectAll";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			// Deselect all
			item = new MenuItem();
			item.Header = Lang.Text("deselectAll");
			item.Tag = "deselectAll";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			context.Items.Add(new Separator());

			// Row list
			item = new MenuItem();
			item.Header = Lang.Text("switchList");
			item.Tag = "switchList";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			this.ScrollViewer.ContextMenu = context;
		}

		/// <summary>
		/// Add a context menu to the breadcrumb.
		/// </summary>
		public void SetBreadcrumbContextMenu()
		{
			// No need for a context menu at series level since the breadcrumb is empty
			if (this.Level == Level.Serie) {
				this.label_Breadcrumb.ContextMenu = null;

				return;
			}

			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			// Copy serie title
			item.Header = Lang.COPY_SERIE_TITLE;
			item.Tag = "copySerieTitle";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			// Copy season title
			if (this.Level == Level.Episode) {
				// Calculate theses episodes's relative path
				item = new MenuItem();
				item.Header = Lang.COPY_SEASON_TITLE;
				item.Tag = "copySeasonTitle";
				item.Click += this.ContextMenu_MenuItem_Click;
				context.Items.Add(item);
			}

			this.label_Breadcrumb.ContextMenu = context;
		}

		/// <summary>
		/// Close the left sidebar.
		/// </summary>
		public void CloseSidebar()
		{
			this.textbox_Search.IsEnabled = true;
			this.combobox_Selector.IsEnabled = true;
			this.Sidebar.IsOpen = false;
			this.Sidebar.IsHitTestVisible = false;

			this.Blackout(false);

			DoubleAnimation doubleAnimation = new DoubleAnimation(0, -this.Sidebar.ActualWidth, TimeSpan.FromSeconds(this.SidebarAnimationTime));

			// Ensure it will remain hidden
			/*doubleAnimation.Completed += (sender, e) => { this.Sidebar.Opacity = 0; };*/

			// The wider the collection the longer the duration
			(this.Sidebar.RenderTransform = new TranslateTransform()).BeginAnimation(TranslateTransform.XProperty, doubleAnimation);
		}

		/// <summary>
		/// Show or hide the "nothing" message in the middle of the collection.
		/// </summary>
		/// <param name="b"></param>
		public void DisplayNothingMessage(bool b)
		{
			this.label_Nothing.Content = (b) ? Lang.NOTHING : null;
		}

		/// <summary>
		/// Select all the tiles in the list.
		/// </summary>
		public void SelectAll()
		{
			this.SelectBetween(0, this.Count);
		}

		/// <summary>
		/// Unselect all items.
		/// </summary>
		public void DeselectAll()
		{
			foreach (int index in this.SelectedIndexes) {
				this.GetItem(index).Unselect();
			}

			this.SelectedIndexes.Clear();
		}

		/// <summary>
		/// Get an item at a certain index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IItem GetItem(int index)
		{
			return this.List.GetItem(index);
		}

		/// <summary>
		/// Get an element at a certain index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public UIElement GetElement(int index)
		{
			return this.List.GetElement(index);
		}

		/// <summary>
		/// Check if we have a selected item. If false, this.SelectedItem will be null.
		/// </summary>
		/// <returns></returns>
		public bool HasAnItemSelected()
		{
			return !(this.selectedIndex < 0 || this.selectedIndex > this.List.Count);
		}

		/// <summary>
		/// Scroll the list to the bottom.
		/// </summary>
		public void ScrollToBottom()
		{
			// Those two lines do the same thing
			//this.ScrollValue = this.grid_Content.ActualHeight;
			this.ScrollViewer.ScrollToEnd();
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Get the X position of a given UIElement instance.
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		private int GetElementPositionX(UIElement element)
		{
			Point position = element.PointToScreen(new Point(0d, 0d));
			Point controlPosition = this.PointToScreen(new Point(0d, 0d));

			return (Int32)(position.X -= controlPosition.X);
		}

		/// <summary>
		/// Add more items to the list while scrolling.
		/// </summary>
		private void DisplayMoreItems()
		{
			int totalItemsToDisplay = _controller.Model.Series.Count;
			byte numberOfItemsToAdd = this.List.TilesPerLines;

			if (totalItemsToDisplay > (NumerOfDisplayedTiles + numberOfItemsToAdd)) {
				for (int i = 0; i < numberOfItemsToAdd; i++) {
					this.AddIndex(i + NumerOfDisplayedTiles);
				}

				this.NumerOfDisplayedTiles += numberOfItemsToAdd;
			} else if (NumerOfDisplayedTiles < totalItemsToDisplay) {
				int leftToDisplay = (totalItemsToDisplay - NumerOfDisplayedTiles);

				for (int i = 0; i < leftToDisplay; i++) {
					this.AddIndex(i + NumerOfDisplayedTiles);
				}

				NumerOfDisplayedTiles += leftToDisplay;
			}

			// End reached
			if (NumerOfDisplayedTiles >= totalItemsToDisplay && this.cancel != null) {
				this.cancel.Cancel();
			}
		}

		/// <summary>
		/// Set the status selector options if needed.
		/// </summary>
		private void PrepareForSerieLevelIfNeeded()
		{
			if (this.Level != Level.Serie) {
				this.Level = Level.Serie;
				this.Breadcrumb = null;

				this.SetSeriesAndSeasonsCombo(_controller.SerieSelectedStatusId);
			}
		}

		/// <summary>
		/// Initialize the BackgroundWorker used for loading items.
		/// </summary>
		/// <param name="count"></param>
		private void InitBackgroundWorker()
		{
			this.backgroundWorker = new BackgroundWorker();

			this.backgroundWorker.WorkerReportsProgress = true;
			this.backgroundWorker.WorkerSupportsCancellation = true;
		}

		/// <summary>
		/// Stop the BackgroundWorker, must be done before starting a new one.
		/// </summary>
		private void StopBackGroundWorker()
		{
			if (this.backgroundWorker != null) {
				this.backgroundWorker.CancelAsync();
				this.backgroundWorker.Dispose();
			}
		}

		/// <summary>
		/// Set a cover image as the collection background.
		/// </summary>
		/// <param name="cover"></param>
		/// <param name="mode"></param>
		private void SetBackgroundCover(string cover, string mode="full")
		{
			if (!Settings.Default.CollectionBg || cover == null) {
				this.BackgroundImage.Source = null;

				return;
			}

			try {
				this.BackgroundImage.Source = new BitmapImage(new Uri(Path.CoverFolder + "\\" + mode + "\\" + cover));
			} catch { }
		}

		/// <summary>
		/// Execute a serie or season search.
		/// </summary>
		private void DoSearch()
		{
			if (this.listIsSeasonalView) {
				_controller.LoadSeasons();
				_controller.DisplaySeasons();
			} else {
				this.PrepareForSerieLevelIfNeeded();

				_controller.LoadSeries();
				_controller.DisplaySeries();
			}

			// Build search breadcrumb
			string searchQuery = this.SearchQuery;

			if (searchQuery != null) {
				this.Breadcrumb = Lang.Content(this.SearchField) + ": \"" + searchQuery + "\"";
			} else {
				this.Breadcrumb = null;
			}
		}

		/// <summary>
		/// Attach the RunWorkerCompleted event to the background worker.
		/// </summary>
		private void AttachRunWorkerCompleted()
		{
			this.backgroundWorker.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) {
				if (this.backgroundWorker.IsBusy) {
					return;
				}

				Transform rt = this.button_Refresh.RenderTransform;

				// Stop the refresh button's animation
				if (rt != null && !rt.IsSealed) {
					rt.BeginAnimation(RotateTransform.AngleProperty, null);
				}
			};
		}

		/// <summary>
		/// Prepare to use the series view mode.
		/// </summary>
		private void SetSeriesViewMode()
		{
			this.Level = Level.Serie;
			this.listIsSeasonalView = false;
		}

		/// <summary>
		/// Prepare to use the seasonal view mode.
		/// </summary>
		private void SetSeasonsViewMode()
		{
			this.Level = Level.Season;
			this.listIsSeasonalView = true;
		}

		private void SetAddButtonEnabled(bool enabled)
		{
			this.button_Add.IsEnabled = enabled;
			this.button_Add.Opacity = (enabled ? 1 : 0.5);
		}

		/// <summary>
		/// Select all items between start and end indexes.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		private void SelectBetween(int start, int end)
		{
			for (int i = start; i < end; i++) {
				this.AddIndexToSelection(i);
			}
		}

		/// <summary>
		/// Same as SelectBetween() but select items from a greater index to a lower index.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		private void ReverseSelectBetween(int start, int end)
		{
			for (int i = start; i > end; i--) {
				this.AddIndexToSelection(i);
			}
		}

		/// <summary>
		/// Add an index to the list of selected items.
		/// </summary>
		/// <param name="index"></param>
		private void AddIndexToSelection(int index)
		{
			this.GetItem(index).Select();

			// Only add indexes that aren't already selected
			if (!this.selectedIndexes.Contains(index)) {
				this.selectedIndexes.Add(index);
			}
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		/// <summary>
		/// Get or set the value of the series/seasons/episodes counter.
		/// </summary>
		public int LabelCounter
		{
			set { this.label_Counter.Content = value.ToString(); }
			get { return Int32.Parse(this.label_Counter.Content.ToString()); }
		}

		/// <summary>
		/// Set the text of the label used to display the current view level (serie, season or episode).
		/// </summary>
		public string LabelLevel
		{
			set { this.label_Level.Content = value; }
		}

		public int SelectedIndex
		{
			get { return this.selectedIndex; }
			set
			{
				this.selectedIndex = value;

				if (value != -1) {
					this.List.GetItem(value).Focus();
				}
			}
		}

		public IItem SelectedItem
		{
			get
			{
				if (!this.HasAnItemSelected()) {
					return null;
				}

				return this.List.GetItem(this.selectedIndex);
			}
		}

		public Level Level
		{
			get { return level; }
			set { level = value; }
		}

		public static Level ItemLevel
		{
			get { return level; }
		}

		public UIElementCollection Elements
		{
			get { return this.List.Elements; }
		}

		public ushort Count
		{
			get { return this.List.Count; }
		}

		public List<Entity.Serie> Series
		{
			get { return _controller.Model.Series; }
		}

		public int CurrentSerieIndex
		{
			get { return this.currentSerieIndex; }
			set { this.currentSerieIndex = value; }
		}

		public int CurrentSeasonIndex
		{
			set { this.currentSeasonIndex = value; }
			get { return this.currentSeasonIndex; }
		}

		public int SelectedStatusId
		{
			get
			{
				if (this.combobox_Selector.SelectedItem == null) {
					return (int)Entity.DefaultStatus.All;
				}

				return int.Parse(((ComboBoxItem)this.combobox_Selector.SelectedItem).Tag.ToString());
			}
			set
			{
				List<ComboBoxItem> items = new List<ComboBoxItem>();

				foreach (object item in this.combobox_Selector.Items) {
					if (item is ComboBoxItem) {
						items.Add((ComboBoxItem)item);
					}
				}

				this.combobox_Selector.SelectedItem = items.Find(i => int.Parse(i.Tag.ToString()) == value);
			}
		}

		public string Breadcrumb
		{
			get { return this.label_Breadcrumb.Content.ToString(); }
			set { this.label_Breadcrumb.Content = value; }
		}

		public IList List
		{
			get { return (IList)this.ScrollViewer.Content; }
			set { this.ScrollViewer.Content = value; }
		}

		/// <summary>
		/// Get access to the Main window.
		/// </summary>
		public MainWindow MainWindow
		{
			get { return ((MainWindow)System.Windows.Application.Current.MainWindow); }
		}

		public int CurrentLine
		{
			get { return this.currentLine; }
			set { this.currentLine = value; }
		}

		/// <summary>
		/// Get text in the Search textbox.
		/// </summary>
		public string SearchQuery
		{
			get
			{
				if (!String.IsNullOrWhiteSpace(this.textbox_Search.ActualText)) {
					return this.textbox_Search.Text.Trim();
				}

				return null;
			}
		}

		/// <summary>
		/// Get the selected search mode from the search combobox.
		/// </summary>
		public string SearchMode
		{
			get
			{
				RadioButton radio = (RadioButton)this.ComboBox_SearchOptions.Items[0];

				if ((bool)radio.IsChecked) {
					return "contains";
				}

				return "startsWith";
			}
		}

		/// <summary>
		/// Get the selected search field from the search combobox.
		/// </summary>
		public string SearchField
		{
			get
			{
				RadioButton radio = (RadioButton)this.ComboBox_SearchOptions.Items[3];

				if ((bool)radio.IsChecked) {
					return "title";
				}

				return "studio";
			}
		}

		public bool IsEmpty
		{
			get { return this.Count == 0; }
		}

		public List<int> SelectedIndexes
		{
			get { return this.selectedIndexes; }
		}

		public double ScrollValue
		{
			get { return this.ScrollViewer.VerticalOffset; }
			set { this.ScrollViewer.ScrollToVerticalOffset(value); }
		}

		/// <summary>
		/// Check which types from the combobox are checked.
		/// </summary>
		public string CheckedTypes
		{
			get
			{
				string query = "(";
				int count = this.ComboBox_Types.Items.Count;
				int added = 0;

				for (byte i = 0; i < count; i++) {
					if (!(bool)((CheckBox)this.ComboBox_Types.Items[i]).IsChecked) {
						continue;
					}

					if (added > 0) {
						query += ",";
					}

					query += i;
					added++;
				}

				// All the checkbox are checked, there's no need for a query
				if (added == count) {
					return null;
				}

				return query + ")";
			}
		}

		/// <summary>
		/// To know if the list use labels.
		/// Right know labels are only usef for the Season view level.
		/// </summary>
		public bool ListIsLabeled
		{
			get { return level == Level.Season; }
		}

		public bool ListIsSeasonalView
		{
			get { return this.listIsSeasonalView; }
		}

		/// <summary>
		/// Used to enable or disable the status selector's SelectionChanged event.
		/// </summary>
		public bool StatusSelectorEventEnabled
		{
			set { this.statusSelectorEventEnabled = value; }
		}

		/// <summary>
		/// How long the open or close animation will be.
		/// </summary>
		private double SidebarAnimationTime
		{
			get { return 0.7 * (this.Sidebar.ActualWidth / 1000); }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called when an item is clicked in the list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Selected(object sender, EventArgs e)
		{
			this.selectedIndex = (ushort)sender;
			IItem selectedItem = this.SelectedItem;

			if (selectedItem == null) {
				return;
			}

			// We clicked on an unselected item
			if (!selectedItem.IsSelected) {
				// The control key wasn't hold, unselect all the other items
				if (!Keyboard.IsKeyDown(Key.LeftCtrl)) {
					this.DeselectAll();
				}

				// Add this item to the selection
				this.selectedIndexes.Add(this.selectedIndex);
			} else if (Keyboard.IsKeyDown(Key.LeftCtrl)) {
				// The control key is hold on a selected item, remove it from the selection
				this.selectedIndexes.Remove(this.selectedIndex);
			}

			// Select all items between this.multiselectStart and this.selectedIndex
			if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) {
				if (this.multiselectStart < this.selectedIndex) {
					this.SelectBetween(this.multiselectStart, this.selectedIndex);
				} else {
					this.ReverseSelectBetween(this.multiselectStart, this.selectedIndex);
				}
			} else {
				this.multiselectStart = this.selectedIndex;
			}
		}

		/// <summary>
		/// Called when the context menu was triggered on an item.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tile_ContextRequested(object sender, EventArgs e)
		{
			_controller.ItemContext(sender.ToString());
		}

		/// <summary>
		/// Collection's context menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Collection_ContextRequested(object sender, EventArgs e)
		{
			_controller.CollectionContext(sender.ToString());
		}

		/// <summary>
		/// Called by changing the selected item in the selector combo box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Use MouseOver to prevent this to be called when combo's content is modified by code.
			if (!this.combobox_Selector.IsLoaded
				|| !this.statusSelectorEventEnabled
				|| this.combobox_Selector.SelectedItem == null) {
				return;
			}

			switch (this.Level) {
				case Level.Serie: {
					_controller.LoadSeries();
					_controller.DisplaySeries();
				}
				break;
				case Level.Season: {
					_controller.LoadSeasons();
					_controller.DisplaySeasons();
				}
				break;
				case Level.Episode: {
					_controller.LoadEpisodes();
					_controller.DisplayEpisodes();
				}
				break;
			}

			if (Settings.Default.RememberFilter) {
				ComboBoxItem selectedItem = (ComboBoxItem)this.combobox_Selector.SelectedItem;
				Settings.Default.LastSelector = short.Parse(selectedItem.Tag.ToString());
			}
		}

		/// <summary>
		/// Called by left-clicking the left arrow icon.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Back_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			_controller.Back();
		}

		/// <summary>
		/// Called by hovering the left arrow icon.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Back_MouseEnter(object sender, MouseEventArgs e)
		{
			this.button_Back.Opacity = 0.75;
		}

		/// <summary>
		/// Called by moving the cursor away from the left arrow icon.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Back_MouseLeave(object sender, MouseEventArgs e)
		{
			this.button_Back.Opacity = 1;
		}

		/// <summary>
		/// Called by left-clicking the '+' button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Add_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// Used to detect a long press on the button
			if (this.timer != null) this.timer.Stop();

			_controller.Add();
		}

		/// <summary>
		/// Called by hovering the '+' button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Add_MouseEnter(object sender, MouseEventArgs e)
		{
			if (this.button_Add.IsEnabled) {
				this.button_Add.Opacity = 0.75;
			}
		}

		/// <summary>
		/// Called by moving away from the '+' button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Add_MouseLeave(object sender, MouseEventArgs e)
		{
			if (this.button_Add.IsEnabled) {
				this.button_Add.Opacity = 1;
			}
		}

		/// <summary>
		/// Called by left-clicking the refresh button, refresh the list or cancel if already running.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Image_Refresh_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (this.backgroundWorker.IsBusy) {
				this.StopBackGroundWorker();
			} else {
				_controller.Refresh();
			}
		}

		/// <summary>
		/// Called by hovering the refresh button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Refresh_MouseEnter(object sender, MouseEventArgs e)
		{
			this.button_Refresh.Opacity = 0.75;
		}

		/// <summary>
		/// Called by moving away from the refresh button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Refresh_MouseLeave(object sender, MouseEventArgs e)
		{
			this.button_Refresh.Opacity = 1;
		}

		/// <summary>
		/// Get a brush from an hexadecimal color.
		/// </summary>
		/// <param name="hexa">
		/// Example: "#000000"
		/// </param>
		/// <returns></returns>
		private Brush BrushFromString(string hexa)
		{
			return (Brush)new BrushConverter().ConvertFromString(hexa);
		}

		/// <summary>
		/// Go to the next level by double clicking on an item.
		/// Series -> Seasons -> Episodes
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tile_DoubleClick(object sender, EventArgs e)
		{
			_controller.Forward((ushort)sender);
		}

		/// <summary>
		/// Called by clicking on the "play" icon on an item in the collection.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tile_PlayClick(object sender, EventArgs e)
		{
			this.selectedIndex = (ushort)sender;

			_controller.Continue();
		}

		/// <summary>
		/// Called when the title field under an item in the collection is unfocused.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tile_Edited(object sender, EventArgs e)
		{
			this.selectedIndex = (ushort)sender;
			IItem selectedItem = this.GetItem(this.selectedIndex);

			// Set correct title on tile
			this.GetItem(this.selectedIndex).Title = selectedItem.Title;

			switch (this.Level) {
				case Level.Serie: {
					if (_controller.SelectedSerie.Title != selectedItem.Title) {
						_controller.SelectedSerie.Title = selectedItem.Title;
						_controller.Model.SerieRepository.Update(_controller.SelectedSerie, "Title");
					}
				} break;
				case Level.Season: {
					ushort number = ushort.Parse(selectedItem.Number);

					if (_controller.SelectedSeason.Title != selectedItem.Title) {
						_controller.SelectedSeason.Title = selectedItem.Title;
						_controller.Model.SeasonRepository.Update(_controller.SelectedSeason, "Title");
					}

					if (_controller.SelectedSeason.Number != number) {
						_controller.SelectedSeason.Number = number;
						_controller.Model.SeasonRepository.Update(_controller.SelectedSeason, "Number");
					}
				} break;
				case Level.Episode: {
					_controller.SelectedEpisode.Title = selectedItem.Title;
					_controller.SelectedEpisode.Number = ushort.Parse(selectedItem.Number);

					_controller.UpdateSelectedEpisode();
				} break;
			}
		}

		/// <summary>
		/// Called when a file is dropped onto an item.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tile_Dropped(object sender, EventArgs e)
		{
			// Prevent from also calling ScrollViewer_Drop
			this.tileDropped = true;

			// pair.Key: item index, pair.Value: file path
			KeyValuePair<int, string> pair = (KeyValuePair<int, string>)sender;

			if (!System.IO.File.Exists(pair.Value)) {
				return;
			}

			// Check the file's type
			string extension = Tool.Path.Extension(pair.Value);

			// Video or comicbook, set episode file
			if (this.Level == Level.Episode && (Constants.VideoFileExtensions.Contains(extension) || Constants.BookFileExtensions.Contains(extension))) {
				// We don't want this behaviour, just import the dropped files
				if (!Settings.Default.ReplaceFileByDrop) {
					this.ScrollViewer_Drop(sender, (DragEventArgs)e);

					return;
				}

				// If the episode already have a file, ask if we realy want to replace it
				if (System.IO.File.Exists(_controller.Model.Episodes[pair.Key].Uri)) {
					TwoButtonsDialog dlg = new TwoButtonsDialog(
						"Replace this episode's file with the one dragged over?",
						"Attention", Lang.YES, Lang.NO
					);
					dlg.Owner = App.Current.MainWindow;

					if (!dlg.Open()) {
						return;
					}
				}

				// Update in model and database
				_controller.Model.Episodes[pair.Key].Uri = pair.Value;
				_controller.Model.EpisodeRepository.UpdateField(_controller.Model.Episodes[pair.Key].Id, "uri", pair.Value);

				// Update the video thumbnail
				IItem item = this.GetItem(pair.Key);

				if (item != null) {
					item.SetCover(_controller.Model.Episodes[pair.Key].Thumbnail);
					item.NoLinkedFile = false;
				}
				
				this.MainWindow.Notify(
					Constants.Notify.Notif,
					Lang.OPERATION_FINISHED_TITLE,
					String.Format(Lang.Content("linkedFileReplaced"), _controller.Model.Episodes[pair.Key].Title)
				);

				return;
			}

			// Image, set as cover
			if (Constants.ImageFilesExtensions.Contains(extension)) {
				string coverFilename = null;
				string title = null;

				using (Tool.Cover coverTool = new Tool.Cover()) {
					coverFilename = coverTool.Create(pair.Value, false);
				}

				// Set item cover
				this.GetItem(pair.Key).SetCover(coverFilename);

				// Save in db
				switch (this.Level) {
					case Level.Serie: { // Series
						_controller.Model.Series[pair.Key].Cover = coverFilename; // We need that for the parent cover
						_controller.Model.SerieRepository.UpdateField(_controller.Model.Series[pair.Key].Id, "cover", coverFilename);
						title = _controller.Model.Series[pair.Key].Title;
					}
					break;
					case Level.Season: { // Seasons
						_controller.Model.Seasons[pair.Key].Cover = coverFilename; // We need that for the parent cover
						_controller.Model.SeasonRepository.UpdateField(_controller.Model.Seasons[pair.Key].Id, "cover", coverFilename);
						title = _controller.Model.Seasons[pair.Key].Title;
					}
					break;
					case Level.Episode: // Episodes
						_controller.Model.EpisodeRepository.UpdateField(_controller.Model.Episodes[pair.Key].Id, "cover", coverFilename);
					title = _controller.Model.Episodes[pair.Key].Title;
					break;
				}
				
				this.MainWindow.Notify(
					Constants.Notify.Notif,
					Lang.OPERATION_FINISHED_TITLE,
					String.Format(Lang.Content("coverFileReplaced"), title)
				);
			}
		}

		/// <summary>
		/// Called by drag and droping a file onto the ScrollViewer.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ScrollViewer_Drop(object sender, DragEventArgs e)
		{
			// Call to ScrollViewer_Drop was prevented by Tile_Dropped, reset
			if (this.tileDropped) {
				this.tileDropped = false;

				return;
			}

			// Episode drop only at View level
			if (this.Level != Level.Episode) {
				return;
			}

			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

			if (files == null) {
				return;
			}

			// List with only the accepted file formats.
			List<string> importables = new List<string>();

			for (int i = 0; i < files.Length; i++) {
				if (Path.IsCorrespondingToFilter(files[i], Constants.VideoFileExtensions) || Path.IsCorrespondingToFilter(files[i], Constants.BookFileExtensions)) {
					importables.Add(files[i]);
				}
			}

			// There were no videos which corresponded to filters
			if (importables.Count == 0) {
				return;
			}

			// Sort in natural order
			importables.Sort();

			this.Blackout(true);

			EpisodeImport import = new EpisodeImport(
				importables,
				this.Series,
				_controller.LastEpisodeNumber,
				Lang.UpperFirst(_controller.CurrentSeasonIsManga ? Lang.CHAPTER : Lang.EPISODE),
				_controller.CurrentSerie.Title,
				_controller.CurrentSeason.Title
			);

			import.Owner = App.Current.MainWindow;
			import.ShowDialog();

			if (import.Result) {
				_controller.AddEpisodes(import);
			}

			this.Blackout(false);
		}

		/// <summary>
		/// Called by dragging files over the collection.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ScrollViewer_DragEnter(object sender, DragEventArgs e)
		{
			// Episode drop only at View level
			if (this.Level != Level.Episode) {
				return;
			}

			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				e.Effects = DragDropEffects.All;
			} else {
				e.Effects = DragDropEffects.None;
			}
		}

		/// <summary>
		/// Called by middle-clicking an item in the collection, opens the left sidebar.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tile_MiddleClick(object sender, EventArgs e)
		{
			this.OpenSidebar();
		}

		/// <summary>
		/// Event raised when right-clicking an item. Create the associated context menu then open it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tile_ContextMenuOpeningRequested(object sender, EventArgs e)
		{
			if (!this.HasAnItemSelected()) {
				return;
			}

			if (this.selectedIndexes.Count <= 1) {
				switch (this.Level) {
					case Level.Serie:
						this.SelectedItem.CreateSerieAndSeasonContextMenu();
					break;
					case Level.Season:
						this.SelectedItem.CreateSerieAndSeasonContextMenu();
					break;
					case Level.Episode:
						this.SelectedItem.CreateEpisodeContextMenu();
					break;
				}
			} else {
				switch (this.Level) {
					case Level.Serie:
						this.SelectedItem.CreateMultipleSeriesAndSeasonsContextMenu();
					break;
					case Level.Season:
						this.SelectedItem.CreateMultipleSeriesAndSeasonsContextMenu();
					break;
					case Level.Episode:
						this.SelectedItem.CreateMultipleEpisodesContextMenu();
					break;
				}
			}

			this.SelectedItem.OpenContext();
		}

		/// <summary>
		/// Called when the sidebar is closed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Sidebar_CloseRequested(object sender, EventArgs e)
		{
			this.CloseSidebar();

			// Save values when closed by hitting the "Save" button.
			if (((Button)sender).Name != "button_Close") {
				return;
			}

			string title = this.Sidebar.Title;
			string cover = this.Sidebar.CoverFromString.Replace("file:///", "");
			Entity.DefaultStatus status = this.Sidebar.Status;
			string synopsis = this.Sidebar.Synopsis;

			Thread t = new Thread(() => {
				switch (this.Level) {
					case Level.Serie: {
						Entity.Serie serie = _controller.SelectedSerie;

						if (title != _controller.SelectedSerie.Title) {
							serie.Title = title;
						}

						if (cover != "pack://application:,,,/res/no.jpg" && serie.Cover != cover) {
							using (Cover coverTool = new Cover()) {
								serie.Cover = coverTool.Create(cover, false);
							}
						}

						if (status != (Entity.DefaultStatus)_controller.SelectedSerie.StatusId) {
							serie.StatusId = (int)status;
						}

						if (synopsis != _controller.SelectedSerie.Synopsis) {
							serie.Synopsis = synopsis;
						}

						_controller.UpdateSerieEntity(serie);
					}
					break;
					case Level.Season: {
						Entity.Season season = _controller.SelectedSeason;

						if (title != _controller.SelectedSeason.Title) {
							season.Title = title;
						}

						if (cover != "pack://application:,,,/res/no.jpg" && season.Cover != cover) {
							using (Cover coverTool = new Cover()) {
								season.Cover = coverTool.Create(cover, false);
							}
						}

						if (status.ToString() != season.StatusId.ToString()) {
							season.StatusId = (int)status;
						}

						if (synopsis != _controller.SelectedSeason.Synopsis) {
							season.Synopsis = synopsis;
						}

						_controller.UpdateSeasonEntity(season);
					}
					break;
					case Level.Episode: {
						Entity.Episode episode = _controller.SelectedEpisode;

						if (title != _controller.SelectedEpisode.Title) {
							episode.Title = title;
						}

						if (cover != "pack://application:,,,/res/no.jpg" && cover != "System.Windows.Interop.InteropBitmap") {
							using (Cover coverTool = new Cover()) {
								episode.Cover = coverTool.Create(cover, false);
							}
						}

						episode.Watched = (status == Entity.DefaultStatus.Watched) ? true : false;

						_controller.UpdateEpisodeEntity(episode);
					}
					break;
				}

				Application.Current.Dispatcher.BeginInvoke((Action)(() => {
					_controller.Refresh();
				}));
			});

			t.Start();
		}

		/// <summary>
		/// Switch between the series and seasons view modes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_SwitchViewMode_Click(object sender, RoutedEventArgs e)
		{
			this.Breadcrumb = null;

			if (!this.listIsSeasonalView) {
				this.SetSeasonsViewMode();
			} else {
				this.SetSeriesViewMode();
			}

			this.SetSeriesAndSeasonsCombo(this.SelectedStatusId);

			_controller.Refresh();
		}

		/// <summary>
		/// Called by overing the right arrow button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Forward_MouseEnter(object sender, MouseEventArgs e)
		{
			this.button_Forward.Opacity = 0.75;
		}

		/// <summary>
		/// Called by clicking on the right arrow button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Forward_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			switch (this.Level) {
				case Level.Serie:
					if (_controller.LastVisitedSerieIndex != -1) {
						_controller.Forward(_controller.LastVisitedSerieIndex);
					}
				break;
				case Level.Season:
					if (_controller.LastVisitedSeasonIndex != -1) {
						_controller.Forward(_controller.LastVisitedSeasonIndex);
					}
				break;
			}
		}

		/// <summary>
		/// Called by moving away from the right arrow button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Forward_MouseLeave(object sender, MouseEventArgs e)
		{
			this.button_Forward.Opacity = 1;
		}

		/// <summary>
		/// Called by clicking on an item in one of the context menus.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ContextMenu_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem mi = sender as MenuItem;

			if (mi == null || ContextRequested == null) {
				return;
			}

			if (mi.Tag == null) { // Use the title
				ContextRequested(mi.Header.ToString(), e);
			} else { // Use the tag
				ContextRequested(mi.Tag.ToString(), e);
			}
		}

		/// <summary>
		/// Called by hovering the maginify icon in the search bar.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Image_Search_MouseEnter(object sender, MouseEventArgs e)
		{
			this.image_Search.Source = new BitmapImage(new Uri(FindResource("Img_DelGlass").ToString()));
		}

		/// <summary>
		/// Called by moving away from the maginify icon in the search bar.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Image_Search_MouseLeave(object sender, MouseEventArgs e)
		{
			this.image_Search.Source = new BitmapImage(new Uri(FindResource("Img_Glass").ToString()));
		}

		/// <summary>
		/// Reset search by clicking on the magnify icon in the search bar.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Image_Search_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (String.IsNullOrEmpty(this.textbox_Search.ActualText)) {
				return;
			}

			this.textbox_Search.Text = "";
			this.Breadcrumb = null;

			if (this.Level == Level.Episode) {
				this.SetSeriesAndSeasonsCombo(_controller.SerieSelectedStatusId);
			}

			if (this.listIsSeasonalView) {
				this.Level = Level.Season;
			} else {
				this.Level = Level.Serie;
			}
		}

		/// <summary>
		/// Called by clicking on the blackout overlay.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Grid_ListBlackout_MouseDown(object sender, MouseButtonEventArgs e)
		{
			this.CloseSidebar();
		}

		/// <summary>
		/// Called when a different search mode is selected in the ComboBox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_SearchOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!this.ComboBox_SearchOptions.IsLoaded) {
				return;
			}

			// Custom search
			if (this.ComboBox_SearchOptions.SelectedIndex == 6) {
				this.Blackout(true);

				CustomSearch cs = new CustomSearch(this.Level, _controller.Model.Status);
				cs.ShowDialog();

				if (cs.NeedReload) {
					if (this.listIsSeasonalView) {
						_controller.LoadSeasonsFromQuery(cs.Query);
						_controller.DisplaySeasons();
					} else {
						this.PrepareForSerieLevelIfNeeded();

						_controller.LoadSeriesFromQuery(cs.Query);
						_controller.DisplaySeries();
					}

					this.Breadcrumb = cs.CreateLabel();
				}

				this.Blackout(false);
			} else { // Other search modes (0: Contains, 1: Starting with)
				// No search string, nothing to do
				if (String.IsNullOrWhiteSpace(this.textbox_Search.ActualText)) {
					return;
				}

				this.DoSearch();
			}

			// Reset index so this event will fire event if we select the same item
			this.ComboBox_SearchOptions.SelectedIndex = -1;
		}

		/// <summary>
		/// Called by typing over the collection, handles some keyboard commands.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TileList_KeyUp(object sender, KeyEventArgs e)
		{
			IItem selectedItem = this.SelectedItem;

			// Do nothing if the sidebar is open or if the item's name is being edited
			if (this.Sidebar.IsOpen || (selectedItem != null && selectedItem.IsBeingEdited)) {
				return;
			}

			if (!this.textbox_Search.IsFocused) {
				// Go to the next level
				if (e.Key == Key.Enter && this.selectedIndex >= 0) {
					_controller.Forward(this.selectedIndex);
				}

				// Delete one or all of the selected items
				if (e.Key == Key.Delete) {
					if (this.SelectedIndexes.Count > 1) {
						_controller.DeleteMultipleItems();
					} else {
						_controller.DeleteSelectedItem();
					}
				}
			}

			// Navigate through the items using the arrow keys
			switch (e.Key) {
				case Key.Right:
					if (this.selectedIndex < this.Count - 1) this.SelectedIndex++;
				break;
				case Key.Left:
					if (this.selectedIndex > 0) this.SelectedIndex--;
				break;
				case Key.Up:
					if (this.selectedIndex - this.List.TilesPerLine >= 0) this.SelectedIndex -= this.List.TilesPerLine;
				break;
				case Key.Down: {
					if (this.selectedIndex + this.List.TilesPerLine < this.Count) {
						this.SelectedIndex += this.List.TilesPerLine;
					} else {
						this.SelectedIndex = this.Count - 1;
					}
				}
				break;
			}

			// Zoom or dezoom items
			if (e.Key == Key.Add || e.Key == Key.OemPlus) {
				this.List.ZoomItems(0.1f);
			} else if (e.Key == Key.Subtract || e.Key == Key.OemMinus) {
				this.List.ZoomItems(-0.1f);
			}
		}

		/// <summary>
		/// Called when typing into the search textbox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_Search_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.textbox_Search.IsLoaded) {
				return;
			}

			_controller.StopRunningThread();

			if (this.timer != null) this.timer.Stop();

			this.timer = new DispatcherTimer();
			timer.Tick += new EventHandler(this.StartSearch);
			timer.Interval = TimeSpan.FromMilliseconds(150);
			timer.Start();
		}

		/// <summary>
		/// Start the search, called by the timer.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void StartSearch(object sender, EventArgs e)
		{
			this.DoSearch();

			// Stop the timer
			((DispatcherTimer)sender).Stop();
		}

		/// <summary>
		/// Called when this.List is loaded.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void List_Loaded(object sender, EventArgs e)
		{
			_controller.LoadAndDisplayItems();

			this.button_ToggleViewMode.IsEnabled = true;
		}

		/// <summary>
		/// Use a timer to detect a long press on the Add button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Add_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// Only used for seasons and episodes
			if (!this.button_Add.IsEnabled || this.Level == Level.Serie) {
				return;
			}

			if (this.timer != null) this.timer.Stop();

			this.timer = new DispatcherTimer();
			timer.Tick += new EventHandler(AddLongPress);
			timer.Interval = TimeSpan.FromMilliseconds(500);
			timer.Start();
		}

		/// <summary>
		/// Called by a long press on the Add button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AddLongPress(object sender, EventArgs e)
		{
			// Stop the timer
			((DispatcherTimer)sender).Stop();

			this.Blackout(true);

			ushort lastNumber = (ushort)(_controller.LastEpisodeNumber + 1);
			AddMultiples addes = new AddMultiples(lastNumber);

			if (this.Level == Level.Season) {
				addes.ConfigureForSeasons();
			} else if (this.Level == Level.Episode) {
				addes.ConfigureForEpisodes(_controller.CurrentSeasonIsManga);
			}

			addes.ShowDialog();

			// Only add if we clicked on the Add button
			if (addes.Greenlight) {
				if (this.Level == Level.Season) {
					_controller.AddMultipleSeasons(addes.Counter, addes.From, addes.DefaultTitle, addes.Type);
				} else if (this.Level == Level.Episode) {
					_controller.AddMultipleEpisodes(addes.Counter, addes.From, addes.DefaultTitle);
				}

				// Notify
				this.MainWindow.Notify(
					Constants.Notify.Notif,
					Lang.OPERATION_FINISHED_TITLE,
					String.Format(Lang.EPISODES_ADDED, addes.Counter)
				);
			}

			this.Blackout(false);
		}

		/// <summary>
		/// Called by scrolling the collection.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			// Only work for non-labeled lists at the moment
			if (!Settings.Default.WhileScrolling || this.ListIsLabeled) {
				return;
			}

			int actualLineNumber = (int)Math.Round(this.ScrollValue / this.List.ItemHeight);

			if (actualLineNumber != currentLineNumber) {
				this.currentLineNumber = actualLineNumber;

				if (actualLineNumber > this.currentLine) {
					this.DisplayMoreItems();
					this.currentLine = actualLineNumber;
				}
			}
		}

		/// <summary>
		/// Called when the Types ComboBox is closed, refreshing the list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_Types_DropDownClosed(object sender, EventArgs e)
		{
			_controller.Refresh();
		}

		/// <summary>
		/// Called when the SearchOptions ComboBox is closed, execute a search.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_SearchOptions_DropDownClosed(object sender, EventArgs e)
		{
			if (this.SearchQuery != null) {
				this.DoSearch();
			}
		}

		/// <summary>
		/// Called when moving the mouse wheel while holding the Ctrl key.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void List_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (Keyboard.Modifiers != System.Windows.Input.ModifierKeys.Control) {
				return;
			}

			if (e.Delta > 0) {
				this.List.ZoomItems(0.1f);
			} else if (e.Delta < 0) {
				this.List.ZoomItems(-0.1f);
			}
		}

		#endregion Event
	}
}
