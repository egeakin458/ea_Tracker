import React from 'react';

interface ToastProps {
  message: string | null;
  type?: 'success' | 'error' | 'info';
  onClose: () => void;
}

export default function Toast({ message, type = 'info', onClose }: ToastProps): JSX.Element | null {
  if (!message) return null;
  const colors = {
    success: { bg: 'var(--success-light)', fg: 'var(--success-color)' },
    error: { bg: 'var(--danger-light)', fg: 'var(--danger-color)' },
    info: { bg: 'var(--primary-light)', fg: 'var(--primary-color)' }
  }[type];

  return (
    <div style={{ position: 'fixed', right: '1rem', bottom: '1rem', zIndex: 2000 }}>
      <div className="p-md" style={{ backgroundColor: colors.bg, color: colors.fg, border: `1px solid ${colors.fg}`, borderRadius: '6px', boxShadow: 'var(--shadow-md)' }}>
        <div className="flex items-center justify-between" style={{ gap: '1rem' }}>
          <span>{message}</span>
          <button onClick={onClose} style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: colors.fg }}>✕</button>
        </div>
      </div>
    </div>
  );
}


