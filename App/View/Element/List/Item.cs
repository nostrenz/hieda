using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;


namespace Hieda.View.Element.List
{
	/// <summary>
	/// Base class for list elements (Tile & Row).
	/// </summary>
	public class Item : UserControl
	{
		public event EventHandler ContextRequested;

		protected ushort index;
		protected bool error;
		protected bool watched;

		private bool canBeHovered = true;

		/*
		============================================
		Protected
		============================================
		*/

		#region Protected

		protected ContextMenu GetSerieAndSeasonContextMenu(string cover)
		{
			ContextMenu context = new ContextMenu();

			// Note: this may be false when running from Visual Studio, as rights restrict access to the filesystem
			bool coverExists = this.CoverExists(cover);

			// Go to seasons
			MenuItem item = new MenuItem();
			item.Header = Lang.GO_TO_NEXT_LEVEL;
			item.Click += this.ContextMenu_MenuItem_Click;
			item.Tag = "goToNextLevel";
			context.Items.Add(item);
			context.Items.Add(new Separator());
			// Continue
			item = new MenuItem();
			item.Header = Lang.CONTINUE;
			item.Click += this.ContextMenu_MenuItem_Click;
			//item.IsEnabled = false;
			item.Tag = "continue";
			context.Items.Add(item);
			// Edit
			item = new MenuItem();
			item.Header = Lang.EDIT;
			item.Click += this.ContextMenu_MenuItem_Click;
			item.Tag = "edit";
			context.Items.Add(item);
			// Delete
			item = new MenuItem();
			item.Header = Lang.DELETE;
			item.Click += this.ContextMenu_MenuItem_Click;
			item.Tag = "delete";
			context.Items.Add(item);
			// Season only
			if (Collection.ItemLevel == Constants.Level.Season) {
				item = new MenuItem();
				item.Header = Lang.Content("move");
				item.Click += this.ContextMenu_MenuItem_Click;
				item.Tag = "moveItems";
				context.Items.Add(item);
			}
			// Relocate episodes
			item = new MenuItem();
			item.Header = Lang.Header("relocateEpisodes");
			item.Tag = "relocateEpisodes";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			context.Items.Add(new Separator());

			// Cover
			item = new MenuItem();
			item.Header = Lang.COVER;
			context.Items.Add(item);
			// View full
			MenuItem subItem = new MenuItem();
			subItem.Header = Lang.VIEW_FULL;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.IsEnabled = coverExists;
			subItem.Tag = "viewFull";
			item.Items.Add(subItem);
			// Copy file
			subItem = new MenuItem();
			subItem.Header = Lang.COPY_FILE;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.IsEnabled = coverExists;
			subItem.Tag = "copyFile";
			item.Items.Add(subItem);
			// Change
			subItem = new MenuItem();
			subItem.Header = Lang.CHANGE;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "change";
			item.Items.Add(subItem);
			// Remove
			subItem = new MenuItem();
			subItem.Header = Lang.REMOVE;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "removeCover";
			subItem.IsEnabled = !String.IsNullOrEmpty(cover);
			item.Items.Add(subItem);
			// Online cover search
			subItem = new MenuItem();
			subItem.Header = Lang.Content("search");
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "onlineCover";
			item.Items.Add(subItem);

			// Status
			this.AddStatusSubContext(ref context);

			return context;
		}

