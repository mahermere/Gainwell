using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using bulkDURLoader.Models;
using bulkDURLoader.Logging;
using Microsoft.Extensions.Configuration;

namespace bulkDURLoader.DataLoading
{
    /// <summary>
    /// CSV reader for DUR quarterly load data with flexible header mapping
    /// </summary>
    public class DurCsvReader
    {
        private readonly Logger _logger;
        private readonly CsvConfiguration _csvConfig;
        private readonly string _encoding;
        private readonly bool _validateData;

        public DurCsvReader(IConfiguration configuration)
        {
            _logger = Logger.Instance;
            _encoding = configuration.GetValue<string>("CsvSettings:Encoding", "UTF-8");
            _validateData = configuration.GetValue<bool>("CsvSettings:ValidateData", true);

            // Configure CSV parsing options
            _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = configuration.GetValue<string>("CsvSettings:Delimiter", ","),
                HasHeaderRecord = configuration.GetValue<bool>("CsvSettings:HasHeader", true),
                HeaderValidated = null, // Don't throw on missing headers
                MissingFieldFound = null, // Don't throw on missing fields
                IgnoreBlankLines = configuration.GetValue<bool>("CsvSettings:IgnoreBlankLines", true),
                TrimOptions = TrimOptions.Trim,
                BadDataFound = (context) =>
                {
                    _logger.Warning($"Bad data found in CSV: {context.RawRecord}");
                }
            };
        }

