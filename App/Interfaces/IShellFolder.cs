using System;
using System.Runtime.InteropServices;

namespace prjInterfaces
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct STRRET
	{
		public ESTRRET uType;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string cstr;
	}
	[StructLayout(LayoutKind.Sequential)]
	internal struct SHFILEINFO
	{
		public IntPtr hIcon;
		public int iIcon;
		public int dwAttributes;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szDisplayName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szTypeName;
	}

	[ComImport(),
	InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
	Guid("000214E6-0000-0000-C000-000000000046")]
	internal interface IShellFolder
	{
		void ParseDisplayName([In] IntPtr hwndOwner, [In] IntPtr pbcReserved, [In, MarshalAs(UnmanagedType.LPWStr)] string lpszDisplayName, [Out] out int pchEaten, [Out] out IntPtr ppidl, [In, Out] IntPtr pdwAttributes);
		IEnumIDList EnumObjects([In] IntPtr hwndOwner, [In] ESHCONTF grfFlags);
		IShellFolder BindToObject([In] IntPtr pidl, [In] IntPtr pbcReserved, [In] ref Guid riid);
		[return: MarshalAs(UnmanagedType.Interface)]
		object BindToStorage([In] IntPtr pidl, [In] IntPtr pbcReserved, [In] ref Guid riid);
		void CompareIDs([In] IntPtr lParam, [In] IntPtr pidl1, [In] IntPtr pidl2);
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object CreateViewObject([In] IntPtr hwndOwner, [In] ref Guid riid);
		void GetAttributesOf([In] int cidl, [In] ref IntPtr apidl, [In, Out] ref ESFGAO rgfInOut);
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object GetUIObjectOf([In] IntPtr hwndOwner, [In] int cidl, ref IntPtr apidl, ref Guid riid, out int prgfInOut);
		void GetDisplayNameOf([In] IntPtr pidl, [In] ESHGDN uFlags, [In, Out] ref STRRET lpName);
		void SetNameOf([In] IntPtr hwndOwner, [In] IntPtr pidl, [In, MarshalAs(UnmanagedType.LPWStr)] string lpszName, [In] ESHCONTF uFlags, [Out] out IntPtr ppidlOut);
	}
}
