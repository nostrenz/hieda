using System.Collections.Generic;
using Hieda.Tool;

namespace Hieda.Repository
{
	/**
	 * Get, Add, Update, Delete Studio entities
	 */
	public class Studio : Abstract
	{
		// Singleton instance
		private static Repository.Studio instance = null;

		// Prevent multiple instence to be created when using multiple thread.
		private static readonly object locker = new object();

		/// <summary>
		/// A singleton constructor is always private.
		/// </summary>
		private Studio()
		{
			this.InitTable();
		}

		/// <summary>
		/// Get instance using this function, do no use a new statment.
		/// </summary>
		/// <returns></returns>
		public static Repository.Studio GetInstance()
		{
			// Prevent threading problems
			lock (locker) {
				if (instance == null) {
					instance = new Repository.Studio();
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
		public Entity.Studio Find(int id)
		{
			Dictionary<string, string> tuple = App.db.GetTupleById(Entity.Studio.TABLE, id);

			return this.FillEntityFromTuple(tuple);
		}

		public List<Entity.Studio> GetAll()
		{
			QueryBuilder qb = new QueryBuilder(Entity.Studio.TABLE)
				.Select("id, name", "g")
				.OrderBy("name", "ASC", true);

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
		public int Add(Entity.Studio entity)
		{
			App.db.Insert(Entity.Studio.TABLE, this.table.GetKeysAndValues(entity));

			return App.db.LastId(Entity.Studio.TABLE);
		}

		/// <summary>
		/// Update the whole entity.
		/// </summary>
		/// <param name="entity"></param>
		public void Update(Entity.Studio entity)
		{
			App.db.Update(Entity.Studio.TABLE, entity.Id, this.table.GetKeysAndValues(entity));
		}

		/// <summary>
		/// Delete a serie entity.
		/// </summary>
		/// <param name="serieId">
		/// Studio ID
		/// </param>
		public void Delete(int genreId)
		{
			App.db.Delete(Entity.Studio.TABLE, genreId);
		}

		public void CreateTable(Entity.Studio genre)
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
				this.table = new Service.Database.Table(Entity.Studio.TABLE);
			}
		}

		/// <summary>
		/// Fill series list from a query.
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		private List<Entity.Studio> FillListFromQuery(string query)
		{
			List<Dictionary<string, string>> tuples = App.db.Fetch(query);
			List<Entity.Studio> series = new List<Entity.Studio>();

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
		private Entity.Studio FillEntityFromTuple(Dictionary<string, string> tuple)
		{
			return (Entity.Studio)this.table.FillEntityFromTuple(Entity.Studio.TABLE, new Entity.Studio(), tuple);
		}

		#endregion

		/*
		======================================
		Accessor
		======================================
		*/

		#region Accessor

		public static Repository.Studio Instance
		{
			get { return GetInstance(); }
		}

		#endregion Accessor
	}
}
