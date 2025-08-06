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
      { Id: '1', Name: 'Inv1', IsRunning: true, ResultCount: 5 },
      { Id: '2', Name: 'Inv2', IsRunning: false, ResultCount: 0 },
    ];
    mockedApi.get.mockResolvedValueOnce({ data: mockData });

    render(<Dashboard />);
    // initially shows loading
    expect(screen.getByText(/Loading\.\.\./i)).toBeInTheDocument();

    // wait for the loading indicator to disappear
    await waitFor(() => expect(screen.queryByText(/Loading\.\.\./i)).not.toBeInTheDocument());

    // verify table headers and first row data
    expect(screen.getByText(/Id/i)).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('Inv1')).toBeInTheDocument();
  });
});
