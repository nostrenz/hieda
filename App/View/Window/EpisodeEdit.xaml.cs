using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Hieda.View.Window
{
	public partial class EpisodeEdit : System.Windows.Window
	{
		private bool greenlight = false;

		public EpisodeEdit(ushort lastNumber, bool isManga)
		{
			InitializeComponent();

			string type = (isManga ? Lang.CHAPTER : Lang.EPISODE);

			this.Owner = App.Current.MainWindow;
			this.Title = Lang.Text("addEpisode", "Add an " + type);

			this.EpisodeTitle = Tools.UpperFirst(type) + " " + lastNumber;

			this.Browse_Cover.AddFilter(Constants.IMAGE_FILTER);
			this.TextBox_Number.Text = lastNumber.ToString();
		}

		public EpisodeEdit(Entity.Episode episode, bool isManga)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
			this.Title = Lang.Text("editEpisode", "Edit this " + (isManga ? Lang.CHAPTER : Lang.EPISODE));

			this.TextBox_Title.Focus();
			this.TextBox_Title.Text = episode.Title;
			this.TextBox_Number.Text = episode.Number.ToString();
			this.Browse_Cover.Text = episode.Cover;
			this.CheckBox_Watched.IsChecked = episode.Watched;
			this.Browse_Cover.AddFilter(Constants.IMAGE_FILTER);
			this.TextBox_Title.Select(this.TextBox_Title.Text.Length, 0);
			this.Browse_FileOrUrl.Text = episode.Uri;
		}

		public EpisodeEdit(List<Entity.Episode> episodes)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
			this.Title = Lang.Text(String.Format("editingEpisodes", episodes.Count));
			this.TextBox_Number.AllowEmpty = true;

			if (episodes.Count < 1) {
				return;
			}

			string title = episodes[0].Title;
			ushort number = episodes[0].Number;
			string cover = episodes[0].Cover;

			foreach (Entity.Episode episode in episodes) {
				// There's at least two different titles
				if (title != Constants.VARIOUS && title != episode.Title) {
					title = Constants.VARIOUS;
				}

				if (number != episode.Number) {
					this.TextBox_Number.Text = Constants.VARIOUS;
				}

				if (cover != episode.Cover) {
					this.Browse_Cover.Text = Constants.VARIOUS;
				}
			}

			this.TextBox_Title.Text = title;
			this.Browse_Cover.Text = cover;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string EpisodeTitle
		{
			get { return this.TextBox_Title.Text; }
			set { this.TextBox_Title.Text = value; }
		}

		public string FileOrUrl
		{
			get
			{
				if (!String.IsNullOrWhiteSpace(this.Browse_FileOrUrl.ActualText)) {
					return this.Browse_FileOrUrl.ActualText.Trim();
				}

				return "";
			}
		}

		public string Cover
		{
			get
			{
				if (!String.IsNullOrWhiteSpace(this.Browse_Cover.ActualText)) {
					return this.Browse_Cover.ActualText.Trim();
				}

				return "";
			}
		}

		public bool Watched
		{
			get { return (bool)this.CheckBox_Watched.IsChecked; }
		}

		public bool DeleteImage
		{
			get { return (bool)this.CheckBox_CoverDelete.IsChecked; }
		}

		public ushort Number
		{
			get
			{
				ushort result = 0;
				ushort.TryParse(this.TextBox_Number.Text, out result);

				return result;
			}
		}

		public bool Greenlight
		{
			get { return this.greenlight; }
		}

		public bool NumberIsVarious
		{
			get { return this.TextBox_Number.Text == Constants.VARIOUS; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		private void Button_Edit_Click(object sender, RoutedEventArgs e)
		{
			this.greenlight = true;

			this.Close();
		}

		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void TextBox_Name_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!TextBox_Title.IsLoaded) {
				return;
			}

			this.Button_Edit.IsEnabled = !String.IsNullOrWhiteSpace(this.TextBox_Title.ActualText);
		}

		#endregion Event
	}
}
