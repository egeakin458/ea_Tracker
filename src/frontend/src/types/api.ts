/**
 * Investigator state returned by the API.
 */
export interface Investigator {
  Id: string;
  Name: string;
  IsRunning: boolean;
  ResultCount: number;
}

/**
 * Single log entry for an investigator.
 */
export interface LogEntry {
  InvestigatorId: string;
  Timestamp: string;
  Message: string;
  Payload?: string;
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
