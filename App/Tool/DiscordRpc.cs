using System;
using System.Runtime.InteropServices;
using Hieda.Properties;

namespace Hieda.Tool
{
	/// <summary>
	/// Allows to use functions from the Discord RPC DLL
	/// https://github.com/nostrenz/cshap-discord-rpc-demo
	/// </summary>
	public class DiscordRpc
	{
		// 32bit Discord RPC DLL
		const string DDL = "discord-rpc-w32";
		const string RPC_CLIENT_ID = "378515022260731904";

		private static RichPresence presence;
		private static EventHandlers rpcHandlers;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ReadyCallback();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void DisconnectedCallback(int errorCode, string message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ErrorCallback(int errorCode, string message);

		public struct EventHandlers
		{
			public ReadyCallback readyCallback;
			public DisconnectedCallback disconnectedCallback;
			public ErrorCallback errorCallback;
		}

		// Values explanation and example: https://discordapp.com/developers/docs/rich-presence/how-to#updating-presence-update-presence-payload-fields
		[System.Serializable]
		public struct RichPresence
		{
			public string state; /* max 128 bytes */
			public string details; /* max 128 bytes */
			public long startTimestamp;
			public long endTimestamp;
			public string largeImageKey; /* max 32 bytes */
			public string largeImageText; /* max 128 bytes */
			public string smallImageKey; /* max 32 bytes */
			public string smallImageText; /* max 128 bytes */
			public string partyId; /* max 128 bytes */
			public int partySize;
			public int partyMax;
			public string matchSecret; /* max 128 bytes */
			public string joinSecret; /* max 128 bytes */
			public string spectateSecret; /* max 128 bytes */
			public bool instance;
		}

		[DllImport(DDL, EntryPoint = "Discord_Initialize", CallingConvention = CallingConvention.Cdecl)]
		private static extern void Initialize(string applicationId, ref EventHandlers handlers, bool autoRegister, string optionalSteamId);

		[DllImport(DDL, EntryPoint = "Discord_UpdatePresence", CallingConvention = CallingConvention.Cdecl)]
		private static extern void UpdatePresence(ref RichPresence presence);

		[DllImport(DDL, EntryPoint = "Discord_RunCallbacks", CallingConvention = CallingConvention.Cdecl)]
		public static extern void RunCallbacks();

		[DllImport(DDL, EntryPoint = "Discord_Shutdown", CallingConvention = CallingConvention.Cdecl)]
		public static extern void Shutdown();

		/*
		============================================
		Custom functions
		============================================
		*/

		/// <summary>
		/// Initialize Discord RPC.
		/// </summary>
		public static void InitializeRpc()
		{
			rpcHandlers = new EventHandlers();

			rpcHandlers.readyCallback = RpcReadyCallback;
			rpcHandlers.disconnectedCallback += RpcDisconnectedCallback;
			rpcHandlers.errorCallback += RpcErrorCallback;

			string clientId = RPC_CLIENT_ID;

			// Use the custom Client ID if available
			if (!String.IsNullOrWhiteSpace(Settings.Default.RPC_ClientId)) {
				clientId = Settings.Default.RPC_ClientId;
			}

			Initialize(clientId, ref rpcHandlers, true, null);
		}

		/// <summary>
		/// Update Discord RPC status.
		/// Accented characters will be replaced by non accented ones as they're not displayed correclty in Discord.
		/// </summary>
		/// <param name="details"></param>
		/// <param name="state"></param>
		/// <param name="startTimestamp"></param>
		/// <param name="largeImg"></param>
		/// <param name="largeTxt"></param>
		public static void UpdateRpc(string details, string state, long startTimestamp = 0, string largeImg = null, string largeTxt = null)
		{
			presence.details = Tools.ReplaceDiacritics(details);
			presence.state = Tools.ReplaceDiacritics(state);
			presence.startTimestamp = startTimestamp;
			presence.largeImageKey = largeImg;
			presence.largeImageText = Tools.ReplaceDiacritics(largeTxt);

			UpdatePresence(ref presence);
		}

		/// <summary>
		/// Will be run when Tool.DiscordRpc.RunCallbacks() is called if ready.
		/// </summary>
		private static void RpcReadyCallback()
		{
			Console.WriteLine("RPC Ready");
		}

		/// <summary>
		/// Will be run when Tool.DiscordRpc.RunCallbacks() is called if disconnected.
		/// </summary>
		private static void RpcDisconnectedCallback(int errorCode, string message)
		{
			Console.WriteLine(string.Format("RPC Disconnected {0}: {1}", errorCode, message));
		}

		/// <summary>
		/// Will be run when Tool.DiscordRpc.RunCallbacks() is called if an error happened.
		/// </summary>
		private static void RpcErrorCallback(int errorCode, string message)
		{
			Console.WriteLine(string.Format("RPC Error {0}: {1}", errorCode, message));
		}

		/// <summary>
		/// Initialize or shutdown Discord RPC.
		/// </summary>
		/// <param name="enabled"></param>
		public static void ToggleRpc(bool enabled)
		{
			// Check if Discord RPC need to be initialized or shutdown
			if (Settings.Default.RPC_Enabled == false && enabled == true) { // Turned on
				InitializeRpc();
			} else if (Settings.Default.RPC_Enabled == true && enabled == false) { // Turned off
				Shutdown();
			}
		}

		/// <summary>
		/// If the client ID had changed, we need to shutdown it then re-initialize it.
		/// </summary>
		public static void RestartRpc()
		{
			// Already initialized, shutdown it first
			if (Settings.Default.RPC_Enabled == true) {
				Shutdown();
			}

			InitializeRpc();
		}
	}
}
