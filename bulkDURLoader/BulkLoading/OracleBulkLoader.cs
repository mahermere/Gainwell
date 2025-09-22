using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using bulkDURLoader.Database;
using bulkDURLoader.Models;
using bulkDURLoader.Logging;
using Microsoft.Extensions.Configuration;

namespace bulkDURLoader.BulkLoading
{
    /// <summary>
    /// Oracle-specific bulk loader for DUR quarterly data
    /// Utilizes Oracle's array binding for high-performance bulk inserts
    /// </summary>
    public class OracleBulkLoader
    {
        private readonly DatabaseConfig _dbConfig;
        private readonly Logger _logger;
        private readonly int _batchSize;
        private readonly int _commandTimeout;

        public OracleBulkLoader(DatabaseConfig dbConfig, IConfiguration configuration)
        {
            _dbConfig = dbConfig;
            _logger = Logger.Instance;
            _batchSize = configuration.GetValue<int>("BulkLoadSettings:BatchSize", 1000);
            _commandTimeout = configuration.GetValue<int>("BulkLoadSettings:CommandTimeout", 300);
        }

        /// <summary>
        /// Bulk insert DUR quarterly load records using Oracle array binding
        /// </summary>
        /// <param name="records">Collection of DurQuarterlyLoad records to insert</param>
        /// <returns>Number of records successfully inserted</returns>
        public async Task<int> BulkInsertAsync(IEnumerable<DurQuarterlyLoad> records)
        {
            var recordList = records.ToList();
            if (!recordList.Any())
            {
                _logger.Warning("No records provided for bulk insert");
                return 0;
            }

            _logger.Info($"Starting bulk insert of {recordList.Count} records to ccBulk_DUR_QTR_LOAD");

            var totalInserted = 0;
            var batches = CreateBatches(recordList, _batchSize);

            foreach (var batch in batches)
            {
                try
                {
                    var batchResult = await BulkInsertBatchAsync(batch);
                    totalInserted += batchResult;
                    _logger.Info($"Successfully inserted batch of {batchResult} records. Total: {totalInserted}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to insert batch of {batch.Count} records", ex);
                    throw;
                }
            }

            _logger.Info($"Bulk insert completed. Total records inserted: {totalInserted}");
            return totalInserted;
        }

        /// <summary>
        /// Insert a single batch using Oracle array binding
        /// </summary>
        private async Task<int> BulkInsertBatchAsync(List<DurQuarterlyLoad> batch)
        {
            const string insertSql = @"
                INSERT INTO ccBulk_DUR_QTR_LOAD (
                    ID, MEMBER_ID, PRESCRIPTION_NUMBER, NDC, SERVICE_DATE,
                    PROVIDER_ID, PHARMACY_ID, DRUG_NAME, DRUG_STRENGTH, QUANTITY,
                    DAYS_SUPPLY, PAID_AMOUNT, DUR_ALERT_CODE, DUR_ALERT_DESCRIPTION,
                    QUARTER, YEAR, BATCH_ID, CREATED_DATE, UPDATED_DATE,
                    STATUS, ERROR_MESSAGE, ADDITIONAL_DATA
                ) VALUES (
                    :ID, :MEMBER_ID, :PRESCRIPTION_NUMBER, :NDC, :SERVICE_DATE,
                    :PROVIDER_ID, :PHARMACY_ID, :DRUG_NAME, :DRUG_STRENGTH, :QUANTITY,
                    :DAYS_SUPPLY, :PAID_AMOUNT, :DUR_ALERT_CODE, :DUR_ALERT_DESCRIPTION,
                    :QUARTER, :YEAR, :BATCH_ID, :CREATED_DATE, :UPDATED_DATE,
                    :STATUS, :ERROR_MESSAGE, :ADDITIONAL_DATA
                )";

            using var connection = await _dbConfig.GetConnectionAsync();
            using var command = new OracleCommand(insertSql, connection);
            command.CommandTimeout = _commandTimeout;
            command.ArrayBindCount = batch.Count;

            // Set up array binding parameters
            SetupArrayBindingParameters(command, batch);

            try
            {
                var result = await command.ExecuteNonQueryAsync();
                return result;
            }
            catch (OracleException oracleEx)
            {
                _logger.Error($"Oracle error during bulk insert: {oracleEx.Message}, Error Code: {oracleEx.Number}", oracleEx);
                throw;
            }
        }

