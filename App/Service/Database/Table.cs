using System;
using System.Collections.Generic;
using System.Linq;
using PropertyInfo = System.Reflection.PropertyInfo;
using Collumn = Hieda.Entity.Attribute.Collumn;

namespace Hieda.Service.Database
{
	/// <summary>
	/// Generate values from an entity that are usables by the database plateform.
	/// (CreateTable, Insert, Update)
	/// </summary>
	class Table
	{
		private string name;

		public Table(string name)
		{
			this.name = name;
		}

		/*
		======================================
		Public
		======================================
		*/

		#region Public

		/// <summary>
		/// Generate the table expression from an entity
		/// Expression is someting like:
		/// "id INTEGER PRIMARY KEY AUTOINCREMENT, title VARCHAR(255), cover VARCHAR(255), ..."
		/// </summary>
		/// <param name="entity">An instance of any entity</param>
		/// <returns></returns>
		public string GetExpression(object entity)
		{
			string expression = this.name + " (";

			expression += this.GetFieldsList(entity);

			expression += ")";
			expression = expression.Replace(", )", ")");

			return expression;
		}

		/// <summary>
		/// The the list of fields in an entity.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="onlyName"></param>
		/// <returns></returns>
		public string GetFieldsList(object entity, bool onlyName=false)
		{
			string expression = "";
			List<string> properties = this.GetPropertiesList(entity);
			byte count = (byte)properties.Count;

			for (byte i = 0; i < count; i++) {
				Collumn attr = this.GetAttributeFrom<Collumn>(entity, properties[i]);

				if (attr == null || !attr.IsMapped) {
					continue;
				}

				expression += attr.Name;

				if (!onlyName) {
					expression += " " + attr.Datatype;

					if (attr.Options != null) {
						expression += " " + attr.Options;
					}
				}

				expression += ", ";
			}

			if (expression.EndsWith(", ")) {
				expression = expression.Substring(0, expression.Length - 2);
			}

			return expression;
		}

		/// <summary>
		/// Get a pair of key:value for the given field of the given entity.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="field"></param>
		/// <returns></returns>
		public string[] GetKeyValue(object entity, string field)
		{
			PropertyInfo property = entity.GetType().GetProperty(field);
			Collumn attribute = this.GetAttributeFrom<Collumn>(entity, field);
			object value = property.GetValue(entity);

			string[] keyvalue = {
				attribute.Name,
				value != null ? this.StringValue(value.ToString()) : null
			};

			return keyvalue;
		}

		/// <summary>
		/// Get a list with keys and values to pass to Insert/Update fonctions from DB plateform.
		/// </summary>
		/// <param name="entity">An instance of any entity</param>
		/// <returns></returns>
		public List<string[]> GetKeysAndValues(object entity)
		{
			List<string[]> keysvalues = new List<string[]>();

			foreach (PropertyInfo property in entity.GetType().GetProperties()) {
				Collumn attribute = this.GetAttributeFrom<Collumn>(entity, property.Name);

				// Non-mapped fields doesn't have a column in table.
				if (attribute != null && attribute.IsMapped && attribute.Name != "id") {
					object propVal = property.GetValue(entity);

					keysvalues.Add(new string[] {
						attribute.Name,
						propVal != null ? this.StringValue(propVal.ToString()) : null
					});
				}
			}

			return keysvalues;
		}

		/// <summary>
		/// Fill an entity from a tuple by linking automaticaly
		/// entity properties and table collumns.
		///
		/// The first execution generate a table schema,
		/// the second just read the schema, allowing for better performances.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="entity"></param>
		/// <param name="tuple"></param>
		/// <returns></returns>
		public object FillEntityFromTuple(string table, object entity, Dictionary<string, string> tuple)
		{
			if (tuple == null) {
				return null;
			}

			PropertyInfo[] properties = entity.GetType().GetProperties();

			foreach (PropertyInfo property in properties) {
				Collumn attribute = this.GetAttributeFrom<Collumn>(entity, property.Name);

				if (attribute == null || attribute.Name == null || !tuple.ContainsKey(attribute.Name)) {
					continue;
				}

				string value = tuple[attribute.Name];
				Type type = property.PropertyType;

				if (type == typeof(Entity.DefaultStatus)) {
					Entity.DefaultStatus status = (Entity.DefaultStatus)Enum.Parse(typeof(Entity.DefaultStatus), value);
					property.SetValue(entity, status);
				} else if (type == typeof(Boolean)) {
					property.SetValue(entity, Convert.ChangeType((value == "1" ? true : false), type));
				} else if (!String.IsNullOrEmpty(value))  {
					try {
						property.SetValue(entity, Convert.ChangeType(value, type));
					} catch { }
				}
			}

			return entity;
		}

		#endregion Public

		/*
		======================================
		Private
		======================================
		*/

		#region Private

		/// <summary>
		/// Get names of all properties inside the given entity
		/// entity: an instance of any entity
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		private List<string> GetPropertiesList(object entity)
		{
			List<string> properties = new List<string>();

			foreach (PropertyInfo property in entity.GetType().GetProperties()) {
				properties.Add(property.Name);
			}

			return properties;
		}

		/// <summary>
		/// Get attribute from a propertie name.
		/// </summary>
		/// <typeparam name="T">
		/// Type of attribute (we want Attribute.Collumn here)
		/// </typeparam>
		/// <param name="entity">
		/// An instance of any entity
		/// </param>
		/// <param name="propertyName">
		/// Name of the propertie associated with the wanted attribute (exemple: "Id")
		/// </param>
		/// <returns></returns>
		private T GetAttributeFrom<T>(object entity, string propertyName) where T : System.Attribute
		{
			PropertyInfo property = entity.GetType().GetProperty(propertyName);

			if (property == null) {
				return null;
			}

			object[] customAttributes = property.GetCustomAttributes(typeof(T), false);

			// Return null if there is no attribute for this property
			if (customAttributes.Any()) {
				return (T)customAttributes.First();
			}

			return null;
		}

		/// <summary>
		/// Check for boolean converted to string.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private string StringValue(string value)
		{
			if (value == "True") {
				return "1";
			} else if (value == "False") {
				return "0";
			}

			return value;
		}

		#endregion Private
	}
}
