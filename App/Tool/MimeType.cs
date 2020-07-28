using System.Linq;

namespace Hieda.Tool
{
	/// <summary>
	/// This class can detect the mimetype of a file by checking its first bytes.
	/// It works pretty well but isn't 100% accurate. For example, some MP4 files does not match any of the signatures bellow.
	///
	/// https://stackoverflow.com/questions/58510/using-net-how-can-you-find-the-mime-type-of-a-file-based-on-the-file-signature
	/// </summary>
	public class MimeType
	{
		// http://www.garykessler.net/library/file_sigs.html
		// Note: values given by this site are exadecimal and need to be converted to base 10 http://www.rapidtables.com/convert/number/hex-to-decimal.htm
		// For that, use HexStringToByteList("the hexa string here").

		// Images
		private static readonly byte[] BMP = { 66, 77 };
		private static readonly byte[] GIF = { 71, 73, 70, 56 };
		private static readonly byte[] ICO = { 0, 0, 1, 0 };
		private static readonly byte[] JPG = { 255, 216, 255 };
		private static readonly byte[] PNG = { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 };

		// Videos
		private static readonly byte[] MP4_ISO = { 0, 0, 0, 20, 102, 116, 121, 112, 105, 115, 111, 109};
		private static readonly byte[] MP4S    = { 0, 0, 0, 24, 102, 116, 121, 112, 51, 103, 112, 53 };
		private static readonly byte[] MP4     = { 0, 0, 0, 28, 102, 116, 121, 112, 77, 83, 78, 86, 1, 41, 0, 70, 77, 83, 78, 86, 109, 112, 52, 50 };
		private static readonly byte[] MOV     = { 0, 0, 0, 20, 102, 116, 121, 112, 113, 116, 32, 32};
		private static readonly byte[] MP4QT   = { 0, 0, 0, 24, 102, 116, 121, 112, 109, 112, 52, 50 };
		private static readonly byte[] WEBM    = { 26, 69, 223, 163 };
		private static readonly byte[] MKV     = { 26, 69, 223, 163, 147, 66, 130, 136, 109, 97, 116, 114, 111, 115, 107, 97 };
		private static readonly byte[] FLV     = { 70, 76, 86, 1 };
		private static readonly byte[] WAV_AVI = { 82, 73, 70, 70 };
		private static readonly byte[] WMV_WMA = { 48, 38, 178, 117, 142, 102, 207, 17, 166, 217, 0, 170, 0, 98, 206, 108 };

		// Other
		private static readonly byte[] ZIP_DOCX = { 80, 75, 3, 4 };
		private static readonly byte[] EXE_DLL  = { 77, 90 };
		private static readonly byte[] PDF = { 37, 80, 68, 70, 45, 49, 46 };

