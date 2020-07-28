using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for AddSeason.xaml
	/// </summary>
	public partial class SeasonEdit : System.Windows.Window
	{
		private bool isNew = false;
		private bool greenlight = false;

		/// <summary>
		/// New season constructor.
		/// </summary>
		/// <param name="serieId"></param>
		/// <param name="userStatusList"></param>
		public SeasonEdit(int serieId, List<Entity.UserStatus> userStatusList)
		{
			InitializeComponent();

			this.InitContent();
			this.SetStatusInCombo(userStatusList);
			this.LoadGroupings(serieId);

			this.isNew = true;
			this.Title = Lang.Text("addSeason", "Add a season");
			this.Browse_Cover.AddFilter(Constants.IMAGE_FILTER);
			this.DateInput.Seasons.SelectedIndex = 0;
			this.Radio_Wide.IsChecked = true;
		}

		/// <summary>
		/// Existing season constructor.
		/// </summary>
		/// <param name="season"></param>
		/// <param name="userStatusList"></param>
		public SeasonEdit(Entity.Season season, List<Entity.UserStatus> userStatusList)
		{
			InitializeComponent();

			this.InitContent(season);
			this.SetStatusInCombo(userStatusList);
			this.LoadGroupings(season.SerieId);

			this.Title = Lang.Text("editSeason", "Edit a season");
			this.Button_Add.Content = Lang.EDIT;
			this.Browse_Cover.AddFilter(Constants.IMAGE_FILTER);
			this.SeasonTitle = season.Title;
			this.Number = season.Number;
			this.EpisodesCount = season.EpisodesTotal;
			this.Cover = (season.Cover == "no") ? "" : season.Cover;
			this.StudioId = season.StudioId;
			this.Seasonal = season.Seasonal;
			this.Year = season.Year;
			this.Month = season.Month;
			this.Day = season.Day;
			this.Cover = season.Cover;
			this.StatusId = season.StatusId;
			this.ComboBox_Source.SelectedIndex = season.SourceIndex;
			this.TextBox_Grouping.Text = season.Grouping;

			this.DateInput.Seasons.SelectedIndex = (int)season.Seasonal;
			this.ComboBox_Type.SelectedIndex = (int)season.Type;
			
			if (season.WideEpisode) {
				this.Radio_Wide.IsChecked = true;
			} else {
				this.Radio_Narrow.IsChecked = true;
			}
		}

		/// <summary>
		/// Multiple seasons constructor.
		/// </summary>
		/// <param name="seasons"></param>
		/// <param name="userStatusList"></param>
		public SeasonEdit(List<Entity.Season> seasons, List<Entity.UserStatus> userStatusList)
		{
			InitializeComponent();

			this.InitContent();
			this.SetStatusInCombo(userStatusList);

			if (seasons.Count < 1) {
				return;
			}

			this.LoadGroupings(seasons[0].SerieId);

			// Window title
			this.Title = String.Format(Lang.Text("editingSeasons"), seasons.Count);
			this.textbox_Number.AllowEmpty = true;

			string title = seasons[0].Title;
			ushort year = seasons[0].Year;
			byte month = seasons[0].Month;
			byte day = seasons[0].Day;
			string grouping = seasons[0].Grouping;
			ushort number = seasons[0].Number;
			string cover = seasons[0].Cover;
			ushort episodesTotal = seasons[0].EpisodesTotal;
			Constants.Type type = seasons[0].Type;
			Constants.Source source = seasons[0].Source;
			Constants.Seasonal seasonal = seasons[0].Seasonal;
			int studioId = seasons[0].StudioId;
			int statusId = seasons[0].StatusId;

			foreach (Entity.Season season in seasons) {
				// There's at least two different titles
				if (title != Constants.VARIOUS && title != season.Title) {
					title = Constants.VARIOUS;
				}

				if (year != season.Year) {
					this.DateInput.Year.Text = Constants.VARIOUS;
				}

				if (month != season.Month) {
					this.DateInput.Month.Text = Constants.VARIOUS;
				}

				if (day != season.Day) {
					this.DateInput.Day.Text = Constants.VARIOUS;
				}

				if (grouping != Constants.VARIOUS && grouping != season.Grouping) {
					grouping = Constants.VARIOUS;
				}

				if (number != season.Number) {
					this.textbox_Number.Text = Constants.VARIOUS;
				}

				if (cover != season.Cover) {
					this.Browse_Cover.Text = Constants.VARIOUS;
				}

				if (episodesTotal != season.EpisodesTotal) {
					this.Textbox_Episodes_Count.Text = Constants.VARIOUS;
				}

				if (type != season.Type) {
					type = Constants.Type.Null;

					this.AddVariousComboBoxItem(this.ComboBox_Type, type);
				}

				if (source != season.Source) {
					source = Constants.Source.Null;

					this.AddVariousComboBoxItem(this.ComboBox_Source, source);
				}

				if (seasonal != season.Seasonal) {
					seasonal = Constants.Seasonal.Null;

					this.AddVariousComboBoxItem(this.DateInput.Seasons, seasonal);
				}

				if (studioId != season.StudioId) {
					studioId = -1;

					this.AddVariousComboBoxItem(this.ComboBox_Studio, studioId);
				}

				if (statusId != season.StatusId) {
					statusId = (int)Entity.DefaultStatus.Null;

					this.AddVariousComboBoxItem(this.ComboBox_Status, statusId);
				}
			}

			this.TextBox_Title.Text = title;
			this.TextBox_Grouping.Text = grouping;
			this.Radio_Narrow.IsChecked = false;
			this.Radio_Wide.IsChecked = false;
			this.Type = type;
			this.Seasonal = seasonal;
			this.Source = source;
			this.StudioId = studioId;
			this.StatusId = statusId;
			this.Cover = cover;
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Initialize the window's content.
		/// </summary>
		private void InitContent(Entity.Season season=null)
		{
			this.Owner = App.Current.MainWindow;
			List<Entity.Studio> studios = Repository.Studio.Instance.GetAll();

			foreach (Entity.Studio studio in studios) {
				this.AddCommboBoxItem(this.ComboBox_Studio, studio.Name, studio.Id);

				if (season != null && studio.Id == season.StudioId) {
					this.SelectValue(this.ComboBox_Studio, studio.Name);
				}
			}

			// Add types
			foreach (Constants.Type type in Enum.GetValues(typeof(Constants.Type))) {
				this.AddCommboBoxItem(this.ComboBox_Type, Lang.Content(type.ToString().ToLower()), type);
			}

			// Add sources
			foreach (Constants.Source source in Enum.GetValues(typeof(Constants.Source))) {
				this.AddCommboBoxItem(this.ComboBox_Source, Lang.Content(source.ToString().ToLower()), source);
			}
		}

		/// <summary>
		/// Add groupings to the combobox.
		/// </summary>
		/// <param name="serieId"></param>
		private void LoadGroupings(int serieId)
		{
			List<string> results = App.db.FetchSingleColumn("grouping", "SELECT DISTINCT grouping FROM " + Entity.Season.TABLE + " WHERE serie_id = " + serieId);

			foreach (string result in results) {
				this.ComboBox_Groupings.Items.Add(result);
			}
		}

		/// <summary>
		/// Add a new item in a ComboBox.
		/// </summary>
		/// <param name="comboBox"></param>
		/// <param name="content"></param>
		/// <param name="tag"></param>
		private void AddCommboBoxItem(ComboBox comboBox, string content, object tag)
		{
			comboBox.Items.Add(new ComboBoxItem() {
				Content = content,
				Tag = tag
			});
		}

		/// <summary>
		/// Add a "<various>" item in a ComboBox.
		/// </summary>
		/// <param name="comboBox"></param>
		/// <param name="nullValue"></param>
		private void AddVariousComboBoxItem(ComboBox comboBox, object nullValue)
		{
			ComboBoxItem item = new ComboBoxItem() {
				Content = Constants.VARIOUS,
				Tag = nullValue
			};

			comboBox.Items.Insert(0, item);
			comboBox.SelectedItem = item;
		}

		/// <summary>
		/// Add user stats in the Status combobox.
		/// </summary>
		/// <param name="userStatusList"></param>
		private void SetStatusInCombo(List<Entity.UserStatus> userStatusList)
		{
			// Add default status
			this.AddCommboBoxItem(this.ComboBox_Status, Lang.Content("none"), Entity.DefaultStatus.None);
			this.AddCommboBoxItem(this.ComboBox_Status, Lang.Content("toWatch"), Entity.DefaultStatus.ToWatch);
			this.AddCommboBoxItem(this.ComboBox_Status, Lang.Content("current"), Entity.DefaultStatus.Current);
			this.AddCommboBoxItem(this.ComboBox_Status, Lang.Content("standBy"), Entity.DefaultStatus.StandBy);
			this.AddCommboBoxItem(this.ComboBox_Status, Lang.Content("finished"), Entity.DefaultStatus.Finished);
			this.AddCommboBoxItem(this.ComboBox_Status, Lang.Content("dropped"), Entity.DefaultStatus.Dropped);

			this.ComboBox_Status.Items.Add(new Separator());

			// Add user status
			foreach (Entity.UserStatus userStatus in userStatusList) {
				this.AddCommboBoxItem(this.ComboBox_Status, userStatus.Text, userStatus.Id);
			}
		}

		/// <summary>
		/// 'comboBox.SelectedValue = value;' doesn't seems to work anymore, use this instead.
		/// </summary>
		/// <param name="comboBox"></param>
		/// <param name="value"></param>
		private void SelectValue(ComboBox comboBox, string value)
		{
			// Doesn't work anymore?
			comboBox.SelectedValue = value;

			foreach (ComboBoxItem item in comboBox.Items) {
				if ((string)item.Content == value) {
					comboBox.SelectedItem = item;

					return;
				}
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string SeasonTitle
		{
			get { return String.IsNullOrWhiteSpace(this.TextBox_Title.Text) ? null : this.TextBox_Title.Text; }
			set { this.TextBox_Title.Text = value; }
		}

		public ushort Number
		{
			get { return (ushort)this.textbox_Number.Number; }
			set
			{
				this.textbox_Number.Text = value.ToString();
				this.TextBox_Title.Text = Tools.UpperFirst(Lang.SEASON) + " " + value;
			}
		}

		public bool NumberIsVarious
		{
			get { return this.textbox_Number.Text == Constants.VARIOUS; }
		}

		public ushort EpisodesCount
		{
			get { return (ushort)this.Textbox_Episodes_Count.Number; }
			set { this.Textbox_Episodes_Count.Text = value.ToString(); }
		}

		/// <summary>
		/// Get or set the selected status from the Status combobox.
		/// </summary>
		public int StatusId
		{
			get
			{
				ComboBoxItem selectedItem = (ComboBoxItem)this.ComboBox_Status.SelectedItem;

				if (selectedItem == null) {
					return 0;
				}

				return (int)selectedItem.Tag;
			}
			set
			{
				foreach (object item in this.ComboBox_Status.Items) {
					// Ignore separator item
					if (item.GetType() != typeof(ComboBoxItem)) {
						continue;
					}

					if ((int)((ComboBoxItem)item).Tag == value) {
						this.ComboBox_Status.SelectedItem = item;

						return;
					}
				}
			}
		}

		public int StudioId
		{
			get
			{
				ComboBoxItem selectedItem = (ComboBoxItem)this.ComboBox_Studio.SelectedItem;

				if (selectedItem == null) {
					return 0;
				}

				return (int)selectedItem.Tag;
			}
			set
			{
				if (value == 0) {
					return;
				}

				if (this.ComboBox_Studio.Items.Count > 0) {
					Entity.Studio studio = Repository.Studio.Instance.Find(value);

					if (studio != null) {
						this.SelectValue(this.ComboBox_Studio, studio.Name);
					}
				}
			}
		}

		public Constants.Seasonal Seasonal
		{
			get
			{
				ComboBoxItem selectedItem = (ComboBoxItem)this.DateInput.Seasons.SelectedItem;

				if (selectedItem == null) {
					return Constants.Seasonal.Null;
				}

				return (Constants.Seasonal)selectedItem.Tag;
			}
			set
			{
				foreach (ComboBoxItem item in this.DateInput.Seasons.Items) {
					if ((Constants.Seasonal)item.Tag == value) {
						this.DateInput.Seasons.SelectedItem = item;

						break;
					}
				}
			}
		}

		public ushort Year
		{
			get { return (ushort)this.DateInput.Year.Number; }
			set { this.DateInput.Year.Number = value; }
		}

		public byte Month
		{
			get { return (byte)this.DateInput.Month.Number; }
			set { this.DateInput.Month.Number = value; }
		}

		public byte Day
		{
			get { return (byte)this.DateInput.Day.Number; }
			set { this.DateInput.Day.Number = value; }
		}

		public string Cover
		{
			get { return this.Browse_Cover.ActualText; }
			set
			{
				if (!String.IsNullOrEmpty(value) && value.Length > 0) {
					this.Browse_Cover.Text = value;
				}
			}
		}

		public bool DeleteImage
		{
			get { return (bool)this.CheckBox_CoverDelete.IsChecked; }
		}

		public bool Greenlight
		{
			get { return this.greenlight; }
		}

		public Constants.Type Type
		{
			get
			{
				ComboBoxItem selectedItem = (ComboBoxItem)this.ComboBox_Type.SelectedItem;

				if (selectedItem == null) {
					return Constants.Type.Null;
				}

				return (Constants.Type)selectedItem.Tag;
			}
			set
			{
				foreach (ComboBoxItem item in this.ComboBox_Type.Items) {
					if ((Constants.Type)item.Tag == value) {
						this.ComboBox_Type.SelectedItem = item;

						break;
					}
				}
			}
		}

		public Constants.Source Source
		{
			get
			{
				ComboBoxItem selectedItem = (ComboBoxItem)this.ComboBox_Source.SelectedItem;

				if (selectedItem == null) {
					return Constants.Source.Null;
				}

				return (Constants.Source)selectedItem.Tag;
			}
			set
			{
				foreach (ComboBoxItem item in this.ComboBox_Source.Items) {
					if ((Constants.Source)item.Tag == value) {
						this.ComboBox_Source.SelectedItem = item;

						break;
					}
				}
			}
		}

		public bool WideEpisodes
		{
			get { return (bool)this.Radio_Wide.IsChecked; }
		}

		public string Grouping
		{
			get { return this.TextBox_Grouping.Text.Trim(); }
		}

		public bool EpisodesCountIsVarious
		{
			get { return this.Textbox_Episodes_Count.Text == Constants.VARIOUS; }
		}

		public bool BothWideEpisodeRadiosAreUnchecked
		{
			get { return !(bool)this.Radio_Narrow.IsChecked && !(bool)this.Radio_Wide.IsChecked; }
		}

		#endregion

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called by clicking on the Cancel button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Called by clicking on the Add button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Add_Click(object sender, RoutedEventArgs e)
		{
			this.greenlight = true;

			this.Close();
		}

		/// <summary>
		/// Called by clicking on the Fetch button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Fetch_Click(object sender, RoutedEventArgs e)
		{
			View.Window.OnlineFetch fetcher = new View.Window.OnlineFetch(this.TextBox_Title.Text);

			if (fetcher.ReplaceEpisodes) {
				this.Textbox_Episodes_Count.Text = fetcher.Episodes;
			}

			if (fetcher.ReplaceStudio) {
				this.SelectValue(this.ComboBox_Studio, fetcher.Studio);
			}

			if (fetcher.ReplacePremiered) {
				this.DateInput.Seasons.SelectedIndex = fetcher.SeasonalIndex;
				this.Year = fetcher.Year;
			}

			if (fetcher.ReplaceType) {
				this.ComboBox_Type.SelectedIndex = fetcher.TypeIndex;
			}

			if (fetcher.ReplaceSource) {
				this.ComboBox_Source.SelectedIndex = fetcher.SourceIndex;
			}

			if (fetcher.ReplaceCover && fetcher.SelectedCover != null) {
				this.Browse_Cover.Text = fetcher.SelectedCover;
			}
		}

		/// <summary>
		/// Called by selecting a different type.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!this.isNew || !this.ComboBox_Type.IsLoaded) {
				return;
			}

			if (this.Type == Constants.Type.Manga || this.Type == Constants.Type.Doujinshi || this.Type == Constants.Type.Novel) {
				this.Radio_Narrow.IsChecked = true;
			} else {
				this.Radio_Wide.IsChecked = true;
			}

			string type = this.Type.ToString();

			if (this.Type == Constants.Type.None) {
				this.TextBox_Grouping.Text = null;
			} else {
				this.TextBox_Grouping.Text = this.Type.ToString();
			}
		}

		/// <summary>
		/// Called by selecting another grouping in the combobox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_Groupings_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.TextBox_Grouping.Text = this.ComboBox_Groupings.SelectedValue.ToString();
		}

		#endregion
	}
}
