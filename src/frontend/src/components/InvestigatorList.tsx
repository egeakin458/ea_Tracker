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
    <div className="flex flex-col" style={{ flex: 1, minWidth: '500px' }}>
      <div className="flex items-center justify-between mb-md">
        <h2 className="text-2xl font-bold text-primary m-0">Investigators</h2>
        <button onClick={onOpenCreate} className="p-sm" style={{ backgroundColor: 'var(--primary-color)', color: 'white', border: 'none', borderRadius: '6px', cursor: 'pointer', fontSize: '0.875rem' }}>
          Create Investigator
        </button>
      </div>

      {error && (
        <div className="p-md mb-md" style={{ backgroundColor: 'var(--danger-light)', color: 'var(--danger-color)', border: '1px solid #fecaca', borderRadius: '6px' }}>{error}</div>
      )}

      {loading ? (
        <div className="p-xl text-center text-secondary">Loading investigators...</div>
      ) : (
        <div className="flex flex-col" style={{ flex: 1, backgroundColor: 'white', border: '1px solid var(--card-border)', borderRadius: '8px', overflow: 'hidden' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ backgroundColor: 'var(--table-stripe)' }}>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid var(--border-color)', fontWeight: 600, width: '200px' }}>ID</th>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid var(--border-color)', fontWeight: 600 }}>Name</th>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid var(--border-color)', fontWeight: 600 }}>Status</th>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid var(--border-color)', fontWeight: 600 }}>Actions</th>
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


