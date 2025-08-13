-- Normal Waybills for Investigation Testing (Non-problematic)
-- This adds healthy waybills to balance the test data

USE ea_tracker_db;

-- Insert 26 normal waybills with future due dates and recent goods issue dates
INSERT INTO Waybills (RecipientName, GoodsIssueDate, ShippedItems, DueDate, WaybillType, CreatedAt, UpdatedAt, HasAnomalies, LastInvestigatedAt) VALUES
-- Recent shipments with reasonable future due dates (7-30 days out)
('Healthy Corp 1', '2025-08-12', 'Standard Electronics Package', '2025-08-25', 0, NOW(), NOW(), 0, NULL),
('Healthy Corp 2', '2025-08-11', 'Office Supplies Batch A', '2025-08-28', 1, NOW(), NOW(), 0, NULL),
('Healthy Corp 3', '2025-08-10', 'Computer Hardware Set', '2025-08-30', 0, NOW(), NOW(), 0, NULL),
('Healthy Corp 4', '2025-08-09', 'Industrial Tools Kit', '2025-09-01', 1, NOW(), NOW(), 0, NULL),
('Healthy Corp 5', '2025-08-08', 'Books and Documents', '2025-08-29', 0, NOW(), NOW(), 0, NULL),

-- Shipments from this week with good delivery windows
('Quick Delivery Ltd 1', '2025-08-13', 'Fresh Produce Package', '2025-08-20', 0, NOW(), NOW(), 0, NULL),
('Quick Delivery Ltd 2', '2025-08-12', 'Medical Equipment', '2025-08-26', 1, NOW(), NOW(), 0, NULL),
('Quick Delivery Ltd 3', '2025-08-11', 'Automotive Parts', '2025-08-27', 0, NOW(), NOW(), 0, NULL),
('Quick Delivery Ltd 4', '2025-08-10', 'Textile Products', '2025-08-31', 1, NOW(), NOW(), 0, NULL),
('Quick Delivery Ltd 5', '2025-08-09', 'Construction Materials', '2025-09-02', 0, NOW(), NOW(), 0, NULL),

-- Standard business deliveries with 2-3 week windows
('Standard Shipping A', '2025-08-08', 'Furniture Components', '2025-08-22', 0, NOW(), NOW(), 0, NULL),
('Standard Shipping B', '2025-08-07', 'Kitchen Appliances', '2025-08-24', 1, NOW(), NOW(), 0, NULL),
('Standard Shipping C', '2025-08-06', 'Garden Equipment', '2025-08-21', 0, NOW(), NOW(), 0, NULL),
('Standard Shipping D', '2025-08-05', 'Sports Equipment', '2025-08-23', 1, NOW(), NOW(), 0, NULL),
('Standard Shipping E', '2025-08-04', 'Educational Materials', '2025-08-25', 0, NOW(), NOW(), 0, NULL),

-- International/longer delivery windows
('Global Express 1', '2025-08-12', 'International Package A', '2025-09-05', 0, NOW(), NOW(), 0, NULL),
('Global Express 2', '2025-08-11', 'International Package B', '2025-09-08', 1, NOW(), NOW(), 0, NULL),
('Global Express 3', '2025-08-10', 'International Package C', '2025-09-10', 0, NOW(), NOW(), 0, NULL),
('Global Express 4', '2025-08-09', 'International Package D', '2025-09-12', 1, NOW(), NOW(), 0, NULL),
('Global Express 5', '2025-08-08', 'International Package E', '2025-09-15', 0, NOW(), NOW(), 0, NULL),

-- Premium delivery services with good timing
('Premium Logistics 1', '2025-08-13', 'High-Value Electronics', '2025-08-19', 0, NOW(), NOW(), 0, NULL),
('Premium Logistics 2', '2025-08-12', 'Pharmaceutical Products', '2025-08-18', 1, NOW(), NOW(), 0, NULL),
('Premium Logistics 3', '2025-08-11', 'Jewelry and Valuables', '2025-08-17', 0, NOW(), NOW(), 0, NULL),
('Premium Logistics 4', '2025-08-10', 'Art and Collectibles', '2025-08-16', 1, NOW(), NOW(), 0, NULL),
('Premium Logistics 5', '2025-08-09', 'Scientific Instruments', '2025-08-20', 0, NOW(), NOW(), 0, NULL),
('Premium Logistics 6', '2025-08-08', 'Precision Manufacturing Parts', '2025-08-22', 1, NOW(), NOW(), 0, NULL);

-- Verify insertion
SELECT 'Normal waybills added' as Status;
SELECT 
    COUNT(*) as Total_Normal_Waybills_Added,
    MIN(GoodsIssueDate) as Earliest_Issue_Date,
    MAX(DueDate) as Latest_Due_Date,
    COUNT(CASE WHEN DueDate > NOW() THEN 1 END) as Future_Due_Dates,
    COUNT(CASE WHEN DATEDIFF(NOW(), GoodsIssueDate) <= 7 THEN 1 END) as Recent_Shipments
FROM Waybills 
WHERE RecipientName LIKE 'Healthy Corp%' 
   OR RecipientName LIKE 'Quick Delivery%' 
   OR RecipientName LIKE 'Standard Shipping%' 
   OR RecipientName LIKE 'Global Express%' 
   OR RecipientName LIKE 'Premium Logistics%';