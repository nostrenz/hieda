using System;
using System.Text;
using System.Security.Cryptography;

namespace Hieda.Tool
{
	class Hash : IDisposable
	{
		/// <summary>
		/// Needed by IDisposable.
		/// </summary>
		public void Dispose()
		{
		}

		/*
		============================================
		Public
		============================================
		*/

		/// <summary>
		/// Calculate MD5 from a file path.
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public string Calculate(string filePath)
		{
			byte[] bytes = System.IO.File.ReadAllBytes(filePath);

			return this.BytesToMd5(bytes);
		}

		/// <summary>
		/// A lot slower than Calculate(string) but produces the same hash as using Create(Bitmap).
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public string CalculateAsBitmap(string filePath)
		{
			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(filePath);

			return this.Calculate(bitmap);
		}

		/// <summary>
		/// Calculate MD5 from a Bitmap object.
		/// </summary>
		/// <param name="bitmap"></param>
		/// <returns></returns>
		public string Calculate(System.Drawing.Bitmap bitmap)
		{
			System.Drawing.Imaging.BitmapData bitmapData = null;

			try {
				bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
				int numBytes = bitmapData.Stride * bitmap.Height;
				byte[] bytes = new byte[numBytes];

				System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, bytes, 0, numBytes);

				return this.BytesToMd5(bytes);
			} finally {
				if (bitmapData != null) {
					bitmap.UnlockBits(bitmapData);
				}
			}
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Converts an array of bytes into a MD5 hash.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		private string BytesToMd5(byte[] bytes)
		{
			bytes = new MD5CryptoServiceProvider().ComputeHash(bytes);
			StringBuilder result = new StringBuilder(bytes.Length * 2);

			for (int i = 0; i < bytes.Length; i++) {
				result.Append(bytes[i].ToString("x2"));
			}

			return result.ToString();
		}
	}
}
