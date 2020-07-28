using Hieda.Entity.Attribute;

namespace Hieda.Entity
{
	public class Studio : IEntity
	{
		public const string TABLE = "studio";

		[Collumn("id", "INTEGER", "PRIMARY KEY AUTOINCREMENT")]
		public int Id { get; set; }

		[Collumn("name", "VARCHAR(255)")]
		public string Name { get; set; }
	}
}
