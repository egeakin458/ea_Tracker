# ea_Tracker - Claude Development Context

## Project Overview

**ea_Tracker** is a sophisticated investigation management system designed to automatically detect anomalies and issues in business entities (Invoices and Waybills). The system features a dynamic investigator architecture that can be configured, started, stopped, and monitored in real-time.

### Core Purpose
- **Invoice Investigation**: Detect anomalous invoices (negative amounts, excessive tax ratios, future dates)
- **Waybill Investigation**: Identify late shipments and delivery issues
- **Dynamic Management**: Create, configure, and control investigators on-demand
- **Real-time Monitoring**: Live dashboard with execution tracking and results
- **Audit Compliance**: Complete investigation history and audit trails

## Architecture Overview

### **Backend (.NET 8.0 Web API)**
- **Framework**: ASP.NET Core 8.0 with Entity Framework Core 8.0.1
- **Database**: MySQL with Pomelo provider
- **Pattern**: Repository pattern with dependency injection
- **Security**: User Secrets for development, environment variables for production
- **Middleware**: Global exception handling, CORS enabled

### **Frontend (React TypeScript)**
- **Framework**: React 18 with TypeScript
- **HTTP Client**: Axios for API communication
- **Testing**: Jest for unit tests, Cypress for E2E testing
- **UI**: Custom CSS with responsive design

### **Database Design (3NF Compliant)**
```
InvestigatorType (Reference Data)
├── InvestigatorInstance (Runtime Configurations)
    ├── InvestigationExecution (Session Tracking)
        └── InvestigationResult (Findings & Audit Trail)

Invoice/Waybill (Business Entities with Audit Fields)
```

## Current Status & Phase Progress

### **Phase 1: Data Persistence & CRUD** ✅ **COMPLETED**
#### Completed:
- [x] **Entity Models**: InvestigatorType, InvestigatorInstance, InvestigationExecution, InvestigationResult
- [x] **Enhanced Business Models**: Invoice, Waybill with audit fields (CreatedAt, UpdatedAt, HasAnomalies, LastInvestigatedAt)
- [x] **Enums**: InvestigatorStatus, ExecutionStatus, ResultSeverity, InvoiceType, WaybillType
- [x] **Database Context**: ApplicationDbContext with optimized configuration, indexes, and relationships
- [x] **EF Migration**: `AddInvestigationPersistence` migration created and applied
- [x] **Repository Pattern**: Generic repository + InvestigatorRepository with specialized business logic
- [x] **User Secrets**: Connection string securely configured
- [x] **InvestigationManager Refactoring**: **UPDATED** to use database persistence instead of in-memory storage
- [x] **API Controllers**: InvestigationsController updated with async database operations
- [x] **DTOs**: InvestigatorStateDto, InvestigatorResultDto, CreateResponse, ApiResponse
- [x] **Frontend Integration**: **UPDATED** React components for new API structure
- [x] **Testing**: Backend (xUnit) and Frontend (Jest/Cypress) tests passing
- [x] **Dependency Injection**: Complete system-wide consistency with scoped services
- [x] **Project Restructuring**: Professional directory structure (src/, tests/, docs/)
- [x] **Unified Test Structure**: All tests moved to unified tests/ directory
- [x] **React Version Conflicts**: Resolved with proper root-level configuration
- [x] **CI/CD Pipeline**: GitHub Actions updated for unified structure
- [x] **Documentation**: README.md updated with unified test architecture

### **Phase 1.5: SOLID Principles Refactoring** ✅ **PHASE 1 COMPLETED**
**Achievement: Comprehensive service layer abstraction with zero breaking changes**

#### **Phase 1.5.1: Service Layer Abstractions** ✅ **COMPLETED**
- [x] **IInvestigationManager Interface**: Created interface to fix DIP violation (Step 1)
- [x] **IInvoiceService Interface**: Business operations abstraction (Step 2) 
- [x] **IWaybillService Interface**: Business operations abstraction (Step 3)
- [x] **InvoiceService Implementation**: Complete business logic moved from controller (Step 4)
- [x] **WaybillService Implementation**: Enhanced with overdue/expiring algorithms (Step 5)
- [x] **Dependency Injection Update**: All service interfaces registered (Step 6)
- [x] **Controller Refactoring**: All controllers use interfaces - API compatibility preserved (Steps 7-9)
- [x] **Phase 1.5.1 Testing**: All tests passing (4/4) - zero breaking changes (Step Test)
- [x] **Git Checkpoint**: Service layer abstractions complete

