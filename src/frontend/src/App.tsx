import { useState, useEffect } from "react";
import Dashboard from "./Dashboard";
import Login from "./components/Login";
import ErrorBoundary from "./components/ErrorBoundary";
import { getAuthToken, clearAuthToken } from "./lib/axios";

function App(): JSX.Element {
  const [user, setUser] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check for existing authentication on app load
    const token = getAuthToken();
    const userInfo = localStorage.getItem('user_info');
    
    if (token && userInfo) {
      try {
        setUser(JSON.parse(userInfo));
      } catch (error) {
        console.error('Failed to parse user info:', error);
        clearAuthToken();
      }
    }
    
    setLoading(false);

    // Listen for authentication events
    const handleAuthLogout = () => {
      setUser(null);
    };

    const handleAuthForbidden = () => {
      console.warn('Access forbidden. Redirecting to login.');
      setUser(null);
    };

    window.addEventListener('auth:logout', handleAuthLogout);
    window.addEventListener('auth:forbidden', handleAuthForbidden);

    return () => {
      window.removeEventListener('auth:logout', handleAuthLogout);
      window.removeEventListener('auth:forbidden', handleAuthForbidden);
    };
  }, []);

  const handleLoginSuccess = (userData: any) => {
    setUser(userData);
  };

  const handleLogout = () => {
    clearAuthToken();
    setUser(null);
  };

  if (loading) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh',
        fontSize: '1.2rem'
      }}>
        Loading...
      </div>
    );
  }

  if (!user) {
    return (
      <ErrorBoundary
        onError={(error, errorInfo) => {
          console.error('Login Error:', error, errorInfo);
        }}
      >
        <Login onLoginSuccess={handleLoginSuccess} />
      </ErrorBoundary>
    );
  }

  return (
    <ErrorBoundary
      onError={(error, errorInfo) => {
        // Log error to console in development
        console.error('Application Error:', error, errorInfo);
        
        // In production, you might want to report to an error monitoring service
        if (process.env.NODE_ENV === 'production') {
          // Example: Report to your monitoring service
        }
      }}
    >
      <div className="App">
        {/* Header with user info and logout */}
        <div style={{
          background: '#1976d2',
          color: 'white',
          padding: '0.5rem 1rem',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center'
        }}>
          <div>
            <strong>EA Tracker</strong> - Welcome, {user.username}
            {user.roles && user.roles.length > 0 && (
              <span style={{ marginLeft: '1rem', fontSize: '0.875rem' }}>
                ({user.roles.join(', ')})
              </span>
            )}
          </div>
          <button
            onClick={handleLogout}
            style={{
              background: 'transparent',
              border: '1px solid white',
              color: 'white',
              padding: '0.5rem 1rem',
              borderRadius: '4px',
              cursor: 'pointer'
            }}
          >
            Logout
          </button>
        </div>

        <ErrorBoundary
          fallback={
            <div style={{ padding: '2rem', textAlign: 'center' }}>
              <h2>Dashboard Error</h2>
              <p>The dashboard encountered an error. Please refresh the page.</p>
            </div>
          }
        >
          <Dashboard />
        </ErrorBoundary>
      </div>
    </ErrorBoundary>
  );
}

export default App;
