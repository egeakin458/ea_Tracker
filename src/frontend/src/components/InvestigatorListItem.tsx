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
    backgroundColor: inv.status === 'Running' ? 'var(--success-light)' : inv.status === 'Queued' ? 'var(--warning-light)' : 'var(--neutral-light)',
    color: inv.status === 'Running' ? '#065f46' : inv.status === 'Queued' ? '#92400e' : 'var(--text-secondary)'
  } as const;

  return (
    <tr onClick={() => inv.id && onClick(inv.id)} className="" style={{ backgroundColor: '#fff', cursor: 'pointer' }}>
      <td className="p-md" style={{ borderBottom: '1px solid var(--border-color)' }}>
        <div className="text-xs" style={{ color: 'var(--text-secondary)', fontFamily: 'monospace' }}>
          {inv.id || 'N/A'}
        </div>
      </td>
      <td className="p-md" style={{ borderBottom: '1px solid var(--border-color)' }}>
        <div className="font-medium">{inv.name}</div>
      </td>
      <td className="p-md" style={{ borderBottom: '1px solid var(--border-color)' }}>
        <span style={pillStyle}>{inv.status}</span>
      </td>
      <td className="p-md" style={{ borderBottom: '1px solid var(--border-color)' }}>
        <button
          onClick={e => { e.stopPropagation(); inv.id && onStart(inv.id); }}
          className="p-sm"
          style={{ marginRight: '0.5rem', backgroundColor: 'var(--success-color)', color: 'white', border: 'none', borderRadius: '6px', cursor: 'pointer', fontSize: '0.875rem', opacity: inv.id ? 1 : 0.5 }}
          disabled={!inv.id}
        >
          Start
        </button>
        <button
          onClick={e => { e.stopPropagation(); inv.id && onDelete(inv.id); }}
          className="p-sm"
          style={{ backgroundColor: 'var(--danger-color)', color: 'white', border: 'none', borderRadius: '6px', cursor: 'pointer', fontSize: '0.875rem', opacity: inv.id ? 1 : 0.5 }}
          disabled={!inv.id}
        >
          Del
        </button>
      </td>
    </tr>
  );
}