#### **Phase 1.5.2: Business Logic Extraction** ✅ **COMPLETED**
- [x] **IInvestigationLogic<T> Interface**: Pure business logic abstraction
- [x] **InvoiceAnomalyLogic Class**: Algorithm isolation from infrastructure
- [x] **WaybillDeliveryLogic Class**: Enhanced overdue/expiring algorithms
- [x] **IInvestigationConfiguration Interface**: Externalize business thresholds
- [x] **InvestigatorFactory Enhancement**: Registration-based strategy pattern
- [x] **Investigator Refactoring**: Use injected business logic components
- [x] **Phase 1.5.2 Testing**: Business logic isolation validation
- [x] **Git Checkpoint**: Business logic extraction complete

### **Phase 2: Enhanced Investigation Features** (Not Started)
- Advanced filtering/search capabilities
- Export results to CSV/PDF
- Investigation scheduling and automation
- Real-time notifications/alerts

### **Phase 3: User Experience** (Not Started)
- User authentication and authorization
- Investigation templates and presets
- Data visualization (charts, dashboards)
- Audit trails and investigation history

### **Phase 4: Integration & Scalability** (Not Started)
- External data source integrations
- Background job processing
- Performance optimization
- API rate limiting and monitoring

## Key Files & Structure

### **Unified Project Structure** ✨
```
ea_Tracker/
├── src/                                    # SOURCE CODE ONLY
│   ├── backend/                            # .NET 8.0 Web API
│   │   ├── Controllers/
│   │   │   ├── InvestigationsController.cs # Investigation management API
│   │   │   ├── InvestigatorController.cs   # Investigator CRUD operations
│   │   │   ├── InvoicesController.cs       # Invoice management API
│   │   │   └── WaybillsController.cs       # Waybill management API
│   │   ├── Data/
│   │   │   └── ApplicationDbContext.cs     # EF DbContext with full configuration
│   │   ├── Enums/
│   │   │   ├── InvestigatorStatus.cs      # Inactive, Stopped, Running, Failed
│   │   │   ├── ExecutionStatus.cs         # Running, Completed, Failed, Cancelled
│   │   │   ├── ResultSeverity.cs          # Info, Warning, Error, Anomaly, Critical
│   │   │   ├── InvoiceType.cs             # Business enum
│   │   │   └── WaybillType.cs             # Business enum
│   │   ├── Models/
│   │   │   ├── InvestigatorType.cs        # Reference data for investigator templates
│   │   │   ├── InvestigatorInstance.cs    # Persistent investigator configurations
│   │   │   ├── InvestigationExecution.cs  # Session tracking
│   │   │   ├── InvestigationResult.cs     # Findings and audit trail
│   │   │   ├── Invoice.cs                 # Enhanced with audit fields
│   │   │   └── Waybill.cs                 # Enhanced with audit fields
│   │   ├── Repositories/
│   │   │   ├── IGenericRepository.cs      # Generic CRUD interface
│   │   │   ├── GenericRepository.cs       # Generic CRUD implementation
│   │   │   ├── IInvestigatorRepository.cs # Investigator-specific operations
│   │   │   └── InvestigatorRepository.cs  # Business logic repository
│   │   ├── Services/
│   │   │   ├── Investigator.cs            # Abstract base class
│   │   │   ├── InvoiceInvestigator.cs     # Invoice anomaly detection logic
│   │   │   ├── WaybillInvestigator.cs     # Enhanced waybill logic (overdue/expiring)
│   │   │   ├── IInvestigatorFactory.cs    # Factory interface
│   │   │   ├── InvestigatorFactory.cs     # DI-based factory implementation
│   │   │   ├── IInvestigationManager.cs   # ✨ NEW - Investigation coordination interface
│   │   │   ├── InvestigationManager.cs    # Fully database-integrated coordinator
│   │   │   ├── IInvoiceService.cs         # ✨ NEW - Invoice business operations interface
│   │   │   ├── IWaybillService.cs         # ✨ NEW - Waybill business operations interface
│   │   │   ├── InvoiceService.cs          # PENDING - Invoice business logic implementation
│   │   │   └── WaybillService.cs          # PENDING - Waybill business logic implementation
│   │   └── Program.cs                     # Startup configuration with user secrets
│   └── frontend/                          # React TypeScript SPA
│       ├── src/
│       │   ├── App.tsx                    # Main application component
│       │   ├── Dashboard.tsx              # Investigation management dashboard
│       │   ├── lib/
│       │   │   └── axios.ts               # HTTP client configuration
│       │   └── types/
│       │       └── api.ts                 # TypeScript API interfaces
│       ├── public/                        # Static assets
│       └── package.json                   # Frontend dependencies
├── tests/                                 # ALL TESTS UNIFIED HERE
│   ├── backend/
│   │   ├── unit/                          # Backend unit tests (xUnit)
│   │   │   ├── InvestigationManagerTests.cs
│   │   │   └── ea_Tracker.Tests.csproj
│   │   └── integration/                   # Future: API integration tests
│   ├── frontend/
│   │   ├── unit/                          # Frontend unit tests (Jest + RTL)
│   │   │   ├── App.spec.tsx               # App component tests
│   │   │   └── axios.spec.ts              # API client tests
│   │   ├── integration/                   # Frontend integration tests
│   │   │   └── Dashboard.spec.tsx         # Dashboard component integration
│   │   └── e2e/                           # End-to-end tests (Cypress)
│   │       ├── smoke.cy.js                # Critical workflow tests
│   │       └── fixtures/                  # Test data
│   │           └── investigators.json
│   └── e2e/                               # Future: Cross-stack system tests
├── package.json                           # Root-level unified configuration
├── jest.config.js                         # Unified Jest configuration
├── cypress.config.js                      # Unified Cypress configuration
└── tsconfig.json                          # Unified TypeScript configuration
```

