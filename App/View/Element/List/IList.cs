using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Hieda.View.Element.List
{
	/// <summary>
	/// Interface used for TileList and RowList view elements.
	/// Allow to call their functions from the collection regardless of the type.
	/// </summary>
	public interface IList
	{
		void PlaceItems(int startingIndex);
		void Add(IItem item);
		IItem Add(Entity.Serie serie);
		IItem Add(Entity.Season season, bool insert=false);
		IItem Add(Entity.Episode episode);
		void RemoveAt(int index);
		void Clear();
		int FocusItemStartingWith(string c);
		void ResizeUpdate();
		IItem GetItem(int index);
		UIElement GetElement(int index);
		void UseWideTile(bool use);
		void ZoomItems(float value);
		IItem CreateItemFromSerie(ushort index, Entity.Serie serie);
		IItem CreateItemFromSeason(ushort index, Entity.Season season, bool placeLabel=true);
		IItem CreateItemFromEpisode(ushort index, Entity.Episode episode);

		/*
		============================================
		Accessor
		============================================
		*/

		List<IItem> Items { get; }
		UIElementCollection Elements { get; }
		byte GetNumberOfItemsFittingInScreen { get; }
		byte TilesPerLines { get; }
		byte TilesPerLine { get; }
		ushort Count { get; }
		int ItemHeight { get; }
		bool IsLabeled { get; set; }
		bool IsSeasonalView { get; set; }

		/*
		============================================
		Event
		============================================
		*/

		event EventHandler OnLoaded;
	}
}
