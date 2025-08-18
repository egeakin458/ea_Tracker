import React, { Component, ErrorInfo, ReactNode } from 'react';

export interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
  errorId: string;
  errorTime: Date;
}

export interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
  resetKeys?: Array<string | number>;
  resetOnPropsChange?: boolean;
}

/**
 * React Error Boundary component that catches JavaScript errors anywhere in the child component tree,
 * logs those errors, and displays a fallback UI instead of the component tree that crashed.
 * 
 * Features:
 * - Automatic error recovery with reset functionality
 * - Error reporting and logging
 * - Customizable fallback UI
 * - Security-conscious error information exposure
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  private resetTimeoutId?: number;

  constructor(props: ErrorBoundaryProps) {
    super(props);
    
    this.state = {
      hasError: false,
      errorId: '',
      errorTime: new Date()
    };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    // Update state so the next render will show the fallback UI
    return {
      hasError: true,
      error,
      errorId: Math.random().toString(36).substring(2, 10),
      errorTime: new Date()
    };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // Log error details (in production, this should go to a logging service)
    const errorDetails = {
      message: error.message,
      stack: error.stack,
      componentStack: errorInfo.componentStack,
      errorId: this.state.errorId,
      timestamp: this.state.errorTime.toISOString(),
      userAgent: navigator.userAgent,
      url: window.location.href
    };

    console.error('React Error Boundary caught an error:', errorDetails);

    // Call custom error handler if provided
    if (this.props.onError) {
      this.props.onError(error, errorInfo);
    }

    // In production, report to error monitoring service
    if (process.env.NODE_ENV === 'production') {
      this.reportErrorToService(errorDetails);
    }
  }

  componentDidUpdate(prevProps: ErrorBoundaryProps) {
    const { resetKeys, resetOnPropsChange } = this.props;
    const { hasError } = this.state;

    // Auto-reset error boundary when resetKeys change
    if (hasError && resetKeys) {
      const hasResetKeyChanged = resetKeys.some((key, index) => 
        key !== prevProps.resetKeys?.[index]
      );
      
      if (hasResetKeyChanged) {
        this.resetErrorBoundary();
      }
    }

    // Reset when any prop changes if resetOnPropsChange is true
    if (hasError && resetOnPropsChange && prevProps !== this.props) {
      this.resetErrorBoundary();
    }
  }

  componentWillUnmount() {
    if (this.resetTimeoutId) {
      window.clearTimeout(this.resetTimeoutId);
    }
  }

  private reportErrorToService = (errorDetails: any) => {
    // In a real application, integrate with error monitoring services like:
    // - Sentry
    // - LogRocket
    // - Bugsnag
    // - Application Insights
    
    try {
      // Example: Send to your API endpoint
      fetch('/api/errors/client', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          ...errorDetails,
          // Remove sensitive information
          stack: errorDetails.stack ? 'Stack trace available' : undefined,
          componentStack: errorDetails.componentStack ? 'Component stack available' : undefined
        }),
      }).catch(() => {
        // Fail silently for error reporting to avoid cascading failures
      });
    } catch {
      // Fail silently
    }
  };

  private resetErrorBoundary = () => {
    this.setState({
      hasError: false,
      error: undefined,
      errorId: '',
      errorTime: new Date()
    });
  };

  private handleRetry = () => {
    this.resetErrorBoundary();
  };

  private handleReload = () => {
    window.location.reload();
  };

  render() {
    const { hasError, error, errorId, errorTime } = this.state;
    const { children, fallback } = this.props;

    if (hasError) {
      // Custom fallback UI if provided
      if (fallback) {
        return fallback;
      }

      // Default error UI
      return (
        <div className="error-boundary" style={{
          padding: '2rem',
          margin: '1rem',
          border: '2px solid #ff6b6b',
          borderRadius: '8px',
          backgroundColor: '#fff5f5',
          fontFamily: 'system-ui, -apple-system, sans-serif'
        }}>
          <div style={{ marginBottom: '1rem' }}>
            <h2 style={{ 
              color: '#d63031', 
              margin: '0 0 0.5rem 0',
              fontSize: '1.5rem'
            }}>
              Something went wrong
            </h2>
            <p style={{ 
              color: '#636e72', 
              margin: 0,
              fontSize: '0.9rem'
            }}>
              An unexpected error occurred. Please try refreshing the page or contact support if the problem persists.
            </p>
          </div>

          {process.env.NODE_ENV === 'development' && error && (
            <details style={{ 
              marginBottom: '1rem',
              padding: '0.5rem',
              backgroundColor: '#f8f9fa',
              border: '1px solid #dee2e6',
              borderRadius: '4px'
            }}>
              <summary style={{ 
                cursor: 'pointer',
                fontWeight: 'bold',
                color: '#495057'
              }}>
                Error Details (Development Only)
              </summary>
              <div style={{ 
                marginTop: '0.5rem',
                fontSize: '0.8rem',
                fontFamily: 'monospace',
                color: '#495057'
              }}>
                <div><strong>Message:</strong> {error.message}</div>
                <div><strong>Error ID:</strong> {errorId}</div>
                <div><strong>Time:</strong> {errorTime.toLocaleString()}</div>
              </div>
            </details>
          )}

          <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
            <button
              onClick={this.handleRetry}
              style={{
                padding: '0.5rem 1rem',
                backgroundColor: '#0984e3',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
                fontSize: '0.9rem'
              }}
              onMouseOver={(e) => e.currentTarget.style.backgroundColor = '#0770c7'}
              onMouseOut={(e) => e.currentTarget.style.backgroundColor = '#0984e3'}
            >
              Try Again
            </button>
            
            <button
              onClick={this.handleReload}
              style={{
                padding: '0.5rem 1rem',
                backgroundColor: '#636e72',
                color: 'white',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
                fontSize: '0.9rem'
              }}
              onMouseOver={(e) => e.currentTarget.style.backgroundColor = '#5a6169'}
              onMouseOut={(e) => e.currentTarget.style.backgroundColor = '#636e72'}
            >
              Reload Page
            </button>
          </div>

          {process.env.NODE_ENV === 'production' && (
            <div style={{
              marginTop: '1rem',
              padding: '0.75rem',
              backgroundColor: '#e3f2fd',
              border: '1px solid #1976d2',
              borderRadius: '4px',
              fontSize: '0.85rem',
              color: '#1565c0'
            }}>
              <strong>Error ID:</strong> {errorId}
              <br />
              <small>Please reference this ID when contacting support.</small>
            </div>
          )}
        </div>
      );
    }

    return children;
  }
}

export default ErrorBoundary;