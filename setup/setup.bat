@echo off
REM ea_Tracker Developer Setup Script - Windows Batch
REM Automates the complete setup process for Windows developers  
REM Usage: setup.bat

setlocal enabledelayedexpansion

echo.
echo 🚀 ea_Tracker Developer Setup
echo ===============================

REM Check if we're in the right directory
if not exist "ea_Tracker.sln" (
    echo ❌ Please run this script from the ea_Tracker project root directory
    echo    Expected file: ea_Tracker.sln
    pause
    exit /b 1
)

if not exist "package.json" (
    echo ❌ Please run this script from the ea_Tracker project root directory
    echo    Expected file: package.json
    pause
    exit /b 1
)

REM Step 1: Check prerequisites
echo.
echo [1/8] Checking prerequisites...

REM Check Node.js
node --version >nul 2>&1
if !errorlevel! neq 0 (
    echo ❌ Node.js not found
    echo    Install Node.js 18+ from https://nodejs.org/
    echo    Or use Chocolatey: choco install nodejs
    pause
    exit /b 1
) else (
    for /f "tokens=*" %%i in ('node --version') do set NODE_VERSION=%%i
    echo ✅ Node.js !NODE_VERSION! found
)

REM Check npm
npm --version >nul 2>&1
if !errorlevel! neq 0 (
    echo ❌ npm not found (should come with Node.js)
    pause
    exit /b 1
) else (
    for /f "tokens=*" %%i in ('npm --version') do set NPM_VERSION=%%i
    echo ✅ npm !NPM_VERSION!
)

REM Check .NET
dotnet --version >nul 2>&1
if !errorlevel! neq 0 (
    echo ❌ .NET SDK not found
    echo    Install .NET 8.0 SDK from: https://dotnet.microsoft.com/download
    echo    Or use winget: winget install Microsoft.DotNet.SDK.8
    pause
    exit /b 1
) else (
    for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VERSION=%%i
    echo ✅ .NET SDK !DOTNET_VERSION!
)

REM Check MySQL
set MYSQL_PATH=mysql
mysql --version >nul 2>&1
if !errorlevel! neq 0 (
    REM Check common installation paths
    set MYSQL_FOUND=0
    
    if exist "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" (
        echo ✅ MySQL found at: C:\Program Files\MySQL\MySQL Server 8.0\bin\
        echo ⚠️  Consider adding MySQL to your PATH environment variable
        set MYSQL_FOUND=1
        set MYSQL_PATH="C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"
    ) else if exist "C:\xampp\mysql\bin\mysql.exe" (
        echo ✅ MySQL found at: C:\xampp\mysql\bin\
        echo ⚠️  Consider adding MySQL to your PATH environment variable  
        set MYSQL_FOUND=1
        set MYSQL_PATH="C:\xampp\mysql\bin\mysql.exe"
    )
    
    if !MYSQL_FOUND! equ 0 (
        echo ❌ MySQL not found
        echo    Install MySQL 8.0+:
        echo      Download: https://dev.mysql.com/downloads/mysql/
        echo      Or use XAMPP: https://www.apachefriends.org/
        pause
        exit /b 1
    )
) else (
    echo ✅ MySQL found in PATH
)

REM Step 2: Node.js version management
echo.
echo [2/8] Managing Node.js version...
if exist ".nvmrc" (
    echo ⚠️  .nvmrc found but NVM for Windows management not implemented in batch
    echo    Current Node.js version should work if ^>= 18.0.0
) else (
    echo ⚠️  .nvmrc file not found
)

REM Step 3: Environment configuration
echo.
echo [3/8] Setting up environment configuration...

if not exist "secret.env" (
    if exist "secret.env.example" (
        copy "secret.env.example" "secret.env" >nul
        echo ✅ Created secret.env from template
        echo ⚠️  IMPORTANT: Edit secret.env with your MySQL credentials!
        echo    Example: DEFAULT_CONNECTION=Server=localhost;Database=ea_tracker_db;Uid=root;Pwd=your_password;
    ) else (
        echo ❌ secret.env.example not found
        pause
        exit /b 1
    )
) else (
    echo ✅ secret.env already exists
)

if not exist "src\frontend\.env" (
    if exist "src\frontend\.env.example" (
        copy "src\frontend\.env.example" "src\frontend\.env" >nul
        echo ✅ Created frontend .env from template
    ) else (
        echo ❌ src\frontend\.env.example not found
        pause
        exit /b 1
    )
) else (
    echo ✅ Frontend .env already exists
)

