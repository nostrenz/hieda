using System.Collections.Generic;
using System.Windows.Controls;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for SeasonsSelector.xaml
	/// </summary>
	public partial class SeasonsSelector : System.Windows.Window
	{
		private bool greenlight = false;

		public SeasonsSelector()
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;
		}

		/*
		============================================
		Public
		============================================
		*/

		public void AddCheckBox(int index, string text)
		{
			CheckBox checkbox = new CheckBox();
			checkbox.Content = text;
			checkbox.Tag = index;
			checkbox.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#333333"); // = StaticResource Collection_BackgroundColor

			this.ListBox_CheckBoxes.Items.Add(checkbox);
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Check or uncheck all the checkboxes.
		/// </summary>
		private void ChangeAllCheckboxes(bool value)
		{
			for (int i = 0; i < this.ListBox_CheckBoxes.Items.Count; i++) {
				CheckBox checkbox = (CheckBox)this.ListBox_CheckBoxes.Items[i];

				checkbox.IsChecked = value;
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public bool Greenlight
		{
			get { return this.greenlight; }
		}

		public List<int> SelectedIndexes
		{
			get
			{
				List<int> indexes = new List<int>();

				for (int i=0; i<this.ListBox_CheckBoxes.Items.Count; i++) {
					CheckBox checkbox = (CheckBox)this.ListBox_CheckBoxes.Items[i];

					if ((bool)checkbox.IsChecked) {
						indexes.Add((int)checkbox.Tag);
					}
				}

				return indexes;
			}
		}

		/*
		============================================
		Event
		============================================
		*/

		/// <summary>
		/// Called by clicking on the "Check all" button, checks all the checkboxes in the list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_CheckAll_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			this.ChangeAllCheckboxes(true);
		}

		/// <summary>
		/// Called by clicking on the "Uncheck all" button, unchecks all the checkboxes in the list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_UncheckAll_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			this.ChangeAllCheckboxes(false);
		}

		/// <summary>
		/// Called by clicking on the "Confirm" button, closes the window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Confirm_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			this.greenlight = true;

			this.Close();
		}
	}
}
