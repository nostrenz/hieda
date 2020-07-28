using Hieda.Properties;

namespace Hieda.Tool.Player
{
	class mpv : VideoPlayer, IPlayer
	{
		/// <summary>
		/// List of supported streaming domains
		/// </summary>
		private string[] supportedStreams = { "youtube.com" };

		public mpv(System.EventHandler exited) : base(exited)
		{
			this.exe = Settings.Default.mpv_Path;
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		public void Play(string filepath, string argument=null)
		{
			// Missing executable, start the file directly
			if (!this.CanBeExecuted()) {
				this.StartProcess(filepath);

				return;
			}

			this.AddArgument(filepath);

			if (Settings.Default.OpenPlayerInFullscreen) {
				this.AddArgument("--fullscreen");
			}

			if (Settings.Default.SavePositionOnQuit) {
				this.AddArgument("--save-position-on-quit");
			}

			this.StartProcess(this.exe, this.command);
		}

		/// <summary>
		/// http://www.pascal-debus.de/index.php/2015/11/18/streaming-without-pain-using-mpv-and-youtube-dl/
		/// </summary>
		/// <param name="url"></param>
		/// <param name="argument"></param>
		public void Stream(string url, string argument=null)
		{
			// Missing executable or can't be streamed by this player
			if (!this.CanBeExecuted() || !this.IsUrlSupported(url)) {
				this.StartProcess(url);

				return;
			}

			this.AddArgument("--ytdl");
			this.AddArgument(url);

			if (Settings.Default.OpenPlayerInFullscreen) {
				this.AddArgument("--fullscreen");
			}

			this.StartProcess(this.exe, this.command);
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private
		
		/// <summary>
		/// Check if the given URL is supported to be streamed by the player.
		/// </summary>
		/// <returns></returns>
		private bool IsUrlSupported(string url)
		{
			url = url.Replace("https://", "");
			url = url.Replace("http://", "");
			url = url.Replace("www.", "");

			foreach (string domain in this.supportedStreams) {
				if (url.StartsWith(domain)) {
					return true;
				}
			}

			return false;
		}

		#endregion Private
	}
}
