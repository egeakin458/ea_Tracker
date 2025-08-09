import React, { useEffect, useState } from "react";
import api from "../lib/axios";
import { CompletedInvestigation } from "../types/api";

interface InvestigationResultsProps {
  highlightedInvestigatorId?: string;
  onResultClick: (executionId: number) => void;
}

export default function InvestigationResults({ highlightedInvestigatorId, onResultClick }: InvestigationResultsProps): JSX.Element {
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

  const formatDateTime = (dateString: string): string => new Date(dateString).toLocaleString();

  const calculateDuration = (startedAt: string, completedAt: string): string => {
    const start = new Date(startedAt);
    const end = new Date(completedAt);
    const diffMs = end.getTime() - start.getTime();
    const minutes = Math.floor(diffMs / (1000 * 60));
    const seconds = Math.floor((diffMs % (1000 * 60)) / 1000);
    return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  };

  const getSeverityColor = (anomalyCount: number): string => {
    if (anomalyCount > 5) return '#dc2626';
    if (anomalyCount > 0) return '#f59e0b';
    return '#10b981';
  };

  if (loading) return <div className="p-xl text-center text-secondary">Loading investigation results...</div>;

  return (
    <div className="flex flex-col" style={{ height: '100%' }}>
      <header className="mb-md">
        <h2 className="text-2xl font-bold text-primary m-0">Investigation Results</h2>
      </header>

      {error && (
        <div className="p-md mb-md" style={{ backgroundColor: 'var(--danger-light)', color: 'var(--danger-color)', border: '1px solid #fecaca', borderRadius: '6px' }}>{error}</div>
      )}

      <div className="flex-1" style={{ overflowY: 'auto', backgroundColor: 'white', border: '1px solid var(--card-border)', borderRadius: '8px' }}>
        {completedInvestigations.length === 0 ? (
          <div style={{ padding: '2rem', textAlign: 'center', color: '#6b7280' }}>
            No completed investigations found.
            <br />
            <small>Start and complete some investigators to see results here.</small>
          </div>
        ) : (
          <div className="p-md">
            {completedInvestigations.map((investigation) => (
              <div
                key={investigation.executionId}
                id={`investigation-${investigation.executionId}`}
                onClick={() => onResultClick(investigation.executionId)}
                style={{ padding: '1rem', marginBottom: '0.75rem', backgroundColor: investigation.executionId === highlightedExecutionId ? '#fef3c7' : '#f9fafb', border: `2px solid ${investigation.executionId === highlightedExecutionId ? '#f59e0b' : 'var(--border-color)'}`, borderRadius: '8px', cursor: 'pointer', transition: 'all 0.2s ease' }}
              >
                <div className="flex items-center justify-between" style={{ marginBottom: '0.5rem' }}>
                  <div>
                    <h3 className="text-base font-semibold text-primary" style={{ margin: '0 0 0.25rem 0' }}>
                      {investigation.investigatorName}
                    </h3>
                    <div className="text-sm text-secondary">
                      Duration: {investigation.completedAt && investigation.startedAt ? calculateDuration(investigation.startedAt, investigation.completedAt) : '00:00'}
                    </div>
                  </div>
                  <div style={{ textAlign: 'right' }}>
                    <div className="text-base font-semibold text-primary">
                      {investigation.resultCount} results
                    </div>
                    {investigation.anomalyCount > 0 && (
                      <div style={{ fontSize: '0.875rem', color: getSeverityColor(investigation.anomalyCount), fontWeight: '500' }}>
                        {investigation.anomalyCount} anomalies
                      </div>
                    )}
                  </div>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', color: '#9ca3af', paddingTop: '0.5rem', borderTop: '1px solid #e5e7eb' }}>
                  <span>Started: {formatDateTime(investigation.startedAt)}</span>
                  <span>Completed: {formatDateTime(investigation.completedAt)}</span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}


