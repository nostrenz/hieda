using System.Runtime.InteropServices;

namespace prjInterfaces
{
	[ComImport, InterfaceType((short)1), Guid("000214F2-0000-0000-C000-000000000046")]
	internal interface IEnumIDList
	{
		[PreserveSig]
		int Next([In] int celt, [In, Out] ref int rgelt, [In, Out] ref int pceltFetched);
		void Skip([In] int celt);
		void Reset();
		[return: MarshalAs(UnmanagedType.Interface)]
		IEnumIDList Clone();
	}
}
