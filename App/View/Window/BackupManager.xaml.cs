using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using File = Hieda.Tool.File;
using Path = Hieda.Tool.Path;

namespace Hieda.View.Window
{
	public partial class BackupManager : System.Windows.Window
	{
		private List<File> files = new List<File>();
		private bool needCollectionRefresh = false;

		public BackupManager(System.Windows.Window owner)
		{
			InitializeComponent();

			this.Owner = owner;
			this.button_Delete.IsEnabled = this.button_Rename.IsEnabled = this.button_Restore.IsEnabled = false;

			this.GetFileList();
			this.ShowDialog();
		}

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Get the files, store them in the list and display them in the view.
		/// </summary>
		private void GetFileList()
		{
			string path = Path.DbBackupFolder;

			if (!System.IO.Directory.Exists(path)) {
				System.IO.Directory.CreateDirectory(path);
			}

			string[] files = Directory.GetFiles(path);

			foreach (string file in files) {
				this.files.Add(new File(file));
				this.FilesList.Items.Add(Path.LastSegment(this.files[this.files.Count - 1].Path).Replace("_.db", "").Replace("hiedb_", ""));
			}
		}

		/// <summary>
		/// Delete all the selecteds copies.
		/// </summary>
		public void Delete()
		{
			foreach (var item in this.FilesList.SelectedItems) {
				this.files[this.FilesList.Items.IndexOf(item)].Delete();
			}

			this.Refresh();
		}

		/// <summary>
		/// Rename the selected copy.
		/// </summary>
		/// <param name="newName"></param>
		public void Rename(string newName)
		{
			if (String.IsNullOrWhiteSpace(newName)) {
				return;
			}

			if (!System.IO.File.Exists(Path.DatabaseFolder + @"\backups\" + "hiedb_" + newName + "_.db")) {
				this.files[this.FilesList.SelectedIndex].Rename("hiedb_" + newName + "_.db");
				this.Refresh();
			} else {
				MessageBox.Show("Error: a file with this name already exists!", "Error");
			}
		}

		/// <summary>
		/// Restore the selected copy.
		/// </summary>
		public void Restore()
		{
			File selected = this.files[this.FilesList.SelectedIndex];
			
			if (!selected.Exists) {
				return;
			}

			App.db.Disconnect();

			try {
				System.IO.File.Delete(Path.DatabaseFolder + @"\hiedb.db");
				selected.CopyRenamed(Path.DatabaseFolder + @"\", "hiedb");

				MessageBox.Show(Lang.Text("dbRestoreSuccess"), Lang.SUCCESS);

				this.needCollectionRefresh = true;
			} catch {
				MessageBox.Show(Lang.Text("dbRestoreFailure"), Lang.ERROR);
			}

			App.db.Connect();
		}

		#endregion

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Reload the file list.
		/// </summary>
		private void Refresh()
		{
			this.FilesList.Items.Clear();
			this.files.Clear();

			this.GetFileList();
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor
		
		/// <summary>
		/// Get the list of database copies.
		/// </summary>
		public ListView FilesList
		{
			get { return this.filesList; }
		}

		/// <summary>
		/// Check if collection's items needs to be reloaded after closing this window.
		/// </summary>
		public bool NeedCollectionRefresh
		{
			get { return this.needCollectionRefresh; }
		}

		#endregion

		/*
		============================================
		Event
		============================================
		*/

		#region Event
		
		/// <summary>
		/// Caled when clicking on the Delete button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Delete_Click(object sender, RoutedEventArgs e)
		{
			if (this.filesList.SelectedIndex != -1) {
				this.Delete();
			} else {
				MessageBox.Show(Lang.SELECT_ITEM_FIRST);
			}
		}

		/// <summary>
		/// Called when clicking on the Restore button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Restore_Click(object sender, RoutedEventArgs e)
		{
			if (this.filesList.SelectedIndex != -1) {
				this.Restore();
			} else {
				MessageBox.Show(Lang.SELECT_ITEM_FIRST);
			}
		}

		/// <summary>
		/// Called when clicking on the Rename button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Rename_Click(object sender, RoutedEventArgs e)
		{
			if (this.filesList.SelectedIndex != -1) {
				InputDialog dialog = new InputDialog(this);
				dialog.Title = dialog.Caption = Lang.RENAME;
				dialog.Text = dialog.Placeholder = this.filesList.SelectedValue.ToString();

				this.Rename(dialog.Open());
			} else {
				MessageBox.Show(Lang.SELECT_ITEM_FIRST);
			}
		}

		/// <summary>
		/// Called when changing the selected entry in the list.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.button_Delete.IsEnabled = this.button_Rename.IsEnabled = this.button_Restore.IsEnabled = (this.filesList.SelectedIndex != -1);
		}

		#endregion
	}
}
