import React, { useEffect, useState } from "react";
import api from "./lib/axios";
import { Investigator, LogEntry } from "./types/api";

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

  const startInvestigators = async (): Promise<void> => {
    try {
      await api.post("/api/investigations/start");
      await loadInvestigators();
    } catch (err: any) {
      setError(err.message || "Failed to start investigators");
    }
  };

  const stopInvestigators = async (): Promise<void> => {
    try {
      await api.post("/api/investigations/stop");
      await loadInvestigators();
    } catch (err: any) {
      setError(err.message || "Failed to stop investigators");
    }
  };

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
    setSelected(id);
    const res = await api.get<LogEntry[]>(`/api/investigations/${id}/results`);
    setLogs(res.data);
  };

  return (
    <div className="p-8 font-sans">
      <h2 className="mb-4 text-xl font-bold">Investigators</h2>
      {error && <div className="mb-2 text-red-600">{error}</div>}
      <div className="mb-4 space-x-2">
        <button onClick={startInvestigators} className="px-2 py-1 bg-green-600 text-white">Start All</button>
        <button onClick={stopInvestigators} className="px-2 py-1 bg-red-600 text-white">Stop All</button>
      </div>
      {loading ? (
        <div>Loading...</div>
      ) : (
        <table className="min-w-full border">
        <thead>
          <tr>
            <th className="border px-2">Id</th>
            <th className="border px-2">Name</th>
            <th className="border px-2">Status</th>
            <th className="border px-2">Results</th>
            <th className="border px-2">Actions</th>
          </tr>
        </thead>
        <tbody>
          {investigators.map((inv) => (
            <tr key={inv.id} className="border" onClick={() => select(inv.id)}>
              <td className="px-2 border">{inv.id}</td>
              <td className="px-2 border">{inv.name}</td>
              <td className="px-2 border">{inv.isRunning ? "Running" : "Stopped"}</td>
              <td className="px-2 border text-center">{inv.resultCount}</td>
              <td className="px-2 border space-x-1">
                <button
                  onClick={e => { e.stopPropagation(); startOne(inv.id); }}
                  className="px-1 bg-green-500 text-white"
                >
                  Start
                </button>
                <button
                  onClick={e => { e.stopPropagation(); stopOne(inv.id); }}
                  className="px-1 bg-red-500 text-white"
                >
                  Stop
                </button>
              </td>
            </tr>
          ))}
        </tbody>
        </table>
      )}
      {selected && (
        <div className="mt-4">
          <h3 className="font-semibold">Logs</h3>
          <ul className="list-disc ml-4">
            {logs.map((l, idx) => (
              <li key={idx}>{l.timestamp} - {l.message}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}

export default Dashboard;
