using Tomers.WPF.Localization;

namespace Hieda
{
	public static class Lang
	{
		// General
		public static string REMOVE
			= "Remove";
		public static string EDIT
			= "Edit";
		public static string START
			= "Start";
		public static string ABORT
			= "Abort";
		public static string EXECUTE
			= "Execute";
		public static string NOTHING
			= "Nothing here";
		public static string SUCCESS
			= "Success!";
		public static string DELETE
			= "Delete";
		public static string COPY
			= "Copy";
		public static string SEARCH_SERIES
			= "Search series";
		public static string ENTER_QUERY
			= "Enter an SQL query here";
		public static string DELETE_FROM_DB
			= "Delete {0} from database?";
		public static string NO_ITEM_SELECTED_TITLE
			= "No item selected";
		public static string NO_ITEM_SELECTED_DESCR
			= "Please select an item in the list first.";
		public static string OPERATION_CONTINUE_NOTHING_TITLE
			= "Nothing to continue";
		public static string FILE_COPIED_TITLE
			= "File copied!";
		public static string FILE_COPIED_DESCR
			= "File '{0}' copied to clipboard.";
		public static string NO_EPISODES
			= "You don't have any episodes";
		public static string NEED_RESTART
			= "Certains changements nécessitent un redémarrage du programme pour être pleinement actif.";
		public static string SIDEBAR_SYNOPSIS
			= "Click here to set the synopsys... ({0} chars max)";
		public static string CANT_QUERY
			= "Can't execute this query";
		public static string CONTAIN
			= "Mode: contain";
		public static string STARTING_WITH
			= "Mode: starting with";
		public static string ALL
			= "All";
		public static string NONE
			= "None";
		public static string TO_WATCH
			= "To watch";
		public static string CURRENT
			= "Current";
		public static string STANDBY
			= "Stand by";
		public static string FINISHED
			= "Finished";
		public static string DROPPED
			= "Dropped";
		public static string WATCHED
			= "Watched";
		public static string NOTWATCHED
			= "Not watched";
		public static string PERSO
			= "Perso";
		public static string OWNED
			= "Owned";
		public static string TOTAL
			= "Total";
		public static string REMOVE_COVERS
			= "Remove covers";
		public static string RELOCATE_FOLDER
			= "Relocate episodes folder";
		public static string CHANGE
			= "Change";
		public static string MARK_AS_WATCHED
			= "Mark as watched";
		public static string COPY_FILE
			= "Copy file";
		public static string VIEW_FULL
			= "View full";
		public static string COVER
			= "Cover";
		public static string OPEN_FOLDER
			= "Open folder";
		public static string LINKED_FILE
			= "Linked file";
		public static string STATUS
			= "Status";
		public static string CONTINUE
			= "Continue";
		public static string GO_TO_NEXT_LEVEL
			= "Go to next level";
		public static string COPY_SERIE_TITLE
			= "Copy serie's title";
		public static string COPY_SEASON_TITLE
			= "Copy season's title";
		public static string NO_EPISODE_DESCR
			= "<NO_EPISODE_DESCR>";
		public static string OPERATION_FINISHED_TITLE
			= "Operation finished!";
		public static string MARK_ALL_WATCHED
			= "Mark as watched";
		public static string MARK_ALL_NOT_WATCHED
			= "Mark as not watched";
		public static string ACTIONS_FOR_ALL
			= "Actions for all";
		public static string EPISODES_ADDED
			= "{0} episode(s) added";
		public static string SERIE
			= "serie";
		public static string SEASON
			= "season";
		public static string EPISODE
			= "episode";
		public static string TITLE_COPIED
			= "{0} title copied!";
		public static string COPIED_TO_CLIPBOARD
			= "'{0}' copied to clipboard";
		public static string YES
			= "Yes";
		public static string NO
			= "No";
		public static string CONFIRM_DELETION
			= "Confirm deletion";
		public static string ACTION_CANCELED
			= "Action canceled";
		public static string BIG
			= "Big";
		public static string SMALL
			= "Small";
		public static string RESTART
			= "Restart";
		public static string RESTART_NOW
			= "Restart now";
		public static string CONTINUE_NO_RESTART
			= "Continue without restarting";
		public static string SELECT_ITEM_FIRST
			= "Select an item first";
		public static string RENAME
			= "Rename";
		public static string CHECK_FOR_UPDATE
			= "Check for update";
		public static string NO_NEW_VERSION
			= "No new version available";
		public static string VERSION_AVAILABLE
			= "Update to release {0}";
		public static string STARTING
			= "Starting...";
		public static string UNCONTIGUOUS_EPISODE_NUMBER
			= "Le dernier épisode vu était le {0} mais il n'y a pas d'épisodes {1}.\nIl vous manque peut être cet épisode. Voulez-vous vraiment lancer l'épisode {2}?";
		public static string NO_COVER_FOUND
			= "No covers found";
		public static string DOWNLOADING_COVER
			= "Downloading cover...";
		public static string SPRING
			= "Spring";
		public static string SUMMER
			= "Summer";
		public static string FALL
			= "Fall";
		public static string WINTER
			= "Winter";
		public static string UNKNOWN
			= "Unknown";
		public static string UPDATED
			= "Updated!";
		public static string ERROR
			= "Error";
		public static string WARNING
			= "Warning";
		public static string COPY_PATH
			= "Copy path";
		public static string COPIED
			= "Copied!";
		public static string IMPORT_SUBTITLE
			= "Import subtitle";
		public static string MISSING_TITLE
			= "Please give at least a title.";
		public static string CHAPTER
			= "chapter";

