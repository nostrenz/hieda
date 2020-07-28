using System;
using System.Windows;
using System.Diagnostics;
using File = System.IO.File;
using Path = System.IO.Path;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using Hieda.Properties;


namespace Hieda.View.Window
{
	/// <summary>
	/// Interaction logic for ThumbnailGenerator.xaml
	/// </summary>
	public partial class ThumbnailGenerator : System.Windows.Window
	{
		const int SLEEP_AMOUNT = 100;
		const int MAX_SLEEP = 30000;

		private string video = null;
		private string thumb = null;
		private string frame = null;

		private Process process = null;

		public ThumbnailGenerator(System.Windows.Window owner, string video, bool autoStart=false)
		{
			InitializeComponent();

			this.Owner = owner;
			this.video = video;

			this.TextBox_FFmepg.Text = Settings.Default.FFmpeg;
			this.TextBox_Timestamp.Text = Settings.Default.Generate_Timestamp;
			this.Slider_Width.Value = Settings.Default.Generate_Width;

			if (autoStart) {
				this.Generate();
			}

			this.ShowDialog();
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Execute this command example:
		/// "E:\Program\App\ffmpeg\bin\ffmpeg.exe" -i "E:\Download\Video\Homura vs Walpurgis Night.mp4" -y -ss 00:00:40.435 -vframes 1 "E:\Download\Video\out.png"
		/// </summary>
		private void Generate()
		{
			string ffmpeg = this.TextBox_FFmepg.Text;

			if (!File.Exists(ffmpeg)) {
				MessageBox.Show(Lang.Text("badFFmpegPath"));

				return;
			}

			// Set and save settings
			Settings.Default.FFmpeg = ffmpeg;
			Settings.Default.Generate_Width = this.Slider_Width.Value;
			Settings.Default.Generate_KeepTimestamp = (bool)this.CheckBox_KeepTimestamp.IsChecked;

			if (Settings.Default.Generate_KeepTimestamp) {
				Settings.Default.Generate_Timestamp = this.TextBox_Timestamp.Text;
			}

			Settings.Default.Save();

			// Get path to the temp folder and create it if needed
			string tempFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\temp\";
			System.IO.Directory.CreateDirectory(tempFolder);

			this.process = new Process();
			this.frame = tempFolder + "frame.png";

			this.process.StartInfo.FileName = ffmpeg;
			this.process.StartInfo.Arguments = "-i \"" + this.video + "\" -y -ss \"" + this.GetTimeStamp() + "\" -vframes 1 \"" + this.frame + "\"";
			this.process.StartInfo.UseShellExecute = false;
			this.process.StartInfo.CreateNoWindow = true;
			this.process.StartInfo.ErrorDialog = true;
			this.process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
			this.process.EnableRaisingEvents = true;
			this.process.Exited += new EventHandler(this.Process_Exited);
			this.process.Start();

			this.ProgressBar.IsIndeterminate = true;
		}

		private string GetTimeStamp()
		{
			if (String.IsNullOrEmpty(this.TextBox_Timestamp.Text)) {
				return Settings.Default.Generate_Timestamp;
			}

			return this.TextBox_Timestamp.Text;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string Thumbnail
		{
			get { return this.thumb; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called when the value of the Width slider changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Slider_Width_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			this.Label_Width.Content = String.Format(Lang.Content("thumbnailWidth"), (int)this.Slider_Width.Value);
		}

		/// <summary>
		/// Called by clicking on the Generate button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Generate_Click(object sender, RoutedEventArgs e)
		{
			this.Generate();
		}

		/// <summary>
		/// Called once FFMPEG has finished producing the frame.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Process_Exited(object sender, System.EventArgs e)
		{
			// Process exit codes: -1=Killed, 0:Success, 1=Error

			// The process was manually killed (when closing the window before the end for example)
			if (this.process.ExitCode == -1) {
				return;
			}

			Dispatcher.BeginInvoke(new Action(delegate {
				this.ProgressBar.IsIndeterminate = false;

				// Process ended with an error or no frame generated
				if (this.process.ExitCode != 0 || !File.Exists(this.frame)) {
					MessageBox.Show(Lang.Text("frameGenerationFailed"));

					return;
				}

				// System.Drawing.Imaging.ImageFormat.Png
				ImageFormat format = ((bool)this.Radio_Jpg.IsChecked ? ImageFormat.Jpeg : ImageFormat.Png);

				// Generate thumbnail
				using (Tool.Cover coverTool = new Tool.Cover()) {
					this.thumb = coverTool.Create(this.frame, true, true, (int)this.Slider_Width.Value, format);
				}

				this.Close();
			}), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);
		}

		/// <summary>
		/// Called when the window is closed, used to kill the process if still running.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (this.process != null && !this.process.HasExited) {
				this.process.Kill();
			}
		}

		#endregion Event
	}
}
