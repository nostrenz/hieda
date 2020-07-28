namespace Hieda.Tool
{
	/// <summary>
	/// Create a query string.
	/// </summary>
	public class QueryBuilder
	{
		private string table;
		private string query;

		public QueryBuilder(string table)
		{
			this.table = table;
		}

		/*
		======================================
		Public
		======================================
		*/

		#region Public

		public QueryBuilder Select(string scope="*", string alias=null)
		{
			this.query += "SELECT " + scope + " FROM " + this.table;

			if (alias != null) {
				this.query += " " + alias;
			}

			return this;
		}

		public QueryBuilder Where(string clause)
		{
			this.query += " WHERE " + clause;

			return this;
		}

		public QueryBuilder AndWhere(string clause)
		{
			this.query += " AND " + clause;

			return this;
		}

		public QueryBuilder OrWhere(string clause)
		{
			this.query += " OR " + clause;

			return this;
		}

		/// <summary>
		/// If using a param like ":label" in a Where clause,
		/// this will replace it with a value.
		/// </summary>
		/// <param name="label"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public QueryBuilder SetParam(string label, string value)
		{
			this.query = this.query.Replace(':' + label, value);

			return this;
		}

		public QueryBuilder SetParam(string label, int value)
		{
			this.query = this.query.Replace(':' + label, value.ToString());

			return this;
		}

		public QueryBuilder AddSelect(string clause) {
			this.query += " SELECT " + clause;

			return this;
		}

		public QueryBuilder OrderBy(string field, string order, bool nocase=false)
		{
			this.query += " ORDER BY " + field + " ";

			if (nocase) {
				this.query += "COLLATE NOCASE ";
			}

			this.query += order;

			return this;
		}

		public QueryBuilder OrderByClause(string clause)
		{
			this.query += " ORDER BY " + clause + " ";

			return this;
		}

		public string IfClause(string clause, string alias)
		{
			return ", IF(" + clause + ") AS " + alias;
		}

		public QueryBuilder InnerJoin(string table, string alias=null, string on=null)
		{
			this.Join("INNER", table, alias);

			if (on != null) {
				this.On(on);
			}

			return this;
		}

		public QueryBuilder FullJoin(string table, string alias=null, string on=null)
		{
			this.Join("FULL", table, alias);

			if (on != null) {
				this.On(on);
			}

			return this;
		}

		public QueryBuilder LeftJoin(string table, string alias=null, string on=null)
		{
			this.Join("LEFT", table, alias);

			if (on != null) {
				this.On(on);
			}

			return this;
		}

		public QueryBuilder RightJoin(string table, string alias=null, string on=null)
		{
			this.Join("RIGHT", table, alias);

			if (on != null) {
				this.On(on);
			}

			return this;
		}

		public QueryBuilder On(string clause)
		{
			this.query += " ON " +  clause;

			return this;
		}

		public QueryBuilder OnDuplicateKeyUpdate(string column, string value)
		{
			this.query += " ON DUPLICATE KEY UPDATE " + column + "=" + value;

			return this;
		}

		public QueryBuilder Limit(int value)
		{
			if (value > 0) {
				this.query += " LIMIT " + value;
			}

			return this;
		}

		public QueryBuilder Delete()
		{
			this.query += "DELETE FROM " + this.table;

			return this;
		}

		#endregion Public

		/*
		======================================
		Private
		======================================
		*/

		#region Private

		private void Join(string type, string table, string alias=null)
		{
			this.query += " " + type + " JOIN " + table;

			if (alias != null) {
				this.query += " " + alias;
			}
		}

		#endregion Private

		/*
		======================================
		Accessor
		======================================
		*/

		#region Accessor

		public string Query
		{
			get { return this.query; }
		}

		#endregion Accessor
	}
}
