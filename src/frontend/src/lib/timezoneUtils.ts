/**
 * Turkey timezone utilities for consistent time display across the application.
 * All times from the backend are now in Turkey timezone, so we format them consistently.
 */

/**
 * Formats a Turkey timezone DateTime string for display.
 * Since backend now sends Turkey time, we just format it consistently.
 * @param turkeyDateString Date string in Turkey timezone from backend
 * @returns Formatted Turkey time string
 */
export const formatTurkeyTime = (turkeyDateString: string | Date): string => {
  const date = new Date(turkeyDateString);
  
  // Format as Turkey time for consistent display
  return date.toLocaleString('tr-TR', {
    year: 'numeric',
    month: '2-digit', 
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false // Use 24-hour format
  });
};

/**
 * Formats a Turkey timezone DateTime for date only display.
 * @param turkeyDateString Date string in Turkey timezone from backend
 * @returns Formatted Turkey date string
 */
export const formatTurkeyDate = (turkeyDateString: string | Date): string => {
  const date = new Date(turkeyDateString);
  
  return date.toLocaleDateString('tr-TR', {
    year: 'numeric',
    month: '2-digit', 
    day: '2-digit'
  });
};

/**
 * Formats a Turkey timezone DateTime for time only display.
 * @param turkeyDateString Date string in Turkey timezone from backend
 * @returns Formatted Turkey time string (HH:MM:SS)
 */
export const formatTurkeyTimeOnly = (turkeyDateString: string | Date): string => {
  const date = new Date(turkeyDateString);
  
  return date.toLocaleTimeString('tr-TR', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false
  });
};