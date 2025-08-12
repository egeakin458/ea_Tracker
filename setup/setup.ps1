# ea_Tracker Developer Setup Script - PowerShell
# Automates the complete setup process for Windows developers
# Usage: .\setup.ps1

# Requires PowerShell 5.1+ (Windows 10/11 default)

param(
    [switch]$SkipTestData = $false
)

# Colors for output
$Script:Colors = @{
    Red = "Red"
    Green = "Green" 
    Yellow = "Yellow"
    Blue = "Blue"
    White = "White"
}

function Write-Step {
    param($StepNumber, $Message)
    Write-Host "`n[$StepNumber/8] $Message" -ForegroundColor Blue
}

function Write-Success {
    param($Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning {
    param($Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error {
    param($Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Info {
    param($Message)
    Write-Host "   $Message" -ForegroundColor White
}

function Test-Command {
    param($CommandName)
    return [bool](Get-Command -Name $CommandName -ErrorAction SilentlyContinue)
}

function Test-Version {
    param($Version, $MinimumVersion)
    return [version]$Version -ge [version]$MinimumVersion
}

function Test-ProjectDirectory {
    if (!(Test-Path "ea_Tracker.sln") -or !(Test-Path "package.json")) {
        Write-Error "Please run this script from the ea_Tracker project root directory"
        Write-Info "Expected files: ea_Tracker.sln, package.json"
        exit 1
    }
}

Write-Host "🚀 ea_Tracker Developer Setup" -ForegroundColor Blue
Write-Host "==============================="

# Validate we're in the right place
Test-ProjectDirectory

# Step 1: Check prerequisites
Write-Step 1 "Checking prerequisites..."

# Check Node.js
if (Test-Command "node") {
    $nodeVersion = node --version
    $nodeVersionNumber = $nodeVersion -replace 'v', ''
    
    if (Test-Version $nodeVersionNumber "18.0.0") {
        Write-Success "Node.js $nodeVersionNumber (>= 18.0.0)"
    } else {
        Write-Error "Node.js version $nodeVersionNumber is too old"
        Write-Info "Required: Node.js 18.0.0 or higher"
        Write-Info "Download from: https://nodejs.org/"
        exit 1
    }
} else {
    Write-Error "Node.js not found"
    Write-Info "Install Node.js 18+ from https://nodejs.org/"
    Write-Info "Or use Chocolatey: choco install nodejs"
    Write-Info "Or use winget: winget install OpenJS.NodeJS"
    exit 1
}

# Check npm
if (Test-Command "npm") {
    $npmVersion = npm --version
    Write-Success "npm $npmVersion"
} else {
    Write-Error "npm not found (should come with Node.js)"
    exit 1
}

# Check .NET
if (Test-Command "dotnet") {
    try {
        $dotnetVersion = dotnet --version 2>$null
        if ($dotnetVersion -match "^[8-9]\." -or $dotnetVersion -match "^[1-9][0-9]+\.") {
            Write-Success ".NET SDK $dotnetVersion"
        } else {
            Write-Warning ".NET SDK $dotnetVersion (recommended: 8.0+)"
        }
    } catch {
        Write-Error "Error checking .NET version"
        exit 1
    }
} else {
    Write-Error ".NET SDK not found"
    Write-Info "Install .NET 8.0 SDK from: https://dotnet.microsoft.com/download"
    Write-Info "Or use winget: winget install Microsoft.DotNet.SDK.8"
    exit 1
}

# Check MySQL (Windows-specific paths)
$mysqlFound = $false
$mysqlPath = ""
if (Test-Command "mysql") {
    Write-Success "MySQL command found in PATH"
    $mysqlFound = $true
    $mysqlPath = "mysql"
} else {
    # Check common MySQL installation paths
    $commonPaths = @(
        "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe",
        "C:\Program Files (x86)\MySQL\MySQL Server 8.0\bin\mysql.exe",
        "C:\xampp\mysql\bin\mysql.exe"
    )
    
    foreach ($path in $commonPaths) {
        if (Test-Path $path) {
            Write-Success "MySQL found at: $path"
            Write-Info "Consider adding MySQL to your PATH environment variable"
            $mysqlFound = $true
            $mysqlPath = $path
            break
        }
    }
}

if (!$mysqlFound) {
    Write-Error "MySQL not found"
    Write-Info "Install MySQL 8.0+:"
    Write-Info "  Download: https://dev.mysql.com/downloads/mysql/"
    Write-Info "  Or use Chocolatey: choco install mysql"
    Write-Info "  Or use XAMPP: https://www.apachefriends.org/"
    exit 1
}

# Check if MySQL service is running
$mysqlService = Get-Service -Name "*mysql*" -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq "Running" }
if ($mysqlService) {
    Write-Success "MySQL service is running ($($mysqlService.Name))"
} else {
    Write-Warning "MySQL service might not be running"
    Write-Info "Start MySQL service from Services.msc or MySQL Workbench"
}

# Step 2: Node.js version management
Write-Step 2 "Managing Node.js version..."
if (Test-Path ".nvmrc") {
    if (Test-Command "nvm") {
        try {
            nvm use
            Write-Success "Switched to Node.js version from .nvmrc"
        } catch {
            Write-Warning "Could not switch Node.js version with NVM"
            Write-Info "Current Node.js version should work if >= 18.0.0"
        }
    } else {
        Write-Warning "NVM for Windows not installed"
        Write-Info "Install from: https://github.com/coreybutler/nvm-windows"
        Write-Info "Current Node.js version should work"
    }
} else {
    Write-Warning ".nvmrc file not found"
}

# Step 3: Environment configuration
Write-Step 3 "Setting up environment configuration..."

# Backend environment
if (!(Test-Path "secret.env")) {
    if (Test-Path "secret.env.example") {
        Copy-Item "secret.env.example" "secret.env"
        Write-Success "Created secret.env from template"
        Write-Warning "IMPORTANT: Edit secret.env with your MySQL credentials!"
        Write-Info "Example: DEFAULT_CONNECTION=Server=localhost;Database=ea_tracker_db;Uid=root;Pwd=your_password;"
    } else {
        Write-Error "secret.env.example not found"
        exit 1
    }
} else {
    Write-Success "secret.env already exists"
}

# Frontend environment
if (!(Test-Path "src\frontend\.env")) {
    if (Test-Path "src\frontend\.env.example") {
        Copy-Item "src\frontend\.env.example" "src\frontend\.env"
        Write-Success "Created frontend .env from template"
    } else {
        Write-Error "src\frontend\.env.example not found"
        exit 1
    }
} else {
    Write-Success "Frontend .env already exists"
}

# Step 4: Install .NET tools
Write-Step 4 "Installing .NET Entity Framework tools..."
try {
    $efOutput = dotnet tool install --global dotnet-ef 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Entity Framework tools installed"
    } else {
        # Try update if install failed
        $efUpdateOutput = dotnet tool update --global dotnet-ef 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Entity Framework tools updated"
        } else {
            # Check if already available
            $efCheckOutput = dotnet ef --version 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Entity Framework tools already available"
            } else {
                Write-Error "Failed to install Entity Framework tools"
                Write-Info "Try manually: dotnet tool install --global dotnet-ef"
                exit 1
            }
        }
    }
} catch {
    Write-Error "Error installing Entity Framework tools: $_"
    exit 1
}

# Step 5: Install dependencies
Write-Step 5 "Installing project dependencies..."

# Clean install from lockfile
Write-Info "Installing root dependencies..."
try {
    $npmCiOutput = npm ci 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Root dependencies installed (npm ci)"
    } else {
        Write-Warning "npm ci failed, trying npm install..."
        $npmInstallOutput = npm install 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Root dependencies installed (npm install)"
        } else {
            Write-Error "Failed to install root dependencies"
            Write-Info "npm ci output: $npmCiOutput"
            exit 1
        }
    }
} catch {
    Write-Error "Error installing dependencies: $_"
    exit 1
}

