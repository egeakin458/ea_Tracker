-- Anomalous Invoices for Investigation Testing
-- This bypasses service layer validation by inserting directly into database

USE ea_tracker_db;

-- Insert invoices with anomalies
INSERT INTO Invoices (RecipientName, TotalAmount, IssueDate, TotalTax, InvoiceType, CreatedAt, UpdatedAt, HasAnomalies, LastInvestigatedAt) VALUES
-- Negative amounts
('Negative Corp 1', -500.00, '2025-01-12', 50.00, 0, NOW(), NOW(), 0, NULL),
('Negative Corp 2', -1200.00, '2025-01-11', 120.00, 1, NOW(), NOW(), 0, NULL),
('Negative Corp 3', -750.00, '2025-01-10', 75.00, 0, NOW(), NOW(), 0, NULL),

-- High tax ratios (>50%)
('HighTax Ltd 1', 1000.00, '2025-01-09', 600.00, 1, NOW(), NOW(), 0, NULL),  -- 60%
('HighTax Ltd 2', 2000.00, '2025-01-08', 1200.00, 0, NOW(), NOW(), 0, NULL), -- 60%
('HighTax Ltd 3', 1500.00, '2025-01-07', 900.00, 1, NOW(), NOW(), 0, NULL),  -- 60%

-- Future dates
('Future Co 1', 3000.00, '2025-12-31', 300.00, 0, NOW(), NOW(), 0, NULL),
('Future Co 2', 2500.00, '2026-01-15', 250.00, 1, NOW(), NOW(), 0, NULL),
('Future Co 3', 4000.00, '2025-11-30', 400.00, 0, NOW(), NOW(), 0, NULL);

-- Verify insertion
SELECT 'Anomalous invoices added' as Status;
SELECT 
    COUNT(*) as Total_Invoices,
    SUM(CASE WHEN TotalAmount < 0 THEN 1 ELSE 0 END) as Negative_Amount,
    SUM(CASE WHEN (TotalTax / NULLIF(TotalAmount, 0)) > 0.5 THEN 1 ELSE 0 END) as High_Tax_Ratio,
    SUM(CASE WHEN IssueDate > CURDATE() THEN 1 ELSE 0 END) as Future_Date
FROM Invoices;