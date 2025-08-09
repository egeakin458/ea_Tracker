import React from 'react';

interface Props {
  isOpen: boolean;
  selectedType: string;
  investigatorName: string;
  setSelectedType: (v: string) => void;
  setInvestigatorName: (v: string) => void;
  onConfirm: () => void;
  onClose: () => void;
}

export default function CreateInvestigatorModal({ isOpen, selectedType, investigatorName, setSelectedType, setInvestigatorName, onConfirm, onClose }: Props): JSX.Element | null {
  if (!isOpen) return null;
  return (
    <div style={{
      position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
      backgroundColor: 'rgba(0, 0, 0, 0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000
    }}>
      <div style={{ backgroundColor: 'white', padding: '2rem', borderRadius: '8px', width: '400px', maxWidth: '90vw' }}>
        <h2 style={{ fontSize: '1.5rem', fontWeight: 600, marginBottom: '1.5rem' }}>Create New Investigator</h2>
        <div style={{ marginBottom: '1rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 500 }}>Type</label>
          <select value={selectedType} onChange={(e) => setSelectedType(e.target.value)} style={{ width: '100%', padding: '0.75rem', border: '1px solid #d1d5db', borderRadius: '6px', fontSize: '1rem' }}>
            <option value="">Select investigator type</option>
            <option value="invoice">Invoice Investigator</option>
            <option value="waybill">Waybill Investigator</option>
          </select>
        </div>
        <div style={{ marginBottom: '2rem' }}>
          <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: 500 }}>Name</label>
          <input type="text" value={investigatorName} onChange={(e) => setInvestigatorName(e.target.value)} placeholder="Enter investigator name" style={{ width: '100%', padding: '0.75rem', border: '1px solid #d1d5db', borderRadius: '6px', fontSize: '1rem' }} />
        </div>
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '1rem' }}>
          <button onClick={onClose} style={{ padding: '0.75rem 1.5rem', backgroundColor: '#6b7280', color: 'white', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 500 }}>Cancel</button>
          <button onClick={onConfirm} style={{ padding: '0.75rem 1.5rem', backgroundColor: '#3b82f6', color: 'white', border: 'none', borderRadius: '6px', cursor: 'pointer', fontWeight: 500 }}>Confirm</button>
        </div>
      </div>
    </div>
  );
}


