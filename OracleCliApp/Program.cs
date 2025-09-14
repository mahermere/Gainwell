
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

class Program
{
	static void Main(string[] args)
	{
		// Setup configuration
		var config = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			.Build();

		// Setup logging
		var logFilePath = config["Logging:LogFilePath"] ?? "oraclecliapp.log";
		using var logStream = new StreamWriter(logFilePath, append: true);
		void Log(string message)
		{
			var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
			Console.WriteLine(logEntry);
			logStream.WriteLine(logEntry);
			logStream.Flush();
		}

		Log("Starting Oracle CLI App");

		// Read Oracle config
		var user = config["Oracle:User"];
		var password = config["Oracle:Password"];
		var dataSource = config["Oracle:DataSource"];

		if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(dataSource))
		{
			Log("ERROR: Oracle connection information is missing in appsettings.json.");
			return;
		}

		var connString = $"User Id={user};Password={password};Data Source={dataSource}";
		Log($"Attempting to connect to Oracle DB at '{dataSource}' as user '{user}'");

		try
		{
			using var conn = new OracleConnection(connString);
			conn.Open();
			Log("Successfully connected to Oracle database.");

			// Read CSV file path from config
			var csvFilePath = config["CsvFilePath"] ?? "import.csv";
			// Read expected columns from config
			var columnSection = config.GetSection("ColumnDefinitions");
			var expectedColumns = columnSection.Get<string[]>() ?? Array.Empty<string>();
			if (expectedColumns.Length == 0)
			{
				Log("ERROR: No column definitions found in configuration (ColumnDefinitions section).");
				return;
			}
			Log($"Configured columns: {string.Join(", ", expectedColumns)}");

			if (!File.Exists(csvFilePath))
			{
				Log($"ERROR: CSV file '{csvFilePath}' not found.");
			}
			else
			{
				Log($"Opening CSV file: {csvFilePath}");
				int rowCount = 0;
				using (var reader = new StreamReader(csvFilePath))
				{
					while (reader.ReadLine() != null)
						rowCount++;
				}
				Log($"CSV file opened. Total rows: {rowCount}");

				// Bulk load logic
				const int batchSize = 10000;
				string tableName = "ccBULK_DUR_QTR_LOAD";
				using (var reader = new StreamReader(csvFilePath))
				{
					string? headerLine = reader.ReadLine();
					if (headerLine == null)
					{
						Log("ERROR: CSV file is empty.");
					}
					else
					{
						var csvColumns = headerLine.Split(',');
						Log($"CSV columns detected: {string.Join(", ", csvColumns)}");
						// Validate CSV columns match config
						if (!csvColumns.SequenceEqual(expectedColumns, StringComparer.OrdinalIgnoreCase))
						{
							Log("ERROR: CSV header columns do not match ColumnDefinitions in config. Import aborted.");
							Log($"CSV header: {string.Join(", ", csvColumns)}");
							Log($"Config columns: {string.Join(", ", expectedColumns)}");
							return;
						}

						// Validate table columns match config
						var tableColumns = GetTableColumns(conn, tableName);
						if (!tableColumns.SequenceEqual(expectedColumns, StringComparer.OrdinalIgnoreCase))
						{
							Log($"ERROR: Table columns in '{tableName}' do not match ColumnDefinitions in config. Import aborted.");
							Log($"Table columns: {string.Join(", ", tableColumns)}");
							Log($"Config columns: {string.Join(", ", expectedColumns)}");
							return;
						}

						var rows = new List<string[]>();
						int totalImported = 0;
						string? line;
						while ((line = reader.ReadLine()) != null)
						{
							var values = line.Split(',');
							if (values.Length != expectedColumns.Length)
							{
								Log($"WARNING: Skipping row with column mismatch: {line}");
								continue;
							}
							rows.Add(values);
							if (rows.Count == batchSize)
							{
								BulkInsert(conn, tableName, expectedColumns, rows, Log);
								totalImported += rows.Count;
								Log($"Imported {totalImported} rows so far...");
								rows.Clear();
							}
						}
						if (rows.Count > 0)
						{
							BulkInsert(conn, tableName, expectedColumns, rows, Log);
							totalImported += rows.Count;
						}
						Log($"Bulk load complete. Total rows imported: {totalImported}");
					}
				}
	static void BulkInsert(OracleConnection conn, string tableName, string[] columns, List<string[]> rows, Action<string> Log)
	{
		try
		{
			string colList = string.Join(", ", columns);
			string paramList = string.Join(", ", columns.Select((c, i) => ":" + (i + 1)));
			string sql = $"INSERT INTO {tableName} ({colList}) VALUES ({paramList})";
			using var cmd = new OracleCommand(sql, conn);
			for (int i = 0; i < columns.Length; i++)
			{
				var paramValues = rows.Select(r => r[i]).ToArray();
				cmd.Parameters.Add($":{i + 1}", OracleDbType.Varchar2, paramValues, System.Data.ParameterDirection.Input);
			}
			cmd.ArrayBindCount = rows.Count;
			int inserted = cmd.ExecuteNonQuery();
			Log($"Batch inserted {inserted} rows into {tableName}.");
		}
		catch (Exception ex)
		{
			Log($"ERROR during bulk insert: {ex.Message}");
		}
	}
			}

			conn.Close();
			Log("Connection closed.");
		}
		catch (Exception ex)
		{
			Log($"ERROR: {ex.Message}");
		}
		Log("Oracle CLI App finished.");
	}
}
