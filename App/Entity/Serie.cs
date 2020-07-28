using Hieda.Entity.Attribute;

namespace Hieda.Entity
{
	public class Serie : IEntity
	{
		public const string TABLE = "serie";

		[Collumn("id", "INTEGER", "PRIMARY KEY AUTOINCREMENT")]
		public int Id { get; set; }

		[Collumn("title", "VARCHAR(255)")]
		public string Title { get; set; }

		[Collumn("cover", "VARCHAR(255)")]
		public string Cover { get; set; }

		[Collumn("synopsis", "TEXT")]
		public string Synopsis { get; set; }

		[Collumn("status_id", "INTEGER")]
		public int StatusId { get; set; }

		[Collumn("seasons_count", "INTEGER", null, false)]
		public byte NumberOfSeasons { get; set; }

		[Collumn("episodes_watched", "INTEGER", null, false)]
		public ushort EpisodesViewed { get; set; }

		[Collumn("episodes_owned", "INTEGER", null, false)]
		public ushort EpisodesOwned { get; set; }

		[Collumn("episodes_total", "INTEGER", null, false)]
		public ushort EpisodesTotal { get; set; }

		[Collumn("seasons_last_watched", "INTEGER")]
		public int LastViewedSeasonId { get; set; }

		[Collumn("rpc_large_image", "VARCHAR(255)")]
		public string RpcLargeImage { get; set; }

		[System.ComponentModel.DefaultValue(null)]
		public Entity.UserStatus UserStatus { get; set; }
	}
}
