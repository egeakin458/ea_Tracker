# ea_Tracker - Claude Development Context

## Project Overview

**ea_Tracker** is a sophisticated investigation management system designed to automatically detect anomalies and issues in business entities (Invoices and Waybills). The system features a dynamic investigator architecture that can be configured, started, stopped, and monitored in **real-time** with professional WebSocket communication.

### Core Purpose
- **Invoice Investigation**: Detect anomalous invoices (negative amounts, excessive tax ratios, future dates)
- **Waybill Investigation**: Identify late shipments and delivery issues
- **Dynamic Management**: Create, configure, and control investigators on-demand
- **Real-time Monitoring**: Live dashboard with instant execution tracking and results via SignalR
- **Audit Compliance**: Complete investigation history and audit trails

## Architecture Overview

### **Backend (.NET 8.0 Web API)**
- **Framework**: ASP.NET Core 8.0 with Entity Framework Core 8.0.1
- **Database**: MySQL with Pomelo provider
- **Real-Time Communication**: SignalR WebSocket hub with automatic reconnection
- **Event-Driven Architecture**: Investigation lifecycle broadcasts to all connected clients
- **Pattern**: Repository pattern with dependency injection and business logic isolation
- **Security**: User Secrets for development, environment variables for production
- **Middleware**: Global exception handling, SignalR-compatible CORS with credential support
- **Health Monitoring**: Comprehensive health checks with database validation

### **Frontend (React TypeScript)**
- **Framework**: React 18 with TypeScript
- **Real-Time Updates**: SignalR client with connection state management and auto-reconnect
- **HTTP Client**: Axios for REST API communication
- **Live UI**: Event-driven status and result updates with zero polling
- **Connection Indicators**: Visual feedback for WebSocket connection health
- **Testing**: Jest for unit tests, Cypress for E2E testing
- **UI**: Custom CSS with responsive design and real-time status indicators

### **Database Design (3NF Compliant)**
```
InvestigatorType (Reference Data)
├── InvestigatorInstance (Runtime Configurations) 
    ├── InvestigationExecution (Session Tracking with Real-time Status)
        └── InvestigationResult (Findings & Audit Trail with Live Updates)

Invoice/Waybill (Business Entities with Audit Fields & Real-time Investigation Status)
```

## Real-Time Communication Architecture ✨

### **SignalR Event Flow**
```
User Action (Start/Stop Investigation)
    ↓
InvestigationManager (Database Operations)
    ↓
IInvestigationNotificationService (Event Broadcasting)
    ↓
InvestigationHub (SignalR WebSocket Hub)
    ↓
All Connected Clients (Multi-User Support)
    ↓
SignalRService.ts (Connection Management)
    ↓
Dashboard.tsx Event Handlers (Live UI Updates)
    ↓
Real-Time User Interface Updates
```

### **SignalR Event Types & Payloads**
- **InvestigationStarted**: `{ investigatorId: GUID, timestamp: DateTime }`
- **InvestigationCompleted**: `{ investigatorId: GUID, resultCount: number, timestamp: DateTime }`
- **NewResultAdded**: `{ investigatorId: GUID, result: InvestigationResult }`
- **StatusChanged**: `{ investigatorId: GUID, newStatus: string }`

### **Connection Management Features**
- **Auto-Reconnection**: Automatic retry with exponential backoff
- **Connection Status**: Visual indicators (Connected/Connecting/Disconnected)
- **Error Recovery**: Robust failure handling with graceful degradation
- **Multi-User Broadcasting**: All connected clients receive simultaneous updates
- **Database Synchronization**: ExternalId property ensures persistent ID alignment

## Current Status & Phase Progress

