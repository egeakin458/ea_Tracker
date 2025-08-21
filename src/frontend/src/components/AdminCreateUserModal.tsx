import { useState } from 'react';
import { AdminCreateUserRequest } from '../types/api';
import { AdminService } from '../lib/adminService';

interface AdminCreateUserModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (message: string) => void;
  onError: (error: string) => void;
}

const AdminCreateUserModal: React.FC<AdminCreateUserModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
  onError
}) => {
  const [formData, setFormData] = useState<AdminCreateUserRequest>({
    username: '',
    email: '',
    password: '',
    displayName: '',
    role: 'User'
  });
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});

  // Reset form when modal closes
  const resetForm = (): void => {
    setFormData({
      username: '',
      email: '',
      password: '',
      displayName: '',
      role: 'User'
    });
    setConfirmPassword('');
    setFormErrors({});
  };

  const handleClose = (): void => {
    resetForm();
    onClose();
  };

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    // Username validation
    if (!formData.username.trim()) {
      errors.username = 'Username is required';
    } else if (formData.username.length > 100) {
      errors.username = 'Username must be 100 characters or less';
    }

    // Email validation
    if (!formData.email.trim()) {
      errors.email = 'Email is required';
    } else if (formData.email.length > 255) {
      errors.email = 'Email must be 255 characters or less';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = 'Please enter a valid email address';
    }

    // Password validation
    if (!formData.password) {
      errors.password = 'Password is required';
    } else if (formData.password.length < 6) {
      errors.password = 'Password must be at least 6 characters';
    } else if (formData.password.length > 200) {
      errors.password = 'Password must be 200 characters or less';
    }

    // Confirm password validation
    if (!confirmPassword) {
      errors.confirmPassword = 'Please confirm your password';
    } else if (confirmPassword !== formData.password) {
      errors.confirmPassword = 'Passwords do not match';
    }

    // Display name validation (optional but if provided, must be within limit)
    if (formData.displayName && formData.displayName.length > 200) {
      errors.displayName = 'Display name must be 200 characters or less';
    }

    // Role validation
    if (!formData.role) {
      errors.role = 'Role is required';
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleInputChange = (field: keyof AdminCreateUserRequest, value: string): void => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear field error when user starts typing
    if (formErrors[field]) {
      setFormErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const handleConfirmPasswordChange = (value: string): void => {
    setConfirmPassword(value);
    // Clear confirm password error when user starts typing
    if (formErrors.confirmPassword) {
      setFormErrors(prev => ({ ...prev, confirmPassword: '' }));
    }
  };

  const handleSubmit = async (): Promise<void> => {
    // Clear previous errors
    setFormErrors({});

    // Validate form
    if (!validateForm()) {
      return;
    }

    setIsLoading(true);

    try {
      // Create user data excluding displayName if empty
      const userData: AdminCreateUserRequest = {
        username: formData.username.trim(),
        email: formData.email.trim(),
        password: formData.password,
        role: formData.role
      };

      // Only include displayName if it's not empty
      if (formData.displayName && formData.displayName.trim()) {
        userData.displayName = formData.displayName.trim();
      }

      await AdminService.createUser(userData);
      
      onSuccess(`User "${userData.username}" created successfully`);
      handleClose();
    } catch (error: any) {
      onError(error.message || 'Failed to create user');
    } finally {
      setIsLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div style={{
      position: 'fixed',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      backgroundColor: 'rgba(0, 0, 0, 0.5)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      zIndex: 1000
    }}>
      <div style={{
        backgroundColor: 'white',
        padding: '2rem',
        borderRadius: '8px',
        width: '500px',
        maxWidth: '90vw',
        maxHeight: '90vh',
        overflow: 'auto'
      }}>
        <h2 style={{ fontSize: '1.5rem', fontWeight: '600', marginBottom: '1.5rem' }}>
          Create New User
        </h2>
        
        {/* Username Field */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: '500' }}>
            Username *
          </label>
          <input
            type="text"
            value={formData.username}
            onChange={(e) => handleInputChange('username', e.target.value)}
            placeholder="Enter username"
            style={{
              width: '100%',
              padding: '0.75rem',
              border: formErrors.username ? '1px solid #dc2626' : '1px solid #d1d5db',
              borderRadius: '6px',
              fontSize: '1rem'
            }}
          />
          {formErrors.username && (
            <div style={{ color: '#dc2626', fontSize: '0.875rem', marginTop: '0.25rem' }}>
              {formErrors.username}
            </div>
          )}
        </div>

        {/* Email Field */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: '500' }}>
            Email *
          </label>
          <input
            type="email"
            value={formData.email}
            onChange={(e) => handleInputChange('email', e.target.value)}
            placeholder="Enter email address"
            style={{
              width: '100%',
              padding: '0.75rem',
              border: formErrors.email ? '1px solid #dc2626' : '1px solid #d1d5db',
              borderRadius: '6px',
              fontSize: '1rem'
            }}
          />
          {formErrors.email && (
            <div style={{ color: '#dc2626', fontSize: '0.875rem', marginTop: '0.25rem' }}>
              {formErrors.email}
            </div>
          )}
        </div>

        {/* Display Name Field */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: '500' }}>
            Display Name
          </label>
          <input
            type="text"
            value={formData.displayName}
            onChange={(e) => handleInputChange('displayName', e.target.value)}
            placeholder="Enter display name (optional)"
            style={{
              width: '100%',
              padding: '0.75rem',
              border: formErrors.displayName ? '1px solid #dc2626' : '1px solid #d1d5db',
              borderRadius: '6px',
              fontSize: '1rem'
            }}
          />
          {formErrors.displayName && (
            <div style={{ color: '#dc2626', fontSize: '0.875rem', marginTop: '0.25rem' }}>
              {formErrors.displayName}
            </div>
          )}
        </div>

        {/* Role Field */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: '500' }}>
            Role *
          </label>
          <select
            value={formData.role}
            onChange={(e) => handleInputChange('role', e.target.value as 'User' | 'Admin')}
            style={{
              width: '100%',
              padding: '0.75rem',
              border: formErrors.role ? '1px solid #dc2626' : '1px solid #d1d5db',
              borderRadius: '6px',
              fontSize: '1rem'
            }}
          >
            <option value="User">User</option>
            <option value="Admin">Admin</option>
          </select>
          {formErrors.role && (
            <div style={{ color: '#dc2626', fontSize: '0.875rem', marginTop: '0.25rem' }}>
              {formErrors.role}
            </div>
          )}
        </div>

        {/* Password Field */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: '500' }}>
            Password *
          </label>
          <input
            type="password"
            value={formData.password}
            onChange={(e) => handleInputChange('password', e.target.value)}
            placeholder="Enter password (minimum 6 characters)"
            style={{
              width: '100%',
              padding: '0.75rem',
              border: formErrors.password ? '1px solid #dc2626' : '1px solid #d1d5db',
              borderRadius: '6px',
              fontSize: '1rem'
            }}
          />
          {formErrors.password && (
            <div style={{ color: '#dc2626', fontSize: '0.875rem', marginTop: '0.25rem' }}>
              {formErrors.password}
            </div>
          )}
        </div>

        {/* Confirm Password Field */}
        <div style={{ marginBottom: '2rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: '500' }}>
            Confirm Password *
          </label>
          <input
            type="password"
            value={confirmPassword}
            onChange={(e) => handleConfirmPasswordChange(e.target.value)}
            placeholder="Confirm password"
            style={{
              width: '100%',
              padding: '0.75rem',
              border: formErrors.confirmPassword ? '1px solid #dc2626' : '1px solid #d1d5db',
              borderRadius: '6px',
              fontSize: '1rem'
            }}
          />
          {formErrors.confirmPassword && (
            <div style={{ color: '#dc2626', fontSize: '0.875rem', marginTop: '0.25rem' }}>
              {formErrors.confirmPassword}
            </div>
          )}
        </div>

        {/* Action Buttons */}
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '1rem' }}>
          <button
            onClick={handleClose}
            disabled={isLoading}
            style={{
              padding: '0.75rem 1.5rem',
              backgroundColor: '#6b7280',
              color: 'white',
              border: 'none',
              borderRadius: '6px',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              fontWeight: '500',
              opacity: isLoading ? 0.6 : 1
            }}
          >
            Cancel
          </button>
          <button
            onClick={handleSubmit}
            disabled={isLoading}
            style={{
              padding: '0.75rem 1.5rem',
              backgroundColor: isLoading ? '#9ca3af' : '#3b82f6',
              color: 'white',
              border: 'none',
              borderRadius: '6px',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              fontWeight: '500',
              display: 'flex',
              alignItems: 'center',
              gap: '0.5rem'
            }}
          >
            {isLoading && (
              <div style={{
                width: '16px',
                height: '16px',
                border: '2px solid #ffffff40',
                borderTop: '2px solid #ffffff',
                borderRadius: '50%',
                animation: 'spin 1s linear infinite'
              }} />
            )}
            {isLoading ? 'Creating...' : 'Create User'}
          </button>
        </div>

        {/* CSS for loading spinner */}
        <style>{`
          @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
          }
        `}</style>
      </div>
    </div>
  );
};

export default AdminCreateUserModal;