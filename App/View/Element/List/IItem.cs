using System;
using System.Windows;
using System.Windows.Media;

namespace Hieda.View.Element.List
{
	public partial interface IItem
	{
		void Focus();
		void SetNoCover();
		void SetEpisodesValues(ushort viewed, ushort owned, ushort total);
		void OpenContext();
		void CreateSerieAndSeasonContextMenu();
		void CreateEpisodeContextMenu();
		void Select();
		void Unselect();
		void ToggleSelect();
		void CreateMultipleSeriesAndSeasonsContextMenu();
		void CreateMultipleEpisodesContextMenu();
		void Update(Entity.Serie serie);
		void Update(Entity.Season season);
		void Update(Entity.Episode episode);
		void SetCover(string imageFilePath);
		void SetCover(System.Drawing.Bitmap bitmap);

		/*
		============================================
		Accessor
		============================================
		*/

		Thickness Margin { get; set; }
		string Title { get; set; }
		ushort Index { get; set; }
		string Number { get; set; }
		bool IsFocused { get; }
		Entity.DefaultStatus Status { set; }
		string StringSmallStatus { set; }
		string StringBigStatus { set; }
		string EpisodesValues { get; }
		string FullCoverPath { get; }
		bool Watched { get; }
		string GetEpisodesValues { get; }
		bool MarkItemAsWatched { set; }
		bool IsBeingEdited { get; }
		bool IsSelected { get; }
		Constants.Type Type { set; }
		ImageSource CoverSource { get; }
		bool NoLinkedFile { set; }

		/*
		============================================
		Event
		============================================
		*/

		event EventHandler DoubleClick;
		event EventHandler PlayClick;
		event EventHandler Edited;
		event EventHandler Selected;
		event EventHandler ContextRequested;
		event EventHandler MiddleClick;
		event EventHandler ContextMenuOpeningRequested;
		event EventHandler Dropped;
	}
}
