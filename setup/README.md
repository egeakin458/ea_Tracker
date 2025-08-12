# ea_Tracker Setup Scripts

Automated setup scripts for cross-platform developer onboarding.

## Quick Start

Choose the script for your operating system:

### Linux/macOS
```bash
./setup.sh
```
- **Features**: Full bash script with comprehensive error handling
- **Requirements**: bash, curl, git
- **Duration**: 2-3 minutes

### Windows PowerShell
```powershell
.\setup.ps1
```
- **Features**: Advanced PowerShell with detailed validation
- **Requirements**: PowerShell 5.1+ (Windows 10/11 default)
- **Duration**: 2-3 minutes

### Windows Command Prompt
```cmd
setup.bat
```
- **Features**: Basic batch script for older Windows systems
- **Requirements**: Windows Command Prompt
- **Duration**: 3-4 minutes

## What These Scripts Do

### 1. Prerequisites Check
- ✅ Node.js 18+ installed
- ✅ .NET 8.0 SDK installed  
- ✅ MySQL 8.0+ installed and running
- ❌ Stops with helpful error messages if missing

### 2. Environment Configuration
- Creates `secret.env` from template
- Creates `src/frontend/.env` from template
- Provides clear instructions for database credentials

### 3. Dependency Management
- Runs `npm ci` for clean dependency installation
- Installs .NET Entity Framework tools globally
- Restores backend NuGet packages

### 4. Validation & Testing
- Verifies TypeScript compilation works
- Verifies .NET compilation works  
- Tests package integrity

### 5. Optional Features
- NVM version switching (if available)
- Test data loading (sample invoices/waybills)
- Service status checking

## After Setup Completion

1. **Edit Database Credentials:**
   ```bash
   # Edit secret.env with your MySQL password
   DEFAULT_CONNECTION=Server=localhost;Database=ea_tracker_db;Uid=root;Pwd=YOUR_PASSWORD;
   ```

2. **Start Backend Server:**
   ```bash
   cd src/backend && dotnet run
   ```

3. **Start Frontend Server:**
   ```bash
   npm start
   ```

4. **Visit Application:**
   - Frontend: http://localhost:3000
   - Backend API: http://localhost:5050
   - Health Check: http://localhost:5050/healthz

## Troubleshooting

### Common Issues

**Script Permission Denied (Linux/Mac):**
```bash
chmod +x setup/setup.sh
```

**PowerShell Execution Policy (Windows):**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

**MySQL Connection Issues:**
- Ensure MySQL service is running
- Check credentials in `secret.env`
- Verify MySQL is accessible on localhost:3306

**Node.js Version Issues:**
- Install Node.js 18+ from https://nodejs.org/
- Or use NVM: `nvm install 18 && nvm use 18`

**Port Conflicts:**
- Backend (5050): Change in `src/backend/appsettings.json`
- Frontend (3000): Change in `src/frontend/.env`

### Getting Help

If you encounter issues:

1. **Check Prerequisites**: Ensure Node.js, .NET, and MySQL are installed
2. **Review Logs**: Setup scripts provide detailed error messages
3. **Manual Setup**: Follow the manual setup guide in main README.md
4. **Create Issue**: Report problems at [GitHub Issues](https://github.com/egeakin458/ea_Tracker/issues)

## Script Details

| Script | Platform | Features | Error Handling |
|--------|----------|----------|----------------|
| `setup.sh` | Linux/macOS | Full automation, NVM support, service checking | Comprehensive |
| `setup.ps1` | Windows PowerShell | Advanced validation, secure password input | Detailed |
| `setup.bat` | Windows CMD | Basic automation, maximum compatibility | Standard |

## Contributing

When updating setup scripts:

1. Test on target platforms
2. Update version checks for new dependencies  
3. Add error handling for new failure scenarios
4. Update this README with any new features

---

**These scripts reduce developer onboarding from 30+ minutes to 2-3 minutes!** 🚀