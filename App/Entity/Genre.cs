using Hieda.Entity.Attribute;

namespace Hieda.Entity
{
	public class Genre : IEntity
	{
		public const string TABLE = "genre";

		[Collumn("id", "INTEGER", "PRIMARY KEY AUTOINCREMENT")]
		public int Id { get; set; }

		[Collumn("name", "VARCHAR(255)")]
		public string Name { get; set; }
	}
}
