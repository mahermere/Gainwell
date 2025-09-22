# CSV File Loading Guide - bulkDURLoader

## Overview

The `bulkDURLoader` application now supports loading DUR (Drug Utilization Review) data directly from CSV files with flexible header mapping. This feature allows you to process real-world DUR data files and bulk load them into the Oracle `ccBulk_DUR_QTR_LOAD` table.

## Features

### 🔄 **Flexible CSV Processing**
- **Header Mapping**: Automatic mapping of various header formats to data model properties
- **Data Validation**: Comprehensive validation of required fields and data types
- **Error Handling**: Graceful handling of malformed data with detailed error reporting
- **Encoding Support**: Configurable character encoding (UTF-8, UTF-16, etc.)

### 📋 **Supported Header Formats**

The CSV reader supports multiple header naming conventions for maximum flexibility:

| Data Field | Supported Header Names |
|------------|----------------------|
| Member ID | `MemberId`, `Member_Id`, `MemberID`, `Member ID` |
| Prescription Number | `PrescriptionNumber`, `Prescription_Number`, `RxNumber` |
| NDC | `Ndc`, `NDC`, `NationalDrugCode`, `National_Drug_Code` |
| Service Date | `ServiceDate`, `Service_Date`, `DateOfService` |
| Provider ID | `ProviderId`, `Provider_Id`, `PrescriberID` |
| Pharmacy ID | `PharmacyId`, `Pharmacy_Id`, `PharmacyID` |
| Drug Name | `DrugName`, `Drug_Name`, `ProductName` |
| Drug Strength | `DrugStrength`, `Drug_Strength`, `Strength`, `Dosage` |
| Quantity | `Quantity`, `QtyDispensed`, `QuantityDispensed` |
| Days Supply | `DaysSupply`, `Days_Supply`, `DaySupply` |
| Paid Amount | `PaidAmount`, `Paid_Amount`, `AmountPaid` |
| DUR Alert Code | `DurAlertCode`, `DUR_Alert_Code`, `AlertCode` |
| Quarter | `Quarter`, `Qtr`, `Q` |
| Year | `Year`, `Yr`, `LoadYear` |
| Batch ID | `BatchId`, `Batch_Id`, `LoadBatch` |

## Configuration

### CSV Settings in appsettings.json

```json
{
  "CsvSettings": {
    "InputFilePath": "Data/DUR_Quarterly_Load.csv",
    "Delimiter": ",",
    "Encoding": "UTF-8",
    "HasHeader": true,
    "IgnoreBlankLines": true,
    "ValidateData": true,
    "BackupOriginalFile": true,
    "ArchiveProcessedFiles": true,
    "ArchiveDirectory": "Data/Archive"
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `InputFilePath` | Path to the CSV file to process | `Data/DUR_Quarterly_Load.csv` |
| `Delimiter` | Field delimiter character | `,` |
| `Encoding` | File encoding | `UTF-8` |
| `HasHeader` | Whether CSV has header row | `true` |
| `IgnoreBlankLines` | Skip empty lines | `true` |
| `ValidateData` | Enable data validation | `true` |

## Sample CSV Format

Here's an example of a properly formatted CSV file:

```csv
Id,Member_Id,Prescription_Number,NDC,Service_Date,Provider_Id,Pharmacy_Id,Drug_Name,Drug_Strength,Quantity,Days_Supply,Paid_Amount,DUR_Alert_Code,DUR_Alert_Description,Quarter,Year,Batch_Id,Status
1,MBR000001,RX123456,12345-678-90,2025-07-15,PRV1001,PHM001,Lisinopril,10mg,30,30,25.50,,,,Q3,2025,CSV_TEST_BATCH,PENDING
2,MBR000002,RX123457,23456-789-01,2025-07-16,PRV1002,PHM002,Metformin,500mg,60,30,18.75,,,,Q3,2025,CSV_TEST_BATCH,PENDING
3,MBR000003,RX123458,34567-890-12,2025-07-17,PRV1001,PHM003,Atorvastatin,20mg,30,30,35.20,D1,Drug interaction detected,Q3,2025,CSV_TEST_BATCH,PENDING
```

## Data Validation Rules

The CSV loader includes comprehensive validation:

### Required Fields
- **Member ID**: Must be provided and not empty
- **Quarter**: Must be Q1, Q2, Q3, or Q4
- **Year**: Must be between 2020 and 2099

### Data Type Validation
- **Dates**: Must be valid date format (YYYY-MM-DD preferred)
- **Numbers**: Quantity, Days Supply, and Paid Amount must be positive numbers
- **Strings**: Must not exceed maximum field lengths

### Field Length Limits
- Member ID: 50 characters
- NDC: 11 characters
- Drug Name: 255 characters
- DUR Alert Description: 500 characters

## Usage Workflow

### 1. **Prepare CSV File**
```bash
# Place your CSV file in the Data directory
cp your_dur_data.csv Data/DUR_Quarterly_Load.csv
```

### 2. **Configure Settings**
Update `appsettings.json` with appropriate CSV settings:
```json
{
  "CsvSettings": {
    "InputFilePath": "Data/DUR_Quarterly_Load.csv"
  }
}
```

### 3. **Run Application**
```bash
dotnet run --project bulkDURLoader/bulkDURLoader.csproj
```

### 4. **Monitor Output**
The application will:
- Analyze the CSV file structure
- Display file information and headers found
- Read and validate the data
- Perform bulk insert to Oracle database
- Report performance metrics and verification results

## Example Output

```
--- Oracle Bulk Loading Demonstration ---
Loading data from CSV file: Data/DUR_Quarterly_Load.csv
CSV file contains approximately 20 records (2,048 bytes)
Headers found: Id, Member_Id, Prescription_Number, NDC, Service_Date, Provider_Id, Pharmacy_Id, Drug_Name, Drug_Strength, Quantity, Days_Supply, Paid_Amount, DUR_Alert_Code, DUR_Alert_Description, Quarter, Year, Batch_Id, Status
✓ CSV file loaded: 20 records in 15ms
Starting bulk insert to ccBulk_DUR_QTR_LOAD table with 20 records...
✓ Bulk insert completed successfully!
  Records inserted: 20
  Time elapsed: 125ms
  Performance: 160.0 records/second