# Frontend dependencies verification
if (Test-Path "src\frontend\node_modules") {
    Write-Success "Frontend dependencies available"
} else {
    Write-Info "Installing frontend dependencies separately..."
    Push-Location "src\frontend"
    try {
        $frontendNpmOutput = npm install 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Frontend dependencies installed"
        } else {
            Write-Error "Failed to install frontend dependencies"
            Write-Info "Output: $frontendNpmOutput"
            Pop-Location
            exit 1
        }
    } catch {
        Write-Error "Error installing frontend dependencies: $_"
        Pop-Location
        exit 1
    }
    Pop-Location
}

# Backend dependencies
Write-Info "Restoring backend packages..."
Push-Location "src\backend"
try {
    $dotnetRestoreOutput = dotnet restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Backend packages restored"
    } else {
        Write-Error "Failed to restore backend packages"
        Write-Info "Output: $dotnetRestoreOutput"
        Pop-Location
        exit 1
    }
} catch {
    Write-Error "Error restoring backend packages: $_"
    Pop-Location
    exit 1
}
Pop-Location

# Step 6: Verification tests
Write-Step 6 "Running verification tests..."

# Test TypeScript compilation
Write-Info "Verifying TypeScript compilation..."
try {
    $tscOutput = npx tsc --noEmit 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "TypeScript compilation OK"
    } else {
        Write-Error "TypeScript compilation failed"
        Write-Info "This might indicate dependency issues"
        Write-Info "Output: $tscOutput"
        exit 1
    }
} catch {
    Write-Error "Error checking TypeScript compilation: $_"
    exit 1
}

