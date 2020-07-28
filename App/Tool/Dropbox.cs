using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DropboxApi = Dropbox.Api;

namespace Hieda.Tool
{
	class Dropbox
	{
		private DropboxApi.DropboxClient dbx;

		/*
		============================================
		Public
		============================================
		*/

		/// <summary>
		/// Initialize the DropboxClient by login using an access token.
		/// </summary>
		public bool Login()
		{
			// No access token, we need a new one
			if (!this.HasAccessToken && !this.OAuth()) {
				return false;
			}

			this.dbx = new DropboxApi.DropboxClient(Properties.Settings.Default.DropboxAccessToken);

			return true;
		}

		/// <summary>
		/// Open a browser window to authenticate with Dropbox and obtain an access token.
		/// </summary>
		/// <returns></returns>
		public bool OAuth()
		{
			View.Window.OAuth oAuth = new View.Window.OAuth();
			oAuth.ShowDialog();

			if (!oAuth.Result || String.IsNullOrEmpty(oAuth.AccessToken)) {
				return false;
			}

			Properties.Settings.Default.DropboxAccessToken = oAuth.AccessToken;
			Properties.Settings.Default.Save();

			return true;
		}

		/// <summary>
		/// Get an object representing the current logged account.
		/// </summary>
		/// <returns></returns>
		public async Task<DropboxApi.Users.FullAccount> GetCurrentAccount()
		{
			return await this.dbx.Users.GetCurrentAccountAsync();
		}

		/// <summary>
		/// Get a list of all the files at the given path.
		/// </summary>
		/// <returns></returns>
		public async Task<List<string>> GetFilesList(string path)
		{
			List<string> files = new List<string>();
			var list = await this.dbx.Files.ListFolderAsync(path);

			foreach (var item in list.Entries.Where(i => i.IsFile)) {
				files.Add(item.Name);
			}

			return files;
		}

		/// <summary>
		/// Download a file from the application's folder.
		/// </summary>
		/// <param name="folder"></param>
		/// <param name="file"></param>
		/// <returns></returns>
		public async Task Download(string souceFilepath, string localFilepath)
		{
			using (var response = await this.dbx.Files.DownloadAsync(souceFilepath)) {
				byte[] buffer = await response.GetContentAsByteArrayAsync();

				System.IO.File.WriteAllBytes(localFilepath, buffer);
			}
		}

		/// <summary>
		/// Upload a file to the application's folder.
		/// </summary>
		/// <param name="folder"></param>
		/// <param name="file"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		public async Task Upload(string localFilepath, string destFilePath)
		{
			try {
				byte[] buffer = System.IO.File.ReadAllBytes(localFilepath);

				using (var mem = new System.IO.MemoryStream(buffer)) {
					var updated = await this.dbx.Files.UploadAsync(
						destFilePath,
						DropboxApi.Files.WriteMode.Overwrite.Instance,
						body: mem
					);
				}
			} catch (System.IO.IOException ioE) {
				System.Windows.MessageBox.Show(ioE.Message);
			}
		}

		/// <summary>
		/// Delete a file on Dropbox.
		/// </summary>
		/// <returns></returns>
		public async Task Delete(string path)
		{
			try {
				await this.dbx.Files.DeleteAsync(path);
			} catch (DropboxApi.ApiException<DropboxApi.Files.DeleteError>) { }
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// List files and folder at the root of the application's folder.
		/// </summary>
		/// <returns></returns>
		private async Task ListFolder(string path)
		{
			var list = await this.dbx.Files.ListFolderAsync(path);

			// show folders then files
			foreach (var item in list.Entries.Where(i => i.IsFolder)) {
				Console.WriteLine("D {0}/", item.Name);
			}

			foreach (var item in list.Entries.Where(i => i.IsFile)) {
				Console.WriteLine("F{0,8} {1}", item.AsFile.Size, item.Name);
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		/// <summary>
		/// Check if we have an access token.
		/// </summary>
		/// <returns></returns>
		public bool HasAccessToken
		{
			get { return !String.IsNullOrEmpty(Properties.Settings.Default.DropboxAccessToken); }
		}
	}
}
