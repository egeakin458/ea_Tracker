import React from 'react';

type ConnState = 'connecting' | 'connected' | 'disconnected';

interface HeaderProps {
  connStatus: ConnState;
}

export default function Header({ connStatus }: HeaderProps): JSX.Element {
  const color = connStatus === 'connected' ? '#065f46' : connStatus === 'connecting' ? '#92400e' : '#991b1b';
  const text = connStatus === 'connected' ? 'Live updates: Connected' : connStatus === 'connecting' ? 'Live updates: Connecting…' : 'Live updates: Disconnected';

  return (
    <header className="flex items-center justify-between mb-xl">
      <h1 className="text-3xl font-bold text-primary" style={{ marginBottom: '0.5rem' }}>
        ea_Tracker Investigation Dashboard
      </h1>
      <div className="text-sm" style={{ color }}>{text}</div>
    </header>
  );
}