# Test backend compilation
Write-Info "Verifying backend compilation..."
Push-Location "src\backend"
try {
    $dotnetBuildOutput = dotnet build 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Backend compilation OK"
    } else {
        Write-Error "Backend compilation failed"
        Write-Info "Output: $dotnetBuildOutput"
        Pop-Location
        exit 1
    }
} catch {
    Write-Error "Error checking backend compilation: $_"
    Pop-Location
    exit 1
}
Pop-Location

# Step 7: Database information
Write-Step 7 "Database setup information..."
Write-Info "Database will be automatically created when you first run the backend"
Write-Warning "Ensure MySQL is running and secret.env has correct credentials"
Write-Info "The backend will run migrations automatically on startup"

# Step 8: Optional test data
Write-Step 8 "Optional test data setup..."
if ((Test-Path "scripts\test-data\seed-data.sql") -and !$SkipTestData) {
    $response = Read-Host "Load sample test data with anomalies? [y/N]"
    if ($response -match "^[Yy]$") {
        $mysqlPassword = Read-Host "Enter MySQL root password" -AsSecureString
        $mysqlPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($mysqlPassword))
        
        try {
            # Create database if it doesn't exist
            $createDbOutput = & $mysqlPath -u root -p"$mysqlPasswordPlain" -e "CREATE DATABASE IF NOT EXISTS ea_tracker_db;" 2>&1
            if ($LASTEXITCODE -eq 0) {
                # Load test data
                $loadDataOutput = Get-Content "scripts\test-data\seed-data.sql" | & $mysqlPath -u root -p"$mysqlPasswordPlain" ea_tracker_db 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "Test data loaded successfully"
                } else {
                    Write-Warning "Failed to load test data (you can do this later)"
                    Write-Info "Command: `"$mysqlPath`" -u root -p ea_tracker_db < scripts\test-data\seed-data.sql"
                }
            } else {
                Write-Warning "Could not connect to MySQL (check credentials)"
                Write-Info "Load test data later: `"$mysqlPath`" -u root -p ea_tracker_db < scripts\test-data\seed-data.sql"
            }
        } catch {
            Write-Warning "Error loading test data: $_"
            Write-Info "Load test data later: `"$mysqlPath`" -u root -p ea_tracker_db < scripts\test-data\seed-data.sql"
        }
    } else {
        Write-Info "Skipping test data. Load later with:"
        Write-Info "`"$mysqlPath`" -u root -p ea_tracker_db < scripts\test-data\seed-data.sql"
    }
} else {
    if (!(Test-Path "scripts\test-data\seed-data.sql")) {
        Write-Warning "Test data file not found"
    }
}

Write-Host ""
Write-Host "🎉 Setup Complete!" -ForegroundColor Green
Write-Host "=================="
Write-Host ""
Write-Host "Your ea_Tracker development environment is ready!"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. " -NoNewline; Write-Host "Review secret.env" -ForegroundColor Yellow -NoNewline; Write-Host " - Update MySQL credentials if needed"
Write-Host "2. " -NoNewline; Write-Host "Start backend:" -ForegroundColor Blue -NoNewline; Write-Host " cd src\backend && dotnet run"
Write-Host "3. " -NoNewline; Write-Host "Start frontend:" -ForegroundColor Blue -NoNewline; Write-Host " npm start (in new PowerShell window)"
Write-Host "4. " -NoNewline; Write-Host "Visit:" -ForegroundColor Green -NoNewline; Write-Host " http://localhost:3000"
Write-Host ""
Write-Host "Useful commands:"
Write-Host "• Run tests: npm run test:frontend -- --watchAll=false"
Write-Host "• Load test data: `"$mysqlPath`" -u root -p ea_tracker_db < scripts\test-data\seed-data.sql"
Write-Host "• Check health: Invoke-WebRequest http://localhost:5050/healthz"
Write-Host ""
Write-Success "Happy coding! 🚀"
Write-Host ""