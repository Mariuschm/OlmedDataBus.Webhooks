# QueueRelations Implementation Summary

## Overview
Successfully implemented the `QueueRelations` model for tracking relationships between queue items in the ProRWS system.

## Files Created

### 1. Model
- **File**: `..\OlmedDataBus\Prospeo.DbContext\Models\QueueRelations.cs`
- **Description**: Entity model representing relationships between queue items
- **Features**:
  - Primary key with auto-increment
  - Foreign keys to Queue table (SourceItemId and TargetItemId)
  - CreatedAt timestamp with database-generated default value
  - Navigation properties for bidirectional relationships
  - Comprehensive XML documentation

### 2. Service Interface
- **File**: `..\OlmedDataBus\Prospeo.DbContext\Interfaces\IQueueRelationsService.cs`
- **Description**: Service interface for managing queue relations
- **Methods**: 22 methods including:
  - Create relations (with duplicate prevention)
  - Retrieve relations (by ID, source, target, or both)
  - Get related queue items
  - Delete relations (single or bulk)
  - Check existence and count relations
  - Eager loading support

### 3. Service Implementation
- **File**: `..\OlmedDataBus\Prospeo.DbContext\Services\QueueRelationsService.cs`
- **Description**: Complete implementation of IQueueRelationsService
- **Features**:
  - Full CRUD operations
  - Entity Framework Core integration
  - Comprehensive logging
  - Validation and error handling
  - Support for both source and target navigation

### 4. Documentation
- **File**: `..\OlmedDataBus\Prospeo.DbContext\Models\README_QueueRelations.md`
- **Description**: Complete documentation with:
  - SQL table structure
  - Model field descriptions
  - Use cases (Order?Invoice, Invoice?Correction, Order?Issue, PurchaseInvoice?Receipt)
  - Service method examples
  - Integration patterns with queue processors
  - Best practices
  - Complete workflow example
  - Performance considerations
  - Security notes

## Files Modified

### 1. Queue Model
- **File**: `..\OlmedDataBus\Prospeo.DbContext\Models\Queue.cs`
- **Changes**:
  - Added `SourceRelations` collection navigation property
  - Added `TargetRelations` collection navigation property
  - Added XML documentation for navigation properties

### 2. Database Context
- **File**: `..\OlmedDataBus\Prospeo.DbContext\Data\ProspeoDbContext.cs`
- **Changes**:
  - Added `DbSet<QueueRelations>` property
  - Added complete Entity Framework configuration in `OnModelCreating`:
    - Table and schema configuration
    - Primary key constraint
    - Unique constraint for (SourceItemId, TargetItemId)
    - Indexes on SourceItemId and TargetItemId
    - Foreign key relationships with DeleteBehavior.Restrict
    - CreatedAt default value configuration

### 3. Service Registration
- **File**: `..\OlmedDataBus\Prospeo.DbContext\Extensions\ServiceCollectionExtensions.cs`
- **Changes**:
  - Added `IQueueRelationsService` ? `QueueRelationsService` registration to all three service registration methods:
    - `AddProspeoServices(IConfiguration, string)`
    - `AddProspeoServices(string)`
    - `AddProspeoServicesDirect(string)`

## Database Schema

```sql
CREATE TABLE ProRWS.QueueRelations
(
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_QueueRelations PRIMARY KEY,
    SourceItemId INT NOT NULL,
    TargetItemId INT NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    
    CONSTRAINT UQ_QueueRelations_Source_Target UNIQUE (SourceItemId, TargetItemId),
    CONSTRAINT FK_QueueRelations_Source FOREIGN KEY (SourceItemId) REFERENCES ProRWS.Queue(Id),
    CONSTRAINT FK_QueueRelations_Target FOREIGN KEY (TargetItemId) REFERENCES ProRWS.Queue(Id)
);

CREATE INDEX IX_QueueRelations_SourceItemId ON ProRWS.QueueRelations (SourceItemId);
CREATE INDEX IX_QueueRelations_TargetItemId ON ProRWS.QueueRelations (TargetItemId);
```

## Entity Framework Configuration

The model is fully configured with:
- **Table Name**: `QueueRelations` in schema `ProRWS`
- **Primary Key**: `PK_QueueRelations` on `Id` column
- **Unique Constraint**: `UQ_QueueRelations_Source_Target` on `(SourceItemId, TargetItemId)`
- **Indexes**: 
  - `IX_QueueRelations_SourceItemId`
  - `IX_QueueRelations_TargetItemId`
- **Foreign Keys**:
  - `FK_QueueRelations_Source` ? `Queue(Id)` with `DeleteBehavior.Restrict`
  - `FK_QueueRelations_Target` ? `Queue(Id)` with `DeleteBehavior.Restrict`
- **Navigation Properties**:
  - Bidirectional: `Queue.SourceRelations` ? `QueueRelations.SourceItem`
  - Bidirectional: `Queue.TargetRelations` ? `QueueRelations.TargetItem`

## Key Features

### 1. Relationship Tracking
- Track parent-child relationships between queue items
- Map dependencies between different operation types
- Enable distributed transactions and rollbacks

