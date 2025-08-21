import React, { useState, useEffect } from 'react';
import { UserSummary, UpdateUserRoleRequest, ToggleUserStatusRequest } from '../types/api';
import { AdminService } from '../lib/adminService';

interface EditUserModalProps {
  isOpen: boolean;
  user: UserSummary | null;
  onClose: () => void;
  onSuccess: (message: string) => void;
  onError: (error: string) => void;
}

const EditUserModal: React.FC<EditUserModalProps> = ({
  isOpen,
  user,
  onClose,
  onSuccess,
  onError
}) => {
  const [formData, setFormData] = useState({
    role: 'User',
    isActive: true,
    reason: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [formErrors, setFormErrors] = useState<Record<string, string>>({});
  const [activeTab, setActiveTab] = useState<'role' | 'status'>('role');

  // Initialize form when user changes
  useEffect(() => {
    if (user) {
      setFormData({
        role: user.roles.length > 0 ? user.roles[0] : 'User',
        isActive: user.isActive,
        reason: ''
      });
      setFormErrors({});
      setActiveTab('role');
    }
  }, [user]);

  // Reset form when modal closes
  const resetForm = (): void => {
    setFormData({
      role: 'User',
      isActive: true,
      reason: ''
    });
    setFormErrors({});
    setActiveTab('role');
  };

  const handleClose = (): void => {
    resetForm();
    onClose();
  };

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    // Role validation
    if (!formData.role) {
      errors.role = 'Role is required';
    }

    // Reason validation for status changes
    if (activeTab === 'status' && user && formData.isActive !== user.isActive) {
      if (!formData.reason.trim()) {
        errors.reason = 'Reason is required when changing user status';
      } else if (formData.reason.length > 200) {
        errors.reason = 'Reason must be 200 characters or less';
      }
    }

    // Reason validation for role changes
    if (activeTab === 'role' && user && formData.role !== (user.roles.length > 0 ? user.roles[0] : 'User')) {
      if (!formData.reason.trim()) {
        errors.reason = 'Reason is required when changing user role';
      } else if (formData.reason.length > 200) {
        errors.reason = 'Reason must be 200 characters or less';
      }
    }

    setFormErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleInputChange = (field: string, value: string | boolean): void => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear field error when user starts typing
    if (formErrors[field]) {
      setFormErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const handleUpdateRole = async (): Promise<void> => {
    if (!user) return;

    // Check if role actually changed
    const currentRole = user.roles.length > 0 ? user.roles[0] : 'User';
    if (formData.role === currentRole) {
      onError('No changes detected for user role');
      return;
    }

    // Validate form
    if (!validateForm()) {
      return;
    }

    setIsLoading(true);

    try {
      const request: UpdateUserRoleRequest = {
        newRole: formData.role,
        reason: formData.reason.trim()
      };

      const result = await AdminService.updateUserRole(user.id, request);
      onSuccess(result);
      handleClose();
    } catch (error: any) {
      onError(error.message || 'Failed to update user role');
    } finally {
      setIsLoading(false);
    }
  };

  const handleUpdateStatus = async (): Promise<void> => {
    if (!user) return;

    // Check if status actually changed
    if (formData.isActive === user.isActive) {
      onError('No changes detected for user status');
      return;
    }

    // Validate form
    if (!validateForm()) {
      return;
    }

    setIsLoading(true);

    try {
      const request: ToggleUserStatusRequest = {
        isActive: formData.isActive,
        reason: formData.reason.trim()
      };

      const result = await AdminService.toggleUserStatus(user.id, request);
      onSuccess(result);
      handleClose();
    } catch (error: any) {
      onError(error.message || 'Failed to update user status');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (): Promise<void> => {
    // Clear previous errors
    setFormErrors({});

    if (activeTab === 'role') {
      await handleUpdateRole();
    } else {
      await handleUpdateStatus();
    }
  };

  if (!isOpen || !user) return null;

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
        <h2 style={{ fontSize: '1.5rem', fontWeight: '600', marginBottom: '0.5rem' }}>
          Edit User: {user.username}
        </h2>
        <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '1.5rem' }}>
          ID: {user.id} | Email: {user.email} | Status: {user.isActive ? 'Active' : 'Inactive'}
          {user.isLocked && <span style={{ color: '#dc2626', fontWeight: '500' }}> (Locked)</span>}
        </div>

        {/* Tab Navigation */}
        <div style={{ marginBottom: '1.5rem', borderBottom: '1px solid #e5e7eb' }}>
          <nav style={{ display: 'flex', gap: '2rem' }}>
            <button
              onClick={() => setActiveTab('role')}
              style={{
                padding: '0.75rem 0',
                fontSize: '0.875rem',
                fontWeight: '500',
                color: activeTab === 'role' ? '#3b82f6' : '#6b7280',
                backgroundColor: 'transparent',
                border: 'none',
                borderBottom: activeTab === 'role' ? '2px solid #3b82f6' : '2px solid transparent',
                cursor: 'pointer'
              }}
            >
              Change Role
            </button>
            <button
              onClick={() => setActiveTab('status')}
              style={{
                padding: '0.75rem 0',
                fontSize: '0.875rem',
                fontWeight: '500',
                color: activeTab === 'status' ? '#3b82f6' : '#6b7280',
                backgroundColor: 'transparent',
                border: 'none',
                borderBottom: activeTab === 'status' ? '2px solid #3b82f6' : '2px solid transparent',
                cursor: 'pointer'
              }}
            >
              Change Status
            </button>
          </nav>
        </div>

        {/* Role Tab */}
        {activeTab === 'role' && (
          <>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: '500' }}>
                Current Role: <span style={{ color: '#059669', fontWeight: '600' }}>{user.roles.length > 0 ? user.roles[0] : 'User'}</span>
              </label>
              <select
                value={formData.role}
                onChange={(e) => handleInputChange('role', e.target.value)}
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
          </>
        )}

        {/* Status Tab */}
        {activeTab === 'status' && (
          <>
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: '500' }}>
                Current Status: <span style={{ color: user.isActive ? '#059669' : '#dc2626', fontWeight: '600' }}>
                  {user.isActive ? 'Active' : 'Inactive'}
                </span>
              </label>
              <select
                value={formData.isActive ? 'true' : 'false'}
                onChange={(e) => handleInputChange('isActive', e.target.value === 'true')}
                style={{
                  width: '100%',
                  padding: '0.75rem',
                  border: '1px solid #d1d5db',
                  borderRadius: '6px',
                  fontSize: '1rem'
                }}
              >
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </select>
            </div>
            
            {user.isLocked && (
              <div style={{ 
                marginBottom: '1rem', 
                padding: '0.75rem', 
                backgroundColor: '#fee2e2', 
                color: '#dc2626', 
                border: '1px solid #fecaca',
                borderRadius: '6px',
                fontSize: '0.875rem'
              }}>
                <strong>Note:</strong> This user account is currently locked due to failed login attempts. 
                Activating the account will automatically unlock it.
              </div>
            )}
          </>
        )}

        {/* Reason Field - shown for both tabs when changes are detected */}
        <div style={{ marginBottom: '2rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: '500' }}>
            Reason for Change *
          </label>
          <textarea
            value={formData.reason}
            onChange={(e) => handleInputChange('reason', e.target.value)}
            placeholder={`Enter reason for ${activeTab === 'role' ? 'role' : 'status'} change...`}
            rows={3}
            style={{
              width: '100%',
              padding: '0.75rem',
              border: formErrors.reason ? '1px solid #dc2626' : '1px solid #d1d5db',
              borderRadius: '6px',
              fontSize: '1rem',
              resize: 'vertical' as const
            }}
          />
          {formErrors.reason && (
            <div style={{ color: '#dc2626', fontSize: '0.875rem', marginTop: '0.25rem' }}>
              {formErrors.reason}
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
              backgroundColor: isLoading ? '#9ca3af' : '#10b981',
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
            {isLoading ? 'Updating...' : `Update ${activeTab === 'role' ? 'Role' : 'Status'}`}
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

export default EditUserModal;