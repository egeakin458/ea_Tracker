import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import api from '../lib/axios';
import { InvestigationDetail } from '../types/api';

export default function InvestigationDetailPage(): JSX.Element {
  const { executionId } = useParams<{ executionId: string }>();
  const [detail, setDetail] = useState<InvestigationDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const load = async (): Promise<void> => {
      if (!executionId) return;
      try {
        setError(null);
        setLoading(true);
        const res = await api.get<InvestigationDetail>(`/api/CompletedInvestigations/${executionId}`);
        setDetail(res.data);
      } catch (err: any) {
        setError(err.message || 'Failed to load investigation details');
      } finally {
        setLoading(false);
      }
    };
    void load();
  }, [executionId]);

  if (loading) return <div className="p-lg text-secondary">Loading…</div>;
  if (error) return <div className="p-lg text-danger">{error}</div>;
  if (!detail) return <div className="p-lg text-secondary">No details found.</div>;

  return (
    <div className="container p-lg">
      <h1 className="text-2xl font-bold mb-lg">Investigation #{detail.summary.executionId}</h1>
      <div className="mb-lg">
        <div className="text-lg font-semibold">{detail.summary.investigatorName}</div>
        <div className="text-secondary">{new Date(detail.summary.startedAt).toLocaleString()} → {new Date(detail.summary.completedAt).toLocaleString()}</div>
      </div>
      <div className="mb-lg">
        <div className="font-semibold">Results ({detail.summary.resultCount})</div>
        <div className="text-secondary">Anomalies: {detail.summary.anomalyCount}</div>
      </div>
      <div style={{ maxHeight: '50vh', overflowY: 'auto', border: '1px solid var(--card-border)', borderRadius: '8px' }}>
        {detail.detailedResults.map((r, idx) => (
          <div key={idx} style={{ padding: '0.75rem', borderBottom: '1px solid var(--border-color)' }}>
            <div className="text-sm text-secondary">{new Date(r.timestamp).toLocaleString()}</div>
            <div className="font-medium">{r.message}</div>
          </div>
        ))}
      </div>
    </div>
  );
}


