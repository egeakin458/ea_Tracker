import { secureApiCall } from './axios';
import { 
  AdminCreateUserRequest, 
  AdminCreateUserResponse,
  GetUsersRequest,
  GetUsersResponse,
  UserDetails,
  UserStats,
  ToggleUserStatusRequest,
  UpdateUserRoleRequest,
  AdminOperationResponse
} from '../types/api';

/**
 * Admin service for user management operations.
 * Provides secure API integration with proper error handling and rate limiting awareness.
 */
export class AdminService {
  /**
   * Creates a new user with admin privileges.
   * Rate limited to 3 requests per minute.
   * @param userData User creation data
   * @returns Promise resolving to user creation response
   * @throws Error on validation failures, authentication issues, or rate limiting
   */
  static async createUser(userData: AdminCreateUserRequest): Promise<AdminCreateUserResponse> {
    try {
      const response = await secureApiCall.post<AdminCreateUserResponse>(
        '/api/auth/admin/create-user',
        userData
      );
      
      return response.data;
    } catch (error: any) {
      // Enhanced error handling for common scenarios
      if (error.response?.status === 429) {
        throw new Error('Rate limit exceeded. Please wait before creating another user (maximum 3 users per minute).');
      }
      
      if (error.response?.status === 400) {
        const errorMessage = error.response.data?.message || 'Invalid user data provided';
        throw new Error(errorMessage);
      }
      
      if (error.response?.status === 409) {
        throw new Error('Username or email already exists. Please choose different credentials.');
      }
      
      if (error.response?.status === 403) {
        throw new Error('Access denied. Admin privileges required to create users.');
      }
      
      // Generic error fallback
      throw new Error(error.response?.data?.message || error.message || 'Failed to create user');
    }
  }

  /**
   * Gets a paginated list of users with optional filtering.
   * @param params Query parameters for pagination and filtering
   * @returns Promise resolving to paginated users list
   * @throws Error on authentication issues or server errors
   */
  static async getUsers(params: GetUsersRequest): Promise<GetUsersResponse> {
    try {
      const queryParams = new URLSearchParams({
        page: params.page.toString(),
        pageSize: params.pageSize.toString(),
        ...(params.search && { search: params.search }),
        ...(params.roleFilter && { roleFilter: params.roleFilter })
      });

      const response = await secureApiCall.get<GetUsersResponse>(
        `/api/auth/admin/users?${queryParams}`
      );
      
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 403) {
        throw new Error('Access denied. Admin privileges required to view users.');
      }
      
      throw new Error(error.response?.data?.message || error.message || 'Failed to retrieve users');
    }
  }

  /**
   * Gets detailed information about a specific user.
   * @param userId The user ID to get details for
   * @returns Promise resolving to detailed user information
   * @throws Error on authentication issues, not found, or server errors
   */
  static async getUserDetails(userId: number): Promise<UserDetails> {
    try {
      const response = await secureApiCall.get<AdminOperationResponse>(
        `/api/auth/admin/users/${userId}`
      );
      
      if (!response.data.success) {
        throw new Error(response.data.message || 'Failed to retrieve user details');
      }

      return response.data.data as UserDetails;
    } catch (error: any) {
      if (error.response?.status === 404) {
        throw new Error('User not found');
      }
      
      if (error.response?.status === 403) {
        throw new Error('Access denied. Admin privileges required to view user details.');
      }
      
      throw new Error(error.response?.data?.message || error.message || 'Failed to retrieve user details');
    }
  }

  /**
   * Gets system-wide user statistics.
   * @returns Promise resolving to user statistics
   * @throws Error on authentication issues or server errors
   */
  static async getUserStats(): Promise<UserStats> {
    try {
      const response = await secureApiCall.get<AdminOperationResponse>(
        '/api/auth/admin/users/stats'
      );
      
      if (!response.data.success) {
        throw new Error(response.data.message || 'Failed to retrieve user statistics');
      }

      return response.data.data as UserStats;
    } catch (error: any) {
      if (error.response?.status === 403) {
        throw new Error('Access denied. Admin privileges required to view user statistics.');
      }
      
      throw new Error(error.response?.data?.message || error.message || 'Failed to retrieve user statistics');
    }
  }

  /**
   * Toggles the active status of a user (activate/deactivate).
   * @param userId The user ID to update
   * @param request Status update request
   * @returns Promise resolving to operation result
   * @throws Error on validation failures, not found, or authentication issues
   */
  static async toggleUserStatus(userId: number, request: ToggleUserStatusRequest): Promise<string> {
    try {
      const response = await secureApiCall.put<AdminOperationResponse>(
        `/api/auth/admin/users/${userId}/toggle-status`,
        request
      );
      
      if (!response.data.success) {
        throw new Error(response.data.message || 'Failed to update user status');
      }

      return response.data.message;
    } catch (error: any) {
      if (error.response?.status === 404) {
        throw new Error('User not found or status update failed');
      }
      
      if (error.response?.status === 403) {
        throw new Error('Access denied. Admin privileges required to modify user status.');
      }
      
      if (error.response?.status === 400) {
        const errorMessage = error.response.data?.message || 'Invalid status update request';
        throw new Error(errorMessage);
      }
      
      throw new Error(error.response?.data?.message || error.message || 'Failed to update user status');
    }
  }

  /**
   * Updates the role assignment for a user.
   * @param userId The user ID to update
   * @param request Role update request
   * @returns Promise resolving to operation result
   * @throws Error on validation failures, not found, or authentication issues
   */
  static async updateUserRole(userId: number, request: UpdateUserRoleRequest): Promise<string> {
    try {
      const response = await secureApiCall.put<AdminOperationResponse>(
        `/api/auth/admin/users/${userId}/update-role`,
        request
      );
      
      if (!response.data.success) {
        throw new Error(response.data.message || 'Failed to update user role');
      }

      return response.data.message;
    } catch (error: any) {
      if (error.response?.status === 404) {
        throw new Error('User not found or role update failed');
      }
      
      if (error.response?.status === 403) {
        throw new Error('Access denied. Admin privileges required to modify user roles.');
      }
      
      if (error.response?.status === 400) {
        const errorMessage = error.response.data?.message || 'Invalid role update request';
        throw new Error(errorMessage);
      }
      
      throw new Error(error.response?.data?.message || error.message || 'Failed to update user role');
    }
  }

  /**
   * Deletes a user from the system (soft delete).
   * @param userId The user ID to delete
   * @returns Promise resolving to operation result
   * @throws Error on not found or authentication issues
   */
  static async deleteUser(userId: number): Promise<string> {
    try {
      const response = await secureApiCall.delete<AdminOperationResponse>(
        `/api/auth/admin/users/${userId}`
      );
      
      if (!response.data.success) {
        throw new Error(response.data.message || 'Failed to delete user');
      }

      return response.data.message;
    } catch (error: any) {
      if (error.response?.status === 404) {
        throw new Error('User not found or deletion failed');
      }
      
      if (error.response?.status === 403) {
        throw new Error('Access denied. Admin privileges required to delete users.');
      }
      
      throw new Error(error.response?.data?.message || error.message || 'Failed to delete user');
    }
  }
}