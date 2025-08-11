# ea_Tracker - Real-Time Investigation Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB)](https://reactjs.org/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--Time-green)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1)](https://www.mysql.com/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A real-time investigation management system for detecting anomalies in business entities (Invoices and Waybills) with live updates via SignalR WebSocket communication.

## 🚀 Features

### Core Functionality
- **Real-Time Monitoring** - Live dashboard with instant updates via SignalR WebSocket
- **Invoice Investigation** - Detect negative amounts, excessive tax ratios, and future dates
- **Waybill Investigation** - Identify late shipments, expiring deliveries, and legacy records
- **Dynamic Management** - Create, configure, and control investigators on-demand
- **Investigation History** - View detailed results with click-to-view modal
- **Bulk Operations** - Clear all investigation results with confirmation

### Technical Features
- **Zero Polling Architecture** - All updates pushed via WebSocket events
- **Auto-Reconnection** - SignalR automatically reconnects on connection loss
- **Repository Pattern** - Clean data access abstraction
- **SOLID Principles** - Well-architected with dependency injection
- **Responsive UI** - Works on desktop and mobile devices

## 📋 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [MySQL 8.0+](https://www.mysql.com/)
- Git

## 🛠️ Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/egeakin458/ea_Tracker.git
cd ea_Tracker
```

### 2. Database Setup
```bash
# Install MySQL if not already installed
sudo apt install mysql-server -y

# Start MySQL service
sudo service mysql start

# Create database (optional - migrations will create it)
mysql -u root -p -e "CREATE DATABASE ea_tracker_db;"
```

### 3. Backend Setup
```bash
cd src/backend

# Install Entity Framework tools
dotnet tool install --global dotnet-ef

# Configure database connection (for development)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ea_tracker_db;Uid=root;Pwd=YOUR_PASSWORD;"

# Apply database migrations
dotnet ef database update

# Run the backend (http://localhost:5050)
dotnet run
```

### 4. Frontend Setup
```bash
# In a new terminal
cd src/frontend

# Install dependencies
npm install

# Start the development server (http://localhost:3000)
npm start
```

## 🎯 Usage Guide

1. **Access Dashboard**: Open http://localhost:3000
2. **Create Investigator**: 
   - Click "Create Investigator"
   - Select type (Invoice or Waybill)
   - Enter a custom name
3. **Run Investigation**:
   - Click "Start" button on an investigator
   - Watch real-time results appear
4. **View Details**:
   - Click any completed investigation in the right panel
   - Modal shows detailed results and metadata
5. **Clear History**:
   - Click "Clear All" button (permanent deletion)
   - Confirm in the dialog

## 🏗️ Architecture

### Technology Stack

| Component | Technology | Version | Purpose |
|-----------|------------|---------|---------|
| **Backend API** | ASP.NET Core | 8.0 | REST API + Business Logic |
| **Real-Time** | SignalR | 8.0 | WebSocket Communication |
| **Database** | MySQL + EF Core | 8.0.1 | Data Persistence |
| **Frontend** | React + TypeScript | 18.2.0 | Single Page Application |
| **HTTP Client** | Axios | 0.27.2 | API Communication |
| **UI Styling** | Inline Styles | - | Component Styling |

### Project Structure
```
ea_Tracker/
├── src/
│   ├── backend/                      # .NET 8.0 Web API
│   │   ├── Controllers/              # 5 API Controllers
│   │   │   ├── CompletedInvestigationsController.cs
│   │   │   ├── InvestigationsController.cs
│   │   │   ├── InvestigatorController.cs
│   │   │   ├── InvoicesController.cs
│   │   │   └── WaybillsController.cs
│   │   ├── Data/
│   │   │   └── ApplicationDbContext.cs
│   │   ├── Enums/                    # Type-safe enumerations
│   │   ├── Hubs/
│   │   │   └── InvestigationHub.cs   # SignalR Hub
│   │   ├── Models/                   # 6 Entity Models
│   │   ├── Repositories/             # Data Access Layer
│   │   ├── Services/                 # Business Logic
│   │   └── Program.cs
│   └── frontend/                     # React TypeScript SPA
│       └── src/
│           ├── Dashboard.tsx         # Main Dashboard
│           ├── InvestigationResults.tsx
│           ├── InvestigationDetailModal.tsx
│           └── lib/
│               └── SignalRService.ts # WebSocket Management
└── tests/                           # Test Suite
```

## 🔌 API Reference

### Investigation Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/investigations` | List all investigators |
| `POST` | `/api/investigations/{id}/start` | Start investigation |
| `POST` | `/api/investigations/{id}/stop` | Stop investigation |
| `GET` | `/api/investigations/{id}/results` | Get investigation results |
| `POST` | `/api/investigations/invoice` | Create invoice investigator |
| `POST` | `/api/investigations/waybill` | Create waybill investigator |
| `DELETE` | `/api/investigations/{id}` | Delete investigator |

