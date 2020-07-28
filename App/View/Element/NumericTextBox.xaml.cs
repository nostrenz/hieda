using System.Windows.Controls;

namespace Hieda.View.Element
{
	/// <summary>
	/// Interaction logic for NumericTextBox.xaml
	/// </summary>
	public partial class NumericTextBox : TextBox
	{
		private int number;

		public NumericTextBox()
		{
			InitializeComponent();
			
			this.AttachEvent();
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Update the counter textbox with the integer value.
		/// </summary>
		private void UpdateText()
		{
			string text = this.Number.ToString();

			if (this.Digits > 0 && this.Pad) {
				text = text.PadLeft(this.Digits, '0');
			}

			this.Text = text;
			this.CaretIndex = text.Length;
		}

		private void AttachEvent()
		{
			this.TextChanged += this.TextBox_TextChanged;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		// Current number
		public int Number
		{
			get { return this.number; }
			set
			{
				this.number = value;

				this.UpdateText();
			}
		}

		// Minimum value
		public int Minimum
		{
			get; set;
		}

		// Maximum value
		public int Maximum
		{
			get; set;
		}

		public bool AllowEmpty
		{
			get; set;
		}

		// Maximum number or digits
		public int Digits
		{
			get; set;
		}

		// If true, leading zeros will be added to match this.digits
		public bool Pad
		{
			get; set;
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called when the content of the textbox is changed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			// Disable the event, prevent from creating a loop when changing the text during the event
			this.TextChanged -= this.TextBox_TextChanged;

			if (!this.IsLoaded) {
				this.AttachEvent();

				return;
			}
			
			// Text length without leading zeros
			int numberLength = this.Text.TrimStart(new char[] { '0' }).Length;

			if (numberLength == 0 && !this.AllowEmpty) {
				this.Number = 0;

				this.UpdateText();
				this.AttachEvent();

				return;
			}

			// Value too long
			if (this.Digits > 0 && numberLength > this.Digits) {
				this.UpdateText();
				this.AttachEvent();

				return;
			}

			int parsed = this.Number;
			bool success = int.TryParse(this.Text, out parsed);

			if (!success) {
				this.UpdateText();
				this.AttachEvent();

				return;
			}

			if (this.Minimum == this.Maximum) {
				this.Number = parsed;
			} else if (parsed > this.Maximum) {
				this.Number = this.Maximum;
			} else if (parsed < this.Minimum) {
				this.Number = this.Minimum;
			} else {
				this.Number = parsed;
			}

			// We have a valid numeric value, update the text field
			this.UpdateText();
			this.AttachEvent();
		}

		#endregion
	}
}
