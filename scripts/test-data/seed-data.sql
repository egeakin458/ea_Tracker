-- Test Data for ea_Tracker Database
-- Run this script in MySQL to create sample invoices and waybills
-- Usage: mysql -u root -p ea_tracker_db < scripts/test-data/seed-data.sql

USE ea_tracker_db;

-- Clear existing test data (optional - uncomment if you want to reset)
-- DELETE FROM InvestigationResults;
-- DELETE FROM InvestigationExecutions;
-- DELETE FROM Invoices;
-- DELETE FROM Waybills;

-- Insert sample Invoices (15 records)
INSERT INTO Invoices (RecipientName, TotalAmount, IssueDate, TotalTax, InvoiceType, CreatedAt, UpdatedAt, HasAnomalies, LastInvestigatedAt) VALUES
-- Normal invoices
('ABC Company Ltd.', 5000.00, '2025-01-10', 500.00, 0, NOW(), NOW(), 0, NULL),
('XYZ Corporation', 12500.50, '2025-01-09', 1250.05, 1, NOW(), NOW(), 0, NULL),
('Tech Solutions Inc.', 8750.00, '2025-01-08', 875.00, 0, NOW(), NOW(), 0, NULL),
('Global Traders', 3200.00, '2025-01-07', 320.00, 1, NOW(), NOW(), 0, NULL),
('Smart Systems LLC', 15000.00, '2025-01-06', 1500.00, 0, NOW(), NOW(), 0, NULL),

-- Invoices with anomalies (will be detected by investigators)
('Suspicious Corp', -500.00, '2025-01-05', 50.00, 0, NOW(), NOW(), 0, NULL),  -- Negative amount
('Tax Haven Inc.', 1000.00, '2025-01-04', 600.00, 1, NOW(), NOW(), 0, NULL),  -- High tax ratio (60%)
('Future Dated Co.', 2000.00, '2025-12-31', 200.00, 0, NOW(), NOW(), 0, NULL), -- Future date
('Big Tax Ltd.', 5000.00, '2025-01-03', 3000.00, 1, NOW(), NOW(), 0, NULL),   -- High tax ratio (60%)
('Negative Sales Inc.', -1200.00, '2025-01-02', 120.00, 0, NOW(), NOW(), 0, NULL), -- Negative amount

-- More normal data
('Retail Store #1', 4500.00, '2025-01-01', 450.00, 0, NOW(), NOW(), 0, NULL),
('Wholesale Depot', 18000.00, '2024-12-31', 1800.00, 1, NOW(), NOW(), 0, NULL),
('Service Provider A', 2800.00, '2024-12-30', 280.00, 0, NOW(), NOW(), 0, NULL),
('Manufacturing Co.', 25000.00, '2024-12-29', 2500.00, 1, NOW(), NOW(), 0, NULL),
('Consulting Group', 7500.00, '2024-12-28', 750.00, 0, NOW(), NOW(), 0, NULL);

-- Insert sample Waybills (15 records)
INSERT INTO Waybills (RecipientName, TotalAmount, IssueDate, ShipmentDate, DeliveryDate, DueDate, Status, Type, CreatedAt, UpdatedAt, HasAnomalies, LastInvestigatedAt) VALUES
-- Normal waybills
('Customer A', 1000.00, '2025-01-10', '2025-01-10', '2025-01-11', '2025-01-15', 0, 0, NOW(), NOW(), 0, NULL),
('Customer B', 2500.00, '2025-01-09', '2025-01-09', '2025-01-10', '2025-01-14', 0, 1, NOW(), NOW(), 0, NULL),
('Customer C', 750.00, '2025-01-08', '2025-01-08', '2025-01-09', '2025-01-13', 0, 0, NOW(), NOW(), 0, NULL),
('Customer D', 3200.00, '2025-01-07', '2025-01-07', '2025-01-08', '2025-01-12', 0, 1, NOW(), NOW(), 0, NULL),
('Customer E', 500.00, '2025-01-06', '2025-01-06', '2025-01-07', '2025-01-11', 0, 0, NOW(), NOW(), 0, NULL),

-- Waybills with issues (will be detected by investigators)
('Late Delivery Inc.', 1500.00, '2025-01-01', '2025-01-01', NULL, '2025-01-05', 1, 0, NOW(), NOW(), 0, NULL), -- Overdue
('Expiring Soon Co.', 2000.00, '2025-01-10', '2025-01-10', NULL, DATE_ADD(NOW(), INTERVAL 12 HOUR), 1, 1, NOW(), NOW(), 0, NULL), -- Expiring soon
('Legacy Shipment', 800.00, '2024-12-01', '2024-12-01', '2024-12-02', '2024-12-05', 0, 0, NOW(), NOW(), 0, NULL), -- Legacy (>7 days old)
('Overdue Delivery', 1200.00, '2024-12-20', '2024-12-20', NULL, '2024-12-25', 1, 1, NOW(), NOW(), 0, NULL), -- Very overdue
('Near Expiry Ltd.', 900.00, '2025-01-11', '2025-01-11', NULL, DATE_ADD(NOW(), INTERVAL 20 HOUR), 1, 0, NOW(), NOW(), 0, NULL), -- Expiring soon

-- More normal data
('Regular Customer F', 1800.00, '2025-01-05', '2025-01-05', '2025-01-06', '2025-01-10', 0, 1, NOW(), NOW(), 0, NULL),
('Regular Customer G', 2200.00, '2025-01-04', '2025-01-04', '2025-01-05', '2025-01-09', 0, 0, NOW(), NOW(), 0, NULL),
('Regular Customer H', 650.00, '2025-01-03', '2025-01-03', '2025-01-04', '2025-01-08', 0, 1, NOW(), NOW(), 0, NULL),
('Regular Customer I', 4000.00, '2025-01-02', '2025-01-02', '2025-01-03', '2025-01-07', 0, 0, NOW(), NOW(), 0, NULL),
('Regular Customer J', 1100.00, '2025-01-01', '2025-01-01', '2025-01-02', '2025-01-06', 0, 1, NOW(), NOW(), 0, NULL);

-- Display summary
SELECT 'Data Inserted Successfully!' as Status;
SELECT 'Invoices' as Table_Name, COUNT(*) as Total, 
       SUM(CASE WHEN TotalAmount < 0 OR (TotalTax / NULLIF(TotalAmount, 0)) > 0.5 OR IssueDate > CURDATE() THEN 1 ELSE 0 END) as With_Anomalies
FROM Invoices
UNION ALL
SELECT 'Waybills', COUNT(*),
       SUM(CASE WHEN (DueDate < NOW() AND DeliveryDate IS NULL) OR 
                     (DueDate BETWEEN NOW() AND DATE_ADD(NOW(), INTERVAL 24 HOUR)) OR 
                     DATEDIFF(NOW(), IssueDate) > 7 THEN 1 ELSE 0 END)
FROM Waybills;