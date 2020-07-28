using System;
using System.Windows;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for Overlay.xaml
	/// </summary>
	public partial class Overlay : System.Windows.Window
	{
		CancellationTokenSource cancel = new CancellationTokenSource();

		public Overlay(string header, string middle, string footer, int time, bool forceForeground = false)
		{
			InitializeComponent();

			this.label_Header.Content = header;
			this.label_Middle.Content = middle;
			this.label_Footer.Content = footer;

			if (forceForeground) {
				this.ForceForeground();
			}

			// Fade in
			this.Fade(true, 0.15);

			DispatcherTimer timer = new DispatcherTimer();
			timer.Tick += new EventHandler(RemoveOverlay);
			timer.Interval = TimeSpan.FromMilliseconds(time);
			timer.Start();
		}

		/// <summary>
		/// Allow the window to be click through.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			var hwnd = new WindowInteropHelper(this).Handle;
			Tool.WindowsServices.SetWindowExTransparent(hwnd);
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Fade the window.
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="time"></param>
		/// <param name="completed"></param>
		private void Fade(bool direction, double time, EventHandler completed = null)
		{
			DoubleAnimation da = new DoubleAnimation();

			da.From = direction ? 0 : 1;
			da.To = direction ? 1 : 0;
			da.Duration = new Duration(TimeSpan.FromSeconds(time));
			da.AutoReverse = false;
			da.RepeatBehavior = new RepeatBehavior(1);

			if (completed != null) {
				da.Completed += new EventHandler(CloseWindow);
			}

			this.BeginAnimation(OpacityProperty, da);
		}

		/// <summary>
		/// Bring the window to the foreground.
		/// </summary>
		private void BringToForeground()
		{
			this.Activate();
		}

		/// <summary>
		/// Force the window to be to the foreground by activating it periodically.
		/// It allows the overlay to be shown above a fullscreen video player.
		/// </summary>
		/// <param name="time"></param>
		private void ForceForeground()
		{
			Tools.RunPeriodicAsync(BringToForeground, TimeSpan.FromSeconds(0.1), cancel.Token);
		}

		#endregion Private

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Start removing the overlay by fade it out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RemoveOverlay(object sender, EventArgs e)
		{
			// Stop the timer
			((DispatcherTimer)sender).Stop();

			// Fade out
			this.Fade(false, 0.3, new EventHandler(CloseWindow));
		}


		/// <summary>
		/// Close the window after the fade out animation.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CloseWindow(object sender, EventArgs e)
		{
			this.cancel.Cancel();
			this.Close();
		}

		#endregion Event
	}
}
