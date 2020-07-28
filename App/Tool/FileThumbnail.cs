using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using prjInterfaces;

/// <summary>
/// Used to retrieve the thumb from a video file.
/// Not done by me, borrowed from https://stackoverflow.com/a/21752100
/// </summary>
public class FileThumbnail : IDisposable
{
	private Bitmap thumbBitmap;

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern int SHGetDesktopFolder(out IShellFolder ppDesktopFolder);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern int SHGetPathFromIDList(
		IntPtr pidl,
		StringBuilder pszPath
	);

	[DllImport("shell32.dll", EntryPoint = "#157", CharSet = CharSet.Unicode)]
	private static extern IntPtr ILCreateFromPath(
		[MarshalAs(UnmanagedType.LPTStr)]string lpszPath
	);

	[DllImport("shell32.dll", EntryPoint = "#155", CharSet = CharSet.Unicode)]
	private static extern int ILFree(IntPtr pidl);

	[DllImport("shell32.dll", EntryPoint = "#18", CharSet = CharSet.Unicode)]
	private static extern IntPtr ILClone(IntPtr pidl);

	[DllImport("shell32.dll", EntryPoint = "#16", CharSet = CharSet.Unicode)]
	private static extern IntPtr ILFindLastID(IntPtr pidl);

	[DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", CharSet = CharSet.Unicode)]
	private static extern int SHGetFileInfo(
		string pszPath,
		int dwFileAttributes,
		ref SHFILEINFO psfi,
		int cbFileInfo,
		SHGFIFLAGS uFlags
	);

	[DllImport("gdi32.dll")]
	static extern bool DeleteObject(IntPtr hObject);

	//==========================================================
	//renvoie un IShellFolder correspondant au dossier parent de szFileName
	//==========================================================
	//IN szFileName : nom de fichier complet
	//OUT pidl : pointeur vers un ITEMLIST contenant le nom de fichier de szFilename
	//renvoie une instance de IShellFolder
	//==========================================================
	private IShellFolder GetShellFolder(string szFileName, ref IntPtr pidl)
	{
		IShellFolder folder = null;
		IShellFolder item = null;
		string szFile;
		string szPath;
		int cEaten = 0;
		IntPtr abspidl;
		
		// Initialize IShellFolder guid
		Guid uuidIShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");

		// Retrieve the desktop folder
		SHGetDesktopFolder(out folder);

		//si c'est un lecteur seul, on le base sur "My Computer" (enfin, son guid)
		if (szFileName.Length == 3) {
			szFileName = Path.GetFullPath(szFileName);
			szPath = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
			szFile = szFileName;
			//sinon, on sépare dossier parent et nom de fichier
		} else {
			szPath = Path.GetDirectoryName(szFileName);
			szFile = Path.GetFileName(szFileName);
		}

		// Parse the parent folder name
		folder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, szPath, out cEaten, out pidl, IntPtr.Zero);
		//on crée un objet séparé (du bureau) pour le dossier parent
		item = folder.BindToObject(pidl, IntPtr.Zero, ref uuidIShellFolder);
		ILFree(pidl);
		
		// Calculate the ITEMLIST of the file's folder (without the parent folder name)
		abspidl = ILCreateFromPath(szFileName);
		pidl = ILFindLastID(abspidl);
		pidl = ILClone(pidl);
		ILFree(abspidl);

		return item;
	}

	//==========================================================
	//renvoie une instance de l'interface d'extraction de miniature pour item
	//==========================================================
	//IN item : instance de IShellFolder depuis laquelle on extrait l'extracteur de miniature
	//IN pidl : pointeur vers un ITEMLIST contenant le nom de fichier dont on veut la miniature
	//renvoie une instance de IExtractImage
	//==========================================================
	private IExtractImage getThumbnail(IShellFolder item, IntPtr pidl)
	{
		int prgf = 0;
		Guid uuidIExtractImage = new Guid("BB2E617C-0920-11D1-9A0B-00C04FC2D6C1");

		try {
			// If we have a file's pidl
			if (pidl != IntPtr.Zero) {
				// Extract the file's thumbnail
				return (IExtractImage)item.GetUIObjectOf(IntPtr.Zero, 1, ref pidl, ref uuidIExtractImage, out prgf);
			} else {
				// Or use the parent folder
				return (IExtractImage)item.CreateViewObject(IntPtr.Zero, ref uuidIExtractImage);
			}
		} catch { return null; }
	}

