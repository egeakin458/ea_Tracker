import React, { useEffect, useState } from "react";
import api from "./lib/axios";
import { Investigator, LogEntry, CreateResponse, ApiResponse } from "./types/api";
import { SignalRService } from './lib/SignalRService';

function Dashboard(): JSX.Element {
  const [investigators, setInvestigators] = useState<Investigator[]>([]);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [selected, setSelected] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [selectedType, setSelectedType] = useState('');
  const [investigatorName, setInvestigatorName] = useState('');
  const [connStatus, setConnStatus] = useState<'connecting' | 'connected' | 'disconnected'>('disconnected');
  const signalR = React.useRef<SignalRService | null>(null);

  const loadInvestigators = async (): Promise<void> => {
    try {
      setError(null);
      setLoading(true);
      const res = await api.get<Investigator[]>("/api/investigations");
      setInvestigators(res.data);
    } catch (err: any) {
      setError(err.message || "Failed to load investigators");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadInvestigators();
    // Initialize SignalR connection
    const svc = new SignalRService();
    signalR.current = svc;
    const baseUrl = process.env.REACT_APP_API_BASE_URL || "http://localhost:5050";
    svc.start(baseUrl, {
      onConnectionChange: setConnStatus,
      onStarted: async () => { await loadInvestigators(); if (selected) { await select(selected); } },
      onCompleted: async () => { await loadInvestigators(); if (selected) { await select(selected); } },
      onNewResult: async (p) => {
        if (selected && p.investigatorId === selected) {
          await select(selected);
        }
        // Optimistically update result count in table without a full reload
        setInvestigators(prev => prev.map(inv =>
          inv.id === p.investigatorId ? { ...inv, resultCount: (inv.resultCount || 0) + 1 } : inv
        ));
      },
      onStatusChanged: async () => { await loadInvestigators(); }
    }).catch(() => setConnStatus('disconnected'));

    return () => { void svc.stop(); };
  }, []);


  const startOne = async (id: string): Promise<void> => {
    try {
      await api.post(`/api/investigations/${id}/start`);
      // No manual refresh; SignalR will update
    } catch (err: any) {
      setError(err.message || `Failed to start investigator ${id}`);
    }
  };

  const stopOne = async (id: string): Promise<void> => {
    try {
      await api.post(`/api/investigations/${id}/stop`);
      // No manual refresh; SignalR will update
    } catch (err: any) {
      setError(err.message || `Failed to stop investigator ${id}`);
    }
  };

  const select = async (id: string): Promise<void> => {
    try {
      setSelected(id);
      const res = await api.get<LogEntry[]>(`/api/investigations/${id}/results`);
      setLogs(res.data);
    } catch (err: any) {
      setError(err.message || `Failed to load results for investigator ${id}`);
    }
  };

  const openCreateModal = (): void => {
    setShowModal(true);
    setSelectedType('');
    setInvestigatorName('');
  };

  const closeCreateModal = (): void => {
    setShowModal(false);
    setSelectedType('');
    setInvestigatorName('');
  };

  const createInvestigator = async (): Promise<void> => {
    // Validation
    if (!selectedType) {
      setError("Please select an investigator type");
      return;
    }
    if (!investigatorName.trim()) {
      setError("Please enter an investigator name");
      return;
    }

    try {
      const endpoint = selectedType === 'invoice' ? '/api/investigations/invoice' : '/api/investigations/waybill';
      // Backend expects a JSON string body for [FromBody] string? customName
      const res = await api.post<CreateResponse>(
        endpoint,
        JSON.stringify(investigatorName.trim()),
        {
          headers: {
            'Content-Type': 'application/json'
          }
        }
      );
      await loadInvestigators();
      setSelected(res.data.id);
      closeCreateModal();
    } catch (err: any) {
      setError(err.message || `Failed to create ${selectedType} investigator`);
    }
  };

  const deleteOne = async (id: string): Promise<void> => {
    if (!window.confirm("Are you sure you want to delete this investigator? This action cannot be undone.")) {
      return;
    }
    
    try {
      await api.delete(`/api/investigations/${id}`);
      await loadInvestigators();
      
      // Clear selection if the deleted investigator was selected
      if (selected === id) {
        setSelected(null);
        setLogs([]);
      }
    } catch (err: any) {
      setError(err.message || `Failed to delete investigator ${id}`);
    }
  };

  return (
    <div style={{ padding: '2rem', maxWidth: '1200px', margin: '0 auto' }}>
      <header style={{ marginBottom: '2rem', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <h1 style={{ fontSize: '2rem', fontWeight: 'bold', color: '#1f2937', marginBottom: '0.5rem' }}>
          Investigators
        </h1>
        <div style={{ fontSize: '0.875rem', color: connStatus === 'connected' ? '#065f46' : connStatus === 'connecting' ? '#92400e' : '#991b1b' }}>
          {connStatus === 'connected' ? 'Live updates: Connected' : connStatus === 'connecting' ? 'Live updates: Connecting…' : 'Live updates: Disconnected'}
        </div>
      </header>
      
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
      
      <div style={{ marginBottom: '2rem' }}>
        <button 
          onClick={openCreateModal} 
          style={{ 
            padding: '0.75rem 1.5rem',
            backgroundColor: '#3b82f6',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer',
            fontWeight: '500'
          }}
        >
          Create Investigator
        </button>
      </div>
      {loading ? (
        <div style={{ padding: '2rem', textAlign: 'center', color: '#6b7280' }}>
          Loading investigators...
        </div>
      ) : (
        <div style={{ 
          backgroundColor: 'white', 
          border: '1px solid #e5e7eb', 
          borderRadius: '8px',
          overflow: 'hidden'
        }}>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ backgroundColor: '#f9fafb' }}>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: '600', width: '200px' }}>
                  ID
                </th>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: '600' }}>
                  Name
                </th>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: '600' }}>
                  Status
                </th>
                <th style={{ padding: '1rem', textAlign: 'center', borderBottom: '1px solid #e5e7eb', fontWeight: '600' }}>
                  Results
                </th>
                <th style={{ padding: '1rem', textAlign: 'left', borderBottom: '1px solid #e5e7eb', fontWeight: '600' }}>
                  Actions
                </th>
              </tr>
            </thead>
            <tbody>
              {investigators.map((inv, index) => (
                <tr 
                  key={inv.id || index} 
                  onClick={() => inv.id && select(inv.id)}
                  style={{ 
                    backgroundColor: index % 2 === 0 ? 'white' : '#f9fafb',
                    cursor: 'pointer'
                  }}
                >
                  <td style={{ padding: '1rem', borderBottom: '1px solid #e5e7eb' }}>
                    <div style={{ fontSize: '0.75rem', color: '#4b5563', fontFamily: 'monospace' }}>
                      {inv.id ? inv.id.toString() : 'N/A'}
                    </div>
                  </td>
                  <td style={{ padding: '1rem', borderBottom: '1px solid #e5e7eb' }}>
                    <div style={{ fontWeight: '500' }}>{inv.name}</div>
                  </td>
                  <td style={{ padding: '1rem', borderBottom: '1px solid #e5e7eb' }}>
                    <span style={{
                      padding: '0.25rem 0.75rem',
                      fontSize: '0.875rem',
                      fontWeight: '500',
                      borderRadius: '9999px',
                      backgroundColor: inv.isRunning ? '#d1fae5' : '#f3f4f6',
                      color: inv.isRunning ? '#065f46' : '#4b5563'
                    }}>
                      {inv.isRunning ? "Running" : "Stopped"}
                    </span>
                  </td>
                  <td style={{ padding: '1rem', textAlign: 'center', borderBottom: '1px solid #e5e7eb' }}>
                    <span style={{ fontWeight: '500' }}>{inv.resultCount}</span>
                  </td>
                  <td style={{ padding: '1rem', borderBottom: '1px solid #e5e7eb' }}>
                    <button
                      onClick={e => { e.stopPropagation(); inv.id && startOne(inv.id); }}
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
                      onClick={e => { e.stopPropagation(); inv.id && stopOne(inv.id); }}
                      style={{
                        padding: '0.5rem 1rem',
                        marginRight: '0.5rem',
                        backgroundColor: '#ef4444',
                        color: 'white',
                        border: 'none',
                        borderRadius: '6px',
                        cursor: 'pointer',
                        fontSize: '0.875rem',
                        opacity: inv.id ? 1 : 0.5
                      }}
                      disabled={!inv.id}
                    >
                      Stop
                    </button>
                    <button
                      onClick={e => { e.stopPropagation(); inv.id && deleteOne(inv.id); }}
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
              ))}
            </tbody>
          </table>
        </div>
      )}
      {selected && (
        <div style={{ marginTop: '2rem', backgroundColor: 'white', border: '1px solid #e5e7eb', borderRadius: '8px', padding: '1.5rem' }}>
          <h3 style={{ fontSize: '1.25rem', fontWeight: '600', marginBottom: '1rem' }}>Results</h3>
          <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
            {logs.map((l, idx) => (
              <div key={idx} style={{ 
                padding: '1rem', 
                borderBottom: idx < logs.length - 1 ? '1px solid #f3f4f6' : 'none',
                marginBottom: '0.5rem'
              }}>
                <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>
                  {new Date(l.timestamp).toLocaleString()}
                </div>
                <div style={{ fontWeight: '500', marginBottom: '0.5rem' }}>
                  {l.message}
                </div>
                {l.payload && (
                  <details style={{ marginTop: '0.5rem' }}>
                    <summary style={{ cursor: 'pointer', color: '#3b82f6', fontSize: '0.875rem' }}>
                      View Details
                    </summary>
                    <pre style={{ 
                      marginTop: '0.5rem', 
                      padding: '0.75rem', 
                      backgroundColor: '#f9fafb', 
                      fontSize: '0.75rem', 
                      borderRadius: '4px',
                      overflow: 'auto',
                      whiteSpace: 'pre-wrap'
                    }}>
                      {l.payload}
                    </pre>
                  </details>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Create Investigator Modal */}
      {showModal && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: 'rgba(0, 0, 0, 0.5)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1000
        }}>
          <div style={{
            backgroundColor: 'white',
            padding: '2rem',
            borderRadius: '8px',
            width: '400px',
            maxWidth: '90vw'
          }}>
            <h2 style={{ fontSize: '1.5rem', fontWeight: '600', marginBottom: '1.5rem' }}>
              Create New Investigator
            </h2>
            
            <div style={{ marginBottom: '1rem' }}>
              <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: '500' }}>
                Type
              </label>
              <select
                value={selectedType}
                onChange={(e) => setSelectedType(e.target.value)}
                style={{
                  width: '100%',
                  padding: '0.75rem',
                  border: '1px solid #d1d5db',
                  borderRadius: '6px',
                  fontSize: '1rem'
                }}
              >
                <option value="">Select investigator type</option>
                <option value="invoice">Invoice Investigator</option>
                <option value="waybill">Waybill Investigator</option>
              </select>
            </div>

            <div style={{ marginBottom: '2rem' }}>
              <label style={{ display: 'block', marginBottom: '0.5rem', fontWeight: '500' }}>
                Name
              </label>
              <input
                type="text"
                value={investigatorName}
                onChange={(e) => setInvestigatorName(e.target.value)}
                placeholder="Enter investigator name"
                style={{
                  width: '100%',
                  padding: '0.75rem',
                  border: '1px solid #d1d5db',
                  borderRadius: '6px',
                  fontSize: '1rem'
                }}
              />
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '1rem' }}>
              <button
                onClick={closeCreateModal}
                style={{
                  padding: '0.75rem 1.5rem',
                  backgroundColor: '#6b7280',
                  color: 'white',
                  border: 'none',
                  borderRadius: '6px',
                  cursor: 'pointer',
                  fontWeight: '500'
                }}
              >
                Cancel
              </button>
              <button
                onClick={createInvestigator}
                style={{
                  padding: '0.75rem 1.5rem',
                  backgroundColor: '#3b82f6',
                  color: 'white',
                  border: 'none',
                  borderRadius: '6px',
                  cursor: 'pointer',
                  fontWeight: '500'
                }}
              >
                Confirm
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default Dashboard;