### **Phase 1: Data Persistence & CRUD** ✅ **COMPLETED**
#### Completed:
- [x] **Entity Models**: InvestigatorType, InvestigatorInstance, InvestigationExecution, InvestigationResult
- [x] **Enhanced Business Models**: Invoice, Waybill with audit fields (CreatedAt, UpdatedAt, HasAnomalies, LastInvestigatedAt)
- [x] **Enums**: InvestigatorStatus, ExecutionStatus, ResultSeverity, InvoiceType, WaybillType
- [x] **Database Context**: ApplicationDbContext with optimized configuration, indexes, and relationships
- [x] **EF Migrations**: Multiple migrations applied (InitialCreate, AddInvestigationPersistence, AddWaybillDueDate, AddWaybillDueDateIndex)
- [x] **Repository Pattern**: Generic repository + InvestigatorRepository with specialized business logic
- [x] **User Secrets**: Connection string securely configured
- [x] **InvestigationManager**: Database persistence with full CRUD operations
- [x] **API Controllers**: InvestigationsController, InvestigatorController, InvoicesController, WaybillsController
- [x] **DTOs**: Comprehensive DTO mapping for API responses
- [x] **Frontend Integration**: React components fully integrated with backend APIs
- [x] **Dependency Injection**: Complete system-wide consistency with scoped services
- [x] **Project Restructuring**: Professional directory structure (src/, tests/, docs/)
- [x] **Health Checks**: Comprehensive health monitoring with database validation

### **Phase 1.5: SOLID Principles Refactoring** ✅ **COMPLETED**
**Achievement: Comprehensive service layer abstraction with zero breaking changes**

#### **Phase 1.5.1: Service Layer Abstractions** ✅ **COMPLETED**
- [x] **IInvestigationManager Interface**: Created interface to fix DIP violation
- [x] **IInvoiceService Interface**: Business operations abstraction
- [x] **IWaybillService Interface**: Business operations abstraction
- [x] **InvoiceService Implementation**: Complete business logic moved from controller
- [x] **WaybillService Implementation**: Enhanced with overdue/expiring algorithms
- [x] **Dependency Injection Update**: All service interfaces registered
- [x] **Controller Refactoring**: All controllers use interfaces - API compatibility preserved

#### **Phase 1.5.2: Business Logic Extraction** ✅ **COMPLETED**
- [x] **IInvestigationLogic<T> Interface**: Pure business logic abstraction
- [x] **InvoiceAnomalyLogic Class**: Algorithm isolation from infrastructure
- [x] **WaybillDeliveryLogic Class**: Enhanced overdue/expiring algorithms
- [x] **IInvestigationConfiguration Interface**: Externalize business thresholds
- [x] **InvestigatorFactory Enhancement**: Registration-based strategy pattern
- [x] **InvestigatorRegistry**: Type registration system for investigator creation
- [x] **Investigator Refactoring**: Use injected business logic components

### **Phase 2: Real-Time Communication System** ✅ **COMPLETED**
**Achievement: Complete elimination of polling with professional SignalR WebSocket implementation**

#### **Phase 2.1: SignalR Infrastructure** ✅ **COMPLETED**
- [x] **InvestigationHub.cs**: SignalR hub for server-to-client broadcasting
- [x] **IInvestigationNotificationService**: Interface for real-time event broadcasting
- [x] **InvestigationNotificationService**: Hub context implementation with 4 event types
- [x] **Program.cs Integration**: SignalR services registration and hub mapping
- [x] **CORS Enhancement**: FrontendDev policy with AllowCredentials for SignalR negotiation
- [x] **Database Synchronization**: ExternalId property for persistent ID alignment between database and UI

#### **Phase 2.2: Backend Real-Time Integration** ✅ **COMPLETED**  
- [x] **InvestigationManager Enhancement**: Inject and use IInvestigationNotificationService
- [x] **Investigation Lifecycle Events**: Start/completion notifications with result counts
- [x] **Investigator Base Class Integration**: Real-time event publishing in Start(), Stop(), RecordResult()
- [x] **ExternalId Synchronization**: Ensure SignalR events use persistent database IDs
- [x] **Async Event Broadcasting**: Fire-and-forget pattern for non-blocking notifications

#### **Phase 2.3: Frontend Real-Time Client** ✅ **COMPLETED**
- [x] **SignalRService.ts**: Comprehensive connection management with logging and error handling
- [x] **Auto-Reconnection**: Built-in automatic reconnection with connection state tracking
- [x] **Dashboard.tsx Integration**: SignalR connection initialization in useEffect
- [x] **Event Handler Implementation**: 4 real-time event listeners for investigation lifecycle
- [x] **Connection Status UI**: Visual indicators in Dashboard header (Connected/Connecting/Disconnected)
- [x] **Optimistic UI Updates**: Live result count increments without full page reloads
- [x] **Error Recovery**: Robust connection failure handling and retry logic