	//==========================================================
	//renvoie une instance de l'interface d'extraction d'icône pour item
	//==========================================================
	//IN item : instance de IShellFolder depuis laquelle on extrait l'extracteur d'icône
	//IN pidl : pointeur vers un ITEMLIST contenant le nom de fichier dont on veut l'icône
	//renvoie une instance de IExtractIcon
	//==========================================================
	private IExtractIcon getIcon(IShellFolder item, IntPtr pidl)
	{
		int prgf = 0;
		Guid uuidIExtractIcon = new Guid("000214FA-0000-0000-C000-000000000046");

		try {
			if (pidl != IntPtr.Zero) {
				return (IExtractIcon)item.GetUIObjectOf(IntPtr.Zero, 1, ref pidl, ref uuidIExtractIcon, out prgf);
			} else {
				return (IExtractIcon)item.CreateViewObject(IntPtr.Zero, ref uuidIExtractIcon);
			}
		} catch { return null; }
	}

	//==========================================================
	//renvoie la miniature (ou à défaut l'icône) du fichier szFileName
	//=========================================================
	//IN szFileName : nom du fichier dont on veut la miniature
	//IN dwCX : largeur de la miniature
	//IN dwCY : hauteur de la miniature
	//IN allowIcon: some videos don't have a true thumbnail.
	//              if true, it will return the icon of the default program used to open this kind of file
	//              (the icon of VLC for example)
	//renvoie une instance d'image VB IPictureDisp
	//==========================================================
	private Bitmap ExtractImage(string szFileName, int dwCX, int dwCY, bool allowIcon = true)
	{
		int priority = 0;
		int requestedColourDepth;
		int flags;
		SIZE sz;
		IntPtr pidl = IntPtr.Zero;
		IShellFolder isf = null;
		IExtractImage ie = null;
		IExtractIcon ii = null;
		StringBuilder szPath;
		int pindex = 0;
		IntPtr pIconLarge = IntPtr.Zero;
		IntPtr pIconSmall = IntPtr.Zero;
		SHFILEINFO shgfi = new SHFILEINFO();
		Bitmap ret = null;

		requestedColourDepth = 32;
		flags = (int)(EIEIFLAG.IEIFLAG_ASPECT | EIEIFLAG.IEIFLAG_OFFLINE | EIEIFLAG.IEIFLAG_SCREEN);

		//on récupère le nom de fichier sous forme ITEMLIST (pidl) et le dossier parent sous forme IShellFolder
		isf = GetShellFolder(szFileName, ref pidl);
		
		// Try to get the thumbnail
		ie = getThumbnail(isf, pidl);
		
		// If not possible
		if (ie == null) {
			// The icon
			ii = getIcon(isf, pidl);

			// If possible
			if (!(ii == null)) {
				// Extract the icon
				szPath = new StringBuilder(260);

				ii.GetIconLocation(0, szPath, 260, ref pindex, ref flags);
				ii.Extract(szPath.ToString(), pindex, ref pIconLarge, ref pIconSmall, dwCX + 65536 * dwCX);

				if (pIconLarge == IntPtr.Zero) {
					SHGetFileInfo(szFileName, 0, ref shgfi, Marshal.SizeOf(shgfi), SHGFIFLAGS.SHGFI_LARGEICON | SHGFIFLAGS.SHGFI_ICON | SHGFIFLAGS.SHGFI_OVERLAYINDEX);
					ret = Icon.FromHandle(shgfi.hIcon).ToBitmap();
				} else {
					ret = Icon.FromHandle(pIconLarge).ToBitmap();
				}
			} else {
				SHGetFileInfo(szFileName, 0, ref shgfi, Marshal.SizeOf(shgfi), SHGFIFLAGS.SHGFI_LARGEICON | SHGFIFLAGS.SHGFI_ICON | SHGFIFLAGS.SHGFI_OVERLAYINDEX);
				ret = Icon.FromHandle(shgfi.hIcon).ToBitmap();
			}
		} else { // If possible
			// Extract the thumbnail with the wanted dimensions
			sz.cx = dwCX;
			sz.cy = dwCY;

			szPath = new StringBuilder(260);
			EIEIFLAG f = (EIEIFLAG)flags;
			int r = ie.GetLocation(szPath, szPath.Capacity, out priority, ref sz, requestedColourDepth, ref f);
			
			try {
				pIconLarge = ie.Extract();
			} catch { }

			// If not possible
			if (pIconLarge == IntPtr.Zero) {
				// Retry to get the icon
				ii = getIcon(isf, pidl);

				// If possible
				if (!(ii == null)) {
					szPath = new StringBuilder(260);

					ii.GetIconLocation(0, szPath, 260, ref pindex, ref flags);
					ii.Extract(szPath.ToString(), pindex, ref pIconLarge, ref pIconSmall, dwCX + 65536 * dwCX);

					if (pIconLarge == IntPtr.Zero) {
						SHGetFileInfo(szFileName, 0, ref shgfi, Marshal.SizeOf(shgfi), SHGFIFLAGS.SHGFI_LARGEICON | SHGFIFLAGS.SHGFI_ICON);
						ret = Icon.FromHandle(shgfi.hIcon).ToBitmap();
					} else {
						if (!allowIcon) {
							return null;
						}

						ret = Icon.FromHandle(pIconLarge).ToBitmap();
					}
				} else {
					SHGetFileInfo(szFileName, 0, ref shgfi, Marshal.SizeOf(shgfi), SHGFIFLAGS.SHGFI_LARGEICON | SHGFIFLAGS.SHGFI_ICON);
					ret = Icon.FromHandle(shgfi.hIcon).ToBitmap();
				}
			} else {
				ret = (Bitmap)Bitmap.FromHbitmap(pIconLarge);
			}
		}

		ILFree(pidl);

		if (ret == null) {
			ret.MakeTransparent();
		}

		if (pIconLarge != IntPtr.Zero) DeleteObject(pIconLarge);
		if (pIconSmall != IntPtr.Zero) DeleteObject(pIconSmall);

		if (isf != null) Marshal.ReleaseComObject(isf);
		if (ie != null) Marshal.ReleaseComObject(ie);
		if (ii != null) Marshal.ReleaseComObject(ii);

		return ret;
	}

