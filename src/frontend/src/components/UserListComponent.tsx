import React, { useState, useEffect } from 'react';
import { UserSummary, GetUsersRequest, GetUsersResponse, PaginationInfo } from '../types/api';
import { AdminService } from '../lib/adminService';

interface UserListComponentProps {
  onUserSelect: (user: UserSummary) => void;
  onUserEdit: (user: UserSummary) => void;
  onUserDelete: (user: UserSummary) => void;
  onError: (error: string) => void;
  refreshTrigger: number; // Used to trigger refresh from parent
}

const UserListComponent: React.FC<UserListComponentProps> = ({
  onUserSelect,
  onUserEdit,
  onUserDelete,
  onError,
  refreshTrigger
}) => {
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [pagination, setPagination] = useState<PaginationInfo>({
    totalItems: 0,
    totalPages: 0,
    currentPage: 1,
    pageSize: 20,
    hasNextPage: false,
    hasPreviousPage: false
  });
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [selectedUserId, setSelectedUserId] = useState<number | null>(null);

  const loadUsers = async (page = 1, search = searchTerm, role = roleFilter): Promise<void> => {
    try {
      setLoading(true);
      onError(''); // Clear any existing errors

      const request: GetUsersRequest = {
        page,
        pageSize: 20,
        ...(search && { search }),
        ...(role && { roleFilter: role })
      };

      const response: GetUsersResponse = await AdminService.getUsers(request);
      setUsers(response.users);
      setPagination(response.pagination);
    } catch (error: any) {
      onError(error.message || 'Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  // Load users on component mount and when refreshTrigger changes
  useEffect(() => {
    void loadUsers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [refreshTrigger]);

  // Handle search with debouncing
  useEffect(() => {
    const debounceTimer = setTimeout(() => {
      if (searchTerm !== '' || roleFilter !== '') {
        void loadUsers(1, searchTerm, roleFilter);
      } else {
        void loadUsers(1);
      }
    }, 300);

    return () => clearTimeout(debounceTimer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchTerm, roleFilter]);

  const handleUserClick = (user: UserSummary): void => {
    setSelectedUserId(user.id);
    onUserSelect(user);
  };

  const handlePageChange = (newPage: number): void => {
    void loadUsers(newPage, searchTerm, roleFilter);
  };

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>): void => {
    setSearchTerm(e.target.value);
  };

  const handleRoleFilterChange = (e: React.ChangeEvent<HTMLSelectElement>): void => {
    setRoleFilter(e.target.value);
  };

  const formatDate = (dateString: string | undefined): string => {
    if (!dateString) return 'Never';
    return new Date(dateString).toLocaleDateString();
  };

  const formatRole = (roles: string[]): string => {
    return roles.length > 0 ? roles[0] : 'User';
  };

  const getUserStatusColor = (user: UserSummary): string => {
    if (user.isLocked) return '#dc2626'; // Red for locked
    if (!user.isActive) return '#f59e0b'; // Orange for inactive
    return '#059669'; // Green for active
  };

  const getUserStatusText = (user: UserSummary): string => {
    if (user.isLocked) return 'Locked';
    if (!user.isActive) return 'Inactive';
    return 'Active';
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      {/* Search and Filter Controls */}
      <div style={{ marginBottom: '1rem', display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
        <div style={{ flex: '1', minWidth: '200px' }}>
          <input
            type="text"
            value={searchTerm}
            onChange={handleSearchChange}
            placeholder="Search by username or email..."
            style={{
              width: '100%',
              padding: '0.5rem',
              border: '1px solid #d1d5db',
              borderRadius: '6px',
              fontSize: '0.875rem'
            }}
          />
        </div>
        <div style={{ minWidth: '120px' }}>
          <select
            value={roleFilter}
            onChange={handleRoleFilterChange}
            style={{
              width: '100%',
              padding: '0.5rem',
              border: '1px solid #d1d5db',
              borderRadius: '6px',
              fontSize: '0.875rem'
            }}
          >
            <option value="">All Roles</option>
            <option value="User">User</option>
            <option value="Admin">Admin</option>
          </select>
        </div>
      </div>

      {/* Loading State */}
      {loading ? (
        <div style={{ 
          flex: 1,
          display: 'flex', 
          alignItems: 'center', 
          justifyContent: 'center',
          backgroundColor: 'white',
          border: '1px solid #e5e7eb',
          borderRadius: '8px',
          padding: '2rem',
          color: '#6b7280'
        }}>
          <div style={{ textAlign: 'center' }}>
            <div style={{ 
              width: '32px', 
              height: '32px', 
              border: '3px solid #e5e7eb',
              borderTop: '3px solid #3b82f6',
              borderRadius: '50%',
              animation: 'spin 1s linear infinite',
              margin: '0 auto 1rem'
            }} />
            Loading users...
          </div>
        </div>
      ) : (
        <>
          {/* Users Table */}
          <div style={{ 
            flex: 1,
            backgroundColor: 'white', 
            border: '1px solid #e5e7eb', 
            borderRadius: '8px',
            overflow: 'hidden',
            display: 'flex',
            flexDirection: 'column'
          }}>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ backgroundColor: '#f9fafb' }}>
                  <th style={{ padding: '0.75rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: '600', fontSize: '0.875rem' }}>
                    User
                  </th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: '600', fontSize: '0.875rem' }}>
                    Role
                  </th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: '600', fontSize: '0.875rem' }}>
                    Status
                  </th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: '600', fontSize: '0.875rem' }}>
                    Last Login
                  </th>
                  <th style={{ padding: '0.75rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: '600', fontSize: '0.875rem' }}>
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody>
                {users.map((user, index) => (
                  <tr 
                    key={user.id}
                    onClick={() => handleUserClick(user)}
                    style={{ 
                      backgroundColor: selectedUserId === user.id ? '#eff6ff' : (index % 2 === 0 ? 'white' : '#f9fafb'),
                      cursor: 'pointer',
                      borderLeft: selectedUserId === user.id ? '3px solid #3b82f6' : '3px solid transparent'
                    }}
                  >
                    <td style={{ padding: '0.75rem', borderBottom: '1px solid #e5e7eb' }}>
                      <div>
                        <div style={{ fontWeight: '500', fontSize: '0.875rem' }}>
                          {user.displayName || user.username}
                        </div>
                        <div style={{ fontSize: '0.75rem', color: '#6b7280' }}>
                          {user.email}
                        </div>
                        {user.displayName && user.displayName !== user.username && (
                          <div style={{ fontSize: '0.75rem', color: '#9ca3af' }}>
                            @{user.username}
                          </div>
                        )}
                      </div>
                    </td>
                    <td style={{ padding: '0.75rem', borderBottom: '1px solid #e5e7eb' }}>
                      <span style={{
                        padding: '0.25rem 0.75rem',
                        fontSize: '0.75rem',
                        fontWeight: '500',
                        borderRadius: '9999px',
                        backgroundColor: formatRole(user.roles) === 'Admin' ? '#ddd6fe' : '#e0f2fe',
                        color: formatRole(user.roles) === 'Admin' ? '#7c3aed' : '#0369a1'
                      }}>
                        {formatRole(user.roles)}
                      </span>
                    </td>
                    <td style={{ padding: '0.75rem', borderBottom: '1px solid #e5e7eb' }}>
                      <span style={{
                        padding: '0.25rem 0.75rem',
                        fontSize: '0.75rem',
                        fontWeight: '500',
                        borderRadius: '9999px',
                        backgroundColor: user.isLocked ? '#fee2e2' : (user.isActive ? '#d1fae5' : '#fed7aa'),
                        color: getUserStatusColor(user)
                      }}>
                        {getUserStatusText(user)}
                      </span>
                      {user.failedLoginAttempts > 0 && (
                        <div style={{ fontSize: '0.75rem', color: '#dc2626', marginTop: '0.25rem' }}>
                          {user.failedLoginAttempts} failed attempt{user.failedLoginAttempts !== 1 ? 's' : ''}
                        </div>
                      )}
                    </td>
                    <td style={{ padding: '0.75rem', borderBottom: '1px solid #e5e7eb', fontSize: '0.875rem' }}>
                      {formatDate(user.lastLoginAt)}
                    </td>
                    <td style={{ padding: '0.75rem', borderBottom: '1px solid #e5e7eb' }}>
                      <div style={{ display: 'flex', gap: '0.5rem' }}>
                        <button
                          onClick={(e) => { e.stopPropagation(); onUserEdit(user); }}
                          style={{
                            padding: '0.25rem 0.75rem',
                            backgroundColor: '#3b82f6',
                            color: 'white',
                            border: 'none',
                            borderRadius: '4px',
                            cursor: 'pointer',
                            fontSize: '0.75rem',
                            fontWeight: '500'
                          }}
                        >
                          Edit
                        </button>
                        <button
                          onClick={(e) => { e.stopPropagation(); onUserDelete(user); }}
                          style={{
                            padding: '0.25rem 0.75rem',
                            backgroundColor: '#dc2626',
                            color: 'white',
                            border: 'none',
                            borderRadius: '4px',
                            cursor: 'pointer',
                            fontSize: '0.75rem',
                            fontWeight: '500'
                          }}
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {users.length === 0 && !loading && (
              <div style={{ 
                padding: '2rem', 
                textAlign: 'center', 
                color: '#6b7280',
                flex: 1,
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center'
              }}>
                <div style={{ fontSize: '2rem', marginBottom: '1rem' }}>👤</div>
                <div style={{ fontSize: '1rem', fontWeight: '500' }}>No users found</div>
                <div style={{ fontSize: '0.875rem', marginTop: '0.5rem' }}>
                  {searchTerm || roleFilter ? 'Try adjusting your search or filters' : 'No users have been created yet'}
                </div>
              </div>
            )}
          </div>

          {/* Pagination */}
          {pagination.totalPages > 1 && (
            <div style={{ 
              marginTop: '1rem', 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'space-between',
              fontSize: '0.875rem',
              color: '#6b7280'
            }}>
              <div>
                Showing {((pagination.currentPage - 1) * pagination.pageSize) + 1} to{' '}
                {Math.min(pagination.currentPage * pagination.pageSize, pagination.totalItems)} of{' '}
                {pagination.totalItems} users
              </div>
              <div style={{ display: 'flex', gap: '0.5rem' }}>
                <button
                  onClick={() => handlePageChange(pagination.currentPage - 1)}
                  disabled={!pagination.hasPreviousPage}
                  style={{
                    padding: '0.5rem 1rem',
                    backgroundColor: pagination.hasPreviousPage ? '#f3f4f6' : '#e5e7eb',
                    color: pagination.hasPreviousPage ? '#374151' : '#9ca3af',
                    border: '1px solid #d1d5db',
                    borderRadius: '6px',
                    cursor: pagination.hasPreviousPage ? 'pointer' : 'not-allowed',
                    fontSize: '0.875rem'
                  }}
                >
                  Previous
                </button>
                <span style={{ 
                  padding: '0.5rem 1rem',
                  backgroundColor: '#3b82f6',
                  color: 'white',
                  borderRadius: '6px',
                  fontSize: '0.875rem',
                  fontWeight: '500'
                }}>
                  {pagination.currentPage} of {pagination.totalPages}
                </span>
                <button
                  onClick={() => handlePageChange(pagination.currentPage + 1)}
                  disabled={!pagination.hasNextPage}
                  style={{
                    padding: '0.5rem 1rem',
                    backgroundColor: pagination.hasNextPage ? '#f3f4f6' : '#e5e7eb',
                    color: pagination.hasNextPage ? '#374151' : '#9ca3af',
                    border: '1px solid #d1d5db',
                    borderRadius: '6px',
                    cursor: pagination.hasNextPage ? 'pointer' : 'not-allowed',
                    fontSize: '0.875rem'
                  }}
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </>
      )}

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

export default UserListComponent;