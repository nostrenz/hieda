using System;
using System.Windows.Controls;

namespace Hieda.View.Element
{
	/// <summary>
	/// Interaction logic for DateInput.xaml
	/// </summary>
	public partial class DateInput : UserControl
    {
        public DateInput()
        {
            InitializeComponent();

			this.Seasons.Items.Add(new ComboBoxItem() { Content = Lang.UNKNOWN, Tag = Constants.Seasonal.Unknown });
			this.Seasons.Items.Add(new ComboBoxItem() { Content = Lang.WINTER, Tag = Constants.Seasonal.Winter });
			this.Seasons.Items.Add(new ComboBoxItem() { Content = Lang.SPRING, Tag = Constants.Seasonal.Spring });
			this.Seasons.Items.Add(new ComboBoxItem() { Content = Lang.SUMMER, Tag = Constants.Seasonal.Summer });
			this.Seasons.Items.Add(new ComboBoxItem() { Content = Lang.FALL, Tag = Constants.Seasonal.Fall });
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Change the selected Seasonal item depending on the given month number.
		/// </summary>
		private void MonthChanged()
		{
			if (this.Month.Number >= 12 || this.Month.Number <= 2) { // December to February
				this.ComboBox_Seasons.SelectedIndex = 1; // Winter
			} else if (this.Month.Number >= 3 && this.Month.Number <= 5) { // March to May
				this.ComboBox_Seasons.SelectedIndex = 2; // Spring
			} else if (this.Month.Number >= 6 && this.Month.Number <= 8) { // June to August
				this.ComboBox_Seasons.SelectedIndex = 3; // Summer
			} else if (this.Month.Number >= 9 && this.Month.Number <= 11) { // Septembre to November
				this.ComboBox_Seasons.SelectedIndex = 4; // Fall
			}
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public NumericTextBox Year
		{
			get { return this.TextBox_Year; }
		}

		public NumericTextBox Month
		{
			get { return this.TextBox_Month; }
		}

		public NumericTextBox Day
		{
			get { return this.TextBox_Day; }
		}

		public ComboBox Seasons
		{
			get { return this.ComboBox_Seasons; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called when text in the Month TextBox changes.
		/// Changes the selected Seasonal value depending on the month.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_Month_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.MonthChanged();
		}

		private void ComboBox_Months_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.TextBox_Month.Number = this.ComboBox_Months.SelectedIndex + 1;

			this.MonthChanged();
		}

		private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			DateTime dt = (DateTime)this.DatePicker.SelectedDate;

			this.Year.Number = dt.Year;
			this.Month.Number = dt.Month;
			this.Day.Number = dt.Day;
		}

		#endregion Event
	}
}
