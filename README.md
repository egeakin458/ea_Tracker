# ea_Tracker

A comprehensive investigation management system built with ASP.NET Core 8.0 backend and React TypeScript frontend. The system provides automated investigation workflows for tracking and analyzing invoices and waybills with persistent data storage.

## 📁 Project Structure

```
ea_Tracker/
├── src/
│   ├── backend/                 # ASP.NET Core 8.0 Web API
│   │   ├── Controllers/         # API controllers
│   │   ├── Data/               # Entity Framework DbContext
│   │   ├── Models/             # Entity models and DTOs
│   │   ├── Services/           # Business logic and investigators
│   │   ├── Repositories/       # Repository pattern implementation
│   │   └── Migrations/         # EF Core database migrations
│   └── frontend/               # React TypeScript application
│       ├── src/                # React source code
│       ├── cypress/            # E2E tests
│       └── tests/              # Unit and integration tests
├── tests/
│   └── unit/Backend.Tests/     # Backend unit tests (xUnit)
├── docs/
│   └── architecture/           # Project documentation
├── database/
│   └── migrations/             # Database migration scripts
└── .github/workflows/          # CI/CD pipeline
```

## 🚀 Features

- **Investigation Management**: Create, start, stop, and monitor investigation workflows
- **Data Persistence**: Full CRUD operations with Entity Framework Core and MySQL
- **Real-time Updates**: Live status tracking of investigation processes
- **Multi-format Export**: Export investigation results in various formats
- **Automated Testing**: Comprehensive unit and integration test suites
- **CI/CD Pipeline**: Automated build and deployment with GitHub Actions

## 🛠️ Technology Stack

### Backend
- **Framework**: ASP.NET Core 8.0
- **Database**: MySQL with Entity Framework Core 8.0
- **ORM**: Entity Framework Core with Code-First approach
- **Testing**: xUnit framework
- **API Documentation**: Swagger/OpenAPI

### Frontend
- **Framework**: React 18 with TypeScript
- **HTTP Client**: Axios
- **Testing**: Jest + React Testing Library
- **E2E Testing**: Cypress
- **Build Tool**: React Scripts

## 📋 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18.x](https://nodejs.org/)
- [MySQL Server](https://dev.mysql.com/downloads/mysql/)

## ⚙️ Configuration

### Database Connection

The application requires a MySQL database connection string. Configure it using one of these methods:

**Option 1: Environment Variable**
```bash
export DEFAULT_CONNECTION="server=localhost;database=ea_tracker_db;user=root;password=yourpassword;"
```

**Option 2: Local Development File**
Create a `secret.env` file in the project root:
```env
DEFAULT_CONNECTION="server=localhost;database=ea_tracker_db;user=root;password=yourpassword;"
```

The application automatically loads the `secret.env` file during startup for local development.

## 🏃‍♂️ Getting Started

### 1. Clone the Repository
```bash
git clone https://github.com/egeakin458/ea_Tracker.git
cd ea_Tracker
```

### 2. Backend Setup
```bash
# Navigate to backend directory
cd src/backend

# Restore packages
dotnet restore

# Apply database migrations
dotnet ef database update

# Run the application
dotnet run
```

The backend API will be available at `https://localhost:5051` (or `http://localhost:5050`)

### 3. Frontend Setup
```bash
# Navigate to frontend directory
cd src/frontend

# Install dependencies
npm install

# Start development server
npm start
```

The frontend will be available at `http://localhost:3000`

## 🧪 Testing

### Backend Tests
```bash
# Run all backend tests
dotnet test tests/unit/Backend.Tests/ea_Tracker.Tests.csproj

# Run with detailed output
dotnet test tests/unit/Backend.Tests/ea_Tracker.Tests.csproj --verbosity normal
```

### Frontend Tests
```bash
cd src/frontend

# Run unit tests
npm test

# Run E2E tests
npm run cypress:run

# Open Cypress interactive mode
npm run cypress:open
```

## 📚 API Documentation

### Core Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/investigations` | List all investigations with status |
| `POST` | `/api/investigations/{id}/start` | Start an investigation |
| `POST` | `/api/investigations/{id}/stop` | Stop an investigation |
| `GET` | `/api/investigations/{id}/results` | Get investigation results |
| `GET` | `/api/investigators` | List available investigators |
| `POST` | `/api/investigators` | Create new investigator |

### Additional Endpoints
- `/api/invoices` - Invoice management
- `/api/waybills` - Waybill management

API documentation is available via Swagger UI at `/swagger` when running in development mode.

## 🏗️ Build and Deployment

### Local Build
```bash
# Build backend
cd src/backend
dotnet build --configuration Release

# Build frontend
cd src/frontend
npm run build
```

### CI/CD Pipeline

The project uses GitHub Actions for automated testing and deployment:

- **Triggers**: Push to main branch or pull requests
- **Backend**: Restores packages, runs tests, builds application
- **Frontend**: Installs dependencies, runs tests, builds for production
- **Status**: [![CI](https://github.com/egeakin458/ea_Tracker/actions/workflows/ci.yml/badge.svg)](https://github.com/egeakin458/ea_Tracker/actions)

## 🎯 Architecture

The application follows clean architecture principles:

- **Controllers**: Handle HTTP requests and responses
- **Services**: Contain business logic and orchestration
- **Repositories**: Abstract data access layer
- **Models**: Define entities and data transfer objects
- **Middleware**: Handle cross-cutting concerns

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines
- Follow existing code conventions
- Write tests for new features
- Update documentation as needed
- Ensure all tests pass before submitting PR

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔍 Troubleshooting

### Common Issues

**Database Connection Issues**
- Ensure MySQL server is running
- Verify connection string format
- Check database permissions

**Build Failures**
- Clear package caches: `dotnet clean` and `npm cache clean --force`
- Restore packages: `dotnet restore` and `npm install`
- Check .NET and Node.js versions

**Test Failures**
- Ensure test database is accessible
- Run migrations: `dotnet ef database update`
- Check for port conflicts

For more help, check the [issues](https://github.com/egeakin458/ea_Tracker/issues) page or create a new issue.