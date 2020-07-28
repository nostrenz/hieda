using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.ComponentModel;
using Hieda.Properties;

namespace Hieda.View.Element.List
{
	/// <summary>
	/// Interaction logic for LabelList.xaml
	/// </summary>
	public partial class TileList : UserControl, IList
	{
		// Tile top margin to prevent placing them over the labels
		public const ushort LABEL_TOP_MARGIN = 55;

		private double row = 0;
		private int addedTiles = 0;
		private int itemWidth = Constants.NARROW_TILE_WIDTH;
		private int itemHeight = Constants.TILE_HEIGHT;
		private float itemZoom = 1;
		private bool useWideTile = false;
		private byte tilesPerLines;
		private double leftMargin;
		private string previousLabel = "";
		private bool firstLabelAdded = false;
		private bool isLabeled;
		private bool isSeasonalView;
		private BackgroundWorker backgroundWorker = null;
		public event EventHandler OnLoaded;

		public TileList()
		{
			InitializeComponent();
		}

		/*
		============================================
		Public
		============================================
		*/

		/// <summary>
		/// Replace all tiles in list.
		/// Faster than using PlaceItem on each items.
		/// </summary>
		/// <param name="startingIndex">
		/// Idex from where tiles will be replaced (the ones before will not be moved)
		/// </param>
		public void PlaceItems(int startingIndex=0)
		{
			int itemCount = this.Count;

			if (itemCount == 0) {
				return;
			}

			// Place the first tile
			this.GetItem(0).Margin = new Thickness { Left = this.leftMargin };

			// There's only one tile, no need to continue
			if (itemCount == 1) {
				return;
			}

			int linesCounter = 0;
			byte tilesCounter = 1;

			for (int i = 1; i < itemCount; i++) {
				if (tilesCounter == this.tilesPerLines) {
					linesCounter++;
					tilesCounter = 0;
				}

				this.GetItem(i).Margin = new Thickness {
					Left = ((this.itemWidth + Constants.TILE_HORIZONTAL_MARGIN) * tilesCounter) + this.leftMargin,
					Top  = (this.itemHeight + Constants.TILE_VERTICAL_MARGIN) * linesCounter
				};

				tilesCounter++;
			}
		}

		/// <summary>
		/// FROM THE OLD TILELIST
		/// Place the tile at the given index.
		/// </summary>
		/// <param name="index"></param>
		public void PlaceItem(ref Tile tile)
		{
			int index = this.Count;
			int width = this.itemWidth + Constants.TILE_HORIZONTAL_MARGIN;

			// Tile is in the first line, no need for a more complex calculation
			if (index < this.tilesPerLines) {
				tile.Margin = new Thickness {
					Left = (width * index) + this.leftMargin
				};

				return;
			}

			// Tile is in another line

			//          (index / this.tilesPerLines) = row
			//          (this.tilesPerLines * row)   = Index of the first tile in line
			// (index - (this.tilesPerLines * row))  = column

			// Two ways of calculating the left position (performances are about the same but the second one is more simple)
			// Left = (width * (index - (this.tilesPerLines * row))) + this.leftMargin
			// Left = this.leftMargin + (width * (index % this.tilesPerLines))

			tile.Margin = new Thickness {
				Left = this.leftMargin + (width * (index % this.tilesPerLines)),
				Top  = (this.itemHeight + Constants.TILE_VERTICAL_MARGIN) * (index / this.tilesPerLines)
			};
		}

		/// <summary>
		/// Same as PlaceItem() but takes the labels into account.
		/// Place the tile at the given index.
		/// </summary>
		/// <param name="index"></param>
		public void PlaceItemWithLabels(ref Tile tile)
		{
			int width = this.itemWidth + Constants.TILE_HORIZONTAL_MARGIN;
			double column = this.addedTiles % this.tilesPerLines;

			// End of row reached, repel tiles to the next row
			if (this.addedTiles > 0 && column == 0) {
				this.row++;
			}

			tile.Margin = new Thickness {
				Left = leftMargin + (width * column),
				Top = (tile.Tag.ToString() != "" ? LABEL_TOP_MARGIN : 0) + (this.itemHeight + Constants.TILE_VERTICAL_MARGIN) * this.row
			};

			this.addedTiles++; // Count added tiles
		}

