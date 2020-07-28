using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Hieda.View.Window
{
	public partial class SerieEdit : System.Windows.Window
	{
		private List<Entity.Genre> genres;
		private int serieId = 0;
		private bool greenlight = false;
		private List<Entity.UserStatus> userStatusList;

		public SerieEdit(List<Entity.UserStatus> userStatusList)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
			this.Title = Lang.Text("addSerie", "Add a serie");
			this.button_CoverBrowser.LinkedTextBox = this.TextBox_Cover;
			this.button_CoverBrowser.AddFilter(Constants.IMAGE_FILTER);
			this.userStatusList = userStatusList;

			this.SetStatusInCombo();
		}

		/// <summary>
		/// Edit Serie or Add Season to a serie.
		/// </summary>
		/// <param name="serie"></param>
		public SerieEdit(Entity.Serie serie, List<Entity.UserStatus> userStatusList)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
			this.TextBox_Cover.Text = serie.Cover;
			this.Title = Lang.Text("editSerie", "Edit this serie");
			this.Button_Add.Content = "Edit";
			this.button_CoverBrowser.LinkedTextBox = this.TextBox_Cover;
			this.button_CoverBrowser.AddFilter(Constants.IMAGE_FILTER);
			this.TextBox_Title.Text = serie.Title;
			this.TextBox_Seasons.IsEnabled = false;
			this.userStatusList = userStatusList;
			this.Button_Minus.IsEnabled = false;
			this.Button_Plus.IsEnabled = false;
			this.Button_Minus.Visibility = System.Windows.Visibility.Hidden;
			this.Button_Plus.Visibility = System.Windows.Visibility.Hidden;

			this.SetGenres(serie.Id);
			this.SetStatusInCombo();

			this.StatusId = serie.StatusId;
			this.serieId = serie.Id;
		}

		public SerieEdit(List<Entity.Serie> series, List<Entity.UserStatus> userStatusList)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
			this.Title = Lang.Text(String.Format("editingSeries", series.Count));
			this.Button_Add.Content = "Edit";
			this.button_CoverBrowser.LinkedTextBox = this.TextBox_Cover;
			this.button_CoverBrowser.AddFilter(Constants.IMAGE_FILTER);
			this.TextBox_Seasons.IsEnabled = false;
			this.userStatusList = userStatusList;

			this.Button_Minus.IsEnabled = false;
			this.Button_Plus.IsEnabled = false;
			this.Button_Minus.Visibility = System.Windows.Visibility.Hidden;
			this.Button_Plus.Visibility = System.Windows.Visibility.Hidden;
			
			this.SetStatusInCombo();

			if (series.Count < 1) {
				return;
			}

			string title = series[0].Title;
			string cover = series[0].Cover;
			int statusId = series[0].StatusId;

			foreach (Entity.Serie serie in series) {
				// There's at least two different titles
				if (title != Constants.VARIOUS && title != serie.Title) {
					title = Constants.VARIOUS;
				}

				if (cover != serie.Cover) {
					this.TextBox_Cover.Text = Constants.VARIOUS;
				}

				if (statusId != serie.StatusId) {
					statusId = (int)Entity.DefaultStatus.Null;

					this.AddVariousComboBoxItem(this.ComboBox_Status, statusId);
				}
			}

			this.TextBox_Title.Text = title;
			this.StatusId = statusId;
			this.Cover = cover;
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private void SetGenres(List<Entity.Genre> genres)
		{
			this.TextBox_Genres.Clear();

			for (byte i = 0; i < genres.Count; i++) {
				this.TextBox_Genres.Text = this.TextBox_Genres.Text + genres[i].Name;

				if (i < genres.Count - 1) {
					this.TextBox_Genres.Text += ", ";
				}
			}

			this.genres = genres;
		}

		private void SetGenres(int serieId)
		{
			this.SetGenres(Repository.Genre.Instance.GetAllBySerie(serieId));
		}

		/// <summary>
		/// Add user stats in the Status combobox.
		/// </summary>
		/// <param name="userStatusList"></param>
		private void SetStatusInCombo()
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
			foreach (Entity.UserStatus userStatus in this.userStatusList) {
				this.AddCommboBoxItem(this.ComboBox_Status, userStatus.Text, userStatus.Id);
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

		#endregion

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public bool Greenlight
		{
			get { return this.greenlight; }
		}

		public new string SerieTitle
		{
			get { return this.TextBox_Title.Text; }
		}

		public string Cover
		{
			get { return this.TextBox_Cover.Text; }
			set
			{
				if (!String.IsNullOrEmpty(value) && value.Length > 0) {
					this.TextBox_Cover.Text = value;
				}
			}
		}

		public int SeasonsCount
		{
			get { return this.TextBox_Seasons.Number; }
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

		public List<Entity.Genre> Genres
		{
			get { return this.genres; }
		}

		public bool DeleteCover
		{
			get { return (bool)this.CheckBox_CoverDelete.IsChecked; }
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
			if (!String.IsNullOrWhiteSpace(this.TextBox_Title.Text)) {
				this.greenlight = true;

				this.Close();
			} else {
				MessageBox.Show(Lang.MISSING_TITLE, Lang.ACTION_CANCELED);
			}
		}

		/// <summary>
		/// Called when the content of the Seasons textbox changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_Seasons_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!this.TextBox_Seasons.IsFocused) {
				return;
			}

			if (this.TextBox_Seasons.IsLoaded) {
				// Replace any incorrect values with 1
				if (this.TextBox_Seasons.Text.Length <= 2 && (!Tools.IsNumeric(this.TextBox_Seasons.Text) || Byte.Parse(this.TextBox_Seasons.Text) <= 1)) {
					this.TextBox_Seasons.Text = "1";
				}
			}

			int result;

			if (this.TextBox_Seasons.Text.Length <= 2 && ((this.TextBox_Seasons.Text != null && this.TextBox_Seasons.Text != "" && int.TryParse(this.TextBox_Seasons.Text, out result) && Byte.Parse(this.TextBox_Seasons.Text) < 30))) {

			}
		}

		/// <summary>
		/// Called by clicking on the '-' button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Minus_Click(object sender, RoutedEventArgs e)
		{
			if (Int32.Parse(this.TextBox_Seasons.Text) > 1) {
				this.TextBox_Seasons.Text = ((Int32.Parse(this.TextBox_Seasons.Text)) - 1).ToString();
			}
		}

		/// <summary>
		/// Called by clicking on the '+' button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Plus_Click(object sender, RoutedEventArgs e)
		{
			if (Int32.Parse(this.TextBox_Seasons.Text) < 30) {
				this.TextBox_Seasons.Text = ((Int32.Parse(this.TextBox_Seasons.Text)) + 1).ToString();
			}
		}

		/// <summary>
		/// Called when the Seasons textbox gets the focus.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_Seasons_GotFocus(object sender, RoutedEventArgs e)
		{
			this.TextBox_Seasons.SelectAll();
		}

		/// <summary>
		/// Called by clicking on the Genres textbox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_Genres_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			GenresSelector gs = new GenresSelector();
			gs.Owner = this;
			gs.Genres = this.genres;
			gs.ShowDialog();

			if (gs.Saved) {
				this.SetGenres(gs.SelectedGenres);
			}
		}

		#endregion
	}
}
