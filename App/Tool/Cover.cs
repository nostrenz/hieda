using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Hieda.Properties;

namespace Hieda.Tool
{
	/// <summary>
	/// Handle covers: Moving file, creating thumbnail...
	/// </summary>
	public class Cover : IDisposable
	{
		private Tool.File file;
		private string folder = Settings.Default.CoverFolder.Replace(":AppFolder:", AppDomain.CurrentDomain.BaseDirectory)+@"\";

		/// <summary>
		/// Needed by IDisposable.
		/// </summary>
		public void Dispose()
		{
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Create and add a cover file from the given path.
		/// </summary>
		/// <param name="filepath"></param>
		/// <param name="deleteOriginalImage"></param>
		public string Create(string filepath, bool deleteOriginalImage, bool keepFull=true, int width=Constants.NARROW_TILE_WIDTH, ImageFormat imageFormat=null)
		{
			if (!System.IO.File.Exists(filepath)) {
				return null;
			}

			this.file = new Hieda.Tool.File(filepath);

			if (keepFull) {
				if (deleteOriginalImage) {
					this.MoveTo("full");
				} else {
					this.CopyTo("full");
				}
			}

			this.GenerateThumbnail(width, imageFormat);

			return Hieda.Tool.Path.LastSegment(this.file.Path);
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// From http://codeverge.com/asp.net.drawinggdi/resizing-images-using-getthumbnailimage-ba/12515
		/// </summary>
		/// <param name="output"></param>
		private void GenerateThumbnail(int width=Constants.NARROW_TILE_WIDTH, ImageFormat imageFormat=null)
		{
			string output = this.folder + @"thumb\" + this.file.MD5 + this.file.Extension;

			// Abort if the previous file can't be deleted (probably in use by the program)
			if (!this.TryDeleteIfExists(output)) {
				return;
			}

			Directory.CreateDirectory(this.folder + @"thumb\");

			Image image = Image.FromFile(this.file.Path);
			int srcWidth = image.Width;
			int srcHeight = image.Height;
			Decimal sizeRatio = ((Decimal)srcHeight / srcWidth);
			int thumbHeight = Decimal.ToInt32(sizeRatio * width);
			Bitmap bmp = new Bitmap(width, thumbHeight);
			Graphics gr = Graphics.FromImage(bmp);
			gr.SmoothingMode = SmoothingMode.HighQuality;
			gr.CompositingQuality = CompositingQuality.HighQuality;
			gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
			Rectangle rectDestination = new Rectangle(0, 0, width, thumbHeight);
			gr.DrawImage(image, rectDestination, 0, 0, srcWidth, srcHeight, GraphicsUnit.Pixel);

			// Default the image format to JPEG
			if (imageFormat == null) {
				imageFormat = ImageFormat.Jpeg;
			}

			try {
				bmp.Save(output, imageFormat);
			} catch (Exception e) {
				Console.WriteLine("Error during thumbnail creation: " + e.Message);
			}

			// Liberate resources
			image.Dispose();
			bmp.Dispose();
			gr.Dispose();

			this.file.Path = output;
		}

		/// <summary>
		/// Move the cover to the given folder.
		/// </summary>
		/// <param name="folder">"full" ou "thumb"</param>
		private void MoveTo(string folder)
		{
			string newfile = this.folder + folder + @"\" + this.file.MD5 + this.file.Extension;
			Directory.CreateDirectory(this.folder + folder + @"\");

			// Abort if the previous file can't be deleted (probably in use by the program)
			if (!this.TryDeleteIfExists(newfile)) {
				return;
			}

			System.IO.File.Move(this.file.Path, newfile);

			this.file.Path = newfile;
		}

		/// <summary>
		/// Copy the cover to the given folder.
		/// </summary>
		/// <param name="folder">"full" ou "thumb"</param>
		private void CopyTo(string folder)
		{
			string newfile = this.folder + folder + @"\" + this.file.MD5 + this.file.Extension;
			Directory.CreateDirectory(this.folder + folder + @"\");

			// Abort if the previous file can't be deleted (probably in use by the program)
			if (!this.TryDeleteIfExists(newfile)) {
				return;
			}

			System.IO.File.Copy(this.file.Path, newfile);

			this.file.Path = newfile;
		}

		/// <summary>
		/// Try to delete a file if it exists.
		/// Returns true for success, false if something went wrong.
		/// </summary>
		/// <param name=""></param>
		public bool TryDeleteIfExists(string filepath)
		{
			// File does not exists, nothing to do
			if (!System.IO.File.Exists(filepath)) {
				return true;
			}

			try {
				System.IO.File.Delete(filepath);
			} catch {
				// Unable to delete
				return false;
			}

			// Successfully delete
			return true;
		}

		#endregion Private
	}
}