#### **Phase 2.4: System Integration & Testing** ✅ **COMPLETED**
- [x] **Polling Elimination**: Complete removal of all 7-second polling intervals
- [x] **Manual Refresh Removal**: Start/stop actions no longer trigger manual API refreshes
- [x] **Multi-User Testing**: Simultaneous investigation notifications validated across browser tabs
- [x] **Network Recovery Testing**: SignalR reconnection after network interruption verified
- [x] **Lifecycle Event Testing**: Full investigation start→running→complete flow tested
- [x] **Connection Management Testing**: Comprehensive logging and error state validation
- [x] **Production Readiness**: Robust error handling and connection state management implemented

## Key Files & Structure

### **Unified Project Structure** ✨
```
ea_Tracker/
├── src/                                    # SOURCE CODE ONLY
│   ├── backend/                            # .NET 8.0 Web API with SignalR
│   │   ├── Controllers/
│   │   │   ├── InvestigationsController.cs # Investigation management API with real-time events
│   │   │   ├── InvestigatorController.cs   # Investigator CRUD operations
│   │   │   ├── InvoicesController.cs       # Invoice management API
│   │   │   └── WaybillsController.cs       # Waybill management API
│   │   ├── Data/
│   │   │   └── ApplicationDbContext.cs     # EF DbContext with full configuration and health checks
│   │   ├── Enums/
│   │   │   ├── InvestigatorStatus.cs      # Inactive, Stopped, Running, Failed
│   │   │   ├── ExecutionStatus.cs         # Running, Completed, Failed, Cancelled
│   │   │   ├── ResultSeverity.cs          # Info, Warning, Error, Anomaly, Critical
│   │   │   ├── InvoiceType.cs             # Business enum
│   │   │   └── WaybillType.cs             # Business enum
│   │   ├── Hubs/                           # ✨ NEW - SignalR Real-Time Communication
│   │   │   └── InvestigationHub.cs        # SignalR hub for broadcasting investigation events
│   │   ├── Middleware/
│   │   │   └── ExceptionHandlingMiddleware.cs # Global exception handling
│   │   ├── Models/
│   │   │   ├── InvestigatorType.cs        # Reference data for investigator templates
│   │   │   ├── InvestigatorInstance.cs    # Persistent investigator configurations
│   │   │   ├── InvestigationExecution.cs  # Session tracking with real-time status
│   │   │   ├── InvestigationResult.cs     # Findings and audit trail with live updates
│   │   │   ├── InvestigatorResult.cs      # Legacy model for investigator results
│   │   │   ├── Invoice.cs                 # Enhanced with audit fields
│   │   │   ├── Waybill.cs                 # Enhanced with audit fields and DueDate
│   │   │   └── Dtos/                      # Complete DTO mapping system
│   │   │       ├── InvestigatorDtos.cs
│   │   │       ├── InvestigatorInstanceDtos.cs
│   │   │       └── InvoiceWaybillDtos.cs
│   │   ├── Repositories/
│   │   │   ├── IGenericRepository.cs      # Generic CRUD interface
│   │   │   ├── GenericRepository.cs       # Generic CRUD implementation
│   │   │   ├── IInvestigatorRepository.cs # Investigator-specific operations
│   │   │   └── InvestigatorRepository.cs  # Business logic repository with summary operations
│   │   ├── Services/
│   │   │   ├── Investigator.cs            # Abstract base class with SignalR integration
│   │   │   ├── InvoiceInvestigator.cs     # Invoice anomaly detection logic with real-time updates
│   │   │   ├── WaybillInvestigator.cs     # Enhanced waybill logic with real-time notifications
│   │   │   ├── IInvestigatorFactory.cs    # Factory interface
│   │   │   ├── InvestigatorFactory.cs     # DI-based factory implementation
│   │   │   ├── IInvestigatorRegistry.cs   # Registry interface for investigator types
│   │   │   ├── InvestigatorRegistry.cs    # Type registration system
│   │   │   ├── IInvestigationManager.cs   # Investigation coordination interface
│   │   │   ├── InvestigationManager.cs    # Database-integrated coordinator with SignalR
│   │   │   ├── IInvestigationNotificationService.cs # ✨ NEW - Real-time notification interface
│   │   │   ├── InvestigationNotificationService.cs  # ✨ NEW - SignalR hub context implementation
│   │   │   ├── IInvoiceService.cs         # Invoice business operations interface
│   │   │   ├── InvoiceService.cs          # Invoice business logic implementation
│   │   │   ├── IWaybillService.cs         # Waybill business operations interface
│   │   │   ├── WaybillService.cs          # Waybill business logic implementation
│   │   │   ├── IInvestigationLogic.cs     # Pure business logic abstraction
│   │   │   ├── InvoiceAnomalyLogic.cs     # Algorithm isolation from infrastructure
│   │   │   ├── WaybillDeliveryLogic.cs    # Enhanced overdue/expiring algorithms
│   │   │   ├── IInvestigationConfiguration.cs # Business thresholds interface
│   │   │   ├── InvestigationConfiguration.cs  # Configuration implementation
│   │   │   └── InvestigationHostedService.cs  # Background service for investigations
│   │   └── Program.cs                     # Startup configuration with SignalR, health checks, and user secrets
│   └── frontend/                          # React TypeScript SPA with Real-Time Updates
│       ├── src/
│       │   ├── App.tsx                    # Main application component
│       │   ├── Dashboard.tsx              # Investigation management dashboard with live updates
│       │   ├── lib/
│       │   │   ├── axios.ts               # HTTP client configuration
│       │   │   └── SignalRService.ts      # ✨ NEW - SignalR connection management with auto-reconnect
│       │   └── types/
│       │       └── api.ts                 # TypeScript API interfaces
│       ├── public/                        # Static assets
│       └── package.json                   # Frontend dependencies with @microsoft/signalr
├── tests/                                 # ALL TESTS UNIFIED HERE (⚠️ NEEDS MAJOR UPDATE)
│   ├── backend/
│   │   └── unit/                          # Backend unit tests (xUnit)
│   │       ├── InvestigationManagerTests.cs # Database creation tests (✅ Current)
│   │       ├── BusinessLogicTests.cs      # Business logic validation (✅ Current)
│   │       └── ea_Tracker.Tests.csproj    # Test project configuration
│   ├── frontend/
│   │   ├── unit/                          # Frontend unit tests (Jest + RTL)
│   │   │   ├── App.spec.tsx               # Basic component tests (✅ Current)
│   │   │   └── axios.spec.ts              # API client tests (✅ Current)
│   │   ├── integration/                   # Frontend integration tests
│   │   │   └── Dashboard.spec.tsx         # ⚠️ OUTDATED - Tests old polling system
│   │   └── e2e/                           # End-to-end tests (Cypress)
│   │       ├── smoke.cy.js                # Critical workflow tests
│   │       └── fixtures/                  # Test data
│   │           └── investigators.json
│   └── setup.ts                           # Test setup configuration
├── package.json                           # Root-level unified configuration with test scripts
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
1. **Clone and Navigate**: `cd src/backend`
2. **Install EF Tools**: `dotnet tool install --global dotnet-ef`
3. **User Secrets**: Already configured with connection string
4. **Install MySQL**: `sudo apt install mysql-server -y`
5. **Apply Migrations**: `dotnet ef database update`
6. **Run**: `dotnet run` (Starts on http://localhost:5050)

### **Frontend Setup**
1. **Navigate**: `cd src/frontend`
2. **Install Dependencies**: `npm install` (includes @microsoft/signalr)
3. **Run**: `npm start` (Starts on http://localhost:3000)
4. **Test**: `npm test`

### **Real-Time Communication Setup**
- **SignalR Hub Endpoint**: `/hubs/investigations`
- **Connection Status**: Displayed in Dashboard header
- **Auto-Reconnect**: Enabled by default with exponential backoff
- **Multi-User Support**: All connected clients receive simultaneous updates

## Database Schema

### **Investigation System Tables**
| Table | Purpose | Key Fields | SignalR Integration |
|-------|---------|------------|---------------------|
| `InvestigatorTypes` | Reference data for investigator templates | `Id`, `Code`, `DisplayName`, `DefaultConfiguration` | Template data for creation |
| `InvestigatorInstances` | Persistent investigator configurations | `Id` (GUID), `TypeId`, `IsActive`, `CustomConfiguration` | **ExternalId used in SignalR events** |
| `InvestigationExecutions` | Session tracking with real-time status | `Id`, `InvestigatorId`, `Status`, `StartedAt`, `CompletedAt`, `ResultCount` | **Live status updates via SignalR** |
| `InvestigationResults` | High-volume findings storage with live updates | `Id` (BIGINT), `ExecutionId`, `Severity`, `EntityType`, `EntityId` | **Real-time result notifications** |

### **Business Tables (Enhanced)**
| Table | Purpose | New Fields | Real-Time Features |
|-------|---------|------------|-------------------|
| `Invoices` | Invoice business data | + `CreatedAt`, `UpdatedAt`, `HasAnomalies`, `LastInvestigatedAt` | Live anomaly detection results |
| `Waybills` | Waybill business data | + `CreatedAt`, `UpdatedAt`, `HasAnomalies`, `LastInvestigatedAt`, `DueDate` | Real-time delivery issue notifications |

### **Performance Indexes**
- `IX_InvestigatorInstance_Type_Active`: Query active investigators by type
- `IX_Result_Execution_Time`: Time-based result queries for real-time display
- `IX_Result_Entity`: Link results to business entities for live updates
- `IX_Result_Severity`: Filter by severity level for priority notifications
- `IX_Waybill_DueDate`: Efficient overdue waybill detection

## API Endpoints

### **Current Endpoints** (InvestigationsController)
| Method | Endpoint | Purpose | SignalR Events Triggered |
|--------|----------|---------|-------------------------|
| `GET` | `/api/investigations` | Get all investigators with status | None |
| `POST` | `/api/investigations/{id}/start` | Start specific investigator | `InvestigationStarted`, `StatusChanged` |
| `POST` | `/api/investigations/{id}/stop` | Stop specific investigator | `InvestigationCompleted`, `StatusChanged` |
| `GET` | `/api/investigations/{id}/results` | Get investigator results | None |
| `POST` | `/api/investigations/invoice` | Create invoice investigator | None |
| `POST` | `/api/investigations/waybill` | Create waybill investigator | None |
| `DELETE` | `/api/investigations/{id}` | Delete investigator with confirmation | None |

### **Additional API Endpoints**
| Controller | Endpoints | Purpose |
|------------|-----------|---------|
| `InvestigatorController` | CRUD operations | Investigator instance management |
| `InvoicesController` | Invoice CRUD | Invoice business entity operations |
| `WaybillsController` | Waybill CRUD | Waybill business entity operations |

### **Health Check Endpoints**
| Endpoint | Purpose | Response Format |
|----------|---------|----------------|
| `/healthz` | Comprehensive health status | JSON with database connectivity and service status |

## Unified Testing Strategy ⚠️ **CRITICAL UPDATE NEEDED**

The project implements a **unified testing approach** with all tests in the `tests/` directory, but **tests are significantly behind the current real-time system**.

### **Test Organization**
- **All Tests**: Centralized in `tests/` directory (no scattered locations)
- **Clear Separation**: Source code (`src/`) vs. Tests (`tests/`)
- **Unified Commands**: Run all tests from project root
- **Zero Duplication**: Single source of truth for test configuration

### **Backend Testing**
- **Framework**: XUnit with Entity Framework InMemory
- **Location**: `tests/backend/unit/`
- **Current Coverage**: 
  - ✅ `InvestigationManagerTests.cs` - Database creation tests (current)
  - ✅ `BusinessLogicTests.cs` - Business logic validation (current)
  - ❌ **MISSING**: SignalR hub testing
  - ❌ **MISSING**: Real-time notification service testing
  - ❌ **MISSING**: ExternalId synchronization testing
- **Command**: `npm run test:backend`

### **Frontend Testing**
- **Frameworks**: Jest + React Testing Library, Cypress for E2E
- **Location**: `tests/frontend/`
- **Current Coverage**:
  - ✅ `App.spec.tsx` - Basic component render (current)
  - ✅ `axios.spec.ts` - API client tests (current)
  - ⚠️ `Dashboard.spec.tsx` - **OUTDATED** - Tests old polling system, not SignalR
  - ❌ **MISSING**: SignalR connection testing
  - ❌ **MISSING**: Real-time event handler testing
  - ❌ **MISSING**: Connection status indicator testing
  - ❌ **MISSING**: Optimistic UI update testing
- **Command**: `npm run test:frontend`

### **Unified Test Commands**
```bash
# From project root
npm test -- --watchAll=false     # All tests (backend + frontend)
npm run test:backend             # Backend unit tests only
npm run test:frontend            # Frontend unit tests only
npm run test:e2e                 # End-to-end tests
npm run test:watch               # Watch mode for development
```

### **Test Status - CRITICAL GAPS IDENTIFIED**
- ✅ **Backend Core**: 2/2 tests passing (database and business logic)
- ⚠️ **Frontend Core**: 2/3 tests passing (1 test outdated for old polling system)
- ❌ **SignalR System**: 0% test coverage for entire real-time architecture
- ❌ **Real-Time Events**: No tests for 4 SignalR event types
- ❌ **Connection Management**: No tests for auto-reconnect and error handling
- ❌ **Integration**: No tests for end-to-end SignalR workflow

**Test System Status**: 
- 🔴 **CRITICAL**: Tests do not reflect current SignalR real-time system
- 🔴 **MISSING**: Entire real-time communication system lacks test coverage
- 🔴 **OUTDATED**: Frontend integration test still assumes polling system

## Security & Configuration

### **Development Security**
- **User Secrets**: Connection strings stored securely
- **Location**: `~/.microsoft/usersecrets/8949b5c3-0a11-436e-9acc-bfce16a1dda2/secrets.json`
- **Connection**: `Server=localhost;Database=ea_tracker_db;Uid=root;Pwd=Hea!90569;`
- **SignalR CORS**: Configured with AllowCredentials for secure WebSocket negotiation

### **Production Considerations**
- Environment variables for connection strings
- SignalR scaling with Redis backplane for multi-instance deployments
- WebSocket security with authentication tokens
- Rate limiting for SignalR connections
- Proper authentication/authorization (Future Phase)
- Monitoring and logging for real-time events

## Current Investigation Logic with Real-Time Integration

### **WaybillInvestigator with SignalR**
```csharp
/// <summary>
/// Begins waybill investigation operations using pure business logic with real-time notifications.
/// Separates data access from business rule evaluation and broadcasts live updates.
/// </summary>
protected override void OnStart()
{
    using var db = _dbFactory.CreateDbContext();
    
    // Data Access: Get all waybills from database
    var waybills = db.Waybills.ToList();
    
    // Business Logic: Evaluate waybills using pure business logic
    var results = _businessLogic.EvaluateWaybills(waybills, _configuration);
    
    // Result Recording: Process and record findings with live updates
    foreach (var result in results.Where(r => r.IsAnomaly))
    {
        var waybill = result.Entity;
        var reasonsText = string.Join(", ", result.Reasons);
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
        
        // RecordResult automatically triggers SignalR NewResultAdded event
        RecordResult(resultMessage, JsonSerializer.Serialize(resultPayload));
    }
    
    // Enhanced Statistics: Record comprehensive statistics with real-time completion
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

    RecordSpecializedSummaries(waybills);
}
```

### **InvoiceInvestigator with SignalR**
```csharp
/// <summary>
/// Begins invoice investigation operations using pure business logic with real-time notifications.
/// Separates data access from business rule evaluation and broadcasts live updates.
/// </summary>
protected override void OnStart()
{
    using var db = _dbFactory.CreateDbContext();
    
    // Data Access: Get all invoices from database
    var invoices = db.Invoices.ToList();
    
    // Business Logic: Evaluate invoices using pure business logic
    var results = _businessLogic.EvaluateInvoices(invoices, _configuration);
    
    // Result Recording: Process and record findings with live updates
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
        
        // RecordResult automatically triggers SignalR NewResultAdded event
        RecordResult(resultMessage, JsonSerializer.Serialize(resultPayload));
    }
    
    // Optional: Record statistics for monitoring with real-time completion
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

