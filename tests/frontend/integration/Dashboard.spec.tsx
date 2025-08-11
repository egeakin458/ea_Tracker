import React from 'react';
import '@testing-library/jest-dom';
import { render, screen, waitFor } from '@testing-library/react';
import Dashboard from '../../../src/frontend/src/Dashboard';
import api from '../../../src/frontend/src/lib/axios';

jest.mock('../../../src/frontend/src/lib/axios');
const mockedApi = api as jest.Mocked<typeof api>;

describe('Dashboard integration', () => {
  it('shows a table of investigators after loading', async () => {
    const mockData = [
      { id: '1', name: 'Inv1', isRunning: true, resultCount: 5 },
      { id: '2', name: 'Inv2', isRunning: false, resultCount: 0 },
    ];
    
    // Mock multiple API calls that Dashboard makes
    mockedApi.get.mockImplementation((url) => {
      if (url === '/api/investigations') {
        return Promise.resolve({ data: mockData });
      }
      if (url === '/api/CompletedInvestigations') {
        return Promise.resolve({ data: [] });
      }
      return Promise.reject(new Error(`Unexpected API call to ${url}`));
    });

    render(<Dashboard />);
    // initially shows loading
    expect(screen.getByText(/Loading investigators\.\.\./i)).toBeInTheDocument();

    // wait for the loading indicator to disappear
    await waitFor(() => expect(screen.queryByText(/Loading investigators\.\.\./i)).not.toBeInTheDocument());

    // verify table headers and first row data (use more specific selectors)
    expect(screen.getByRole('columnheader', { name: /ID/i })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: /Name/i })).toBeInTheDocument();
    expect(screen.getByText('Inv1')).toBeInTheDocument();
  });
});
