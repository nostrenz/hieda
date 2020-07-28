using System;
using System.Collections.Generic;
using Hieda.Properties;
using Hieda.Tool;

namespace Hieda.Repository
{
	/**
	 * Get, Add, Update, Delete Series entities
	 */
	public class Serie : Abstract
	{
		// Singleton instance
		private static Repository.Serie instance = null;

		// Prevent multiple instence to be created when using multiple thread.
		private static readonly object locker = new object();

		/// <summary>
		/// A singleton constructor is always private.
		/// </summary>
		private Serie()
		{
			this.InitTable();
		}

		/// <summary>
		/// Get instance using this function, do no use a new statment.
		/// </summary>
		/// <returns></returns>
		public static Repository.Serie GetInstance()
		{
			// Prevent threading problems
			lock (locker) {
				if (instance == null) {
					instance = new Repository.Serie();
				}

				return instance;
			}
		}

		/*
		======================================
		Getter
		======================================
		*/

		#region Getter

		public Entity.Serie Find(int serieId)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Serie.TABLE)
				.Select(this.GetSelectQuery(), "s1")
				.Where("id = :serieId")
				.SetParam("serieId", serieId);

			List<Entity.Serie> series = this.FillListFromQuery(qb.Query);

			if (series.Count == 0) {
				return null;
			}

