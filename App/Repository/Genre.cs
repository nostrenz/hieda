using System.Collections.Generic;
using Hieda.Tool;

namespace Hieda.Repository
{
	/**
	 * Get, Add, Update, Delete Genre entities
	 */
	public class Genre : Abstract
	{
		// Singleton instance
		private static Repository.Genre instance = null;

		// Prevent multiple instence to be created when using multiple thread.
		private static readonly object locker = new object();

		/// <summary>
		/// A singleton constructor is always private.
		/// </summary>
		private Genre()
		{
			this.InitTable();
		}

		/// <summary>
		/// Get instance using this function, do no use a new statment.
		/// </summary>
		/// <returns></returns>
		public static Repository.Genre GetInstance()
		{
			// Prevent threading problems
			lock (locker) {
				if (instance == null) {
					instance = new Repository.Genre();
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

		public List<Entity.Genre> GetAll()
		{
			QueryBuilder qb = new QueryBuilder(Entity.Genre.TABLE)
				.Select("id, name", "g")
				.OrderBy("name", "ASC", true);

			return this.FillListFromQuery(qb.Query);
		}

		public List<Entity.Genre> GetAllBySerie(int serieId)
		{
			QueryBuilder qb = new QueryBuilder(Entity.Genre.TABLE)
				.Select("id, name", "g")
				.InnerJoin("serie_genre", "sg")
				.On("sg.genre_id = g.id")
				.Where("sg.serie_id = :serieId")
				.SetParam("serieId", serieId)
				.OrderBy("name", "ASC", true);

			return this.FillListFromQuery(qb.Query);
		}

		/// <summary>
		/// Get a list of Genre entities from their names.
		/// </summary>
		/// <param name="genres"></param>
		/// <returns></returns>
		public List<Entity.Genre> FindByNames(string[] genres)
		{
			string inClause = "";
			int lenght = genres.Length;

			for (byte i=0; i< lenght; i++) {
				inClause += '"' + genres[i] + '"';

				if (i+1 < lenght) {
					inClause += ",";
				}
			}

			QueryBuilder qb = new QueryBuilder(Entity.Genre.TABLE)
				.Select("id, name", "g")
				.Where("g.name IN (" + inClause + ")")
				.OrderBy("name", "ASC", true);

			return this.FillListFromQuery(qb.Query);
		}

		/// <summary>
		/// Insert a list of genres if they don't already exist.
		/// </summary>
		/// <param name="genres"></param>
		public void InsertIfNotExist(string[] genres)
		{
			string inserts = "";

			foreach (string genre in genres) {
				inserts += "INSERT INTO genre(name) SELECT '" + genre + "' WHERE NOT EXISTS(SELECT 1 FROM genre WHERE name = '" + genre + "');";
			}

			App.db.Execute(inserts);
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
		public int Add(Entity.Genre entity)
		{
			App.db.Insert(Entity.Genre.TABLE, this.table.GetKeysAndValues(entity));

			return App.db.LastId(Entity.Genre.TABLE);
		}

		/// <summary>
		/// Update the whole entity.
		/// </summary>
		/// <param name="entity"></param>
		public void Update(Entity.Genre entity)
		{
			App.db.Update(Entity.Genre.TABLE, entity.Id, this.table.GetKeysAndValues(entity));
		}

		/// <summary>
		/// Delete a serie entity.
		/// </summary>
		/// <param name="serieId">
		/// Genre ID
		/// </param>
		public void Delete(int genreId)
		{
			App.db.Delete(Entity.Genre.TABLE, genreId);
		}

		public void CreateTable(Entity.Genre genre)
		{
			string exp = this.table.GetExpression(genre);

			App.db.CreateTable(exp);
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
				this.table = new Service.Database.Table(Entity.Genre.TABLE);
			}
		}

		/// <summary>
		/// Fill series list from a query.
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		private List<Entity.Genre> FillListFromQuery(string query)
		{
			List<Dictionary<string, string>> tuples = App.db.Fetch(query);
			List<Entity.Genre> series = new List<Entity.Genre>();

			if (tuples.Count > 0) {
				foreach (Dictionary<string, string> tuple in tuples) {
					series.Add(this.FillEntityFromTuple(tuple));
				}
			}

			return series;
		}

		/// <summary>
		/// Return an entity from a tuple.
		/// </summary>
		/// <param name="tuple">
		/// Got it from a GetTuple-like methode from SQLite plateform.
		/// </param>
		/// <returns></returns>
		private Entity.Genre FillEntityFromTuple(Dictionary<string, string> tuple)
		{
			return (Entity.Genre)this.table.FillEntityFromTuple(Entity.Genre.TABLE, new Entity.Genre(), tuple);
		}

		#endregion

		/*
		======================================
		Accessor
		======================================
		*/

		#region Accessor

		public static Repository.Genre Instance
		{
			get { return GetInstance(); }
		}

		#endregion Accessor
	}
}