### Investigation Results

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/CompletedInvestigations` | Get all completed investigations |
| `GET` | `/api/CompletedInvestigations/{id}` | Get detailed investigation results |
| `DELETE` | `/api/CompletedInvestigations/clear` | Clear all results (permanent) |
| `DELETE` | `/api/CompletedInvestigations/{id}` | Delete specific investigation |

### Health & Monitoring

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/healthz` | Comprehensive health check with database status |
| `GET` | `/swagger` | API documentation (development only) |

## 📡 SignalR Events

### Client Receives
- `InvestigationStarted` - Investigation begins
- `InvestigationCompleted` - Investigation finishes with result count
- `NewResultAdded` - Real-time anomaly detection
- `StatusChanged` - Investigator status update

### Event Payload Examples
```javascript
// InvestigationCompleted Event
{
  investigatorId: "uuid",
  resultCount: 42,
  timestamp: "2024-01-01T12:00:00Z"
}
```

## 🗄️ Database Schema

### Core Tables
- **InvestigatorTypes** - Investigation type templates
- **InvestigatorInstances** - Configured investigators
- **InvestigationExecutions** - Investigation run history
- **InvestigationResults** - Anomaly findings (BIGINT for scale)

### Business Tables
- **Invoices** - Invoice records with audit fields
- **Waybills** - Waybill records with due dates

### Key Indexes
- `IX_InvestigatorInstance_Type_Active`
- `IX_Result_Execution_Time`
- `IX_Result_Severity`

## ⚙️ Configuration

### Backend Configuration
```bash
# User Secrets (Development)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"

# Environment Variables (Production)
export ConnectionStrings__DefaultConnection="YOUR_CONNECTION_STRING"
export ASPNETCORE_ENVIRONMENT="Production"
```

### Frontend Configuration
```bash
# Create .env file in src/frontend
echo "REACT_APP_API_BASE_URL=http://localhost:5050" > .env
```

### Investigation Settings
Configure thresholds in `appsettings.json`:
```json
{
  "Investigation": {
    "Invoice": {
      "MaxTaxRatio": 0.5,
      "MaxFutureDays": 0
    },
    "Waybill": {
      "ExpiringSoonHours": 24,
      "LegacyCutoffDays": 7
    }
  }
}
```

## 🧪 Testing

```bash
# Run all tests
npm test

# Backend tests only (xUnit)
npm run test:backend

# Frontend tests only (Jest)
npm run test:frontend

# End-to-end tests (Cypress)
npm run test:e2e
```

## 🐳 Docker Support

```bash
# Build and run with Docker Compose
docker-compose up -d

# View logs
docker-compose logs -f

# Stop containers
docker-compose down
```

## 🚦 System Status

- ✅ **Production Ready** - Core functionality complete
- ⚠️ **Security Note** - Add authentication before production deployment
- 🔄 **Active Development** - Regular updates and improvements

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Development Guidelines
- Follow C# and TypeScript naming conventions
- Add XML documentation for public APIs
- Write unit tests for new features
- Ensure SignalR events are properly handled

## 📈 Performance

- **Batch Processing** - Investigations process in optimized batches
- **Connection Pooling** - Efficient database connection management
- **Auto-Reconnection** - SignalR reconnects with exponential backoff
- **Indexed Queries** - Database queries use proper indexing

## 🔒 Security Considerations

### Current Security
- CORS configured for development
- User secrets for sensitive data
- Global exception handling
- Input validation on all endpoints

### Before Production
- [ ] Implement authentication (JWT recommended)
- [ ] Add authorization policies
- [ ] Enable rate limiting
- [ ] Configure HTTPS
- [ ] Review CORS settings

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **Ege Akin** - *Initial Development* - [egeakin458](https://github.com/egeakin458)

## 🙏 Acknowledgments

- Built with ASP.NET Core 8.0 and React 18
- Real-time communication powered by SignalR
- Database management with Entity Framework Core
- MySQL for reliable data persistence

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/egeakin458/ea_Tracker/issues)
- **Discussions**: [GitHub Discussions](https://github.com/egeakin458/ea_Tracker/discussions)
- **Email**: Contact via GitHub profile

---

**Version**: 1.0.0 | **Last Updated**: January 2025 | **Status**: Active Development