		// Unused
		// private static readonly byte[] OGG    = { 79, 103, 103, 83, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0 };
		//private static readonly byte[] DOC     = { 208, 207, 17, 224, 161, 177, 26, 225 };
		//private static readonly byte[] MP3     = { 255, 251, 48 };
		//private static readonly byte[] RAR     = { 82, 97, 114, 33, 26, 7, 0 };
		//private static readonly byte[] SWF     = { 70, 87, 83 };
		//private static readonly byte[] TIFF    = { 73, 73, 42, 0 };
		//private static readonly byte[] TORRENT = { 100, 56, 58, 97, 110, 110, 111, 117, 110, 99, 101 };
		//private static readonly byte[] TTF     = { 0, 1, 0, 0, 0 };

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		public static string Detect(string filepath)
		{
			byte[] bytes = System.IO.File.ReadAllBytes(filepath);

			string mime = "application/octet-stream"; // Default unknown mimetype

			// Ensure that the filename isn't empty or null
			if (string.IsNullOrWhiteSpace(filepath)) {
				return mime;
			}

			// Get the file extension
			string extension = System.IO.Path.GetExtension(filepath) == null ? string.Empty : System.IO.Path.GetExtension(filepath).ToUpper();

			// Get the MIME Type
			if (bytes.Take(BMP.Length).SequenceEqual(BMP)) {
				mime = "image/bmp";
			} else if (bytes.Take(EXE_DLL.Length).SequenceEqual(EXE_DLL)) {
				mime = "application/x-msdownload"; //both use same mime type
			} else if (bytes.Take(GIF.Length).SequenceEqual(GIF)) {
				mime = "image/gif";
			} else if (bytes.Take(ICO.Length).SequenceEqual(ICO)) {
				mime = "image/x-icon";
			} else if (bytes.Take(JPG.Length).SequenceEqual(JPG)) {
				mime = "image/jpeg";
			} else if (bytes.Take(PNG.Length).SequenceEqual(PNG)) {
				mime = "image/png";
			} else if (bytes.Take(WAV_AVI.Length).SequenceEqual(WAV_AVI)) {
				mime = extension == ".AVI" ? "video/x-msvideo" : "audio/x-wav";
			} else if (bytes.Take(WMV_WMA.Length).SequenceEqual(WMV_WMA)) {
				mime = extension == ".WMA" ? "audio/x-ms-wma" : "video/x-ms-wmv";
			} else if (bytes.Take(ZIP_DOCX.Length).SequenceEqual(ZIP_DOCX)) {
				mime = extension == ".DOCX" ? "application/vnd.openxmlformats-officedocument.wordprocessingml.document" : "application/x-zip-compressed";
			} else if (Match(bytes, MKV)) {
				return "video/x-matroska";
			} else if (Match(bytes, WEBM)) {
				return "video/webm";
			} else if (Match(bytes, MP4) || Match(bytes, MP4QT) || Match(bytes, MP4S) || Match(bytes, MP4_ISO)) {
				return "video/mp4";
			} else if (Match(bytes, FLV)) {
				return "video/x-flv";
			}

			return mime;
		}

		/// <summary>
		/// Check if the file correspond to any image type.
		/// This one should be 100% accurate.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static bool IsImage(string filepath)
		{
			byte[] bytes = System.IO.File.ReadAllBytes(filepath);

			return Match(bytes, JPG)
				|| Match(bytes, PNG)
				|| Match(bytes, BMP)
				|| Match(bytes, GIF);
		}

		/// <summary>
		/// Check if the file correspond to any video type.
		/// This one isn't 100% accurate, be careful.
		/// It can also take a long time for big files.
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		public static bool IsVideo(string filepath)
		{
			byte[] bytes = System.IO.File.ReadAllBytes(filepath);

			return Match(bytes, MKV)
				|| Match(bytes, MP4)
				|| Match(bytes, MP4QT)
				|| Match(bytes, MP4S)
				|| Match(bytes, MP4_ISO)
				|| Match(bytes, WEBM)
				|| Match(bytes, MOV)
				|| Match(bytes, FLV)
				|| Match(bytes, WAV_AVI)
				|| Match(bytes, WMV_WMA);
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Check if the filebytes match the signature given by the compareBytes.
		/// </summary>
		/// <param name="fileBytes"></param>
		/// <param name="compareBytes"></param>
		/// <returns></returns>
		private static bool Match(byte[] fileBytes, byte[] compareBytes)
		{
			return fileBytes.Take(compareBytes.Length).SequenceEqual(compareBytes);
		}

		/// <summary>
		/// Convert a list of hexadecimal bytes like "00 00 00 14 66 74 79 70 69 73 6F 6D" to a list of decimal ones.
		/// </summary>
		/// <param name="hexstr"></param>
		/// <returns></returns>
		private static string HexStringToByteList(string hexstr)
		{
			System.String[] substrings = hexstr.Split(' ');
			System.String bytelist = "";

			foreach (System.String str in substrings) {
				bytelist += HexToByte(str) + ", ";
			}

			return bytelist;
		}

		/// <summary>
		/// Convert an hexadeciaml string to a decimal byte.
		/// </summary>
		/// <param name="hex"></param>
		/// <returns></returns>
		private static byte HexToByte(string hex)
		{
			return (byte)System.Convert.ToInt32(hex, 16);
		}

		#endregion Private
	}
}
