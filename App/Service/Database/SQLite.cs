using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;
using Hieda.Tool;
using Hieda.View.Window;

namespace Hieda.Service.Database
{
	public class SQLite
	{
		public const string BD_FILE_NAME = "hiedb.db";

		string dbLocation = Path.DatabaseFolder;
		SQLiteConnection dbConnection;

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Save the DB file in the backups/ folder, and rename it with a timestamp.
		/// </summary>
		public void Backup()
		{
			string path = Path.DbBackupFolder;

			if (!System.IO.Directory.Exists(path)) {
				System.IO.Directory.CreateDirectory(path);
			}

			try {
				System.IO.File.Copy(
					this.dbLocation + @"\" + BD_FILE_NAME,
					path + @"\hiedb_" + DateTime.Now.ToString("yy/MM/dd HH:mm").Replace("/", "-").Replace(":", "h").Replace(" ", "@") + "_.db"
				);
			} catch (Exception) { }
		}

		/// <summary>
		/// Create the DB file.
		/// </summary>
		public void Create()
		{
			System.IO.Directory.CreateDirectory(this.dbLocation);

			SQLiteConnection.CreateFile(this.dbLocation + @"\" + SQLite.BD_FILE_NAME);
			this.Connect();

			Table serie = new Table(Entity.Serie.TABLE);
			this.CreateTable(serie.GetExpression(new Entity.Serie()), false);

			Table season = new Table(Entity.Season.TABLE);
			this.CreateTable(season.GetExpression(new Entity.Season()), false);

			Table episode = new Table(Entity.Episode.TABLE);
			this.CreateTable(episode.GetExpression(new Entity.Episode()), false);

			Table studio = new Table(Entity.Studio.TABLE);
			this.CreateTable(studio.GetExpression(new Entity.Studio()), false);

			Table genre = new Table(Entity.Genre.TABLE);
			this.CreateTable(genre.GetExpression(new Entity.Genre()), false);

			Table status = new Table(Entity.UserStatus.TABLE);
			this.CreateTable(status.GetExpression(new Entity.UserStatus()), false);

			this.CreateTable("serie_genre (serie_id INTEGER, genre_id INTEGER)");

			this.Version = Constants.DB_VERSION;
		}

		/// <summary>
		/// Open connection to DB.
		/// </summary>
		public void Connect()
		{
			this.dbConnection = new SQLiteConnection(
				"Data Source=" + this.dbLocation + "/" + SQLite.BD_FILE_NAME + ";"
			);

			this.dbConnection.Open();
		}

		/// <summary>
		/// Close DB connection.
		/// </summary>
		public void Disconnect()
		{
			this.dbConnection.Close();
		}

		/// <summary>
		/// Restart the connection.
		/// </summary>
		public void Restart()
		{
			this.Disconnect();
			this.Connect();
		}

		/// <summary>
		/// If program's DB version is greater than the one registered in DB, start update process.
		/// </summary>
		public void CheckForUpdates()
		{
			ushort dbVersion = this.Version;

			if (dbVersion < Constants.DB_VERSION) {
				new DbUpdate(dbVersion, Constants.DB_VERSION).ShowDialog();
			} else if (dbVersion > Constants.DB_VERSION) {
				MessageBox.Show(String.Format(Lang.Text("dbVersionMismatch"), dbVersion));
			}
		}

		/// <summary>
		/// Delete the db file and recreate it.
		/// </summary>
		public void Recreate()
		{
			this.Disconnect();

			try {
				System.IO.File.Delete(this.FilePath);
			} catch { }

			this.Create();
		}

		/// <summary>
		/// Create a table.
		/// </summary>
		/// <param name="expression">Example: "highscores (name VARCHAR(20), score INT)"</param>
		/// <param name="ifNotExist"></param>
		public void CreateTable(string expression, bool ifNotExist=true)
		{
			this.ExecuteAsync("CREATE TABLE IF NOT EXISTS " + expression);
		}

		/// <summary>
		/// Insert keys,values in a table.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="keys"></param>
		/// <param name="values"></param>
		public void Insert(string tableName, string[] keys, string[] values)
		{
			string labels = "";
			string newValues = "";

			for (byte i = 0; i < keys.Length; i++) {
				labels += keys[i];

				if (values[i] == null) {
					newValues += "NULL";
				} else if (Tools.IsNumeric(values[i])) {
					newValues += values[i];
				} else {
					newValues += "\"" + values[i].Replace("\"", "'") + "\"";
				}

				if (i != keys.Length - 1) {
					labels += ", ";
					newValues += ", ";
				}
			}

			this.ExecuteAsync("INSERT INTO " + tableName + " (" + labels + ") VALUES (" + newValues + ")");
		}

		/// <summary>
		/// Update multiple.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="keysvalues">
		/// keysvalues: a list of string array (structure: array[0]=key, array[1]=value)
		/// Use Service.Database.Table.GetKeysAndValues() to get it. 
		/// </param>
		public void Insert(string tableName, List<string[]> keysvalues)
		{
			string labels = "";
			string newValues = "";
			byte i = 0;

			foreach (string[] array in keysvalues) {
				labels += array[0];

				if (array[1] == null) {
					newValues += "NULL";
				} else if (Tools.IsNumeric(array[1])) {
					newValues += array[1];
				} else {
					newValues += "\"" + array[1].Replace("\"", "'") + "\"";
				}

				if (i != keysvalues.Count - 1) {
					labels += ", ";
					newValues += ", ";
				}

				i++;
			}

			this.ExecuteAsync(
				"INSERT INTO " + tableName + " (" + labels + ") VALUES (" + newValues + ")"
			);
		}

		/// <summary>
		/// Insert a key,value couple into a table.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Insert(string tableName, string key, string value)
		{
			this.ExecuteAsync("INSERT INTO " + tableName + " (" + key + ") VALUES (" + value + ")");
		}

		/// <summary>
		/// Update the string value of a single column.
		/// String values MUST be in SIMPLE quotes 
		/// (example: key and value must be "title" and "'mdr'").
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="tupleId"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Update(string tableName, int tupleId, string key, string value)
		{
			if (value == null) {
				value = "NULL";
			} else if (!Tools.IsNumeric(value)) {
				value = "\"" + value.Replace("\"", "'") + "\"";
			}

			this.ExecuteAsync("UPDATE " + tableName + " SET " + key + " = " + value + " WHERE ID=" + tupleId);
		}

		/// <summary>
		/// Update the integer value of a single column.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="tupleId"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Update(string tableName, int tupleId, string key, int value)
		{
			this.ExecuteAsync("UPDATE " + tableName + " SET " + key + " = " + value + " WHERE ID=" + tupleId);
		}

		/// <summary>
		/// Variente with a single string array.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="tupleId"></param>
		/// <param name="keyvalue"></param>
		public void Update(string tableName, int tupleId, string[] keyvalue)
		{
			this.Update(tableName, tupleId, keyvalue[0], keyvalue[1]);
		}

		/// <summary>
		/// Update multiple.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="tupleId"></param>
		/// <param name="keys"></param>
		/// <param name="values"></param>
		public void Update(string tableName, int tupleId, string[] keys, string[] values)
		{
			string datas = "";

			for (byte i = 0; i < keys.Length; i++) {
				if (values[i] == null) {
					datas += keys[i] + "=NULL";
				} else if (Tools.IsNumeric(values[i])) {
					datas += keys[i] + "=" + values[i];
				} else {
					datas += keys[i] + "=\"" + values[i].Replace("\"", "'") + "\"";
				}

				if (i != keys.Length - 1) {
					datas += ", ";
				}
			}

			this.ExecuteAsync(
				"UPDATE " + tableName + " SET " + datas + " WHERE id=" + tupleId
			);
		}

		/// <summary>
		/// Update multiple.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="tupleId"></param>
		/// <param name="keysvalues">
		/// keysvalues: a list of string array (structure: array[0]=key, array[1]=value)
		/// Use Service.Database.Table.GetKeysAndValues() to get it. 
		/// </param>
		public void Update(string tableName, int tupleId, List<string[]> keysvalues)
		{
			string datas = "";
			byte i = 0;

			foreach (string[] array in keysvalues) {
				if (array[1] == null) {
					datas += array[0] + "=NULL";
				} else if (Tools.IsNumeric(array[1])) {
					datas += array[0] + "=" + array[1];
				} else {
					datas += array[0] + "=\"" + array[1].Replace("\"", "'") + "\"";
				}

				if (i != keysvalues.Count - 1) {
					datas += ", ";
				}

				i++;
			}

			this.ExecuteAsync("UPDATE " + tableName + " SET " + datas + " WHERE id=" + tupleId);
		}

		/// <summary>
		/// Update a field in multiple tuples at once from a list of IDs.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="ids"></param>
		/// <param name="column"></param>
		/// <param name="value"></param>
		public void Update(string tableName, List<int> ids, string column, string value)
		{
			if (value == null) {
				value = "NULL";
			} else if (!Tools.IsNumeric(value)) {
				value = "\"" + value.Replace("\"", "'") + "\"";
			}

			this.ExecuteAsync("UPDATE " + tableName + " SET " + column + " = " + value + " WHERE ID IN (" + Tools.InClause(ids) + ")");
		}

		/// <summary>
		/// Update a tuple by its index.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="tupleIndex"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void UpdateByIndex(string tableName, int tupleIndex, string key, string value)
		{
			if (value == null) {
				value = "NULL";
			} else if (!Tools.IsNumeric(value)) {
				value = "\"" + value.Replace("\"", "'") + "\"";
			}

			this.ExecuteAsync("UPDATE " + tableName + " SET " + key + " = " + value);
		}

		/// <summary>
		/// Get the value of a column for a certain tuple ID.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="tupleId"></param>
		/// <param name="requestedKey"></param>
		/// <returns></returns>
		public string GetValueById(string tableName, int tupleId, string requestedKey)
		{
			using (SQLiteDataReader reader = this.CreateReader("SELECT " + requestedKey + " FROM " + tableName + " WHERE id = " + tupleId)) {
				return reader[requestedKey].ToString();
			}
		}

		/// <summary>
		/// Returns true if the table exists, false otherwise.
		/// </summary>
		/// <returns></returns>
		public bool TableExists(string table)
		{
			using (SQLiteDataReader reader = this.CreateReader("SELECT name FROM sqlite_master WHERE type='table' AND name='" + table + "';")) {
				bool found = false;

				while (reader.Read() && !found) {
					found = (reader[0].ToString() == table);
				}

				return found;
			}
		}

		/// <summary>
		/// Returns true if the column exists in table, false otherwise.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="collumn"></param>
		/// <returns></returns>
		public bool ColumnExists(string table, string column)
		{
			using (SQLiteDataReader reader = this.CreateReader("PRAGMA table_info(" + table + ");")) {
				bool found = false;

				while (reader.Read() && !found) {
					found = (reader[1].ToString() == column);
				}

				return found;
			}
		}

		/// <summary>
		/// Get a column value in table by row index.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="whereIndex"></param>
		/// <param name="requestedKey"></param>
		/// <returns></returns>
		public string GetAValueByIndex(string tableName, byte whereIndex, string requestedKey)
		{
			using (SQLiteDataReader reader = this.CreateReader("SELECT " + requestedKey + " FROM " + tableName)) {
				string returned = "";

				while (reader.Read()) {
					returned = reader[whereIndex].ToString();
				}

				return returned;
			}
		}

		/// <summary>
		/// Return query result.
		/// </summary>
		/// <param name="sql"></param>
		/// <returns></returns>
		public string[] Get(string sql)
		{
			using (SQLiteDataReader reader = this.CreateReader(sql)) {
				string[] returned = new string[reader.FieldCount];

				while (reader.Read()) {
					for (byte i = 0; i < returned.Length; i++) {
						returned[i] = reader[i].ToString();
					}
				}

				return returned;
			}
		}

		/// <summary>
		/// Get a value fro ma tuple with its ID and name of collumn.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="tupleId"></param>
		/// <param name="label"></param>
		/// <returns></returns>
		public string GetValueByID(string tableName, int tupleId, string label)
		{
			using (SQLiteDataReader reader = this.CreateReader("SELECT " + label + " FROM " + tableName + " WHERE id = " + tupleId)) {
				string returned = "";

				while (reader.Read()) {
					returned = reader[label].ToString();
				}

				return returned;
			}
		}

		/// <summary>
		/// Obtain the number of collumns in a tuple.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="tupleId"></param>
		/// <returns></returns>
		public int TupleLenght(string tableName, int tupleId)
		{
			return this.CreateReader("SELECT * FROM " + tableName + " WHERE id = " + tupleId).FieldCount;
		}

		/// <summary>
		/// Obtain the number of tuples in the table.
		/// </summary>
		/// <param name="tableName"></param>
		/// <returns></returns>
		public int TableLenght(string tableName)
		{
			using (SQLiteDataReader reader = this.CreateReader("SELECT COUNT() FROM " + tableName)) {
				return Int32.Parse(reader[0].ToString());
			}
		}

		/// <summary>
		/// Return a tuple with the given ID.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="tupleID"></param>
		/// <returns></returns>
		public Dictionary<string, string> GetTupleById(string tableName, int tupleID)
		{
			using (SQLiteDataReader reader = this.CreateReader("SELECT * FROM " + tableName + " WHERE id = " + tupleID)) {
				// If there is results
				if (reader.HasRows) {
					Dictionary<string, string> tuple = new Dictionary<string, string>();

					while (reader.Read()) {
						for (byte i = 0; i < reader.FieldCount; i++) {
							tuple[reader.GetName(i)] = reader[i].ToString();
						}
					}

					return tuple;
				}

				return null;
			}
		}

		/// <summary>
		// Obtain all tuples from a table.
		/// </summary>
		/// <param name="tableName"></param>
		/// <returns></returns>
		public List<Dictionary<string, string>> GetAllTuples(string tableName)
		{
			return this.Fetch("SELECT * FROM " + tableName);
		}

		/// <summary>
		/// Get tuples from a query.
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		//public List<String[]> Fetch(string query)
		public List<Dictionary<string, string>> Fetch(string query)
		{
			using (SQLiteDataReader reader = this.CreateReader(query)) {
				List<Dictionary<string, string>> tuples = new List<Dictionary<string, string>>();

				// For each tuple in query result
				while (reader.Read()) {
					Dictionary<string, string> tuple = new Dictionary<string, string>();

					for (byte i = 0; i < reader.FieldCount; i++) {
						tuple[reader.GetName(i)] = reader[i].ToString();
					}

					tuples.Add(tuple);
				}

				return tuples;
			}
		}

		/// <summary>
		/// Same as Fetch() but return a list of values for a single column.
		/// </summary>
		/// <param name="column"></param>
		/// <param name="query"></param>
		/// <returns></returns>
		public List<string> FetchSingleColumn(string column, string query)
		{
			using (SQLiteDataReader reader = this.CreateReader(query)) {
				List<string> values = new List<string>();

				// For each tuple in query result
				while (reader.Read()) {
					for (byte i = 0; i < reader.FieldCount; i++) {
						if (reader.GetName(i) == column) {
							values.Add(reader[i].ToString());
						}
					}
				}

				return values;
			}
		}

		/// <summary>
		/// Get index of a column from its name.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="collumn"></param>
		/// <returns></returns>
		public byte GetCollumnIndex(string table, string collumn)
		{
			using (SQLiteDataReader reader = this.CreateReader("SELECT * FROM " + table)) {
				return (byte)reader.GetOrdinal(collumn);
			}
		}

		/// <summary>
		/// Get name of a column from its index.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="collumn"></param>
		/// <returns></returns>
		public string GetCollumnName(string table, byte collumn)
		{
			using (SQLiteDataReader reader = this.CreateReader("SELECT * FROM " + table)) {
				return reader.GetName(collumn);
			}
		}

		/// <summary>
		/// For the given table, return an array containing 
		/// all columns name and their index.
		/// </summary>
		/// <param name="table"></param>
		/// <returns></returns>
		public List<string[]> GetCollumnsIndexName(string table)
		{
			SQLiteDataReader reader = this.CreateReader("SELECT * FROM " + table);
			List<string[]> tuples = new List<string[]>();
			byte count = 0;

			// For each tuple in query result
			while (reader.Read()) {
				tuples.Add(new string[] { count.ToString(), reader.GetName(count) });
				count++;
			}

			return tuples;
		}

		/// <summary>
		/// Get all tuple from a table with a WHERE clause.
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="where"></param>
		/// <returns></returns>
		public List<Dictionary<string, string>> GetAllTuplesWhere(string tableName, string where)
		{
			return this.Fetch("SELECT * FROM " + tableName + " " + where);
		}

		/// <summary>
		/// Optimize database. 
		/// </summary>
		public void Vacuum()
		{
			this.ExecuteAsync("VACUUM;");
		}

		/// <summary>
		/// Obtain the last ID of a table.
		/// </summary>
		/// <param name="table"></param>
		/// <returns></returns>
		public int LastId(string table)
		{
			using (SQLiteDataReader reader = this.CreateReader("SELECT seq FROM sqlite_sequence WHERE name='" + table + "'")) {
				if (reader.HasRows) {
					return Int32.Parse(reader["seq"].ToString());
				}
			}

			// In case sqlite_sequence does not have a record for the given table name, use a less efficient method instead
			using (SQLiteDataReader reader = this.CreateReader("SELECT id FROM " + table + " ORDER BY id DESC")) {
				while (reader.Read()) {
					return Int32.Parse(reader["id"].ToString());
				}
			}

			return 1;
		}

		/// <summary>
		/// Delete a tuple.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="id"></param>
		public void Delete(string table, int id)
		{
			this.ExecuteAsync("DELETE FROM " + table + " WHERE id=" + id);
		}

		/// <summary>
		/// Delete all tuples in a table (truncate).
		/// </summary>
		/// <param name="table"></param>
		public void EmptyTable(string table)
		{
			this.ExecuteAsync("DELETE FROM " + table + ";");
		}

		/// <summary>
		/// Give a new name to an existing table.
		/// </summary>
		/// <param name="actual"></param>
		/// <param name="renamed"></param>
		public void RenameTable(string actual, string renamed)
		{
			this.ExecuteAsync("ALTER TABLE " + actual + " RENAME TO " + renamed + ";");
		}

		/// <summary>
		/// Drop a table.
		/// </summary>
		/// <param name="table"></param>
		public void DropTable(string table)
		{
			this.ExecuteAsync("DROP TABLE IF EXISTS " + table + ";");
		}

		/// <summary>
		/// Add a collum to a table.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="column"></param>
		/// <param name="datatype">
		/// Example: "VARCHAR(255)"
		/// </param>
		/// <param name="check"></param>
		public bool AddColumn(string table, string column, string datatype, string defaultValue=null)
		{
			if (this.ColumnExists(table, column)) {
				return false;
			}

			string query = "ALTER TABLE " + table + " ADD COLUMN " + column + " " + datatype;

			if (defaultValue != null) {
				query += " DEFAULT '" + defaultValue + "'";
			}

			this.Execute(query + ";");

			return true;
		}

		// Execute une requete qui ne retourne pas de valeur (pas SELECT donc, mais CREATE ou DELETE par exemple)
		public int Execute(string query)
		{
			using (SQLiteCommand command = this.dbConnection.CreateCommand()) {
				command.CommandText = query;

				return command.ExecuteNonQuery();
			}
		}

		public void Transaction(List<string> queries)
		{
			using (SQLiteCommand command = new SQLiteCommand(this.dbConnection)) {
				using (SQLiteTransaction transaction = this.dbConnection.BeginTransaction()) {
					for (int i = 0; i < queries.Count; i++) {
						command.CommandText = queries[i];
						command.ExecuteNonQuery();
					}

					transaction.Commit();
				}
			}
		}

		public SQLiteDataReader CreateReader(string query)
		{
			using (SQLiteCommand command = new SQLiteCommand(query, this.dbConnection)) {
				return command.ExecuteReader();
			}
		}

		/// <summary>
		/// Get the database version from the user_version pragma.
		/// </summary>
		/// <returns></returns>
		public ushort GetUserVersion()
		{
			return ushort.Parse(this.CreateReader("PRAGMA user_version")[0].ToString());
		}

		#endregion

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Execute a query that doesn't return values (no SELECT but CREATE or DELETE for example).
		/// </summary>
		/// <param name="query"></param>
		private void ExecuteAsync(string query)
		{
			using (SQLiteCommand command = this.dbConnection.CreateCommand()) {
				command.CommandText = query;
				command.ExecuteNonQueryAsync();
			}
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string FilePath
		{
			get { return this.dbLocation + "/" + SQLite.BD_FILE_NAME; }
		}

		/// <summary>
		/// Check if the DB file exists.
		/// </summary>
		public bool Exists
		{
			get { return System.IO.File.Exists(this.FilePath); }
		}

		/// <summary>
		/// Get the version stored in database.
		/// </summary>
		/// <returns></returns>
		public ushort Version
		{
			get
			{
				// Prior to version 6 we used a table to store the value
				if (this.TableExists("version")) {
					return ushort.Parse(this.GetAValueByIndex("version", 0, "version"));
				}

				// Now we use the user_version pragma
				return this.GetUserVersion();
			}
			set
			{
				this.Execute("PRAGMA user_version = " + value.ToString());
			}
		}

		#endregion Accessor
	}
}
