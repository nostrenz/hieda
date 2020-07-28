using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Hieda.View.Element
{
	/// <summary>
	/// Interaction logic for CoverSelectorItem.xaml
	/// </summary>
	public partial class CoverSelectorItem : UserControl
	{
		public event EventHandler Clicked;
		private string fullCover;

		public CoverSelectorItem()
		{
			InitializeComponent();

			this.image_Cover.Focusable = true;
		}

		/// <summary>
		/// Only the thumb is shown in the view, the full image is juste kept by the object.
		/// </summary>
		/// <param name="thumbUrl"></param>
		/// <param name="fullUrl"></param>
		public CoverSelectorItem(string thumbUrl, string fullUrl = null)
		{
			InitializeComponent();

			this.image_Cover.Focusable = true;
			this.ThumbCover = thumbUrl;

			if (fullUrl != null) {
				this.FullCover = fullUrl;
			} else {
				this.FullCover = thumbUrl;
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string ThumbCover
		{
			set
			{
				try {
					this.image_Cover.Source = new BitmapImage(new Uri(value));
				} catch (Exception) {
					Console.WriteLine("*SYSTEM: Bad path for cover file");
				}
			}
			get { return this.image_Cover.Source.ToString(); }
		}

		public string FullCover
		{
			set { this.fullCover = value; }
			get { return this.fullCover; }
		}

		#endregion

		/*
		============================================
		Event
		============================================
		*/

		#region Event
		
		/// <summary>
		/// Called when the cover image loses focus.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Image_Cover_LostFocus(object sender, RoutedEventArgs e)
		{
			this.border_Cover.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#000000");
		}

		/// <summary>
		/// Called when the cover image gets the focus.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Image_Cover_GotFocus(object sender, RoutedEventArgs e)
		{
			this.border_Cover.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#3A78FF");

			// Event intercepeted in OnlineFetch
			if (Clicked != null) Clicked(this.FullCover, e);
		}

		/// <summary>
		/// Called by clicking on the cover image.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Image_Cover_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!this.image_Cover.IsFocused) this.image_Cover.Focus();
		}

		#endregion
	}
}
