using System;
using System.Collections.Generic;
using Hieda.Properties;
using Hieda.Tool;

namespace Hieda.Repository
{
	public class Season : Abstract
	{
		// Singleton instance
		private static Repository.Season instance = null;

		// Prevent multiple instence to be created when using multiple thread.
		private static readonly object locker = new object();

		/// <summary>
		/// A singleton constructor is always private.
		/// </summary>
		private Season()
		{
			this.InitTable();
		}

		/// <summary>
		/// Get instance using this function, do no use a new statment.
		/// </summary>
		/// <returns></returns>
		public static Repository.Season GetInstance()
		{
			// Prevent threading problems
			lock (locker) {
				if (instance == null) {
					instance = new Repository.Season();
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

		/// <summary>
		/// Get a single entity by ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Entity.Season Find(int id)
		{
			Dictionary<string, string> tuple = App.db.GetTupleById(Entity.Season.TABLE, id);

			return this.FillEntityFromTuple(tuple);
		}

		/// <summary>
		/// Same as Find() but get more info about the season using GetSelectQuery().
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Entity.Season GetOneById(int id)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Season.TABLE)
				.Select(this.GetSelectQuery(), "sa")
				.InnerJoin("serie", "se")
				.On("se.id = sa.serie_id")
				.Where("sa.id = " + id)
				.OrderBy("sa.number, sa.title", Settings.Default.TileOrderByDirection, true);

			List<Entity.Season> result = this.FillListFromQuery(qb.Query);

			if (result.Count > 0) {
				return result[0];
			}

			return null;
		}

		public Entity.Season GetOneBySerie(int serieId)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Season.TABLE)
				.Select(this.GetSelectQuery(), "sa")
				.InnerJoin("serie", "se")
				.On("se.id = sa.serie_id")
				.Where("serie_id = " + serieId)
				.OrderBy("sa.number, sa.title", Settings.Default.TileOrderByDirection, true);

			List<Entity.Season> result = this.FillListFromQuery(qb.Query);

			if (result.Count > 0) {
				return result[0];
			}

			return null;
		}

		public List<Entity.Season> GetAll()
		{
			QueryBuilder qb = new QueryBuilder(Entity.Season.TABLE)
				.Select(this.GetSelectQuery(), "sa")
				.InnerJoin("serie", "se")
				.On("se.id = sa.serie_id")
				.OrderBy("sa.number, sa.title", Settings.Default.TileOrderByDirection, true);

			return this.FillListFromQuery(qb.Query);
		}