        /// <summary>
        /// Read DUR records from a CSV file
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <returns>List of DurQuarterlyLoad records</returns>
        public async Task<List<DurQuarterlyLoad>> ReadCsvFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found: {filePath}");
            }

            _logger.Info($"Starting to read CSV file: {filePath}");

            var records = new List<DurQuarterlyLoad>();
            var errorCount = 0;
            var lineNumber = 0;

            try
            {
                using var reader = new StreamReader(filePath, System.Text.Encoding.GetEncoding(_encoding));
                using var csv = new CsvReader(reader, _csvConfig);

                // Register the class map for flexible header mapping
                csv.Context.RegisterClassMap<DurQuarterlyLoadMap>();

                await foreach (var record in csv.GetRecordsAsync<DurQuarterlyLoad>())
                {
                    lineNumber++;
                    try
                    {
                        // Set default values if not provided
                        if (record.Id == 0)
                        {
                            record.Id = lineNumber;
                        }

                        if (record.CreatedDate == default)
                        {
                            record.CreatedDate = DateTime.UtcNow;
                        }

                        if (string.IsNullOrEmpty(record.Status))
                        {
                            record.Status = "PENDING";
                        }

                        // Validate required fields if validation is enabled
                        if (_validateData && !ValidateRecord(record, lineNumber))
                        {
                            errorCount++;
                            continue;
                        }

                        records.Add(record);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.Error($"Error processing CSV record at line {lineNumber}", ex);
                    }
                }

                _logger.Info($"CSV file read completed. Records: {records.Count}, Errors: {errorCount}, Total lines processed: {lineNumber}");

                if (errorCount > 0)
                {
                    _logger.Warning($"CSV file contained {errorCount} records with errors that were skipped");
                }

                return records;
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to read CSV file: {filePath}", ex);
                throw;
            }
        }

        /// <summary>
        /// Validate a DUR record for required fields and data integrity
        /// </summary>
        private bool ValidateRecord(DurQuarterlyLoad record, int lineNumber)
        {
            var errors = new List<string>();

            // Check required fields
            if (string.IsNullOrWhiteSpace(record.MemberId))
            {
                errors.Add("MemberId is required");
            }

            if (string.IsNullOrWhiteSpace(record.Quarter))
            {
                errors.Add("Quarter is required");
            }
            else if (!new[] { "Q1", "Q2", "Q3", "Q4" }.Contains(record.Quarter))
            {
                errors.Add($"Quarter must be Q1, Q2, Q3, or Q4, but was '{record.Quarter}'");
            }

            if (record.Year < 2020 || record.Year > 2099)
            {
                errors.Add($"Year must be between 2020 and 2099, but was {record.Year}");
            }

            // Check data ranges
            if (record.Quantity.HasValue && record.Quantity.Value < 0)
            {
                errors.Add("Quantity cannot be negative");
            }

            if (record.DaysSupply.HasValue && record.DaysSupply.Value < 0)
            {
                errors.Add("DaysSupply cannot be negative");
            }

            if (record.PaidAmount.HasValue && record.PaidAmount.Value < 0)
            {
                errors.Add("PaidAmount cannot be negative");
            }

            // Check string lengths
            if (record.MemberId?.Length > 50)
            {
                errors.Add("MemberId exceeds maximum length of 50 characters");
            }

            if (record.Ndc?.Length > 11)
            {
                errors.Add("NDC exceeds maximum length of 11 characters");
            }

            if (errors.Any())
            {
                _logger.Warning($"Validation failed for record at line {lineNumber}: {string.Join(", ", errors)}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get file information and preview first few records
        /// </summary>
        public async Task<CsvFileInfo> GetFileInfoAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CSV file not found: {filePath}");
            }

            var fileInfo = new FileInfo(filePath);
            var info = new CsvFileInfo
            {
                FilePath = filePath,
                FileSize = fileInfo.Length,
                LastModified = fileInfo.LastWriteTime
            };

            try
            {
                using var reader = new StreamReader(filePath, System.Text.Encoding.GetEncoding(_encoding));
                using var csv = new CsvReader(reader, _csvConfig);

                // Read header
                await csv.ReadAsync();
                csv.ReadHeader();
                info.Headers = csv.HeaderRecord?.ToList() ?? new List<string>();

                // Count total lines
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                var lineCount = 0;
                while (await reader.ReadLineAsync() != null)
                {
                    lineCount++;
                }
                info.TotalLines = lineCount;
                info.EstimatedRecords = lineCount - (info.Headers.Any() ? 1 : 0); // Subtract header line

                _logger.Info($"CSV file info - Size: {info.FileSize} bytes, Lines: {info.TotalLines}, Estimated records: {info.EstimatedRecords}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to analyze CSV file: {filePath}", ex);
                throw;
            }

            return info;
        }
    }

    /// <summary>
    /// CSV class map for flexible header mapping to DurQuarterlyLoad properties
    /// </summary>
    public sealed class DurQuarterlyLoadMap : ClassMap<DurQuarterlyLoad>
    {
        public DurQuarterlyLoadMap()
        {
            // Map with multiple possible header names for flexibility
            Map(m => m.Id).Name("Id", "ID", "RecordId", "Record_Id");
            Map(m => m.MemberId).Name("MemberId", "Member_Id", "MemberID", "Member ID");
            Map(m => m.PrescriptionNumber).Name("PrescriptionNumber", "Prescription_Number", "PrescriptionID", "Prescription ID", "RxNumber");
            Map(m => m.Ndc).Name("Ndc", "NDC", "NationalDrugCode", "National_Drug_Code");
            Map(m => m.ServiceDate).Name("ServiceDate", "Service_Date", "DateOfService", "Date_Of_Service");
            Map(m => m.ProviderId).Name("ProviderId", "Provider_Id", "ProviderID", "Provider ID", "PrescriberID");
            Map(m => m.PharmacyId).Name("PharmacyId", "Pharmacy_Id", "PharmacyID", "Pharmacy ID");
            Map(m => m.DrugName).Name("DrugName", "Drug_Name", "DrugBrandName", "Drug Brand Name", "ProductName");
            Map(m => m.DrugStrength).Name("DrugStrength", "Drug_Strength", "Strength", "Dosage");
            Map(m => m.Quantity).Name("Quantity", "QtyDispensed", "Qty_Dispensed", "QuantityDispensed");
            Map(m => m.DaysSupply).Name("DaysSupply", "Days_Supply", "DaySupply", "Day_Supply");
            Map(m => m.PaidAmount).Name("PaidAmount", "Paid_Amount", "AmountPaid", "Amount_Paid", "TotalPaid");
            Map(m => m.DurAlertCode).Name("DurAlertCode", "DUR_Alert_Code", "AlertCode", "Alert_Code");
            Map(m => m.DurAlertDescription).Name("DurAlertDescription", "DUR_Alert_Description", "AlertDescription", "Alert_Description");
            Map(m => m.Quarter).Name("Quarter", "Qtr", "Q");
            Map(m => m.Year).Name("Year", "Yr", "LoadYear", "Load_Year");
            Map(m => m.BatchId).Name("BatchId", "Batch_Id", "BatchID", "Batch ID", "LoadBatch");
            Map(m => m.CreatedDate).Name("CreatedDate", "Created_Date", "CreateDate", "Create_Date").Optional();
            Map(m => m.UpdatedDate).Name("UpdatedDate", "Updated_Date", "UpdateDate", "Update_Date").Optional();
            Map(m => m.Status).Name("Status", "RecordStatus", "Record_Status").Optional();
            Map(m => m.ErrorMessage).Name("ErrorMessage", "Error_Message", "Error", "ErrorText").Optional();
            Map(m => m.AdditionalData).Name("AdditionalData", "Additional_Data", "ExtraData", "Extra_Data", "Notes").Optional();
        }
    }

    /// <summary>
    /// Information about a CSV file
    /// </summary>
    public class CsvFileInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public List<string> Headers { get; set; } = new();
        public int TotalLines { get; set; }
        public int EstimatedRecords { get; set; }
    }
}