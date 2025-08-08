import React, { useEffect, useState } from "react";
import api from "./lib/axios";
import { Investigator, LogEntry, CreateResponse, ApiResponse } from "./types/api";

function Dashboard(): JSX.Element {
  const [investigators, setInvestigators] = useState<Investigator[]>([]);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [selected, setSelected] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

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
  }, []);


  const startOne = async (id: string): Promise<void> => {
    try {
      await api.post(`/api/investigations/${id}/start`);
      await loadInvestigators();
    } catch (err: any) {
      setError(err.message || `Failed to start investigator ${id}`);
    }
  };

  const stopOne = async (id: string): Promise<void> => {
    try {
      await api.post(`/api/investigations/${id}/stop`);
      await loadInvestigators();
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

  const createInvoice = async (): Promise<void> => {
    try {
      const res = await api.post<CreateResponse>(`/api/investigations/invoice`);
      await loadInvestigators();
      setSelected(res.data.id);
    } catch (err: any) {
      setError(err.message || "Failed to create invoice investigator");
    }
  };

  const createWaybill = async (): Promise<void> => {
    try {
      const res = await api.post<CreateResponse>(`/api/investigations/waybill`);
      await loadInvestigators();
      setSelected(res.data.id);
    } catch (err: any) {
      setError(err.message || "Failed to create waybill investigator");
    }
  };

  const deleteOne = async (id: string): Promise<void> => {
    if (!confirm("Are you sure you want to delete this investigator? This action cannot be undone.")) {
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
      <header style={{ marginBottom: '2rem' }}>
        <h1 style={{ fontSize: '2rem', fontWeight: 'bold', color: '#1f2937', marginBottom: '0.5rem' }}>
          Investigators
        </h1>
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
          onClick={createInvoice} 
          style={{ 
            padding: '0.75rem 1.5rem',
            marginRight: '1rem',
            backgroundColor: '#3b82f6',
            color: 'white',
            border: 'none',
            borderRadius: '6px',
            cursor: 'pointer',
            fontWeight: '500'
          }}
        >
          New Invoice Investigator
        </button>
        <button 
          onClick={createWaybill} 
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
          New Waybill Investigator
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
                  key={inv.Id || index} 
                  onClick={() => inv.Id && select(inv.Id)}
                  style={{ 
                    backgroundColor: index % 2 === 0 ? 'white' : '#f9fafb',
                    cursor: 'pointer'
                  }}
                >
                  <td style={{ padding: '1rem', borderBottom: '1px solid #e5e7eb' }}>
                    <div style={{ fontWeight: '500' }}>{inv.Name}</div>
                    <div style={{ fontSize: '0.875rem', color: '#6b7280' }}>
                      {inv.Id ? inv.Id.toString().split('-')[0] + '...' : 'N/A'}
                    </div>
                  </td>
                  <td style={{ padding: '1rem', borderBottom: '1px solid #e5e7eb' }}>
                    <span style={{
                      padding: '0.25rem 0.75rem',
                      fontSize: '0.875rem',
                      fontWeight: '500',
                      borderRadius: '9999px',
                      backgroundColor: inv.IsRunning ? '#d1fae5' : '#f3f4f6',
                      color: inv.IsRunning ? '#065f46' : '#4b5563'
                    }}>
                      {inv.IsRunning ? "Running" : "Stopped"}
                    </span>
                  </td>
                  <td style={{ padding: '1rem', textAlign: 'center', borderBottom: '1px solid #e5e7eb' }}>
                    <span style={{ fontWeight: '500' }}>{inv.ResultCount}</span>
                  </td>
                  <td style={{ padding: '1rem', borderBottom: '1px solid #e5e7eb' }}>
                    <button
                      onClick={e => { e.stopPropagation(); inv.Id && startOne(inv.Id); }}
                      style={{
                        padding: '0.5rem 1rem',
                        marginRight: '0.5rem',
                        backgroundColor: '#10b981',
                        color: 'white',
                        border: 'none',
                        borderRadius: '6px',
                        cursor: 'pointer',
                        fontSize: '0.875rem',
                        opacity: inv.Id ? 1 : 0.5
                      }}
                      disabled={!inv.Id}
                    >
                      Start
                    </button>
                    <button
                      onClick={e => { e.stopPropagation(); inv.Id && stopOne(inv.Id); }}
                      style={{
                        padding: '0.5rem 1rem',
                        marginRight: '0.5rem',
                        backgroundColor: '#ef4444',
                        color: 'white',
                        border: 'none',
                        borderRadius: '6px',
                        cursor: 'pointer',
                        fontSize: '0.875rem',
                        opacity: inv.Id ? 1 : 0.5
                      }}
                      disabled={!inv.Id}
                    >
                      Stop
                    </button>
                    <button
                      onClick={e => { e.stopPropagation(); inv.Id && deleteOne(inv.Id); }}
                      style={{
                        padding: '0.5rem 1rem',
                        backgroundColor: '#dc2626',
                        color: 'white',
                        border: 'none',
                        borderRadius: '6px',
                        cursor: 'pointer',
                        fontSize: '0.875rem',
                        opacity: inv.Id ? 1 : 0.5
                      }}
                      disabled={!inv.Id}
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
                  {new Date(l.Timestamp).toLocaleString()}
                </div>
                <div style={{ fontWeight: '500', marginBottom: '0.5rem' }}>
                  {l.Message}
                </div>
                {l.Payload && (
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
                      {l.Payload}
                    </pre>
                  </details>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export default Dashboard;
