using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hieda.Properties;
using Microsoft.Win32;
using File = System.IO.File;
using Level = Hieda.Constants.Level;

// From WindowsAPICodePack-Core and WindowsAPICodePack-Shell (available with NuGet)
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace Hieda.View.Element
{
	public partial class Sidebar : UserControl
	{
		private bool isOpen = false;
		private bool isEpisode = false;

		public event EventHandler CloseRequested;

		public Sidebar()
		{
			InitializeComponent();
			
			this.field_Synopsis.SetHeight = 500;
			this.label_Title.MaxLength = 70;
			this.field_Synopsis.Placeholder = Lang.SIDEBAR_SYNOPSIS;
			this.field_Synopsis.CanBeNull = true;
			this.label_Title.CanBeNull = false;
			this.field_Synopsis.IsMultiline = true;
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		public void RemoveBigCover()
		{
			this.image_BigPicture.Source = null;
		}

		public void SetEpisodesCombo()
		{
			this.combo_Status.Items.Clear();
			this.combo_Status.Items.Add(Lang.WATCHED);
			this.combo_Status.Items.Add(Lang.NOTWATCHED);
		}

		public void SetSerieAndSeasonsCombo()
		{
			this.combo_Status.Items.Clear();
			this.combo_Status.Items.Add(Lang.NONE);
			this.combo_Status.Items.Add(Lang.TO_WATCH);
			this.combo_Status.Items.Add(Lang.CURRENT);
			this.combo_Status.Items.Add(Lang.STANDBY);
			this.combo_Status.Items.Add(Lang.FINISHED);
			this.combo_Status.Items.Add(Lang.DROPPED);
		}

		public async void SetEpisode(Entity.Episode episode)
		{
			// Reset
			this.Label_Size.Content = null;
			this.Label_Framerate.Content = null;
			this.Label_Resolution.Content = null;
			this.Label_Duration.Content = null;

			if (episode == null) {
				return;
			}

			string filepath = episode.File.Path;

			// Autodetect framerate, width and height if possible
			if (!File.Exists(filepath)) {
				return;
			}

			try {
				await System.Threading.Tasks.Task.Run(() => this.SetFileInfos(filepath));
			} catch (ShellException) { }
		}

		#endregion

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Get some informations about a file and put them in the view.
		/// </summary>
		private void SetFileInfos(string filepath)
		{
			System.IO.FileInfo info = new System.IO.FileInfo(filepath);
			ShellObject shellObj = ShellObject.FromParsingName(filepath);

			// Available properties: https://msdn.microsoft.com/en-us/library/windows/desktop/ff521738(v=vs.85).aspx
			IShellProperty frameRate = shellObj.Properties.System.Video.FrameRate;
			IShellProperty frameWidth = shellObj.Properties.System.Video.FrameWidth;
			IShellProperty frameHeight = shellObj.Properties.System.Video.FrameHeight;
			IShellProperty duration = shellObj.Properties.System.Media.Duration;

			Application.Current.Dispatcher.BeginInvoke((Action)(() => {
				this.Label_Size.Content = "Size: " + (info.Length / 1000000) + "Mo";

				if (frameRate != null && frameRate.ValueAsObject != null) {
					this.Label_Framerate.Content = "Framerate: " + ((uint)frameRate.ValueAsObject * 0.001).ToString() + "FPS";
				}

				if ((frameWidth != null && frameWidth.ValueAsObject != null) && (frameHeight != null && frameHeight.ValueAsObject != null)) {
					this.Label_Resolution.Content = "Resolution: " + frameWidth.ValueAsObject.ToString() + "x" + frameHeight.ValueAsObject.ToString();
				}

				if (duration != null) {
					this.Label_Duration.Content = "Duration: " + duration.FormatForDisplay(PropertyDescriptionFormatOptions.None);
				}
			}));
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string Synopsis
		{
			get { return this.field_Synopsis.ActualText; }
			set
			{
				if (String.IsNullOrWhiteSpace(value)) {
					this.field_Synopsis.SetPlaceholder();
				} else {
					this.field_Synopsis.Text = value;
				}
			}
		}

		public string EpisodesValues
		{
			set { this.label_EpisodesValues.Content = value; }
			get { return this.label_EpisodesValues.Content.ToString(); }
		}

		public bool IsOpen
		{
			get { return this.isOpen; }
			set { this.isOpen = value; }
		}

		public bool IsEpisode
		{
			get { return this.isEpisode; }
			set { this.isEpisode = value; }
		}

		public string Title
		{
			get { return this.label_Title.ActualText; }
			set { this.label_Title.Text = value; }
		}

		public double BigHeight
		{
			get { return this.image_BigPicture.Height; }
			set { this.image_BigPicture.Height = value; }
		}

		public string CoverFromString
		{
			get { return this.image_Cover.Source.ToString(); }
			set
			{
				if (!String.IsNullOrEmpty(value) && File.Exists(value)) {
					this.image_Cover.Source = new BitmapImage(new Uri(value));
				}
			}
		}

		public ImageSource CoverSource
		{
			get { return this.image_Cover.Source; }
			set
			{
				this.image_Cover.Source = value;

				if (Settings.Default.SidebarBackground) {
					this.image_BigPicture.Source = value;
				}
			}
		}

		public Level Type
		{
			set
			{
				switch (value) {
					case Level.Serie:
						this.label_Type.Content = Lang.SERIE;
					break;
					case Level.Season:
						this.label_Type.Content = Lang.SEASON;
					break;
					case Level.Episode:
						this.label_Type.Content = Lang.EPISODE;
					break;
				}
			}
			get
			{
				string label = this.label_Type.Content.ToString();

				if (label == Lang.SERIE) {
					return Level.Serie;
				} else if (label == Lang.SEASON) {
					return Level.Season;
				} else if (label == Lang.EPISODE) {
					return Level.Episode;
				}

				return 0;
			}
		}

		/// <summary>
		/// Get or set the selected status.
		///
		/// When setting, a positive value means it's a personnal status which we don't support here.
		/// In that case, the selected value in combo will remain blank.
		/// </summary>
		public Entity.DefaultStatus Status
		{
			get { return (Entity.DefaultStatus)(this.combo_Status.SelectedIndex * -1); }
			set { this.combo_Status.SelectedIndex = (value <= 0) ? (int)value * -1 : -1; }
		}

		public List<Entity.Genre> Genres
		{
			set
			{
				this.label_Genres.Text = "";

				if (value == null) {
					return;
				}

				for (byte i = 0; i < value.Count; i++) {
					this.label_Genres.Text = this.label_Genres.Text + value[i].Name;

					if (i < value.Count - 1) {
						this.label_Genres.Text += ", ";
					}
				}
			}
		}

		public string Studio
		{
			set
			{
				this.label_Studio.Content = "";

				if (value != null) {
					this.label_Studio.Content += "Studio: " + value;
				}
			}
		}

		public string Premiered
		{
			set
			{
				this.label_Premiered.Content = "";

				if (value != null) {
					this.label_Premiered.Content += "Premiered: " + value;
				}
			}
		}

		#endregion

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called when the element is resized.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			// Check IsLoaded to prevent a NullReferenceException from the designer on Collection.xaml
			if (this.IsLoaded) {
				Thickness margin = this.Margin;
				margin.Right = App.Current.MainWindow.ActualWidth / 2;
				this.Margin = margin;
			}

			Point btnPoint = this.button_Close.TransformToAncestor(this.grid_Content).Transform(new Point(0, 0));
			Point synopsisPoint = this.field_Synopsis.TransformToAncestor(this.grid_Content).Transform(new Point(0, 0));

			this.field_Synopsis.Width = this.ActualWidth + 120;
			this.field_Synopsis.SetHeight = (btnPoint.Y - synopsisPoint.Y) - 10;
		}

		/// <summary>
		/// Called by clicking on the close button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Close_Click(object sender, RoutedEventArgs e)
		{
			if (CloseRequested != null) {
				CloseRequested(sender, e);
			}
		}

		/// <summary>
		/// Called by clicking on the cover image.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Image_Cover_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton.Equals(MouseButton.Left)) {
				string coverSource = this.CoverSource.ToString();

				if (coverSource != "pack://application:,,,/res/no.jpg") {
					if (coverSource.StartsWith("file:///")) {
						Process.Start((coverSource.Replace("file:///", "")).Replace("thumb", "full"));
					}
				} else {
					OpenFileDialog dlg = Tools.CreateOpenImageDialog();

					if ((bool)dlg.ShowDialog()) {
						this.CoverFromString = dlg.FileName;
					}
				}
			} else if (e.ChangedButton.Equals(MouseButton.Right)) {
				OpenFileDialog dlg = Tools.CreateOpenImageDialog();

				if ((bool)dlg.ShowDialog()) {
					this.CoverFromString = dlg.FileName;
				}
			}
		}

		#endregion
	}
}