	public Bitmap Thumbnail
	{
		get { return thumbBitmap; }
		set { thumbBitmap = value; }
	}

	public FileThumbnail(string szFileName, int cx, int cy, bool allowIcon = true)
	{
		if (string.IsNullOrEmpty(szFileName)) {
			throw new ArgumentNullException("szFileName");
		}

		string coverDir = Hieda.App.appFolder + @"\files\covers\thumb\";
		string coverFile = coverDir + Path.GetFileNameWithoutExtension(szFileName) + ".png";

		// That image is in the covers directory, use it instead of generating a new one
		if (File.Exists(coverFile)) {
			this.thumbBitmap = new Bitmap(Bitmap.FromFile(coverFile));

			return;
		}

		if (cx < 0) {
			throw new ArgumentOutOfRangeException("cx");
		}

		if (cy < 0) {
			throw new ArgumentOutOfRangeException("cy");
		}

		this.thumbBitmap = this.ExtractImage(szFileName, cx, cy, allowIcon);

		if (this.thumbBitmap == null) {
			return;
		}

		// Save the image in the cover directory
		if (!Directory.Exists(coverDir)) {
			Directory.CreateDirectory(coverDir);
		}

		this.thumbBitmap.Save(coverFile);
	}

	#region IDisposable Members

	public void Dispose()
	{
		if (this.thumbBitmap != null) {
			this.thumbBitmap.Dispose();
			this.thumbBitmap = null;
		}
	}

	#endregion
}