### 2. Data Integrity
- Unique constraint prevents duplicate relations
- Foreign keys ensure referential integrity
- Restrict delete behavior prevents orphaned records
- Validation in service layer

### 3. Performance
- Indexes on both foreign keys for fast lookups
- Support for eager loading with `Include()`
- Efficient queries for both directions (source?target, target?source)

### 4. Use Cases
- **Order ? Invoice**: Track invoices generated from orders
- **Invoice ? Correction**: Track corrections for invoices
- **Order ? Issue**: Track warehouse issues for orders
- **Purchase Invoice ? Receipt**: Track warehouse receipts for purchase invoices
- **Invoice ? Payment**: Track payments for invoices

### 5. Service Methods
```csharp
// Creating relations
CreateRelationAsync(sourceId, targetId)
CreateOrGetRelationAsync(sourceId, targetId)

// Retrieving relations
GetByIdAsync(id)
GetRelationAsync(sourceId, targetId)
GetSourceRelationsAsync(sourceId)
GetTargetRelationsAsync(targetId)
GetAllRelationsForItemAsync(itemId)

// Retrieving related items
GetTargetItemsAsync(sourceId)
GetSourceItemsAsync(targetId)

// Deleting relations
DeleteAsync(id)
DeleteRelationAsync(sourceId, targetId)
DeleteSourceRelationsAsync(sourceId)
DeleteTargetRelationsAsync(targetId)
DeleteAllRelationsForItemAsync(itemId)

// Checking and counting
RelationExistsAsync(sourceId, targetId)
GetSourceRelationsCountAsync(sourceId)
GetTargetRelationsCountAsync(targetId)
GetAllAsync(includeSource, includeTarget)
```

## Integration Points

### Queue Processors
All queue service processors can now:
1. Create relations when generating dependent tasks
2. Query relations to find parent or child tasks
3. Track workflow state across multiple queue items
4. Implement rollback logic using relations
5. Validate dependencies before processing

### Example Integration
```csharp
// In OrderQueueService
var orderQueue = await _queueService.AddAsync(orderItem);
var invoiceQueue = await CreateInvoiceForOrder(order);
await _queueRelationsService.CreateRelationAsync(orderQueue.Id, invoiceQueue.Id);

// In InvoiceCorrectionQueueService
var originalInvoice = await FindOriginalInvoice(correctionDto.InvoiceId);
var correctionQueue = await _queueService.AddAsync(correctionItem);
await _queueRelationsService.CreateRelationAsync(originalInvoice.Id, correctionQueue.Id);
```

## Next Steps

### 1. Database Migration
Run Entity Framework migration to create the table:
```bash
cd ..\OlmedDataBus\Prospeo.DbContext
dotnet ef migrations add AddQueueRelations
dotnet ef database update
```

Or execute the SQL script manually if not using EF migrations.

### 2. Update Queue Processors
Update the following services to use QueueRelations:
- `OrderQueueService` - create relations when generating invoices/issues
- `InvoiceQueueService` - track orders that generated invoices
- `InvoiceCorrectionQueueService` - link corrections to original invoices
- `CorrectionQueueService` - track corrected documents
- `MmwQueueService` - link issues to orders
- `PwQueueService` - link receipts to purchase invoices
- `RwQueueService` - link returns to original documents

### 3. Add Monitoring
Implement monitoring to:
- Track unprocessed dependent tasks
- Alert on orphaned relations
- Monitor processing chains
- Generate workflow reports

### 4. Testing
Create unit and integration tests for:
- Relation creation and validation
- Duplicate prevention
- Cascade operations
- Orphan handling
- Performance with large datasets

## Build Status

? **Build Successful** - All files compile without errors

## Compatibility

- **Framework**: .NET 9.0
- **Entity Framework Core**: 9.0.0
- **SQL Server**: Compatible with SQL Server 2016+
- **C# Version**: 13.0

## Security Considerations

- Foreign key constraints ensure data integrity
- Unique constraint prevents duplicate relations
- Restrict delete behavior prevents accidental data loss
- Service layer validates all inputs
- Logging tracks all operations for audit trail

## Performance Considerations

- Indexes created on both foreign key columns
- Efficient queries for both navigation directions
- Support for eager loading to reduce N+1 queries
- Minimal overhead on queue operations
- Optimized for both read and write operations

## Dependencies

The implementation depends on:
- `Microsoft.EntityFrameworkCore` (9.0.0)
- `Microsoft.EntityFrameworkCore.SqlServer` (9.0.0)
- `Microsoft.Extensions.Logging.Abstractions` (9.0.0)
- Existing `Queue` model
- Existing `ProspeoDataContext`
- Existing `IQueueService` and `QueueService`

## Documentation

Complete documentation is available in:
- XML comments in all code files
- `README_QueueRelations.md` with comprehensive examples
- Inline code examples in the README
- SQL schema documentation

## Conclusion

The QueueRelations model has been successfully implemented with:
- ? Complete entity model with navigation properties
- ? Full service interface and implementation
- ? Entity Framework Core configuration
- ? Service registration
- ? Comprehensive documentation
- ? Best practices and usage examples
- ? Build verification

The implementation is ready for:
1. Database migration/creation
2. Integration with queue processors
3. Testing and validation
4. Production deployment
