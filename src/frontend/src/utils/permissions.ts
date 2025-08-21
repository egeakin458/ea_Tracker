/**
 * Utility functions for role-based permissions in the frontend.
 * These functions help determine what UI elements and actions should be available
 * based on the user's role.
 */

export interface User {
  id: string;
  username: string;
  roles: string[];
}

/**
 * Checks if a user has admin privileges.
 * @param user The user object from authentication
 * @returns true if the user has the Admin role
 */
export const isAdmin = (user: User | null): boolean => {
  return user?.roles?.includes('Admin') || false;
};

/**
 * Checks if a user has user-level access (User or Admin role).
 * @param user The user object from authentication
 * @returns true if the user has User or Admin role
 */
export const hasUserAccess = (user: User | null): boolean => {
  return user?.roles?.some(role => ['User', 'Admin'].includes(role)) || false;
};

/**
 * Checks if a user can perform create operations (Admin only).
 * @param user The user object from authentication
 * @returns true if the user can create resources
 */
export const canCreate = (user: User | null): boolean => {
  return isAdmin(user);
};

/**
 * Checks if a user can perform update operations (Admin only).
 * @param user The user object from authentication
 * @returns true if the user can update resources
 */
export const canUpdate = (user: User | null): boolean => {
  return isAdmin(user);
};

/**
 * Checks if a user can perform delete operations (Admin only).
 * @param user The user object from authentication
 * @returns true if the user can delete resources
 */
export const canDelete = (user: User | null): boolean => {
  return isAdmin(user);
};

/**
 * Checks if a user can view read-only content (User and Admin).
 * @param user The user object from authentication
 * @returns true if the user can view content
 */
export const canView = (user: User | null): boolean => {
  return hasUserAccess(user);
};

/**
 * Checks if a user can start investigations (User and Admin).
 * @param user The user object from authentication
 * @returns true if the user can start investigations
 */
export const canStartInvestigations = (user: User | null): boolean => {
  return hasUserAccess(user);
};

/**
 * Checks if a user can export data (User and Admin).
 * @param user The user object from authentication
 * @returns true if the user can export data
 */
export const canExport = (user: User | null): boolean => {
  return hasUserAccess(user);
};

/**
 * Gets a user object from localStorage, typically used in components
 * that don't have direct access to the App component's user state.
 * @returns User object or null if not found or invalid
 */
export const getCurrentUser = (): User | null => {
  try {
    const userInfo = localStorage.getItem('user_info');
    if (userInfo) {
      return JSON.parse(userInfo) as User;
    }
    return null;
  } catch (error) {
    console.error('Failed to parse user info from localStorage:', error);
    return null;
  }
};

/**
 * Checks if a user can manage other users (Admin only).
 * @param user The user object from authentication
 * @returns true if the user can manage users
 */
export const canManageUsers = (user: User | null): boolean => {
  return isAdmin(user);
};

/**
 * Displays user-friendly messages for restricted actions.
 * @param action The action that was restricted
 * @returns A message explaining the restriction
 */
export const getRestrictionMessage = (action: string): string => {
  return `This action (${action}) is restricted to administrators only. Please contact your administrator if you need access.`;
};