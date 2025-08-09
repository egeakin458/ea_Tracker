import React from 'react';

type ConnState = 'connecting' | 'connected' | 'disconnected';

interface HeaderProps {
  connStatus: ConnState;
}

export default function Header({ connStatus }: HeaderProps): JSX.Element {
  const color = connStatus === 'connected' ? '#065f46' : connStatus === 'connecting' ? '#92400e' : '#991b1b';
  const text = connStatus === 'connected' ? 'Live updates: Connected' : connStatus === 'connecting' ? 'Live updates: Connecting…' : 'Live updates: Disconnected';

  return (
    <header style={{ marginBottom: '2rem', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
      <h1 style={{ fontSize: '2rem', fontWeight: 'bold', color: '#1f2937', marginBottom: '0.5rem' }}>
        ea_Tracker Investigation Dashboard
      </h1>
      <div style={{ fontSize: '0.875rem', color }}>{text}</div>
    </header>
  );
}