REM Step 4: Install .NET tools
echo.
echo [4/8] Installing .NET Entity Framework tools...
dotnet tool install --global dotnet-ef >nul 2>&1
if !errorlevel! equ 0 (
    echo ✅ Entity Framework tools installed
) else (
    REM Try update if install failed
    dotnet tool update --global dotnet-ef >nul 2>&1
    if !errorlevel! equ 0 (
        echo ✅ Entity Framework tools updated
    ) else (
        REM Check if already available
        dotnet ef --version >nul 2>&1
        if !errorlevel! equ 0 (
            echo ✅ Entity Framework tools already available
        ) else (
            echo ❌ Failed to install Entity Framework tools
            echo    Try manually: dotnet tool install --global dotnet-ef
            pause
            exit /b 1
        )
    )
)

REM Step 5: Install dependencies
echo.
echo [5/8] Installing project dependencies...

echo    Installing root dependencies...
npm ci >nul 2>&1
if !errorlevel! neq 0 (
    echo ⚠️  npm ci failed, trying npm install...
    npm install >nul 2>&1
    if !errorlevel! neq 0 (
        echo ❌ Failed to install root dependencies
        pause
        exit /b 1
    ) else (
        echo ✅ Root dependencies installed (npm install)
    )
) else (
    echo ✅ Root dependencies installed (npm ci)
)

if exist "src\frontend\node_modules" (
    echo ✅ Frontend dependencies available
) else (
    echo    Installing frontend dependencies separately...
    cd src\frontend
    npm install >nul 2>&1
    if !errorlevel! neq 0 (
        echo ❌ Failed to install frontend dependencies
        cd ..\..
        pause
        exit /b 1
    ) else (
        echo ✅ Frontend dependencies installed
    )
    cd ..\..
)

echo    Restoring backend packages...
cd src\backend
dotnet restore >nul 2>&1
if !errorlevel! neq 0 (
    echo ❌ Failed to restore backend packages
    cd ..\..
    pause
    exit /b 1
) else (
    echo ✅ Backend packages restored
)
cd ..\..

REM Step 6: Verification tests
echo.
echo [6/8] Running verification tests...

echo    Verifying TypeScript compilation...
npx tsc --noEmit >nul 2>&1
if !errorlevel! neq 0 (
    echo ❌ TypeScript compilation failed
    echo    This might indicate dependency issues
    pause
    exit /b 1
) else (
    echo ✅ TypeScript compilation OK
)

echo    Verifying backend compilation...
cd src\backend
dotnet build >nul 2>&1
if !errorlevel! neq 0 (
    echo ❌ Backend compilation failed
    cd ..\..
    pause
    exit /b 1
) else (
    echo ✅ Backend compilation OK
)
cd ..\..

REM Step 7: Database information
echo.
echo [7/8] Database setup information...
echo    Database will be automatically created when you first run the backend
echo ⚠️  Ensure MySQL is running and secret.env has correct credentials
echo    The backend will run migrations automatically on startup

REM Step 8: Optional test data
echo.
echo [8/8] Optional test data setup...
if exist "scripts\test-data\seed-data.sql" (
    set /p response="Load sample test data with anomalies? [y/N]: "
    if /i "!response!"=="y" (
        echo    Test data loading in batch is limited due to password input
        echo    Please run this command manually after setup:
        echo    !MYSQL_PATH! -u root -p ea_tracker_db ^< scripts\test-data\seed-data.sql
    ) else (
        echo    Skipping test data. Load later with:
        echo    !MYSQL_PATH! -u root -p ea_tracker_db ^< scripts\test-data\seed-data.sql
    )
) else (
    echo ⚠️  Test data file not found
)

echo.
echo 🎉 Setup Complete!
echo ==================
echo.
echo Your ea_Tracker development environment is ready!
echo.
echo Next steps:
echo 1. Review secret.env - Update MySQL credentials if needed
echo 2. Start backend: cd src\backend ^&^& dotnet run
echo 3. Start frontend: npm start (in new Command Prompt window)
echo 4. Visit: http://localhost:3000
echo.
echo Useful commands:
echo • Run tests: npm run test:frontend -- --watchAll=false
echo • Load test data: !MYSQL_PATH! -u root -p ea_tracker_db ^< scripts\test-data\seed-data.sql
echo • Check health: curl http://localhost:5050/healthz
echo.
echo ✅ Happy coding! 🚀
echo.
pause