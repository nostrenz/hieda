using Hieda.Entity.Attribute;
using Microsoft.WindowsAPICodePack.Shell;

namespace Hieda.Entity
{
	public class Episode : IEntity
	{
		public const string TABLE = "episode";

		[Collumn("id", "INTEGER", "PRIMARY KEY AUTOINCREMENT")]
		public int Id { get; set; }

		[Collumn("serie_id", "INTEGER")]
		public int SerieId { get; set; }

		[Collumn("season_id", "INTEGER")]
		public int SeasonId { get; set; }

		[Collumn("title", "VARCHAR(255)")]
		public string Title { get; set; }

		[Collumn("cover", "VARCHAR(255)")]
		public string Cover { get; set; }

		[Collumn("number", "INTEGER")]
		public ushort Number { get; set; }

		[Collumn("uri", "VARCHAR(255)")]
		public string Uri { get; set; }

		[Collumn("watched", "INTEGER")]
		public bool Watched { get; set; }

		[Collumn("parent_cover", "VARCHAR(255)", null, false)]
		public string ParentCover { get; set; }

		[System.ComponentModel.DefaultValue(false)]
		public bool Fake { get; set; }

		/// <summary>
		/// Obtain a File object.
		/// </summary>
		public Tool.File File
		{
			get { return new Tool.File(this.Uri); }
		}

		/// <summary>
		/// To Know if the Uri is a file.
		/// </summary>
		public bool IsFile
		{
			get
			{
				// Note: cannot use `new System.Uri(this.Uri)` here as it will block the thread
				//return System.Uri.IsWellFormedUriString(this.Uri, System.UriKind.RelativeOrAbsolute);

				return !this.IsUrl && !string.IsNullOrEmpty(this.Uri);
			}
		}

		/// <summary>
		/// To know if the Uri is an URL.
		/// </summary>
		public bool IsUrl
		{
			get
			{
				return System.Uri.IsWellFormedUriString(this.Uri, System.UriKind.Absolute)
					&& (this.Uri.StartsWith("http://") || this.Uri.StartsWith("https://"));
			}
		}

		/// <summary>
		/// Returns the absolute path to the file URI.
		/// </summary>
		public string AbsoluteUri
		{
			get
			{
				if (string.IsNullOrEmpty(this.Uri)) {
					return null;
				}

				// If path do not start by a drive letter (impliying a relative path, add the program's folder path)
				if (this.Uri.Substring(1, 2) != @":\") {
					return (System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase) + @"\").Replace(@"file:\", "") + this.Uri;
				}

				return this.Uri;
			}
		}

		/// <summary>
		/// Returns the absolute path to the thumbnail cover file.
		/// </summary>
		public string AbsoluteFullCoverPath
		{
			get { return Hieda.Tool.Path.CoverFolder + @"\full\" + this.Cover; }
		}

		/// <summary>
		/// Returns the absolute path to the thumbnail cover file.
		/// </summary>
		public string AbsoluteThumbCoverPath
		{
			get { return Hieda.Tool.Path.CoverFolder + @"\thumb\" + this.Cover; }
		}

		/// <summary>
		/// Create a Bitmap thumbnail from the file URI ans set it a cover.
		/// We use the WindowsApiCodePack but the FileThumbnail tool could also be used.
		/// </summary>
		/// <returns></returns>
		public System.Drawing.Bitmap Thumbnail
		{
			get
			{
				try {
					// We already have a cover registered
					if (this.Cover != null) {
						string absoluteThumbnail = this.AbsoluteThumbCoverPath;

						// The cover file exists, load it
						if (System.IO.File.Exists(absoluteThumbnail)) {
							return new System.Drawing.Bitmap(System.Drawing.Bitmap.FromFile(absoluteThumbnail));
						}

						// The cover file cannot be found, nullify it
						this.Cover = null;
					}

					ShellFile thumbNail = ShellFile.FromFilePath(this.AbsoluteUri);

					thumbNail.Thumbnail.AllowBiggerSize = false;
					thumbNail.Thumbnail.FormatOption = ShellThumbnailFormatOption.ThumbnailOnly;
					thumbNail.Thumbnail.RetrievalOption = ShellThumbnailRetrievalOption.Default;
					thumbNail.Thumbnail.CurrentSize = new System.Windows.Size(512, 512);

					try {
						System.Drawing.Bitmap bitmap = thumbNail.Thumbnail.ExtraLargeBitmap;

						if (bitmap == null) {
							return null;
						}

						// Name the cpver file with the bitmap's MD5
						using (Tool.Hash hashTool = new Tool.Hash()) {
							this.Cover = hashTool.Calculate(bitmap) + ".jpg";
						}

						string absoluteThumbCoverPath = this.AbsoluteThumbCoverPath;

						if (!System.IO.File.Exists(absoluteThumbCoverPath)) {
							// Create dirs if needed
							System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absoluteThumbCoverPath));

							// Write thumbnail on disk
							bitmap.Save(absoluteThumbCoverPath);
						}

						return bitmap;
					} catch (System.InvalidOperationException) {
						// The file may not have a thumbnail
					}
				}
				catch (System.Exception) { }

				return null;
			}
		}

		/// <summary>
		/// Get the cover to be displayed on a tile.
		/// </summary>
		public string DisplayCover
		{
			get
			{
				// No cover
				if (string.IsNullOrEmpty(this.Cover)) {
					return this.ParentCover;
				}
				
				// There's an existing cover
				if (System.IO.File.Exists(this.AbsoluteThumbCoverPath)) {
					return this.Cover;
				}

				return this.ParentCover;
			}
		}
	}
}
