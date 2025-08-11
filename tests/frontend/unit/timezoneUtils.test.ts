import { formatTurkeyTime, formatTurkeyDate, formatTurkeyTimeOnly } from '../../../src/frontend/src/lib/timezoneUtils';

describe('timezoneUtils', () => {
  describe('formatTurkeyTime', () => {
    it('formats Turkey time correctly', () => {
      // Since backend now sends Turkey time, we format it as received
      const turkeyTimeString = '2025-08-11T04:08:42';
      const formatted = formatTurkeyTime(turkeyTimeString);
      
      // Should format as Turkish locale
      expect(formatted).toMatch(/11\.08\.2025.*04:08:42/);
    });

    it('handles Date objects', () => {
      const turkeyDate = new Date('2025-08-11T04:08:42');
      const formatted = formatTurkeyTime(turkeyDate);
      
      expect(formatted).toMatch(/11\.08\.2025.*04:08:42/);
    });
  });

  describe('formatTurkeyDate', () => {
    it('formats Turkey date correctly', () => {
      const turkeyTimeString = '2025-08-11T04:08:42';
      const formatted = formatTurkeyDate(turkeyTimeString);
      
      expect(formatted).toBe('11.08.2025');
    });
  });

  describe('formatTurkeyTimeOnly', () => {
    it('formats Turkey time only correctly', () => {
      const turkeyTimeString = '2025-08-11T04:08:42';
      const formatted = formatTurkeyTimeOnly(turkeyTimeString);
      
      expect(formatted).toBe('04:08:42');
    });
  });

  describe('consistency', () => {
    it('all formatters handle the same input consistently', () => {
      const turkeyTimeString = '2025-08-11T04:08:42';
      
      const fullTime = formatTurkeyTime(turkeyTimeString);
      const dateOnly = formatTurkeyDate(turkeyTimeString);
      const timeOnly = formatTurkeyTimeOnly(turkeyTimeString);
      
      // All should be defined and non-empty
      expect(fullTime).toBeDefined();
      expect(dateOnly).toBeDefined();
      expect(timeOnly).toBeDefined();
      expect(fullTime.length).toBeGreaterThan(0);
      expect(dateOnly.length).toBeGreaterThan(0);
      expect(timeOnly.length).toBeGreaterThan(0);
    });
  });
});