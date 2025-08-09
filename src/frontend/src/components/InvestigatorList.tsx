import React from 'react';
import { Investigator } from '../types/api';
import InvestigatorListItem from './InvestigatorListItem';

interface Props {
  investigators: Investigator[];
  loading: boolean;
  error: string | null;
  onStart: (id: string) => void;
  onDelete: (id: string) => void;
  onRowClick: (id: string) => void;
  onOpenCreate: () => void;
}

export default function InvestigatorList({ investigators, loading, error, onStart, onDelete, onRowClick, onOpenCreate }: Props): JSX.Element {
  return (
    <div style={{ flex: 1, minWidth: '500px', display: 'flex', flexDirection: 'column' }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '1rem' }}>
        <h2 style={{ fontSize: '1.5rem', fontWeight: 'bold', color: '#1f2937', margin: 0 }}>
          Investigators
        </h2>
        <button
          onClick={onOpenCreate}
          style={{
            padding: '0.5rem 1rem',
            backgroundColor: '#3b82f6',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer',
            fontSize: '0.875rem'
          }}
        >
          Create Investigator
        </button>
      </div>

      {error && (
        <div style={{
          marginBottom: '1rem',
          padding: '1rem',
          backgroundColor: '#fee2e2',
          color: '#dc2626',
          border: '1px solid #fecaca',
          borderRadius: '6px'
        }}>
          {error}
        </div>
      )}

      {loading ? (
        <div style={{ padding: '2rem', textAlign: 'center', color: '#6b7280' }}>
          Loading investigators...
        </div>
      ) : (
        <div style={{
          flex: 1,
          backgroundColor: 'white',
          border: '1px solid #e5e7eb',
          borderRadius: '8px',
          overflow: 'hidden',
          display: 'flex',
          flexDirection: 'column'
        }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ backgroundColor: '#f9fafb' }}>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: 600, width: '200px' }}>ID</th>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: 600 }}>Name</th>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: 600 }}>Status</th>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: 600 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {investigators.map((inv, index) => (
                <InvestigatorListItem
                  key={inv.id || index.toString()}
                  investigator={inv}
                  onStart={onStart}
                  onDelete={onDelete}
                  onClick={onRowClick}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}


