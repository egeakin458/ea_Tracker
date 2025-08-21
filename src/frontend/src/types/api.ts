/**
 * Investigator state returned by the API.
 */
export interface Investigator {
  id: string;
  name: string;
  isRunning: boolean;
  resultCount: number;
}

/**
 * Single log entry for an investigator.
 */
export interface LogEntry {
  investigatorId: string;
  timestamp: string;
  message: string;
  payload?: string;
}

/**
 * API response wrapper for creation operations.
 */
export interface CreateResponse {
  id: string;
  message: string;
}

/**
 * API response wrapper for operations with message.
 */
export interface ApiResponse {
  message: string;
}

/**
 * Completed investigation data for the results panel.
 */
export interface CompletedInvestigation {
  executionId: number;
  investigatorId: string;
  investigatorName: string;
  startedAt: string;
  completedAt: string;
  resultCount: number;
  anomalyCount: number;
  isHighlighted?: boolean;
}

/**
 * Detailed investigation information for modal display.
 */
export interface InvestigationDetail {
  summary: CompletedInvestigation;
  detailedResults: LogEntry[];
}

/**
 * Admin user creation request - matches backend CreateUserAsAdminRequest DTO
 */
export interface AdminCreateUserRequest {
  username: string;
  email: string;
  password: string;
  displayName?: string;
  role: 'User' | 'Admin';
}

/**
 * User information returned from admin endpoints - avoids conflict with permissions.ts User interface
 */
export interface AdminUserInfo {
  id: string;
  username: string;
  roles: string[];
}

/**
 * Response from admin user creation endpoint
 */
export interface AdminCreateUserResponse {
  message: string;
  user: AdminUserInfo;
}

/**
 * User summary for list display - matches backend UserSummaryDto
 */
export interface UserSummary {
  id: number;
  username: string;
  email: string;
  displayName?: string;
  isActive: boolean;
  roles: string[];
  createdAt: string;
  lastLoginAt?: string;
  failedLoginAttempts: number;
  isLocked: boolean;
}

/**
 * Pagination information for API responses
 */
export interface PaginationInfo {
  totalItems: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/**
 * Request for getting users list with pagination and filtering
 */
export interface GetUsersRequest {
  page: number;
  pageSize: number;
  search?: string;
  roleFilter?: string;
}

/**
 * Response for users list endpoint
 */
export interface GetUsersResponse {
  users: UserSummary[];
  pagination: PaginationInfo;
}

/**
 * Request to toggle user status
 */
export interface ToggleUserStatusRequest {
  isActive: boolean;
  reason?: string;
}

/**
 * Request to update user role
 */
export interface UpdateUserRoleRequest {
  newRole: string;
  reason?: string;
}

/**
 * Standard admin operation response
 */
export interface AdminOperationResponse {
  success: boolean;
  message: string;
  data?: any;
}

/**
 * User activity entry for audit trail
 */
export interface UserActivity {
  timestamp: string;
  action: string;
  details: string;
  ipAddress?: string;
}

/**
 * Detailed user information for admin view
 */
export interface UserDetails {
  id: number;
  username: string;
  email: string;
  displayName?: string;
  isActive: boolean;
  roles: string[];
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;
  failedLoginAttempts: number;
  lockedOutAt?: string;
  isLocked: boolean;
  recentActivity: UserActivity[];
}

/**
 * User statistics for dashboard
 */
export interface UserStats {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  lockedUsers: number;
  usersByRole: Record<string, number>;
  newUsersThisMonth: number;
  loginAttemptsToday: number;
  failedLoginsToday: number;
}
