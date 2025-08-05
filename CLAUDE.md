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

### **Phase 1: Data Persistence & CRUD** (80% Complete)
#### Completed:
- [x] **Entity Models**: InvestigatorType, InvestigatorInstance, InvestigationExecution, InvestigationResult
- [x] **Enhanced Business Models**: Invoice, Waybill with audit fields (CreatedAt, UpdatedAt, HasAnomalies, LastInvestigatedAt)
- [x] **Enums**: InvestigatorStatus, ExecutionStatus, ResultSeverity
- [x] **Database Context**: ApplicationDbContext with optimized configuration, indexes, and relationships
- [x] **EF Migration**: `AddInvestigationPersistence` migration created and ready
- [x] **Repository Pattern**: Generic repository + InvestigatorRepository with business logic
- [x] **User Secrets**: Connection string securely configured
- [x] **Seed Data**: InvestigatorType reference data (invoice, waybill types)

#### In Progress:
- [ ] **MySQL Installation**: Need to install MySQL in WSL environment
- [ ] **Migration Application**: Run `dotnet ef database update`

#### Next Steps:
- [ ] **API Controllers**: InvestigatorController, InvoiceController, WaybillController
- [ ] **DTOs**: Request/Response models for API endpoints
- [ ] **Service Layer**: Business logic services
- [ ] **Frontend Integration**: Update React components for new persistence layer

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

### **Backend Structure**
```
Backend/
├── Controllers/
│   └── InvestigationsController.cs          # Current API endpoints
├── Data/
│   └── ApplicationDbContext.cs              # EF DbContext with full configuration
├── Enums/
│   ├── InvestigatorStatus.cs               # Inactive, Stopped, Running, Failed
│   ├── ExecutionStatus.cs                  # Running, Completed, Failed, Cancelled
│   ├── ResultSeverity.cs                   # Info, Warning, Error, Anomaly, Critical
│   ├── InvoiceType.cs                      # Business enum
│   └── WaybillType.cs                      # Business enum
├── Models/
│   ├── InvestigatorType.cs                 # Reference data for investigator templates
│   ├── InvestigatorInstance.cs             # Persistent investigator configurations
│   ├── InvestigationExecution.cs           # Session tracking
│   ├── InvestigationResult.cs              # Findings and audit trail
│   ├── Invoice.cs                          # Enhanced with audit fields
│   ├── Waybill.cs                          # Enhanced with audit fields
│   └── InvestigatorResult.cs               # Legacy model (being replaced)
├── Repositories/
│   ├── IGenericRepository.cs               # Generic CRUD interface
│   ├── GenericRepository.cs                # Generic CRUD implementation
│   ├── IInvestigatorRepository.cs          # Investigator-specific operations
│   └── InvestigatorRepository.cs           # Business logic repository
├── Services/
│   ├── Investigator.cs                     # Abstract base class for runtime behavior
│   ├── InvoiceInvestigator.cs              # Invoice anomaly detection logic
│   ├── WaybillInvestigator.cs              # Waybill delay detection logic
│   ├── IInvestigatorFactory.cs             # Factory interface
│   ├── InvestigatorFactory.cs              # DI-based factory implementation
│   └── InvestigationManager.cs             # Coordinator (needs refactoring for persistence)
├── Migrations/
│   ├── 20250727133003_InitialCreate.*      # Original migration
│   └── 20250804194819_AddInvestigationPersistence.*  # New persistence structure
└── Program.cs                              # Startup configuration with user secrets
```

### **Frontend Structure**
```
frontend/
├── src/
│   ├── App.tsx                             # Main application component
│   ├── Dashboard.tsx                       # Investigation management dashboard
│   ├── lib/
│   │   └── axios.ts                        # HTTP client configuration
│   └── types/
│       └── api.ts                          # TypeScript API interfaces
├── tests/
│   ├── unit/                               # Jest unit tests
│   └── integration/                        # React Testing Library tests
├── cypress/                                # E2E tests
└── package.json                            # Dependencies and scripts
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

## Testing Strategy

### **Backend Testing**
- **Framework**: XUnit with InMemory database
- **Coverage**: InvestigationManagerTests.cs (1 test passing)
- **Strategy**: Repository pattern enables easy mocking

### **Frontend Testing**
- **Unit Tests**: Jest (3 tests passing)
- **Integration**: React Testing Library
- **E2E**: Cypress with smoke tests
- **Files**: `App.spec.tsx`, `Dashboard.spec.tsx`, `axios.spec.ts`

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

### **Invoice Investigator**
```csharp
// Detects anomalies in invoices
var anomalies = db.Invoices
    .Where(i => i.TotalAmount < 0 ||                    // Negative amounts
                i.TotalTax > i.TotalAmount * 0.5m ||     // Tax > 50% of amount
                i.IssueDate > DateTime.UtcNow)           // Future dates
    .ToList();
```

### **Waybill Investigator**
```csharp
// Detects late waybills
var cutoff = DateTime.UtcNow.AddDays(-7);
var late = db.Waybills
    .Where(w => w.GoodsIssueDate < cutoff)              // Older than 7 days
    .ToList();
```

## Next Session Priorities

### **Immediate (Phase 1 Completion)**
1. **Install MySQL**: `sudo apt install mysql-server -y`
2. **Apply Migration**: `dotnet ef database update`
3. **Create API Controllers**: Full CRUD for all entities
4. **Create DTOs**: Request/response models
5. **Update InvestigationManager**: Use persistence instead of in-memory

### **Critical Dependencies**
- MySQL must be installed and running
- Migration must be applied successfully
- Repository pattern integration with existing services

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

### **Current Blockers**
1. **MySQL Installation**: Required before migration can be applied
2. **InvestigationManager Refactoring**: Currently uses in-memory storage, needs persistence integration

### **Technical Debt**
- InvestigatorResult.cs (legacy) vs InvestigationResult.cs (new) - need to consolidate
- Frontend needs updating for new persistence API endpoints
- Exception handling could be enhanced with structured logging

## Success Metrics

### **Phase 1 Complete When:**
- [ ] MySQL installed and migration applied
- [ ] All CRUD operations work through API
- [ ] Frontend integrated with persistent storage
- [ ] All existing functionality preserved
- [ ] Tests passing with database integration

---
*Last Updated: August 4, 2025*
*Claude Session Context: Phase 1 implementation - 80% complete*