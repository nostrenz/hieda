using System.Windows;
using System.Threading.Tasks;
using System.Collections.Generic;
using FullAccount = Dropbox.Api.Users.FullAccount;
using System.Linq;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for Dropbox.xaml
	/// </summary>
	public partial class Dropbox : System.Windows.Window
	{
		// Path the the database file in the Dropbox folder
		const string REMOTE_DB_PATH = "/db/hiedb.db";

		const string REMOTE_FULL_PATH = "/covers/full/";
		const string REMOTE_THUMB_PATH = "/covers/thumb/";

		private Tool.Dropbox dropboxTool = new Tool.Dropbox();
		private string localDbPath = Tool.Path.DatabaseFolder + @"\" + Service.Database.SQLite.BD_FILE_NAME;
		private bool needReload = false;
		private bool canBeClosed = true;

		public Dropbox()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.TryLogin();
			this.ShowDialog();
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Try to log into Dropbox then enable the buttons if successful.
		/// </summary>
		private void TryLogin()
		{
			if (this.dropboxTool.HasAccessToken && this.dropboxTool.Login()) {
				this.Button_Download.IsEnabled = true;
				this.Button_Upload.IsEnabled = true;

				this.Label_Authenticated.Content = Lang.Content("dropboxLogged");
				this.Label_Action.Content = Lang.Content("authenticated") + ".";
				this.Button_Authenticate.IsEnabled = false;

				this.SetAccountLabel();
			}
		}

		/// <summary>
		/// Get the current account then set the label once retrieved.
		/// </summary>
		private async void SetAccountLabel()
		{
			FullAccount fullAccount = await this.dropboxTool.GetCurrentAccount();

			this.Label_Authenticated.Content = fullAccount.Name.DisplayName + " - " + fullAccount.Email;
		}

		/// <summary>
		/// Prepare labels and buttons to start or end an action.
		/// </summary>
		/// <param name="start"></param>
		/// <param name="label"></param>
		private void PrepareAction(bool start, string label, int progress=0)
		{
			this.Grid_Actions.IsEnabled = !start;
			this.ProgressBar.IsIndeterminate = start;
			this.Label_Action.Content = label;
			this.ProgressBar.Value = progress;
		}

		/// <summary>
		/// Update the progress bar and label for a long operation.
		/// </summary>
		/// <param name="total"></param>
		/// <param name="progress"></param>
		/// <param name="status"></param>
		private void UpdateProgress(int total, int progress, string status)
		{
			double percentage = (double)progress / (double)total * 100;
			int truncated = int.Parse(System.Math.Truncate(percentage).ToString());

			this.Label_Action.Content = status + " " + (progress+1) + " / " + total + " (" + truncated + "%)";
			this.ProgressBar.Value = truncated;
		}

		/// <summary>
		/// Disconnect the database and prevent from closing the window.
		/// </summary>
		private void DisconnectDb(bool backup=false)
		{
			this.canBeClosed = false;

			if (backup) {
				App.db.Backup();
			}

			App.db.Disconnect();
		}

		/// <summary>
		/// Reconnect the database and allow to close the window again.
		/// </summary>
		private void ReconnectDb()
		{
			App.db.Connect();

			this.canBeClosed = true;
		}

		/// <summary>
		/// Delete from Dropbox all the covers that are not in the database.
		/// </summary>
		/// <param name="coverDir"></param>
		/// <param name="localFullCovers"></param>
		/// <param name="localThumbCovers"></param>
		/// <param name="remoteFullCovers"></param>
		/// <param name="remoteThumbCovers"></param>
		/// <returns></returns>
		private async Task DeleteRemoteCoversNotInDatabase(string coverDir, List<string> remoteFullCovers, List<string> remoteThumbCovers)
		{
			List<string> dbCovers = Tools.ListCoversInDatabase();
			List<string> remoteFullCoversToDelete = new List<string>();
			List<string> remoteThumbCoversToDelete = new List<string>();

			// Check fulls
			foreach (string remoteFullCover in remoteFullCovers) {
				if (!dbCovers.Contains(remoteFullCover) && !remoteFullCoversToDelete.Contains(remoteFullCover)) {
					remoteFullCoversToDelete.Add(remoteFullCover);
				}
			}

			// Check thumbs
			foreach (string remoteThumbCover in remoteThumbCovers) {
				if (!dbCovers.Contains(remoteThumbCover) && !remoteThumbCoversToDelete.Contains(remoteThumbCover)) {
					remoteThumbCoversToDelete.Add(remoteThumbCover);
				}
			}

			int deletableCount = remoteFullCoversToDelete.Count + remoteThumbCoversToDelete.Count;
			int deleted = 0;
			this.ProgressBar.IsIndeterminate = false;
			this.ProgressBar.Value = 0;

			// Delete full covers from Dropbox
			foreach (string remoteFullCoverToDelete in remoteFullCoversToDelete) {
				this.UpdateProgress(deletableCount, deleted, "Deleting cover");

				await this.dropboxTool.Delete(REMOTE_FULL_PATH + remoteFullCoverToDelete);

				// Remove cove from list so we won't try to download it latter
				remoteFullCovers.Remove(remoteFullCoverToDelete);

				deleted++;
			}

			// Delete thumb covers from Dropbox
			foreach (string remoteThumbCoverToDelete in remoteThumbCoversToDelete) {
				this.UpdateProgress(deletableCount, deleted, "Deleting cover");

				await this.dropboxTool.Delete(REMOTE_THUMB_PATH + remoteThumbCoverToDelete);

				// Remove cover from list so we won't try to download it latter
				remoteThumbCovers.Remove(remoteThumbCoverToDelete);

				deleted++;
			}
		}

		/// <summary>
		/// Download covers from Drobox that are not in the local folder.
		/// </summary>
		private async Task DownloadLocallyMissingCovers(string coverDir, string[] localFullCovers, string[] localThumbCovers, List<string> remoteFullCovers, List<string> remoteThumbCovers)
		{
			List<string> missingFullCovers = new List<string>();
			List<string> missingThumbCovers = new List<string>();

			// Find which covers from Dropbox are missing locally
			foreach (string remoteFullCover in remoteFullCovers) {
				if (!System.Array.Exists(localFullCovers, localFullCover => Tool.Path.LastSegment(localFullCover) == remoteFullCover)) {
					missingFullCovers.Add(remoteFullCover);
				}
			}
			foreach (string remoteThumbCover in remoteThumbCovers) {
				if (!System.Array.Exists(localThumbCovers, localThumbCover => Tool.Path.LastSegment(localThumbCover) == remoteThumbCover)) {
					missingThumbCovers.Add(remoteThumbCover);
				}
			}

			int missingCount = missingFullCovers.Count + missingThumbCovers.Count;
			int downloaded = 0;
			this.ProgressBar.IsIndeterminate = false;
			this.ProgressBar.Value = 0;

			// Download missing covers
			foreach (string missingFullCover in missingFullCovers) {
				this.UpdateProgress(missingCount, downloaded, "Downloading cover");

				await this.dropboxTool.Download(REMOTE_FULL_PATH + missingFullCover, (coverDir + @"\full\") + missingFullCover);

				downloaded++;
			}
			foreach (string missingThumbCover in missingThumbCovers) {
				this.UpdateProgress(missingCount, downloaded, "Downloading cover");

				await this.dropboxTool.Download(REMOTE_THUMB_PATH + missingThumbCover, (coverDir + @"\thumb\") + missingThumbCover);

				downloaded++;
			}
		}

		/// <summary>
		/// Upload covers from the local folder that are not on Dropbox.
		/// </summary>
		private async Task UploadRemotelyMissingCovers(string coverDir, string[] localFullCovers, string[] localThumbCovers, List<string> remoteFullCovers, List<string> remoteThumbCovers)
		{
			// Covers not on Dropbox
			List<string> missingFullCovers = new List<string>();
			List<string> missingThumbCovers = new List<string>();

			// Find local covers not in Dropbox
			foreach (string localFullCover in localFullCovers) {
				string filename = Tool.Path.LastSegment(localFullCover);

				if (!remoteFullCovers.Contains(filename)) {
					missingFullCovers.Add(filename);
				}
			}
			foreach (string localThumbCover in localThumbCovers) {
				string filename = Tool.Path.LastSegment(localThumbCover);

				if (!remoteThumbCovers.Contains(filename)) {
					missingThumbCovers.Add(filename);
				}
			}

			int missingCount = missingFullCovers.Count + missingThumbCovers.Count;
			int downloaded = 0;
			this.ProgressBar.IsIndeterminate = false;
			this.ProgressBar.Value = 0;

			// Upload missing covers
			foreach (string missingFullCover in missingFullCovers) {
				this.UpdateProgress(missingCount, downloaded, "Uploading cover");

				await this.dropboxTool.Upload((coverDir + @"\full\") + missingFullCover, REMOTE_FULL_PATH + missingFullCover);

				downloaded++;
			}
			foreach (string missingThumbCover in missingThumbCovers) {
				this.UpdateProgress(missingCount, downloaded, "Uploading cover");

				await this.dropboxTool.Upload((coverDir + @"\thumb\") + missingThumbCover, REMOTE_THUMB_PATH + missingThumbCover);

				downloaded++;
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public bool NeedReload
		{
			get { return this.needReload; }
		}

		/*
		============================================
		Event
		============================================
		*/

		/// <summary>
		/// Authenticate into Drobox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Authenticate_Click(object sender, RoutedEventArgs e)
		{
			if (this.dropboxTool.OAuth()) {
				this.TryLogin();
			}
		}

		/// <summary>
		/// Download the database file from Dropbox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void Button_DownloadDb_Click(object sender, RoutedEventArgs e)
		{
			this.PrepareAction(true, Lang.Content("downloading", "Downloading") + "...");
			this.DisconnectDb(true);

			if (System.IO.File.Exists(this.localDbPath)) {
				System.IO.File.Delete(this.localDbPath);
			}

			await this.dropboxTool.Download(REMOTE_DB_PATH, this.localDbPath);

			this.ReconnectDb();
			this.PrepareAction(false, Lang.Content("downloaded", "Downloaded") + ".", 100);

			// Will reload the collection
			this.needReload = true;

			// Check DB version for migration
			App.db.CheckForUpdates();
		}

		/// <summary>
		/// Upload the local database file to Dropbox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void Button_UploadDb_Click(object sender, RoutedEventArgs e)
		{
			this.PrepareAction(true, Lang.Content("uploading", "Uploading") + "...");
			this.DisconnectDb(false);

			await this.dropboxTool.Upload(this.localDbPath, REMOTE_DB_PATH);

			this.ReconnectDb();
			this.PrepareAction(false, Lang.Content("uploaded", "Uploaded") + ".", 100);
		}

		/// <summary>
		/// Called by clicking the "Synchronize" button under "Covers".
		/// Find locally missing covers from Dropbox and download them.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void Button_SyncCovers_Click(object sender, RoutedEventArgs e)
		{
			this.PrepareAction(true, "Listing covers...");

			string coverDir = Tool.Path.CoverFolder;

			// List local covers
			string[] localFullCovers = System.IO.Directory.GetFiles(coverDir + @"\full\");
			string[] localThumbCovers = System.IO.Directory.GetFiles(coverDir + @"\thumb\");

			// List remote covers
			List<string> remoteFullCovers = await this.dropboxTool.GetFilesList(REMOTE_FULL_PATH);
			List<string> remoteThumbCovers = await this.dropboxTool.GetFilesList(REMOTE_THUMB_PATH);

			await this.DeleteRemoteCoversNotInDatabase(coverDir, remoteFullCovers, remoteThumbCovers);
			await this.DownloadLocallyMissingCovers(coverDir, localFullCovers, localThumbCovers, remoteFullCovers, remoteThumbCovers);
			await this.UploadRemotelyMissingCovers(coverDir, localFullCovers, localThumbCovers, remoteFullCovers, remoteThumbCovers);

			this.PrepareAction(false, "Covers synchronized.", 100);

			// Will reload the collection
			this.needReload = true;
		}

		/// <summary>
		/// Custom window closing function.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			// PRevent closing the window
			if (!canBeClosed) {
				e.Cancel = true;
			}
		}
	}
}
