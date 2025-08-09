import React from "react";
import { Routes, Route, Navigate } from 'react-router-dom';
import DashboardPage from './pages/DashboardPage';
import InvestigationDetailPage from './pages/InvestigationDetailPage';

function App(): JSX.Element {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route path="/dashboard" element={<DashboardPage />} />
      <Route path="/investigations/results/:executionId" element={<InvestigationDetailPage />} />
    </Routes>
  );
}

export default App;
