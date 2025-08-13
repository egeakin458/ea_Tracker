-- Anomalous and Normal Waybills for Investigation Testing
-- This bypasses service layer validation by inserting directly into database

USE ea_tracker_db;

-- Insert waybills with anomalies and normal ones
INSERT INTO Waybills (RecipientName, GoodsIssueDate, ShippedItems, DueDate, WaybillType, CreatedAt, UpdatedAt, HasAnomalies, LastInvestigatedAt) VALUES
-- ANOMALOUS: Overdue waybills (DueDate in the past)
('Late Delivery Co 1', '2025-01-01', 'Urgent Electronics', '2025-01-05', 0, NOW(), NOW(), 0, NULL),
('Late Delivery Co 2', '2024-12-20', 'Holiday Packages', '2024-12-25', 1, NOW(), NOW(), 0, NULL),
('Late Delivery Co 3', '2024-12-15', 'Medical Supplies', '2024-12-20', 0, NOW(), NOW(), 0, NULL),
('Late Delivery Co 4', '2025-01-08', 'Office Equipment', '2025-01-10', 1, NOW(), NOW(), 0, NULL),

-- ANOMALOUS: Expiring soon (DueDate within next 24 hours)
('Expiring Soon 1', '2025-01-12', 'Perishable Goods', DATE_ADD(NOW(), INTERVAL 12 HOUR), 0, NOW(), NOW(), 0, NULL),
('Expiring Soon 2', '2025-01-11', 'Fresh Produce', DATE_ADD(NOW(), INTERVAL 18 HOUR), 1, NOW(), NOW(), 0, NULL),
('Expiring Soon 3', '2025-01-10', 'Time-sensitive Items', DATE_ADD(NOW(), INTERVAL 6 HOUR), 0, NOW(), NOW(), 0, NULL),

-- ANOMALOUS: Legacy shipments (GoodsIssueDate > 7 days old)
('Legacy Shipping 1', '2025-01-01', 'Old Inventory Batch 1', '2025-02-01', 1, NOW(), NOW(), 0, NULL),
('Legacy Shipping 2', '2024-12-25', 'Old Inventory Batch 2', '2025-01-25', 0, NOW(), NOW(), 0, NULL),
('Legacy Shipping 3', '2024-12-20', 'Old Inventory Batch 3', '2025-01-20', 1, NOW(), NOW(), 0, NULL),

-- NORMAL: Good waybills (no anomalies)
('Normal Customer A', '2025-01-12', 'Standard Package A', '2025-01-20', 0, NOW(), NOW(), 0, NULL),
('Normal Customer B', '2025-01-11', 'Electronics Shipment', '2025-01-18', 1, NOW(), NOW(), 0, NULL),
('Normal Customer C', '2025-01-10', 'Books and Supplies', '2025-01-17', 0, NOW(), NOW(), 0, NULL),
('Normal Customer D', '2025-01-09', 'Furniture Set', '2025-01-16', 1, NOW(), NOW(), 0, NULL),
('Normal Customer E', '2025-01-08', 'Computer Hardware', '2025-01-15', 0, NOW(), NOW(), 0, NULL),
('Normal Customer F', '2025-01-07', 'Industrial Tools', '2025-01-14', 1, NOW(), NOW(), 0, NULL);

-- Verify insertion
SELECT 'Waybills with anomalies added' as Status;
SELECT 
    COUNT(*) as Total_Waybills,
    SUM(CASE WHEN DueDate < NOW() THEN 1 ELSE 0 END) as Overdue,
    SUM(CASE WHEN DueDate BETWEEN NOW() AND DATE_ADD(NOW(), INTERVAL 24 HOUR) THEN 1 ELSE 0 END) as Expiring_Soon,
    SUM(CASE WHEN DATEDIFF(NOW(), GoodsIssueDate) > 7 THEN 1 ELSE 0 END) as Legacy_Shipments
FROM Waybills;