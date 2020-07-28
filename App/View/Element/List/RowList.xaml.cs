using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Hieda.Properties;

namespace Hieda.View.Element.List
{
	public partial class RowList : UserControl, IList
	{
		// Vertical margin between the rows
		const byte VERTICAL_MARGIN = 22;

		public event EventHandler OnLoaded;
		private int itemWidth = Constants.NARROW_TILE_WIDTH;
		private int itemHeight = Constants.TILE_HEIGHT;
		private float itemZoom = 1;
		private bool useWideTile = false;

		public RowList()
		{
			InitializeComponent();
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Returns index of first item in list with a title stating with the given string.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public int FocusItemStartingWith(string c)
		{
			IItem tile = this.Items.Find(delegate(IItem item) {
				return item.Title.ToLower().StartsWith(c.ToLower());
			});

			if (tile != null) {
				tile.Focus();

				return tile.Index;
			} else {
				return -1;
			}
		}

		/// <summary>
		/// Add a row to the list using an object.
		/// </summary>
		/// <param name="row"></param>
		public void Add(IItem item)
		{
			this.Add((Row)item);
		}

		/// <summary>
		/// Add a row to the list using an object.
		/// </summary>
		/// <param name="row"></param>
		public void Add(Row row)
		{
			row.CoverWidth = this.itemWidth;
			row.CoverHeight = this.ItemHeight;

			// Calculate its position
			this.PlaceItem(ref row);

			// Then display it
			this.grid_Content.Children.Add(row);
		}

		/// <summary>
		/// Add a serie row to the list.
		/// </summary>
		/// <param name="tile"></param>
		public IItem Add(Entity.Serie serie)
		{
			IItem row = this.CreateItemFromSerie(this.Count, serie);

			this.Add(row);

			return row;
		}

		/// <summary>
		/// Add a season row to the list.
		/// </summary>
		/// <param name="tile"></param>
		public IItem Add(Entity.Season season, bool insert=false)
		{
			IItem row = this.CreateItemFromSeason(this.Count, season);

			this.Add(row);

			return row;
		}

		/// <summary>
		/// Add an episode row to the list.
		/// </summary>
		/// <param name="tile"></param>
		public IItem Add(Entity.Episode episode)
		{
			IItem row = this.CreateItemFromEpisode(this.Count, episode);

			// Add the row in the list
			this.Add(row);

			return row;
		}

		/// <summary>
		/// Place all row in list.
		/// Used for resizes.
		/// </summary>
		/// <param name="containerWidth"></param>
		public void PlaceItems(int containerWidth)
		{
			for (int i = 0; i < this.Count; i++) {
				Row row = this.GetRow(i);

				row.Margin = new Thickness { Bottom = VERTICAL_MARGIN };
				row.Width = containerWidth;
			}
		}

		/// <summary>
		/// Place the row at the given index.
		/// </summary>
		/// <param name="index"></param>
		public void PlaceItem(ref Row row)
		{
			row.Width = this.grid_Content.Width;
			row.Margin = new Thickness { Bottom = VERTICAL_MARGIN };
		}

		/// <summary>
		/// Do not removel unused for rows but used in TileList.
		/// </summary>
		/// <param name="index"></param>
		public void RemoveAt(int index)
		{
		}

		/// <summary>
		/// Empty the list.
		/// </summary>
		public void Clear()
		{
			this.grid_Content.Children.Clear();
		}

		/// <summary>
		/// Get a list item at a certain index or null if it doesn't exists.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IItem GetItem(int index)
		{
			if (index < this.grid_Content.Children.Count) {
				return (IItem)this.grid_Content.Children[index];
			}

			return null;
		}

		/// <summary>
		/// This should be used instead of GetItem() when possible.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public UIElement GetElement(int index)
		{
			return this.grid_Content.Children[index];
		}

		/// <summary>
		/// Reorder rows in the list.
		/// </summary>
		/// <param name="containerWidth"></param>
		/// <param name="containerHeight"></param>
		public void ResizeUpdate()
		{
			if (this.IsLoaded) {
				this.PlaceItems((int)this.ActualWidth);
			}
		}

		/// <summary>
		/// Set the width for the tiles.
		/// </summary>
		/// <param name="width"></param>
		public void UseWideTile(bool use)
		{
			this.useWideTile = use;
			this.itemWidth = (use ? Constants.WIDE_TILE_WIDTH : Constants.NARROW_TILE_WIDTH);
			this.itemHeight = Constants.TILE_HEIGHT;
		}

		/// <summary>
		/// Increase of decrease the item sizes depending on the given value.
		/// </summary>
		/// <param name="value"></param>
		public void ZoomItems(float value)
		{
			float previousZoom = this.itemZoom;

			this.itemZoom += value;

			// Prevent from negative zoom
			if (this.itemZoom <= 0.3) {
				this.itemZoom = previousZoom;

				return;
			}

			int newWidth = (int)((this.useWideTile ? Constants.WIDE_TILE_WIDTH : Constants.NARROW_TILE_WIDTH) * this.itemZoom);
			int newHeight = (int)(Constants.TILE_HEIGHT * this.itemZoom);

			// Prevent from having tiles bigger than the window
			if (newWidth >= App.Current.MainWindow.ActualWidth || newHeight >= App.Current.MainWindow.ActualHeight) {
				// Return to the previous zoom value
				this.itemZoom -= value;

				return;
			}

			this.itemWidth = newWidth;
			this.itemHeight = newHeight;

			// Apply new sizes to all items
			foreach (Row row in this.Items) {
				row.CoverWidth = this.itemWidth;
				row.CoverHeight = this.itemHeight;
			}
		}

		/// <summary>
		/// Create a row item from a serie.
		/// </summary>
		/// <param name=""></param>
		/// <returns></returns>
		public IItem CreateItemFromSerie(ushort index, Entity.Serie serie)
		{
			Row row = new Row(index);

			row.Update(serie);

			return row;
		}

		/// <summary>
		/// Create a row item from a season.
		/// </summary>
		/// <param name=""></param>
		/// <returns></returns>
		public IItem CreateItemFromSeason(ushort index, Entity.Season season, bool placeLabel=false)
		{
			Row row = new Row(index);

			row.Update(season);

			// Create an empty context menu now to prevent the collection one to open instead when right clicking a tile.
			row.Image_Cover.ContextMenu = new ContextMenu();

			return row;
		}

		/// <summary>
		/// Create a row item from an episode.
		/// </summary>
		/// <param name=""></param>
		/// <returns></returns>
		public IItem CreateItemFromEpisode(ushort index, Entity.Episode episode)
		{
			Row row = new Row(index);

			row.Update(episode);

			row.CanBeHovered = true;
			row.CoverWidth = this.itemWidth;

			// Create an empty context menu now to prevent the collection one to open instead when right clicking a tile.
			row.Image_Cover.ContextMenu = new ContextMenu();

			return row;
		}

		#endregion

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private Row GetRow(int index)
		{
			return (Row)this.grid_Content.Children[index];
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public byte TilesPerLine
		{
			get { return 1; }
		}

		public ushort Count
		{
			get { return (ushort)this.grid_Content.Children.Count; }
		}

		/// <summary>
		/// This is quite inefficient and should only be used when no others options are available.
		/// </summary>
		public List<IItem> Items
		{
			get
			{
				List<IItem> items = new List<IItem>();

				foreach (UIElement element in this.grid_Content.Children) {
					items.Add((IItem)element);
				}

				return items;
			}
		}

		/// <summary>
		/// This should be used instead of Items when possible.
		/// </summary>
		public UIElementCollection Elements
		{
			get { return this.grid_Content.Children; }
		}

		/// <summary>
		/// Get the number of rows that can fit on the screen.
		/// Allow to know from which index we will need to load more rows we scrolling.
		///
		/// Returns 0, because we don't support that feature for RowList yet.
		/// </summary>
		public byte GetNumberOfItemsFittingInScreen
		{
			get { return 0; }
		}

		public byte TilesPerLines
		{
			get { return 1; }
		}

		public int ItemHeight
		{
			get { return this.itemHeight; }
		}

		public bool IsLabeled
		{
			get { return false; }
			set { }
		}

		public bool IsSeasonalView
		{
			get { return false; }
			set { }
		}

		#endregion

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Will raise the OnLoaded event when the list is fully loaded.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RowList_Loaded(object sender, RoutedEventArgs e)
		{
			if (OnLoaded != null) {
				OnLoaded(sender, e);
			}
		}

		#endregion
	}
}
