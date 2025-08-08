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
