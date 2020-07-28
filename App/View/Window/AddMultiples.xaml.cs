using System.Windows;
using System.Windows.Controls;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for AddEpidodes.xaml
	/// </summary>
	public partial class AddMultiples : System.Windows.Window
	{
		const int MIN = 1;
		const int MAX = 250;

		private ushort counter = 2;
		private ushort from = 1;
		private bool greenlight = false;

		public AddMultiples(ushort lastNumber)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.from = lastNumber;
			this.TextBox_From.Text = lastNumber.ToString();
		}

		/*
		============================================
		Public
		============================================
		*/

		/// <summary>
		/// Configure the window for adding multiple seasons.
		/// </summary>
		public void ConfigureForSeasons()
		{
			// Add types
			foreach (int i in System.Enum.GetValues(typeof(Constants.Type))) {
				this.ComboBox_Type.Items.Add(new ComboBoxItem() {
					Content = Lang.Content(System.Enum.GetName(typeof(Constants.Type), i).ToLower())
				});
			}

			this.ComboBox_Type.SelectedIndex = (byte)Constants.Type.None;
			this.ComboBox_Type.IsEnabled = true;
			this.TextBox_Title.Text = Tools.UpperFirst(Lang.SEASON) + " %number%";
		}

		/// <summary>
		/// Configure the window for adding multiple episodes.
		/// </summary>
		public void ConfigureForEpisodes(bool currentSeasonIsManga)
		{
			this.ComboBox_Type.IsEnabled = false;
			this.TextBox_Title.Text = Tools.UpperFirst(currentSeasonIsManga ? Lang.CHAPTER : Lang.EPISODE) + " %number%";
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Handle field update on text changed events.
		/// </summary>
		/// <param name="textbox"></param>
		/// <param name="value"></param>
		private void HandleTextChanged(TextBox textbox, ref ushort value)
		{
			if (!textbox.IsLoaded) {
				return;
			}

			//int parsed = int.Parse(this.TextBox_Counter.Text);
			ushort parsed = value;
			bool success = ushort.TryParse(textbox.Text, out parsed);

			if (success && parsed >= MIN && parsed <= MAX) {
				value = parsed;
			}

			textbox.Text = value.ToString();
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string DefaultTitle
		{
			get { return this.TextBox_Title.Text; }
		}

		public Constants.Type Type
		{
			get { return (Constants.Type)this.ComboBox_Type.SelectedIndex; }
		}

		public ushort Counter
		{
			get { return this.counter; }
		}

		public ushort From
		{
			get { return this.from; }
		}

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
		/// Called when the content of the counter textbox is changed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_Counter_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.HandleTextChanged(this.TextBox_Counter, ref this.counter);
		}

		/// <summary>
		/// Called when the content of the from textbox is changed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_From_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.HandleTextChanged(this.TextBox_From, ref this.from);
		}

		/// <summary>
		/// Called by clicking on the '-' button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Minus_Click(object sender, RoutedEventArgs e)
		{
			// Prevent the value to be lower than 1
			if (this.counter <= MIN) {
				return;
			}

			this.counter--;
			this.TextBox_Counter.Text = this.counter.ToString();
		}

		/// <summary>
		/// Called by clicking on the '+' button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Plus_Click(object sender, RoutedEventArgs e)
		{
			// Prevent the value to be greater than 100
			if (this.counter >= MAX) {
				return;
			}

			this.counter++;
			this.TextBox_Counter.Text = this.counter.ToString();
		}

		/// <summary>
		/// Called by clicking on the "Add" button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Add_Click(object sender, RoutedEventArgs e)
		{
			this.greenlight = true;

			this.Close();
		}

		#endregion Event
	}
}
