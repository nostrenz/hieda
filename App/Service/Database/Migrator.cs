using System;
using System.Collections.Generic;

namespace Hieda.Service.Database
{
	class Migrator
	{
		/*
		============================================
		Public
		============================================
		*/

		#region Public

		/// <summary>
		/// Do a migration.
		/// </summary>
		/// <param name="from">Current database file version</param>
		/// <param name="to">Program's database schema version</param>
		public ushort Migrate(ushort from)
		{
			switch (from) {
				case 5:
					this.From5To6();
				break;
				case 6:
					this.From6To7();
				break;
				case 7:
					this.From7To8();
				break;
				case 8:
					this.From8To9();
				break;
				case 9:
					this.From9To10();
				break;
				case 10:
					this.From10To11();
				break;
				case 11:
					this.From11To12();
				break;
				case 12:
					this.From12To13();
				break;
				case 13:
					this.From13To14();
				break;
				case 14:
					this.From14To15();
				break;
				case 15:
					this.From15To16();
				break;
				case 16:
					this.From16To17();
				break;
				case 17:
					this.From17To18();
				break;
			}

			ushort to = (ushort)(from + 1);

			// Update the db version
			App.db.Version = to;

			return to;
		}

		#endregion

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private void RenewTable(string name, Entity.IEntity entity)
		{
			Table table = new Table(name);

			string fields = table.GetFieldsList(entity, true);
			string create = table.GetExpression(entity);

			this.RenewTable(name, fields, create);
		}

		/// <summary>
		/// Save data from a table, drop it then recreate it with the original data.
		/// Used to update the table structure as ALTER TABLE is very limited in SQLite.
		/// </summary>
		private void RenewTable(string name, string fields, string create)
		{
			List<string> queries = new List<string>();
			queries.Add(String.Format("CREATE TABLE tmp_{0};", create));
			queries.Add(String.Format("INSERT INTO tmp_{0} SELECT {1} FROM {0};", name, fields));

			// To prevent errors du to a locked table or a changed schema, commit the changes by ending the
			// transaction, close the current connection and open en new one then finish with a new transaction.
			App.db.Transaction(queries);
			App.db.Restart();

			queries = new List<string>();
			queries.Add(String.Format("DROP TABLE IF EXISTS {0};", name));
			queries.Add(String.Format("ALTER TABLE tmp_{0} RENAME TO {0};", name));

			App.db.Transaction(queries);
			App.db.Restart();
		}

		private void From5To6()
		{
			// We now uses the user_version pragma instead of a table to store the db version
			App.db.DropTable("version");

			// Link series and genres
			App.db.CreateTable(@"`serie_genre` (
					`serie_id` INTEGER NOT NULL,
					`genre_id` INTEGER NOT NULL
				);
			");

			// Add genres to series
			Repository.Genre genreRepo = Repository.Genre.Instance;
			genreRepo.CreateTable(new Entity.Genre());
			App.db.AddColumn("serie", "genre_id", "INTEGER");

			// Add studio to seasons
			Repository.Studio studioRepo = Repository.Studio.Instance;
			studioRepo.CreateTable(new Entity.Studio());
			App.db.AddColumn("season", "studio_id", "INTEGER");

			// Add premiered season and year to seasons
			App.db.AddColumn("season", "seasonal", "VARCHAR(255)");
			App.db.AddColumn("season", "year", "INTEGER");
		}

		private void From6To7()
		{
			// We do not want whitespaces in the cover column, replace them with null
			string query = @"
				UPDATE serie SET cover = NULLIF(LTRIM(RTRIM(cover)), '');
				UPDATE season SET cover = NULLIF(LTRIM(RTRIM(cover)), '');
				UPDATE episode SET cover = NULLIF(LTRIM(RTRIM(cover)), '');
			";

			App.db.Execute(query);
		}