		/// <summary>
		/// Same as PlaceItems() but also places labels.
		/// </summary>
		/// <param name="startingIndex"></param>
		public void PlaceItemsWithLabels(int startingIndex=0)
		{
			int itemCount = this.Count;

			// There's only one tile, no need to continue
			if (itemCount <= 1) {
				return;
			}

			string label = "";
			int labelIndex = 0;
			this.row = 0;
			this.addedTiles = 0;
			this.firstLabelAdded = false;
			int width = this.itemWidth + Constants.TILE_HORIZONTAL_MARGIN;

			for (int i = 0; i < itemCount; i++) {
				string nextLabel = ((Tile)this.GetItem(i)).Tag.ToString();

				// Only place a label if different from the previous one
				if (nextLabel != label) {
					label = nextLabel;

					// Add space between the last and next row of tiles to place the label
					if (i != 0) {
						this.row += 1;

						// Not the first label
						if (labelIndex > 0) {
							this.row += 0.2;
						}

						this.addedTiles = 0;
					}

					Label labelObj = this.GetLabel(labelIndex);

					if (labelObj != null) {
						this.ApplyLabelMargin(labelObj);
					}

					labelIndex++;
				}

				// Place the tile
				double column = this.addedTiles % this.tilesPerLines;

				// End of row reached, repel tiles to the next row
				if (this.addedTiles > 0 && column == 0) {
					this.row++;
				}

				Tile tile = (Tile)this.GetItem(i);

				tile.Margin = new Thickness {
					Left = leftMargin + (width * column),
					Top  = (tile.Tag.ToString() != "" ? LABEL_TOP_MARGIN : 0) + (this.itemHeight + Constants.TILE_VERTICAL_MARGIN) * this.row
				};

				this.addedTiles++; // Count added tiles
			}
		}

		/// <summary>
		/// Add a tile to the list.
		/// </summary>
		/// <param name="tile"></param>
		public void Add(IItem item)
		{
			this.Add((Tile)item);
		}

		/// <summary>
		/// Add a tile to the list.
		/// </summary>
		/// <param name="tile"></param>
		public void Add(Tile tile)
		{
			tile.CoverWidth = this.itemWidth;
			tile.CoverHeight = this.ItemHeight;

			// Calculate its position
			if (!this.isLabeled) {
				this.PlaceItem(ref tile);
			} else {
				this.PlaceItemWithLabels(ref tile);
			}

			// Display it
			this.grid_Content.Children.Add(tile);
		}

		/// <summary>
		/// Not used as the LabelList won't display series for now.
		/// </summary>
		/// <param name="serie"></param>
		/// <returns></returns>
		public IItem Add(Entity.Serie serie)
		{
			IItem tile = this.CreateItemFromSerie(this.Count, serie);

			this.Add(tile);

			return tile;
		}

		/// <summary>
		/// Add a season tile to the list.
		/// </summary>
		/// <param name="tile"></param>
		public IItem Add(Entity.Season season, bool insert=false)
		{
			// As we use labels we want the tile to be added in the right category
			if (insert && this.isLabeled) {
				return this.InsertAfterTag(season);
			}

			// Without labels we can just add it at the end of the list.
			IItem tile = this.CreateItemFromSeason(this.Count, season, true);

			this.Add(tile);

			return tile;
		}

		/// <summary>
		/// Not used as the LabelList won't display episodes for now.
		/// </summary>
		/// <param name="episode"></param>
		/// <returns></returns>
		public IItem Add(Entity.Episode episode)
		{
			IItem tile = this.CreateItemFromEpisode(this.Count, episode);

			// Add the tile in the list
			this.Add(tile);

			return tile;
		}

		/// <summary>
		/// Remove a tile at the given index.
		/// </summary>
		/// <param name="index"></param>
		public void RemoveAt(int index)
		{
			this.grid_Content.Children.RemoveAt(index);

			// Possible optimization: replace only tiles whos index follow the removed one?
			this.PlaceItemsDependingOnMode(index);
		}

		/// <summary>
		/// Empty the list.
		/// </summary>
		public void Clear()
		{
			this.row = 0;
			this.addedTiles = 0;
			this.previousLabel = "";
			this.firstLabelAdded = false;

			this.grid_Label.Children.Clear();
			this.grid_Content.Children.Clear();
		}

		/// <summary>
		/// Focus the first item with a title starting with the given string.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public int FocusItemStartingWith(string c)
		{
			IItem tile = this.Items.Find(delegate (IItem item) {
				return item.Title.ToLower().StartsWith(c.ToLower());
			});

			if (tile == null) {
				return -1;
			}

				tile.Focus();

				return tile.Index;
			}

		/// <summary>
		/// Called by the Collection when the window is resized, used to replace the elements.
		/// </summary>
		public void ResizeUpdate()
		{
			this.tilesPerLines = this.TilesPerLines;
			this.leftMargin = this.LeftMargin;

			if (this.IsLoaded) {
				this.PlaceItemsDependingOnMode(0);
			}
		}

