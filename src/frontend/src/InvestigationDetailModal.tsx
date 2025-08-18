import { useEffect, useState } from "react";
import api from "./lib/axios";
import { InvestigationDetail } from "./types/api";

interface InvestigationDetailModalProps {
  executionId: number | null;
  isOpen: boolean;
  onClose: () => void;
}

function InvestigationDetailModal({ executionId, isOpen, onClose }: InvestigationDetailModalProps): JSX.Element | null {
  const [details, setDetails] = useState<InvestigationDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadDetails = async (execId: number): Promise<void> => {
    try {
      setLoading(true);
      setError(null);
      const response = await api.get<InvestigationDetail>(`/api/CompletedInvestigations/${execId}`);
      setDetails(response.data);
    } catch (err: any) {
      setError(err.message || "Failed to load investigation details");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen && executionId) {
      loadDetails(executionId);
    }
  }, [isOpen, executionId]);

  const formatDateTime = (dateString: string): string => {
    return new Date(dateString).toLocaleString();
  };

  if (!isOpen) return null;

  return (
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
        width: '800px',
        maxWidth: '90vw',
        maxHeight: '80vh',
        display: 'flex',
        flexDirection: 'column'
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
          <h2 style={{ fontSize: '1.5rem', fontWeight: '600', margin: 0 }}>
            Investigation Details
          </h2>
          <button
            onClick={onClose}
            style={{
              backgroundColor: 'transparent',
              border: 'none',
              fontSize: '1.5rem',
              cursor: 'pointer',
              color: '#6b7280',
              padding: '0.25rem',
              lineHeight: 1
            }}
          >
            ×
          </button>
        </div>

        {loading && (
          <div style={{ padding: '2rem', textAlign: 'center', color: '#6b7280' }}>
            Loading investigation details...
          </div>
        )}

        {error && (
          <div style={{ 
            padding: '1rem', 
            backgroundColor: '#fee2e2', 
            color: '#dc2626', 
            border: '1px solid #fecaca',
            borderRadius: '6px',
            marginBottom: '1rem'
          }}>
            {error}
          </div>
        )}

        {details && !loading && (
          <div style={{ flex: 1, overflowY: 'auto' }}>
            {/* Summary Section */}
            <div style={{ 
              backgroundColor: '#f9fafb', 
              padding: '1rem', 
              borderRadius: '6px',
              marginBottom: '1.5rem'
            }}>
              <h3 style={{ fontSize: '1.125rem', fontWeight: '600', marginBottom: '0.75rem' }}>
                {details.summary.investigatorName}
              </h3>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '0.5rem', fontSize: '0.875rem' }}>
                <div>
                  <span style={{ color: '#6b7280' }}>Execution ID:</span> #{details.summary.executionId}
                </div>
                <div>
                  <span style={{ color: '#6b7280' }}>Total Results:</span> {details.summary.resultCount}
                </div>
                <div>
                  <span style={{ color: '#6b7280' }}>Started:</span> {formatDateTime(details.summary.startedAt)}
                </div>
                <div>
                  <span style={{ color: '#6b7280' }}>Completed:</span> {formatDateTime(details.summary.completedAt)}
                </div>
                {details.summary.anomalyCount > 0 && (
                  <div style={{ gridColumn: 'span 2' }}>
                    <span style={{ 
                      color: '#dc2626', 
                      fontWeight: '600' 
                    }}>
                      Anomalies Found: {details.summary.anomalyCount}
                    </span>
                  </div>
                )}
              </div>
            </div>

            {/* Results Section */}
            <div>
              <h3 style={{ fontSize: '1.125rem', fontWeight: '600', marginBottom: '1rem' }}>
                Investigation Results
              </h3>
              <div style={{ 
                maxHeight: '400px', 
                overflowY: 'auto',
                border: '1px solid #e5e7eb',
                borderRadius: '6px'
              }}>
                {details.detailedResults && details.detailedResults.length > 0 ? (
                  [...details.detailedResults]
                    .sort((a, b) => {
                      // Move "Investigation complete" messages to top
                      const aIsComplete = a.message && a.message.includes('Investigation complete:');
                      const bIsComplete = b.message && b.message.includes('Investigation complete:');
                      if (aIsComplete && !bIsComplete) return -1;
                      if (!aIsComplete && bIsComplete) return 1;
                      return 0; // Keep original order for other messages
                    })
                    .map((result, index) => (
                    <div 
                      key={index}
                      style={{ 
                        padding: '0.75rem',
                        borderBottom: index < details.detailedResults.length - 1 ? '1px solid #e5e7eb' : 'none',
                        backgroundColor: index % 2 === 0 ? 'white' : '#f9fafb'
                      }}
                    >
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                        <div style={{ flex: 1 }}>
                          <div style={{ 
                            fontWeight: '500', 
                            marginBottom: '0.25rem',
                            color: '#1f2937'
                          }}>
                            {result.message}
                          </div>
                          {result.payload && (
                            <div style={{ 
                              fontSize: '0.75rem', 
                              color: '#6b7280',
                              fontFamily: 'monospace',
                              backgroundColor: '#f3f4f6',
                              padding: '0.25rem 0.5rem',
                              borderRadius: '4px',
                              marginTop: '0.25rem'
                            }}>
                              {result.payload}
                            </div>
                          )}
                        </div>
                        <div style={{ 
                          fontSize: '0.75rem', 
                          color: '#9ca3af',
                          marginLeft: '1rem',
                          whiteSpace: 'nowrap'
                        }}>
                          {new Date(result.timestamp).toLocaleTimeString()}
                        </div>
                      </div>
                    </div>
                  ))
                ) : (
                  <div style={{ padding: '2rem', textAlign: 'center', color: '#6b7280' }}>
                    No detailed results available for this investigation.
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '1.5rem', paddingTop: '1rem', borderTop: '1px solid #e5e7eb' }}>
          <button
            onClick={onClose}
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
            Close
          </button>
        </div>
      </div>
    </div>
  );
}

export default InvestigationDetailModal;