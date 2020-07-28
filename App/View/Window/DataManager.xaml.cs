using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for DataManager.xaml
	/// </summary>
	public partial class DataManager : System.Windows.Window
	{
		private List<Entity.UserStatus> statusList;
		private List<Entity.Genre> genres;
		private List<Entity.Studio> studios;

		private bool needStatusReload = false;

		public DataManager()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.LoadStatus();
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Add a status to list and db.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="type"></param>
		private void AddStatus(string text, byte type)
		{
			text = text.Trim();

			if (!this.NameIsValid(text, this.listBox_Status.Items)) {
				this.ShowInvalidMessage();

				return;
			}

			Entity.UserStatus status = new Entity.UserStatus() {
				Text = text,
				Type = type
			};

			// In order, add to object list, display list and DB
			this.statusList.Add(status);
			this.listBox_Status.Items.Add(status.Text);

			Repository.Status.Instance.Add(status);

			// Collection will reload its status list after the window's closure.
			this.needStatusReload = true;
		}

		/// <summary>
		/// Add a genre to list and db.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="type"></param>
		private void AddGenre(string text)
		{
			text = text.Trim();

			if (!this.NameIsValid(text, this.list_Genres.Items)) {
				this.ShowInvalidMessage();

				return;
			}

			Entity.Genre genre = new Entity.Genre() { Name = text };

			// In order, add to object list, display list and DB
			this.genres.Add(genre);
			this.list_Genres.Items.Add(genre.Name);

			Repository.Genre.Instance.Add(genre);
		}

		/// <summary>
		/// Add a studio to list and db.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="type"></param>
		private void AddStudio(string text)
		{
			text = text.Trim();

			if (!this.NameIsValid(text, this.list_Studios.Items)) {
				this.ShowInvalidMessage();

				return;
			}

			Entity.Studio studio = new Entity.Studio() { Name = text };

			// In order, add to object list, display list and DB
			this.studios.Add(studio);
			this.list_Studios.Items.Add(studio.Name);

			Repository.Studio.Instance.Add(studio);
		}

		/// <summary>
		/// Modify an existing status.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="type"></param>
		private void EditStatus(string text, byte type)
		{
			text = text.Trim();

			if (!this.NameIsValid(text, this.listBox_Status.Items)) {
				this.ShowInvalidMessage();

				return;
			}

			if (this.listBox_Status.SelectedIndex >= 0) {
				Entity.UserStatus status = this.statusList[this.listBox_Status.SelectedIndex];
				status.Text = text;
				status.Type = type;

				Repository.Status.Instance.Update(status);

				// Collection will reload its status list after the window's closure.
				this.needStatusReload = true;
			}

			this.listBox_Status.Items.Clear();
			this.LoadStatus();
		}

		/// <summary>
		/// Modify an existing genre.
		/// </summary>
		/// <param name="text"></param>
		private void EditGenre(string text)
		{
			text = text.Trim();

			if (!this.NameIsValid(text, this.list_Genres.Items)) {
				this.ShowInvalidMessage();

				return;
			}

			if (this.list_Genres.SelectedIndex >= 0) {
				Entity.Genre genre = this.genres[this.list_Genres.SelectedIndex];
				genre.Name = text;

				Repository.Genre.Instance.Update(genre);
			}

			this.list_Genres.Items.Clear();
			this.LoadGenres();
		}

		/// <summary>
		/// Modify an existing studio.
		/// </summary>
		/// <param name="text"></param>
		private void EditStudio(string text)
		{
			text = text.Trim();

			if (!this.NameIsValid(text, this.list_Studios.Items)) {
				this.ShowInvalidMessage();

				return;
			}

			if (this.list_Studios.SelectedIndex >= 0) {
				Entity.Studio studio = this.studios[this.list_Studios.SelectedIndex];
				studio.Name = text;

				Repository.Studio.Instance.Update(studio);
			}

			this.list_Studios.Items.Clear();
			this.LoadStudios();
		}

		/// <summary>
		/// Remove a status from list and db.
		/// </summary>
		private void RemoveStatus()
		{
			if (this.listBox_Status.SelectedIndex < 0) {
				return;
			}

			int statusId = this.statusList[this.listBox_Status.SelectedIndex].Id;

			this.statusList.RemoveAt(this.listBox_Status.SelectedIndex);
			this.listBox_Status.Items.RemoveAt(this.listBox_Status.SelectedIndex);

			Repository.Status.Instance.Delete(statusId);

			// Collection will reload its status list after the window's closure.
			this.needStatusReload = true;
		}

		/// <summary>
		/// Remove a genre from list and db.
		/// </summary>
		private void RemoveGenre()
		{
			if (this.list_Genres.SelectedIndex < 0) {
				return;
			}

			int genreId = this.genres[this.list_Genres.SelectedIndex].Id;

			this.genres.RemoveAt(this.list_Genres.SelectedIndex);
			this.list_Genres.Items.RemoveAt(this.list_Genres.SelectedIndex);

			Repository.Genre.Instance.Delete(genreId);
		}

		/// <summary>
		/// Remove a studio from list and db.
		/// </summary>
		private void RemoveStudio()
		{
			if (this.list_Studios.SelectedIndex < 0) {
				return;
			}

			int studioId = this.studios[this.list_Studios.SelectedIndex].Id;

			this.studios.RemoveAt(this.list_Studios.SelectedIndex);
			this.list_Studios.Items.RemoveAt(this.list_Studios.SelectedIndex);

			Repository.Studio.Instance.Delete(studioId);
		}

		/// <summary>
		/// Load genres from DB and fill the list.
		/// </summary>
		private void LoadGenres()
		{
			this.genres = Repository.Genre.Instance.GetAll();

			foreach (Entity.Genre genre in this.genres) {
				this.list_Genres.Items.Add(genre.Name);
			}
		}

		/// <summary>
		/// Load studios from DB and fill the list.
		/// </summary>
		private void LoadStudios()
		{
			this.studios = Repository.Studio.Instance.GetAll();

			foreach (Entity.Studio studio in this.studios) {
				this.list_Studios.Items.Add(studio.Name);
			}
		}

		private void ShowInvalidMessage()
		{
			MessageBox.Show(Lang.Text("invalidName", "This name is invalid or already exists, please select another one."));
		}

		/// <summary>
		/// Check if a name is valid: not null nor whitespace and not already in given list.
		/// </summary>
		/// this.list_Status.Items
		/// <returns></returns>
		private bool NameIsValid(string name, ItemCollection items)
		{
			if (String.IsNullOrWhiteSpace(name)) {
				return false;
			}

			int i = 0;

			while (i < items.Count) {
				if (items[i].ToString() == name) {
					return false;
				}

				i++;
			}

			return true;
		}

		/// <summary>
		/// Load status from DB and fill the list.
		/// </summary>
		private void LoadStatus()
		{
			this.statusList = Repository.Status.Instance.GetAll();

			foreach (Entity.UserStatus status in this.statusList) {
				this.listBox_Status.Items.Add(status.Text);
			}
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public bool NeedStatusReload
		{
			get { return this.needStatusReload; }
			set { this.needStatusReload = value; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		// Add

		private void Button_AddStatus_Click(object sender, RoutedEventArgs e)
		{
			this.AddStatus(this.Textbox_Status.ActualText, (byte)((bool)this.Radio_StatusType_Small.IsChecked ? 0 : 1));

			this.Textbox_Status.Text = String.Empty;
		}

		private void Button_AddGenre_Click(object sender, RoutedEventArgs e)
		{
			this.AddGenre(this.text_Genre.ActualText);

			this.text_Genre.Text = String.Empty;
		}

		private void Button_AddStudio_Click(object sender, RoutedEventArgs e)
		{
			this.AddStudio(this.text_Studio.ActualText);

			this.text_Studio.Text = String.Empty;
		}

		// Remove

		private void Button_RemoveStatus_Click(object sender, RoutedEventArgs e)
		{
			this.RemoveStatus();
		}

		private void Button_RemoveGenre_Click(object sender, RoutedEventArgs e)
		{
			this.RemoveGenre();
		}

		private void Button_RemoveStudio_Click(object sender, RoutedEventArgs e)
		{
			this.RemoveStudio();
		}

		// Edit

		private void Button_EditStatus_Click(object sender, RoutedEventArgs e)
		{
			this.EditStatus(this.Textbox_Status.ActualText, (byte)((bool)this.Radio_StatusType_Small.IsChecked ? 0 : 1));
		}

		private void Button_EditGenre_Click(object sender, RoutedEventArgs e)
		{
			this.EditGenre(this.text_Genre.ActualText);
		}

		private void Button_EditStudio_Click(object sender, RoutedEventArgs e)
		{
			this.EditStudio(this.text_Studio.ActualText);
		}

		// Selection changed

		private void ListBox_Status_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.listBox_Status.SelectedIndex < 0) {
				return;
			}

			this.Textbox_Status.Text = this.statusList[this.listBox_Status.SelectedIndex].Text;

			if (this.statusList[this.listBox_Status.SelectedIndex].Type == 0) {
				this.Radio_StatusType_Small.IsChecked = true;
			} else {
				this.Radio_StatusType_Big.IsChecked = true;
			}
		}

		private void ListBox_Genres_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.list_Genres.SelectedIndex >= 0) {
				this.text_Genre.Text = this.genres[this.list_Genres.SelectedIndex].Name;
			}
		}

		private void ListBox_Studios_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this.list_Studios.SelectedIndex >= 0) {
				this.text_Studio.Text = this.studios[this.list_Studios.SelectedIndex].Name;
			}
		}

		private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!this.Tabs.IsLoaded) {
				return;
			}

			switch (this.Tabs.SelectedIndex) {
				case 0: { // Status
					if (this.statusList == null) {
						this.LoadStatus();
					}
				} break;
				case 1: { // Genre
					if (this.genres == null) {
						this.LoadGenres();
					}
				}
				break;
				case 2: { // Studio
					if (this.studios == null) {
						this.LoadStudios();
					}
				}
				break;
			}
		}

		#endregion Event
	}
}
