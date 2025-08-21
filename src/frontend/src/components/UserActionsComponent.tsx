import React, { useState } from 'react';
import { UserSummary } from '../types/api';
import { AdminService } from '../lib/adminService';

interface UserActionsComponentProps {
  user: UserSummary | null;
  onSuccess: (message: string) => void;
  onError: (error: string) => void;
  onUserUpdated: () => void; // Callback to refresh user list
}

const UserActionsComponent: React.FC<UserActionsComponentProps> = ({
  user,
  onSuccess,
  onError,
  onUserUpdated
}) => {
  const [isLoading, setIsLoading] = useState<string | null>(null); // Track which action is loading

  const handleQuickToggleStatus = async (): Promise<void> => {
    if (!user) return;

    const action = user.isActive ? 'deactivate' : 'activate';
    const confirmMessage = user.isActive 
      ? `Are you sure you want to deactivate user "${user.username}"? They will not be able to log in.`
      : `Are you sure you want to activate user "${user.username}"? ${user.isLocked ? 'This will also unlock their account.' : ''}`;

    if (!window.confirm(confirmMessage)) {
      return;
    }

    setIsLoading('status');

    try {
      const result = await AdminService.toggleUserStatus(user.id, {
        isActive: !user.isActive,
        reason: `Quick ${action} by admin`
      });

      onSuccess(result);
      onUserUpdated();
    } catch (error: any) {
      onError(error.message || `Failed to ${action} user`);
    } finally {
      setIsLoading(null);
    }
  };

  const handleQuickRoleToggle = async (): Promise<void> => {
    if (!user) return;

    const currentRole = user.roles.length > 0 ? user.roles[0] : 'User';
    const newRole = currentRole === 'Admin' ? 'User' : 'Admin';
    
    const confirmMessage = `Are you sure you want to change "${user.username}"'s role from ${currentRole} to ${newRole}?`;

    if (!window.confirm(confirmMessage)) {
      return;
    }

    setIsLoading('role');

    try {
      const result = await AdminService.updateUserRole(user.id, {
        newRole,
        reason: `Quick role change by admin from ${currentRole} to ${newRole}`
      });

      onSuccess(result);
      onUserUpdated();
    } catch (error: any) {
      onError(error.message || 'Failed to update user role');
    } finally {
      setIsLoading(null);
    }
  };

  const handleDeleteUser = async (): Promise<void> => {
    if (!user) return;

    const confirmMessage = `Are you sure you want to DELETE user "${user.username}"?\n\nThis action cannot be undone. The user will be permanently removed from the system.\n\nType the username "${user.username}" to confirm:`;
    
    const confirmation = window.prompt(confirmMessage);
    
    if (confirmation !== user.username) {
      if (confirmation !== null) { // User didn't cancel
        onError('Username confirmation does not match. User deletion cancelled.');
      }
      return;
    }

    setIsLoading('delete');

    try {
      const result = await AdminService.deleteUser(user.id);
      onSuccess(result);
      onUserUpdated();
    } catch (error: any) {
      onError(error.message || 'Failed to delete user');
    } finally {
      setIsLoading(null);
    }
  };

  if (!user) {
    return (
      <div style={{ 
        padding: '2rem', 
        textAlign: 'center', 
        color: '#6b7280',
        backgroundColor: 'white',
        border: '1px solid #e5e7eb',
        borderRadius: '8px'
      }}>
        <div style={{ fontSize: '1.5rem', marginBottom: '0.5rem' }}>⚙️</div>
        <div style={{ fontSize: '1rem', fontWeight: '500' }}>User Actions</div>
        <div style={{ fontSize: '0.875rem', marginTop: '0.5rem' }}>
          Select a user to view available actions
        </div>
      </div>
    );
  }

  return (
    <div style={{
      backgroundColor: 'white',
      border: '1px solid #e5e7eb',
      borderRadius: '8px',
      padding: '1.5rem'
    }}>
      <h3 style={{ fontSize: '1.25rem', fontWeight: '600', marginBottom: '1rem', color: '#1f2937' }}>
        Quick Actions: {user.displayName || user.username}
      </h3>

      {/* User Info Summary */}
      <div style={{ 
        marginBottom: '1.5rem',
        padding: '1rem',
        backgroundColor: '#f9fafb',
        borderRadius: '6px',
        fontSize: '0.875rem'
      }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>
          <div><strong>ID:</strong> {user.id}</div>
          <div><strong>Email:</strong> {user.email}</div>
          <div><strong>Role:</strong> {user.roles.length > 0 ? user.roles[0] : 'User'}</div>
          <div><strong>Status:</strong> 
            <span style={{ 
              marginLeft: '0.25rem',
              color: user.isLocked ? '#dc2626' : (user.isActive ? '#059669' : '#f59e0b')
            }}>
              {user.isLocked ? 'Locked' : (user.isActive ? 'Active' : 'Inactive')}
            </span>
          </div>
        </div>
        {user.failedLoginAttempts > 0 && (
          <div style={{ marginTop: '0.5rem', color: '#dc2626' }}>
            <strong>Failed Attempts:</strong> {user.failedLoginAttempts}
          </div>
        )}
      </div>

      {/* Action Buttons */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem' }}>
        
        {/* Toggle Status Button */}
        <button
          onClick={handleQuickToggleStatus}
          disabled={isLoading !== null}
          style={{
            padding: '0.75rem 1rem',
            backgroundColor: user.isActive ? '#f59e0b' : '#10b981',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: isLoading !== null ? 'not-allowed' : 'pointer',
            fontWeight: '500',
            fontSize: '0.875rem',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '0.5rem',
            opacity: isLoading !== null ? 0.6 : 1
          }}
        >
          {isLoading === 'status' && (
            <div style={{
              width: '16px',
              height: '16px',
              border: '2px solid #ffffff40',
              borderTop: '2px solid #ffffff',
              borderRadius: '50%',
              animation: 'spin 1s linear infinite'
            }} />
          )}
          {user.isActive ? '⏸️ Deactivate User' : '▶️ Activate User'}
          {user.isLocked && !user.isActive && ' (Will Unlock)'}
        </button>

        {/* Toggle Role Button */}
        <button
          onClick={handleQuickRoleToggle}
          disabled={isLoading !== null}
          style={{
            padding: '0.75rem 1rem',
            backgroundColor: '#3b82f6',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: isLoading !== null ? 'not-allowed' : 'pointer',
            fontWeight: '500',
            fontSize: '0.875rem',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '0.5rem',
            opacity: isLoading !== null ? 0.6 : 1
          }}
        >
          {isLoading === 'role' && (
            <div style={{
              width: '16px',
              height: '16px',
              border: '2px solid #ffffff40',
              borderTop: '2px solid #ffffff',
              borderRadius: '50%',
              animation: 'spin 1s linear infinite'
            }} />
          )}
          {(user.roles.length > 0 ? user.roles[0] : 'User') === 'Admin' 
            ? '👤 Change to User' 
            : '👑 Change to Admin'
          }
        </button>

        {/* Delete Button */}
        <button
          onClick={handleDeleteUser}
          disabled={isLoading !== null}
          style={{
            padding: '0.75rem 1rem',
            backgroundColor: '#dc2626',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: isLoading !== null ? 'not-allowed' : 'pointer',
            fontWeight: '500',
            fontSize: '0.875rem',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: '0.5rem',
            opacity: isLoading !== null ? 0.6 : 1
          }}
        >
          {isLoading === 'delete' && (
            <div style={{
              width: '16px',
              height: '16px',
              border: '2px solid #ffffff40',
              borderTop: '2px solid #ffffff',
              borderRadius: '50%',
              animation: 'spin 1s linear infinite'
            }} />
          )}
          🗑️ Delete User
        </button>

      </div>

      {/* Warning Message */}
      <div style={{
        marginTop: '1.5rem',
        padding: '0.75rem',
        backgroundColor: '#fef3c7',
        color: '#92400e',
        borderRadius: '6px',
        fontSize: '0.75rem',
        border: '1px solid #fbbf24'
      }}>
        <strong>⚠️ Warning:</strong> Quick actions bypass detailed reason entry. Use the Edit modal for actions requiring detailed audit logs.
      </div>

      {/* CSS for loading spinner */}
      <style>{`
        @keyframes spin {
          0% { transform: rotate(0deg); }
          100% { transform: rotate(360deg); }
        }
      `}</style>
    </div>
  );
};

export default UserActionsComponent;