### **Real-Time Integration Points**
- **Investigator.Start()**: Triggers `InvestigationStarted` and `StatusChanged` events
- **Investigator.Stop()**: Triggers `StatusChanged` event
- **Investigator.RecordResult()**: Triggers `NewResultAdded` event for each finding
- **InvestigationManager.StopInvestigatorAsync()**: Triggers `InvestigationCompleted` event with final result count

## Performance Considerations

### **Database Optimizations**
- Strategic indexes for high-query tables and real-time lookups
- BIGINT for InvestigationResult.Id (high volume with real-time inserts)
- Proper foreign key relationships with cascade rules
- Audit field automation in SaveChangesAsync
- ExternalId synchronization for SignalR event consistency

### **Query Optimizations**
- Repository pattern with IQueryable support
- Include() for navigation properties in real-time queries
- AsNoTracking() for read-only queries
- Pagination support for large result sets
- Optimized result counting for live UI updates

### **Real-Time Performance**
- Fire-and-forget SignalR event broadcasting (async without await)
- Connection pooling for multiple simultaneous users
- Efficient JSON serialization for event payloads
- Auto-reconnection with exponential backoff to prevent connection storms

## System Health & Status

### **Current System Health**
- ✅ **Backend**: Builds successfully (Debug + Release)
- ✅ **Frontend**: Builds and runs with real-time updates
- ✅ **Database**: All migrations applied, indexes optimized
- ✅ **SignalR**: Fully operational with multi-user support tested
- ✅ **Real-Time Events**: All 4 event types broadcasting successfully
- ✅ **Connection Management**: Auto-reconnect and error recovery validated
- ✅ **Multi-User Support**: Simultaneous notifications working across browser tabs
- ✅ **Network Recovery**: SignalR reconnection after interruption verified
- ✅ **Zero Polling**: All 7-second intervals completely eliminated from system

