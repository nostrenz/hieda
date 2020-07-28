using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Hieda.View.Element
{
	public partial class EditableLabel : Grid
	{
		private string placeholder = "";
		private bool canBeNull = false;
		private bool enabled = true;
		private ushort maxChars = 0;
		private bool onlyNumeric = false;
		private bool edition = false;
		private string hideText = null;

		public event EventHandler Selected;
		public event EventHandler Edited;

		public EditableLabel()
		{
			InitializeComponent();

			this.TextBox.Focusable = false;
			this.TextBox.IsHitTestVisible = false;
			this.TextBox.Opacity = 0;
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		public void SetPlaceholder()
		{
			this.Label.Text = this.placeholder;
			this.Label.Foreground = new SolidColorBrush(Colors.Gray);
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private void ToTextbox()
		{
			this.edition = true;

			this.Label.Opacity = 0;
			this.Label.Focusable = false;
			this.Label.IsHitTestVisible = false;

			this.TextBox.Focusable = true;
			this.TextBox.IsHitTestVisible = true;
			this.TextBox.Focus();
			this.TextBox.Opacity = 1;
			this.TextBox.FontSize = this.Label.FontSize;
		}

		private void ToLabel()
		{
			this.edition = false;

			if (String.IsNullOrWhiteSpace(this.TextBox.Text)) {
				if (this.canBeNull) {
					if (this.placeholder != "") {
						this.Label.Text = this.placeholder;
						this.Label.Foreground = new SolidColorBrush(Colors.Gray);
					}
				}
			} else if (this.TextBox.Text == this.hideText) {
				this.Label.Text = "";
			} else {
				// Only numbers are allowed
				if (this.onlyNumeric) {
					if (Tools.IsNumeric(this.TextBox.Text)) {
						this.Label.Text = this.TextBox.Text;
					}
				} else {
					this.Label.Text = this.TextBox.Text;
				}
			}

			// Limit chars
			if (this.maxChars > 0 && this.Label.Text.Length > this.maxChars) {
				this.Label.Text = this.Label.Text.Substring(0, this.maxChars);
			}

			this.Label.Opacity = 1;
			this.Label.Focusable = true;
			this.Label.IsHitTestVisible = true;

			this.TextBox.Focusable = false;
			this.TextBox.IsHitTestVisible = false;
			this.TextBox.Opacity = 0;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public ushort BorderThickness
		{
			get { return 0; }
			set { this.TextBox.BorderThickness = new Thickness(value); }
		}

		/// <summary>
		/// To know if the field is currently being edited.
		/// </summary>
		public bool Edition
		{
			get { return this.edition; }
			set { this.edition = value; }
		}

		public bool OnlyNumeric
		{
			get { return this.onlyNumeric; }
			set { this.onlyNumeric = value; }
		}

		public TextAlignment TextAlignment
		{
			get { return this.Label.TextAlignment; }
			set
			{
				this.Label.TextAlignment = value;
				this.TextBox.TextAlignment = value;
			}
		}

		public TextWrapping TextWrapping
		{
			get { return this.Label.TextWrapping; }
			set { this.Label.TextWrapping = value; }
		}

		public ushort MaxChars
		{
			get { return this.maxChars; }
			set { this.maxChars = value; }
		}

		public bool Enabled
		{
			get { return this.enabled; }
			set
			{
				this.enabled = value;
				this.Label.IsHitTestVisible = value;
			}
		}

		public double TextboxWidth
		{
			get { return this.TextBox.Width; }
			set { this.TextBox.Width = value; }
		}

		public double TextboxHeight
		{
			get { return this.TextBox.Height; }
			set { this.TextBox.Height = value; }
		}

		public bool IsMultiline
		{
			get { return this.TextBox.AcceptsReturn; }
			set { this.TextBox.AcceptsReturn = value; }
		}

		public bool CanBeNull
		{
			get { return this.canBeNull; }
			set { this.canBeNull = value; }
		}

		public Brush LabelBackground
		{
			get { return this.Label.Background; }
			set { this.Label.Background = value; }
		}

		public Brush TextboxBgColor
		{
			get { return this.TextBox.Background; }
			set { this.TextBox.Background = value; }
		}

		public int MaxLines
		{
			set { this.TextBox.MaxLines = value; }
			get { return this.TextBox.MaxLines; }
		}

		public int MaxLength
		{
			set { this.TextBox.MaxLength = value; }
			get { return this.TextBox.MaxLength; }
		}

		public double SetHeight
		{
			set
			{
				if (value < 0) {
					value = 0;
				}

				this.TextBox.Height = value;
				this.Label.Height = value;
				this.Height = value;
			}
			get { return this.Height; }
		}

		public string Placeholder
		{
			get { return this.placeholder; }
			set { this.placeholder = value; }
		}

		public string Text
		{
			get { return this.TextBox.Text.Trim(); }
			set {
				this.TextBox.Text = value;

				if (value != this.hideText) {
					this.Label.Text = value;
				}
			}
		}

		// Same as this.Text but without the placeholder
		public string ActualText
		{
			get
			{
				string text = this.Text;

				if (text == this.placeholder) {
					return "";
				}

				return text;
			}
		}

		public int FontSize
		{
			get { return (int)this.Label.FontSize; }
			set { this.Label.FontSize = value; }
		}

		public Brush Foreground
		{
			get { return this.Label.Foreground; }
			set
			{
				this.Label.Foreground = value;
				this.TextBox.Foreground = value;
			}
		}

		public string HideText
		{
			set { this.hideText = value; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event
		
		/// <summary>
		/// Called by clicking on the label.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Label_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!this.enabled) {
				return;
			}

			if (Selected != null) Selected(null, e);

			this.ToTextbox();
		}

		/// <summary>
		/// Called by focusing a different element that isn't the text box.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (!this.enabled) {
				return;
			}

			// Define if the field's content was changed
			bool changed = (this.Label.Text != this.TextBox.Text);

			this.ToLabel();

			// Raise event
			if (changed && Edited != null) {
				Edited(null, e);
			}
		}

		#endregion Event
	}
}
