import React, { useEffect, useState } from "react";
import api from "./lib/axios";
import { CompletedInvestigation, InvestigationDetail } from "./types/api";

interface InvestigationResultsProps {
  highlightedInvestigatorId?: string;
  onResultClick: (executionId: number) => void;
}

function InvestigationResults({ highlightedInvestigatorId, onResultClick }: InvestigationResultsProps): JSX.Element {
  const [completedInvestigations, setCompletedInvestigations] = useState<CompletedInvestigation[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [highlightedExecutionId, setHighlightedExecutionId] = useState<number | null>(null);

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

  if (loading) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center', color: '#6b7280' }}>
        Loading investigation results...
      </div>
    );
  }

  return (
    <div style={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
      <header style={{ marginBottom: '1rem' }}>
        <h2 style={{ fontSize: '1.5rem', fontWeight: 'bold', color: '#1f2937', margin: 0 }}>
          Investigation Results
        </h2>
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
                onClick={() => onResultClick(investigation.executionId)}
                style={{
                  padding: '1rem',
                  marginBottom: '0.75rem',
                  backgroundColor: investigation.executionId === highlightedExecutionId ? '#fef3c7' : '#f9fafb',
                  border: `2px solid ${investigation.executionId === highlightedExecutionId ? '#f59e0b' : '#e5e7eb'}`,
                  borderRadius: '8px',
                  cursor: 'pointer',
                  transition: 'all 0.2s ease'
                }}
                onMouseEnter={(e) => {
                  if (investigation.executionId !== highlightedExecutionId) {
                    e.currentTarget.style.backgroundColor = '#f3f4f6';
                  }
                }}
                onMouseLeave={(e) => {
                  if (investigation.executionId !== highlightedExecutionId) {
                    e.currentTarget.style.backgroundColor = '#f9fafb';
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
    </div>
  );
}

export default InvestigationResults;