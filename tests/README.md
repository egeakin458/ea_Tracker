# ea_Tracker Unified Test Suite

This directory contains the **complete unified test structure** for the ea_Tracker investigation management system - implementing true test organization where **ALL tests live in one place**.

## 🎯 Overview

The ea_Tracker project now implements a **professional unified testing strategy** with complete separation of concerns:

- ✅ **Source Code**: `src/` directory (backend + frontend)
- ✅ **All Tests**: `tests/` directory (backend + frontend + e2e)
- ✅ **Single Command Interface**: Run all tests from project root
- ✅ **Zero Duplication**: No scattered test files across the project

## 📁 Unified Directory Structure

```
tests/
├── backend/
│   ├── unit/                      # Backend unit tests (xUnit)
│   │   ├── ea_Tracker.Tests.csproj
│   │   └── InvestigationManagerTests.cs
│   └── integration/               # Future: API integration tests
├── frontend/                      
│   ├── unit/                      # Frontend unit tests (Jest + RTL)
│   │   ├── App.spec.tsx           # App component tests
│   │   └── axios.spec.ts          # API client tests
│   ├── integration/               # Frontend integration tests
│   │   └── Dashboard.spec.tsx     # Dashboard component integration
│   └── e2e/                       # End-to-end tests (Cypress)
│       ├── smoke.cy.js            # Critical workflow tests
│       └── fixtures/              # Test data
│           └── investigators.json
└── e2e/                           # Future: Cross-stack system tests
```

## 🚀 Running Tests (Unified Commands)

### From Project Root (Recommended)
```bash
# Run ALL tests
npm test -- --watchAll=false

# Run specific test suites
npm run test:backend              # Backend unit tests only
npm run test:frontend             # Frontend unit + integration tests
npm run test:e2e                  # End-to-end tests only
npm run test:e2e:open             # E2E tests (interactive)

# Development workflows
npm run test:watch                # Watch mode for development
npm run test:coverage             # Generate coverage reports
```

### Legacy Commands (Still Supported)
```bash
# Backend tests (direct .NET)
dotnet test tests/backend/unit/ea_Tracker.Tests.csproj --verbosity normal

# All tests (CI/CD compatible)
dotnet restore
npm ci && cd src/frontend && npm ci
npm test -- --watchAll=false
npm run test:backend
```

## Test Configuration

### Backend Configuration
- **Framework**: xUnit with .NET 8.0
- **Database**: Entity Framework InMemory for unit tests
- **Mocking**: Built-in xUnit capabilities
- **Project File**: `tests/backend/unit/ea_Tracker.Tests.csproj`

### Frontend Configuration
- **Framework**: Jest with React Testing Library
- **Environment**: jsdom for DOM simulation
- **E2E Framework**: Cypress
- **Configuration Files**: 
  - `src/frontend/jest.config.js`
  - `src/frontend/cypress.config.js`

## Test Coverage

### Backend Coverage
✅ **InvestigationManager** - Core business logic
- Investigator lifecycle management
- Database persistence operations
- Service coordination

### Frontend Coverage
✅ **App Component** - Main application shell
✅ **Dashboard Component** - Investigation management UI with API integration
✅ **Axios Configuration** - API client setup and defaults
✅ **E2E Smoke Tests** - Critical user workflows

## Adding New Tests

### Backend Unit Tests
1. Create test class in `tests/backend/unit/`
2. Follow naming convention: `{ComponentName}Tests.cs`
3. Use xUnit attributes: `[Fact]`, `[Theory]`
4. Use InMemory database for data layer tests

### Frontend Unit Tests
1. Create test file in `src/frontend/tests/unit/`
2. Follow naming convention: `{ComponentName}.spec.tsx`
3. Use Jest and React Testing Library
4. Mock external dependencies

### Frontend Integration Tests
1. Create test file in `src/frontend/tests/integration/`
2. Test component interactions and data flow
3. Mock API calls with realistic responses

### E2E Tests
1. Create test file in `src/frontend/cypress/e2e/`
2. Follow naming convention: `{feature}.cy.js`
3. Test complete user workflows
4. Use realistic test data from fixtures

## Best Practices

### Backend Tests
- Use InMemory database for isolated tests
- Test business logic, not infrastructure
- Follow AAA pattern (Arrange, Act, Assert)
- Use descriptive test names

### Frontend Tests
- Test behavior, not implementation
- Use semantic queries (getByRole, getByText)
- Mock external dependencies consistently
- Test user interactions, not component internals

### E2E Tests
- Focus on critical user paths
- Use data-testid sparingly, prefer semantic selectors
- Keep tests independent and idempotent
- Use fixtures for consistent test data

## Maintenance

This unified test structure was implemented to:
- Improve test organization and discoverability
- Standardize test running procedures across environments
- Enable better CI/CD pipeline integration
- Facilitate future test expansion and maintenance

For questions about the test structure or adding new tests, refer to the project documentation or reach out to the development team.

---
*Last Updated: August 6, 2025*
*Test Structure Version: 1.0*