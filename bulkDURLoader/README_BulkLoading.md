# bulkDURLoader - Oracle Bulk Loading for DUR Quarterly Data

## Overview

The `bulkDURLoader` application is designed to efficiently load large volumes of DUR (Drug Utilization Review) quarterly data into an Oracle database using Oracle's high-performance array binding capabilities.

## Features

### 🚀 High-Performance Bulk Loading
- **Oracle Array Binding**: Utilizes Oracle's native array binding for maximum performance
- **Batch Processing**: Configurable batch sizes to optimize memory usage and performance
- **Multi-Framework Support**: Targets .NET 6.0, 8.0, and 9.0 for maximum compatibility

### 📊 DUR Data Management
- **Comprehensive Data Model**: Supports complete DUR quarterly load data structure
- **Flexible Schema**: Includes additional data fields for future extensibility
- **Data Validation**: Built-in validation for critical fields and constraints

### 🔧 Configuration & Monitoring
- **Configurable Settings**: Batch size, timeouts, and performance tuning options
- **Comprehensive Logging**: Detailed logging with multiple log levels
- **Performance Metrics**: Real-time performance monitoring and reporting

## Database Table Structure

The application targets the `ccBulk_DUR_QTR_LOAD` table with the following key fields:

| Field | Type | Description |
|-------|------|-------------|
| ID | NUMBER(19) | Unique identifier |
| MEMBER_ID | VARCHAR2(50) | Member identifier |
| PRESCRIPTION_NUMBER | VARCHAR2(50) | Prescription number |
| NDC | VARCHAR2(11) | National Drug Code |
| SERVICE_DATE | DATE | Date of service |
| PROVIDER_ID | VARCHAR2(50) | Prescribing provider ID |
| PHARMACY_ID | VARCHAR2(50) | Pharmacy identifier |
| DRUG_NAME | VARCHAR2(255) | Drug name |
| QUANTITY | NUMBER(10,2) | Quantity dispensed |
| DAYS_SUPPLY | NUMBER(10) | Days supply |
| PAID_AMOUNT | NUMBER(10,2) | Amount paid |
| DUR_ALERT_CODE | VARCHAR2(10) | DUR alert code |
| QUARTER | VARCHAR2(2) | Quarter (Q1-Q4) |
| YEAR | NUMBER(4) | Year |
| BATCH_ID | VARCHAR2(50) | Batch identifier |
| STATUS | VARCHAR2(20) | Processing status |

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "OracleConnection": "Data Source=localhost:1521/XE;User Id=your_username;Password=your_password;Connection Timeout=30;"
  },
  "BulkLoadSettings": {
    "BatchSize": 1000,
    "CommandTimeout": 300,
    "MaxRecordsPerLoad": 50000,
    "SampleDataCount": 100
  }
}
```

### Key Configuration Options

- **BatchSize**: Number of records to process in each batch (default: 1000)
- **CommandTimeout**: Timeout for database commands in seconds (default: 300)
- **MaxRecordsPerLoad**: Maximum records allowed per load operation (default: 50000)
- **SampleDataCount**: Number of sample records to generate for testing (default: 100)

## Usage

### Basic Bulk Loading
```csharp
// Initialize the bulk loader
var bulkLoader = new OracleBulkLoader(dbConfig, configuration);

// Prepare your data
List<DurQuarterlyLoad> records = GetYourDurData();

// Perform bulk insert
int insertedCount = await bulkLoader.BulkInsertAsync(records);
```

### Generate Sample Data
```csharp
// Generate sample data for testing
var sampleData = OracleBulkLoader.GenerateSampleData(
    recordCount: 1000,
    quarter: "Q3",
    year: 2025,
    batchId: "BATCH_20250920_143000"
);
```

## Performance Characteristics

### Typical Performance Metrics
- **Small Batches (100-1000 records)**: 500-2000 records/second
- **Medium Batches (1000-10000 records)**: 2000-5000 records/second
- **Large Batches (10000+ records)**: 3000-8000 records/second

### Performance Factors
- **Batch Size**: Larger batches generally perform better but use more memory
- **Network Latency**: Lower latency to Oracle database improves performance
- **Database Configuration**: Oracle buffer sizes and connection pooling affect performance
- **Data Complexity**: Records with more populated fields may process slightly slower

## Database Setup

1. **Create Table**: Run the provided SQL script to create the table structure
   ```sql
   -- See Database/CreateTable_ccBulk_DUR_QTR_LOAD.sql
   ```

2. **Create Indexes**: The script includes performance-optimized indexes
   ```sql
   CREATE INDEX IDX_ccBulk_DUR_MEMBER ON ccBulk_DUR_QTR_LOAD (MEMBER_ID);
   CREATE INDEX IDX_ccBulk_DUR_QUARTER_YEAR ON ccBulk_DUR_QTR_LOAD (QUARTER, YEAR);
   ```

3. **Set Permissions**: Grant appropriate permissions to your application user
   ```sql
   GRANT SELECT, INSERT, UPDATE, DELETE ON ccBulk_DUR_QTR_LOAD TO your_app_user;
   ```

## Error Handling

The bulk loader includes comprehensive error handling:

- **Connection Failures**: Automatic retry logic with configurable delays
- **Batch Failures**: Individual batch error reporting without stopping the entire load
- **Data Validation**: Pre-insert validation for critical fields
- **Oracle-Specific Errors**: Detailed Oracle error code reporting and handling

## Logging

Comprehensive logging at multiple levels:

- **Debug**: Detailed parameter and execution information
- **Info**: High-level operation status and performance metrics
- **Warning**: Non-critical issues and retry attempts
- **Error**: Critical failures with full exception details

## Testing

The application includes built-in testing capabilities:

1. **Sample Data Generation**: Creates realistic test data
2. **Performance Benchmarking**: Measures and reports insertion performance
3. **Data Verification**: Verifies successful insertion by counting records
4. **Connection Testing**: Validates database connectivity before bulk operations

## Building and Running

### Prerequisites
- .NET 6.0, 8.0, or 9.0 runtime
- Oracle database with appropriate permissions
- Oracle.ManagedDataAccess.Core NuGet package

### Build
```bash
dotnet build bulkDURLoader/bulkDURLoader.csproj
```

### Run
```bash
dotnet run --project bulkDURLoader/bulkDURLoader.csproj
```

## Deployment

The application supports multiple deployment scenarios:

- **Framework-Dependent**: Requires .NET runtime on target machine
- **Self-Contained**: Includes .NET runtime in deployment package
- **Single File**: Packages everything into a single executable

See `DEPLOYMENT.md` for detailed deployment instructions.

## Troubleshooting

### Common Issues

1. **Connection String**: Verify Oracle connection string format and credentials
2. **Table Permissions**: Ensure application user has INSERT permissions
3. **Memory Usage**: Adjust batch size if experiencing memory issues
4. **Performance**: Tune batch size and command timeout for your environment

### Performance Tuning

1. **Increase Batch Size**: For better throughput (up to 5000-10000 records)
2. **Adjust Command Timeout**: For large batches or slow networks
3. **Connection Pooling**: Configure Oracle connection pooling for high-volume scenarios
4. **Database Tuning**: Optimize Oracle buffer sizes and redo log configuration

## Support

For issues and questions:
1. Check the application logs for detailed error information
2. Verify database connectivity and permissions
3. Review configuration settings
4. Test with smaller batch sizes to isolate issues