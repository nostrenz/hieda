using System.Windows;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for InputDialog.xaml
	/// </summary>
	public partial class InputDialog : System.Windows.Window
	{
		private bool greenlight = false;

		public InputDialog(System.Windows.Window owner=null)
		{
			InitializeComponent();

			this.Owner = owner;
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		public string Open()
		{
			this.ShowDialog();

			return this.Text;
		}

		#endregion Public

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string Caption
		{
			set { this.label.Content = value; }
		}

		public string Text
		{
			set { this.textbox.Text = value; }
			get { return this.textbox.ActualText; }
		}

		public string Placeholder
		{
			set { this.textbox.Placeholder = value; }
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
		/// Called by clicking on the Ok button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Ok_Click(object sender, RoutedEventArgs e)
		{
			this.greenlight = true;

			this.Close();
		}

		#endregion Event
	}
}
