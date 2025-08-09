# ea_Tracker - Claude Development Context

## Project Overview

**ea_Tracker** is an investigation management system for detecting anomalies in business entities (Invoices and Waybills) with real-time SignalR communication.

### Core Features
- **Invoice Investigation**: Detect negative amounts, excessive tax ratios, future dates
- **Waybill Investigation**: Identify late shipments and delivery issues  
- **Real-time Monitoring**: Live dashboard with SignalR WebSocket updates
- **Dynamic Management**: Create, configure, and control investigators on-demand

## Architecture

### **Backend (.NET 8.0)**
- ASP.NET Core 8.0 + Entity Framework Core 8.0.1
- MySQL with Pomelo provider
- SignalR WebSocket hub with auto-reconnection
- Repository pattern + dependency injection
- User Secrets (dev), environment variables (prod)

### **Frontend (React TypeScript)**
- React 18 with TypeScript
- SignalR client with connection management
- Axios for REST API
- Zero polling - all updates via WebSocket events

### **Database Schema**
```
InvestigatorType → InvestigatorInstance → InvestigationExecution → InvestigationResult
Invoice/Waybill (Business entities with audit fields)
```

### **SignalR Events**
- `InvestigationStarted`, `InvestigationCompleted`, `NewResultAdded`, `StatusChanged`
- Multi-user broadcasting with auto-reconnection

## Implementation Status

### **Completed** ✅
- **Data Layer**: Entity models, EF migrations, MySQL database
- **Service Layer**: Repository pattern, business logic isolation, SOLID principles
- **Real-Time System**: Complete SignalR implementation with zero polling
- **Frontend**: React TypeScript with live WebSocket updates
- **API**: Full REST API with SignalR hub integration

### **Current TODO List** 🚧
**Priority**: CRITICAL - Comprehensive testing implementation to match production-ready SignalR system

#### **Phase 1: Legacy Code Cleanup** (Low-hanging fruit, 2-4 hours)
- [ ] **1.1 Analysis**: Analyze InvestigatorResult usage across codebase - map all references to understand refactoring scope
- [ ] **1.2 Investigator Refactor**: Update Investigator.cs Report Action to use InvestigationResult instead of InvestigatorResult
- [ ] **1.3 Manager Refactor**: Refactor InvestigationManager.SaveResultAsync to eliminate InvestigatorResult conversion step
- [ ] **1.4 DTO Refactor**: Remove InvestigatorResultDto and update GetResultsAsync to return InvestigatorResultDto mapped from InvestigationResult
- [ ] **1.5 Controller Update**: Update InvestigationsController.Results endpoint to use refactored DTO mapping
- [ ] **1.6 File Cleanup**: Delete InvestigatorResult.cs model file and verify no compilation errors
- [ ] **1.7 Validation**: Run existing tests to ensure Phase 1 refactoring doesn't break current functionality

#### **Phase 2: Comprehensive Backend Testing** (Critical Priority, 1-2 days)

##### **Setup & Dependencies**
- [ ] **2.1 SignalR Test Package**: Add Microsoft.AspNetCore.SignalR.Client test package to ea_Tracker.Tests.csproj for SignalR testing
- [ ] **2.2 Mocking Package**: Add Moq package to ea_Tracker.Tests.csproj for mocking dependencies in unit tests

##### **InvestigationManager Testing** (Core orchestration)
- [ ] **2.3 Manager Success**: Create InvestigationManagerTests - test StartInvestigatorAsync with valid investigator (success case)
- [ ] **2.4 Manager Failure**: Create InvestigationManagerTests - test StartInvestigatorAsync with inactive investigator (failure case)
- [ ] **2.5 Stop Success**: Create InvestigationManagerTests - test StopInvestigatorAsync with running investigator (success case)
- [ ] **2.6 Stop Failure**: Create InvestigationManagerTests - test StopInvestigatorAsync with non-running investigator (failure case)
- [ ] **2.7 SignalR Integration**: Create InvestigationManagerTests - test SignalR notification calls are made during start/stop operations
- [ ] **2.8 Create Validation**: Create InvestigationManagerTests - test CreateInvestigatorAsync creates database record with correct ExternalId
- [ ] **2.9 Delete Cascade**: Create InvestigationManagerTests - test DeleteInvestigatorAsync removes all related data (cascade delete)

##### **Business Logic Testing** (Investigation algorithms)
- [ ] **2.10 Invoice Negatives**: Create InvoiceInvestigatorTests - test OnStart method processes invoices and detects negative amounts
- [ ] **2.11 Invoice Tax**: Create InvoiceInvestigatorTests - test OnStart method detects excessive tax ratios with configurable threshold
- [ ] **2.12 Invoice Dates**: Create InvoiceInvestigatorTests - test OnStart method detects future dates beyond configured days
- [ ] **2.13 Invoice SignalR**: Create InvoiceInvestigatorTests - test RecordResult method triggers SignalR NewResultAdded event
- [ ] **2.14 Waybill Overdue**: Create WaybillInvestigatorTests - test OnStart method detects overdue deliveries based on DueDate
- [ ] **2.15 Waybill Expiring**: Create WaybillInvestigatorTests - test OnStart method detects expiring soon deliveries within configured hours
- [ ] **2.16 Waybill Legacy**: Create WaybillInvestigatorTests - test OnStart method identifies legacy waybills beyond cutoff days
- [ ] **2.17 Waybill SignalR**: Create WaybillInvestigatorTests - test RecordResult method triggers SignalR NewResultAdded event