		protected ContextMenu GetEpisodeContextMenu(string cover)
		{
			ContextMenu context = new ContextMenu();
			bool coverExists = this.CoverExists(cover);

			// Edit
			MenuItem item = new MenuItem();
			item.Header = Lang.EDIT;
			item.Click += this.ContextMenu_MenuItem_Click;
			item.Tag = "edit";
			context.Items.Add(item);
			// Delete
			item = new MenuItem();
			item.Header = Lang.DELETE;
			item.Click += this.ContextMenu_MenuItem_Click;
			item.Tag = "delete";
			context.Items.Add(item);
			// Move
			item = new MenuItem();
			item.Header = Lang.Content("move");
			item.Click += this.ContextMenu_MenuItem_Click;
			item.Tag = "moveItems";
			context.Items.Add(item);
			// -
			context.Items.Add(new Separator());
			// Play with
			item = new MenuItem();
			item.Header = Lang.Header("playWith");
			context.Items.Add(item);
			// Default player
			MenuItem subItem = new MenuItem();
			subItem.Header = Lang.Header("defaultPlayer");
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.IsEnabled = !this.error;
			subItem.Tag = "playWithDefault";
			item.Items.Add(subItem);
			// VLC
			subItem = new MenuItem();
			subItem.Header = "VLC";
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.IsEnabled = !this.error;
			subItem.Tag = "playWithVlc";
			item.Items.Add(subItem);
			// MPC-HC
			subItem = new MenuItem();
			subItem.Header = "MPC-HC";
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.IsEnabled = !this.error;
			subItem.Tag = "playWithMpcHc";
			item.Items.Add(subItem);
			// mpv
			subItem = new MenuItem();
			subItem.Header = "mpv";
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.IsEnabled = !this.error;
			subItem.Tag = "playWithMpv";
			item.Items.Add(subItem);
			// Linked file
			item = new MenuItem();
			item.Header = Lang.LINKED_FILE;
			context.Items.Add(item);
			// Copy
			subItem = new MenuItem();
			subItem.Header = Lang.COPY;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.IsEnabled = !this.error;
			subItem.Tag = "copy";
			item.Items.Add(subItem);
			// Copy path
			subItem = new MenuItem();
			subItem.Header = Lang.COPY_PATH;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.IsEnabled = !this.error;
			subItem.Tag = "copyLinkedPath";
			item.Items.Add(subItem);
			// Delete
			subItem = new MenuItem();
			subItem.Header = Lang.DELETE;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "deleteLinkedFile";
			subItem.IsEnabled = !this.error;
			item.Items.Add(subItem);
			// Relocate
			subItem = new MenuItem();
			subItem.Header = Lang.Header("relocate");
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "relocateEpisode";
			item.Items.Add(subItem);
			// Open folder
			subItem = new MenuItem();
			subItem.Header = Lang.OPEN_FOLDER;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.IsEnabled = !this.error;
			subItem.Tag = "openFolder";
			item.Items.Add(subItem);
			// Import subtitle
			subItem = new MenuItem();
			subItem.Header = Lang.IMPORT_SUBTITLE;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.IsEnabled = !this.error;
			subItem.Tag = "importSubtitle";
			item.Items.Add(subItem);
			// Cover
			item = new MenuItem();
			item.Header = Lang.COVER;
			context.Items.Add(item);
			// View full
			subItem = new MenuItem();
			subItem.Header = Lang.VIEW_FULL;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "viewFull";
			item.Items.Add(subItem);
			subItem.IsEnabled = coverExists;
			// Copy file
			subItem = new MenuItem();
			subItem.Header = Lang.COPY_FILE;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "copyFile";
			item.Items.Add(subItem);
			subItem.IsEnabled = coverExists;
			// Change
			subItem = new MenuItem();
			subItem.Header = Lang.CHANGE;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "change";
			item.Items.Add(subItem);
			// Remove
			subItem = new MenuItem();
			subItem.Header = Lang.REMOVE;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "removeCover";
			subItem.IsEnabled = !String.IsNullOrEmpty(cover);
			item.Items.Add(subItem);
			// Online cover search
			subItem = new MenuItem();
			subItem.Header = Lang.Content("search");
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "onlineCover";
			item.Items.Add(subItem);
			// Generate thumbnail
			subItem = new MenuItem();
			subItem.Header = Lang.Content("generate");
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.IsEnabled = !this.error;
			subItem.Tag = "generateThumbnail";
			item.Items.Add(subItem);
			// Mark as watched
			CheckBox checkItem = new CheckBox();
			checkItem.Content = Lang.MARK_AS_WATCHED;
			checkItem.IsChecked = this.watched;
			checkItem.Click += this.ContextMenu_MenuItem_Click;
			checkItem.Tag = "markAsWatched";
			context.Items.Add(checkItem);

			return context;
		}

		/// <summary>
		/// Context menu when multiple items are selected.
		/// </summary>
		/// <returns></returns>
		protected ContextMenu GetMultipleSeriesAndSeasonsContextMenu()
		{
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			this.AddCommonMultipleContextMenuitems(ref context);
            this.AddStatusSubContext(ref context);

			return context;
		}

		/// <summary>
		/// Context menu when multiple items are selected.
		/// </summary>
		/// <returns></returns>
		protected ContextMenu GetMultipleEpisodesContextMenu()
		{
			ContextMenu context = new ContextMenu();
			MenuItem item = new MenuItem();

			this.AddCommonMultipleContextMenuitems(ref context);

			// Mark all as watched
			item.Header = Lang.MARK_ALL_WATCHED;
			item.Tag = "markAllWatched";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			// Mark all as not watched
			item = new MenuItem();
			item.Header = Lang.MARK_ALL_NOT_WATCHED;
			item.Tag = "markAllNotWatched";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			return context;
		}

		protected BitmapSource LoadBitmap(Bitmap source)
		{
			return Imaging.CreateBitmapSourceFromHBitmap(source.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}

		/// <summary>
		/// Make the item appear iwith a fade-in effect.
		/// </summary>
		protected void FadeIn()
		{
			this.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(0.35)));
		}

