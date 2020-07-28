using System;
using System.Windows;
using System.Windows.Controls;

namespace Hieda.View.Window
{
	public partial class Query : System.Windows.Window
	{
		// True when we click on the Execute button.
		private bool greenlight = false;

		// From the view level we're in.
		private string table;
		private int serieId;
		private int seasonId;

		private string[] presets = {
			"UPDATE <TABLE> SET watched = '0' WHERE season_id = <SEASON_ID>",
			"INSERT INTO episode VALUES (null, <SERIE_ID>, <SEASON_ID>, 'Title', null, 0, null, 0);"
		};

		public Query(System.Windows.Window owner, string table, int serieId = 0, int seasonId = 0)
		{
			InitializeComponent();

			this.textbox_Query.Text = this.textbox_Query.Placeholder = Lang.ENTER_QUERY;

			this.Owner = owner;
			this.table = table;
			this.serieId = serieId;
			this.seasonId = seasonId;

			foreach (string exp in this.presets) {
				this.combo_Presets.Items.Add(exp);
			}
		}

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private string InsertValuesInPreset(string exp)
		{
			if (!String.IsNullOrWhiteSpace(this.table)) {
				exp = exp.Replace("<TABLE>", this.table);
			}

			if (this.serieId > 0) {
				exp = exp.Replace("<SERIE_ID>", this.serieId.ToString());
			}

			if (this.seasonId > 0) {
				exp = exp.Replace("<SEASON_ID>", this.seasonId.ToString());
			}

			return exp;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public bool MustDoBackup
		{
			get { return (bool)this.checkbox_Backup.IsChecked; }
		}

		public bool Greenlight
		{
			get { return this.greenlight; }
		}

		public string SqlQuery
		{
			get { return this.textbox_Query.ActualText; }
		}

		#endregion Accessor

		/*
		============================================
		Event
		============================================
		*/

		#region Event

		/// <summary>
		/// Called by selecting a different item in the Presets combobox.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_Presets_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.textbox_Query.Text = this.InsertValuesInPreset(this.combo_Presets.SelectedValue.ToString());
		}

		/// <summary>
		/// Called by clicking on the Execute button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Execute_Click(object sender, RoutedEventArgs e)
		{
			this.greenlight = true;

			this.Close();
		}

		#endregion Event
	}
}
