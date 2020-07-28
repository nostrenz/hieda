using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Hieda.Properties;
using Hieda.View.Element;
using Hieda.View.Window;
using Microsoft.VisualBasic.FileIO;
using Clipboard = System.Windows.Forms.Clipboard;
using File = System.IO.File;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = Hieda.Tool.Path;
using Level = Hieda.Constants.Level;

namespace Hieda.Controller
{
	public class Collection
	{
		// The view and the model
		View.Element.Collection _view;
		Model.Collection _model;

		// Keep selected status after having entered or quitted an item
		private int backSelectedStatusId = (int)Entity.DefaultStatus.All;
		private int nextSelectedStatusId = (int)Entity.DefaultStatus.All;
		private int serieSelectedStatusId = (int)Entity.DefaultStatus.All;

		private double lastScrollLevel = 0;
		private int lastVisitedSerieIndex = -1;
		private int lastVisitedSeasonIndex = -1;
		private int lastPlayedEpisodeNumber = 0;
		private Thread loadingThread;

		public Collection(View.Element.Collection view, Model.Collection model)
		{
			this._view = view;
			this._model = model;

			_view.SetController(this);

			this.DbConnect();
			this.RefreshStatus();

			if (Settings.Default.RPC_Enabled) {
				Tool.DiscordRpc.InitializeRpc();
			}
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Connect to the database.
		/// If it does not exists, create it.
		/// </summary>
		public void DbConnect()
		{
			// The database does not exists, create it
			if (!App.db.Exists) {
				App.db.Create();

				return;
			}

			// Connect to the existing database
			try {
				App.db.Connect();
				App.db.CheckForUpdates();
			} catch (SQLiteException) {
				_view.MainWindow.Notify(
					Constants.Notify.Warning,
					Lang.ERROR,
					Lang.Text("cantConnectDb", "Can't connect to database")
				);
			}
		}

		/// <summary>
		/// Load series in the model.
		/// Takes filters from the view to obtains the correspondig list.
		/// </summary>
		public void LoadSeries()
		{
			_model.FillSeries(_view.SearchQuery, _view.SearchMode, _view.SearchField, _view.SelectedStatusId);
		}

		/// <summary>
		/// Load series in the model from a query.
		/// Once series are load in the model, using DisplaySeries() will show them in the list.
		/// </summary>
		/// <param name="query"></param>
		public void LoadSeriesFromQuery(string query)
		{
			_model.LoadSeriesFromQuery(query);
		}

		/// <summary>
		/// Load seasons in the model from a query.
		/// Once seasons are load in the model, using DisplaySeasons() will show them in the list.
		/// </summary>
		/// <param name="query"></param>
		public void LoadSeasonsFromQuery(string query)
		{
			_model.LoadSeasonsFromQuery(query);
		}

		/// <summary>
		/// Load seasons in the model.
		/// Takes filters from the view to obtains the correspondig list.
		/// </summary>
		public void LoadSeasons()
		{
			if (_view.ListIsSeasonalView) { // Displays seasons orderer by seasonals
				_model.FillSeasonsForLabelList(_view.SearchQuery, _view.SearchMode, _view.SearchField, _view.SelectedStatusId, _view.CheckedTypes);
			} else { // Display seasons ordered by types
				_model.FillSeasons(_view.SelectedStatusId, _view.CheckedTypes, "grouping");
			}
		}

		/// <summary>
		/// Load episodes in the model.
		/// Takes filters from the view to obtains the correspondig list.
		/// </summary>
		public void LoadEpisodes()
		{
			_model.FillEpisodes(_view.CurrentSerieIndex, _view.CurrentSeasonIndex, _view.SelectedStatusId);
		}

		/// <summary>
		/// Display series in the list from the model.
		/// </summary>
		public void DisplaySeries()
		{
			this.StopRunningThread();

			_view.LabelCounter = _model.Series.Count;
			_view.LabelLevel = Lang.Plural("serie");
			_view.CurrentLine = 0;

			_view.UpdateSeries(_model.Series);
			_view.CreateCollectionContextMenu();
		}

		/// <summary>
		/// Display seasons in the list from the model.
		/// </summary>
		public void DisplaySeasons()
		{
			this.StopRunningThread();

			_view.LabelCounter = _model.Seasons.Count;
			_view.LabelLevel = Lang.Plural("season");
			_view.CurrentLine = 0;

			_view.UpdateSeasons(_model.Seasons);
			_view.CreateCollectionContextMenu();
		}

		/// <summary>
		/// Display episodes in the list from the model.
		/// </summary>
		public void DisplayEpisodes()
		{
			this.StopRunningThread();

			_view.LabelCounter = _model.Episodes.Count;
			_view.LabelLevel = Lang.UpperFirst(Lang.Plural(this.CurrentSeasonIsManga ? "chapter" : "episode"));

			_view.UpdateEpisodes(_model.Episodes);
			_view.CreateCollectionContextMenu();
		}

		/// <summary>
		/// Apply a semi-transparent black overlay  to darken the collection.
		/// </summary>
		/// <param name="toggle">true to enable, false to disable</param>
		public void Blackout(bool toggle)
		{
			_view.Blackout(toggle);
		}

		/// <summary>
		/// Load serie, season or episode entities and display them in the list depending on the view level.
		/// </summary>
		public void LoadAndDisplayItems()
		{
			this.StopRunningThread();

			switch (_view.Level) {
				case Level.Serie: {
					this.LoadSeries();
					this.DisplaySeries();
				}
				break;
				case Level.Season: {
					this.LoadSeasons();
					this.DisplaySeasons();
				}
				break;
				case Level.Episode: {
					this.LoadEpisodes();
					this.DisplayEpisodes();
				}
				break;
			}
		}

		/// <summary>
		/// Reload data from db and update the view at the current level.
		/// </summary>
		public void Refresh()
		{
			_view.RotateRefreshButton();

			this.LoadAndDisplayItems();
		}

		/// <summary>
		/// Go back to the previous view level.
		/// </summary>
		public void Back()
		{
			this.NextSelectedStatusId = _view.SelectedStatusId;

			// Cannot go below the serie level nor go back with the label list
			if (_view.Level <= Level.Serie || (_view.Level == Level.Season && _view.ListIsSeasonalView)) {
				return;
			}

			switch (_view.Level) {
				// Return to Series level
				case Level.Season: {
					_view.Level = Level.Serie;
					_view.SetSeriesAndSeasonsCombo(this.serieSelectedStatusId);

					// Load items then display them
					this.LoadSeries();
					this.DisplaySeries();

					_view.Breadcrumb = "";
					_view.LabelCounter = _model.Series.Count;
					_model.Seasons.Clear();
					_model.CurrentSeason = null;
					_model.CurrentSerie = null;
				} break;
				case Level.Episode: {
					Entity.Serie currentSerie = this.CurrentSerie;

					// Return to Series level
					if (currentSerie.NumberOfSeasons == 1 && Settings.Default.TileDirect) {
						_view.Level = Level.Serie;
						_view.SetSeriesAndSeasonsCombo(this.serieSelectedStatusId);

						// Load items then display them
						this.LoadSeries();
						this.DisplaySeries();

						_view.Breadcrumb = "";
						_view.LabelCounter = _model.Series.Count;
						_model.Seasons.Clear();
						_model.CurrentSerie = null;
						_model.CurrentSeason = null;
					} else { // Return to Season level
						_view.Level = Level.Season;
						_view.SetSeriesAndSeasonsCombo(this.backSelectedStatusId);

						// Load items then display them
						this.LoadSeasons();
						this.DisplaySeasons();

						_view.Breadcrumb = currentSerie.Title;
						_view.LabelCounter = _model.Seasons.Count;
					}

					_model.Episodes.Clear();
					_model.CurrentSeason = null;
				} break;
			}

			_view.ScrollValue = this.lastScrollLevel;

			if (Settings.Default.WhileScrolling) {
				_view.CurrentLine = 0;
			}

			_view.SetBreadcrumbContextMenu();
		}

		/// <summary>
		/// Launch the selected episode tile.
		/// </summary>
		public void PlayEpisode(Entity.Episode episode, string player=null)
		{
			// Open a file selection dialog and abort if nothing was selected
			if (episode.IsFile) {
				this.PlayEpisodeFile(episode, player);
			} else if (episode.IsUrl) {
				this.PlayEpisodeUrl(episode, player);
			} else {
				_view.MainWindow.Notify(
					Constants.Notify.Notif,
					Lang.Content("cannotPlayEpisode"),
					Lang.Content("noLinkedEpisode")
				);
			}
		}

		/// <summary>
		/// Mark an episode as watched or not in database, memory and view.
		/// </summary>
		public void SetEpisodeWatched(Entity.Episode episode, bool watched)
		{
			episode.Watched = watched;

			if (_view.Level == Level.Serie) {
				this.SelectedSerie.LastViewedSeasonId = episode.SeasonId;
				_model.SerieRepository.UpdateField(this.SelectedSerie.Id, "seasons_last_watched", episode.SeasonId);
			} else if (_view.Level == Level.Episode) {
				// Mark in view (IndexOf() doesn't work for some reason)
				int index = _model.Episodes.FindIndex(e => episode.Id.Equals(e.Id));

				if (index >= 0) {
					_view.List.Items[index].MarkItemAsWatched = watched;
				}
			}

			this.UpdateEpisodeTuple(episode, "watched", watched ? "1" : "0");
		}

		/// <summary>
		/// Add an item (Serie/Season/Episode) to the collection.
		/// </summary>
		/// <param name="level"></param>
		public void Add()
		{
			this.Blackout(true);

			switch (_view.Level) {
				case Level.Serie: { // Add serie
					SerieEdit adda = new SerieEdit(_model.Status);
					adda.ShowDialog();

					if (adda.Greenlight) {
						// Create a Serie object
						Entity.Serie serie = new Entity.Serie();
						serie.Title = adda.SerieTitle;
						serie.NumberOfSeasons = (byte)adda.SeasonsCount;
						serie.StatusId = adda.StatusId;

						// Find corresponding user status
						if (serie.StatusId > 0) {
							serie.UserStatus = this.Model.FindUserStatus(serie.StatusId);
						}

						// Add cover
						if (!String.IsNullOrEmpty(adda.Cover) && File.Exists(adda.Cover)) {
							using (Tool.Cover coverTool = new Tool.Cover()) {
								serie.Cover = coverTool.Create(adda.Cover, adda.DeleteCover);
							}
						}

						// Add serie to list and database
						int createdSerieId = _model.Add(serie);

						// Set genres
						_model.SerieRepository.AddGenresForSerie(createdSerieId, adda.Genres);

						// Add seasons
						for (int i = 0; i < adda.SeasonsCount; i++) {
							Entity.Season season = new Entity.Season();
							string number = (i + 1).ToString();

							season.SerieId = createdSerieId;
							season.Title = Lang.UpperFirst(Lang.Content("season")) + number.PadLeft(2, '0');
							serie.EpisodesTotal += season.EpisodesTotal;

							_model.SeasonRepository.Add(season);
						}

						// Add the new season to the list
						// (in model and view, a the end of the list)
						_view.DisplayNothingMessage(false);
						_view.ScrollToBottom();
						_view.Add(serie);
						_view.LabelCounter += 1;
					}
				} break;
				case Level.Season: {
					Entity.Serie currentSerie = this.CurrentSerie;
					SeasonEdit addS = new SeasonEdit(currentSerie.Id, _model.Status);
					addS.Owner = App.Current.MainWindow;

					// Automatically add the season number based on the last one.
					int lastSeasonIndex = _model.Seasons.Count - 1;

					if (_model.Seasons.Count > 0) {
						addS.Number = (ushort)(_model.Seasons[lastSeasonIndex].Number + 1);
					} else {
						addS.Number = 1;
					}

					addS.ShowDialog();

					if (addS.Greenlight) {
						Entity.Season season = new Entity.Season();
						season.Title = addS.SeasonTitle;
						season.StatusId = addS.StatusId;

						// Find corresponding user status
						if (season.StatusId > 0) {
							season.UserStatus = this.Model.FindUserStatus(season.StatusId);
						}

						// Add cover
						if (!String.IsNullOrEmpty(addS.Cover) && File.Exists(addS.Cover)) {
							using (Tool.Cover coverTool = new Tool.Cover()) {
								season.Cover = coverTool.Create(addS.Cover, addS.DeleteImage);
							}
						}

						season.SerieId = currentSerie.Id;
						season.Number = addS.Number;
						season.EpisodesTotal = addS.EpisodesCount;
						season.StudioId = addS.StudioId;
						season.Seasonal = addS.Seasonal;
						season.Year = addS.Year;
						season.Month = addS.Month;
						season.Day = addS.Day;
						season.Type = addS.Type;
						season.Source = addS.Source;
						season.ParentCover = currentSerie.Cover;
						season.WideEpisode = addS.WideEpisodes;
						season.Grouping = addS.Grouping;

						// Add to memory and db
						_model.Add(season);
						currentSerie.NumberOfSeasons += 1;

						_view.DisplayNothingMessage(false);
						_view.ScrollToBottom();
						_view.Add(season);
					}
				} break;
				case Level.Episode: {
					EpisodeEdit adde = new EpisodeEdit((ushort)(this.LastEpisodeNumber + 1), this.CurrentSeasonIsManga);
					adde.ShowDialog();

					if (adde.Greenlight) {
						Entity.Episode episode = new Entity.Episode();
						episode.Title = adde.EpisodeTitle;
						episode.Number = adde.Number;
						episode.Watched = adde.Watched;
						episode.SerieId = this.CurrentSerie.Id;
						episode.SeasonId = this.CurrentSeason.Id;
						episode.Uri = adde.FileOrUrl;
						episode.ParentCover = this.CurrentSeason.DisplayCover;

							// Add cover
						if (!String.IsNullOrEmpty(adde.Cover) && File.Exists(adde.Cover)) {
							using (Tool.Cover coverTool = new Tool.Cover()) {
								episode.Cover = coverTool.Create(adde.Cover, adde.DeleteImage);
							}
						}

						int lastEpisodeNumber = 0;

						if (_model.Episodes.Count > 0) {
							lastEpisodeNumber = _model.Episodes[_model.Episodes.Count - 1].Number;
						}

						// Add to memory and db
						_model.Add(episode);

						// Just add the tile to the end of the list
						if (episode.Number >= lastEpisodeNumber) {
							_view.DisplayNothingMessage(false);
							_view.ScrollToBottom();
							_view.Add(episode);
						} else {
							// Refresh the have the tiles in the right order
							this.Refresh();
						}
					}
				} break;
			}

			this.Blackout(false);
		}

		/// <summary>
		/// Add an episode using the EpisodeImport window.
		/// </summary>
		/// <param name="import"></param>
		public void AddEpisodes(EpisodeImport import)
		{
			Entity.Episode episode = null;
			ushort lastEpisodeNumber = this.LastEpisodeNumber;

			foreach (ImportRow row in import.Rows) {
				ushort episodeNumber = ushort.Parse(row.Number);

				// Replace option enabled, try to find an episode with the same number
				if (import.Replace) {
					episode = this.CurrentEpisodes.Find(e => e.Number.Equals(episodeNumber));

					if (episode == null) {
						episode = new Entity.Episode();
					}
				} else {
					episode = new Entity.Episode();
				}

				// Import in DB
				episode.Title = row.Title;
				episode.Number = episodeNumber;
				episode.Watched = row.Watched;
				episode.SerieId = this.CurrentSerie.Id;
				episode.SeasonId = this.CurrentSeason.Id;
				episode.ParentCover = this.CurrentSeason.DisplayCover;
				episode.Uri = row.Path;

				if (Settings.Default.FakeEpisode) {
					this.RemoveFakeEpisodeWithNumber(episode.Number);
				}

				// The episode already has an ID meaning we got it earlier using CurrentEpisodes.Find()
				if (import.Replace && episode.Id > 0) {
					this.UpdateEpisodeEntity(episode);
				} else {
					_model.Add(episode);
				}
			}

			// Notify
			_view.MainWindow.Notify(
				Constants.Notify.Notif,
				Lang.OPERATION_FINISHED_TITLE,
				String.Format(Lang.EPISODES_ADDED, import.Rows.Length)
			);

			// There's more than one episode or the new one does not follow the previous ones, refresh to have them in the right order...
			if (import.Rows.Length > 1 || episode.Number <= lastEpisodeNumber) {
				this.Refresh();

				return;
			}

			// there only one episode, just add it at the end of the list
			_view.Add(episode);

			// Update episodes counters and remove the nothing message
			this.UpdateEpisodesCounters((ushort)import.Rows.Length, (ushort)import.GetViewedEpisodeNumber());
			_view.DisplayNothingMessage(false);

			// Load thumbs after
			if (episode.File.IsCompatibleWithThumb && Settings.Default.TileVideoThumbs) {
				this.StopRunningThread();

				this.loadingThread = new Thread(new ThreadStart(_view.UpdateEpisodesCovers));
				this.loadingThread.Start();
			}
		}

		/// <summary>
		/// Launch the episode following the last viewed one.
		/// </summary>
		public void Continue()
		{
			Entity.Serie selectedSerie = null;
			Entity.Season season = null;
			int lastViewedSeasonId = 0;

			if (_view.Level == Level.Serie) {
				this.LoadSeasonsForSelectedSerie();

				selectedSerie = this.SelectedSerie;
				lastViewedSeasonId = selectedSerie.LastViewedSeasonId;
				season = this.SelectedSeasons.Find(s => s.Id == lastViewedSeasonId);

				bool finished = selectedSerie.StatusId == (int)Entity.DefaultStatus.Finished;

				if (season != null) {
					finished = season.StatusId == (int)Entity.DefaultStatus.Finished;
				}

				// No LastViewedSeasonId set or season is finished
				if (lastViewedSeasonId == 0 || finished) {
					lastViewedSeasonId = this.FindNotFinishedSeasonId(_model.Seasons);

					// LastViewedSeasonId is still 0, there is nothing to continue
					if (lastViewedSeasonId == 0) {
						_view.MainWindow.Notify(
							Constants.Notify.Warning,
							Lang.OPERATION_CONTINUE_NOTHING_TITLE,
							Lang.NO_EPISODES
						);

						return;
					}

					season = this.SelectedSeasons.Find(s => s.Id == lastViewedSeasonId);
				}

				// Season finished, mark it as finished and rerun this function
				if (season.EpisodesTotal != 0 && season.EpisodesViewed == season.EpisodesTotal) {
					season.StatusId = (int)Entity.DefaultStatus.Finished;

					this.UpdateSeasonEntity(season, "StatusId");
					this.Continue();
				}
			} else if (_view.Level == Level.Season) {
				season = this.SelectedSeason;
				lastViewedSeasonId = season.Id;
			} else if (_view.Level == Level.Episode) {
				season = this.CurrentSeason;
				lastViewedSeasonId = season.Id;
			}

			// Now we can retieve episodes
			List<Entity.Episode> episodes = _model.EpisodeRepository.GetAllBySeason(lastViewedSeasonId);

			// If no episodes
			if (episodes.Count == 0) {
				_view.MainWindow.Notify(
					Constants.Notify.Warning,
					Lang.OPERATION_CONTINUE_NOTHING_TITLE,
					Lang.NO_EPISODES
				);

				return;
			}

			Entity.Episode episode = this.FindNextEpisode(episodes);

			if (episode == null) {
				_view.MainWindow.Notify(
					Constants.Notify.Warning,
					Lang.OPERATION_CONTINUE_NOTHING_TITLE,
					String.Format(Lang.NO_EPISODE_DESCR)
				);

				return;
			}

			if (!File.Exists(episode.Uri)) {
				_view.MainWindow.Notify(
					Constants.Notify.Warning,
					Lang.OPERATION_CONTINUE_NOTHING_TITLE,
					String.Format(Lang.Content("episodeNoFile"), episode.Number)
				);

				return;
			}

			// Launch episode
			this.PlayEpisode(episode);
			episode.Watched = true;

			// Update counters on tile
			if (_view.Level == Level.Serie) {
				selectedSerie.EpisodesViewed++;
				_view.SelectedItem.SetEpisodesValues(selectedSerie.EpisodesViewed, selectedSerie.EpisodesOwned, selectedSerie.EpisodesTotal);
			} else if (_view.Level == Level.Season) {
				this.SelectedSeason.EpisodesViewed++;
				_view.SelectedItem.SetEpisodesValues(this.SelectedSeason.EpisodesViewed, this.SelectedSeason.EpisodesOwned, this.SelectedSeason.EpisodesTotal);
			}

			this.ActualSerie.LastViewedSeasonId = episode.SeasonId;

			this.UpdateEpisodeEntity(episode, "Watched");
			this.UpdateSerieEntity(this.ActualSerie);
		}

		/// <summary>
		/// Populate seasons for the selected serie.
		/// </summary>
		private void LoadSeasonsForSelectedSerie()
		{
			_model.LoadSeasonsBySerieId(this.SelectedSerie.Id);
		}

		/// <summary>
		/// Try to find a non-finished season.
		/// </summary>
		/// <param name="seasons"></param>
		/// <returns></returns>
		private int FindNotFinishedSeasonId(List<Entity.Season> seasons)
		{
			foreach (Entity.Season season in seasons) {
				if (season.StatusId != (int)Entity.DefaultStatus.Finished) {
					return season.Id;
				}
			}

			return 0;
		}

		/// <summary>
		/// Find the next episode to watch which will be the first unwatched one in the list.
		/// </summary>
		/// <param name="episodes"></param>
		/// <returns></returns>
		private Entity.Episode FindNextEpisode(List<Entity.Episode> episodes)
		{
			foreach (Entity.Episode episode in episodes) {
				if (!episode.Watched) {
					return episode;
				}
			}

			return null;
		}

		/// <summary>
		/// Called when an event is raised from a tile's context menu.
		/// </summary>
		/// <param name="sender"></param>
		public void ItemContext(string sender)
		{
			switch (sender) {
				case "goToNextLevel":
					this.Forward(_view.SelectedIndex, true);
				break;
				case "continue":
					this.Continue();
				break;
				case "edit":
					this.EditRequested();
				break;
				case "remove":
					_view.RemoveSelectedItem();
				break;
				case "delete":
					this.DeleteSelectedItem();
				break;
				case "moveItems":
					if (_view.Level == Level.Season) {
						this.MoveSeasons();
					} else if (_view.Level == Level.Episode) {
						this.MoveEpisodes();
					}
				break;
				case "openFolder": {
					string uri = this.SelectedEpisode.Uri;

					if (uri == null) {
						return;
					}

					if (uri.StartsWith(@"\files\Animes\")) {
						ProcessStartInfo processInfo = new ProcessStartInfo(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.ContainingPath(uri));
						Process myProcess = Process.Start(processInfo);
					} else {
						ProcessStartInfo processInfo = new ProcessStartInfo(Path.ContainingPath(uri));
						Process myProcess = Process.Start(processInfo);
					}
				}
				break;
				// Linked file
				case "copy": {
					this.SelectedEpisode.File.CopyToClipboard();
					_view.MainWindow.Notify(
						Constants.Notify.Info,
						Lang.FILE_COPIED_TITLE,
						String.Format(Lang.FILE_COPIED_DESCR, this.SelectedEpisode.Uri)
					);
				}
				break;
				case "copyLinkedPath":
					string filepath = this.SelectedEpisode.Uri;

					if (filepath == null) {
						return;
					}

					try {
						Clipboard.SetText(filepath);
					} catch (System.Runtime.InteropServices.ExternalException) { }

					_view.MainWindow.Notify(
						Constants.Notify.Info,
						Lang.COPIED,
						String.Format(Lang.COPIED_TO_CLIPBOARD, filepath)
					);
				break;
				case "deleteLinkedFile": {
					if (new TwoButtonsDialog("Delete file '" + Path.LastSegment(this.SelectedEpisode.Uri) + "' from disk?", "Confirm deletion", "Yes", "No").Open()) {
						this.SelectedEpisode.File.Delete();
						this.SelectedEpisode.Uri = null;
						this.Refresh();
					}
				}
				break;
				case "relocateEpisode":
					this.RelocateEpisode(this.SelectedEpisode);
				break;
				case "importSubtitle":
					this.ImportSubtitleForSelectedEpisode();
				break;
				// Status
				// Series & Seasons
				case "none":
					this.ChangeSelectedItemsStatus(Entity.DefaultStatus.None);
				break;
				case "toWatch":
					this.ChangeSelectedItemsStatus(Entity.DefaultStatus.ToWatch);
				break;
				case "current":
					this.ChangeSelectedItemsStatus(Entity.DefaultStatus.Current);
				break;
				case "standBy":
					this.ChangeSelectedItemsStatus(Entity.DefaultStatus.StandBy);
				break;
				case "finished":
					this.ChangeSelectedItemsStatus(Entity.DefaultStatus.Finished);
				break;
				case "dropped":
					this.ChangeSelectedItemsStatus(Entity.DefaultStatus.Dropped);
				break;
				case "perso": {
					this.Blackout(true);
					int selectedIndex = new View.Window.ItemSelector(_model.Status).Open().SelectedIndex;
					this.Blackout(false);

					if (selectedIndex >= 0) {
						this.ChangeSelectedItemsStatus(_model.Status[selectedIndex]);
					}
				}
				break;
				// Episodes
				case "markAsWatched":
					this.SetEpisodeWatched(this.SelectedEpisode, _view.SelectedItem.Watched);
				break;
				// cover
				case "viewFull": {
					string fullCoverPath = _view.SelectedItem.FullCoverPath;

					if (File.Exists(fullCoverPath)) {
						Process.Start(fullCoverPath);
					} else {
						_view.MainWindow.Notify(
							Constants.Notify.Warning,
							"Attention",
							Lang.Text("fileDoesNotExists") + ": \"" + fullCoverPath + '"'
						);
					}
				}
				break;
				case "copyFile": {
					StringCollection FileCollection = new StringCollection();
					FileCollection.Add(_view.SelectedItem.FullCoverPath);
					Clipboard.SetFileDropList(FileCollection);
				}
				break;
				case "change":
					this.ChangeSelectedItemCover();
				break;
				case "removeCover":
					this.RemoveSelectedItemCover();
				break;
				case "generateThumbnail":
					this.GenerateThumbnailDialog();
				break;
				case "onlineCover":
					this.SearchOnlineCover();
				break;
				// Play with
				case "playWithDefault":
					this.PlayEpisode(this.SelectedEpisode);
				break;
				case "playWithVlc":
					this.PlayEpisode(this.SelectedEpisode, "VLC");
				break;
				case "playWithMpcHc":
					this.PlayEpisode(this.SelectedEpisode, "MPC-HC");
				break;
				case "playWithMpv":
					this.PlayEpisode(this.SelectedEpisode, "mpv");
				break;
				// Multiple selected
				case "editMutliple":
					this.EditMultipleItems();
				break;
				case "deleteMutliple":
					this.DeleteMultipleItems();
				break;
				case "changeMultipleCovers":
					this.ChangeMultipleCovers();
				break;
				case "removeMultipleCovers":
					this.RemoveMultipleCovers();
				break;
				case "markAllWatched":
					this.MarkAllAsWatched(true);
				break;
				case "markAllNotWatched":
					this.MarkAllAsWatched(false);
				break;
				case "relocateEpisodes":
					this.RelocateEpisodes();
				break;
			}
		}

		/// <summary>
		/// Callend when an event is raised from the collection's context menu.
		/// </summary>
		/// <param name="sender"></param>
		public void CollectionContext(string sender)
		{
			switch (sender) {
				case "copySerieTitle": {
					string currentSerieTitle = this.CurrentSerie.Title;

					System.Windows.Clipboard.SetText(currentSerieTitle);
					_view.MainWindow.Notify(
						Constants.Notify.Info,
						String.Format(Lang.TITLE_COPIED, Lang.SERIE),
						String.Format(Lang.COPIED_TO_CLIPBOARD, currentSerieTitle)
					);
				}
				break;
				case "copySeasonTitle": {
					System.Windows.Clipboard.SetText(this.CurrentSeason.Title);
					_view.MainWindow.Notify(
						Constants.Notify.Info,
						String.Format(Lang.TITLE_COPIED, Lang.SEASON),
						String.Format(Lang.COPIED_TO_CLIPBOARD, this.CurrentSeason.Title)
					);
				}
				break;
				case "selectAll":
					_view.SelectAll();
				break;
				case "deselectAll":
					_view.DeselectAll();
				break;
				case "switchList":
					_view.SwitchList();
				break;
			}
		}

		/// <summary>
		/// Delete an item.
		/// Remove from view, memory, database.
		/// Also set new values for seasons and episodes counters.
		/// </summary>
		public void DeleteSelectedItem()
		{
			this.Blackout(true);

			TwoButtonsDialog dlg = new TwoButtonsDialog(
				String.Format(Lang.DELETE_FROM_DB, _view.SelectedItem.Title),
				Lang.CONFIRM_DELETION, Lang.YES, Lang.NO
			);
			dlg.Owner = App.Current.MainWindow;

			// No opened file, abort
			if (!dlg.Open()) {
				this.Blackout(false);

				return;
			}

			switch (_view.Level) {
				case Level.Serie: {
					// Delete the serie, and everything under it (seasons and episodes)
					_model.DeleteSerie(_view.SelectedIndex);

					this.Refresh();
				}
				break;
				case Level.Season: {
					if (_model.Seasons.Count > 1) {
						Entity.Serie currentSerie = this.CurrentSerie;

						// Update seasons counter
						currentSerie.NumberOfSeasons -= 1;

						// Update serie's episodes counter
						List<Entity.Episode> episodes = _model.EpisodeRepository.GetAllBySeason(this.SelectedSeason.Id);
						ushort ownedEpisodes = 0;
						ushort viewedEpisodes = 0;

						// Count how many episodes need to be removed from counters.
						foreach (Entity.Episode episode in episodes) {
							ownedEpisodes++;

							if (episode.Watched) {
								viewedEpisodes++;
							}
						}

						// Remove episodes from serie's owned counter.
						if (currentSerie.EpisodesOwned >= ownedEpisodes) {
							currentSerie.EpisodesOwned -= ownedEpisodes;
						} else {
							currentSerie.EpisodesOwned = 0;
						}

						// Remove episodes from serie's viewed counter.
						if (currentSerie.EpisodesViewed >= viewedEpisodes) {
							currentSerie.EpisodesViewed -= viewedEpisodes;
						} else {
							currentSerie.EpisodesViewed = 0;
						}

						// Persist serie in DB:
						_model.SerieRepository.Update(currentSerie);

						// Delete season from DB, and everything under it (episodes).
						_model.DeleteSeason(_view.SelectedIndex);

						this.Refresh();
					} else {
						_view.MainWindow.Notify(
							Constants.Notify.Warning,
							Lang.ACTION_CANCELED,
							Lang.Text("atLeastOneSeason", "A series must always have at least one season.")
						);
					}
				}
				break;
				case Level.Episode: {
					Entity.Serie currentSerie = this.CurrentSerie;
					Entity.Season currentSeason = this.CurrentSeason;

					// Update counters.
					if (currentSeason.EpisodesOwned > 0) {
						currentSeason.EpisodesOwned -= 1;
					}

					if (currentSerie.EpisodesOwned > 0) {
						currentSerie.EpisodesOwned -= 1;
					}

					// Remove this episode from parent serie and season's counters.
					if (_view.SelectedItem.Watched) {
						if (currentSeason.EpisodesViewed > 0) {
							currentSeason.EpisodesViewed -= 1;
						}

						if (currentSerie.EpisodesViewed > 0) {
							currentSerie.EpisodesViewed -= 1;
						}
					}

					_model.SerieRepository.Update(currentSerie);
					_model.SeasonRepository.Update(currentSeason);

					// Delete from DB
					_model.DeleteEpisode(_view.SelectedIndex);

					this.Refresh();
				}
				break;
			}

			this.Blackout(false);
		}

		public void EditMultipleItems()
		{
			this.Blackout(true);

			List<int> itemIds = new List<int>();
			List<KeyValuePair<string, string>> columns = new List<KeyValuePair<string, string>>();
			string title = null;
			string cover = null;
			ushort number = 0;

			switch (_view.Level) {
				case Level.Serie:
					List<Entity.Serie> series = new List<Entity.Serie>();

					foreach (int index in _view.SelectedIndexes) {
						series.Add(_model.Series[index]);
						itemIds.Add(_model.Series[index].Id);
					}

					SerieEdit serieEdit = new SerieEdit(series, _model.Status);
					serieEdit.ShowDialog();

					title = serieEdit.SerieTitle;
					cover = serieEdit.Cover;

					if (title != Constants.VARIOUS) {
						columns.Add(new KeyValuePair<string, string>("title", title));
					}
					if (cover != Constants.VARIOUS) {
						columns.Add(new KeyValuePair<string, string>("cover", cover));
					}

					if (serieEdit.Greenlight) {
						_model.SerieRepository.UpdateMultiples(columns, itemIds);
					}
				break;
				case Level.Season:
					List<Entity.Season> seasons = new List<Entity.Season>();

					foreach (int index in _view.SelectedIndexes) {
						seasons.Add(_model.Seasons[index]);
						itemIds.Add(_model.Seasons[index].Id);
					}

					SeasonEdit seasonEdit = new SeasonEdit(seasons, _model.Status);
					seasonEdit.ShowDialog();

					if (!seasonEdit.Greenlight) {
						this.Blackout(false);

						return;
					}

					title = seasonEdit.SeasonTitle;
					cover = seasonEdit.Cover;
					number = seasonEdit.Number;
					ushort year = seasonEdit.Year;
					byte month = seasonEdit.Month;
					byte day = seasonEdit.Day;
					string grouping = seasonEdit.Grouping;
					ushort episodesTotal = seasonEdit.EpisodesCount;
					bool wideEpisode = seasonEdit.WideEpisodes;
					int statusId = seasonEdit.StatusId;
					int studioId = seasonEdit.StudioId;
					Constants.Seasonal seasonal = seasonEdit.Seasonal;
					Constants.Type type = seasonEdit.Type;
					Constants.Source source = seasonEdit.Source;

					if (title != Constants.VARIOUS) {
						columns.Add(new KeyValuePair<string, string>("title", title));
					}
					if (year > 0) {
						columns.Add(new KeyValuePair<string, string>("year", year.ToString()));
					}
					if (month > 0) {
						columns.Add(new KeyValuePair<string, string>("month", month.ToString()));
					}
					if (day > 0) {
						columns.Add(new KeyValuePair<string, string>("day", day.ToString()));
					}
					if (grouping != Constants.VARIOUS) {
						columns.Add(new KeyValuePair<string, string>("grouping", grouping));
					}
					if (cover != Constants.VARIOUS) {
						columns.Add(new KeyValuePair<string, string>("cover", cover));
					}
					if (!seasonEdit.NumberIsVarious) {
						columns.Add(new KeyValuePair<string, string>("number", number.ToString()));
					}
					if (!seasonEdit.EpisodesCountIsVarious) {
						columns.Add(new KeyValuePair<string, string>("episodes_total", episodesTotal.ToString()));
					}
					if (statusId != (int)Entity.DefaultStatus.Null) {
						columns.Add(new KeyValuePair<string, string>("status_id", statusId.ToString()));
					}
					if (studioId != -1) {
						columns.Add(new KeyValuePair<string, string>("studio_id", studioId.ToString()));
					}
					if (!seasonEdit.BothWideEpisodeRadiosAreUnchecked) {
						columns.Add(new KeyValuePair<string, string>("wide_episode", seasonEdit.WideEpisodes ? "1" : "0"));
					}
					if (seasonal != Constants.Seasonal.Null) {
						columns.Add(new KeyValuePair<string, string>("seasonal", ((int)seasonal).ToString()));
					}
					if (type != Constants.Type.Null) {
						columns.Add(new KeyValuePair<string, string>("type", ((int)type).ToString()));
					}
					if (source != Constants.Source.Null) {
						columns.Add(new KeyValuePair<string, string>("source", ((int)source).ToString()));
					}

					if (seasonEdit.Greenlight) {
						_model.SeasonRepository.UpdateMultiples(columns, itemIds);
					}
				break;
				case Level.Episode:
					List<Entity.Episode> episodes = new List<Entity.Episode>();

					foreach (int index in _view.SelectedIndexes) {
						episodes.Add(_model.Episodes[index]);
						itemIds.Add(_model.Episodes[index].Id);
					}

					EpisodeEdit episodeEdit = new EpisodeEdit(episodes);
					episodeEdit.ShowDialog();

					title = episodeEdit.EpisodeTitle;
					cover = episodeEdit.Cover;
					number = episodeEdit.Number;

					if (title != Constants.VARIOUS) {
						columns.Add(new KeyValuePair<string, string>("title", title));
					}
					if (cover != Constants.VARIOUS) {
						columns.Add(new KeyValuePair<string, string>("cover", cover));
					}
					if (!episodeEdit.NumberIsVarious) {
						columns.Add(new KeyValuePair<string, string>("number", number.ToString()));
					}

					if (episodeEdit.Greenlight) {
						_model.EpisodeRepository.UpdateMultiples(columns, itemIds);
					}
				break;
			}

			this.Blackout(false);
			this.Refresh();
		}

		public void DeleteMultipleItems()
		{
			this.Blackout(true);

			TwoButtonsDialog dlg = new TwoButtonsDialog(
				String.Format(Lang.Text("deleteMultipleFromDb"), _view.SelectedIndexes.Count),
				Lang.CONFIRM_DELETION, Lang.YES, Lang.NO
			);
			dlg.Owner = App.Current.MainWindow;

			if (dlg.Open()) {
				switch (_view.Level) {
					case Level.Serie:
						_model.DeleteSeries(_view.SelectedIndexes);
						break;
					case Level.Season:
						_model.DeleteSeasons(_view.SelectedIndexes);
						break;
					case Level.Episode:
						_model.DeleteEpisodes(_view.SelectedIndexes);
						break;
				}

				this.Refresh();
			}

			this.Blackout(false);
		}

		public void UpdateSerieEntity(Entity.Serie serie)
		{
			_model.SerieRepository.Update(serie);
		}

		public void UpdateSeasonEntity(Entity.Season season)
		{
			_model.SeasonRepository.Update(season);
		}

		public void UpdateSeasonEntity(Entity.Season season, string field = null)
		{
			_model.SeasonRepository.Update(season, field);
		}

		public void UpdateEpisodeEntity(Entity.Episode episode)
		{
			// If it's a fake episode, add it.
			if (Settings.Default.FakeEpisode && episode.Fake) {
				_model.EpisodeRepository.Add(episode);
			} else {
				_model.EpisodeRepository.Update(episode);
			}
		}

		public void UpdateEpisodeEntity(Entity.Episode episode, string field = null)
		{
			// If it's a fake episode, add it.
			if (Settings.Default.FakeEpisode && episode.Fake) {
				_model.EpisodeRepository.Add(episode);
				this.Refresh();
			} else {
				_model.EpisodeRepository.Update(episode, field);
			}
		}

		public void UpdateEpisodeTuple(Entity.Episode episode, string column, string value)
		{
			// If it's a fake episode, add it.
			if (Settings.Default.FakeEpisode && episode.Fake) {
				_model.EpisodeRepository.Add(episode);
				this.Refresh();
			} else {
				_model.EpisodeRepository.UpdateField(episode.Id, column, value);
			}
		}

		public void UpdateSelectedEpisode()
		{
			this.UpdateEpisodeEntity(this.SelectedEpisode);
		}

		public void UpdateSelectedEpisode(string field = null)
		{
			this.UpdateEpisodeEntity(this.SelectedEpisode, field);
		}

		public void UpdateSelectedEpisode(string column, string value)
		{
			this.UpdateEpisodeTuple(this.SelectedEpisode, column, value);
		}

		/// <summary>
		/// Go to the next view level.
		/// </summary>
		/// <param name="index">
		/// Item of the selected tile.
		/// </param>
		/// <param name="ignoreDirectToEpisodes">
		/// By default if there's only one season for the selected serie,
		/// go directly to the episode level.
		/// Setting this to true allow to override this behaviour.
		/// </param>
		public void Forward(int index, bool ignoreDirectToEpisodes=false)
		{
			_view.StatusSelectorEventEnabled = false;
			this.lastScrollLevel = _view.ScrollValue;

			switch (_view.Level) {
				case Level.Serie: {
					this.SerieSelectedStatusId = _view.SelectedStatusId;
					this.BackSelectedStatusId = _view.SelectedStatusId;
					_model.CurrentSerie = _model.Series[index];

					// This series has only one season, go to Episode level
					if (_model.CurrentSerie.NumberOfSeasons == 1
					&& Settings.Default.TileDirect
					&& !ignoreDirectToEpisodes) {
						_view.Level = Level.Episode;
						_view.SetEpisodesCombo(this.nextSelectedStatusId);
						_view.CurrentSerieIndex = index;
						_view.CurrentSeasonIndex = 0;

						this.LoadEpisodes();
						this.DisplayEpisodes();

						_view.Breadcrumb = _model.CurrentSerie.Title;
					} else {  // This series has more than one season, go to Season level
						_view.Level = Level.Season;
						_view.CurrentSerieIndex = index;
						_view.SelectedStatusId = this.nextSelectedStatusId;

						this.LoadSeasons();
						this.DisplaySeasons();

						_view.Breadcrumb = this.CurrentSerie.Title;
					}

					this.lastVisitedSerieIndex = index;

					_model.Series.Clear();
				}
				break;

				// Season level, we want to load episodes
				case Level.Season: {
					this.BackSelectedStatusId = _view.SelectedStatusId;

					_model.CurrentSeason = _model.Seasons[index];
					_view.Level = Level.Episode;
					_view.SetEpisodesCombo(this.nextSelectedStatusId);
					_view.CurrentSeasonIndex = index;

					this.LoadEpisodes();
					this.DisplayEpisodes();

					_view.Breadcrumb = this.CurrentSeason.Title;

					Entity.Serie currentSerie = this.CurrentSerie;

					if (currentSerie != null) {
						_view.Breadcrumb = currentSerie.Title;

						// Add the season title if not empty
						if (!String.IsNullOrEmpty(this.CurrentSeason.Title)) {
							_view.Breadcrumb += " > " + this.CurrentSeason.Title;
						}
					}

					this.lastVisitedSeasonIndex = index;
					_model.Seasons.Clear();
				}
				break;
				case Level.Episode:
					if (this.SelectedEpisode != null) this.PlayEpisode(this.SelectedEpisode);
				break;
			}

			_view.SetBreadcrumbContextMenu();
			_view.StatusSelectorEventEnabled = true;
		}

		/// <summary>
		/// If the loading thread is running, stop it.
		/// </summary>
		public void StopRunningThread()
		{
			if (this.loadingThread != null && this.loadingThread.IsAlive) {
				this.loadingThread.Abort();
			}
		}

		/// <summary>
		/// Add multiple new seasons in view and db.
		/// </summary>
		public void AddMultipleSeasons(ushort count, ushort from, string title, Constants.Type type)
		{
			Entity.Season season = new Entity.Season();

			season.SerieId = this.CurrentSerie.Id;

			for (byte i = 0; i < count; i++) {
				season.Number = (ushort)(from + i);
				season.Title = title.Replace("%number%", season.Number.ToString());
				season.Type = type;

				_model.Add(season);
			}

			this.Refresh();
		}

		/// <summary>
		/// Add multiple new episodes in view and db.
		/// </summary>
		public void AddMultipleEpisodes(ushort count, ushort from, string title)
		{
			Entity.Episode episode = new Entity.Episode();

			episode.SerieId = this.CurrentSerie.Id;
			episode.SeasonId = this.CurrentSeason.Id;

			for (byte i = 0; i<count; i++) {
				episode.Number = (ushort)(from + i);
				episode.Title = title.Replace("%number%", episode.Number.ToString());

				_model.Add(episode);
			}

			this.Refresh();
		}

		/// <summary>
		/// Handle the Discord RPC window.
		/// </summary>
		public bool ShowRpcWindow()
		{
			// No selected serie or season nor currently in a season
			if (_view.SelectedIndex < 0 && _view.Level != Level.Episode) {
				return false;
			}

			string title = null;
			string largeImage = null;

			// Get the title and large image for the selected serie, selected season or current season
			if (this._view.Level == Level.Serie) {
				title = this.SelectedSerie.Title;
				largeImage = this.SelectedSerie.RpcLargeImage;
			} else if (this._view.Level == Level.Season && _view.ListIsSeasonalView) { // We don't have a serie from this view mode
				title = _view.SelectedItem.Title;
				largeImage = this.SelectedSeason.RpcLargeImage;
			} else {
				Entity.Serie currentSerie = this.CurrentSerie;

				title = currentSerie.Title;

				if (this._view.Level == Level.Season) {
					if (!String.IsNullOrEmpty(this.SelectedSeason.Title)) {
						title += " - " + this.SelectedSeason.Title;
					}

					largeImage = this.SelectedSeason.RpcLargeImage;
				} else if (this._view.Level == Level.Episode) {
					if (!String.IsNullOrEmpty(this.CurrentSeason.Title)) {
						title += " - " + this.CurrentSeason.Title;
					}

					largeImage = this.CurrentSeason.RpcLargeImage;
				}

				// No large image from the season, use the one from the serie instead
				if (String.IsNullOrEmpty(largeImage)) {
					largeImage = currentSerie.RpcLargeImage;
				}
			}

			const string FIELD = "rpc_large_image";
			const string QUERY = "SELECT {0} FROM {1} WHERE {0} IS NOT NULL";

			List<string> largeImages = App.db.FetchSingleColumn(
				FIELD,
				String.Format(QUERY, FIELD, "serie") + " UNION " + String.Format(QUERY, FIELD, "season")
			);

			_view.Blackout(true);

			View.Window.DiscordRpc rpc = new View.Window.DiscordRpc(title, largeImage, largeImages);
			rpc.ShowDialog();

			_view.Blackout(false);

			// No changes were made
			if (!rpc.Saved) {
				return true;
			}

			// Save the large image for the selected serie, selected season or current season
			if (this._view.Level == Level.Serie) {
				this.SelectedSerie.RpcLargeImage = rpc.LargeImage;
				this.Model.SerieRepository.UpdateField(this.SelectedSerie.Id, "rpc_large_image", rpc.LargeImage);
			} else if (this._view.Level == Level.Season) {
				this.SelectedSeason.RpcLargeImage = rpc.LargeImage;
				this.Model.SeasonRepository.UpdateField(this.SelectedSeason.Id, "rpc_large_image", rpc.LargeImage);
			} else if (this._view.Level == Level.Episode) {
				this.CurrentSeason.RpcLargeImage = rpc.LargeImage;
				this.Model.SeasonRepository.UpdateField(this.CurrentSeason.Id, "rpc_large_image", rpc.LargeImage);
			}

			return true;
		}

		/// <summary>
		/// Reload status from the database then re-set them in the collection's combobox.
		/// </summary>
		public void RefreshStatus()
		{
			this.Model.LoadStatus();

			if (_view.Level < Level.Episode) {
				_view.SetSeriesAndSeasonsCombo(Settings.Default.RememberFilter ? Settings.Default.LastSelector : (int)Entity.DefaultStatus.All);
			}
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Change filepath for the selected episode item.
		/// </summary>
		/// <returns>True if the file was relocated, false otherwise (closing the dialog without selecting a file for example)</returns>
		private bool RelocateEpisode(Entity.Episode episode)
		{
			OpenFileDialog dlg = Tools.CreateOpenFileDialog("Any file|*.*|" + Constants.VIDEO_FILTER + '|' + Constants.BOOK_FILTER);

			// Open the file's directory if it still exists
			if (episode.File.Exists) {
				string containingDir = Tool.Path.ContainingPath(episode.Uri);

				if (Directory.Exists(containingDir)) {
					dlg.InitialDirectory = containingDir;
				}
			}

			if (_view.Level != Level.Episode || !(bool)dlg.ShowDialog()) {
				return false;
			}

			episode.Uri = dlg.FileName;
			this.UpdateSelectedEpisode("uri", episode.Uri);

			if (!File.Exists(episode.Uri)) {
				return false;
			}

			_view.SelectedItem.NoLinkedFile = false;
			this.SetSelectedEpisodeCover();

			return true;
		}

		/// <summary>
		/// Import a subtitle file for the selected episode.
		/// </summary>
		private void ImportSubtitleForSelectedEpisode()
		{
			OpenFileDialog dlg = Tools.CreateOpenFileDialog(Constants.SUBTITLE_FILTER);

			if (_view.Level != Level.Episode || !(bool)dlg.ShowDialog()) {
				return;
			}

			Tool.File episodeFile = this.SelectedEpisode.File;

			FileSystem.CopyFile(
				dlg.FileName,
				episodeFile.Path.Replace(episodeFile.Extension, Path.Extension(dlg.FileName)),
				UIOption.AllDialogs
			);

			_view.MainWindow.Notify(
				Constants.Notify.Info,
				Lang.Content("import", "Import"),
				String.Format(Lang.Text("fileImported", "File {0} imported!"), Path.LastSegment(dlg.FileName))
			);
		}

		/// <summary>
		/// Shows the overlay. NO SHIT SHERLOCK.
		/// </summary>
		/// <param name="text"></param>
		private void ShowOverlay(Entity.Episode episode, string fileOrUrl, int time, bool force=false)
		{
			if (!Settings.Default.ShowOverlay) {
				return;
			}

			string serieTitle = "";
			string seasonTitle = "";
			ushort episodesTotal = 0;

			if (_view.Level > 0) {
				serieTitle = this.CurrentSerie.Title;
			} else {
				serieTitle = this.SelectedSerie.Title;
			}

			if (this.CurrentSeason != null) {
				seasonTitle = this.CurrentSeason.Title;
				episodesTotal = this.CurrentSeason.EpisodesTotal;
			} else {
				Entity.Season season = _model.SeasonRepository.Find(episode.SeasonId);

				seasonTitle = season.Title;
				episodesTotal = season.EpisodesTotal;
			}

			string header = serieTitle + ", " + seasonTitle;
			string middle = episode.Title + " (" + episode.Number + "/" + (episodesTotal != 0 ? episodesTotal.ToString() : "?") + ")";
			string footer = fileOrUrl;

			Overlay overlay = new Overlay(header, middle, footer, time, force);
			overlay.Show();
		}

		/// <summary>
		/// Called when a video player has exited.
		/// Update the Discord RPC status if enabled.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PlayerExited(object sender, EventArgs e)
		{
			// Reset RPC
			if (Settings.Default.RPC_Enabled) {
				Tool.DiscordRpc.UpdateRpc(null, null);
			}

			if (Settings.Default.ContinueOnClose) {
				System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() => {
					this.Continue();
				}));
			}
		}

		/// <summary>
		/// Play an episode file.
		/// Also update the Discord RPC status if enabled.
		/// </summary>
		/// <param name="episode">Episode instance</param>
		/// <param name="player">Specifying this can be used to override Settings.Default.Player_Prefered</param>
		private void PlayEpisodeFile(Entity.Episode episode, string player=null)
		{
			Tool.File episodeFile = episode.File;

			if (!episodeFile.Exists) {
				_view.MainWindow.Notify(Constants.Notify.Warning, "Attention", Lang.Text("fileDoesNotExists") + ": \"" + episodeFile.Path + '"');

				return;
			}

			this.ShowOverlay(episode, Path.LastSegment(episode.Uri), 2300, true);

			bool episodeIsVideo = episodeFile.IsVideo;

			// Check if file format is accepted by the player
			if (episodeIsVideo) {
				switch (player == null ? Settings.Default.PreferedPlayer : player) {
					case "VLC":
						new Tool.Player.VLC(this.PlayerExited).Play(episode.Uri);
					break;
					case "MPC-HC":
						new Tool.Player.MPC_HC(this.PlayerExited).Play(episode.Uri);
					break;
					case "mpv":
						new Tool.Player.mpv(this.PlayerExited).Play(episode.Uri);
					break;
					default:
						new Tool.VideoPlayer(this.PlayerExited).StartProcess(episode.Uri);
					break;
				}
			} else if (episodeFile.IsBook) {
				new Tool.VideoPlayer(this.PlayerExited).StartProcess(episode.Uri);
			} else { // Call the file to use the default program associated with this format
				new Tool.VideoPlayer(this.PlayerExited).StartProcess(episode.Uri);
			}

			// Mark the episode as watched
			if (!episode.Watched && Settings.Default.AutoMarkAsWatched) {
				this.SetEpisodeWatched(episode, true);
			}

			this.UpdateRpcStatusForEpisode(episode);
			this.lastPlayedEpisodeNumber = episode.Number;

			// Now that the episode is playing starts generating its thumbnail for mkv/webm
			if (Settings.Default.GenerateThumbOnLaunch && episode.Cover == null && episodeIsVideo && File.Exists(Settings.Default.FFmpeg)) {
				ThumbnailGenerator generator = new ThumbnailGenerator(_view.MainWindow, episode.Uri, true);

				if (generator.Thumbnail == null) {
					return;
				}

				episode.Cover = generator.Thumbnail;
				this.UpdateEpisodeEntity(episode, "Cover");

				this.Refresh();
			}
		}

		/// <summary>
		/// Play an episode for its URL.
		/// </summary>
		/// <param name="episode"></param>
		/// <param name="player"></param>
		private void PlayEpisodeUrl(Entity.Episode episode, string player=null)
		{
			if (!episode.IsUrl) {
				return;
			}

			this.ShowOverlay(episode, episode.Uri, 2300, true);

			switch (player == null ? Settings.Default.PreferedPlayer : player) {
				case "VLC": // Streaming not implemented yet
					new Tool.VideoPlayer(this.PlayerExited).StartProcess(episode.Uri);
				break; // Streaming not implemented yet
				case "MPC-HC":
					new Tool.VideoPlayer(this.PlayerExited).StartProcess(episode.Uri);
				break;
				case "mpv":
					new Tool.Player.mpv(this.PlayerExited).Stream(episode.Uri);
				break;
				default:
					new Tool.VideoPlayer(this.PlayerExited).StartProcess(episode.Uri);
				break;
			}

			// Mark the episode as watched
			if (!episode.Watched && Settings.Default.AutoMarkAsWatched) {
				this.SetEpisodeWatched(episode, true);
			}

			this.UpdateRpcStatusForEpisode(episode);
			this.lastPlayedEpisodeNumber = episode.Number;
		}

		/// <summary>
		/// Update the RPC status for a playing episode.
		/// </summary>
		/// <param name="episode"></param>
		private void UpdateRpcStatusForEpisode(Entity.Episode episode)
		{
			// Update Discord RPC status
			if (Settings.Default.RPC_Enabled) {
				Entity.Serie serie = null;
				Entity.Season season = null;

				// Get serie and season depending on the view level
				if (_view.Level == Level.Episode) {
					serie = this.CurrentSerie;
					season = this.CurrentSeason;
				} else if (_view.Level == Level.Season) {
					serie = this.CurrentSerie;
					season = this.SelectedSeason;
				} else if (_view.Level == Level.Serie) {
					serie = this.SelectedSerie;
					season = this.Model.SeasonRepository.GetOneById(episode.SeasonId);
				}

				ushort total = season.EpisodesTotal;
				ushort owned = season.EpisodesOwned;
				ushort count = (total > owned ? total : owned);
				string totalStr = count.ToString();
				int length = totalStr.Length;
				string watchingTitle = serie.Title;
				string episodeCounts = episode.Number.ToString().PadLeft(length, '0');

				if (season.Title != watchingTitle && serie.NumberOfSeasons > 1 && !String.IsNullOrEmpty(season.Title)) {
					watchingTitle += " - " + season.Title;
				}

				if (count > 0) {
					episodeCounts += "/" + totalStr.PadLeft(length, '0');
				}

				string largeImage = season.RpcLargeImage;

				// No large image for that season, use the one from the serie instead
				if (String.IsNullOrEmpty(largeImage)) {
					largeImage = serie.RpcLargeImage;
				}

				// Update Discord RPC
				Tool.DiscordRpc.UpdateRpc(
					watchingTitle,
					Tools.UpperFirst(Lang.Content("episode")) + " " + episodeCounts,
					this.DateTimeToTimestamp(DateTime.UtcNow),
					largeImage, Settings.Default.RPC_LargeText
				);
			}
		}

		/// <summary>
		/// Remove cover from the selected item.
		/// </summary>
		private void RemoveSelectedItemCover()
		{
			// Set empty cover in memory and db
			switch (_view.Level) {
				case Level.Serie: { // Serie
					this.SelectedSerie.Cover = null;

					_model.SerieRepository.Update(this.SelectedSerie, "Cover");
				}
				break;
				case Level.Season: { // Season
					_view.SelectedItem.SetCover(this.SelectedSeason.ParentCover);
					this.SelectedSeason.Cover = null;

					_model.SeasonRepository.Update(this.SelectedSeason, "Cover");
				}
				break;
				case Level.Episode: { // Episode
					this.SetSelectedEpisodeCover();

					this.SelectedEpisode.Cover = null;
					this.UpdateSelectedEpisode("Cover");
				}
				break;
			}
		}

		/// <summary>
		/// Open a file selector for changing the select item's cover.
		/// </summary>
		private void ChangeSelectedItemCover()
		{
			this.Blackout(true);

			OpenFileDialog dlg = Tools.CreateOpenImageDialog();

			if ((bool)dlg.ShowDialog()) {
				using (Tool.Cover coverTool = new Tool.Cover()) {
					this.SetItemCover(_view.SelectedIndex, coverTool.Create(dlg.FileName, false));
				}
			}

			this.Blackout(false);
		}

		/// <summary>
		/// Edit the selectd item.
		/// Save new data then reload view.
		/// </summary>
		private void EditRequested()
		{
			this.Blackout(true);

			switch (_view.Level) {
				case Level.Serie: {
					Entity.Serie selectedSerie = this.SelectedSerie;

					SerieEdit adda = new SerieEdit(selectedSerie, _model.Status);
					adda.ShowDialog();

					if (adda.Greenlight) {
						selectedSerie.Title = adda.SerieTitle;
						selectedSerie.StatusId = adda.StatusId;

						// Set new cover if needed
						if (Path.LastSegment(adda.Cover) != selectedSerie.Cover) {
							using (Tool.Cover coverTool = new Tool.Cover()) {
								selectedSerie.Cover = coverTool.Create(adda.Cover, adda.DeleteCover);
							}
						}

						_model.SerieRepository.Update(selectedSerie);

						// Set genres
						_model.SerieRepository.DeleteAllGenresForSerie(selectedSerie.Id);
						_model.SerieRepository.AddGenresForSerie(selectedSerie.Id, adda.Genres);

						// Update the item in view
						_view.SelectedItem.Update(selectedSerie);
					}
				}
				break;
				case Level.Season: {
					Entity.Season selectedSeason = this.SelectedSeason;
					string previousGrouping = (selectedSeason.Grouping != null ? selectedSeason.Grouping : "");

					SeasonEdit edtS = new SeasonEdit(selectedSeason, _model.Status);
					edtS.SeasonTitle = selectedSeason.Title;
					edtS.ShowDialog();

					if (edtS.Greenlight) {
						selectedSeason.Title = edtS.SeasonTitle;
						selectedSeason.StatusId = edtS.StatusId;
						selectedSeason.Number = edtS.Number;
						selectedSeason.EpisodesTotal = edtS.EpisodesCount;
						selectedSeason.StudioId = edtS.StudioId;
						selectedSeason.Seasonal = edtS.Seasonal;
						selectedSeason.Year = edtS.Year;
						selectedSeason.Month = edtS.Month;
						selectedSeason.Day = edtS.Day;
						selectedSeason.Type = edtS.Type;
						selectedSeason.Source = edtS.Source;
						selectedSeason.WideEpisode = edtS.WideEpisodes;
						selectedSeason.Grouping = edtS.Grouping;

						// Set cover
						if (Path.LastSegment(edtS.Cover) != selectedSeason.Cover) {
							using (Tool.Cover tool = new Tool.Cover()) {
								selectedSeason.Cover = tool.Create(edtS.Cover, edtS.DeleteImage);
							}
						}

						_model.SeasonRepository.Update(selectedSeason);

						// Grouping changed, refresh list
						if (selectedSeason.Grouping != previousGrouping) {
							this.Refresh();
						} else { // Just update the item in view
							_view.SelectedItem.Update(selectedSeason);
						}
					}
				}
				break;
				case Level.Episode: {
					Entity.Episode selectedEpisode = this.SelectedEpisode;

					EpisodeEdit edite = new EpisodeEdit(selectedEpisode, this.CurrentSeasonIsManga);
					edite.ShowDialog();

					if (edite.Greenlight) {
						selectedEpisode.Title = edite.EpisodeTitle;
						selectedEpisode.Number = edite.Number;
						selectedEpisode.Watched = edite.Watched;

							// Add file or URL
							selectedEpisode.Uri = edite.FileOrUrl;

							// Set cover
							if (Path.LastSegment(edite.Cover) != selectedEpisode.Cover) {
							using (Tool.Cover tool = new Tool.Cover()) {
								selectedEpisode.Cover =  tool.Create(edite.Cover, edite.DeleteImage);
							}
						}

						this.UpdateEpisodeEntity(selectedEpisode);

						// Update the item in view
						_view.SelectedItem.Update(selectedEpisode);
					}
				}
				break;
			}

			this.Blackout(false);
		}

		/// <summary>
		/// Change the status for all the selected items.
		/// </summary>
		private void ChangeSelectedItemsStatus(Entity.DefaultStatus status)
		{
			// Only one item selected
			if (_view.SelectedIndexes.Count == 1) {
				this.ChangeSelectedItemStatus(status);

				return;
			}

			List<int> ids = new List<int>();
			int statusId = (int)status;

			// Multiple selected items
			foreach (int index in _view.SelectedIndexes) {
				_view.GetItem(index).Status = status;

				if (_view.Level == Level.Serie) {
					_model.Series[index].StatusId = statusId;

					ids.Add(_model.Series[index].Id);
				} else if (_view.Level == Level.Season) {
					_model.Seasons[index].StatusId = statusId;

					ids.Add(_model.Seasons[index].Id);
				}
			}

			if (_view.Level == Level.Serie) {
				_model.SerieRepository.UpdateFieldIn(ids, "status_id", statusId.ToString());
			} else if (_view.Level == Level.Season) {
				_model.SeasonRepository.UpdateFieldIn(ids, "status_id", statusId.ToString());
			}
		}

		/// <summary>
		/// Change the user status for all the selected items.
		/// </summary>
		private void ChangeSelectedItemsStatus(Entity.UserStatus status)
		{
			// Only one item selected
			if (_view.SelectedIndexes.Count == 1) {
				this.ChangeSelectedItemStatus(status);

				return;
			}

			List<int> ids = new List<int>();
			Entity.UserStatus userStatus = _model.StatusRepository.Find(status.Id);

			// Multiple selected items
			foreach (int index in _view.SelectedIndexes) {
				if (_view.Level == Level.Serie) {
					_model.Series[index].StatusId = status.Id;
					_model.Series[index].UserStatus = userStatus;

					// Set view
					if (_model.Series[index].UserStatus.Type == 0) {
						_view.GetItem(index).StringSmallStatus = _model.Series[index].UserStatus.Text;
					} else {
						_view.GetItem(index).StringBigStatus = _model.Series[index].UserStatus.Text;
					}

					ids.Add(_model.Series[index].Id);
				} else if (_view.Level == Level.Season) {
					_model.Seasons[index].StatusId = status.Id;
					_model.Seasons[index].UserStatus = userStatus;

					// Set view
					if (_model.Seasons[index].UserStatus.Type == 0) {
						_view.GetItem(index).StringSmallStatus = _model.Seasons[index].UserStatus.Text;
					} else {
						_view.GetItem(index).StringBigStatus = _model.Seasons[index].UserStatus.Text;
					}

					ids.Add(_model.Seasons[index].Id);
				}
			}

			if (_view.Level == Level.Serie) {
				_model.SerieRepository.UpdateFieldIn(ids, "status_id", status.Id.ToString());
			} else if (_view.Level == Level.Season) {
				_model.SeasonRepository.UpdateFieldIn(ids, "status_id", status.Id.ToString());
			}
		}

		/// <summary>
		/// Change the status of the currently selected item.
		/// </summary>
		/// <param name="status"></param>
		private void ChangeSelectedItemStatus(Entity.DefaultStatus status)
		{
			int statusId = (int)status;

			switch (_view.Level) {
				case Level.Serie: // Series
				{
					_view.GetItem(_view.SelectedIndex).Status = status;
					this.SelectedSerie.StatusId = statusId;

					// If the serie has only one season, update status of the child season
					if (this.SelectedSerie.NumberOfSeasons == 1) {
						List<Entity.Season> seasons = _model.SeasonRepository.GetAllBySerie(this.SelectedSerie.Id);
						_model.SeasonRepository.UpdateField(seasons[0].Id, "status_id", statusId);
					}

					// Update serie
					_model.SerieRepository.UpdateField(this.SelectedSerie.Id, "status_id", statusId);
				}
				break;
				case Level.Season: // Seasons
				{
					this.SelectedSeason.StatusId = statusId;
					_view.GetItem(_view.SelectedIndex).Status = status;

					Entity.Serie currentSerie = this.CurrentSerie;

					// If the serie has only one season, update status of the parent serie
					if (currentSerie != null && currentSerie.NumberOfSeasons == 1) {
						currentSerie.StatusId = (int)status;
						_model.SerieRepository.UpdateField(CurrentSerie.Id, "status_id", statusId);
					}

					// Update season
					_model.SeasonRepository.UpdateField(this.SelectedSeason.Id, "status_id", statusId);
				}
				break;
			}
		}

		/// <summary>
		/// Change status for the selected item.
		/// </summary>
		/// <param name="status"></param>
		private void ChangeSelectedItemStatus(Entity.UserStatus status)
		{
			if (status == null) {
				return;
			}

			switch (_view.Level) {
				case Level.Serie: {
					this.SelectedSerie.StatusId = status.Id;
					this.SelectedSerie.UserStatus = _model.StatusRepository.Find(status.Id);
					_model.SerieRepository.Update(this.SelectedSerie);

					// Set view
					if (this.SelectedSerie.UserStatus.Type == 0) {
						_view.SelectedItem.StringSmallStatus = this.SelectedSerie.UserStatus.Text;
					} else {
						_view.SelectedItem.StringBigStatus = this.SelectedSerie.UserStatus.Text;
					}
				}
				break;
				case Level.Season: {
					this.SelectedSeason.StatusId = status.Id;
					this.SelectedSeason.UserStatus = _model.StatusRepository.Find(status.Id);
					_model.SeasonRepository.Update(this.SelectedSeason);

					// Set view
					if (this.SelectedSeason.UserStatus.Type == 0) {
						_view.SelectedItem.StringSmallStatus = this.SelectedSeason.UserStatus.Text;
					} else {
						_view.SelectedItem.StringBigStatus = this.SelectedSeason.UserStatus.Text;
					}
				}
				break;
			}
		}

		/// <summary>
		/// Change covers for all the selected items.
		/// </summary>
		private void ChangeMultipleCovers()
		{
			if (_view.IsEmpty) {
				_view.MainWindow.Notify(
					Constants.Notify.Warning,
					Lang.ACTION_CANCELED,
					Lang.Text("noElements", "No element(s) to apply this action to")
				);

				return;
			}

			this.Blackout(true);

			OpenFileDialog dlg = Tools.CreateOpenImageDialog();

			if ((bool)dlg.ShowDialog()) {
				using (Tool.Cover coverTool = new Tool.Cover()) {
					string filename = coverTool.Create(dlg.FileName, false);

					foreach (int index in _view.SelectedIndexes) {
						this.SetItemCover(index, filename);
					}
				}
			}

			this.Blackout(false);
		}

		/// <summary>
		/// Set Cover for an item using its index.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="filename"></param>
		private void SetItemCover(int index, string filename)
		{
			_view.GetItem(index).SetCover(filename);

			switch (_view.Level) {
				case Level.Serie: { // Series
					this.Model.Series[index].Cover = filename; // We need that for the parent cover
					_model.SerieRepository.UpdateField(this.Model.Series[index].Id, "cover", filename);
				}
				break;
				case Level.Season: { // Seasons
					this.Model.Seasons[index].Cover = filename; // We need that for the parent cover
					_model.SeasonRepository.UpdateField(this.Model.Seasons[index].Id, "cover", filename);
				}
				break;
				case Level.Episode: { // Episodes
					_model.EpisodeRepository.UpdateField(this.Model.Episodes[index].Id, "cover", filename);
				}
				break;
			}
		}

		/// <summary>
		/// Remove covers for all the selected items.
		/// </summary>
		private void RemoveMultipleCovers()
		{
			if (_view.IsEmpty) {
				_view.MainWindow.Notify(
					Constants.Notify.Warning,
					Lang.ACTION_CANCELED,
					Lang.Text("noElements", "No element(s) to apply this action to")
				);

				return;
			}

			string table = "";

			if (_view.Level == Level.Serie) {
				table = Entity.Serie.TABLE;
			} else if (_view.Level == Level.Season) {
				table = Entity.Season.TABLE;
			} else if (_view.Level == Level.Episode) {
				table = Entity.Episode.TABLE;
			}

			App.db.Execute(String.Format(
				"UPDATE {0} SET cover = NULL WHERE id IN ({1});",
				table, Tools.InClause(this.GetSelectedIds())
			));
		}

		/// <summary>
		/// Open the EpisodesRelocator to relocate episodes in the selected series or seasons.
		/// </summary>
		private void RelocateEpisodes()
		{
			if (_view.SelectedIndexes.Count < 1) {
				_view.MainWindow.Notify(Constants.Notify.Info, "Error", "No selected series or seasons.");

				return;
			}

			List<int> selectedIds = new List<int>();
			List<Entity.Episode> episodes = null;

			if (_view.Level == Level.Serie) { // Series
				foreach (int selectedIndex in _view.SelectedIndexes) {
					selectedIds.Add(_model.Series[selectedIndex].Id);
				}

				episodes = _model.EpisodeRepository.GetAllBySeries(selectedIds);
			} else if (_view.Level == Level.Season) { // Seasons
				foreach (int selectedIndex in _view.SelectedIndexes) {
					selectedIds.Add(_model.Seasons[selectedIndex].Id);
				}

				episodes = _model.EpisodeRepository.GetAllBySeasons(selectedIds);
			} else { // Episodes
				episodes = this.CurrentEpisodes;
			}

			if (episodes.Count < 1) {
				_view.MainWindow.Notify(Constants.Notify.Info, "Error", "The selected series or seasons doesn't have any episodes.");

				return;
			}

			View.Window.EpisodesRelocator episodeRelocator = new View.Window.EpisodesRelocator(episodes);
			episodeRelocator.ShowDialog();

			if (!episodeRelocator.Validated) {
				return;
			}

			// Update episodes links
			foreach (Entity.Episode relocatedEpisode in episodeRelocator.Episodes) {
				_model.EpisodeRepository.UpdateField(relocatedEpisode.Id, "uri", relocatedEpisode.Uri);
			}

			this.Refresh();
		}

		/// <summary>
		/// Get the list of all the selected item's IDs.
		/// </summary>
		/// <returns></returns>
		private List<int> GetSelectedIds()
		{
			List<int> ids = new List<int>();

			if (_view.Level == Level.Serie) {
				foreach (int index in _view.SelectedIndexes) {
					ids.Add(_model.Series[index].Id);
				}
			} else if (_view.Level == Level.Season) {
				foreach (int index in _view.SelectedIndexes) {
					ids.Add(_model.Seasons[index].Id);
				}
			} else if (_view.Level == Level.Episode) {
				foreach (int index in _view.SelectedIndexes) {
					ids.Add(_model.Episodes[index].Id);
				}
			}

			return ids;
		}

		/// <summary>
		/// Mark all current episodes as watched or not watched.
		/// </summary>
		/// <param name="watched"></param>
		private void MarkAllAsWatched(bool watched)
		{
			if (_view.IsEmpty) {
				_view.MainWindow.Notify(
					Constants.Notify.Warning,
					Lang.ACTION_CANCELED,
					Lang.Text("noElements", "No element(s) to apply this action to")
				);

				return;
			}

			List<int> ids = new List<int>();

			foreach (int index in _view.SelectedIndexes) {
				_model.Episodes[index].Watched = watched;
				_view.GetItem(index).MarkItemAsWatched = watched;
				ids.Add(_model.Episodes[index].Id);
			}

			_model.EpisodeRepository.UpdateFieldIn(ids, "watched", watched ? "1" : "0");
		}

		/// <summary>
		/// Update the Watched/Owned/Total episode counters.
		/// </summary>
		/// <param name="added"></param>
		/// <param name="watched"></param>
		private void UpdateEpisodesCounters(ushort added, ushort watched)
		{
			Entity.Serie currentSerie = this.CurrentSerie;

			currentSerie.EpisodesOwned += added;
			currentSerie.EpisodesViewed += watched;
			this.CurrentSeason.EpisodesOwned += added;
			this.CurrentSeason.EpisodesViewed += watched;
		}

		/// <summary>
		/// Remove the episode with the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		private void RemoveFakeEpisodeWithNumber(ushort number)
		{
			for (ushort i = 0; i < this.CurrentEpisodes.Count; i++) {
				if (this.CurrentEpisodes[i].Number != number || !this.CurrentEpisodes[i].Fake) {
					continue;
				}

				this.CurrentEpisodes.RemoveAt(i);
				_view.List.RemoveAt(i);

				return;
			}
		}

		/// <summary>
		/// Set cover from thumb or the parent cover for the selected episode.
		/// </summary>
		private void SetSelectedEpisodeCover()
		{
			// Set the video thumb or the parent cover depending on the configuration
			if (Settings.Default.TileVideoThumbs && this.SelectedEpisode.File.IsCompatibleWithThumb) {
				_view.SelectedItem.SetCover(this.SelectedEpisode.Thumbnail);
			} else {
				_view.SelectedItem.SetCover(this.SelectedEpisode.ParentCover);
			}
		}

		/// <summary>
		/// Show a dialog for generating a thumbnail of the current video file.
		/// </summary>
		private void GenerateThumbnailDialog()
		{
			Entity.Episode selectedEpisode = this.SelectedEpisode;

			ThumbnailGenerator generator = new ThumbnailGenerator(_view.MainWindow, selectedEpisode.Uri);

			if (generator.Thumbnail == null) {
				return;
			}

			this.SelectedEpisode.Cover = generator.Thumbnail;
			this.UpdateSelectedEpisode("Cover");
		
			_view.SelectedItem.Update(selectedEpisode);
		}

		/// <summary>
		/// Search a cover using online services.
		/// </summary>
		private void SearchOnlineCover()
		{
			string searchText = _view.SelectedItem.Title;

			// Add serie or season name to the search text
			if (_view.Level == Level.Season && !_view.ListIsSeasonalView) {
				searchText = this.CurrentSerie.Title + " - " + searchText;
			} else if (_view.Level == Level.Episode) {
				searchText = this.CurrentSerie.Title + " - " + this.CurrentSeason.Title + " - " + searchText;
			}

			View.Window.OnlineFetch selector = new View.Window.OnlineFetch(searchText);

			if (selector.SelectedCover == null || !File.Exists(selector.SelectedCover)) {
				return;
			}

			using (Tool.Cover coverTool = new Tool.Cover()) {
				this.SetItemCover(_view.SelectedIndex, coverTool.Create(selector.SelectedCover, true));
			}
		}

		/// <summary>
		/// Move all the selected seasons into another serie.
		/// </summary>
		private void MoveSeasons()
		{
			Entity.Serie serie = this.CurrentSerie;
			List<Entity.Serie> series;

			if (serie != null) {
				series = _model.SerieRepository.GetAllIdAndTitles(serie.Id);
			} else {
				series = _model.SerieRepository.GetAll();
			}

			ItemSelector selector = new ItemSelector(series).Open();

			if (!selector.Greenlight) {
				return;
			}

			foreach (int index in _view.SelectedIndexes) {
				_model.Seasons[index].SerieId = selector.SelectedId;
				_model.SeasonRepository.Update(_model.Seasons[index], "SerieId");
			}

			_view.MainWindow.Notify(
				Constants.Notify.Info,
				Lang.SUCCESS,
				String.Format(Lang.Text("movedSeasons"), _view.SelectedIndexes.Count, selector.SelectedTitle)
			);

			this.Refresh();
		}

		/// <summary>
		/// Move all the selected episodes into another season.
		/// </summary>
		private void MoveEpisodes()
		{
			List<Entity.Season> seasons = _model.SeasonRepository.GetAllBySerie(this.CurrentSerie.Id);

			ItemSelector selector = new ItemSelector(seasons).Open();

			if (!selector.Greenlight) {
				return;
			}

			foreach (int index in _view.SelectedIndexes) {
				_model.Episodes[index].SeasonId = selector.SelectedId;
				_model.EpisodeRepository.Update(_model.Episodes[index], "SeasonId");
			}

			_view.MainWindow.Notify(
				Constants.Notify.Info,
				Lang.SUCCESS,
				String.Format(Lang.Text("movedEpisodes"), _view.SelectedIndexes.Count, selector.SelectedTitle)
			);

			this.Refresh();
		}

		/// <summary>
		/// Convert a DateTime object into a timestamp.
		/// </summary>
		/// <param name="dt"></param>
		/// <returns></returns>
		private long DateTimeToTimestamp(DateTime dt)
		{
			return (dt.Ticks - 621355968000000000) / 10000000;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public Model.Collection Model
		{
			get { return this._model; }
		}

		public Entity.Serie CurrentSerie
		{
			get
			{
				if (_view.Level < Level.Season) return null;

				return _model.CurrentSerie;
			}
		}

		public Entity.Serie SelectedSerie
		{
			get
			{
				if (_view.Level != Level.Serie) return null;

				return _model.Series[_view.SelectedIndex];
			}
		}

		/// <summary>
		/// Get the selected or current serie depending on the view level.
		/// </summary>
		public Entity.Serie ActualSerie
		{
			get
			{
				if (_view.Level == Level.Serie) {
					return this.SelectedSerie;
				}

				return this.CurrentSerie;
			}
		}

		public Entity.Season SelectedSeason
		{
			get
			{
				if (_view.Level != Level.Season) return null;

				return _model.Seasons[_view.SelectedIndex];
			}
		}

		public Entity.Episode SelectedEpisode
		{
			get
			{
				if (_view.Level != Level.Episode || _view.SelectedIndex == -1) {
					return null;
				}

				return _model.Episodes[_view.SelectedIndex];
			}
		}

		/// <summary>
		/// Returns seasons for the selected serie.
		/// If they are not loaded yet, do it.
		/// </summary>
		public List<Entity.Season> SelectedSeasons
		{
			get
			{
				if (_model.Seasons == null) {
					_model.LoadSeasonsBySerieId(this.SelectedSerie.Id);
				}

				return _model.Seasons;
			}
		}

		public Entity.Season CurrentSeason
		{
			get
			{
				if (_view.Level != Level.Episode) return null;

				// This is needed when we go directly from serie to episode level as the season isn't loaded
				if (_model.CurrentSeason == null) {
					_model.CurrentSeason = Repository.Season.Instance.GetOneBySerie(this.CurrentSerie.Id);
				}

				return _model.CurrentSeason;
			}
		}

		public List<Entity.Episode> CurrentEpisodes
		{
			get { return _model.Episodes; }
			set { _model.Episodes = value; }
		}

		public int BackSelectedStatusId
		{
			get { return this.backSelectedStatusId; }
			set { this.backSelectedStatusId = value; }
		}

		public int NextSelectedStatusId
		{
			get { return this.nextSelectedStatusId; }
			set { this.nextSelectedStatusId = value; }
		}

		public int SerieSelectedStatusId
		{
			get { return this.serieSelectedStatusId; }
			set { this.serieSelectedStatusId = value; }
		}

		public int LastVisitedSerieIndex
		{
			get { return this.lastVisitedSerieIndex; }
		}

		public int LastVisitedSeasonIndex
		{
			get { return this.lastVisitedSeasonIndex; }
		}

		public ushort LastEpisodeNumber
		{
			get
			{
				int count = this.CurrentEpisodes.Count;

				if (count == 0) {
					return 0;
				}

				return this.CurrentEpisodes[count - 1].Number;
			}
		}

		public List<Entity.UserStatus> StatusList
		{
			get { return _model.Status; }
		}

		/// <summary>
		/// Check if the current season's type is "Manga".
		/// </summary>
		public bool CurrentSeasonIsManga
		{
			get { return this.CurrentSeason.Type == Constants.Type.Manga; }
		}

		#endregion Accessor
	}
}