### **Technical Dependencies - Current Versions**
- ✅ **Backend**: .NET 8.0 with built-in SignalR
- ✅ **Frontend**: @microsoft/signalr 8.0.5
- ✅ **Database**: MySQL 8.0 with Pomelo.EntityFrameworkCore.MySQL
- ✅ **Repository Pattern**: All service implementations using interfaces
- ✅ **Business Logic Isolation**: Pure algorithms separated from infrastructure
- ✅ **Configuration Externalization**: All business thresholds configurable

### **Known Issues & Dependencies**

#### **Resolved Technical Debt** ✅
- ✅ **Polling System**: Completely eliminated and replaced with SignalR
- ✅ **InvestigationManager**: Database persistence with real-time notifications
- ✅ **Frontend Integration**: Full SignalR client with connection management
- ✅ **Database Synchronization**: ExternalId ensures UI-database ID alignment
- ✅ **Service Layer**: Complete SOLID principles implementation
- ✅ **Business Logic**: Isolated algorithms with dependency injection

#### **Current Technical Debt** ⚠️
- 🔴 **Test Coverage**: Tests do not reflect SignalR real-time system (CRITICAL)
- 🟡 **Exception Handling**: Could be enhanced with structured logging
- 🟡 **Performance Monitoring**: Real-time event metrics could be added
- 🟡 **Authentication**: SignalR connections not authenticated (future enhancement)

**System Architecture Status:**
- ✅ **Real-Time Communication**: Professional SignalR implementation operational
- ✅ **Multi-User Support**: Simultaneous notifications working
- ✅ **Connection Resilience**: Auto-reconnect and error recovery implemented
- ✅ **UI/UX**: Live status indicators and optimistic updates working
- ✅ **Database Integration**: Complete persistence with real-time synchronization
- ✅ **Event-Driven Architecture**: Zero polling, all updates via WebSocket events

**Project Structure Status:**
- ✅ **Source Code**: Clean `src/` directory (backend + frontend)
- ✅ **All Tests**: Unified `tests/` directory structure
- ✅ **Configuration**: Root-level unified configuration files
- ✅ **SignalR Integration**: Complete end-to-end real-time communication system
- 🔴 **Test Relevance**: Test content does not match current SignalR architecture

---
*Last Updated: August 8, 2025*
*Claude Session Context: **Post-SignalR Implementation - Real-Time System Operational***
*Current Focus: System is fully functional with professional real-time communication. Test suite requires major update to reflect SignalR architecture.*
*Next Phase Recommendation: Update test suite to cover SignalR system, then proceed with UI/UX enhancements.*