using System;
using System.Collections.Generic;
using Hieda.Properties;
using Hieda.Tool;

namespace Hieda.Repository
{
	public class Episode : Abstract
	{
		// Singleton instance
		private static Repository.Episode instance = null;

		// Prevent multiple instence to be created when using multiple thread.
		private static readonly object locker = new object();

		/// <summary>
		/// A singleton constructor is always private.
		/// </summary>
		private Episode()
		{
			this.InitTable();
		}

		/// <summary>
		/// Get instance using this function, do no use a new statment.
		/// </summary>
		/// <returns></returns>
		public static Repository.Episode GetInstance()
		{
			// Prevent threading problems
			lock (locker) {
				if (instance == null) {
					instance = new Repository.Episode();
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

		public List<Entity.Episode> GetAll()
		{
			QueryBuilder qb = new QueryBuilder(Entity.Episode.TABLE)
				.Select(this.GetSelectQuery(), "ep")
				.InnerJoin("serie", "se", "se.id = ep.serie_id")
				.InnerJoin("season", "sa", "sa.id = ep.season_id")
				.OrderBy("ep.number, ep.title", "ASC", true);

			return this.FillListFromQuery(qb.Query);
		}

		public List<Entity.Episode> GetAllBySerie(int serieId)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Episode.TABLE)
				.Select(this.GetSelectQuery(), "ep")
				.InnerJoin("serie", "se", "se.id = ep.serie_id")
				.InnerJoin("season", "sa", "sa.id = ep.season_id")
				.Where("ep.serie_id = " + serieId)
				.OrderBy("ep.number, ep.title", "ASC", true);

			return this.FillListFromQuery(qb.Query);
		}

		public List<Entity.Episode> GetAllBySeason(int seasonId)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Episode.TABLE)
				.Select(this.GetSelectQuery(), "ep")
				.InnerJoin("serie", "se", "se.id = ep.serie_id")
				.InnerJoin("season", "sa", "sa.id = ep.season_id")
				.Where("ep.season_id = " + seasonId)
				.OrderBy("ep.number, ep.title", "ASC", true);

			return this.FillListFromQuery(qb.Query);
		}

