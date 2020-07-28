using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QueryBuilder = Hieda.Tool.QueryBuilder;
using Hieda.Properties;
using Level = Hieda.Constants.Level;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for CustomSearch.xaml
	/// </summary>
	public partial class CustomSearch : System.Windows.Window
	{
		private bool needReload = false;
		private bool seasonJoinExists = false;
		private Level level;

		private List<Entity.Genre> genres;
		private List<Entity.Studio> studios;

		public CustomSearch(Level level, List<Entity.UserStatus> userStatusList)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
			this.level = level;

			foreach (Entity.UserStatus userStatus in userStatusList) {
				this.combo_Status.Items.Add(new ComboBoxItem() { Tag = userStatus.Id, Content = userStatus.Text });
			}

			this.studios = Repository.Studio.Instance.GetAll();

			foreach (Entity.Studio studio in this.studios) {
				this.combo_Studio.Items.Add(studio.Name);
			}

			this.combo_Seasonal.Items.Add(Lang.UNKNOWN);
			this.combo_Seasonal.Items.Add(Lang.WINTER);
			this.combo_Seasonal.Items.Add(Lang.SPRING);
			this.combo_Seasonal.Items.Add(Lang.SUMMER);
			this.combo_Seasonal.Items.Add(Lang.FALL);

			// Some fields aren't available for seasons
			if (level == Level.Season) {
				this.combo_SeasonsOperator.IsEnabled = false;
				this.text_Seasons.IsEnabled = false;
			}
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Create a label from the selected parameters that can be used as the collection's breadcrumb text.
		/// </summary>
		/// <returns></returns>
		public string CreateLabel()
		{
			string label = "";

			string studio = (string)this.combo_Studio.SelectedValue;
			string season = (string)this.combo_Seasonal.SelectedValue;
			string year = (string)this.text_Year.Text;

			if (!String.IsNullOrEmpty(studio)) {
				label += studio;
			}

			if (!String.IsNullOrEmpty(season)) {
				label += (label.Length > 0 ? ", " : "") + season;
			}

			if (!String.IsNullOrEmpty(year)) {
				label += (label.Length > 0 ? " " : "") + year;
			}

			return label;
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Build a serie query from given data in the window fields.
		/// </summary>
		/// <returns></returns>
		private string CreateSerieQuery()
		{
			QueryBuilder qb = new QueryBuilder(Entity.Serie.TABLE)
				.Select("DISTINCT " + Repository.Serie.Instance.GetSelectQuery(), "s1");

			if (!String.IsNullOrEmpty((string)this.combo_Studio.SelectedValue)) {
				this.NeedSeasonJoin(ref qb);
				qb.AndWhere("s2.studio_id = :studioId");
				qb.SetParam("studioId", this.studios[this.combo_Studio.SelectedIndex].Id);
			}

			if (this.genres != null && this.genres.Count > 0) {
				foreach (Entity.Genre genre in this.genres) {
					string id = genre.Id.ToString();

					qb.InnerJoin("serie_genre", "sg" + id);
					qb.On("sg" + id + ".serie_id = s1.id");

					qb.AndWhere("sg" + id + ".genre_id = :genreId" + id);
					qb.SetParam("genreId" + id, genre.Id);
				}
			}

			// Seasonal
			if (this.combo_Seasonal.SelectedValue != null
			&& !String.IsNullOrEmpty((string)this.combo_Seasonal.SelectedValue.ToString())) {
				this.NeedSeasonJoin(ref qb);
				qb.AndWhere("s2.seasonal = :seasonalIndex");
				qb.SetParam("seasonalIndex", this.combo_Seasonal.SelectedIndex);
			}

			// Year
			if (!String.IsNullOrEmpty((string)this.text_Year.Text)) {
				this.NeedSeasonJoin(ref qb);
				qb.AndWhere("s2.year = :year");
				qb.SetParam("year", (string)this.text_Year.Text);
			}

			// Type
			if (this.ComboBox_Type.SelectedValue != null
			&& !String.IsNullOrEmpty((string)this.ComboBox_Type.SelectedValue.ToString())) {
				this.NeedSeasonJoin(ref qb);
				qb.AndWhere("s2.type = :typeIndex");
				qb.SetParam("typeIndex", this.ComboBox_Type.SelectedIndex);
			}

			// Source
			if (this.ComboBox_Source.SelectedValue != null
			&& !String.IsNullOrEmpty((string)this.ComboBox_Source.SelectedValue.ToString())) {
				this.NeedSeasonJoin(ref qb);
				qb.AndWhere("s2.source = :sourceIndex");
				qb.SetParam("sourceIndex", this.ComboBox_Source.SelectedIndex);
			}

			// WHEREs must be after JOIN statments
			qb.Where("1");

			if (!String.IsNullOrWhiteSpace(this.textbox_Title.ActualText)) {
				qb.AndWhere("s1.title LIKE '%" + this.textbox_Title.ActualText + "%'");
			}

			if (!String.IsNullOrWhiteSpace(this.text_Seasons.Text)) {
				qb.AndWhere("seasons_count " + this.combo_SeasonsOperator.Text + " :seasons");
				qb.SetParam("seasons", this.text_Seasons.Number);
			}

			if (!String.IsNullOrWhiteSpace(this.text_Episodes.Text)) {
				qb.AndWhere("episodes_total " + this.combo_EpisodesOperator.Text + " :episodes");
				qb.SetParam("episodes", this.text_Episodes.Number);
			}

			if (this.StatusId != (int)Entity.DefaultStatus.All) {
				qb.AndWhere("s1.status_id = :status");
				qb.SetParam("status", this.StatusId);
			}

			qb.OrderBy(Settings.Default.TileOrderBy, Settings.Default.TileOrderByDirection, true);

			return qb.Query;
		}

		/// <summary>
		/// Build a season query from given data in the window fields.
		/// </summary>
		/// <returns></returns>
		private string CreateSeasonQuery()
		{
			QueryBuilder qb = new QueryBuilder(Entity.Season.TABLE)
				.Select(Repository.Season.Instance.GetSelectQueryForLabeledList(), "sa")
				.InnerJoin("serie", "se")
				.On("sa.serie_id = se.id");

			if (this.genres != null && this.genres.Count > 0) {
				foreach (Entity.Genre genre in this.genres) {
					string id = genre.Id.ToString();

					qb.InnerJoin("serie_genre", "sg" + id);
					qb.On("sg" + id + ".serie_id = se.id");

					qb.AndWhere("sg" + id + ".genre_id = :genreId" + id);
					qb.SetParam("genreId" + id, genre.Id);
				}
			}

			if (!String.IsNullOrEmpty((string)this.combo_Studio.SelectedValue)) {
				qb.AndWhere("sa.studio_id = :studioId");
				qb.SetParam("studioId", this.studios[this.combo_Studio.SelectedIndex].Id);
			}

			// Seasonal
			if (this.combo_Seasonal.SelectedValue != null
			&& !String.IsNullOrEmpty((string)this.combo_Seasonal.SelectedValue.ToString())) {
				qb.AndWhere("sa.seasonal = :seasonalIndex");
				qb.SetParam("seasonalIndex", this.combo_Seasonal.SelectedIndex);
			}

			// Year
			if (!String.IsNullOrEmpty((string)this.text_Year.Text)) {
				qb.AndWhere("sa.year = :year");
				qb.SetParam("year", (string)this.text_Year.Text);
			}

			// Type
			if (this.ComboBox_Type.SelectedValue != null
			&& !String.IsNullOrEmpty((string)this.ComboBox_Type.SelectedValue.ToString())) {
				qb.AndWhere("sa.type = :typeIndex");
				qb.SetParam("typeIndex", this.ComboBox_Type.SelectedIndex);
			}

			// Source
			if (this.ComboBox_Source.SelectedValue != null
			&& !String.IsNullOrEmpty((string)this.ComboBox_Source.SelectedValue.ToString())) {
				qb.AndWhere("sa.source = :sourceIndex");
				qb.SetParam("sourceIndex", this.ComboBox_Source.SelectedIndex);
			}

			// WHEREs must be after JOIN statments
			qb.Where("1");

			if (!String.IsNullOrWhiteSpace(this.textbox_Title.ActualText)) {
				qb.AndWhere("sa.title LIKE '%" + this.textbox_Title.ActualText + "%'");
			}

			if (!String.IsNullOrWhiteSpace(this.text_Seasons.Text)) {
				qb.AndWhere("seasons_count " + this.combo_SeasonsOperator.Text + " :seasons");
				qb.SetParam("seasons", this.text_Seasons.Number);
			}

			if (!String.IsNullOrWhiteSpace(this.text_Episodes.Text)) {
				qb.AndWhere("episodes_total " + this.combo_EpisodesOperator.Text + " :episodes");
				qb.SetParam("episodes", this.text_Episodes.Number);
			}

			if (this.StatusId != (int)Entity.DefaultStatus.All) {
				qb.AndWhere("sa.status_id = :status");
				qb.SetParam("status", this.StatusId);
			}

			qb.OrderBy("sa." + Settings.Default.TileOrderBy, Settings.Default.TileOrderByDirection, true);

			return qb.Query;
		}

		/// <summary>
		/// Check if the query need to have a join to the season table.
		/// </summary>
		/// <param name="qb"></param>
		private void NeedSeasonJoin(ref QueryBuilder qb)
		{
			if (!seasonJoinExists) {
				qb.InnerJoin("season", "s2");
				qb.On("s2.serie_id = s1.id");

				this.seasonJoinExists = true;
			}
		}

		/// <summary>
		/// Set a list of genres in the field.
		/// </summary>
		/// <param name="genres"></param>
		private void SetGenres(List<Entity.Genre> genres)
		{
			this.text_Genres.Clear();

			for (byte i = 0; i < genres.Count; i++) {
				this.text_Genres.Text = this.text_Genres.Text + genres[i].Name;

				if (i < genres.Count - 1) {
					this.text_Genres.Text += ", ";
				}
			}

			this.genres = genres;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		/// <summary>
		/// Get or set the selected status from the Status combobox.
		/// </summary>
		public int StatusId
		{
			get
			{
				ComboBoxItem selected = (ComboBoxItem)this.combo_Status.SelectedItem;

				if (selected != null) {
					return int.Parse(selected.Tag.ToString());
				}

				return (int)Entity.DefaultStatus.All;
			}
		}

		public string Query
		{
			get
			{
				if (this.level == Level.Serie) {
					return this.CreateSerieQuery();
				} else if (this.level == Level.Season) {
					return this.CreateSeasonQuery();
				}

				return null;
			}
		}

		public bool NeedReload
		{
			get { return this.needReload; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called by clicking on the search button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Search(object sender, RoutedEventArgs e)
		{
			this.needReload = true;

			this.Close();
		}

		/// <summary>
		/// Called by clicking on the genres textbox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_Genres_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (this.genres == null) {
				this.genres = new List<Entity.Genre>();
			}

			GenresSelector gs = new GenresSelector();
			gs.Owner = this;
			gs.Genres = this.genres;
			gs.ShowDialog();

			if (gs.Saved) {
				this.SetGenres(gs.SelectedGenres);
			}
		}

		#endregion Event
	}
}