		/// <summary>
		/// Cause an error when loading the program.
		/// </summary>
		public static void Initialize()
		{
			// General
			Set(ref EDIT,
				Header("edit"));
			Set(ref REMOVE,
				Header("remove"));
			Set(ref START,
				Text("start"));
			Set(ref ABORT,
				Text("abort"));
			Set(ref NOTHING,
				Content("nothingHere"));
			Set(ref DELETE_FROM_DB,
				Text("deleteFromDb"));
			Set(ref NO_ITEM_SELECTED_TITLE,
				Text("noItemSelected"));
			Set(ref NO_ITEM_SELECTED_DESCR,
				Text("pleaseSelectItem"));
			Set(ref OPERATION_CONTINUE_NOTHING_TITLE,
				Text("nothingToContinue"));
			Set(ref FILE_COPIED_TITLE,
				Text("fileCopiedTitle"));
			Set(ref NO_EPISODES,
				Text("noEpisodes"));
			Set(ref SIDEBAR_SYNOPSIS,
				Text("sidebarSynopsis"));
			Set(ref EXECUTE,
				Text("execute"));
			Set(ref SUCCESS,
				Text("success"));
			Set(ref ALL,
				Text("all"));
			Set(ref TO_WATCH,
				Text("toWatch"));
			Set(ref CURRENT,
				Text("current"));
			Set(ref STANDBY,
				Text("standBy"));
			Set(ref FINISHED,
				Text("finished"));
			Set(ref DROPPED,
				Text("dropped"));
			Set(ref WATCHED,
				Header("watched"));
			Set(ref NOTWATCHED,
				Text("notWatched"));
			Set(ref PERSO,
				Header("perso"));
			Set(ref OWNED,
				Text("owned"));
			Set(ref TOTAL,
				Text("total"));
			Set(ref REMOVE_COVERS,
				Header("removeCovers"));
			Set(ref COPY_FILE,
				Header("copyFile"));
			Set(ref COPY,
				Header("copy"));
			Set(ref OPEN_FOLDER,
				Header("openFolder"));
			Set(ref MARK_AS_WATCHED,
				Header("markAsWatched"));
			Set(ref LINKED_FILE,
				Header("linkedFile"));
			Set(ref DELETE,
				Header("delete"));
			Set(ref COVER,
				Header("cover"));
			Set(ref CONTINUE,
				Header("continue"));
			Set(ref GO_TO_NEXT_LEVEL,
				Header("goToNextLevel"));
			Set(ref VIEW_FULL,
				Header("viewFull"));
			Set(ref CHANGE,
				Header("change"));
			Set(ref COPY_SERIE_TITLE,
				Header("copySerieTitle"));
			Set(ref COPY_SEASON_TITLE,
				Header("copySeasonTitle"));
			Set(ref SEARCH_SERIES,
				Text("searchSeries"));
			Set(ref ENTER_QUERY,
				Text("enterQueryHere"));
			Set(ref CONTAIN,
				Content("contain"));
			Set(ref STARTING_WITH,
				Content("startingWith"));
			Set(ref NONE,
				Text("none"));
			Set(ref FILE_COPIED_DESCR,
				Text("fileCopiedDescr"));
			Set(ref NO_EPISODE_DESCR,
				Text("noEpisodeDescr"));
			Set(ref OPERATION_FINISHED_TITLE,
				Text("operationFinished"));
			Set(ref MARK_ALL_WATCHED,
				Content("markAllWatched"));
			Set(ref MARK_ALL_NOT_WATCHED,
				Header("markAllNotWatched"));
			Set(ref ACTIONS_FOR_ALL,
				Header("actionsForAll"));
			Set(ref EPISODES_ADDED,
				Content("episodesAdded"));
			Set(ref SERIE,
				Content("serie"));
			Set(ref SEASON,
				Content("season"));
			Set(ref EPISODE,
				Content("episode"));
			Set(ref TITLE_COPIED,
				Content("titleCopied"));
			Set(ref COPIED_TO_CLIPBOARD,
				Content("copiedToClipboard"));
			Set(ref YES,
				Content("yes"));
			Set(ref NO,
				Content("no"));
			Set(ref CONFIRM_DELETION,
				Content("confirmDeletion"));
			Set(ref ACTION_CANCELED,
				Content("actionCanceled"));
			Set(ref BIG,
				Content("big"));
			Set(ref SMALL,
				Content("small"));
			Set(ref RESTART,
				Content("restart"));
			Set(ref RESTART_NOW,
				Content("restartNow"));
			Set(ref CONTINUE_NO_RESTART,
				Content("continueNoRestart"));
			Set(ref SELECT_ITEM_FIRST,
				Content("selectItemFirst"));
			Set(ref RENAME,
				Content("rename"));
			Set(ref NO_NEW_VERSION,
				Content("noNewVersion"));
			Set(ref VERSION_AVAILABLE,
				Content("versionAvailable"));
			Set(ref STARTING,
				Content("starting"));
			Set(ref UNCONTIGUOUS_EPISODE_NUMBER,
				Content("uncontiguousEpisodeNumber"));
			Set(ref CHECK_FOR_UPDATE,
				Header("checkForUpdate"));
			Set(ref NO_COVER_FOUND,
				Content("noCoversFound"));
			Set(ref DOWNLOADING_COVER,
				Content("downloadingCover"));
			Set(ref SPRING,
				Text("spring"));
			Set(ref SUMMER,
				Text("summer"));
			Set(ref FALL,
				Text("fall"));
			Set(ref WINTER,
				Text("winter"));
			Set(ref UNKNOWN,
				Text("unknown"));
			Set(ref UPDATED,
				Text("updated"));
			Set(ref ERROR,
				Text("error"));
			Set(ref WARNING,
				Text("warning"));
			Set(ref COPY_PATH,
				Text("copyPath"));
			Set(ref COPIED,
				Text("copied"));
			Set(ref IMPORT_SUBTITLE,
				Text("importSubtitle"));
			Set(ref MISSING_TITLE,
				Text("missingTitle"));
			Set(ref CHAPTER,
				Content("chapter"));
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		public static string Text(string id, string otherwise=null)
		{
			return Translate(id, "Text", otherwise);
		}

		public static string Content(string id, string otherwise=null)
		{
			return Translate(id, "Content", otherwise);
		}

		public static string Header(string id, string otherwise=null)
		{
			return Translate(id, "Header", otherwise);
		}

		public static string Plural(string id, string otherwise = null)
		{
			return Translate(id, "Plural", otherwise);
		}

		/// <summary>
		/// Uppercase the first char of a text.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string UpperFirst(string text)
		{
			return char.ToUpper(text[0]) + text.Substring(1);
		}

		/// <summary>
		/// Lowercase the first char of a text.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string LowerFirst(string text)
		{
			return char.ToLower(text[0]) + text.Substring(1);
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private static void Set(ref string label, string value)
		{
			if (value != null && value.Length > 0) {
				label = value;
			}
		}

		private static string Translate(string id, string context, string otherwise = null)
		{
			string value = LanguageDictionary.Current.Translate<string>(id, context);

			if (value == null || value.Length == 0) {
				return otherwise;
			}

			return value;
		}

		#endregion Private
	}
}
