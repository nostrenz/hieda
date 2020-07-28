using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for SeasonsMover.xaml
	/// </summary>
	public partial class ItemSelector : System.Windows.Window
	{
		private List<ListBoxItem> items;
		private bool greenlight = false;

		public ItemSelector(List<Entity.Serie> series)
		{
			this.Initialize();
			this.FillListBox(series);
		}

		public ItemSelector(List<Entity.Season> seasons)
		{
			this.Initialize();
			this.FillListBox(seasons);
		}

		public ItemSelector(List<Entity.UserStatus> statusList)
		{
			this.Initialize();
			this.FillListBox(statusList);
		}

		/*
		============================================
		Public
		============================================
		*/

		public ItemSelector Open()
		{
			this.ShowDialog();

			return this;
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Initialize the window.
		/// </summary>
		private void Initialize()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
		}

		/// <summary>
		/// Fill the list with series.
		/// </summary>
		/// <param name="series"></param>
		private void FillListBox(List<Entity.Serie> series)
		{
			foreach (Entity.Serie serie in series) {
				this.ListBox_Items.Items.Add(new ListBoxItem() { Content = serie.Title, Tag = serie.Id });
			}

			this.items = this.ListBox_Items.Items.OfType<ListBoxItem>().ToList();
		}

		/// <summary>
		/// Fill the list with seasons.
		/// </summary>
		/// <param name="seasons"></param>
		private void FillListBox(List<Entity.Season> seasons)
		{
			foreach (Entity.Season season in seasons) {
				this.ListBox_Items.Items.Add(new ListBoxItem() { Content = season.Title, Tag = season.Id });
			}

			this.items = this.ListBox_Items.Items.OfType<ListBoxItem>().ToList();
		}

		/// <summary>
		/// Fill the list with episodes.
		/// </summary>
		/// <param name="statusList"></param>
		private void FillListBox(List<Entity.UserStatus> statusList)
		{
			foreach (Entity.UserStatus status in statusList) {
				this.ListBox_Items.Items.Add(new ListBoxItem() { Content = status.Text, Tag = status.Id });
			}

			this.items = this.ListBox_Items.Items.OfType<ListBoxItem>().ToList();
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor
		
		/// <summary>
		/// Obtain index of the selected item in the list.
		/// </summary>
		public int SelectedIndex
		{
			get { return this.ListBox_Items.SelectedIndex; }
		}

		/// <summary>
		/// Obtain ID of the selected item in the list.
		/// </summary>
		public int SelectedId
		{
			get { return int.Parse(((ListBoxItem)this.ListBox_Items.SelectedItem).Tag.ToString()); }
		}

		/// <summary>
		/// Obtain Title of the selected item in the list.
		/// </summary>
		public string SelectedTitle
		{
			get { return this.ListBox_Items.SelectedItem.ToString().Replace("System.Windows.Controls.ListBoxItem: ", ""); }
		}

		/// <summary>
		/// To know if the Confirm button was clicked before the window's closure.
		/// </summary>
		public bool Greenlight
		{
			get { return this.greenlight; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called when the content of the Filter textbox changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_Filter_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (this.ListBox_Items == null || !this.ListBox_Items.IsLoaded) {
				return;
			}
			
			List<ListBoxItem> filteredItems = this.items.Where(x => x.Content.ToString().IndexOf(this.TextBox_Filter.ActualText, System.StringComparison.OrdinalIgnoreCase) >= 0).ToList();

			this.ListBox_Items.Items.Clear();

			foreach (ListBoxItem item in filteredItems) {
				this.ListBox_Items.Items.Add(new ListBoxItem() { Content = item.Content, Tag = item.Tag });
			}
		}

		/// <summary>
		/// Called by clicking on the Confirm button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Confirm_Click(object sender, RoutedEventArgs e)
		{
			// Ensure we have a selected index
			this.greenlight = (this.ListBox_Items.SelectedIndex >= 0);

			this.Close();
		}

		#endregion Event
	}
}
