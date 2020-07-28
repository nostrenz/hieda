using System;
using System.IO;
using System.Threading.Tasks;

namespace Hieda.Tool
{
	/// <summary>
	/// Check Github releases for an update to download.
	/// </summary>
	class Update
	{
		public Update()
		{
			// https://stackoverflow.com/questions/36006333/httpwebrequest-the-request-was-aborted-could-not-create-ssl-tls-secure-channel
			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
		}

		/// <summary>
		/// Asynchronously access and parse the Github latest release page to extract informations.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> RetrieveAsync()
		{
			return await Task.Run(() => this.Retrieve());
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Access and parse the Github latest release page to extract informations.
		/// </summary>
		/// <returns></returns>
		private bool Retrieve()
		{
			Supremes.Nodes.Document doc = Supremes.Dcsoup.Parse(new Uri(this.GithubLatestUrl), 5000);

			if (doc == null) {
				return false;
			}

			Supremes.Nodes.Element tag = doc.Select("div.release-meta > ul.tag-references a.css-truncate > span.css-truncate-target").First;
			Supremes.Nodes.Element changelog = doc.Select("div.release-body div.markdown-body").First;

			if (tag == null || changelog == null) {
				this.ParseErrorMessage();

				return false;
			}

			short release;

			// Release number is prefixed with 'r'
			if (!short.TryParse(tag.Text.Remove(1), out release)) {
				this.ParseErrorMessage();

				return false;
			}

			// Not a newer release
			if (release <= Constants.RELEASE) {
				return false;
			}

			this.Release = release;
			this.Changelog = changelog.Text;

			return true;
		}

		/// <summary>
		/// Display a message when the release page if found but can't be parsed.
		/// </summary>
		private void ParseErrorMessage()
		{
			System.Windows.Forms.MessageBox.Show(Lang.Text("githubParseError") + this.GithubLatestUrl);
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public short Release
		{
			get; set;
		}

		public string Changelog
		{
			get; set;
		}

		private string GithubLatestUrl
		{
			get { return Constants.GITHUB_URL + "/latest"; }
		}

		#endregion Accessor
	}
}