        /// <summary>
        /// Setup Oracle array binding parameters for bulk insert
        /// </summary>
        private void SetupArrayBindingParameters(OracleCommand command, List<DurQuarterlyLoad> batch)
        {
            // Arrays for parameter values
            var ids = new long[batch.Count];
            var memberIds = new string[batch.Count];
            var prescriptionNumbers = new string[batch.Count];
            var ndcs = new string[batch.Count];
            var serviceDates = new DateTime?[batch.Count];
            var providerIds = new string[batch.Count];
            var pharmacyIds = new string[batch.Count];
            var drugNames = new string[batch.Count];
            var drugStrengths = new string[batch.Count];
            var quantities = new decimal?[batch.Count];
            var daysSupplies = new int?[batch.Count];
            var paidAmounts = new decimal?[batch.Count];
            var durAlertCodes = new string[batch.Count];
            var durAlertDescriptions = new string[batch.Count];
            var quarters = new string[batch.Count];
            var years = new int[batch.Count];
            var batchIds = new string[batch.Count];
            var createdDates = new DateTime[batch.Count];
            var updatedDates = new DateTime?[batch.Count];
            var statuses = new string[batch.Count];
            var errorMessages = new string[batch.Count];
            var additionalData = new string[batch.Count];

            // Fill arrays with data
            for (int i = 0; i < batch.Count; i++)
            {
                var record = batch[i];
                ids[i] = record.Id;
                memberIds[i] = record.MemberId;
                prescriptionNumbers[i] = record.PrescriptionNumber ?? string.Empty;
                ndcs[i] = record.Ndc ?? string.Empty;
                serviceDates[i] = record.ServiceDate;
                providerIds[i] = record.ProviderId ?? string.Empty;
                pharmacyIds[i] = record.PharmacyId ?? string.Empty;
                drugNames[i] = record.DrugName ?? string.Empty;
                drugStrengths[i] = record.DrugStrength ?? string.Empty;
                quantities[i] = record.Quantity;
                daysSupplies[i] = record.DaysSupply;
                paidAmounts[i] = record.PaidAmount;
                durAlertCodes[i] = record.DurAlertCode ?? string.Empty;
                durAlertDescriptions[i] = record.DurAlertDescription ?? string.Empty;
                quarters[i] = record.Quarter;
                years[i] = record.Year;
                batchIds[i] = record.BatchId ?? string.Empty;
                createdDates[i] = record.CreatedDate;
                updatedDates[i] = record.UpdatedDate;
                statuses[i] = record.Status;
                errorMessages[i] = record.ErrorMessage ?? string.Empty;
                additionalData[i] = record.AdditionalData ?? string.Empty;
            }

            // Add parameters with array binding
            command.Parameters.Add("ID", OracleDbType.Int64, ids, ParameterDirection.Input);
            command.Parameters.Add("MEMBER_ID", OracleDbType.Varchar2, memberIds, ParameterDirection.Input);
            command.Parameters.Add("PRESCRIPTION_NUMBER", OracleDbType.Varchar2, prescriptionNumbers, ParameterDirection.Input);
            command.Parameters.Add("NDC", OracleDbType.Varchar2, ndcs, ParameterDirection.Input);
            command.Parameters.Add("SERVICE_DATE", OracleDbType.Date, serviceDates, ParameterDirection.Input);
            command.Parameters.Add("PROVIDER_ID", OracleDbType.Varchar2, providerIds, ParameterDirection.Input);
            command.Parameters.Add("PHARMACY_ID", OracleDbType.Varchar2, pharmacyIds, ParameterDirection.Input);
            command.Parameters.Add("DRUG_NAME", OracleDbType.Varchar2, drugNames, ParameterDirection.Input);
            command.Parameters.Add("DRUG_STRENGTH", OracleDbType.Varchar2, drugStrengths, ParameterDirection.Input);
            command.Parameters.Add("QUANTITY", OracleDbType.Decimal, quantities, ParameterDirection.Input);
            command.Parameters.Add("DAYS_SUPPLY", OracleDbType.Int32, daysSupplies, ParameterDirection.Input);
            command.Parameters.Add("PAID_AMOUNT", OracleDbType.Decimal, paidAmounts, ParameterDirection.Input);
            command.Parameters.Add("DUR_ALERT_CODE", OracleDbType.Varchar2, durAlertCodes, ParameterDirection.Input);
            command.Parameters.Add("DUR_ALERT_DESCRIPTION", OracleDbType.Varchar2, durAlertDescriptions, ParameterDirection.Input);
            command.Parameters.Add("QUARTER", OracleDbType.Varchar2, quarters, ParameterDirection.Input);
            command.Parameters.Add("YEAR", OracleDbType.Int32, years, ParameterDirection.Input);
            command.Parameters.Add("BATCH_ID", OracleDbType.Varchar2, batchIds, ParameterDirection.Input);
            command.Parameters.Add("CREATED_DATE", OracleDbType.Date, createdDates, ParameterDirection.Input);
            command.Parameters.Add("UPDATED_DATE", OracleDbType.Date, updatedDates, ParameterDirection.Input);
            command.Parameters.Add("STATUS", OracleDbType.Varchar2, statuses, ParameterDirection.Input);
            command.Parameters.Add("ERROR_MESSAGE", OracleDbType.Varchar2, errorMessages, ParameterDirection.Input);
            command.Parameters.Add("ADDITIONAL_DATA", OracleDbType.Clob, additionalData, ParameterDirection.Input);
        }

