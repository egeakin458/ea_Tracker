import React, { useEffect, useState } from "react";
import api from "./lib/axios";
import { CompletedInvestigation, InvestigationDetail } from "./types/api";
import ExportModal from "./components/ExportModal";

interface InvestigationResultsProps {
  highlightedInvestigatorId?: string;
  onResultClick: (executionId: number) => void;
}

function InvestigationResults({ highlightedInvestigatorId, onResultClick }: InvestigationResultsProps): JSX.Element {
  const [completedInvestigations, setCompletedInvestigations] = useState<CompletedInvestigation[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [highlightedExecutionId, setHighlightedExecutionId] = useState<number | null>(null);
  
  // Export functionality state
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [showExportModal, setShowExportModal] = useState(false);
  const [isExporting, setIsExporting] = useState(false);

  const loadCompletedInvestigations = async (): Promise<void> => {
    try {
      setError(null);
      setLoading(true);
      const res = await api.get<CompletedInvestigation[]>("/api/CompletedInvestigations");
      setCompletedInvestigations(res.data);
    } catch (err: any) {
      setError(err.message || "Failed to load completed investigations");
    } finally {
      setLoading(false);
    }
  };

  const highlightLatestForInvestigator = async (investigatorId: string): Promise<void> => {
    // Find latest investigation for this investigator from existing data
    const latestForInvestigator = completedInvestigations
      .filter(inv => inv.investigatorId === investigatorId)
      .sort((a, b) => new Date(b.completedAt).getTime() - new Date(a.completedAt).getTime())[0];
    
    if (latestForInvestigator) {
      setHighlightedExecutionId(latestForInvestigator.executionId);
      setTimeout(() => {
        const highlightedElement = document.getElementById(`investigation-${latestForInvestigator.executionId}`);
        if (highlightedElement) {
          highlightedElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
      }, 100);
    } else {
      setHighlightedExecutionId(null);
    }
  };

  useEffect(() => {
    void loadCompletedInvestigations();
  }, []);

  useEffect(() => {
    if (highlightedInvestigatorId) {
      void highlightLatestForInvestigator(highlightedInvestigatorId);
    } else {
      setHighlightedExecutionId(null);
    }
  }, [highlightedInvestigatorId]);

  const formatDateTime = (dateString: string): string => {
    return new Date(dateString).toLocaleString();
  };

  const calculateDuration = (startedAt: string, completedAt: string): string => {
    const start = new Date(startedAt);
    const end = new Date(completedAt);
    const diffMs = end.getTime() - start.getTime();
    const minutes = Math.floor(diffMs / (1000 * 60));
    const seconds = Math.floor((diffMs % (1000 * 60)) / 1000);
    return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  };

  const getSeverityColor = (anomalyCount: number): string => {
    if (anomalyCount > 5) return '#dc2626'; // Red for high anomalies
    if (anomalyCount > 0) return '#f59e0b'; // Yellow for some anomalies
    return '#10b981'; // Green for no anomalies
  };

  const clearAllResults = async (): Promise<void> => {
    if (!window.confirm("Are you sure you want to permanently delete ALL investigation results from the database? This action cannot be undone.")) {
      return;
    }
    
    try {
      await api.delete("/api/CompletedInvestigations/clear");
      setCompletedInvestigations([]);
      setHighlightedExecutionId(null);
      setSelectedIds([]); // Clear selections when clearing all results
    } catch (err: any) {
      setError(err.message || "Failed to clear investigation results");
    }
  };

  // Selection handlers
  const handleCheckboxChange = (executionId: number, checked: boolean): void => {
    setSelectedIds(prev => 
      checked 
        ? [...prev, executionId]
        : prev.filter(id => id !== executionId)
    );
  };

  const handleSelectAll = (): void => {
    setSelectedIds(
      selectedIds.length === completedInvestigations.length 
        ? [] 
        : completedInvestigations.map(inv => inv.executionId)
    );
  };

  if (loading) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center', color: '#6b7280' }}>
        Loading investigation results...
      </div>
    );
  }

  return (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <header style={{ marginBottom: '1rem', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
          <h2 style={{ fontSize: '1.5rem', fontWeight: 'bold', color: '#1f2937', margin: 0 }}>
            Investigation Results
          </h2>
          {completedInvestigations.length > 0 && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', fontSize: '0.875rem', color: '#6b7280' }}>
              <input
                type="checkbox"
                checked={selectedIds.length === completedInvestigations.length && completedInvestigations.length > 0}
                onChange={handleSelectAll}
                disabled={completedInvestigations.length === 0}
                style={{ width: '16px', height: '16px', cursor: 'pointer' }}
              />
              <span>Select All ({selectedIds.length} selected)</span>
            </div>
          )}
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          {selectedIds.length > 0 && (
            <button
              onClick={() => setShowExportModal(true)}
              disabled={isExporting}
              style={{
                padding: '0.5rem 1rem',
                backgroundColor: '#059669',
                color: 'white',
                border: 'none',
                borderRadius: '6px',
                cursor: 'pointer',
                fontSize: '0.875rem',
                fontWeight: '500',
                opacity: isExporting ? 0.5 : 1
              }}
              onMouseEnter={(e) => !isExporting && (e.currentTarget.style.backgroundColor = '#047857')}
              onMouseLeave={(e) => !isExporting && (e.currentTarget.style.backgroundColor = '#059669')}
            >
              Export Selected ({selectedIds.length})
            </button>
          )}
          {completedInvestigations.length > 0 && (
            <button
              onClick={clearAllResults}
              style={{
                padding: '0.5rem 1rem',
                backgroundColor: '#dc2626',
                color: 'white',
                border: 'none',
                borderRadius: '6px',
                cursor: 'pointer',
                fontSize: '0.875rem',
                fontWeight: '500'
              }}
              onMouseEnter={(e) => e.currentTarget.style.backgroundColor = '#b91c1c'}
              onMouseLeave={(e) => e.currentTarget.style.backgroundColor = '#dc2626'}
            >
              Clear All
            </button>
          )}
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

      <div style={{ 
        flex: 1, 
        overflowY: 'auto', 
        backgroundColor: 'white',
        border: '1px solid #e5e7eb',
        borderRadius: '8px'
      }}>
        {completedInvestigations.length === 0 ? (
          <div style={{ padding: '2rem', textAlign: 'center', color: '#6b7280' }}>
            No completed investigations found.
            <br />
            <small>Start and complete some investigators to see results here.</small>
          </div>
        ) : (
          <div style={{ padding: '1rem' }}>
            {completedInvestigations.map((investigation, index) => (
              <div
                key={investigation.executionId}
                id={`investigation-${investigation.executionId}`}
                style={{
                  padding: '1rem',
                  marginBottom: '0.75rem',
                  backgroundColor: investigation.executionId === highlightedExecutionId ? '#fef3c7' : '#f9fafb',
                  border: `2px solid ${investigation.executionId === highlightedExecutionId ? '#f59e0b' : '#e5e7eb'}`,
                  borderRadius: '8px',
                  transition: 'all 0.2s ease'
                }}
              >
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: '0.75rem' }}>
                  {/* Checkbox */}
                  <input
                    type="checkbox"
                    checked={selectedIds.includes(investigation.executionId)}
                    onChange={(e) => handleCheckboxChange(investigation.executionId, e.target.checked)}
                    onClick={(e) => e.stopPropagation()}
                    style={{ 
                      width: '18px', 
                      height: '18px', 
                      cursor: 'pointer',
                      marginTop: '0.125rem'
                    }}
                  />
                  
                  {/* Investigation content */}
                  <div 
                    style={{ 
                      flex: 1, 
                      cursor: 'pointer' 
                    }}
                    onClick={() => onResultClick(investigation.executionId)}
                    onMouseEnter={(e) => {
                      if (investigation.executionId !== highlightedExecutionId) {
                        e.currentTarget.parentElement!.style.backgroundColor = '#f3f4f6';
                      }
                    }}
                    onMouseLeave={(e) => {
                      if (investigation.executionId !== highlightedExecutionId) {
                        e.currentTarget.parentElement!.style.backgroundColor = '#f9fafb';
                      }
                    }}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.5rem' }}>
                      <div>
                        <h3 style={{ 
                          fontSize: '1rem', 
                          fontWeight: '600', 
                          color: '#1f2937', 
                          margin: '0 0 0.25rem 0'
                        }}>
                          {investigation.investigatorName}
                        </h3>
                        <div style={{ fontSize: '0.875rem', color: '#6b7280' }}>
                          Duration: {investigation.completedAt && investigation.startedAt ? calculateDuration(investigation.startedAt, investigation.completedAt) : '00:00'}
                        </div>
                      </div>
                      <div style={{ textAlign: 'right' }}>
                        <div style={{ fontSize: '1rem', fontWeight: '600', color: '#1f2937' }}>
                          {investigation.resultCount} results
                        </div>
                        {investigation.anomalyCount > 0 && (
                          <div style={{ 
                            fontSize: '0.875rem', 
                            color: getSeverityColor(investigation.anomalyCount),
                            fontWeight: '500'
                          }}>
                            {investigation.anomalyCount} anomalies
                          </div>
                        )}
                      </div>
                    </div>
                    
                    <div style={{ 
                      display: 'flex', 
                      justifyContent: 'space-between', 
                      fontSize: '0.75rem', 
                      color: '#9ca3af',
                      paddingTop: '0.5rem',
                      borderTop: '1px solid #e5e7eb'
                    }}>
                      <span>Started: {formatDateTime(investigation.startedAt)}</span>
                      <span>Completed: {formatDateTime(investigation.completedAt)}</span>
                    </div>
                  </div>
                </div>
                
                {investigation.executionId === highlightedExecutionId && (
                  <div style={{
                    position: 'absolute',
                    right: '-8px',
                    top: '50%',
                    transform: 'translateY(-50%)',
                    width: '4px',
                    height: '80%',
                    backgroundColor: '#f59e0b',
                    borderRadius: '2px'
                  }} />
                )}
              </div>
            ))}
          </div>
        )}
      </div>
      
      {/* Export Modal */}
      {showExportModal && (
        <ExportModal
          isOpen={showExportModal}
          onClose={() => setShowExportModal(false)}
          selectedCount={selectedIds.length}
          onExport={(format) => {
            // TODO: Implement export API call in next commit
            console.log('Export requested:', { selectedIds, format });
            setShowExportModal(false);
          }}
          isExporting={isExporting}
        />
      )}
    </div>
  );
}

export default InvestigationResults;