##### **SignalR System Testing** (Real-time communication)
- [ ] **2.18 Hub Connection**: Create InvestigationHubTests - test hub accepts client connections without errors
- [ ] **2.19 Start Event**: Create InvestigationNotificationServiceTests - test InvestigationStartedAsync broadcasts to all clients
- [ ] **2.20 Complete Event**: Create InvestigationNotificationServiceTests - test InvestigationCompletedAsync includes resultCount in payload
- [ ] **2.21 Result Event**: Create InvestigationNotificationServiceTests - test NewResultAddedAsync broadcasts InvestigationResult object
- [ ] **2.22 Status Event**: Create InvestigationNotificationServiceTests - test StatusChangedAsync broadcasts status string correctly

##### **Integration Testing** (End-to-end backend)
- [ ] **2.23 E2E Flow**: Create end-to-end integration test - start investigator and verify SignalR events are fired in correct sequence
- [ ] **2.24 ID Sync**: Create end-to-end integration test - verify ExternalId synchronization between database and SignalR events

#### **Phase 3: Frontend Testing Implementation** (Frontend validation, 1 day)

##### **Setup & Infrastructure**
- [ ] **3.1 Frontend Setup**: Update frontend test setup to include @microsoft/signalr mock utilities

##### **SignalR Service Testing** (Connection management)
- [ ] **3.2 Connection Test**: Create SignalRService.test.ts - test connection establishment and state management
- [ ] **3.3 Reconnection Test**: Create SignalRService.test.ts - test auto-reconnection logic with simulated network failure
- [ ] **3.4 Event Handlers**: Create SignalRService.test.ts - test event handler registration and cleanup

##### **Dashboard Component Testing** (UI integration)
- [ ] **3.5 Polling Replacement**: Update Dashboard.spec.tsx - replace polling test with SignalR event simulation
- [ ] **3.6 Start Event UI**: Update Dashboard.spec.tsx - test InvestigationStarted event updates investigator status
- [ ] **3.7 Complete Event UI**: Update Dashboard.spec.tsx - test InvestigationCompleted event refreshes results and status
- [ ] **3.8 Result Event UI**: Update Dashboard.spec.tsx - test NewResultAdded event increments result count optimistically
- [ ] **3.9 Connection Status**: Update Dashboard.spec.tsx - test connection status indicator changes (Connected/Connecting/Disconnected)

##### **End-to-End Testing** (Full workflow validation)
- [ ] **3.10 E2E Workflow**: Create Cypress E2E test - full investigation workflow with real SignalR connection
- [ ] **3.11 Multi-Tab Test**: Create Cypress E2E test - multi-tab SignalR event broadcasting verification

#### **Validation & Documentation** (Quality assurance)
- [ ] **4.1 Coverage Validation**: Run complete test suite and ensure 90%+ code coverage for all new tests
- [ ] **4.2 Documentation Update**: Update CLAUDE.md test status section to reflect completed testing implementation

### **Implementation Notes**
- **Total Effort**: ~3-4 days of focused development
- **Risk Level**: Low (systematic approach with validation at each step)
- **Dependencies**: Phase 1 must complete before Phase 2, but Phase 2 and 3 can run in parallel
- **Success Criteria**: All tests passing, >90% coverage, zero legacy InvestigatorResult references

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

## Testing Status ⚠️

### **Current Status**
- **Backend**: 2/2 tests passing (database + business logic)
- **Frontend**: 2/3 tests passing (1 outdated polling test)
- **SignalR**: 0% test coverage - needs complete implementation

### **Test Commands**
```bash
npm test                 # All tests
npm run test:backend     # Backend only
npm run test:frontend    # Frontend only
npm run test:e2e         # End-to-end
```

**Critical Gap**: Test suite needs major update to cover SignalR system

## Configuration

### **Development**
- User Secrets for connection strings
- SignalR CORS with AllowCredentials
- MySQL connection: `Server=localhost;Database=ea_tracker_db;Uid=root;Pwd=Hea!90569;`

### **Production Considerations**
- Environment variables for secrets
- SignalR Redis backplane for scaling
- Authentication/authorization (future)
- Rate limiting and monitoring



### **SignalR Integration Points**
- `Investigator.Start()` → `InvestigationStarted`, `StatusChanged` events
- `Investigator.RecordResult()` → `NewResultAdded` event  
- `InvestigationManager.StopInvestigatorAsync()` → `InvestigationCompleted` event

## Performance Notes
- Database indexes for real-time queries
- Fire-and-forget SignalR broadcasting
- Connection pooling and auto-reconnection
- Optimized JSON serialization

## System Status

### **Current Health** ✅
- Backend: .NET 8.0 builds successfully
- Frontend: React + SignalR working with live updates  
- Database: MySQL with all migrations applied
- Real-Time: SignalR fully operational with multi-user support
- Zero polling: All updates via WebSocket events

### **Technical Debt** ⚠️
- 🔴 **Critical**: Test suite needs SignalR coverage update
- 🟡 Authentication not implemented (future)
- 🟡 Monitoring/logging could be enhanced

---
*Updated: Aug 2025 - Real-time SignalR system operational*
*Next: Update test suite for SignalR coverage*