## Development Setup

### **Prerequisites**
- .NET 8.0 SDK
- Node.js 18+
- MySQL 8.0+
- Git

### **Backend Setup**
1. **Clone and Navigate**: `cd Backend`
2. **Install EF Tools**: `dotnet tool install --global dotnet-ef`
3. **User Secrets**: Already configured with connection string
4. **Install MySQL**: `sudo apt install mysql-server -y`
5. **Apply Migration**: `dotnet ef database update`
6. **Run**: `dotnet run`

### **Frontend Setup**
1. **Navigate**: `cd frontend`
2. **Install**: `npm install`
3. **Run**: `npm start`
4. **Test**: `npm test`

## Database Schema

### **Investigation System Tables**
| Table | Purpose | Key Fields |
|-------|---------|------------|
| `InvestigatorTypes` | Reference data for investigator templates | `Id`, `Code`, `DisplayName`, `DefaultConfiguration` |
| `InvestigatorInstances` | Persistent investigator configurations | `Id` (GUID), `TypeId`, `IsActive`, `CustomConfiguration` |
| `InvestigationExecutions` | Session tracking with status | `Id`, `InvestigatorId`, `Status`, `StartedAt`, `CompletedAt` |
| `InvestigationResults` | High-volume findings storage | `Id` (BIGINT), `ExecutionId`, `Severity`, `EntityType`, `EntityId` |

### **Business Tables (Enhanced)**
| Table | Purpose | New Fields |
|-------|---------|------------|
| `Invoices` | Invoice business data | + `CreatedAt`, `UpdatedAt`, `HasAnomalies`, `LastInvestigatedAt` |
| `Waybills` | Waybill business data | + `CreatedAt`, `UpdatedAt`, `HasAnomalies`, `LastInvestigatedAt` |

### **Performance Indexes**
- `IX_InvestigatorInstance_Type_Active`: Query active investigators by type
- `IX_Result_Execution_Time`: Time-based result queries
- `IX_Result_Entity`: Link results to business entities
- `IX_Result_Severity`: Filter by severity level

## API Endpoints

### **Current Endpoints** (InvestigationsController)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| `GET` | `/api/investigations` | Get all investigators with status |
| `POST` | `/api/investigations/start` | Start all investigators |
| `POST` | `/api/investigations/stop` | Stop all investigators |
| `POST` | `/api/investigations/{id}/start` | Start specific investigator |
| `POST` | `/api/investigations/{id}/stop` | Stop specific investigator |
| `GET` | `/api/investigations/{id}/results` | Get investigator results |
| `POST` | `/api/investigations/invoice` | Create invoice investigator |
| `POST` | `/api/investigations/waybill` | Create waybill investigator |

### **Planned Endpoints** (Phase 1 Completion)
- `GET` `/api/investigators` - CRUD operations for investigator instances
- `GET` `/api/invoices` - Invoice CRUD with search/filter
- `GET` `/api/waybills` - Waybill CRUD with search/filter
- `GET` `/api/executions` - Execution history and analytics
- `GET` `/api/results` - Result search with pagination

## Unified Testing Strategy ✨

The project now implements a **professional unified testing approach** with all tests organized in the `tests/` directory.

### **Test Organization**
- **All Tests**: Centralized in `tests/` directory (no scattered locations)
- **Clear Separation**: Source code (`src/`) vs. Tests (`tests/`)
- **Unified Commands**: Run all tests from project root
- **Zero Duplication**: Single source of truth for test configuration

