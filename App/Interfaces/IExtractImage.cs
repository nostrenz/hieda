using System;
using System.Runtime.InteropServices;
using System.Text;

namespace prjInterfaces
{
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	internal struct SIZE
	{
		public int cx;
		public int cy;
	}

	[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("BB2E617C-0920-11D1-9A0B-00C04FC2D6C1")]
	internal interface IExtractImage
	{
		[PreserveSig()]
		int GetLocation([In, Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszPathBuffer, [In] int cch, [Out] out int pdwPriority, [In] ref SIZE prgSize, [In] int dwRecClrDepth, [In] ref EIEIFLAG pdwFlags);
		IntPtr Extract();
	}
}
