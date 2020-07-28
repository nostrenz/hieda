using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;

namespace Hieda.Tool
{
	public class File
	{
		// Example: @"C:\video.avi"
		private string path;

		// Those values depends on this.path
		private string fullPath = null;
		private bool exists = false;
		private string md5 = null;

		public File(string path="")
		{
			this.path = path;
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Execute the file.
		/// </summary>
		public void Call()
		{
			Process.Start(this.Path);
		}

		/// <summary>
		/// Copy the file with a different name.
		/// </summary>
		/// <param name="location"></param>
		/// <param name="name"></param>
		/// <param name="extension"></param>
		public void CopyRenamed(string location, string name, string extension = null)
		{
			if (extension == null) {
				System.IO.File.Copy(this.Path, location + name + this.Extension);
			} else {
				System.IO.File.Copy(this.Path, location + name + extension);
			}
		}

		/// <summary>
		/// Delete the file.
		/// </summary>
		public void Delete()
		{
			System.IO.File.Delete(this.Path);
		}

		/// <summary>
		/// Rename the file (including extension).
		/// </summary>
		/// <param name="name"></param>
		public void Rename(string name)
		{
			System.IO.File.Move(this.Path, Tool.Path.ContainingPath(this.Path) + name);
		}

		/// <summary>
		/// Copy the file to the system clipboard.
		/// </summary>
		public void CopyToClipboard()
		{
			StringCollection FileCollection = new StringCollection();
			FileCollection.Add(this.Path);
			Clipboard.SetFileDropList(FileCollection);
		}

		/// <summary>
		/// Returns true if the file extension is the same than the one given (ignore case).
		/// </summary>
		/// <param name="extension"></param>
		/// <returns></returns>
		public bool ExtensionIs(string extension)
		{
			return this.Extension.ToLower() == extension.ToLower();
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private
		
		/// <summary>
		/// Checks if this file's extension in among a given list.
		/// </summary>
		/// <param name="extensions"></param>
		/// <returns></returns>
		private bool ExtensionIsIn(string[] extensions)
		{
			if (!this.Exists) {
				return false;
			}

			return extensions.Contains(this.Extension.ToLower());
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		/// <summary>
		/// Calculate the MD5 hash of the file.
		/// </summary>
		public string MD5
		{
			get
			{
				if (this.md5 != null) {
					return this.md5;
				}

				using (Tool.Hash hashTool = new Tool.Hash()) {
					this.md5 = hashTool.Calculate(this.path);
				}

				return this.md5;
			}
		}

		public string Extension
		{
			get { return Tool.Path.Extension(this.path); }
		}

		/// <summary>
		/// Get the file's full path.
		/// </summary>
		public string Path
		{
			get
			{
				if (this.fullPath != null) {
					return this.fullPath;
				}

				this.fullPath = this.path.TrimStart(new char[] { '\\' });

				// Path is relative to the app folder
				if (this.path.StartsWith(@"files\")) {
					this.fullPath = AppDomain.CurrentDomain.BaseDirectory + this.fullPath.Substring(0, this.fullPath.Length);
				}

				return this.fullPath;
			}
			set
			{
				this.path = value;

				// Update depending values
				this.fullPath = null;
				this.exists = false;
				this.md5 = null;
			}
		}

		/// <summary>
		/// Check if we can extract a thumbnail from this file.
		/// </summary>
		public bool IsCompatibleWithThumb
		{
			get
			{
				if (!this.Exists) {
					return false;
				}

				string extension = this.Extension;

				return
					String.Compare(extension, ".mp4", true) == 0
					|| String.Compare(extension, ".avi", true) == 0
					|| String.Compare(extension, ".mov", true) == 0;
			}
		}

		public bool IsVideo
		{
			get	{ return this.ExtensionIsIn(Constants.VideoFileExtensions); }
		}

		public bool IsBook
		{
			get { return this.ExtensionIsIn(Constants.BookFileExtensions); }
		}

		/// <summary>
		/// Check if the file exists.
		/// </summary>
		public bool Exists
		{
			get
			{
				this.exists = System.IO.File.Exists(this.path);

				return this.exists;
			}
		}

		#endregion Accessor
	}
}
