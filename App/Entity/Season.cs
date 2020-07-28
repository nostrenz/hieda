using Hieda.Entity.Attribute;

namespace Hieda.Entity
{
	public class Season : IEntity
	{
		public const string TABLE = "season";

		/// <summary>
		/// Mapped fields
		/// </summary>

		[Collumn("id", "INTEGER", "PRIMARY KEY AUTOINCREMENT")]
		public int Id { get; set; }

		[Collumn("serie_id", "INTEGER")]
		public int SerieId { get; set; }

		[Collumn("title", "VARCHAR(255)")]
		public string Title { get; set; }

		[Collumn("cover", "VARCHAR(255)")]
		public string Cover { get; set; }

		[Collumn("synopsis", "TEXT")]
		public string Synopsis { get; set; }

		[Collumn("number", "INTEGER")]
		public ushort Number { get; set; }

		[Collumn("episodes_total", "INTEGER")]
		public ushort EpisodesTotal { get; set; }

		[Collumn("status_id", "INTEGER")]
		public int StatusId { get; set; }

		[Collumn("studio_id", "INTEGER")]
		public int StudioId { get; set; }

		[Collumn("seasonal", "VARCHAR(255)")]
		public byte SeasonalIndex { get; set; }

		[Collumn("year", "INTEGER")]
		public ushort Year { get; set; }

		[Collumn("month", "INTEGER")]
		public byte Month { get; set; }

		[Collumn("day", "INTEGER")]
		public byte Day { get; set; }

		[System.ComponentModel.DefaultValue(null)]
		public Entity.UserStatus UserStatus { get; set; }

		[Collumn("type", "INTEGER")]
		public byte TypeIndex { get; set; }

		[Collumn("source", "INTEGER")]
		public byte SourceIndex { get; set; }

		[Collumn("rpc_large_image", "VARCHAR(255)")]
		public string RpcLargeImage { get; set; }

		[Collumn("wide_episodes", "INTEGER")]
		public bool WideEpisode { get; set; }

		[Collumn("grouping", "VARCHAR(255)")]
		public string Grouping { get; set; }

		/// <summary>
		/// Non-mapped fields
		/// </summary>

		[Collumn("episodes_watched", "INTEGER", null, false)]
		public ushort EpisodesViewed { get; set; }

		[Collumn("episodes_owned", "INTEGER", null, false)]
		public ushort EpisodesOwned { get; set; }

		// Used to hold a different title to display in the view
		[Collumn("display_title", "VARCHAR(255)", null, false)]
		public string DisplayTitle { get; set; }

		[Collumn("parent_cover", "VARCHAR(255)", null, false)]
		public string ParentCover { get; set; }

		/// <summary>
		/// Accessors
		/// </summary>

		public Constants.Seasonal Seasonal
		{
			get { return (Constants.Seasonal)this.SeasonalIndex; }
			set { this.SeasonalIndex = (byte)value; }
		}

		public Constants.Type Type
		{
			get { return (Constants.Type)this.TypeIndex; }
			set { this.TypeIndex = (byte)value; }
		}

		public Constants.Source Source
		{
			get { return (Constants.Source)this.SourceIndex; }
			set { this.SourceIndex = (byte)value; }
		}

		public string Premiered
		{
			get
			{
				string seasonal = "";
				string year = this.Year.ToString();

				int index = (int)this.Seasonal;

				switch ((int)this.Seasonal) {
					case 1: seasonal = Lang.WINTER; break;
					case 2: seasonal = Lang.SPRING; break;
					case 3: seasonal = Lang.SUMMER; break;
					case 4: seasonal = Lang.FALL; break;
					default: seasonal = ""; break;
				}

				if (year.Length != 4) {
					year = "";
				}

				if (seasonal.Length == 0 && year.Length == 0) {
					return Lang.UNKNOWN;
				}

				return seasonal + " " + year;
			}
		}

		public string DisplayCover
		{
			get
			{
				if (!System.String.IsNullOrEmpty(this.Cover)) {
					return this.Cover;
				}

				return this.ParentCover;
			}
		}
	}
}
