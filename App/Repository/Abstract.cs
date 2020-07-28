using System;
using System.Collections.Generic;

namespace Hieda.Repository
{
	/// <summary>
	/// Note:
	/// It is possible to use FillListFromQuery and FillEntityFromTuple from this class inside repositories,
	/// however it uses reflexion which can be 1000 times slower. Slow never use them unless absolutly necessary or
	/// for testing purposes. Instead implement them with the correct Entity type insite each repositories.
	/// </summary>
	public class Abstract
	{
		internal Service.Database.Table table = null;

		/// <summary>
		/// Fill entities list from a query.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="query"></param>
		/// <param name="auto"></param>
		/// <returns></returns>
		internal List<T> FillListFromQuery<T>(string query, bool auto = false)
		{
			List<Dictionary<string, string>> tuples = App.db.Fetch(query);
			List<T> entities = new List<T>();

			if (tuples.Count > 0) {
				foreach (Dictionary<string, string> tuple in tuples) {
					entities.Add((T)this.FillEntityFromTuple<T>(tuple));
				}
			}

			return entities;
		}

		/// <summary>
		/// Return an entity from a tuple.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="tuple">
		/// Got it from a GetTuple-like methode from SQLite plateform
		/// </param>
		/// <returns></returns>
		internal T FillEntityFromTuple<T>(Dictionary<string, string> tuple)/* where T :new()*/
		{
			string tableName = typeof(T).GetField("Table").GetValue(null).ToString();

			// Warning: work, but 11 times slower than new T();
			var instance = (T)Activator.CreateInstance(typeof(T));
			//T instance = default(T);
			//var instance =  new T();

			if (this.table == null) {
				this.table = new Service.Database.Table(tableName);
			}

			object entity = table.FillEntityFromTuple(tableName, instance, tuple);

			return (T)entity;
		}

		/// <summary>
		/// Execute an update query on multiple columns at once.
		/// 
		/// UPDATE
		///		table
		///	SET column_1 = new_value_1,
		///		column_2 = new_value_2
		///	WHERE
		///		id IN(< ids >)
		/// </summary>
		/// <param name="sableName"></param>
		/// <param name="pairs"></param>
		/// <param name="ids"></param>
		internal void UpdateMultiples(string tableName, List<KeyValuePair<string, string>> pairs, List<int> ids)
		{
			// Nothing to update
			if (pairs.Count < 1) {
				return;
			}

			string columns = "";
			int count = pairs.Count;

			for (int i = 0; i < count; i++) {
				columns += pairs[i].Key + "=\"" + pairs[i].Value + "\"";

				if (i < count - 1) {
					columns += ", ";
				}
			}

			App.db.Execute("UPDATE " + tableName + " SET " + columns + " WHERE id IN (" + Tools.InClause(ids) + ")");
		}
	}
}
