import React from "react";
import { Investigator, LogEntry, CreateResponse } from "./types/api";
import InvestigationResults from './components/InvestigationResults';
import InvestigationDetailModal from './InvestigationDetailModal';
import { useNavigate } from 'react-router-dom';
import Header from './components/Header';
import InvestigatorList from './components/InvestigatorList';
import { useInvestigations } from './hooks/useInvestigations';
import Toast from './components/Toast';
import api from './lib/axios';
import CreateInvestigatorModal from './components/CreateInvestigatorModal';

function Dashboard(): JSX.Element {
  const {
    investigators,
    logs,
    selected,
    loading,
    error,
    connStatus,
    resultsKey: investigationResultsKey,
    setSelected,
    loadInvestigators,
    select,
    startOne,
    deleteOne,
    setErrorMessage,
  } = useInvestigations();
  const [showModal, setShowModal] = React.useState(false);
  const [selectedType, setSelectedType] = React.useState('');
  const [investigatorName, setInvestigatorName] = React.useState('');
  const [highlightedInvestigatorId, setHighlightedInvestigatorId] = React.useState<string | undefined>(undefined);
  const [detailModalExecutionId, setDetailModalExecutionId] = React.useState<number | null>(null);
  const [isDetailModalOpen, setIsDetailModalOpen] = React.useState(false);
  const navigate = useNavigate();
  const [toast, setToast] = React.useState<{ message: string; type?: 'success' | 'error' | 'info' } | null>(null);

  // SignalR and data loading is handled inside useInvestigations


  // startOne provided by hook

  // Removed stopOne; investigations are one-shot operations now

  // select provided by hook


  const handleInvestigatorClick = (id: string): void => {
    // Select for left panel results (existing functionality)
    void select(id);
    // Highlight in right panel (new functionality)
    setHighlightedInvestigatorId(id);
  };

  const handleResultClick = (executionId: number): void => {
    // Navigate to the detail page; keep modal support temporarily if needed
    navigate(`/investigations/results/${executionId}`);
    // setDetailModalExecutionId(executionId);
    // setIsDetailModalOpen(true);
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
      setErrorMessage("Please select an investigator type");
      return;
    }
    if (!investigatorName.trim()) {
      setErrorMessage("Please enter an investigator name");
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
      setErrorMessage(err.message || `Failed to create ${selectedType} investigator`);
    }
  };

  // deleteOne provided by hook (confirm in caller)

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

      {/* Investigation Detail Modal (temporarily kept, no longer used when navigating) */}

      {/* Toast */}
      <Toast message={toast?.message || null} type={toast?.type} onClose={() => setToast(null)} />
    </div>
  );
}

export default Dashboard;