		public List<Entity.Season> GetAllBySerie(int serieId)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Season.TABLE)
				.Select(this.GetSelectQuery(), "sa")
				.InnerJoin("serie", "se")
				.On("se.id = sa.serie_id")
				.Where("sa.serie_id = " + serieId)
				.OrderBy("sa.number, sa.title", Settings.Default.TileOrderByDirection, true);

			return this.FillListFromQuery(qb.Query);
		}

		public List<Entity.Season> GetAllBySerieAndStatus(int serieId, int status, string types=null, string orderBy="number")
		{
			QueryBuilder qb = new QueryBuilder(Entity.Season.TABLE)
				.Select(this.GetSelectQuery(), "sa")
				.InnerJoin("serie", "se")
				.On("se.id = sa.serie_id")
				.Where("sa.serie_id = " + serieId);

			if (status != (int)Entity.DefaultStatus.All) {
				qb.AndWhere("sa.status_id = " + status + "");
			}

			if (types != null) {
				qb.AndWhere("sa.type IN " + types);
			}

			qb.OrderBy("sa." + orderBy + ", sa.number, sa.year, sa.month, sa.day, sa.title", Settings.Default.TileOrderByDirection, true);

			return this.FillListFromQuery(qb.Query);
		}

		/// <summary>
		/// Get all the seasons for the LabelList.
		/// </summary>
		/// <param name="search"></param>
		/// <param name="status"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		public List<Entity.Season> GetAllOrderedByYearAndSeasonal(string query, string mode, string field, int status = (int)Entity.DefaultStatus.All, string types=null)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Season.TABLE)
				.Select(this.GetSelectQueryForLabeledList(), "sa")
				.InnerJoin("serie", "se")
				.On("sa.serie_id = se.id");

			if (!String.IsNullOrWhiteSpace(query)) {
				string like = " LIKE '" + (mode == "contains" ? "%" : "") + query + "%'";

				if (field == "title") {
					qb.Where("(se.title LIKE '%" + query + "%' OR sa.title" + like + ")");
				} else if (field == "studio") {
					qb.InnerJoin("studio", "st");
					qb.On("st.id = sa.studio_id");

					qb.Where("st.name" + like);
				}
			} else {
				qb.Where("1");
			}

			if (status != (int)Entity.DefaultStatus.All) {
				qb.AndWhere("sa.status_id = " + status + "");
			}

			if (types != null) {
				qb.AndWhere("sa.type IN " + types);
			}

			qb.OrderByClause("sa.year DESC, sa.seasonal DESC, sa.type ASC, se.title ASC, sa.title ASC");

			return this.FillListFromQuery(qb.Query);
		}

        /// <summary>
        /// Get the query used to count how many episodes are in a season.
        /// </summary>
        /// <returns></returns>
        private string GetOwnedQuery()
		{
			if (!Settings.Default.FakeEpisode) {
				return new QueryBuilder(Entity.Episode.TABLE)
					.Select("COUNT(*)", "ep")
					.Where("ep.season_id = sa.id")
					.Query;
			}

			return @"
				CASE
					WHEN sa.status_id = 'Finished' AND sa.episodes_total > 0 THEN sa.episodes_total
					ELSE (
						SELECT COUNT(*)
						FROM episode ep
						WHERE ep.season_id = sa.id
					)
				END
			";
		}

		/// <summary>
		/// Get the query used to count how many watched episodes are in a season.
		/// </summary>
		/// <returns></returns>
		private string GetWatchedQuery()
		{
			if (!Settings.Default.FakeEpisode) {
				return new QueryBuilder(Entity.Episode.TABLE)
					.Select("COUNT(*)", "ep")
					.Where("ep.season_id = sa.id")
					.AndWhere("ep.watched = 1")
					.Query;
			}

			return @"
				CASE
					WHEN sa.status_id = 'Finished' AND sa.episodes_total > 0 THEN sa.episodes_total
					ELSE (
						SELECT COUNT(*)
						FROM episode ep
						WHERE ep.season_id = sa.id
						AND ep.watched = 1
					)
				END
			";
		}

		public string GetSelectQuery()
		{
			return @"sa.id, sa.title, sa.serie_id, sa.synopsis, sa.number, sa.status_id, sa.studio_id, sa.seasonal, sa.year, sa.month, sa.day, sa.episodes_total, sa.type, sa.rpc_large_image, sa.source, sa.wide_episodes, sa.grouping,"
			+ "(" + this.GetOwnedQuery() + @") AS episodes_owned, (" + this.GetWatchedQuery() + @") AS episodes_watched, sa.cover"
			+ ", se.cover AS parent_cover";
		}

		public string GetSelectQueryForLabeledList()
		{
			return "sa.*," +
				"(CASE WHEN (sa.title IS NULL OR sa.title == se.title) THEN se.title ELSE (se.title || ' ' || sa.title) END) AS display_title," +
				"se.cover AS parent_cover," +
				"(" + this.GetOwnedQuery() + @") AS episodes_owned, (" + this.GetWatchedQuery() + @") AS episodes_watched";
		}

		/// <summary>
		/// Get a list of Season entites from an SQL query.
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public List<Entity.Season> FillListFromQuery(string query)
		{
			List<Dictionary<string, string>> tuples = App.db.Fetch(query);
			List<Entity.Season> seasons = new List<Entity.Season>();

			if (tuples.Count > 0) {
				foreach (Dictionary<string, string> tuple in tuples) {
					seasons.Add(this.FillEntityFromTuple(tuple));
				}
			}

			return seasons;
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
		public int Add(Entity.Season season)
		{
			App.db.Insert(Entity.Season.TABLE, this.table.GetKeysAndValues(season));

			return App.db.LastId(Entity.Season.TABLE);
		}

		/// <summary>
		/// Update the whole entity.
		/// </summary>
		/// <param name="season"></param>
		public void Update(Entity.Season season)
		{
			App.db.Update(Entity.Season.TABLE, season.Id, this.table.GetKeysAndValues(season));
		}

		/// <summary>
		/// Update a field from the entity.
		/// Faster than updating the whole entity but slower than UpdateField().
		/// </summary>
		/// <param name="season"></param>
		/// <param name="field"></param>
		public void Update(Entity.Season season, string field)
		{
			App.db.Update(Entity.Season.TABLE, season.Id, this.table.GetKeyValue(season, field));
		}

		/// <summary>
		/// Update a field with a value.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="column"></param>
		/// <param name="value"></param>
		public void UpdateField(int id, string column, string value)
		{
			App.db.Update(Entity.Season.TABLE, id, column, value);
		}

		public void UpdateField(int id, string column, int value)
		{
			App.db.Update(Entity.Season.TABLE, id, column, value);
		}

        /// <summary>
		/// Update a field for multiple episodes using their IDs.
		/// </summary>
		/// <param name="ids"></param>
		/// <param name="column"></param>
		/// <param name="value"></param>
		public void UpdateFieldIn(List<int> ids, string column, string value)
        {
            App.db.Update(Entity.Season.TABLE, ids, column, value);
        }

        public void UpdateEpisodesValues(Entity.Season season)
		{
			string[] keys = {
				"episodes_Owned",
				"episodes_Total",
				"episodes_Viewed"
			};
			string[] values = {
				season.EpisodesOwned.ToString(),
				season.EpisodesTotal.ToString(),
				season.EpisodesViewed.ToString()
			};

			App.db.Update(Entity.Season.TABLE, season.Id, keys, values);
		}

		public void Delete(int id)
		{
			App.db.Delete(Entity.Season.TABLE, id);
		}

		/// <summary>
		/// Delete a season and all episodes in it.
		/// </summary>
		/// <param name="seasonId"></param>
		public void Erase(int seasonId)
		{
			string query = String.Format(@"
				DELETE FROM season WHERE id = {0};
				DELETE FROM episode WHERE season_id = {0};",
				seasonId.ToString()
			);

			App.db.Execute(query);
		}

		public void EraseMultiple(List<int> ids)
		{
			string query = String.Format(@"
				DELETE FROM season WHERE id IN ({0});
				DELETE FROM episode WHERE season_id IN ({0});",
				Tools.InClause(ids));

			App.db.Execute(query);
		}

		/// <summary>
		/// Execute an update query on multiple columns at once.
		/// </summary>
		/// <param name="pairs"></param>
		/// <param name="ids"></param>
		public void UpdateMultiples(List<KeyValuePair<string, string>> pairs, List<int> ids)
		{
			this.UpdateMultiples(Entity.Season.TABLE, pairs, ids);
		}

		#endregion

		/*
		======================================
		Privates
		* 
		* Thoses functions are present in Repository.Abstract, but are also implemented
		* here because they are a lot faster since they don't use as much reflexion.
		* 
		* So it's not necessary to implement them, but do it to speed up the application.
		======================================
		*/

		#region Privates

		private void InitTable()
		{
			if (this.table == null) {
				this.table = new Service.Database.Table(Entity.Season.TABLE);
			}
		}

		private Entity.Season FillEntityFromTuple(Dictionary<string, string> tuple)
		{
			Entity.Season season = (Entity.Season)this.table.FillEntityFromTuple(Entity.Season.TABLE, new Entity.Season(), tuple);

			return season;
		}

		#endregion

		/*
		======================================
		Accessor
		======================================
		*/

		#region Accessor

		public static Repository.Season Instance
		{
			get { return GetInstance(); }
		}

		#endregion Accessor
	}
}
