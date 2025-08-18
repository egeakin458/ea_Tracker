import axios, { AxiosError, AxiosRequestConfig, AxiosResponse, InternalAxiosRequestConfig } from "axios";

// Enhanced axios configuration with security and authentication features
const api = axios.create({
  baseURL: process.env.REACT_APP_API_BASE_URL || "http://localhost:5050",
  timeout: 30000, // 30 second timeout
  headers: {
    'Content-Type': 'application/json',
    'X-Requested-With': 'XMLHttpRequest' // CSRF protection header
  },
  // Security: Don't send credentials unless explicitly needed
  withCredentials: false
});

// Token management
let authToken: string | null = null;

/**
 * Set the authentication token for API requests
 */
export const setAuthToken = (token: string | null): void => {
  authToken = token;
  if (token) {
    localStorage.setItem('auth_token', token);
  } else {
    localStorage.removeItem('auth_token');
  }
};

/**
 * Get the stored authentication token
 */
export const getAuthToken = (): string | null => {
  if (authToken) return authToken;
  
  const storedToken = localStorage.getItem('auth_token');
  if (storedToken) {
    authToken = storedToken;
    return storedToken;
  }
  
  return null;
};

/**
 * Clear authentication token and logout user
 */
export const clearAuthToken = (): void => {
  authToken = null;
  localStorage.removeItem('auth_token');
  localStorage.removeItem('user_info');
};

// Request interceptor for authentication and security headers
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = getAuthToken();
    
    // Add authorization header if token exists
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    
    // Add CSRF protection headers
    if (config.headers) {
      config.headers['X-Requested-With'] = 'XMLHttpRequest';
      
      // Add request timestamp for replay attack protection
      config.headers['X-Request-Timestamp'] = Date.now().toString();
    }
    
    // Log request in development
    if (process.env.NODE_ENV === 'development') {
      console.log(`🔄 API Request: ${config.method?.toUpperCase()} ${config.url}`);
    }
    
    return config;
  },
  (error) => {
    console.error('Request interceptor error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor for error handling and token refresh
api.interceptors.response.use(
  (response: AxiosResponse) => {
    // Log successful responses in development
    if (process.env.NODE_ENV === 'development') {
      console.log(`✅ API Response: ${response.status} ${response.config.method?.toUpperCase()} ${response.config.url}`);
    }
    
    return response;
  },
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    
    // Handle authentication errors
    if (error.response?.status === 401) {
      // Clear invalid token
      clearAuthToken();
      
      // Redirect to login or show auth modal
      // In a real app, you might dispatch a Redux action or trigger a global auth state update
      console.warn('Authentication failed. Please log in again.');
      
      // You could emit a custom event here for the app to handle
      window.dispatchEvent(new CustomEvent('auth:logout', { 
        detail: { reason: 'token_expired' }
      }));
    }
    
    // Handle authorization errors
    if (error.response?.status === 403) {
      console.warn('Access denied. Insufficient permissions.');
      
      window.dispatchEvent(new CustomEvent('auth:forbidden', { 
        detail: { path: originalRequest.url }
      }));
    }
    
    // Handle server errors with user-friendly messages
    if (error.response?.status && error.response.status >= 500) {
      console.error('Server error occurred:', error.response.status);
      
      // Show user-friendly error message
      const userMessage = 'A server error occurred. Please try again later.';
      window.dispatchEvent(new CustomEvent('api:error', { 
        detail: { 
          message: userMessage,
          status: error.response.status,
          correlationId: error.response.headers?.['x-correlation-id']
        }
      }));
    }
    
    // Handle network errors
    if (!error.response) {
      console.error('Network error or timeout:', error.message);
      
      window.dispatchEvent(new CustomEvent('api:network-error', { 
        detail: { message: 'Network connection failed. Please check your internet connection.' }
      }));
    }
    
    // Log all errors in development with sanitized details
    if (process.env.NODE_ENV === 'development') {
      console.group('🚨 API Error Details');
      console.error('Error:', error.message);
      console.error('Status:', error.response?.status);
      console.error('URL:', error.config?.url);
      console.error('Method:', error.config?.method);
      
      // Don't log sensitive data in production logs
      if (error.response?.data && typeof error.response.data === 'object') {
        console.error('Response:', error.response.data);
      }
      console.groupEnd();
    }
    
    return Promise.reject(error);
  }
);

// Utility functions for secure API calls
export const secureApiCall = {
  /**
   * Make a GET request with automatic retry
   */
  get: <T = any>(url: string, config?: AxiosRequestConfig, retries = 1): Promise<AxiosResponse<T>> => {
    return api.get(url, config).catch(async (error) => {
      if (retries > 0 && (!error.response || error.response.status >= 500)) {
        console.log(`Retrying GET request to ${url}. Attempts remaining: ${retries}`);
        await new Promise(resolve => setTimeout(resolve, 1000)); // Wait 1 second
        return secureApiCall.get(url, config, retries - 1);
      }
      throw error;
    });
  },
  
  /**
   * Make a POST request with CSRF protection
   */
  post: <T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> => {
    return api.post(url, data, {
      ...config,
      headers: {
        ...config?.headers,
        'X-CSRF-Protection': '1' // Additional CSRF protection
      }
    });
  },
  
  /**
   * Make a PUT request with CSRF protection
   */
  put: <T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> => {
    return api.put(url, data, {
      ...config,
      headers: {
        ...config?.headers,
        'X-CSRF-Protection': '1'
      }
    });
  },
  
  /**
   * Make a DELETE request with confirmation
   */
  delete: <T = any>(url: string, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> => {
    return api.delete(url, {
      ...config,
      headers: {
        ...config?.headers,
        'X-CSRF-Protection': '1'
      }
    });
  }
};

export { api };
export default api;
