using System;
using System.Runtime.InteropServices;
using System.Text;

namespace prjInterfaces
{
	[ComImport, Guid("000214FA-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IExtractIcon
	{
		void GetIconLocation([In] int uFlags, [In, Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder szIconFile, [In] int cchMax, [In, Out] ref int piIndex, [In, Out] ref int pwFlags);
		void Extract([In, MarshalAs(UnmanagedType.LPWStr)] string pszFile, [In] int nIconIndex, [In, Out] ref IntPtr phiconLarge, [In, Out] ref IntPtr phiconSmall, [In] int nIconSize);
	}
}
