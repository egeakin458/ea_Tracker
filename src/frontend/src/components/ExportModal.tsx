import React, { useState } from 'react';

interface ExportModalProps {
  isOpen: boolean;
  onClose: () => void;
  selectedCount: number;
  onExport: (format: string) => void;
  isExporting: boolean;
}

function ExportModal({ isOpen, onClose, selectedCount, onExport, isExporting }: ExportModalProps) {
  const [selectedFormat, setSelectedFormat] = useState<string>('json');
  
  if (!isOpen) return null;
  
  const formats = [
    { value: 'json', label: 'JSON', description: 'Structured data format, ideal for processing' },
    { value: 'csv', label: 'CSV', description: 'Comma-separated values, opens in Excel' },
    { value: 'excel', label: 'Excel', description: 'Microsoft Excel workbook with multiple sheets' }
  ];

  const handleExport = () => {
    onExport(selectedFormat);
  };

  return (
    <div 
      style={{
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
      }}
      onClick={onClose}
    >
      <div 
        style={{
          backgroundColor: 'white',
          borderRadius: '8px',
          width: '90%',
          maxWidth: '500px',
          boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)'
        }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          padding: '20px',
          borderBottom: '1px solid #e0e0e0'
        }}>
          <h2 style={{ margin: 0, color: '#333' }}>Export Investigations</h2>
          <button 
            style={{
              background: 'none',
              border: 'none',
              fontSize: '24px',
              cursor: 'pointer',
              color: '#666',
              padding: 0,
              width: '30px',
              height: '30px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center'
            }}
            onClick={onClose}
            onMouseEnter={(e) => e.currentTarget.style.color = '#000'}
            onMouseLeave={(e) => e.currentTarget.style.color = '#666'}
          >
            &times;
          </button>
        </div>
        
        {/* Body */}
        <div style={{ padding: '20px' }}>
          <p style={{
            marginBottom: '20px',
            color: '#666',
            fontSize: '16px'
          }}>
            Exporting {selectedCount} investigation{selectedCount !== 1 ? 's' : ''}
          </p>
          
          <div>
            <h3 style={{
              marginBottom: '15px',
              color: '#333',
              fontSize: '16px'
            }}>
              Select Export Format:
            </h3>
            {formats.map(format => (
              <label 
                key={format.value}
                style={{
                  display: 'flex',
                  alignItems: 'flex-start',
                  marginBottom: '15px',
                  cursor: 'pointer',
                  padding: '10px',
                  border: '1px solid #e0e0e0',
                  borderRadius: '4px',
                  transition: 'background-color 0.2s',
                  backgroundColor: selectedFormat === format.value ? '#f0f9ff' : 'transparent',
                  borderColor: selectedFormat === format.value ? '#0ea5e9' : '#e0e0e0'
                }}
                onMouseEnter={(e) => {
                  if (selectedFormat !== format.value) {
                    e.currentTarget.style.backgroundColor = '#f5f5f5';
                  }
                }}
                onMouseLeave={(e) => {
                  if (selectedFormat !== format.value) {
                    e.currentTarget.style.backgroundColor = 'transparent';
                  }
                }}
              >
                <input
                  type="radio"
                  value={format.value}
                  checked={selectedFormat === format.value}
                  onChange={(e) => setSelectedFormat(e.target.value)}
                  disabled={isExporting}
                  style={{ 
                    marginRight: '10px',
                    marginTop: '3px',
                    cursor: 'pointer'
                  }}
                />
                <div style={{ display: 'flex', flexDirection: 'column' }}>
                  <strong style={{ color: '#333', marginBottom: '4px' }}>
                    {format.label}
                  </strong>
                  <span style={{ color: '#666', fontSize: '14px' }}>
                    {format.description}
                  </span>
                </div>
              </label>
            ))}
          </div>
        </div>
        
        {/* Footer */}
        <div style={{
          display: 'flex',
          justifyContent: 'flex-end',
          gap: '10px',
          padding: '20px',
          borderTop: '1px solid #e0e0e0'
        }}>
          <button 
            style={{
              padding: '10px 20px',
              borderRadius: '4px',
              border: 'none',
              cursor: 'pointer',
              fontSize: '14px',
              transition: 'opacity 0.2s',
              backgroundColor: '#f0f0f0',
              color: '#333',
              opacity: isExporting ? 0.5 : 1
            }}
            onClick={onClose}
            disabled={isExporting}
            onMouseEnter={(e) => !isExporting && (e.currentTarget.style.backgroundColor = '#e0e0e0')}
            onMouseLeave={(e) => !isExporting && (e.currentTarget.style.backgroundColor = '#f0f0f0')}
          >
            Cancel
          </button>
          <button 
            style={{
              padding: '10px 20px',
              borderRadius: '4px',
              border: 'none',
              cursor: 'pointer',
              fontSize: '14px',
              transition: 'opacity 0.2s',
              backgroundColor: '#007bff',
              color: 'white',
              opacity: isExporting ? 0.5 : 1
            }}
            onClick={handleExport}
            disabled={isExporting}
            onMouseEnter={(e) => !isExporting && (e.currentTarget.style.backgroundColor = '#0056b3')}
            onMouseLeave={(e) => !isExporting && (e.currentTarget.style.backgroundColor = '#007bff')}
          >
            {isExporting ? 'Exporting...' : 'Export'}
          </button>
        </div>
      </div>
    </div>
  );
}

export default ExportModal;