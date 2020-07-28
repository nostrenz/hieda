using Hieda.Entity.Attribute;

namespace Hieda.Entity
{
	public class UserStatus : IEntity
	{
		public const string TABLE = "status";

		[Collumn("id", "INTEGER", "PRIMARY KEY AUTOINCREMENT")]
		public int Id { get; set; }

		[Collumn("text", "VARCHAR(255)")]
		public string Text { get; set; }

		[Collumn("type", "INTEGER")]
		public byte Type { get; set; }
	}

	public enum DefaultStatus
	{
		// When in a combo, those needs to be the selected index * -1
		None = 0,
		ToWatch = -1,
		Current = -2,
		StandBy = -3,
		Finished = -4,
		Dropped = -5,

		// Those are used in special cases accross the application and are never actually stored in db
		All = -999,
		Watched = -998,
		NotWatched = -997,
		Null = -996
	};
}
