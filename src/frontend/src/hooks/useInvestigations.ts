import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import api from '../lib/axios';
import { Investigator, LogEntry, CreateResponse } from '../types/api';
import { SignalRService } from '../lib/SignalRService';

export type ConnState = 'connecting' | 'connected' | 'disconnected';

export function useInvestigations() {
  const [investigators, setInvestigators] = useState<Investigator[]>([]);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [selected, setSelectedInternal] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [connStatus, setConnStatus] = useState<ConnState>('disconnected');
  const [resultsKey, setResultsKey] = useState(0);
  const signalR = useRef<SignalRService | null>(null);
  const selectedRef = useRef<string | null>(null);

  const setSelected = useCallback((value: string | null) => {
    setSelectedInternal(value);
    selectedRef.current = value;
  }, []);

  const loadInvestigators = useCallback(async () => {
    try {
      setError(null);
      setLoading(true);
      const res = await api.get<Investigator[]>("/api/investigations");
      // Ensure status is always present for UI
      setInvestigators(res.data.map((inv: any) => ({
        ...inv,
        status: inv.status ?? 'Stopped',
      })));
    } catch (err: any) {
      setError(err.message || 'Failed to load investigators');
    } finally {
      setLoading(false);
    }
  }, []);

  const select = useCallback(async (id: string) => {
    try {
      setSelected(id);
      const res = await api.get<LogEntry[]>(`/api/investigations/${id}/results`);
      setLogs(res.data);
    } catch (err: any) {
      setError(err.message || `Failed to load results for investigator ${id}`);
    }
  }, [setSelected]);

  const startOne = useCallback(async (id: string) => {
    try {
      const res = await api.post(`/api/investigations/${id}/start`);
      const jobId = res.data?.jobId as string | undefined;
      setInvestigators(prev => prev.map(inv => inv.id === id ? { ...inv, status: 'Queued', jobId } : inv));
    } catch (err: any) {
      setError(err.message || `Failed to start investigator ${id}`);
    }
  }, []);

  const deleteOne = useCallback(async (id: string) => {
    try {
      await api.delete(`/api/investigations/${id}`);
      await loadInvestigators();
      if (selectedRef.current === id) {
        setSelected(null);
        setLogs([]);
      }
    } catch (err: any) {
      setError(err.message || `Failed to delete investigator ${id}`);
    }
  }, [loadInvestigators, setSelected]);

  useEffect(() => {
    void loadInvestigators();
    const svc = new SignalRService();
    signalR.current = svc;
    const baseUrl = process.env.REACT_APP_API_BASE_URL as string;
    svc.start(baseUrl, {
      onConnectionChange: setConnStatus,
      onStarted: async () => {
        await loadInvestigators();
        if (selectedRef.current) await select(selectedRef.current);
      },
      onCompleted: async () => {
        await loadInvestigators();
        if (selectedRef.current) await select(selectedRef.current);
        setResultsKey(prev => prev + 1);
      },
      onNewResult: async (p) => {
        setInvestigators(prev => prev.map(inv => inv.id === p.investigatorId ? { ...inv, resultCount: (inv.resultCount || 0) + 1 } : inv));
        if (p.investigatorId === selectedRef.current) {
          const res = await api.get<LogEntry[]>(`/api/investigations/${p.investigatorId}/results`);
          setLogs(res.data);
        }
      },
      onStatusChanged: async () => {
        await loadInvestigators();
      }
    }).catch(() => setConnStatus('disconnected'));

    return () => { void svc.stop(); };
  }, [loadInvestigators, select]);

  return useMemo(() => ({
    investigators,
    logs,
    selected,
    loading,
    error,
    connStatus,
    resultsKey,
    setSelected,
    loadInvestigators,
    select,
    startOne,
    deleteOne,
    // expose error setter for caller-side operations
    setErrorMessage: setError,
  }), [investigators, logs, selected, loading, error, connStatus, resultsKey, setSelected, loadInvestigators, select, startOne, deleteOne]);
}


