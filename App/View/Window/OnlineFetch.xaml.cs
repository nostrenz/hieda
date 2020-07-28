using System;
using System.Windows;
using System.Windows.Input;
using Hieda.View.Element;
using System.Threading;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for OnlineFetch.xaml
	/// </summary>
	public partial class OnlineFetch : System.Windows.Window
	{
		private string selectedCover = null;
		private bool greenlight = false;
		private bool isSearching = false;

		private delegate void PerformDelegate();
		private delegate void SetDelegate(string text);
		private delegate void AddCoverDelegate(string thumbUrl, string fullUrl);
		private delegate void SetTextBoxDelegate(System.Windows.Controls.TextBox textBox, string text);
		private delegate bool IsValidSourceUrlDelegate(string text);

		private Thread searchThread;

		public OnlineFetch(string searchText)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.scroller.Focusable = false;
			this.Covers.Focusable = false;
			this.button_Select.IsEnabled = false;
			this.ComboBox_Source.SelectedIndex = 0;
			this.TextBox_Title.Text = searchText;
			this.label_Loading.Opacity = 0;
			
			this.ShowDialog();
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private void SetTextBox(System.Windows.Controls.TextBox textBox, string text)
		{
			textBox.Text = text;
		}

		/// <summary>
		/// Add a cover to the window.
		/// </summary>
		/// <param name="thumbUrl"></param>
		/// <param name="fullUrl"></param>
		private void AddCover(string thumbUrl, string fullUrl = null)
		{
			CoverSelectorItem cover = new CoverSelectorItem(thumbUrl, fullUrl);

			cover.Margin = new Thickness { Left = (this.Covers.Children.Count * 90) + 10, Right = 10 };
			cover.Clicked += new EventHandler(this.Cover_Clicked);

			this.Covers.Children.Add(cover);
		}

		/// <summary>
		/// Start fetching online covers.
		/// </summary>
		/// <param name="animeTitle"></param>
		private void StartSearch()
		{
			// Search only if we have a valid text
			if (String.IsNullOrWhiteSpace(this.TextBox_Title.Text)) {
				return;
			}

			this.isSearching = true;
			this.label_Loading.Content = Lang.Content("loading");
			this.label_Loading.Opacity = 1;
			this.Button_Search.Content = Lang.Content("cancel");
			this.button_Select.IsEnabled = false;

			this.Covers.Children.Clear();

			this.TextBox_Type.Clear();
			this.TextBox_Episodes.Clear();
			this.TextBox_Seasonal.Clear();
			this.TextBox_Year.Clear();
			this.TextBox_Genres.Clear();
			this.TextBox_Source.Clear();
			this.ComboBox_Studio.Items.Clear();

			this.searchThread = new Thread(new ParameterizedThreadStart(FindDataFromMal));
			this.searchThread.Start(this.TextBox_Title.Text);
		}

		/// <summary>
		/// Check if the given text is a valid URL for the selected source.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private bool IsValidSourceUrl(string text)
		{
			// MyAnimeList
			if (this.ComboBox_Source.SelectedIndex == 0 && text.StartsWith("https://myanimelist.net/anime/")) {
				return true;
			}

			return false;
		}

		/// <summary>
		/// Retrieves informations and covers from MyAnimeList.
		/// </summary>
		/// <param name="serieTitle"></param>
		private void FindDataFromMal(object serieTitle)
		{
			string title = (string)serieTitle;
			bool success = false;
			Tool.Provider.MyAnimeList provider = new Tool.Provider.MyAnimeList();

			// Search from a title or an URL
			if ((bool)Dispatcher.Invoke((IsValidSourceUrlDelegate)IsValidSourceUrl, title)) {
				success = provider.FromUrl(title);
			} else {
				success = provider.FromTitle(title);
			}

			// The provider encountered an error
			if (!success) {
				Dispatcher.Invoke((PerformDelegate)UpdateLabel);
				Dispatcher.Invoke((PerformDelegate)SearchEnded);

				return;
			}

			// Set informations
			Dispatcher.Invoke((SetTextBoxDelegate)SetTextBox, this.TextBox_Type, provider.Type);
			Dispatcher.Invoke((SetTextBoxDelegate)SetTextBox, this.TextBox_Episodes, provider.Episodes);
			Dispatcher.Invoke((SetTextBoxDelegate)SetTextBox, this.TextBox_Seasonal, provider.Seasonal);
			Dispatcher.Invoke((SetTextBoxDelegate)SetTextBox, this.TextBox_Year, provider.Year);
			Dispatcher.Invoke((SetTextBoxDelegate)SetTextBox, this.TextBox_Genres, String.Join(", ", provider.Genres));
			Dispatcher.Invoke((SetTextBoxDelegate)SetTextBox, this.TextBox_Source, provider.Source);

			if (provider.Studios != null) {
				Dispatcher.Invoke((SetDelegate)AddStudios, provider.Studios);
			}

			// Add covers
			if (provider.Covers != null) {
				foreach (string[] cover in provider.Covers) {
					Dispatcher.Invoke((AddCoverDelegate)AddCover, cover[0], cover[1]);
				}
			}

			// Remove the "Loading..." label
			Dispatcher.Invoke((PerformDelegate)UpdateLabel);
			Dispatcher.Invoke((PerformDelegate)SearchEnded);
		}

		/// <summary>
		/// Add studios in the combobox.
		/// </summary>
		/// <param name="studios"></param>
		private void AddStudios(string studios)
		{
			string[] parts = studios.Split(',');

			foreach (string studio in parts) {
				this.ComboBox_Studio.Items.Add(studio.Trim());
			}

			this.ComboBox_Studio.SelectedIndex = 0;
		}

		/// <summary>
		/// Download an image into the temp folder and keep its path in selectedCover.
		/// Close the window once finished.
		/// </summary>
		/// <param name="param"></param>
		private void DownloadImage(object param)
		{
			string url = (string)param;

			int index = url.LastIndexOf("/") + 1;
			string filename = url.Substring(index);

			string localFilename = @"temp\";

			if (!System.IO.Directory.Exists(localFilename)) {
				System.IO.Directory.CreateDirectory("temp");
			}

			localFilename += filename;

			try {
				using (System.Net.WebClient client = new System.Net.WebClient()) {
					client.DownloadFile(url, localFilename);
				}
			} catch { }

			if (System.IO.File.Exists(localFilename)) {
				Dispatcher.Invoke((SetDelegate)SetSelectedCover, localFilename);
			}

			Dispatcher.Invoke((PerformDelegate)Close);
		}

		private void UpdateLabel()
		{
			if (this.Covers.Children.Count == 0) {
				this.label_Loading.Content = Lang.NO_COVER_FOUND;
			} else {
				this.label_Loading.Opacity = 0;
			}
		}

		private void SetSelectedCover(string selected)
		{
			this.selectedCover = selected;
		}

		/// <summary>
		/// Called one a search has ended.
		/// </summary>
		private void SearchEnded()
		{
			this.isSearching = false;
			this.Button_Search.Content = Lang.Content("search");
			this.button_Select.IsEnabled = true;
		}

		private void StopSearch()
		{
			if (this.searchThread.IsAlive) {
				this.searchThread.Abort();
			}

			this.isSearching = false;
			this.Button_Search.Content = Lang.Content("search");
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string SelectedCover
		{
			get { return this.selectedCover; }
		}

		public string Type
		{
			get { return this.TextBox_Type.Text.Trim(); }
		}

		public int TypeIndex
		{
			get
			{
				switch (this.Type) {
					case "TV": return 1;
					case "OVA": return 2;
					case "Movie": return 3;
					case "Manga": return 4;
				}

				return 0;
			}
		}

		public string Episodes
		{
			get { return this.TextBox_Episodes.Text.Trim(); }
		}

		public string Seasonal
		{
			get { return this.TextBox_Seasonal.Text.Trim(); }
		}

		public int SeasonalIndex
		{
			get
			{
				switch (this.Seasonal) {
					case "Winter": return 1;
					case "Spring": return 2;
					case "Summer": return 3;
					case "Fall": return 4;
				}

				return 0;
			}
		}

		public ushort Year
		{
			get
			{
				if (Tools.IsNumeric(this.TextBox_Year.Text)) {
					return ushort.Parse(this.TextBox_Year.Text);
				}

				return 0;
			}
		}

		public string Studio
		{
			get { return (string)this.ComboBox_Studio.SelectedItem; }
		}

		public string[] Genres
		{
			get {
				if (String.IsNullOrEmpty(this.TextBox_Genres.Text)) {
					return new string[] { };
				}

				string[] genres = this.TextBox_Genres.Text.Split(',');

				// Trim the names
				for (byte i=0; i<genres.Length; i++) {
					genres[i] = genres[i].Trim();
				}

				return genres;
			}
		}

		public string Source
		{
			get { return this.TextBox_Source.Text.Trim(); }
		}

		public int SourceIndex
		{
			get
			{
				switch (this.Source) {
					case "Original": return 1;
					case "Manga": return 2;
					case "Web manga": return 3;
					case "Novel": return 4;
					case "Light novel": return 5;
				}

				// None
				return 0;
			}
		}

		public bool Greenlight
		{
			get { return this.greenlight; }
		}

		public bool ReplaceCover
		{
			get { return (bool)this.CheckBox_Cover.IsChecked; }
		}

		public bool ReplaceStudio
		{
			get { return (bool)this.CheckBox_Studios.IsChecked; }
		}

		public bool ReplaceType
		{
			get { return (bool)this.CheckBox_Type.IsChecked; }
		}

		public bool ReplaceEpisodes
		{
			get { return (bool)this.CheckBox_Episodes.IsChecked; }
		}

		public bool ReplacePremiered
		{
			get { return (bool)this.CheckBox_Premiered.IsChecked; }
		}

		public bool ReplaceGenres
		{
			get { return (bool)this.CheckBox_Genres.IsChecked; }
		}

		public bool ReplaceSource
		{
			get { return (bool)this.CheckBox_Source.IsChecked; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called by rolling the mouse wheel over the scrollviewer.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset + e.Delta);
		}

		/// <summary>
		/// Called by clicking on the apply button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Apply_Click(object sender, RoutedEventArgs e)
		{
			this.greenlight = true;
			this.button_Select.IsEnabled = false;

			// Download the selected cover if there is one
			if (this.Covers.Children.Count > 0 && this.selectedCover != null && this.ReplaceCover) {
				this.label_Loading.Content = Lang.DOWNLOADING_COVER;
				this.label_Loading.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#000000");
				this.label_Loading.Background.Opacity = 0.7;
				this.label_Loading.Opacity = 1;
				this.label_Loading.IsHitTestVisible = true;

				new Thread(new ParameterizedThreadStart(DownloadImage)).Start(this.selectedCover);
			} else {
				// Or just close the window
				this.Close();
			}
		}

		/// <summary>
		/// Called by clicking on one of the covers in the scrollviewer.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Cover_Clicked(object sender, EventArgs e)
		{
			// Obtain the full cover from the clicked image
			this.selectedCover = (string)sender;
		}

		/// <summary>
		/// Called by clicking the search button, start or cancel a search.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Search_Click(object sender, RoutedEventArgs e)
		{
			if (this.isSearching) {
				this.StopSearch();
			} else {
				this.StartSearch();
			}
		}

		#endregion Event
	}
}
