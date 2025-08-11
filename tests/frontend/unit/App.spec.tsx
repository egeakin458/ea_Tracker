import React from 'react';
import '@testing-library/jest-dom';
import { render, screen } from '@testing-library/react';
import App from '../../../src/frontend/src/App';

describe('App component', () => {
  it('renders without crashing', () => {
    render(<App />);
    // Should render the Investigators dashboard heading (use heading role to be specific)
    expect(screen.getByRole('heading', { name: /Investigators/i })).toBeInTheDocument();
  });
});
