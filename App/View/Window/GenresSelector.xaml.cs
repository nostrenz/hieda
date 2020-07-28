using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for GenresManager.xaml
	/// </summary>
	public partial class GenresSelector : System.Windows.Window
	{
		private List<Entity.Genre> genres;
		private bool saved = false;

		public GenresSelector()
		{
			InitializeComponent();

			this.genres = Repository.Genre.Instance.GetAll();

			foreach (Entity.Genre genre in this.genres) {
				CheckBox checkbox = new CheckBox();
				checkbox.Content = genre.Name;

				this.list_Genres.Items.Add(checkbox);
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public List<Entity.Genre> Genres
		{
			set
			{
				foreach (CheckBox checkbox in this.list_Genres.Items) {
					checkbox.IsChecked = (value.Find(g => g.Name == (string)checkbox.Content) != null);
				}
			}
		}

		public List<Entity.Genre> SelectedGenres
		{
			get
			{
				List<Entity.Genre> selectedGenres = new List<Entity.Genre>();

				for (byte i = 0; i < this.list_Genres.Items.Count; i++) {
					if ((bool)((CheckBox)this.list_Genres.Items[i]).IsChecked) {
						selectedGenres.Add(this.genres[i]);
					}
				}

				return selectedGenres;
			}
		}

		public bool Saved
		{
			get { return this.saved; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		private void button_Save_Click(object sender, RoutedEventArgs e)
		{
			this.saved = true;

			this.Close();
		}

		#endregion Event
	}
}
