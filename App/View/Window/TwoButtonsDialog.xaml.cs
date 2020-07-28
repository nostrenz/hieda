using System.Windows;

namespace Hieda.View.Window
{
	public partial class TwoButtonsDialog : System.Windows.Window
	{
		private bool result = false;
		int charPerLines = 45;
		int lineHeight = 10;

		public TwoButtonsDialog(string text, string caption, string leftButtonText, string rightButtonText)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			this.Title = caption;
			this.textBlock_Text.Text = text;

			// Count the number of "\n" (takes only 2 chars but count as a line)
			int countCadridgeReturn = text.Length - text.Replace("\n", "").Length;
			int textHeight = 0;

			if (text.Length > this.charPerLines) {
				textHeight = ((text.Length / this.charPerLines) * this.lineHeight) + countCadridgeReturn * this.lineHeight;
			} else {
				textHeight = this.lineHeight + 5;
			}

			this.textBlock_Text.Height = textHeight + 12;
			this.Height = this.textBlock_Text.Height + 100;

			this.LeftButtonText = leftButtonText;
			this.RightButtonText = rightButtonText;

			int leftWidth = leftButtonText.Length * 7;
			int rightWidth = rightButtonText.Length * 7;

			// Size the buttons relative to the text length
			this.button_Yes.Width = leftWidth < 30 ? 30 : leftWidth;
			this.button_No.Width = rightWidth < 30 ? 30 : rightWidth;

			// Place the buttons
			this.button_Yes.Margin = new Thickness { Left = 10, Bottom = 10 };
			this.button_No.Margin = new Thickness { Right = 10, Bottom = 10 };
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		public bool Open()
		{
			this.ShowDialog();

			return this.result;
		}

		#endregion Public

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public bool Result
		{
			get { return this.result; }
		}

		public string LeftButtonText
		{
			set { this.button_Yes.Content = value; }
		}

		public string RightButtonText
		{
			set { this.button_No.Content = value; }
		}

		public int SetWidth
		{
			set
			{
				this.textBlock_Text.Width = value;
				this.Width = value + 100;
				this.button_No.Margin = new Thickness { Top = this.textBlock_Text.Height + 25, Left = value - 10 };
				this.button_Yes.Margin = new Thickness { Top = this.textBlock_Text.Height + 25, Left = 10 };
			}
		}

		public int SetHeight
		{
			set
			{
				this.textBlock_Text.Height = value;
				this.Height = value + 100;
				this.button_No.Margin = new Thickness { Top = value + 25, Left = this.textBlock_Text.Width - 10 };
				this.button_Yes.Margin = new Thickness { Top = value + 25, Left = 10 };
			}
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called by clicking on the Yes button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Yes_Click(object sender, RoutedEventArgs e)
		{
			this.result = true;

			this.Close();
		}

		/// <summary>
		/// Called by clicking on the No button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_No_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Called by tapping on a keyboard key with the window focused.
		/// Tapping one the Return key does the same thing as clicking on the Yes button, and
		/// tapping one the Escape key does the same thing as clicking on the No button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Return) {
				this.result = true;

				this.Close();
			} else if (e.Key == System.Windows.Input.Key.Escape) {
				this.Close();
			}
		}

		#endregion Event
	}
}