		/// <summary>
		/// Get one of the item by its index as a IItem.
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
		/// Like GetItem() but get one of the added labels instead of one of the tiles.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Label GetLabel(int index)
		{
			if (index < this.grid_Label.Children.Count) {
				return (Label)this.grid_Label.Children[index];
			}

			return null;
		}

		/// <summary>
		/// Get one of the item by its index as a UIElement.
		/// Should be used instead of GetItem() when possible.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public UIElement GetElement(int index)
		{
			return this.grid_Content.Children[index];
		}

		/// <summary>
		/// Only used for recalculating positioning values as we won't display episodes for now.
		/// </summary>
		/// <param name="width"></param>
		public void UseWideTile(bool use)
		{
			this.useWideTile = use;
			this.itemWidth = (use ? Constants.WIDE_TILE_WIDTH : Constants.NARROW_TILE_WIDTH);
			this.itemHeight = Constants.TILE_HEIGHT;

			// Recalculate those values since they depend on itemWidth
			this.tilesPerLines = this.TilesPerLines;
			this.leftMargin = this.LeftMargin;
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
			foreach (Tile tile in this.Items) {
				tile.CoverWidth = this.itemWidth;
				tile.CoverHeight = this.itemHeight;
			}

			// Recalculate those values since they depend on itemWidth
			this.tilesPerLines = this.TilesPerLines;
			this.leftMargin = this.LeftMargin;

			this.PlaceItemsDependingOnMode(0);
		}

		/// <summary>
		/// Create a tile item from a serie.
		/// </summary>
		/// <param name=""></param>
		/// <returns></returns>
		public IItem CreateItemFromSerie(ushort index, Entity.Serie serie)
		{
			Tile tile = new Tile(index);

			tile.Update(serie);

			return tile;
		}

		/// <summary>
		/// Create a tile item from a season.
		/// </summary>
		/// <param name=""></param>
		/// <returns></returns>
		public IItem CreateItemFromSeason(ushort index, Entity.Season season, bool placeLabel=true)
		{
			Tile tile = new Tile(index);

			// Add the label, only for the season view mode
			if (this.isLabeled) {
				string nextLabel = this.GetSeasonLabel(season);

				// Set the label as a tag
				tile.Tag = nextLabel;

				// Only place a label if different from the previous one
				if (placeLabel && nextLabel != this.previousLabel) {
					this.previousLabel = nextLabel;

					// Add space between the last and next row of tiles to place the label
					if (index != 0) {
						this.row += 1;

						if (this.firstLabelAdded) {
							this.row += 0.2;
						}
						
						this.addedTiles = 0;
					}

					this.AddLabel(this.previousLabel);
				}
			}

			// Add the tile
			tile.Update(season);

			// Create an empty context menu now to prevent the collection one to open instead when right clicking a tile.
			tile.Image_Cover.ContextMenu = new ContextMenu();

			return tile;
		}