        /// <summary>
        /// Create batches from the full record list
        /// </summary>
        private static IEnumerable<List<DurQuarterlyLoad>> CreateBatches(List<DurQuarterlyLoad> records, int batchSize)
        {
            for (int i = 0; i < records.Count; i += batchSize)
            {
                yield return records.Skip(i).Take(batchSize).ToList();
            }
        }

        /// <summary>
        /// Generate sample test data for bulk loading
        /// </summary>
        public static List<DurQuarterlyLoad> GenerateSampleData(int recordCount, string quarter, int year, string batchId)
        {
            var random = new Random();
            var sampleData = new List<DurQuarterlyLoad>();

            for (int i = 1; i <= recordCount; i++)
            {
                sampleData.Add(new DurQuarterlyLoad
                {
                    Id = i,
                    MemberId = $"MBR{i:D6}",
                    PrescriptionNumber = $"RX{random.Next(100000, 999999)}",
                    Ndc = $"{random.Next(10000, 99999):D5}-{random.Next(100, 999):D3}-{random.Next(10, 99):D2}",
                    ServiceDate = DateTime.Now.AddDays(-random.Next(1, 90)),
                    ProviderId = $"PRV{random.Next(1000, 9999)}",
                    PharmacyId = $"PHM{random.Next(100, 999)}",
                    DrugName = $"Drug{random.Next(1, 100)}",
                    DrugStrength = $"{random.Next(5, 100)}mg",
                    Quantity = random.Next(30, 90),
                    DaysSupply = random.Next(30, 90),
                    PaidAmount = (decimal)(random.NextDouble() * 500),
                    DurAlertCode = random.Next(1, 10) <= 2 ? $"D{random.Next(1, 5)}" : null,
                    DurAlertDescription = random.Next(1, 10) <= 2 ? "Drug interaction detected" : null,
                    Quarter = quarter,
                    Year = year,
                    BatchId = batchId,
                    CreatedDate = DateTime.UtcNow,
                    Status = "PENDING"
                });
            }

            return sampleData;
        }
    }
}