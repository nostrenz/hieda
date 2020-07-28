using System;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Hieda.View.Element
{
	/// <summary>
	/// Interaction logic for Notify.xaml
	/// </summary>
	public partial class Notify : UserControl
	{
		public Notify()
		{
			InitializeComponent();
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public Constants.Notify Type
		{
			set
			{
				switch (value) {
					case Constants.Notify.Info:
						this.Icon = "info";
					break;
					case Constants.Notify.Notif:
						this.Icon = "notif";
					break;
					case Constants.Notify.Warning:
						this.Icon = "warn";
					break;
					default:
						this.Icon = "info";
					break;
				}
			}
		}

		private string Icon
		{
			set
			{
				string path = @"pack://siteoforigin:,,,/res/" + value + ".png";

				if (!System.IO.File.Exists(path)) {
					return;
				}

				this.icon.Source = new BitmapImage(new Uri(path));
			}
		}

		public string Title
		{
			get { return this.label_title.Content.ToString(); }
			set { this.label_title.Content = value; }
		}

		public string Message
		{
			get { return this.textblock_message.Text; }
			set { this.textblock_message.Text = value; }
		}

		#endregion
	}
}
