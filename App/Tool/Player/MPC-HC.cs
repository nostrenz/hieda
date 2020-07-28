using Hieda.Properties;

namespace Hieda.Tool.Player
{
	class MPC_HC : VideoPlayer, IPlayer
	{
		public MPC_HC(System.EventHandler exited) : base(exited)
		{
			this.exe = Settings.Default.MPCHC_Path;
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
				this.AddArgument("/fullscreen");
			}

			this.AddArgument("/play");

			this.StartProcess(this.exe, this.command);
		}

		public void Stream(string url, string argument=null)
		{
			// Not implemented yet
			this.StartProcess(url);
		}

		#endregion Public
	}
}
