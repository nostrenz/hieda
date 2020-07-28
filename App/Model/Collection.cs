using System.Collections.Generic;

namespace Hieda.Model
{
	public class Collection
	{
		private List<Entity.Serie> series = new List<Entity.Serie>();
		private List<Entity.Season> seasons = new List<Entity.Season>();
		private List<Entity.Episode> episodes = new List<Entity.Episode>();

		private Entity.Serie currentSerie = null;
		private Entity.Season currentSeason = null;

		private List<Entity.UserStatus> status = new List<Entity.UserStatus>();

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Add the given serie in DB and in the 'series' list<>.
		/// </summary>
		/// <param name="serie"></param>
		/// <returns>
		/// Created serie Id
		/// </returns>
		public int Add(Entity.Serie serie)
		{
			// Add to database
			serie.Id = this.SerieRepository.Add(serie);

			// Add to list
			this.series.Add(serie);

			return serie.Id;
		}

		public int Add(Entity.Season season)
		{
			// Add to database
			season.Id = this.SeasonRepository.Add(season);

			// Add to list
			this.seasons.Add(season);

			return season.Id;
		}

		public int Add(Entity.Episode episode)
		{
			// Add to database
			episode.Id = this.EpisodeRepository.Add(episode);

			// Add to list
			this.episodes.Add(episode);

			return episode.Id;
		}

		/// <summary>
		/// Fill series list using some parameters like search title and selected status.
		/// </summary>
		/// <param name="selector"></param>
		/// <param name="search"></param>
		public void FillSeries(string searchQuery=null, string searchMode=null, string searchField=null, int statusId=(int)Entity.DefaultStatus.All)
		{
			this.SetSeries(
				this.SerieRepository.GetAllForSearch(searchQuery, searchMode, searchField, statusId)
			);
		}

		/// <summary>
		/// Fill seasons list using some parameters like search title and selected status.
		/// </summary>
		/// <param name="serieIndex"></param>
		/// <param name="selector"></param>
		public void FillSeasons(int statusId=(int)Entity.DefaultStatus.All, string types=null, string orderBy="number")
		{
			this.SetSeasons(
				this.SeasonRepository.GetAllBySerieAndStatus(this.currentSerie.Id, statusId, types, orderBy)
			);
		}

		/// <summary>
		/// Variant of FillSeasons() only used for the LabelList.
		/// </summary>
		/// <param name="selector"></param>
		public void FillSeasonsForLabelList(string searchQuery=null, string searchMode=null, string searchField=null, int statusId=(int)Entity.DefaultStatus.All, string types = null)
		{
			this.SetSeasons(
				this.SeasonRepository.GetAllOrderedByYearAndSeasonal(searchQuery, searchMode, searchField, statusId, types)
			);
		}

        /// <summary>
        /// Fill episodes list using some parameters like search title and selected status.
        /// </summary>
        /// <param name="serieIndex"></param>
        /// <param name="seasonIndex"></param>
        /// <param name="selector"></param>
        public void FillEpisodes(int serieIndex, int seasonIndex, int statusId=(int)Entity.DefaultStatus.All)
		{
			this.episodes.Clear();

			if (statusId == (int)Entity.DefaultStatus.All) {
				if (this.currentSeason != null) {
					this.episodes = this.EpisodeRepository.GetAllBySeason(this.currentSeason.Id);
				} else {
					this.episodes = this.EpisodeRepository.GetAllBySerie(this.currentSerie.Id);
				}
			} else {
				bool watched = (statusId == (int)Entity.DefaultStatus.Watched);

				if (this.currentSeason != null) {
					this.episodes = this.EpisodeRepository.GetAllWatchedForSeason(this.currentSeason.Id, watched);
				} else {
					this.episodes = this.EpisodeRepository.GetAllWatchedForSerie(this.currentSerie.Id, watched);
				}
			}
		}

		/// <summary>
		/// Delete a serie and every seasons and episodes in it.
		/// </summary>
		/// <param name="serieIndex"></param>
		public void DeleteSerie(int serieIndex)
		{
			this.SerieRepository.Erase(this.series[serieIndex].Id);
		}

		/// <summary>
		/// Delete a season and every episodes in it.
		/// </summary>
		/// <param name="serieIndex"></param>
		/// <param name="seasonIndex"></param>
		public void DeleteSeason(int seasonIndex)
		{
			this.SeasonRepository.Erase(this.seasons[seasonIndex].Id);
		}

		/// <summary>
		/// Delete an episode.
		/// </summary>
		/// <param name="episodeIndex"></param>
		public void DeleteEpisode(int episodeIndex)
		{
			// Remove from database
			this.EpisodeRepository.Delete(this.episodes[episodeIndex].Id);

			// Remove from memory
			this.episodes.RemoveAt(episodeIndex);
		}

		/// <summary>
		/// Delete multiple series.
		/// </summary>
		/// <param name="indexes"></param>
		public void DeleteSeries(List<int> indexes)
		{
			List<int> ids = new List<int>();

			foreach (int index in indexes) {
				ids.Add(this.series[index].Id);
			}

			// Remove from database
			this.SerieRepository.EraseMultiple(ids);
		}

