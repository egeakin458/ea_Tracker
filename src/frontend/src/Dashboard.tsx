import React, { useEffect, useState } from "react";
import api from "./lib/axios";
import { Investigator, LogEntry, CreateResponse, ApiResponse } from "./types/api";
import { SignalRService } from './lib/SignalRService';
import InvestigationResults from './components/InvestigationResults';
import InvestigationDetailModal from './InvestigationDetailModal';
import Header from './components/Header';
import InvestigatorList from './components/InvestigatorList';
import CreateInvestigatorModal from './components/CreateInvestigatorModal';

function Dashboard(): JSX.Element {
  const [investigators, setInvestigators] = useState<Investigator[]>([]);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [selected, setSelectedInternal] = useState<string | null>(null);
  
  // Custom setter that updates both state and ref
  const setSelected = (value: string | null): void => {
    setSelectedInternal(value);
    selectedRef.current = value;
  };
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [selectedType, setSelectedType] = useState('');
  const [investigatorName, setInvestigatorName] = useState('');
  const [connStatus, setConnStatus] = useState<'connecting' | 'connected' | 'disconnected'>('disconnected');
  const [highlightedInvestigatorId, setHighlightedInvestigatorId] = useState<string | undefined>(undefined);
  const [detailModalExecutionId, setDetailModalExecutionId] = useState<number | null>(null);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [investigationResultsKey, setInvestigationResultsKey] = useState(0); // Force refresh key
  const signalR = React.useRef<SignalRService | null>(null);
  const selectedRef = React.useRef<string | null>(null); // Keep track of selected in ref for SignalR handlers

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
    const baseUrl = process.env.REACT_APP_API_BASE_URL as string;
    svc.start(baseUrl, {
      onConnectionChange: setConnStatus,
      onStarted: async (p) => { 
        console.log('Dashboard: InvestigationStarted received', p);
        await loadInvestigators(); 
        if (selectedRef.current) { 
          console.log('Dashboard: Refreshing selected investigator', selectedRef.current);
          await select(selectedRef.current); 
        } 
      },
      onCompleted: async (p) => { 
        console.log('Dashboard: InvestigationCompleted received', p);
        await loadInvestigators(); 
        if (selectedRef.current) { 
          console.log('Dashboard: Refreshing selected investigator results', selectedRef.current);
          await select(selectedRef.current); 
        }
        // Force InvestigationResults to refresh
        console.log('Dashboard: Forcing InvestigationResults refresh');
        setInvestigationResultsKey(prev => prev + 1);
      },
      onNewResult: async (p) => {
        console.log('Dashboard: NewResultAdded received', p);
        // Optimistically update result count in table without a full reload
        setInvestigators(prev => prev.map(inv =>
          inv.id === p.investigatorId ? { ...inv, resultCount: (inv.resultCount || 0) + 1 } : inv
        ));
        // If this is the selected investigator, refresh its logs
        if (p.investigatorId === selectedRef.current) {
          console.log('Dashboard: Refreshing logs for selected investigator', selectedRef.current);
          const res = await api.get<LogEntry[]>(`/api/investigations/${p.investigatorId}/results`);
          setLogs(res.data);
        }
      },
      onStatusChanged: async () => { await loadInvestigators(); }
    }).catch(() => setConnStatus('disconnected'));

    return () => { void svc.stop(); };
  }, []);


  const startOne = async (id: string): Promise<void> => {
    try {
      const res = await api.post(`/api/investigations/${id}/start`);
      const jobId = res.data?.jobId as string | undefined;
      // Optimistically set status to Queued for immediate feedback
      setInvestigators(prev => prev.map(inv =>
        inv.id === id ? { ...inv, status: 'Queued', jobId } : inv
      ));
    } catch (err: any) {
      setError(err.message || `Failed to start investigator ${id}`);
    }
  };

  // Removed stopOne; investigations are one-shot operations now

  const select = async (id: string): Promise<void> => {
    try {
      setSelected(id);
      const res = await api.get<LogEntry[]>(`/api/investigations/${id}/results`);
      setLogs(res.data);
    } catch (err: any) {
      setError(err.message || `Failed to load results for investigator ${id}`);
    }
  };


  const handleInvestigatorClick = (id: string): void => {
    // Select for left panel results (existing functionality)
    void select(id);
    // Highlight in right panel (new functionality)
    setHighlightedInvestigatorId(id);
  };

  const handleResultClick = (executionId: number): void => {
    setDetailModalExecutionId(executionId);
    setIsDetailModalOpen(true);
  };

  const closeDetailModal = (): void => {
    setIsDetailModalOpen(false);
    setDetailModalExecutionId(null);
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
      const endpoint = `/api/investigations/create/${selectedType}`;
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
    <div style={{ padding: '2rem', height: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Header connStatus={connStatus} />

      {/* Two-panel layout */}
      <div style={{ flex: 1, display: 'flex', gap: '2rem', minHeight: 0 }}>
        {/* LEFT COLUMN */}
        <div style={{ flex: '1', minWidth: '500px', display: 'flex', flexDirection: 'column' }}>
          <InvestigatorList
            investigators={investigators}
            loading={loading}
            error={error}
            onStart={startOne}
            onDelete={deleteOne}
            onRowClick={handleInvestigatorClick}
            onOpenCreate={openCreateModal}
          />
          {selected && (
            <div style={{
              marginTop: '1rem',
              backgroundColor: '#f9fafb',
              border: '1px solid #e5e7eb',
              borderRadius: '6px',
              padding: '1rem',
              maxHeight: '300px',
              overflow: 'hidden'
            }}>
              <h3 style={{ fontSize: '1rem', fontWeight: 600, marginBottom: '0.75rem' }}>Recent Results</h3>
              <div style={{ maxHeight: '200px', overflowY: 'auto' }}>
                {logs.slice(0, 10).map((l, idx) => (
                  <div key={idx} style={{
                    padding: '0.5rem',
                    borderBottom: idx < Math.min(logs.length, 10) - 1 ? '1px solid #e5e7eb' : 'none',
                    fontSize: '0.875rem'
                  }}>
                    <div style={{ color: '#6b7280', fontSize: '0.75rem' }}>
                      {new Date(l.timestamp).toLocaleString()}
                    </div>
                    <div style={{ fontWeight: 500, marginTop: '0.25rem' }}>
                      {l.message}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* RIGHT COLUMN */}
        <div style={{ flex: '1', minWidth: '400px' }}>
          <InvestigationResults
            key={investigationResultsKey}
            highlightedInvestigatorId={highlightedInvestigatorId}
            onResultClick={handleResultClick}
          />
        </div>
      </div>

      {/* Create Investigator Modal */}
      <CreateInvestigatorModal
        isOpen={showModal}
        selectedType={selectedType}
        investigatorName={investigatorName}
        setSelectedType={setSelectedType}
        setInvestigatorName={setInvestigatorName}
        onConfirm={createInvestigator}
        onClose={closeCreateModal}
      />

      {/* Investigation Detail Modal */}
      <InvestigationDetailModal
        executionId={detailModalExecutionId}
        isOpen={isDetailModalOpen}
        onClose={closeDetailModal}
      />
    </div>
  );
}

export default Dashboard;
