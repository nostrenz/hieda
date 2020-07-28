using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Hieda.View.Element;

namespace Hieda.View.Window
{
	partial class EpisodeImport : System.Windows.Window
	{
		private ImportRow[] rows;
		private bool result = false; // True: Import, False: Cancel

		public EpisodeImport()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Constructor with filelist for addition by drag and drop.
		/// </summary>
		/// <param name="fileslist"></param>
		/// <param name="series"></param>
		/// <param name="currentSerie"></param>
		/// <param name="currentSeason"></param>
		internal EpisodeImport(List<string> fileslist, List<Entity.Serie> series, ushort lastEpisodeNumber=0, string defaultTitle=null, string currentSerie=null, string currentSeason=null)
		{
			InitializeComponent();

			this.SetFilelist(fileslist, lastEpisodeNumber, defaultTitle);
			this.textbox_Pattern.Text = this.rows[0].textbox_Filepath.Text;
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Returns the number of episodes marked as watched in the list.
		/// </summary>
		/// <returns></returns>
		public int GetViewedEpisodeNumber()
		{
			int count = 0;

			for (int i = 0; i < this.rows.Length; i++) {
				if (this.rows[i].Watched)
					count++;
			}

			return count;
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private string[,] ExtractFromFilename(string filename, string pattern)
		{
			filename = filename.Substring(0, filename.LastIndexOf('.') + 1); // Cut before the extension
			pattern = pattern.Substring(0, pattern.LastIndexOf('.') + 1);
			string[] partsToBeRemoved = new string[filename.Length]; // Will contains cutted parts to remove
			string[,] extractResults = new string[filename.Length, 2]; // [placehorlder, value]
			
			try {
				byte i = 0;

				while (pattern.Length != 0) {
					// Starting string (pattern): "[Y-F] :title: - :number: HD v2[HD][Anime-Ultime]"
					partsToBeRemoved[i] = pattern.Substring(0, pattern.IndexOf(":")); // Save the first partsToBoRemoved

					// -> ":title: - :number: HD v2[HD][Anime-Ultime]"
					if (partsToBeRemoved[i].Length > 0) {
						pattern = pattern.Replace(partsToBeRemoved[i], "");
					}

					extractResults[i, 0] = this.DetectPlaceholderOccurence(pattern); // Retrieve the first tag/variable (ici, ":title:"):
					pattern = pattern.Replace(extractResults[i, 0], ""); // -> " - :number: HD v2[HD][Anime-Ultime]"
					
					if (pattern.IndexOf(":") == -1) {
						partsToBeRemoved[i + 1] = pattern;
						pattern = ""; // No more placeholder, exit loop
					}

					i++;
				}

				for (i = 0; i < extractResults.Length / 2 && extractResults[i, 0] != null; i++) {
					if (partsToBeRemoved[i].Length > 0) {
						filename = filename.Replace(partsToBeRemoved[i], ""); // -> "Red Data Girl - 01 HD v2[HD][Anime-Ultime]"
					}

					extractResults[i, 1] = filename.Substring(0, filename.IndexOf(partsToBeRemoved[i + 1])); // Extract "Red Data Girl"
					filename = filename.Replace(extractResults[i, 1], ""); // -> " - 01 HD v2[HD][Anime-Ultime]"
				}
			} catch (Exception) {
				return null;
			}

			return extractResults;
		}

		private void SetFilelist(List<string> fileslist, ushort lastEpisodeNumber, string defaultTitle)
		{
			this.rows = new ImportRow[fileslist.Count()];

			for (ushort i = 0; i < fileslist.Count(); i++) {
				this.rows[i] = new ImportRow(fileslist[i]);
				this.listbow_FilesList.Items.Add(rows[i]);
				this.rows[i].DefaultTitle = defaultTitle;
				this.rows[i].Number = (i + lastEpisodeNumber + 1).ToString();
			}
		}

		/// <summary>
		/// Find placeholders in the pattern string.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private string DetectPlaceholderOccurence(string str)
		{
			string[] placeholders = { ":title:", ":number:" };
			string result = null;

			foreach (string placeholder in placeholders) {
				if (str.IndexOf(placeholder) == 0) {
					result = placeholder;
				}
			}

			return result;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public ImportRow[] Rows
		{
			get { return this.rows; }
		}

		/// <summary>
		/// Returns true when all rows are greenlighted.
		/// </summary>
		private bool RowGreenlight
		{
			get
			{
				bool b = false;

				for (byte i = 0; i < this.rows.Count(); i++) {
					b = this.rows[i].Greenlight;
				}

				return b;
			}
		}

		public bool Replace
		{
			get { return (bool)this.CheckBox_Replace.IsChecked; }
		}

		public bool Result
		{
			get { return this.result; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		private void button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.result = false;

			this.Close();
		}

		private void button_Import_Click(object sender, RoutedEventArgs e)
		{
			this.result = true;

			this.Close();
		}

		private void button_Test_Click(object sender, RoutedEventArgs e)
		{
			string[,] extractResults = this.ExtractFromFilename(this.rows[0].Filename, this.textbox_Pattern.Text);

			if (extractResults != null) {
				string testing = null;

				for (byte i = 0; i < extractResults.Length / 2 && extractResults[i, 0] != null; i++) {
					testing += extractResults[i, 0] + ": " + extractResults[i, 1] + "\n";
				}

				MessageBox.Show(testing, "Résultats trouvés");
			}
		}

		/// <summary>
		/// Apply found values from file names.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button_Apply_Click(object sender, RoutedEventArgs e)
		{
			string text = this.textbox_Pattern.Text.Trim();

			// No text, just name the episodes with the filename
			if (String.IsNullOrEmpty(text)) {
				foreach (ImportRow row in this.rows) {
					row.Title = System.IO.Path.GetFileNameWithoutExtension(row.Filename);
				}

				return;
			}

			foreach (ImportRow row in this.rows) {
				string[,] extractResults = this.ExtractFromFilename(row.Filename, text);

				if (extractResults != null) {
					for (byte i = 0; i < extractResults.Length / 2 && extractResults[i, 0] != null; i++) {
						if (extractResults[i, 0] == ":number:" && Tools.IsNumeric(extractResults[i, 1])) {
							row.Number = extractResults[i, 1];
						}

						if (extractResults[i, 0] == ":title:") {
							row.Title = extractResults[i, 1];
						}
					}
				}
			}
		}

		private void menuitem_Title_Click(object sender, RoutedEventArgs e)
		{
			this.textbox_Pattern.Text = this.textbox_Pattern.Text.Replace(this.textbox_Pattern.SelectedText, ":title:");
		}

		private void menuitem_Number_Click(object sender, RoutedEventArgs e)
		{
			this.textbox_Pattern.Text = this.textbox_Pattern.Text.Replace(this.textbox_Pattern.SelectedText, ":number:");
		}

		/// <summary>
		/// Replace the Pattern text by double clicking on a row (outside fields).
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FilesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (this.listbow_FilesList.SelectedItem != null) {
				ImportRow row = (ImportRow)this.listbow_FilesList.SelectedItem;
				this.textbox_Pattern.Text = row.Filename;
			}
		}

		private void Button_ApplyOrder_Click(object sender, RoutedEventArgs e)
		{
			int from = 1;

			try {
				from = Int32.Parse(this.textbox_orderFrom.Text);
			} catch { }

			for (byte i = 0; i < this.rows.Count(); i++) {
				this.rows[i].Number = (i + from).ToString();
			}
		}

		private void CheckBox_MarkAllAsWatched_Click(object sender, RoutedEventArgs e)
		{
			for (byte i = 0; i < this.rows.Count(); i++) {
				this.rows[i].Watched = (bool)((System.Windows.Controls.CheckBox)sender).IsChecked;
			}
		}

		#endregion Event
	}
}