		/// <summary>
		/// Create a tile item from an episode.
		/// </summary>
		/// <param name=""></param>
		/// <returns></returns>
		public IItem CreateItemFromEpisode(ushort index, Entity.Episode episode)
		{
			Tile tile = new Tile(index);

			tile.Update(episode);

			tile.CanBeHovered = true;
			tile.CoverWidth = this.itemWidth;

			// Create an empty context menu now to prevent the collection one to open instead when right clicking a tile.
			tile.Image_Cover.ContextMenu = new ContextMenu();

			return tile;
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Set a label's position.
		/// </summary>
		/// <param name="label"></param>
		private void ApplyLabelMargin(Label label)
		{
			label.Margin = new Thickness {
				Left = this.leftMargin,
				Top = (this.row * (this.itemHeight + Constants.TILE_VERTICAL_MARGIN))
			};
		}

		/// <summary>
		/// Add a new label to the grid_Label
		/// </summary>
		private void AddLabel(string content)
		{
			Label label = new Label();

			label.Content = content;
			label.FontSize = 30;

			this.ApplyLabelMargin(label);
			this.grid_Label.Children.Add(label);

			this.firstLabelAdded = true;
		}

		/// <summary>
		/// Initialize the BackgroundWorker used for loading items.
		/// </summary>
		/// <param name="count"></param>
		private void InitBackGroundWorker(int count)
		{
			this.backgroundWorker = new BackgroundWorker();

			this.backgroundWorker.WorkerReportsProgress = true;
			this.backgroundWorker.WorkerSupportsCancellation = true;

			this.backgroundWorker.DoWork += delegate (object sender, DoWorkEventArgs e) {
				BackgroundWorker worker = sender as BackgroundWorker;

				for (int i = 0; i < count; i++) {
					if (worker.CancellationPending == true) {
						e.Cancel = true;
						break;
					} else {
						Thread.Sleep(Settings.Default.LoadSpeed);
						worker.ReportProgress(i);
					}
				}
			};
		}

		/// <summary>
		/// Stop the BackgroundWorker, must be done before starting a new one.
		/// </summary>
		private void StopBackGroundWorker()
		{
			if (this.backgroundWorker != null) {
				this.backgroundWorker.CancelAsync();
				this.backgroundWorker.Dispose();
			}
		}

		/// <summary>
		/// Use the PlaceItems function corresponding to the current view mode.
		/// </summary>
		/// <param name="index"></param>
		private void PlaceItemsDependingOnMode(int index)
		{
			if (!isLabeled) {
				this.PlaceItems(index);
			} else {
				this.PlaceItemsWithLabels(index);
			}
		}

		/// <summary>
		/// Insert a season tile under the label corresponding to its type.
		/// </summary>
		/// <param name="season"></param>
		/// <returns></returns>
		private IItem InsertAfterTag(Entity.Season season)
		{
			string seasonLabel = this.GetSeasonLabel(season);

			int index = 0;
			bool found = false;

			// Seach the label in the list
			foreach (UIElement element in this.grid_Content.Children) {
				Tile tileElement = (Tile)element;

				if (tileElement.Tag.ToString() == seasonLabel) {
					found = true;
				} else if (found) {
					break; // Index found
				}

				index++;
			}

			// Important: we should only set the last agrument of CreateItemFromSeason() to false if index is inferior to this.grid_Content.Children.Count.
			bool shouldPlaceLabel = false;

			if (index >= this.grid_Content.Children.Count) {
				shouldPlaceLabel = true;
			}

			Tile tile = (Tile)this.CreateItemFromSeason(this.Count, season, shouldPlaceLabel);

			tile.CoverWidth = this.itemWidth;
			tile.CoverHeight = this.ItemHeight;

			// Display it
			this.grid_Content.Children.Insert(index, tile);

			// Calculate its position
			if (!this.isLabeled) {
				this.PlaceItems(index);
			} else {
				this.PlaceItemsWithLabels(index);
			}

			return tile;
		}

		/// <summary>
		/// Get the label to be displayed for a season.
		/// </summary>
		/// <param name="season"></param>
		/// <returns></returns>
		private string GetSeasonLabel(Entity.Season season)
		{
			if (this.isSeasonalView) {
				return season.Premiered;
			}

			return (season.Grouping != null ? season.Grouping : "");
		}

		/*
		============================================
		Accessor
		============================================
		*/

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
		/// Get thye number of tiles that can fit on the screen.
		/// Allow to know from which index we will need to load more tiles we scrolling.
		/// </summary>
		public byte GetNumberOfItemsFittingInScreen
		{
			get
			{
				return (byte)(this.TilesPerLines * Math.Round(this.ActualHeight / (this.itemHeight + Constants.TILE_VERTICAL_MARGIN)));
			}
		}

		/// <summary>
		/// Count the items in the list.
		/// </summary>
		public ushort Count
		{
			get { return (ushort)this.grid_Content.Children.Count; }
		}

		/// <summary>
		/// SAME FUNCTION AS IN TileList.xaml.cs
		/// </summary>
		public byte TilesPerLine
		{
			get { return this.tilesPerLines; }
		}

		/// <summary>
		/// Calculate the number of tiles per lines.
		/// </summary>
		public byte TilesPerLines
		{
			get { return (byte)Math.Floor(this.ActualWidth / (this.itemWidth + Constants.TILE_HORIZONTAL_MARGIN)); }
		}

		/// <summary>
		/// Calculate left margin for tile centering.
		/// </summary>
		/// <param name="tilesPerLines"></param>
		/// <returns></returns>
		private double LeftMargin
		{
			get { return (this.ActualWidth - ((this.itemWidth * this.tilesPerLines) + (Constants.TILE_HORIZONTAL_MARGIN * (this.tilesPerLines - 1)))) / 2; }
		}

		public int ItemHeight
		{
			get { return this.itemHeight; }
		}

		public bool IsLabeled
		{
			get { return this.isLabeled; }
			set { this.isLabeled = value; }
		}

		public bool IsSeasonalView
		{
			get { return this.isSeasonalView; }
			set { this.isSeasonalView = value; }
		}

		/*
		============================================
		Event
		============================================
		*/

		/// <summary>
		/// Once loaded retrieve the seasons and display them in the list.
		/// Will raise the OnLoaded event when the list is fully loaded.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void LabelList_Loaded(object sender, RoutedEventArgs e)
		{
			if (OnLoaded != null) {
				OnLoaded(sender, e);
			}
		}
	}
}