### **Backend Testing**
- **Framework**: XUnit with Entity Framework InMemory
- **Location**: `tests/backend/unit/`
- **Coverage**: InvestigationManagerTests.cs (1 test passing)
- **Strategy**: Repository pattern enables easy mocking
- **Command**: `npm run test:backend`

### **Frontend Testing**
- **Unit Tests**: Jest + React Testing Library (2 tests passing)
- **Integration Tests**: Component interaction testing (1 test passing)
- **E2E Tests**: Cypress for workflow validation
- **Location**: `tests/frontend/`
- **Files**: `App.spec.tsx`, `Dashboard.spec.tsx`, `axios.spec.ts`
- **Command**: `npm run test:frontend`

### **Unified Test Commands**
```bash
# From project root
npm test -- --watchAll=false     # All tests (backend + frontend)
npm run test:backend             # Backend unit tests only
npm run test:frontend            # Frontend unit + integration tests
npm run test:e2e                 # End-to-end tests
npm run test:watch               # Watch mode for development
```

### **Test Status**
- ✅ **Backend**: 1/1 tests passing
- ✅ **Frontend**: 3/3 tests passing  
- ✅ **Total**: 4/4 tests passing
- ✅ **React Version Conflicts**: Resolved
- ✅ **CI/CD Integration**: Fully functional

## Security & Configuration

### **Development Security**
- **User Secrets**: Connection strings stored securely
- **Location**: `~/.microsoft/usersecrets/8949b5c3-0a11-436e-9acc-bfce16a1dda2/secrets.json`
- **Connection**: `Server=localhost;Database=ea_tracker_db;Uid=root;Pwd=Hea!90569;`

### **Production Considerations**
- Environment variables for connection strings
- Proper authentication/authorization (Phase 3)
- Rate limiting and monitoring (Phase 4)

## Current Investigation Logic

# WaybillInvestigator
```csharp
/// <summary>
        /// Begins waybill investigation operations using pure business logic.
        /// Separates data access from business rule evaluation.
        /// </summary>
        protected override void OnStart()
        {
            using var db = _dbFactory.CreateDbContext();
            
            // Data Access: Get all waybills from database
            var waybills = db.Waybills.ToList();
            
            // Business Logic: Evaluate waybills using pure business logic
            var results = _businessLogic.EvaluateWaybills(waybills, _configuration);
            
            // Result Recording: Process and record findings
            foreach (var result in results.Where(r => r.IsAnomaly))
            {
                var waybill = result.Entity;
                var reasonsText = string.Join(", ", result.Reasons);
                
                // Determine the primary issue type for classification
                var issueType = DetermineIssueType(result.Reasons);
                var resultMessage = $"{issueType.ToUpper()}: Waybill {waybill.Id} - {reasonsText}";
                
                // Enhanced result payload with detailed information
                var resultPayload = new
                {
                    waybill.Id,
                    waybill.RecipientName,
                    waybill.GoodsIssueDate,
                    waybill.DueDate,
                    IssueType = issueType,
                    DeliveryReasons = result.Reasons,
                    EvaluatedAt = result.EvaluatedAt,
                    Configuration = new
                    {
                        _configuration.Waybill.ExpiringSoonHours,
                        _configuration.Waybill.LegacyCutoffDays,
                        _configuration.Waybill.CheckOverdueDeliveries,
                        _configuration.Waybill.CheckExpiringSoon,
                        _configuration.Waybill.CheckLegacyWaybills
                    }
                };
                
                RecordResult(resultMessage, JsonSerializer.Serialize(resultPayload));
            }
            
            // Enhanced Statistics: Record comprehensive statistics for monitoring
            var stats = _businessLogic.GetDeliveryStatistics(waybills, _configuration);
            if (stats.TotalWaybills > 0)
            {
                var statsMessage = $"Investigation complete: {stats.TotalProblematic}/{stats.TotalWaybills} issues found ({stats.ProblematicRate:F1}%)";
                var statsPayload = new
                {
                    stats.TotalWaybills,
                    stats.TotalProblematic,
                    stats.ProblematicRate,
                    DeliveryBreakdown = new
                    {
                        stats.OverdueCount,
                        stats.ExpiringSoonCount,
                        stats.LegacyOverdueCount
                    },
                    WaybillTypes = new
                    {
                        stats.WithDueDateCount,
                        stats.LegacyWaybillCount
                    },
                    CompletedAt = DateTime.UtcNow,
                    ConfigurationApplied = new
                    {
                        _configuration.Waybill.ExpiringSoonHours,
                        _configuration.Waybill.LegacyCutoffDays
                    }
                };
                
                RecordResult(statsMessage, JsonSerializer.Serialize(statsPayload));
            }

            // Optional: Record specialized category summaries for dashboard purposes
            RecordSpecializedSummaries(waybills);
        }
```