		public List<Entity.Episode> GetAllBySeries(List<int> serieIds)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Episode.TABLE)
				.Select(this.GetSelectQuery(), "ep")
				.InnerJoin("serie", "se", "se.id = ep.serie_id")
				.InnerJoin("season", "sa", "sa.id = ep.season_id")
				.Where("ep.serie_id IN (" + Tools.InClause(serieIds) + ")")
				.OrderBy("ep.number, ep.title", "ASC", true);

			return this.FillListFromQuery(qb.Query);
		}

		public List<Entity.Episode> GetAllBySeasons(List<int> seasonIds)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Episode.TABLE)
				.Select(this.GetSelectQuery(), "ep")
				.InnerJoin("serie", "se", "se.id = ep.serie_id")
				.InnerJoin("season", "sa", "sa.id = ep.season_id")
				.Where("ep.season_id IN (" + Tools.InClause(seasonIds) + ")")
				.OrderBy("ep.number, ep.title", "ASC", true);

			return this.FillListFromQuery(qb.Query);
		}

		public List<Entity.Episode> GetAllWatchedForSeason(int seasonId, bool watched)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Episode.TABLE)
				.Select(this.GetSelectQuery(), "ep")
				.InnerJoin("serie", "se", "se.id = ep.serie_id")
				.InnerJoin("season", "sa", "sa.id = ep.season_id")
				.Where("ep.season_id = " + seasonId)
				.AndWhere("ep.watched = " + (watched ? "1" : "0"))
				.OrderBy("ep.number, ep.title", "ASC", true);

			return this.FillListFromQuery(qb.Query);
		}

		public List<Entity.Episode> GetAllWatchedForSerie(int serieId, bool watched)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Episode.TABLE)
				.Select(this.GetSelectQuery(), "ep")
				.InnerJoin("serie", "se", "se.id = ep.serie_id")
				.InnerJoin("season", "sa", "sa.id = ep.season_id")
				.Where("ep.serie_id = " + serieId)
				.AndWhere("ep.watched = " + (watched ? "1" : "0"))
				.OrderBy("ep.number, ep.title", "ASC", true);

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
		public int Add(Entity.Episode entity)
		{
			App.db.Insert(Entity.Episode.TABLE, this.table.GetKeysAndValues(entity));

			return App.db.LastId(Entity.Episode.TABLE);
		}

		/// <summary>
		/// Update the whole entity.
		/// </summary>
		/// <param name="entity"></param>
		public void Update(Entity.Episode entity)
		{
			App.db.Update(Entity.Episode.TABLE, entity.Id, this.table.GetKeysAndValues(entity));
		}

		/// <summary>
		/// Update a field from the entity.
		/// Faster than updating the whole entity but slower than UpdateField().
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="field"></param>
		public void Update(Entity.Episode entity, string field)
		{
			App.db.Update(Entity.Episode.TABLE, entity.Id, this.table.GetKeyValue(entity, field));
		}

		/// <summary>
		/// Update a field with a value.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="column"></param>
		/// <param name="value"></param>
		public void UpdateField(int id, string column, string value)
		{
			App.db.Update(Entity.Episode.TABLE, id, column, value);
		}

		/// <summary>
		/// Update a field for multiple episodes using their IDs.
		/// </summary>
		/// <param name="ids"></param>
		/// <param name="column"></param>
		/// <param name="value"></param>
		public void UpdateFieldIn(List<int> ids, string column, string value)
		{
			App.db.Update(Entity.Episode.TABLE, ids, column, value);
		}

		/// <summary>
		/// Execute an update query on multiple columns at once.
		/// </summary>
		/// <param name="pairs"></param>
		/// <param name="ids"></param>
		public void UpdateMultiples(List<KeyValuePair<string, string>> pairs, List<int> ids)
		{
			this.UpdateMultiples(Entity.Episode.TABLE, pairs, ids);
		}

		public void Delete(int id)
		{
			App.db.Delete(Entity.Episode.TABLE, id);
		}

		public void DeleteMultiple(List<int> ids)
		{
			string query = String.Format("DELETE FROM episode WHERE id IN ({0});", Tools.InClause(ids));

			App.db.Execute(query);
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
				this.table = new Service.Database.Table(Entity.Episode.TABLE);
			}
		}

		/// <summary>
		/// Fill series list from a query.
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		private List<Entity.Episode> FillListFromQuery(string query)
		{
			List<Dictionary<string, string>> tuples = App.db.Fetch(query);
			List<Entity.Episode> episodes = new List<Entity.Episode>();

			if (tuples.Count > 0) {
				foreach (Dictionary<string, string> tuple in tuples) {
					episodes.Add(this.FillEntityFromTuple(tuple));
				}
			}

			return episodes;
		}

		/// <summary>
		/// Return an entity from a tuple.
		/// </summary>
		/// <param name="tuple">
		/// Got it from a GetTuple-like methode from SQLite plateform
		/// </param>
		/// <returns></returns>
		private Entity.Episode FillEntityFromTuple(Dictionary<string, string> tuple)
		{
			Entity.Episode episodes = (Entity.Episode)this.table.FillEntityFromTuple(Entity.Episode.TABLE, new Entity.Episode(), tuple);

			return episodes;
		}

		private string GetSelectQuery()
		{
			return @"ep.id, ep.serie_id, ep.season_id, ep.title, ep.number, ep.uri, ep.watched, ep.cover"
			+ @", (CASE WHEN sa.cover IS NOT NULL THEN sa.cover ELSE se.cover END) AS parent_cover";
		}

		#endregion

		/*
		======================================
		Accessor
		======================================
		*/

		#region Accessor

		public static Repository.Episode Instance
		{
			get { return GetInstance(); }
		}

		#endregion Accessor
	}
}
