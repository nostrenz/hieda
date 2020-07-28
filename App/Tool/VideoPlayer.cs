using System;
using System.Diagnostics;

namespace Hieda.Tool
{
	class VideoPlayer : IDisposable
	{
		// Path to the program's executable.
		protected string exe;

		// Command line to launch the programe
		protected string command;

		// Event to be called when the player is exited
		protected EventHandler exited;

		public VideoPlayer(EventHandler exited)
		{
			this.exited = exited;
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public
		
		/// <summary>
		/// Check if the executable exists.
		/// </summary>
		public bool CanBeExecuted()
		{
			return !String.IsNullOrWhiteSpace(this.exe) && System.IO.File.Exists(this.exe);
		}
		
		/// <summary>
		/// Start a file with an optional command line.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="command"></param>
		public void StartProcess(string filepath, string command=null)
		{
			ProcessStartInfo processInfo = new ProcessStartInfo(filepath, command);
			Process process = Process.Start(processInfo);

			if (this.exited != null) {
				try {
					// you can add the process to your process list, chack existence and etc...
					process.EnableRaisingEvents = true;
					process.Exited += this.exited;
				} catch (Exception) { }
			}
		}

		/// <summary>
		/// Add an argument to the command.
		/// </summary>
		/// <param name="argument"></param>
		public void AddArgument(string argument)
		{
			this.command += " \"" + argument + "\"";
		}

		public void Dispose()
		{
		}

		#endregion Public
	}
}