# Invoice Investigator
```csharp
/// <summary>
        /// Begins invoice investigation operations using pure business logic.
        /// Separates data access from business rule evaluation.
        /// </summary>
        protected override void OnStart()
        {
            using var db = _dbFactory.CreateDbContext();
            
            // Data Access: Get all invoices from database
            var invoices = db.Invoices.ToList();
            
            // Business Logic: Evaluate invoices using pure business logic
            var results = _businessLogic.EvaluateInvoices(invoices, _configuration);
            
            // Result Recording: Process and record findings
            foreach (var result in results.Where(r => r.IsAnomaly))
            {
                var invoice = result.Entity;
                var reasonsText = string.Join(", ", result.Reasons);
                var resultMessage = $"Anomalous invoice {invoice.Id}: {reasonsText}";
                
                // Enhanced result payload with detailed information
                var resultPayload = new
                {
                    invoice.Id,
                    invoice.TotalAmount,
                    invoice.TotalTax,
                    invoice.IssueDate,
                    invoice.RecipientName,
                    AnomalyReasons = result.Reasons,
                    EvaluatedAt = result.EvaluatedAt,
                    Configuration = new
                    {
                        _configuration.Invoice.MaxTaxRatio,
                        _configuration.Invoice.CheckNegativeAmounts,
                        _configuration.Invoice.CheckFutureDates,
                        _configuration.Invoice.MaxFutureDays
                    }
                };
                
                RecordResult(resultMessage, JsonSerializer.Serialize(resultPayload));
            }
            
            // Optional: Record statistics for monitoring
            var stats = _businessLogic.GetAnomalyStatistics(invoices, _configuration);
            if (stats.TotalInvoices > 0)
            {
                var statsMessage = $"Investigation complete: {stats.TotalAnomalies}/{stats.TotalInvoices} anomalies found ({stats.AnomalyRate:F1}%)";
                var statsPayload = new
                {
                    stats.TotalInvoices,
                    stats.TotalAnomalies,
                    stats.AnomalyRate,
                    stats.NegativeAmountCount,
                    stats.ExcessiveTaxCount,
                    stats.FutureDateCount,
                    CompletedAt = DateTime.UtcNow
                };
                
                RecordResult(statsMessage, JsonSerializer.Serialize(statsPayload));
            }
        }
```

### **Technical Dependencies**
- All service implementations must use repository pattern
- Proper exception handling and logging maintained
- DTO mapping preserved exactly as in controllers
- Business validation rules consistently applied

## Performance Considerations

### **Database Optimizations**
- Strategic indexes for high-query tables
- BIGINT for InvestigationResult.Id (high volume)
- Proper foreign key relationships with cascade rules
- Audit field automation in SaveChangesAsync

### **Query Optimizations**
- Repository pattern with IQueryable support
- Include() for navigation properties
- AsNoTracking() for read-only queries
- Pagination support for large result sets

## Known Issues & Dependencies

### **Technical Debt** 
✅ **RESOLVED**: Major technical debt items addressed in Phase 1:
- InvestigationManager now uses database persistence
- Frontend updated for new API structure  
- InvestigatorResult.cs (legacy) replaced with InvestigationResult.cs persistence model
- Dependency injection consistency achieved system-wide

### **Minor Remaining Items**
- Exception handling could be enhanced with structured logging (Phase 2 candidate)
- Unit test coverage could be expanded (currently 1 backend test, 3 frontend tests)

**System Health Status:**
- ✅ Backend builds (Debug + Release)
- ✅ Backend tests pass (1/1) from `tests/backend/unit/`
- ✅ Frontend tests pass (3/3) from `tests/frontend/`
- ✅ **Unified test commands work perfectly** (`npm test`, `npm run test:backend`, `npm run test:frontend`)
- ✅ CI/CD pipeline updated for unified structure
- ✅ All major functionality preserved and enhanced
- ✅ Database persistence fully integrated
- ✅ **True professional test organization achieved**

**Project Structure Status:**
- ✅ **Source Code**: Clean `src/` directory (backend + frontend)
- ✅ **All Tests**: Unified `tests/` directory (backend + frontend + e2e)
- ✅ **Configuration**: Root-level unified configuration files
- ✅ **Zero Duplication**: No scattered test files or conflicting configurations

---
*Last Updated: August 7, 2025*
*Claude Session Context: **Phase 1.5.2 Business Logic Extraction - COMPLETED (8/8 Steps)***
*Next Phase: Ready for Phase 2: Enhanced Investigation Features or user direction*