		/// <summary>
		/// Check if the mouse cursor is over the element.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected bool CursorIsOver()
		{
			// Due to the timer, the element can sometime not yet be visible when
			// PointToScreen() is called, causing an InvalidOperationException.
			if (!this.IsVisible) {
				return false;
			}

			// Get tile and mouse positions
			System.Windows.Point elemUpperLeft = this.PointToScreen(new System.Windows.Point(0d, 0d));
			System.Windows.Point elemLowerRight = elemUpperLeft;

			elemLowerRight.X += this.ActualWidth;
			elemLowerRight.Y += this.ActualHeight;

			System.Drawing.Point mousePos = System.Windows.Forms.Cursor.Position;

			// Abort if the mouse cursor is out of the tile
			if (mousePos.X < elemUpperLeft.X || mousePos.X > elemLowerRight.X ||
				mousePos.Y < elemUpperLeft.Y || mousePos.Y > elemLowerRight.Y) {
				return false;
			}

			return true;
		}

		/// <summary>
		/// Get border color from resources or use a default value if null.
		/// </summary>
		protected string BorderColor
		{
			get
			{
				object resColor = Application.Current.Resources["Tile_BorderColor"];

				return resColor != null ? resColor.ToString() : "#3A78FF";
			}
		}

		#endregion Protected

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private void AddCommonMultipleContextMenuitems(ref ContextMenu context)
		{
			MenuItem item = new MenuItem();

			// Edit all selected items
			item = new MenuItem();
			item.Header = Lang.Text("editAllSelected");
			item.Tag = "editMutliple";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			// Delete all selected items
			item = new MenuItem();
			item.Header = Lang.Text("deleteAllSelected");
			item.Tag = "deleteMutliple";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			// Season and episode only
			if (Collection.ItemLevel != Constants.Level.Serie) {
				item = new MenuItem();
				item.Header = Lang.Header("moveAllSelected");
				item.Click += this.ContextMenu_MenuItem_Click;
				item.Tag = "moveItems";
				context.Items.Add(item);
			}

			// Change cover
			item = new MenuItem();
			item.Header = Lang.Text("changeMultipleCovers");
			item.Tag = "changeMultipleCovers";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			// Remove covers
			item = new MenuItem();
			item.Header = Lang.REMOVE_COVERS;
			item.Tag = "removeMultipleCovers";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);

			// Relocate (series and seasons only)
			item = new MenuItem();
			item.Header = Lang.Header("relocateEpisodes");
			item.Tag = "relocateEpisodes";
			item.Click += this.ContextMenu_MenuItem_Click;
			context.Items.Add(item);
		}

		/// <summary>
		/// Add the "Status" contextmenu item.
		/// </summary>
		private void AddStatusSubContext(ref ContextMenu context)
		{
			// Status
			MenuItem item = new MenuItem();
			item.Header = Lang.STATUS;
			context.Items.Add(item);
			// None
			MenuItem subItem = new MenuItem();
			subItem.Header = Lang.NONE;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "none";
			item.Items.Add(subItem);
			// To watch
			subItem = new MenuItem();
			subItem.Header = Lang.TO_WATCH;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "toWatch";
			item.Items.Add(subItem);
			// Current
			subItem = new MenuItem();
			subItem.Header = Lang.CURRENT;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "current";
			item.Items.Add(subItem);
			// Stand by
			subItem = new MenuItem();
			subItem.Header = Lang.STANDBY;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "standBy";
			item.Items.Add(subItem);
			// Finished
			subItem = new MenuItem();
			subItem.Header = Lang.FINISHED;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "finished";
			item.Items.Add(subItem);
			// Dropped
			subItem = new MenuItem();
			subItem.Header = Lang.DROPPED;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "dropped";
			item.Items.Add(subItem);
			// -----
			item.Items.Add(new Separator());
			// Perso
			subItem = new MenuItem();
			subItem.Header = Lang.PERSO;
			subItem.Click += this.ContextMenu_MenuItem_Click;
			subItem.Tag = "perso";
			item.Items.Add(subItem);
		}

		private bool CoverExists(string cover)
		{
			return File.Exists(cover.Replace(@"file:///", ""));
		}

		#endregion

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public ushort Index
		{
			get { return this.index; }
			set { this.index = value; }
		}

		public bool CanBeHovered
		{
			get { return this.canBeHovered; }
			set { this.canBeHovered = value; }
		}

		#endregion

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		private void ContextMenu_MenuItem_Click(object sender, RoutedEventArgs e)
		{
			MenuItem mi = sender as MenuItem;

			if (mi != null) {
				if (ContextRequested != null) {
					if (mi.Tag == null) {
						ContextRequested(mi.Header.ToString(), e); // Use the title
					} else {
						ContextRequested(mi.Tag.ToString(), e); // Use the tag, when title is already taken
					}
				}
			} else if (ContextRequested != null) {
				this.watched = !this.watched;
				ContextRequested("markAsWatched", e);
			}
		}

		#endregion Event
	}
}
