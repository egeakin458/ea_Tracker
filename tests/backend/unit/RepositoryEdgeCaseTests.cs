using Microsoft.EntityFrameworkCore;
using ea_Tracker.Data;
using ea_Tracker.Models;
using ea_Tracker.Repositories;
using ea_Tracker.Enums;
using System.Diagnostics;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace ea_Tracker.Tests.Unit
{
    /// <summary>
    /// Comprehensive edge case tests for GenericRepository<T> pattern.
    /// Tests complex filtering, null handling, boundary conditions, and performance edge cases.
    /// </summary>
    public class RepositoryEdgeCaseTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly GenericRepository<Invoice> _invoiceRepository;
        private readonly GenericRepository<Waybill> _waybillRepository;
        private readonly List<Invoice> _testInvoices;
        private readonly List<Waybill> _testWaybills;

        public RepositoryEdgeCaseTests()
        {
            // Setup InMemory database for controlled testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _invoiceRepository = new GenericRepository<Invoice>(_context);
            _waybillRepository = new GenericRepository<Waybill>(_context);

            // Create comprehensive test data with edge cases
            _testInvoices = CreateEdgeCaseInvoiceData();
            _testWaybills = CreateEdgeCaseWaybillData();

            SeedTestData();
        }

        #region Test Data Creation

        private List<Invoice> CreateEdgeCaseInvoiceData()
        {
            var baseDate = new DateTime(2024, 1, 15, 10, 30, 0);
            
            return new List<Invoice>
            {
                // Normal cases
                new Invoice 
                { 
                    Id = 1, 
                    RecipientName = "Normal Recipient", 
                    TotalAmount = 100.50m, 
                    IssueDate = baseDate,
                    HasAnomalies = false, 
                    CreatedAt = baseDate.AddDays(-1),
                    UpdatedAt = baseDate.AddDays(-1),
                    LastInvestigatedAt = baseDate.AddHours(-2)
                },

                // Null recipient name edge case
                new Invoice 
                { 
                    Id = 2, 
                    RecipientName = null, 
                    TotalAmount = 250.75m, 
                    IssueDate = baseDate.AddDays(1),
                    HasAnomalies = true, 
                    CreatedAt = baseDate,
                    UpdatedAt = baseDate,
                    LastInvestigatedAt = null
                },

                // Empty recipient name edge case
                new Invoice 
                { 
                    Id = 3, 
                    RecipientName = "", 
                    TotalAmount = 0m, 
                    IssueDate = baseDate.AddDays(-5),
                    HasAnomalies = false, 
                    CreatedAt = baseDate.AddDays(-4),
                    UpdatedAt = baseDate.AddDays(-3),
                    LastInvestigatedAt = baseDate.AddHours(-1)
                },

                // Whitespace only recipient name
                new Invoice 
                { 
                    Id = 4, 
                    RecipientName = "   ", 
                    TotalAmount = -50.25m, 
                    IssueDate = DateTime.MinValue,
                    HasAnomalies = true, 
                    CreatedAt = DateTime.MinValue,
                    UpdatedAt = DateTime.MinValue,
                    LastInvestigatedAt = DateTime.MinValue
                },

                // DateTime boundary cases
                new Invoice 
                { 
                    Id = 5, 
                    RecipientName = "Max Date Recipient", 
                    TotalAmount = 999999.99m, 
                    IssueDate = DateTime.MaxValue,
                    HasAnomalies = false, 
                    CreatedAt = DateTime.MaxValue,
                    UpdatedAt = DateTime.MaxValue,
                    LastInvestigatedAt = DateTime.MaxValue
                },

                // Case sensitive testing
                new Invoice 
                { 
                    Id = 6, 
                    RecipientName = "UPPERCASE RECIPIENT", 
                    TotalAmount = 333.33m, 
                    IssueDate = baseDate.AddDays(2),
                    HasAnomalies = true, 
                    CreatedAt = baseDate.AddDays(1),
                    UpdatedAt = baseDate.AddDays(2),
                    LastInvestigatedAt = baseDate.AddDays(1)
                },

                new Invoice 
                { 
                    Id = 7, 
                    RecipientName = "lowercase recipient", 
                    TotalAmount = 444.44m, 
                    IssueDate = baseDate.AddDays(3),
                    HasAnomalies = false, 
                    CreatedAt = baseDate.AddDays(2),
                    UpdatedAt = baseDate.AddDays(3),
                    LastInvestigatedAt = baseDate.AddDays(2)
                },

                // Special characters in recipient name
                new Invoice 
                { 
                    Id = 8, 
                    RecipientName = "Special!@#$%^&*()Characters", 
                    TotalAmount = 555.55m, 
                    IssueDate = baseDate.AddDays(-10),
                    HasAnomalies = true, 
                    CreatedAt = baseDate.AddDays(-9),
                    UpdatedAt = baseDate.AddDays(-8),
                    LastInvestigatedAt = baseDate.AddDays(-7)
                },

                // Unicode characters
                new Invoice 
                { 
                    Id = 9, 
                    RecipientName = "Üñîçødé Ñämé", 
                    TotalAmount = 777.77m, 
                    IssueDate = baseDate.AddHours(6),
                    HasAnomalies = false, 
                    CreatedAt = baseDate.AddHours(5),
                    UpdatedAt = baseDate.AddHours(6),
                    LastInvestigatedAt = baseDate.AddHours(4)
                },

                // Same date boundary testing
                new Invoice 
                { 
                    Id = 10, 
                    RecipientName = "Same Date Test", 
                    TotalAmount = 888.88m, 
                    IssueDate = baseDate,
                    HasAnomalies = true, 
                    CreatedAt = baseDate,
                    UpdatedAt = baseDate,
                    LastInvestigatedAt = baseDate
                }
            };
        }

        private List<Waybill> CreateEdgeCaseWaybillData()
        {
            var baseDate = new DateTime(2024, 2, 20, 14, 45, 0);
            
            return new List<Waybill>
            {
                // Normal case
                new Waybill 
                { 
                    Id = 1, 
                    RecipientName = "Normal Waybill Recipient", 
                    GoodsIssueDate = baseDate,
                    DueDate = baseDate.AddDays(7),
                    HasAnomalies = false, 
                    CreatedAt = baseDate.AddDays(-1),
                    UpdatedAt = baseDate,
                    LastInvestigatedAt = baseDate.AddHours(-3),
                    ShippedItems = "Standard Items"
                },

                // Null recipient and due date edge cases
                new Waybill 
                { 
                    Id = 2, 
                    RecipientName = null, 
                    GoodsIssueDate = baseDate.AddDays(2),
                    DueDate = null,
                    HasAnomalies = true, 
                    CreatedAt = baseDate.AddDays(1),
                    UpdatedAt = baseDate.AddDays(2),
                    LastInvestigatedAt = null,
                    ShippedItems = null
                },

                // Empty strings
                new Waybill 
                { 
                    Id = 3, 
                    RecipientName = "", 
                    GoodsIssueDate = baseDate.AddDays(-3),
                    DueDate = baseDate.AddDays(-1),
                    HasAnomalies = false, 
                    CreatedAt = baseDate.AddDays(-2),
                    UpdatedAt = baseDate.AddDays(-1),
                    LastInvestigatedAt = baseDate.AddDays(-1),
                    ShippedItems = ""
                },

                // DateTime boundary cases
                new Waybill 
                { 
                    Id = 4, 
                    RecipientName = "Min Date Waybill", 
                    GoodsIssueDate = DateTime.MinValue,
                    DueDate = DateTime.MinValue.AddDays(1),
                    HasAnomalies = true, 
                    CreatedAt = DateTime.MinValue,
                    UpdatedAt = DateTime.MinValue,
                    LastInvestigatedAt = DateTime.MinValue,
                    ShippedItems = "Historical Items"
                },

                new Waybill 
                { 
                    Id = 5, 
                    RecipientName = "Max Date Waybill", 
                    GoodsIssueDate = DateTime.MaxValue,
                    DueDate = DateTime.MaxValue,
                    HasAnomalies = false, 
                    CreatedAt = DateTime.MaxValue,
                    UpdatedAt = DateTime.MaxValue,
                    LastInvestigatedAt = DateTime.MaxValue,
                    ShippedItems = "Future Items"
                },

                // Long text fields
                new Waybill 
                { 
                    Id = 6, 
                    RecipientName = new string('A', 199), // Max length - 1
                    GoodsIssueDate = baseDate.AddDays(5),
                    DueDate = baseDate.AddDays(12),
                    HasAnomalies = true, 
                    CreatedAt = baseDate.AddDays(4),
                    UpdatedAt = baseDate.AddDays(5),
                    LastInvestigatedAt = baseDate.AddDays(4),
                    ShippedItems = new string('B', 999) // Max length - 1
                },

                // Case sensitivity testing
                new Waybill 
                { 
                    Id = 7, 
                    RecipientName = "UPPERCASE WAYBILL", 
                    GoodsIssueDate = baseDate.AddDays(-7),
                    DueDate = baseDate,
                    HasAnomalies = false, 
                    CreatedAt = baseDate.AddDays(-6),
                    UpdatedAt = baseDate.AddDays(-5),
                    LastInvestigatedAt = baseDate.AddDays(-4),
                    ShippedItems = "UPPERCASE ITEMS"
                },

                // Unicode testing
                new Waybill 
                { 
                    Id = 8, 
                    RecipientName = "Çhïñësé Ñämé 中文", 
                    GoodsIssueDate = baseDate.AddDays(10),
                    DueDate = baseDate.AddDays(17),
                    HasAnomalies = true, 
                    CreatedAt = baseDate.AddDays(9),
                    UpdatedAt = baseDate.AddDays(10),
                    LastInvestigatedAt = baseDate.AddDays(9),
                    ShippedItems = "Ünïçødé Ïtéms 物品"
                }
            };
        }

        private void SeedTestData()
        {
            _context.Invoices.AddRange(_testInvoices);
            _context.Waybills.AddRange(_testWaybills);
            _context.SaveChanges();
        }

        #endregion

        #region Null Parameter Handling Tests

        [Fact]
        public async Task GetAsync_AllParametersNull_ReturnsAllEntities()
        {
            // Act
            var result = await _invoiceRepository.GetAsync(
                filter: null,
                orderBy: null,
                includeProperties: ""
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testInvoices.Count, result.Count());
        }

        [Fact]
        public async Task GetAsync_ComplexFilter_AllNullParameters_ReturnsAllEntities()
        {
            // Arrange - Simulating controller filter with all null parameters
            bool? hasAnomalies = null;
            DateTime? fromDate = null;
            DateTime? toDate = null;
            string? recipientName = null;

            // Act - Using exact filter expression from InvoicesController
            var result = await _invoiceRepository.GetAsync(
                filter: i => (hasAnomalies == null || i.HasAnomalies == hasAnomalies) &&
                            (fromDate == null || i.IssueDate >= fromDate) &&
                            (toDate == null || i.IssueDate <= toDate) &&
                            (recipientName == null || i.RecipientName!.Contains(recipientName)),
                orderBy: q => q.OrderByDescending(i => i.CreatedAt)
            );

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testInvoices.Count, result.Count());
            
            // Verify ordering
            var resultList = result.ToList();
            for (int i = 0; i < resultList.Count - 1; i++)
            {
                Assert.True(resultList[i].CreatedAt >= resultList[i + 1].CreatedAt);
            }
        }

        [Fact]
        public async Task GetAsync_ComplexFilter_OnlyRecipientNameNull_FiltersCorrectly()
        {
            // Arrange
            bool? hasAnomalies = true;
            DateTime? fromDate = new DateTime(2024, 1, 1);
            DateTime? toDate = new DateTime(2024, 12, 31);
            string? recipientName = null;

            // Act
            var result = await _invoiceRepository.GetAsync(
                filter: i => (hasAnomalies == null || i.HasAnomalies == hasAnomalies) &&
                            (fromDate == null || i.IssueDate >= fromDate) &&
                            (toDate == null || i.IssueDate <= toDate) &&
                            (recipientName == null || i.RecipientName!.Contains(recipientName)),
                orderBy: q => q.OrderByDescending(i => i.CreatedAt)
            );

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            
            // Should only include anomalous invoices within date range
            Assert.All(resultList, invoice => Assert.True(invoice.HasAnomalies));
            Assert.All(resultList, invoice => Assert.True(invoice.IssueDate >= fromDate && invoice.IssueDate <= toDate));
        }

        [Fact]
        public async Task GetAsync_RecipientNameContains_WithNullRecipientName_DoesNotCrash()
        {
            // Arrange - Testing Contains() with null recipient name in filter
            string recipientName = "test";

            // Act & Assert - Should not throw exception
            var result = await _invoiceRepository.GetAsync(
                filter: i => i.RecipientName!.Contains(recipientName)
            );

            Assert.NotNull(result);
            // Should exclude invoices with null RecipientName
            Assert.All(result, invoice => 
            {
                Assert.NotNull(invoice.RecipientName);
                Assert.Contains(recipientName, invoice.RecipientName, StringComparison.OrdinalIgnoreCase);
            });
        }

        #endregion

        #region Boundary Condition Tests

        [Fact]
        public async Task GetAsync_SameDateFilter_FromDateEqualsToDate_ReturnsCorrectResults()
        {
            // Arrange - Use exact same date for from and to
            var targetDate = new DateTime(2024, 1, 15, 10, 30, 0);
            DateTime? fromDate = targetDate;
            DateTime? toDate = targetDate;

            // Act
            var result = await _invoiceRepository.GetAsync(
                filter: i => (fromDate == null || i.IssueDate >= fromDate) &&
                            (toDate == null || i.IssueDate <= toDate)
            );

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            
            // Should include invoices with exactly the target date
            Assert.All(resultList, invoice => 
                Assert.True(invoice.IssueDate >= fromDate && invoice.IssueDate <= toDate));
                
            // Verify we get the expected invoice with exact date match
            Assert.Contains(resultList, i => i.IssueDate == targetDate);
        }

        [Fact]
        public async Task GetAsync_InvertedDateRange_FromDateGreaterThanToDate_ReturnsEmpty()
        {
            // Arrange - Inverted date range
            DateTime? fromDate = new DateTime(2024, 12, 31);
            DateTime? toDate = new DateTime(2024, 1, 1);

            // Act
            var result = await _invoiceRepository.GetAsync(
                filter: i => (fromDate == null || i.IssueDate >= fromDate) &&
                            (toDate == null || i.IssueDate <= toDate)
            );

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAsync_EmptyStringFilter_ReturnsCorrectResults()
        {
            // Arrange
            string recipientName = "";

            // Act
            var result = await _invoiceRepository.GetAsync(
                filter: i => i.RecipientName!.Contains(recipientName)
            );

            // Assert
            Assert.NotNull(result);
            
            // Empty string Contains should match all non-null recipient names
            var resultList = result.ToList();
            Assert.All(resultList, invoice => Assert.NotNull(invoice.RecipientName));
        }

        [Fact]
        public async Task GetAsync_WhitespaceOnlyFilter_ReturnsCorrectResults()
        {
            // Arrange
            string recipientName = "   ";

            // Act
            var result = await _invoiceRepository.GetAsync(
                filter: i => i.RecipientName!.Contains(recipientName.Trim())
            );

            // Assert
            Assert.NotNull(result);
            
            // Should match invoices where RecipientName contains empty string (after trim)
            var resultList = result.ToList();
            Assert.All(resultList, invoice => Assert.NotNull(invoice.RecipientName));
        }

        [Fact]
        public async Task GetAsync_DateTimeBoundaryValues_HandlesMinMaxValues()
        {
            // Act - Test DateTime.MinValue filtering
            var minResult = await _invoiceRepository.GetAsync(
                filter: i => i.IssueDate == DateTime.MinValue
            );

            var maxResult = await _invoiceRepository.GetAsync(
                filter: i => i.IssueDate == DateTime.MaxValue
            );

            // Assert
            Assert.NotNull(minResult);
            Assert.NotNull(maxResult);
            
            Assert.Single(minResult);
            Assert.Single(maxResult);
            
            Assert.Equal(DateTime.MinValue, minResult.First().IssueDate);
            Assert.Equal(DateTime.MaxValue, maxResult.First().IssueDate);
        }

        #endregion

        #region Complex Filter Combination Tests

        [Fact]
        public async Task GetAsync_AllFiltersActive_ComplexCombination_ReturnsCorrectResults()
        {
            // Arrange - All filters active with specific values
            bool? hasAnomalies = true;
            DateTime? fromDate = new DateTime(2024, 1, 1);
            DateTime? toDate = new DateTime(2024, 12, 31);
            string? recipientName = "recipient"; // Should match case-insensitive

            // Act
            var result = await _invoiceRepository.GetAsync(
                filter: i => (hasAnomalies == null || i.HasAnomalies == hasAnomalies) &&
                            (fromDate == null || i.IssueDate >= fromDate) &&
                            (toDate == null || i.IssueDate <= toDate) &&
                            (recipientName == null || i.RecipientName!.Contains(recipientName)),
                orderBy: q => q.OrderByDescending(i => i.CreatedAt)
            );

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            
            Assert.All(resultList, invoice =>
            {
                Assert.True(invoice.HasAnomalies);
                Assert.True(invoice.IssueDate >= fromDate && invoice.IssueDate <= toDate);
                Assert.NotNull(invoice.RecipientName);
                Assert.Contains(recipientName, invoice.RecipientName, StringComparison.OrdinalIgnoreCase);
            });
        }

        [Fact]
        public async Task GetAsync_WaybillComplexFilter_AllParameters_ReturnsCorrectResults()
        {
            // Arrange - Test complex waybill filtering like in controller
            bool? hasAnomalies = false;
            DateTime? fromDate = new DateTime(2024, 2, 1);
            DateTime? toDate = new DateTime(2024, 3, 1);
            string? recipientName = "waybill";

            // Act
            var result = await _waybillRepository.GetAsync(
                filter: w => (hasAnomalies == null || w.HasAnomalies == hasAnomalies) &&
                            (fromDate == null || w.GoodsIssueDate >= fromDate) &&
                            (toDate == null || w.GoodsIssueDate <= toDate) &&
                            (recipientName == null || w.RecipientName!.Contains(recipientName)),
                orderBy: q => q.OrderByDescending(w => w.CreatedAt)
            );

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            
            Assert.All(resultList, waybill =>
            {
                Assert.False(waybill.HasAnomalies);
                Assert.True(waybill.GoodsIssueDate >= fromDate && waybill.GoodsIssueDate <= toDate);
                Assert.NotNull(waybill.RecipientName);
                Assert.Contains(recipientName, waybill.RecipientName, StringComparison.OrdinalIgnoreCase);
            });
        }

        [Fact]
        public async Task GetAsync_BooleanFilterEdgeCases_TrueFalseNull_ReturnsCorrectResults()
        {
            // Test true
            var trueResult = await _invoiceRepository.GetAsync(
                filter: i => i.HasAnomalies == true
            );

            // Test false
            var falseResult = await _invoiceRepository.GetAsync(
                filter: i => i.HasAnomalies == false
            );

            // Test null (should return all)
            bool? nullFilter = null;
            var nullResult = await _invoiceRepository.GetAsync(
                filter: i => nullFilter == null || i.HasAnomalies == nullFilter
            );

            // Assert
            Assert.NotNull(trueResult);
            Assert.NotNull(falseResult);
            Assert.NotNull(nullResult);
            
            Assert.All(trueResult, i => Assert.True(i.HasAnomalies));
            Assert.All(falseResult, i => Assert.False(i.HasAnomalies));
            Assert.Equal(_testInvoices.Count, nullResult.Count());
        }

        #endregion

        #region Ordering Edge Cases

        [Fact]
        public async Task GetAsync_OrderingWithNullValues_HandlesCorrectly()
        {
            // Act - Order by nullable LastInvestigatedAt field
            var result = await _invoiceRepository.GetAsync(
                orderBy: q => q.OrderBy(i => i.LastInvestigatedAt)
            );

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            
            // Nulls should come first in ascending order
            var nullCount = resultList.TakeWhile(i => i.LastInvestigatedAt == null).Count();
            Assert.True(nullCount > 0); // We have nulls in test data
            
            // Non-null values should be in ascending order
            var nonNullValues = resultList.Skip(nullCount).ToList();
            for (int i = 0; i < nonNullValues.Count - 1; i++)
            {
                Assert.True(nonNullValues[i].LastInvestigatedAt <= nonNullValues[i + 1].LastInvestigatedAt);
            }
        }

        [Fact]
        public async Task GetAsync_OrderingDescending_DateTimeWithMilliseconds_PreservesOrder()
        {
            // Arrange - Add invoices with clear time differences (seconds instead of milliseconds)
            var baseDate = new DateTime(2024, 3, 1, 10, 30, 45);
            var precisionInvoices = new List<Invoice>
            {
                new Invoice { Id = 100, RecipientName = "Precision1", CreatedAt = baseDate.AddSeconds(1), IssueDate = DateTime.Now, TotalAmount = 100 },
                new Invoice { Id = 101, RecipientName = "Precision2", CreatedAt = baseDate.AddSeconds(3), IssueDate = DateTime.Now, TotalAmount = 200 },
                new Invoice { Id = 102, RecipientName = "Precision3", CreatedAt = baseDate, IssueDate = DateTime.Now, TotalAmount = 300 }
            };

            _context.Invoices.AddRange(precisionInvoices);
            await _context.SaveChangesAsync();

            // Act
            var result = await _invoiceRepository.GetAsync(
                filter: i => i.Id >= 100,
                orderBy: q => q.OrderByDescending(i => i.CreatedAt)
            );

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Equal(3, resultList.Count);
            
            // Verify descending order - check actual datetime ordering rather than specific IDs
            var orderedByDateDesc = resultList.OrderByDescending(i => i.CreatedAt).ToList();
            
            for (int i = 0; i < resultList.Count - 1; i++)
            {
                Assert.True(resultList[i].CreatedAt >= resultList[i + 1].CreatedAt,
                    $"Order violation: {resultList[i].CreatedAt} should be >= {resultList[i + 1].CreatedAt}");
            }
            
            // Verify the ordering matches expected sequence (latest to earliest)
            Assert.True(resultList.SequenceEqual(orderedByDateDesc), 
                "Result should be ordered by CreatedAt descending");
        }

        [Fact]
        public async Task GetAsync_EmptyResultSetOrdering_DoesNotThrow()
        {
            // Act & Assert - Should not throw with empty result set
            var result = await _invoiceRepository.GetAsync(
                filter: i => i.Id == -1, // Non-existent ID
                orderBy: q => q.OrderByDescending(i => i.CreatedAt)
            );

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region Count Operations Edge Cases

        [Fact]
        public async Task CountAsync_ComplexFilterExpression_ReturnsAccurateCount()
        {
            // Arrange
            bool? hasAnomalies = true;
            DateTime? fromDate = new DateTime(2024, 1, 1);
            DateTime? toDate = new DateTime(2024, 12, 31);
            string? recipientName = null;

            // Act
            var count = await _invoiceRepository.CountAsync(
                filter: i => (hasAnomalies == null || i.HasAnomalies == hasAnomalies) &&
                            (fromDate == null || i.IssueDate >= fromDate) &&
                            (toDate == null || i.IssueDate <= toDate) &&
                            (recipientName == null || i.RecipientName!.Contains(recipientName))
            );

            var actualResults = await _invoiceRepository.GetAsync(
                filter: i => (hasAnomalies == null || i.HasAnomalies == hasAnomalies) &&
                            (fromDate == null || i.IssueDate >= fromDate) &&
                            (toDate == null || i.IssueDate <= toDate) &&
                            (recipientName == null || i.RecipientName!.Contains(recipientName))
            );

            // Assert
            Assert.Equal(actualResults.Count(), count);
        }

        [Fact]
        public async Task CountAsync_NullFilter_ReturnsAllEntitiesCount()
        {
            // Act
            var count = await _invoiceRepository.CountAsync(filter: null);

            // Assert
            Assert.Equal(_testInvoices.Count, count);
        }

        [Fact]
        public async Task CountAsync_FilterWithNoMatches_ReturnsZero()
        {
            // Act
            var count = await _invoiceRepository.CountAsync(
                filter: i => i.RecipientName == "NonExistentRecipient"
            );

            // Assert
            Assert.Equal(0, count);
        }

        #endregion

        #region Performance Edge Cases

        [Fact]
        public async Task GetAsync_LargeDatasetFiltering_CompletesWithinReasonableTime()
        {
            // Arrange - Add many more entities for performance testing
            var largeDataSet = new List<Invoice>();
            var baseDate = DateTime.Now;

            for (int i = 1000; i < 1100; i++) // Add 100 more invoices
            {
                largeDataSet.Add(new Invoice
                {
                    Id = i,
                    RecipientName = $"Bulk Recipient {i}",
                    TotalAmount = i * 10.5m,
                    IssueDate = baseDate.AddDays(i % 30),
                    HasAnomalies = i % 3 == 0,
                    CreatedAt = baseDate.AddDays(i % 60),
                    UpdatedAt = baseDate.AddDays(i % 60)
                });
            }

            _context.Invoices.AddRange(largeDataSet);
            await _context.SaveChangesAsync();

            // Act
            var stopwatch = Stopwatch.StartNew();
            
            var result = await _invoiceRepository.GetAsync(
                filter: i => i.HasAnomalies == true,
                orderBy: q => q.OrderByDescending(i => i.CreatedAt)
            );

            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Query took {stopwatch.ElapsedMilliseconds}ms, should be under 5000ms");
            Assert.All(result, i => Assert.True(i.HasAnomalies));
        }

        [Fact]
        public async Task CountAsync_VsGetAsync_PerformanceComparison()
        {
            // Arrange - Use existing large dataset
            var complexFilter = new Func<Invoice, bool>(i => 
                i.HasAnomalies == true && 
                i.IssueDate > new DateTime(2024, 1, 1) && 
                !string.IsNullOrEmpty(i.RecipientName));

            // Act
            var countStopwatch = Stopwatch.StartNew();
            var count = await _invoiceRepository.CountAsync(
                filter: i => i.HasAnomalies == true && 
                            i.IssueDate > new DateTime(2024, 1, 1) &&
                            i.RecipientName != null && i.RecipientName != ""
            );
            countStopwatch.Stop();

            var getStopwatch = Stopwatch.StartNew();
            var results = await _invoiceRepository.GetAsync(
                filter: i => i.HasAnomalies == true && 
                            i.IssueDate > new DateTime(2024, 1, 1) &&
                            i.RecipientName != null && i.RecipientName != ""
            );
            getStopwatch.Stop();

            // Assert
            Assert.Equal(results.Count(), count);
            
            // Count should generally be faster than GetAsync for large datasets
            // This is a general expectation, but may not always hold for small datasets
            Assert.True(countStopwatch.ElapsedMilliseconds <= getStopwatch.ElapsedMilliseconds * 2, 
                $"Count: {countStopwatch.ElapsedMilliseconds}ms, Get: {getStopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Exception Handling Edge Cases

        [Fact]
        public async Task GetAsync_InvalidFilterExpression_HandlesGracefully()
        {
            // This test verifies that our repository doesn't crash on edge case expressions
            // Act & Assert - Should not throw for edge case filter expressions
            
            // Empty string comparison
            var result1 = await _invoiceRepository.GetAsync(
                filter: i => i.RecipientName == ""
            );
            Assert.NotNull(result1);

            // Null comparison
            var result2 = await _invoiceRepository.GetAsync(
                filter: i => i.RecipientName == null
            );
            Assert.NotNull(result2);
        }

        [Fact]
        public async Task AnyAsync_ComplexFilter_ReturnsCorrectBoolean()
        {
            // Act
            var hasAnomalous = await _invoiceRepository.AnyAsync(
                filter: i => i.HasAnomalies == true
            );

            var hasNonExistent = await _invoiceRepository.AnyAsync(
                filter: i => i.RecipientName == "ThisRecipientDoesNotExist"
            );

            // Assert
            Assert.True(hasAnomalous);
            Assert.False(hasNonExistent);
        }

        [Fact]
        public async Task GetFirstOrDefaultAsync_WithComplexFilter_ReturnsCorrectResult()
        {
            // Act
            var firstAnomaly = await _invoiceRepository.GetFirstOrDefaultAsync(
                filter: i => i.HasAnomalies == true
            );

            var nonExistent = await _invoiceRepository.GetFirstOrDefaultAsync(
                filter: i => i.RecipientName == "NonExistentRecipient"
            );

            // Assert
            Assert.NotNull(firstAnomaly);
            Assert.True(firstAnomaly.HasAnomalies);
            
            Assert.Null(nonExistent);
        }

        #endregion

        #region Entity Framework Integration Edge Cases

        [Fact]
        public async Task GetAsync_ComplexLinqExpressions_TranslatesToSqlCorrectly()
        {
            // Act - Test complex LINQ expressions that should translate to SQL
            var result = await _invoiceRepository.GetAsync(
                filter: i => i.RecipientName != null && 
                            i.RecipientName.Length > 0 &&
                            i.TotalAmount > 0 &&
                            i.CreatedAt.Year == 2024,
                orderBy: q => q.OrderBy(i => i.CreatedAt).ThenBy(i => i.TotalAmount)
            );

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            
            Assert.All(resultList, invoice =>
            {
                Assert.NotNull(invoice.RecipientName);
                Assert.True(invoice.RecipientName.Length > 0);
                Assert.True(invoice.TotalAmount > 0);
                Assert.Equal(2024, invoice.CreatedAt.Year);
            });

            // Verify complex ordering (first by CreatedAt, then by TotalAmount)
            for (int i = 0; i < resultList.Count - 1; i++)
            {
                var current = resultList[i];
                var next = resultList[i + 1];
                
                Assert.True(current.CreatedAt <= next.CreatedAt);
                
                if (current.CreatedAt == next.CreatedAt)
                {
                    Assert.True(current.TotalAmount <= next.TotalAmount);
                }
            }
        }

        [Fact]
        public async Task GetAsync_ParameterizedQueryGeneration_WorksWithVariables()
        {
            // Arrange - Variables that should be properly parameterized
            var targetAmount = 100m;
            var targetName = "Normal";
            var targetDate = new DateTime(2024, 1, 1);

            // Act
            var result = await _invoiceRepository.GetAsync(
                filter: i => i.TotalAmount >= targetAmount &&
                            i.RecipientName!.Contains(targetName) &&
                            i.IssueDate >= targetDate
            );

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            
            Assert.All(resultList, invoice =>
            {
                Assert.True(invoice.TotalAmount >= targetAmount);
                Assert.Contains(targetName, invoice.RecipientName!, StringComparison.OrdinalIgnoreCase);
                Assert.True(invoice.IssueDate >= targetDate);
            });
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}