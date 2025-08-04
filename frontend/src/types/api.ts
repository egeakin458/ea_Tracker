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
  timestamp: string;
  message: string;
}
