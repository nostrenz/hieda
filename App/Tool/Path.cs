using System;
using Hieda.Properties;

namespace Hieda.Tool
{
	public static class Path
	{
		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Check if the given path has one of the file extension from the given array.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="filters"></param>
		/// <returns></returns>
		public static bool IsCorrespondingToFilter(string path, string[] filters)
		{
			byte correspondancesCounter = 0;

			foreach (string filter in filters) {
				if (Extension(path).ToLower() == filter) {
					correspondancesCounter++;
				}
			}

			return correspondancesCounter >= 1;
		}

		/// <summary>
		/// Note: Dot included (example: returns ".txt").
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string Extension(string path)
		{
			int dot = path.LastIndexOf(".");

			if (dot < 0) {
				return String.Empty;
			}

			return path.Substring(dot, path.Length - path.LastIndexOf("."));
		}

		/// <summary>
		/// Examples:
		/// E:\Anime -> Anime
		/// E:\Anime\lol.mp4 -> lol.mp4
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string LastSegment(string path)
		{
			return path.Substring(path.LastIndexOf(@"\") + 1, path.Length - path.LastIndexOf(@"\") - 1);
		}

		/// <summary>
		/// Example:
		/// E:\Anime\lol.mp4 -> E:\Anime\
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string ContainingPath(string path)
		{
			return path.Replace(LastSegment(path), "");
		}

		/// <summary>
		/// Remove characters forbidden by the Windows' filesystem.
		/// Returns a name and not a path (to provent replacing "C:\Program" by "CProgram" for example)
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static string ClearNameFromInvalidChars(string name)
		{
			var invalidChars = System.IO.Path.GetInvalidFileNameChars();

			foreach (char c in invalidChars) {
				name = name.Replace(c.ToString(), "");
			}

			return name.Trim();
		}

		#endregion Public

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public static string CoverFolder
		{
			get { return Settings.Default.CoverFolder.Replace(":AppFolder:", App.appFolder); }
		}

		public static string DatabaseFolder
		{
			get { return Settings.Default.Dbfolder.Replace(":AppFolder:", App.appFolder); }
		}

		public static string DbBackupFolder
		{
			get { return DatabaseFolder + @"\backups"; }
		}

		#endregion Accessor
	}
}
