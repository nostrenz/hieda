using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for DbUpdate.xaml
	/// </summary>
	public partial class DbUpdate : System.Windows.Window
	{
		private bool finished = false;

		public delegate void ProgressDelegate(ushort left, ushort final);
		public delegate void PerformDeletage();

		public DbUpdate(ushort old, ushort final)
		{
			InitializeComponent();

			this.Owner = App.Current.MainWindow;

			// Prevent closing the window
			this.Closing += new System.ComponentModel.CancelEventHandler(CustomClosing);

			this.label_Versions.Content = String.Format("Current database version: {0} | Program database version: {1}", old, final);
			this.Progress(old, final);

			this.progressBar.Minimum = old;
			this.progressBar.Maximum = final;

			this.DoUpdate(old, final);
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Do a DB update.
		/// </summary>
		/// <param name="old"></param>
		/// <param name="left"></param>
		/// <param name="final"></param>
		public void DoUpdate(ushort from, ushort final)
		{
			App.db.Backup();

			Thread t = new Thread(() => {
				Service.Database.Migrator migrator = new Service.Database.Migrator();

				while (from < final) {
					from = migrator.Migrate(from);
					Dispatcher.Invoke((ProgressDelegate)this.Progress, from, final);
				}

				Dispatcher.Invoke((PerformDeletage)this.UpdateFinished);
			});

			t.Start();
		}

		#endregion

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Increase the progress bar.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="final"></param>
		private void Progress(ushort from, ushort final)
		{
			this.label_Versions.Content = String.Format("Current database version: {0} | Program database version: {1}", from, final);
			this.label_Progress.Content = (ushort)(final - from) + " uptades left to version " + final;

			this.progressBar.Value++;
		}

		/// <summary>
		/// Once the update is finished, wait a little then close the window.
		/// </summary>
		private void UpdateFinished()
		{
			this.finished = true;

			// Wait a little before closing the window
			DispatcherTimer timer = new DispatcherTimer();
			timer.Tick += new EventHandler(Closure);
			timer.Interval = TimeSpan.FromMilliseconds(1000);
			timer.Start();
		}

		#endregion

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called when the window is closed.
		/// It prevent doing it until the update is finished.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void CustomClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = !this.finished;
		}

		/// <summary>
		/// Close the window.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Closure(object sender, EventArgs e)
		{
			this.Close();
		}

		#endregion Event
	}
}