		private void From7To8()
		{
			string query = @"
				UPDATE {0} SET status_id =
				(
					CASE
						WHEN status = 'ToSee' AND (status_id IS NULL OR status_id = 0) THEN '-1'
						WHEN status = 'Current' AND (status_id IS NULL OR status_id = 0) THEN '-2'
						WHEN status = 'StandBy' AND (status_id IS NULL OR status_id = 0) THEN '-3'
						WHEN status = 'Finished' AND (status_id IS NULL OR status_id = 0) THEN '-4'
					END
				);
				UPDATE season SET status_id =
				(
					CASE
						WHEN status = 'ToSee' AND (status_id IS NULL OR status_id = 0) THEN '-1'
						WHEN status = 'Current' AND (status_id IS NULL OR status_id = 0) THEN '-2'
						WHEN status = 'StandBy' AND (status_id IS NULL OR status_id = 0) THEN '-3'
						WHEN status = 'Finished' AND (status_id IS NULL OR status_id = 0) THEN '-4'
					END
				);
			";

			// Update status_id values from status
			App.db.Execute(String.Format(query, "serie"));
			App.db.Execute(String.Format(query, "season"));

			// Renew tables as some columns are not used anymore.
			this.RenewTable("serie", new Entity.Serie());
			this.RenewTable("season", new Entity.Season());
			this.RenewTable("episode", new Entity.Episode());
		}

		/// <summary>
		/// Add the "type" column in the "season" table.
		/// </summary>
		private void From8To9()
		{
			App.db.AddColumn("season", "type", "INTEGER");
		}

		/// <summary>
		/// Change seasonal order.
		/// Before: 0 => unknown, 1 => spring, 2 => summer, 3 => fall, 4 => winter
		/// After : 0 => unknown, 1 => winter, 2 => spring, 3 => summer, 4 => fall
		/// </summary>
		private void From9To10()
		{
			App.db.Execute(@"
				UPDATE season
				SET seasonal =
					CASE
						WHEN seasonal = 4 THEN 1
						WHEN seasonal = 1 THEN 2
						WHEN seasonal = 2 THEN 3
						WHEN seasonal = 3 THEN 4
					END
				WHERE seasonal IS NOT NULL
			");
		}

		/// <summary>
		/// Add a new column "rpc_large_image" to the serie and season tables.
		/// </summary>
		private void From10To11()
		{
			App.db.AddColumn("serie", "rpc_large_image", "VARCHAR(255)");
			App.db.AddColumn("season", "rpc_large_image", "VARCHAR(255)");
		}

		/// <summary>
		/// Add the "url" column in the "episode" table.
		/// </summary>
		private void From11To12()
		{
			App.db.AddColumn("episode", "url", "VARCHAR(255)");
		}

		/// <summary>
		/// Add the "source" column in the "season" table.
		/// </summary>
		private void From12To13()
		{
			App.db.AddColumn("season", "source", "INTEGER");
		}

		/// <summary>
		/// Column "episodes_last_watched" delete from the season table.
		/// </summary>
		private void From13To14()
		{
			this.RenewTable("season", new Entity.Season());
		}

		/// <summary>
		/// Columns "file" and "url" removed from the "episode" table and replaced by the "uri" column.
		/// </summary>
		private void From14To15()
		{
			App.db.AddColumn("episode", "uri", "VARCHAR(255)");
			App.db.Execute(@"UPDATE episode SET uri = file;");
			this.RenewTable("episode", new Entity.Episode());
		}

		/// <summary>
		/// Add column "wide_episodes" in the "season" table.
		/// </summary>
		private void From15To16()
		{
			App.db.AddColumn("season", "wide_episodes", "INTEGER", "1");
		}

		/// <summary>
		/// Add column "grouping" in the "season" table.
		/// </summary>
		private void From16To17()
		{
			App.db.AddColumn("season", "grouping", "VARCHAR(255)");
		}

		/// <summary>
		/// Add the "month" and "day" columns in the "season" table.
		/// </summary>
		private void From17To18()
		{
			App.db.AddColumn("season", "month", "INTEGER");
			App.db.AddColumn("season", "day", "INTEGER");
		}

		#endregion
	}
}
