import React from 'react';
import '@testing-library/jest-dom';
import { render, screen } from '@testing-library/react';
import App from '../../src/App';

describe('App component', () => {
  it('renders without crashing', () => {
    render(<App />);
    // Should render the Investigators dashboard heading
    expect(screen.getByText(/Investigators/i)).toBeInTheDocument();
  });
});