			return series[0];
		}

		public List<Entity.Serie> GetAll()
		{
			QueryBuilder qb = new QueryBuilder(Entity.Serie.TABLE)
				.Select(this.GetSelectQuery(), "s1")
				.OrderBy(Settings.Default.TileOrderBy, Settings.Default.TileOrderByDirection, true);

			return this.FillListFromQuery(qb.Query);
		}

		public List<Entity.Serie> GetAllByTitleAndStatus(string search = "", int status = (int)Entity.DefaultStatus.All)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Serie.TABLE)
				.Select(this.GetSelectQuery(), "s1")
				.Where("1");

			if (!String.IsNullOrWhiteSpace(search)) {
				qb.AndWhere("title LIKE '%" + search + "%'");
			}

			if (status != (int)Entity.DefaultStatus.All) {
				qb.AndWhere("status_id = " + status);
			}

			qb.OrderBy(Settings.Default.TileOrderBy, Settings.Default.TileOrderByDirection, true);

			return this.FillListFromQuery(qb.Query);
		}

		/// <summary>
		/// Used for the search function.
		/// </summary>
		/// <returns></returns>
		public List<Entity.Serie> GetAllForSearch(string query, string mode, string field, int status=(int)Entity.DefaultStatus.All)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Serie.TABLE)
				.Select("DISTINCT " + this.GetSelectQuery(), "s1");

			if (!String.IsNullOrWhiteSpace(query)) {
				string like = " LIKE '" + (mode == "contains" ? "%" : "") + query + "%'";

				if (field == "title") {
					qb.Where("s1.title" + like);
				} else if (field == "studio") {
					qb.InnerJoin("season", "s2");
					qb.On("s1.id = s2.serie_id");

					qb.InnerJoin("studio", "s3");
					qb.On("s3.id = s2.studio_id");

					qb.Where("s3.name" + like);
				}
			} else {
				qb.Where("1");
			}

			if (status != (int)Entity.DefaultStatus.All) {
				qb.AndWhere("s1.status_id = " + status);
			}

			qb.OrderBy(Settings.Default.TileOrderBy, Settings.Default.TileOrderByDirection, true);

			return this.FillListFromQuery(qb.Query);
		}

		public List<Entity.Serie> GetAllIdAndTitles(int excludeSerieId)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Serie.TABLE)
				.Select("s1.id, s1.title", "s1")
				.Where("s1.id != :serieId")
				.SetParam("serieId", excludeSerieId)
				.OrderBy(Settings.Default.TileOrderBy, Settings.Default.TileOrderByDirection, true);

			return this.FillListFromQuery(qb.Query);
		}

		#endregion

		/*
		======================================
		Action
		======================================
		*/

		#region Action

		/// <summary>
		/// Add an entity in database.
		/// </summary>
		/// <param name="episode"></param>
		/// <returns>created entity ID</returns>
		public int Add(Entity.Serie entity)
		{
			App.db.Insert(Entity.Serie.TABLE, this.table.GetKeysAndValues(entity));

			return App.db.LastId(Entity.Serie.TABLE);
		}

		/// <summary>
		/// Update the whole entity.
		/// </summary>
		/// <param name="entity"></param>
		public void Update(Entity.Serie entity)
		{
			App.db.Update(Entity.Serie.TABLE, entity.Id, this.table.GetKeysAndValues(entity));
		}

		/// <summary>
		/// Update a field from the entity.
		/// Faster than updating the whole entity but slower than UpdateField().
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="field"></param>
		public void Update(Entity.Serie entity, string field)
		{
			App.db.Update(Entity.Serie.TABLE, entity.Id, this.table.GetKeyValue(entity, field));
		}

		/// <summary>
		/// Update a field with a value.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="column"></param>
		/// <param name="value"></param>
		public void UpdateField(int id, string column, string value)
		{
			App.db.Update(Entity.Serie.TABLE, id, column, value);
		}

		public void UpdateField(int id, string column, int value)
		{
			App.db.Update(Entity.Serie.TABLE, id, column, value);
		}

        /// <summary>
		/// Update a field for multiple episodes using their IDs.
		/// </summary>
		/// <param name="ids"></param>
		/// <param name="column"></param>
		/// <param name="value"></param>
		public void UpdateFieldIn(List<int> ids, string column, string value)
        {
            App.db.Update(Entity.Serie.TABLE, ids, column, value);
        }

		/// <summary>
		/// Execute an update query on multiple columns at once.
		/// </summary>
		/// <param name="pairs"></param>
		/// <param name="ids"></param>
		public void UpdateMultiples(List<KeyValuePair<string, string>> pairs, List<int> ids)
		{
			this.UpdateMultiples(Entity.Serie.TABLE, pairs, ids);
		}

		public void DeleteAllGenresForSerie(int serieId)
		{
			QueryBuilder qb = new QueryBuilder("serie_genre")
				.Delete()
				.Where("serie_id = :serieId")
				.SetParam("serieId", serieId);

			App.db.Execute(qb.Query);
		}

		public void AddGenresForSerie(int serieId, List<Entity.Genre> genres)
		{
			if (genres == null || genres.Count == 0) {
				return;
			}

			string query = "INSERT INTO 'serie_genre'";

			for (byte i = 0; i < genres.Count; i++) {
				if (i == 0) {
					query += " SELECT " + serieId + " AS 'serie_id', " + genres[i].Id.ToString() + " AS 'genre_id' ";
				} else {
					query += " UNION ALL SELECT " + serieId + ", " + genres[i].Id.ToString();
				}
			}

			App.db.Execute(query);
		}

		/// <summary>
		/// Delete a serie and all season/episodes in it.
		/// </summary>
		/// <param name="serieId"></param>
		public void Erase(int serieId)
		{
			string query = String.Format(@"
				DELETE FROM serie WHERE id = {0};
				DELETE FROM season WHERE serie_id = {0};
				DELETE FROM episode WHERE serie_id = {0};",
				serieId.ToString()
			); 

			App.db.Execute(query);
		}

		public void EraseMultiple(List<int> ids)
		{
			string query = String.Format(@"
				DELETE FROM serie WHERE id IN ({0});
				DELETE FROM season WHERE serie_id IN ({0});
				DELETE FROM episode WHERE serie_id IN ({0});",
				Tools.InClause(ids));

			App.db.Execute(query);
		}

		/// <summary>
		/// Fill series list from a query.
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public List<Entity.Serie> FillListFromQuery(string query)
		{
			List<Dictionary<string, string>> tuples = App.db.Fetch(query);
			List<Entity.Serie> series = new List<Entity.Serie>();

			if (tuples.Count > 0) {
				foreach (Dictionary<string, string> tuple in tuples) {
					series.Add(this.FillEntityFromTuple(tuple));
				}
			}

			return series;
		}

		public string GetSelectQuery()
		{
			string seasonsQuery = "";
			string ownedQuery = "";
			string watchedQuery = "";
			string episodesQuery = "";

			QueryBuilder seasonsQb = new QueryBuilder(Entity.Season.TABLE)
					.Select("COUNT(*)", "s2")
					.Where("s2.serie_id = s1.id");
			QueryBuilder episodesQb = new QueryBuilder(Entity.Season.TABLE)
					.Select("SUM(episodes_total)", "s2")
					.Where("s2.serie_id = s1.id");

			seasonsQuery = seasonsQb.Query;
			episodesQuery = episodesQb.Query;

			if (!Settings.Default.FakeEpisode) {
				QueryBuilder ownedQb = new QueryBuilder(Entity.Episode.TABLE)
					.Select("COUNT(*)", "ep")
					.Where("ep.serie_id = s1.id");
				QueryBuilder watchedQb = new QueryBuilder(Entity.Episode.TABLE)
					.Select("COUNT(*)", "ep")
					.Where("ep.serie_id = s1.id")
					.AndWhere("ep.watched = 1");

				ownedQuery = ownedQb.Query;
				watchedQuery = watchedQb.Query;
			} else {
				ownedQuery = @"
					SELECT
					(SUM(CASE
						WHEN s2.status_id = -6 AND s2.episodes_total > 0 THEN s2.episodes_total
						ELSE (
							SELECT COUNT(*)
							FROM episode ep
							WHERE ep.season_id = s2.id
						)
					END))
					FROM season s2
					WHERE s2.serie_id = s1.id
				";

				watchedQuery = @"
					SELECT
					(SUM(CASE
						WHEN s2.status_id = -6 AND s2.episodes_total > 0 THEN s2.episodes_total
						ELSE (
							SELECT COUNT(*)
							FROM episode ep
							WHERE ep.season_id = s2.id
							AND ep.watched = 1
						)
					END))
					FROM season s2
					WHERE s2.serie_id = s1.id
				";
			}

			return "s1.*, (" + seasonsQuery + ") AS seasons_count, (" + ownedQuery + ") AS episodes_owned, (" + watchedQuery + ") AS episodes_watched, (" + episodesQuery + ") AS episodes_total";
		}

		#endregion

		/*
		======================================
		Privates
		* 
		* Thoses functions are present in Repository.Abstract, but are also implemented
		* here because they are a lot faster since they don't use as mush reflexion.
		* 
		* So it's not necessary to implement them, but do it to speed up the application.
		======================================
		*/

		#region Privates

		private void InitTable()
		{
			if (this.table == null) {
				this.table = new Service.Database.Table(Entity.Serie.TABLE);
			}
		}

		/// <summary>
		/// Return an entity from a tuple.
		/// </summary>
		/// <param name="tuple">
		/// Got it from a GetTuple-like methode from SQLite plateform.
		/// </param>
		/// <returns></returns>
		private Entity.Serie FillEntityFromTuple(Dictionary<string, string> tuple)
		{
			return (Entity.Serie)this.table.FillEntityFromTuple(Entity.Serie.TABLE, new Entity.Serie(), tuple);
		}

		#endregion

		/*
		======================================
		Accessor
		======================================
		*/

		#region Accessor

		public static Repository.Serie Instance
		{
			get { return GetInstance(); }
		}

		#endregion Accessor
	}
}