✓ Verification: 20 records found in database for batch CSV_20250920_143000
```

## Error Handling

### Common Issues and Solutions

1. **File Not Found**
   ```
   Error: CSV file not found: Data/DUR_Quarterly_Load.csv
   Solution: Ensure the file exists and path is correct
   ```

2. **Invalid Headers**
   ```
   Warning: No matching header found for required field
   Solution: Check header names match supported formats
   ```

3. **Data Validation Errors**
   ```
   Warning: Validation failed for record at line 5: Quarter must be Q1-Q4
   Solution: Fix data values in CSV file
   ```

4. **Encoding Issues**
   ```
   Error: Unable to read CSV file with UTF-8 encoding
   Solution: Change encoding setting in appsettings.json
   ```

## Performance Considerations

### File Size Guidelines
- **Small files** (< 1MB): Process directly
- **Medium files** (1-50MB): Use default batch size (1000)
- **Large files** (> 50MB): Consider increasing batch size to 5000-10000

### Optimization Tips
1. **Remove unnecessary columns** from CSV to reduce processing time
2. **Sort data by Member ID** for better database performance
3. **Use consistent date formats** to avoid parsing overhead
4. **Pre-validate critical fields** before processing large files

## Troubleshooting

### Data Quality Issues

**Missing Required Fields**
```csv
# Problem: Missing Member_Id
,RX123456,12345-678-90,2025-07-15,...

# Solution: Ensure all required fields have values
MBR000001,RX123456,12345-678-90,2025-07-15,...
```

**Invalid Date Formats**
```csv
# Problem: Invalid date format
MBR000001,RX123456,12345-678-90,07/15/2025,...

# Solution: Use ISO format (YYYY-MM-DD)
MBR000001,RX123456,12345-678-90,2025-07-15,...
```

**Incorrect Quarter Values**
```csv
# Problem: Invalid quarter
MBR000001,RX123456,12345-678-90,2025-07-15,...,Quarter1,2025,...

# Solution: Use Q1, Q2, Q3, Q4 format
MBR000001,RX123456,12345-678-90,2025-07-15,...,Q3,2025,...
```

### Performance Issues

**Slow Processing**
- Check file encoding matches configuration
- Reduce batch size if memory issues occur
- Verify database connectivity and performance

**High Memory Usage**
- Process files in smaller chunks
- Reduce batch size in configuration
- Consider streaming processing for very large files

## Integration with Existing Workflow

The CSV loading feature integrates seamlessly with the existing bulk loading infrastructure:

1. **Fallback Behavior**: If no CSV file is found, the application generates sample data
2. **Same Performance**: Uses identical Oracle array binding for maximum throughput
3. **Unified Logging**: All operations are logged with the same logging system
4. **Consistent Validation**: Same validation rules apply whether loading from CSV or generating sample data

## Advanced Features

### Custom Header Mapping
You can extend the `DurQuarterlyLoadMap` class to support additional header formats:

```csharp
// Add custom header mappings
Map(m => m.MemberId).Name("MemberId", "Member_Id", "YourCustomHeaderName");
```

### Data Transformation
The CSV reader supports data transformation during loading:

```csharp
// Custom data transformation in ValidateRecord method
if (record.Quarter?.ToUpper() == "QUARTER1") 
{
    record.Quarter = "Q1";
}
```

### Batch Processing
For very large files, consider implementing batch file processing:

```csharp
// Process multiple CSV files
var csvFiles = Directory.GetFiles("Data", "*.csv");
foreach (var file in csvFiles)
{
    await ProcessCsvFile(file);
}
```

This comprehensive CSV loading functionality makes the `bulkDURLoader` application production-ready for processing real-world DUR data files!