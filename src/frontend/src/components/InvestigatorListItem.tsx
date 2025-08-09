import React from 'react';
import { Investigator } from '../types/api';

interface Props {
  investigator: Investigator;
  onStart: (id: string) => void;
  onDelete: (id: string) => void;
  onClick: (id: string) => void;
}

export default function InvestigatorListItem({ investigator: inv, onStart, onDelete, onClick }: Props): JSX.Element {
  const pillStyle = {
    padding: '0.25rem 0.75rem',
    fontSize: '0.875rem',
    fontWeight: 500,
    borderRadius: '9999px',
    backgroundColor: inv.status === 'Running' ? '#d1fae5' : inv.status === 'Queued' ? '#fef3c7' : '#f3f4f6',
    color: inv.status === 'Running' ? '#065f46' : inv.status === 'Queued' ? '#92400e' : '#4b5563'
  } as const;

  return (
    <tr
      onClick={() => inv.id && onClick(inv.id)}
      style={{ backgroundColor: '#fff', cursor: 'pointer' }}
    >
      <td style={{ padding: '1rem', borderBottom: '1px solid #e5e7eb' }}>
        <div style={{ fontSize: '0.75rem', color: '#4b5563', fontFamily: 'monospace' }}>
          {inv.id || 'N/A'}
        </div>
      </td>
      <td style={{ padding: '1rem', borderBottom: '1px solid #e5e7eb' }}>
        <div style={{ fontWeight: 500 }}>{inv.name}</div>
      </td>
      <td style={{ padding: '1rem', borderBottom: '1px solid #e5e7eb' }}>
        <span style={pillStyle}>{inv.status}</span>
      </td>
      <td style={{ padding: '1rem', borderBottom: '1px solid #e5e7eb' }}>
        <button
          onClick={e => { e.stopPropagation(); inv.id && onStart(inv.id); }}
          style={{
            padding: '0.5rem 1rem',
            marginRight: '0.5rem',
            backgroundColor: '#10b981',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer',
            fontSize: '0.875rem',
            opacity: inv.id ? 1 : 0.5
          }}
          disabled={!inv.id}
        >
          Start
        </button>
        <button
          onClick={e => { e.stopPropagation(); inv.id && onDelete(inv.id); }}
          style={{
            padding: '0.5rem 1rem',
            backgroundColor: '#dc2626',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer',
            fontSize: '0.875rem',
            opacity: inv.id ? 1 : 0.5
          }}
          disabled={!inv.id}
        >
          Del
        </button>
      </td>
    </tr>
  );
}


