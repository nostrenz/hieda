using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for EpisodesRelocator.xaml
	/// </summary>
	public partial class EpisodesRelocator : System.Windows.Window
	{
		private bool validated = false;
		private bool moved = false;

		public EpisodesRelocator(List<Entity.Episode> episodes)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
			this.TextBox_FolderPath.IsFolderPicker = true;

			foreach (Entity.Episode episode in episodes) {
				// We'll use the "Fake" field temporarily to hold the "file exists" status
				episode.Fake = System.IO.File.Exists(episode.AbsoluteUri);
			}

			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();
			item.Header = "Locate selected files in...";
			item.Tag = "locateSelectedFiles";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			item = new MenuItem();
			item.Header = "Move selected files to...";
			item.Tag = "moveSelectedFiles";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			this.ListView.ItemsSource = episodes;
			this.ListView.ContextMenu = context;
		}

		/// <summary>
		/// List all files in the given folder path, including in subfolders.
		/// Code borrowed by Marisa from https://stackoverflow.com/a/2107294
		/// </summary>
		/// <param name="rootFolderPath"></param>
		/// <param name="fileSearchPattern"></param>
		/// <returns></returns>
		public IEnumerable<string> GetFileList(string rootFolderPath, string fileSearchPattern="*")
		{
			Queue<string> pending = new Queue<string>();
			pending.Enqueue(rootFolderPath);
			string[] tmp;

			while (pending.Count > 0) {
				rootFolderPath = pending.Dequeue();

				try {
					tmp = System.IO.Directory.GetFiles(rootFolderPath, fileSearchPattern);
				} catch (UnauthorizedAccessException) {
					continue;
				}

				for (int i = 0; i < tmp.Length; i++) {
					yield return tmp[i];
				}

				tmp = System.IO.Directory.GetDirectories(rootFolderPath);

				for (int i = 0; i < tmp.Length; i++) {
					pending.Enqueue(tmp[i]);
				}
			}
		}

		/*
		============================================
		Private
		============================================
		*/

		private async Task LocateEpisodes(string folderPath, System.Collections.IEnumerable items)
		{
			// Not an existing folder
			if (!System.IO.Directory.Exists(folderPath)) {
				MessageBox.Show("Invalid folder selected.");

				return;
			}

			List<string> files = new List<string>(this.GetFileList(folderPath));

			if (files.Count < 1) {
				return;
			}

			foreach (Entity.Episode episode in items) {
				// Already found, skip
				if (episode.Fake) {
					continue;
				}

				string episodeFileName = System.IO.Path.GetFileName(episode.AbsoluteUri);

				// Try to find this episode's files among the listed files
				foreach (string file in files) {
					string listedFileName = System.IO.Path.GetFileName(file);

					if (episodeFileName != listedFileName) {
						continue;
					}

					// File found
					episode.Uri = file;
					episode.Fake = true;
				}
			}

			this.ListView.Items.Refresh();
		}
		
		private async Task LocateSelectedEpisodes()
		{
			CommonOpenFileDialog cofd = new CommonOpenFileDialog { IsFolderPicker = true };

			if (cofd.ShowDialog() == CommonFileDialogResult.Ok) {
				await this.LocateEpisodes(cofd.FileName, this.ListView.SelectedItems);
			}
		}

		private async Task MoveSelectedEpisodes()
		{
			CommonOpenFileDialog cofd = new CommonOpenFileDialog { IsFolderPicker = true };

			if (cofd.ShowDialog() != CommonFileDialogResult.Ok) {
				return;
			}

			if (!System.IO.Directory.Exists(cofd.FileName)) {
				MessageBox.Show("Error", "The destination directory does not exists.");

				return;
			}

			this.ProgressBar.Value = 0;
			this.ProgressBar.Maximum = this.ListView.Items.Count;

			// Move selected episodes to the folder
			foreach (Entity.Episode episode in this.ListView.SelectedItems) {
				// File not found, skip
				if (!episode.Fake) {
					this.ProgressBar.Value++;

					continue;
				}

				string absoluteUri = episode.AbsoluteUri;
				string destination = cofd.FileName + @"\" + System.IO.Path.GetFileName(absoluteUri);

				// Move the file...
				System.IO.File.Move(absoluteUri, destination);

				// Update the URI
				episode.Uri = destination;

				this.ProgressBar.Value++;

				this.moved = true;
			}

			this.ListView.Items.Refresh();
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public List<Entity.Episode> Episodes
		{
			get { return (List<Entity.Episode>)this.ListView.ItemsSource; }
		}

		public bool Validated
		{
			get { return this.validated; }
		}

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		private async void Button_Search_Click(object sender, RoutedEventArgs e)
		{
			await this.LocateEpisodes(this.TextBox_FolderPath.Text, this.ListView.ItemsSource);
		}

		private void Button_Validate_Click(object sender, RoutedEventArgs e)
		{
			this.validated = true;

			this.Close();
		}

		/// <summary>
		/// Called by clicking on an item in one of the context menus.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void ContextMenu_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem mi = sender as MenuItem;

			if (mi == null) {
				return;
			}

			switch (mi.Tag.ToString()) {
				case "locateSelectedFiles":
					await this.LocateSelectedEpisodes();
				break;
				case "moveSelectedFiles":
					await this.MoveSelectedEpisodes();
				break;
			}
		}

		/// <summary>
		/// Custom window closing function.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (this.validated || !this.moved) {
				return;
			}

			// Some files were moved, warn the user about it
			bool clickedOnYes = new TwoButtonsDialog(
				"Some episode files were moved. If you close this window without validating the changes those episodes will have missing files.",
				"Warning", "Yes", "No"
			).Open();

			// Prevent the window to be closed
			if (!clickedOnYes) {
				e.Cancel = true;
			}
		}

		#endregion Event
	}
}
