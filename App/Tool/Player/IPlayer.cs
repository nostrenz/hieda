using System;

namespace Hieda.Tool.Player
{
	interface IPlayer : IDisposable
	{
		/*
		============================================
		Method
		============================================
		*/

		void Play(string filepath, string argument=null);
		void Stream(string url, string argument=null);
	}
}
