using System;

/// <summary>
/// Finds informations about a serie or season from its title.
/// </summary>
namespace Hieda.Tool.Provider
{
	class MyAnimeList
	{
		private string animeUrl;

		// Retrieved values
		private string type;
		private string episodes;
		private string seasonal;
		private string year;
		private string studios;
		private string source;
		private string[] genres;
		private string[][] covers;

		/*
		============================================
		Public
		============================================
		*/

		/// <summary>
		/// Get informations from the anime's title.
		/// </summary>
		/// <param name="title"></param>
		public bool FromTitle(string title)
		{
			string url = this.GetAnimeUrl(title);

			if (url == null) {
				return false;
			}

			return this.FromUrl(url);
		}

		/// <summary>
		/// Get informations from the page's URL.
		/// </summary>
		/// <param name="url"></param>
		public bool FromUrl(string url)
		{
			this.animeUrl = url;

			bool success = false;

			success = this.GetInformations();
			success = this.GetCovers();

			return success;
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Get the URL to an anime's page from its title.
		/// </summary>
		/// <returns></returns>
		private string GetAnimeUrl(string title)
		{
			Supremes.Nodes.Document doc = null;

			// Search for the anime
			try {
				doc = Supremes.Dcsoup.Parse(
					new Uri("http://myanimelist.net/search/all?q="
					+ ((string)title).Trim().Replace(" ", "%20"))
					, 5000
				);
			} catch {
				return null;
			}

			if (doc == null) {
				return null;
			}

			Supremes.Nodes.Elements item = doc.Select("article > div.list.di-t.w100 > div.picSurround.di-tc.thumb");

			// Get first link in the list
			return item.Select("a").Attr("href");
		}

		/// <summary>
		/// Get general informations fro mthe page about this anime (type, episodes, premiered, studio).
		/// </summary>
		private bool GetInformations()
		{
			Supremes.Nodes.Document animeDoc = null;

			// Search for the anime
			try {
				animeDoc = Supremes.Dcsoup.Parse(new Uri(this.animeUrl), 5000);
			} catch {
				return false;
			}

			Supremes.Nodes.Elements sidebar = animeDoc.Select("div#content td.borderClass > div.js-scrollfix-bottom");
			Supremes.Nodes.Element h2 = sidebar.Select("h2").First;

			// There's 3 h2 elements in the sidebar: "Alternative Titles", "Information" and "Statistics"
			Supremes.Nodes.Element nextDiv = h2.NextElementSibling;

			while (nextDiv != null) {
				Supremes.Nodes.Elements spans = nextDiv.Select("span.dark_text");
				Supremes.Nodes.Element span = spans.First;

				if (span == null) {
					nextDiv = nextDiv.NextElementSibling;
					continue;
				}

				string text = span.Text.Trim();

				if (text.Length == 0) {
					nextDiv = nextDiv.NextElementSibling;

					continue;
				}

				if (text == "Type:") this.type = this.GetValue(nextDiv, text);
				if (text == "Episodes:") {
					this.episodes = this.GetValue(nextDiv, text);

					if (this.episodes == "Unknown") this.episodes = "0";
				}
				if (text == "Premiered:") {
					string premiered = this.GetValue(nextDiv, text);

					if (premiered != "?") {
						string[] parts = premiered.Split(' ');

						this.seasonal = parts[0];
						this.year = parts[1];
					}
				}
				if (text == "Studios:") {
					this.studios = this.GetValue(nextDiv, text);

					if (this.studios == "None found, add some") {
						this.studios = null;
					}
				}

				if (text == "Source:") {
					this.source = this.GetValue(nextDiv, text);
				}

				if (text == "Genres:") {
					Supremes.Nodes.Elements aElements = nextDiv.Select("a");
					this.genres = new string[aElements.Count];

					for (int i = 0; i < this.genres.Length; i++) {
						this.genres[i] = aElements[i].Attr("title");
					}
				}

				nextDiv = nextDiv.NextElementSibling;
			}

			return true;
		}

		private string GetValue(Supremes.Nodes.Element nextDiv, string text)
		{
			return nextDiv.Text.Replace(text + " ", "").Trim();
		}

		/// <summary>
		/// Get covers thumb and full URLs.
		/// </summary>
		/// <returns></returns>
		private bool GetCovers()
		{
			Supremes.Nodes.Document picsDoc = null;

			// Get pics
			try {
				picsDoc = Supremes.Dcsoup.Parse(new Uri(this.animeUrl + "/pics"), 5000);
			} catch {
				return false;
			}

			if (picsDoc == null) {
				return false;
			}

			Supremes.Nodes.Elements fulls = picsDoc.Select("div.picSurround > a.js-picture-gallery");
			Supremes.Nodes.Elements thumbs = picsDoc.Select("div.picSurround > a.js-picture-gallery > img");

			if (fulls.Count != thumbs.Count) {
				return false;
			}

			int count = thumbs.Count;
			string[][] covers = new string[count][];

			for (byte i = 0; i < count; i++) {
				string thumbUrl = thumbs[i].Attr("src");
				string fullUrl = fulls[i].Attr("href");

				// Lazyloaded
				if (String.IsNullOrEmpty(thumbUrl)) {
					thumbUrl = thumbs[i].Attr("data-src");
				}

				covers[i] = new string[] { thumbUrl, fullUrl };
			}

			this.covers = covers;

			return true;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public string Type
		{
			get { return this.type; }
		}

		public string Episodes
		{
			get { return this.episodes; }
		}

		public string Seasonal
		{
			get { return this.seasonal;  }
		}

		public string Year
		{
			get { return this.year; }
		}

		public string Studios
		{
			get { return this.studios; }
		}

		public string[] Genres
		{
			get { return this.genres; }
		}

		public string Source
		{
			get { return this.source; }
		}

		public string[][] Covers
		{
			get { return this.covers; }
		}
	}
}