		/// <summary>
		/// Delete multiple seasons.
		/// </summary>
		/// <param name="indexes"></param>
		public void DeleteSeasons(List<int> indexes)
		{
			List<int> ids = new List<int>();

			foreach (int index in indexes) {
				ids.Add(this.seasons[index].Id);
			}

			// Remove from database
			this.SeasonRepository.EraseMultiple(ids);
		}

		/// <summary>
		/// Delete multiple episodes.
		/// </summary>
		/// <param name="indexes"></param>
		public void DeleteEpisodes(List<int> indexes)
		{
			List<int> ids = new List<int>();

			foreach (int index in indexes) {
				ids.Add(this.episodes[index].Id);
			}

			// Remove from database
			this.EpisodeRepository.DeleteMultiple(ids);
		}

		/// <summary>
		/// Load personnal status from db.
		/// </summary>
		public void LoadStatus()
		{
			this.Status = this.StatusRepository.GetAll();
		}

		/// <summary>
		/// Load series in the model from a query using the serie repository.
		/// </summary>
		/// <param name="query"></param>
		public void LoadSeriesFromQuery(string query)
		{
			this.SetSeries(
				Repository.Serie.Instance.FillListFromQuery(query)
			);
		}

		/// <summary>
		/// Load seasons in the model from a query using the season repository.
		/// </summary>
		/// <param name="query"></param>
		public void LoadSeasonsFromQuery(string query)
		{
			this.SetSeasons(
				Repository.Season.Instance.FillListFromQuery(query)
			);
		}

		/// <summary>
		/// Load seasons using their serie Id.
		/// </summary>
		/// <param name="serieId"></param>
		public void LoadSeasonsBySerieId(int serieId)
		{
			this.seasons = this.SeasonRepository.GetAllBySerie(serieId);
		}

		/// <summary>
		/// Try to find a UserStatus corresponding to the given status Id.
		/// </summary>
		/// <param name="statusId"></param>
		/// <returns></returns>
		public Entity.UserStatus FindUserStatus(int statusId)
		{
			foreach (Entity.UserStatus userStatus in this.status) {
				if (userStatus.Id == statusId) {
					return userStatus;
				}
			}

			return null;
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Set UserStatus for all series in list.
		/// If a status cannot be found, reset it to null.
		/// </summary>
		private void SetSeriesUserStatus()
		{
			foreach (Entity.Serie serie in this.series) {
				if (serie.StatusId <= 0) {
					continue;
				}

				serie.UserStatus = this.FindUserStatus(serie.StatusId);
			}
		}

		/// <summary>
		/// Clear stored series, current serie and current season.
		/// </summary>
		private void ClearSeries()
		{
			this.series.Clear();

			this.currentSerie = null;
			this.currentSeason = null;
		}

		/// <summary>
		/// Clear stored series, current serie and current season.
		/// </summary>
		private void ClearSeasons()
		{
			this.seasons.Clear();

			this.currentSeason = null;
		}

		/// <summary>
		/// Store series in the model.
		/// </summary>
		/// <param name="series"></param>
		private void SetSeries(List<Entity.Serie> series)
		{
			this.ClearSeries();

			this.series = series;

			// Set UserStatus
			this.SetSeriesUserStatus();
		}

		/// <summary>
		/// Store seasons in the model.
		/// </summary>
		/// <param name="seasons"></param>
		private void SetSeasons(List<Entity.Season> seasons)
		{
			this.ClearSeasons();

			this.seasons = seasons;

			// Set UserStatus
			foreach (Entity.Season season in this.seasons) {
				if (season.StatusId != 0) {
					season.UserStatus = this.StatusRepository.Find(season.StatusId);
				}
			}
		}

		#endregion

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		/// <summary>
		/// Get the stored series.
		/// Use SetSeries() to set them.
		/// </summary>
		public List<Entity.Serie> Series
		{
			get { return this.series; }
		}

		/// <summary>
		/// Get the stored seasons.
		/// Use SetSeasons() to set them.
		/// </summary>
		public List<Entity.Season> Seasons
		{
			get { return this.seasons; }
		}

		public List<Entity.Episode> Episodes
		{
			get { return this.episodes; }
			set { this.episodes = value; }
		}

		/// <summary>
		/// Get the stored current serie or query it from the database.
		/// </summary>
		public Entity.Serie CurrentSerie
		{
			get
			{
				if (this.currentSerie == null && this.currentSeason != null) {
					this.currentSerie = this.SerieRepository.Find(this.currentSeason.SerieId);
				}

				return this.currentSerie;
			}
			set { this.currentSerie = value; }
		}

		public Entity.Season CurrentSeason
		{
			get { return this.currentSeason; }
			set { this.currentSeason = value; }
		}

		public List<Entity.UserStatus> Status
		{
			get { return this.status; }
			set { this.status = value; }
		}

		public Repository.Serie SerieRepository
		{
			get { return Repository.Serie.Instance; }
		}

		public Repository.Season SeasonRepository
		{
			get { return Repository.Season.Instance; }
		}

		public Repository.Episode EpisodeRepository
		{
			get { return Repository.Episode.Instance; }
		}

		public Repository.Status StatusRepository
		{
			get { return Repository.Status.Instance; }
		}

		#endregion Accessor
	}
}
