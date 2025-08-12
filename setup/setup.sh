#!/bin/bash

# ea_Tracker Developer Setup Script
# Automates the complete setup process for new developers
# Usage: ./setup.sh

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Helper functions
print_step() {
    echo -e "\n${BLUE}[$1/8]${NC} $2"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_info() {
    echo -e "   $1"
}

# Check if we're in the right directory
validate_directory() {
    if [[ ! -f "ea_Tracker.sln" ]] || [[ ! -f "package.json" ]]; then
        print_error "Please run this script from the ea_Tracker project root directory"
        print_info "Expected files: ea_Tracker.sln, package.json"
        exit 1
    fi
}

# Version comparison function
version_ge() {
    printf '%s\n%s\n' "$2" "$1" | sort -C -V
}

echo "🚀 ea_Tracker Developer Setup"
echo "==============================="

# Validate we're in the right place
validate_directory

# Step 1: Check prerequisites
print_step 1 "Checking prerequisites..."

# Check Node.js
if command -v node >/dev/null 2>&1; then
    NODE_VERSION=$(node --version | sed 's/v//')
    if version_ge "$NODE_VERSION" "18.0.0"; then
        print_success "Node.js $NODE_VERSION (>= 18.0.0)"
    else
        print_error "Node.js version $NODE_VERSION is too old"
        print_info "Required: Node.js 18.0.0 or higher"
        print_info "Install from: https://nodejs.org/ or use NVM"
        exit 1
    fi
else
    print_error "Node.js not found"
    print_info "Install Node.js 18+ from https://nodejs.org/"
    print_info "Or install NVM: curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash"
    exit 1
fi

# Check npm
if command -v npm >/dev/null 2>&1; then
    NPM_VERSION=$(npm --version)
    print_success "npm $NPM_VERSION"
else
    print_error "npm not found (should come with Node.js)"
    exit 1
fi

# Check .NET
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_VERSION=$(dotnet --version 2>/dev/null)
    if [[ $? -eq 0 ]]; then
        if [[ "$DOTNET_VERSION" == 8.* ]] || [[ "$DOTNET_VERSION" > "8" ]]; then
            print_success ".NET SDK $DOTNET_VERSION"
        else
            print_warning ".NET SDK $DOTNET_VERSION (recommended: 8.0+)"
        fi
    else
        print_error "Error checking .NET version"
        exit 1
    fi
else
    print_error ".NET SDK not found"
    print_info "Install .NET 8.0 SDK from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Check MySQL (more thorough)
MYSQL_OK=false
MYSQL_PATH=""
if command -v mysql >/dev/null 2>&1; then
    # Try to get MySQL version
    MYSQL_VERSION=$(mysql --version 2>/dev/null | grep -oP 'mysql\s+Ver\s+\K[0-9]+\.[0-9]+' | head -1)
    if [[ -n "$MYSQL_VERSION" ]]; then
        print_success "MySQL $MYSQL_VERSION found"
        MYSQL_OK=true
        MYSQL_PATH="mysql"
    else
        print_warning "MySQL command found but version check failed"
        MYSQL_PATH="mysql"
    fi
fi

# Check if MySQL service is running (Linux/Mac specific)
if [[ "$MYSQL_OK" == true ]]; then
    if command -v systemctl >/dev/null 2>&1; then
        if systemctl is-active --quiet mysql 2>/dev/null || systemctl is-active --quiet mysqld 2>/dev/null; then
            print_success "MySQL service is running"
        else
            print_warning "MySQL service might not be running"
            print_info "Try: sudo systemctl start mysql"
        fi
    elif command -v brew >/dev/null 2>&1; then
        if brew services list | grep mysql | grep started >/dev/null 2>&1; then
            print_success "MySQL service is running (Homebrew)"
        else
            print_warning "MySQL service might not be running"
            print_info "Try: brew services start mysql"
        fi
    else
        print_info "Cannot check MySQL service status on this system"
    fi
fi

if [[ "$MYSQL_OK" == false ]]; then
    print_error "MySQL not found or not working"
    print_info "Install MySQL 8.0+:"
    print_info "  Ubuntu/Debian: sudo apt update && sudo apt install mysql-server"
    print_info "  macOS: brew install mysql"
    print_info "  Or download from: https://dev.mysql.com/downloads/mysql/"
    exit 1
fi

# Step 2: Node.js version management
print_step 2 "Managing Node.js version..."
if [[ -f ".nvmrc" ]]; then
    if [[ -s "$HOME/.nvm/nvm.sh" ]]; then
        # Source NVM if available
        source "$HOME/.nvm/nvm.sh"
        if command -v nvm >/dev/null 2>&1; then
            nvm use
            print_success "Switched to Node.js version from .nvmrc"
        else
            print_warning "NVM not available in current shell"
            print_info "Current Node.js version should work if >= 18.0.0"
        fi
    else
        print_warning "NVM not installed, using system Node.js"
        print_info "Current version is compatible"
    fi
else
    print_warning ".nvmrc file not found"
fi

# Step 3: Environment configuration
print_step 3 "Setting up environment configuration..."

# Backend environment
if [[ ! -f "secret.env" ]]; then
    if [[ -f "secret.env.example" ]]; then
        cp secret.env.example secret.env
        print_success "Created secret.env from template"
        print_warning "IMPORTANT: Edit secret.env with your MySQL credentials!"
        print_info "Example: DEFAULT_CONNECTION=Server=localhost;Database=ea_tracker_db;Uid=root;Pwd=your_password;"
    else
        print_error "secret.env.example not found"
        exit 1
    fi
else
    print_success "secret.env already exists"
fi

# Frontend environment
if [[ ! -f "src/frontend/.env" ]]; then
    if [[ -f "src/frontend/.env.example" ]]; then
        cp src/frontend/.env.example src/frontend/.env
        print_success "Created frontend .env from template"
    else
        print_error "src/frontend/.env.example not found"
        exit 1
    fi
else
    print_success "Frontend .env already exists"
fi

# Step 4: Install .NET tools
print_step 4 "Installing .NET Entity Framework tools..."
if dotnet tool install --global dotnet-ef >/dev/null 2>&1; then
    print_success "Entity Framework tools installed"
elif dotnet tool update --global dotnet-ef >/dev/null 2>&1; then
    print_success "Entity Framework tools updated"
else
    # Try to check if it's already installed
    if dotnet ef --version >/dev/null 2>&1; then
        print_success "Entity Framework tools already available"
    else
        print_error "Failed to install Entity Framework tools"
        print_info "Try manually: dotnet tool install --global dotnet-ef"
        exit 1
    fi
fi

# Step 5: Install dependencies
print_step 5 "Installing project dependencies..."

# Clean install from lockfile
print_info "Installing root dependencies..."
if npm ci >/dev/null 2>&1; then
    print_success "Root dependencies installed (npm ci)"
else
    print_warning "npm ci failed, trying npm install..."
    if npm install >/dev/null; then
        print_success "Root dependencies installed (npm install)"
    else
        print_error "Failed to install root dependencies"
        exit 1
    fi
fi

# Frontend dependencies (they should be installed by root npm ci, but verify)
if [[ -d "src/frontend/node_modules" ]]; then
    print_success "Frontend dependencies available"
else
    print_info "Installing frontend dependencies separately..."
    cd src/frontend
    if npm install >/dev/null; then
        print_success "Frontend dependencies installed"
    else
        print_error "Failed to install frontend dependencies"
        exit 1
    fi
    cd ../..
fi

# Backend dependencies
print_info "Restoring backend packages..."
cd src/backend
if dotnet restore >/dev/null; then
    print_success "Backend packages restored"
else
    print_error "Failed to restore backend packages"
    exit 1
fi
cd ../..

# Step 6: Verification tests
print_step 6 "Running verification tests..."

# Test TypeScript compilation
print_info "Verifying TypeScript compilation..."
if npx tsc --noEmit >/dev/null 2>&1; then
    print_success "TypeScript compilation OK"
else
    print_error "TypeScript compilation failed"
    print_info "This might indicate dependency issues"
    exit 1
fi

# Test backend compilation
print_info "Verifying backend compilation..."
cd src/backend
if dotnet build >/dev/null 2>&1; then
    print_success "Backend compilation OK"
else
    print_error "Backend compilation failed"
    cd ../..
    exit 1
fi
cd ../..

# Step 7: Database information
print_step 7 "Database setup information..."
print_info "Database will be automatically created when you first run the backend"
print_warning "Ensure MySQL is running and secret.env has correct credentials"
print_info "The backend will run migrations automatically on startup"

# Step 8: Optional test data
print_step 8 "Optional test data setup..."
if [[ -f "scripts/test-data/seed-data.sql" ]]; then
    echo -n "Load sample test data with anomalies? [y/N]: "
    read -r response
    if [[ "$response" =~ ^[Yy]$ ]]; then
        echo -n "Enter MySQL root password: "
        read -s mysql_password
        echo ""
        
        if "$MYSQL_PATH" -u root -p"$mysql_password" -e "CREATE DATABASE IF NOT EXISTS ea_tracker_db;" >/dev/null 2>&1; then
            if "$MYSQL_PATH" -u root -p"$mysql_password" ea_tracker_db < scripts/test-data/seed-data.sql >/dev/null 2>&1; then
                print_success "Test data loaded successfully"
            else
                print_warning "Failed to load test data (you can do this later)"
                print_info "Command: \"$MYSQL_PATH\" -u root -p ea_tracker_db < scripts/test-data/seed-data.sql"
            fi
        else
            print_warning "Could not connect to MySQL (check credentials)"
            print_info "Load test data later: \"$MYSQL_PATH\" -u root -p ea_tracker_db < scripts/test-data/seed-data.sql"
        fi
    else
        print_info "Skipping test data. Load later with:"
        print_info "\"$MYSQL_PATH\" -u root -p ea_tracker_db < scripts/test-data/seed-data.sql"
    fi
else
    print_warning "Test data file not found"
fi

echo ""
echo "🎉 Setup Complete!"
echo "=================="
echo ""
echo "Your ea_Tracker development environment is ready!"
echo ""
echo "Next steps:"
echo "1. ${YELLOW}Review secret.env${NC} - Update MySQL credentials if needed"
echo "2. ${BLUE}Start backend:${NC} cd src/backend && dotnet run"
echo "3. ${BLUE}Start frontend:${NC} npm start (in new terminal)"
echo "4. ${GREEN}Visit:${NC} http://localhost:3000"
echo ""
echo "Useful commands:"
echo "• Run tests: npm run test:frontend -- --watchAll=false"
echo "• Load test data: \"$MYSQL_PATH\" -u root -p ea_tracker_db < scripts/test-data/seed-data.sql"
echo "• Check health: curl http://localhost:5050/healthz"
echo ""
print_success "Happy coding! 🚀"
echo ""