import api from '../../../src/frontend/src/lib/axios';

describe('axios instance', () => {
  it('uses default baseURL when env var is not set', () => {
    expect(api.defaults.baseURL).toBe('http://localhost:5050');
  });
});
