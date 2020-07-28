namespace Hieda
{
	public static class Constants
	{
		public const ushort RELEASE                = 1;
		public const long   BUILD                  = 2007281749;
		public const ushort DB_VERSION             = 18; // Db will update itself if this number is greater than the stored one
		public const ushort NARROW_TILE_WIDTH      = 205;
		public const ushort WIDE_TILE_WIDTH        = 516;
		public const int    TILE_HEIGHT            = 318;
		public const byte   TILE_HORIZONTAL_MARGIN = 35;
		public const byte   TILE_VERTICAL_MARGIN   = 15;
		public const string IMAGE_FILTER           = "Image files|*.jpg;*.png;*.bmp;*.gif;*.jpeg";
		public const string VIDEO_FILTER           = "Video files|*.mkv;*.mp4;*.webm*.avi;*.flv;*.f4v;*.mov;*.m4v;*.ogm;";
		public const string BOOK_FILTER            = "Book files|*.cbz;*.cbr:*.cb7;*.pdf";
		public const string SUBTITLE_FILTER        = "Subtitle files|*.srt;*.ass;";
		public const string GITHUB_URL             = "https://github.com/nostrenz/hieda/releases";
		public const string VARIOUS                = "<various>";

		/// <summary>
		/// Get the narrow tile width as a double.
		/// </summary>
		public static double DoubleTileWidth
		{
			get { return Constants.NARROW_TILE_WIDTH; }
		}

		/// <summary>
		/// Get the tile height as a double.
		/// </summary>
		public static double DoubleTileHeight
		{
			get { return Constants.TILE_HEIGHT; }
		}

		/// <summary>
		/// List of accepted video file extensions when importing.
		/// </summary>
		public static string[] VideoFileExtensions
		{
			get
			{
				return new string[] { ".mkv", ".mp4", ".webm", ".avi", ".flv", ".f4v", ".mov", ".m4v", ".ogm" };
			}
		}

		/// <summary>
		/// List of accepted comicbook file extensions when importing.
		/// </summary>
		public static string[] BookFileExtensions
		{
			get
			{
				return new string[] { ".cbz", ".cbr", ".cb7", ".pdf" };
			}
		}

		/// <summary>
		/// List of accepted image file extensions when importing.
		/// </summary>
		public static string[] ImageFilesExtensions
		{
			get
			{
				return new string[] { ".jpg", ".png", ".bmp", ".gif", ".jpeg" };
			}
		}

		public enum Level
		{
			Serie,
			Season,
			Episode
		}

		public enum Notify
		{
			Info,
			Notif,
			Warning
		};

		public enum Seasonal
		{
			Null    = -1,
			Unknown = 0,
			Winter  = 1,
			Spring  = 2,
			Summer  = 3,
			Fall    = 4

		};

		public enum Type
		{
			Null      = -1,
			None      = 0,
			TV        = 1,
			OVA       = 2,
			Movie     = 3,
			Manga     = 4,
			Special   = 5,
			Novel     = 6,
			Doujinshi = 7
		};

		public enum Source
		{
			Null       = -1,
			None       = 0,
			Original   = 1,
			Manga      = 2,
			WebManga   = 3,
			Novel      = 4,
			LightNovel = 5
		};

		// Embeded themes. The first one is the default one.
		public static string[] embededThemes = { "Scroll", "MayaDark", "default" };
	}
}
