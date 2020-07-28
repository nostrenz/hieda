using System.Collections.Generic;
using Hieda.Tool;

namespace Hieda.Repository
{
	/**
	 * Get, Add, Update, Delete entities
	 */
	public class Status : Abstract
	{
		// Singleton instance
		private static Repository.Status instance = null;

		// Prevent multiple instence to be created when using multiple thread.
		private static readonly object locker = new object();

		/// <summary>
		/// A singleton constructor is always private.
		/// </summary>
		private Status()
		{
			this.InitTable();
		}

		/// <summary>
		/// Get instance using this function, do not use a new statment.
		/// </summary>
		/// <returns></returns>
		public static Repository.Status GetInstance()
		{
			// Prevent threading problems
			lock (locker) {
				if (instance == null) {
					instance = new Repository.Status();
				}

				return instance;
			}
		}

		/*
		======================================
		Getters
		======================================
		*/

		#region Getters

		/// <summary>
		/// Get a single entity by Id.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="auto"></param>
		/// <returns></returns>
		public Entity.UserStatus Find(int id, bool auto = false)
		{
			Dictionary<string, string> tuple = App.db.GetTupleById(Entity.UserStatus.TABLE, id);

			//return this.FillEntityFromTuple<Entity.Status>(tuple);
			return this.FillEntityFromTuple(tuple);
		}

		public List<Entity.UserStatus> GetAll(bool auto = false)
		{
			QueryBuilder qb = new QueryBuilder(Entity.UserStatus.TABLE)
				.Select()
				.OrderBy("text", "ASC", true);

			//return this.FillListFromQuery<Entity.Status>(qb.Query);
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
		public int Add(Entity.UserStatus entity)
		{
			App.db.Insert(Entity.UserStatus.TABLE, this.table.GetKeysAndValues(entity));

			return App.db.LastId(Entity.UserStatus.TABLE);
		}

		/// <summary>
		/// Update the whole entity.
		/// </summary>
		/// <param name="entity"></param>
		public void Update(Entity.UserStatus entity)
		{
			App.db.Update(Entity.UserStatus.TABLE, entity.Id, this.table.GetKeysAndValues(entity));
		}

		/// <summary>
		/// Update a field with a value.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="column"></param>
		/// <param name="value"></param>
		public void UpdateField(int id, string column, string value)
		{
			App.db.Update(Entity.UserStatus.TABLE, id, column, value);
		}

		/// <summary>
		/// Delete an entity entity.
		/// </summary>
		/// <param name="id">
		/// Status ID
		/// </param>
		public void Delete(int id)
		{
			App.db.Delete(Entity.UserStatus.TABLE, id);
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
				this.table = new Service.Database.Table(Entity.UserStatus.TABLE);
			}
		}

		// Fill entity list from a query
		private List<Entity.UserStatus> FillListFromQuery(string query, bool auto = false)
		{
			List<Dictionary<string, string>> tuples = App.db.Fetch(query);
			List<Entity.UserStatus> status = new List<Entity.UserStatus>();

			if (tuples.Count > 0) {
				foreach (Dictionary<string, string> tuple in tuples) {
					status.Add(this.FillEntityFromTuple(tuple));
				}
			}

			return status;
		}

		/// <summary>
		/// Return an entity from a tuple.
		/// </summary>
		/// <param name="tuple">
		/// Got it from a GetTuple-like methode from SQLite plateform.
		/// </param>
		/// <returns></returns>
		private Entity.UserStatus FillEntityFromTuple(Dictionary<string, string> tuple)
		{
			return (Entity.UserStatus)this.table.FillEntityFromTuple(Entity.UserStatus.TABLE, new Entity.UserStatus(), tuple);
		}

		#endregion

		/*
		======================================
		Accessor
		======================================
		*/

		#region Accessor

		public static Repository.Status Instance
		{
			get { return GetInstance(); }
		}

		#endregion Accessor
	}
}
