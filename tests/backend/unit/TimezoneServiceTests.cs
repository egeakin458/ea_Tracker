using System;
using Xunit;
using ea_Tracker.Services;

namespace ea_Tracker.Tests.Unit
{
    /// <summary>
    /// Tests for TimezoneService to ensure consistent Turkey timezone conversion.
    /// </summary>
    public class TimezoneServiceTests
    {
        [Fact]
        public void ConvertUtcToTurkeyTime_ConvertsCorrectly_StandardTime()
        {
            // Arrange - UTC time (winter time, UTC+3)
            var utcTime = new DateTime(2025, 1, 15, 1, 8, 42, DateTimeKind.Utc);
            
            // Act
            var turkeyTime = TimezoneService.ConvertUtcToTurkeyTime(utcTime);
            
            // Assert - Should be 3 hours ahead
            Assert.Equal(4, turkeyTime.Hour);
            Assert.Equal(8, turkeyTime.Minute);
            Assert.Equal(42, turkeyTime.Second);
        }

        [Fact]
        public void ConvertUtcToTurkeyTime_ConvertsCorrectly_DaylightSavingTime()
        {
            // Arrange - UTC time during daylight saving (summer time, UTC+3)
            var utcTime = new DateTime(2025, 8, 11, 1, 8, 42, DateTimeKind.Utc);
            
            // Act
            var turkeyTime = TimezoneService.ConvertUtcToTurkeyTime(utcTime);
            
            // Assert - Should be 3 hours ahead (Turkey doesn't observe DST anymore)
            Assert.Equal(4, turkeyTime.Hour);
            Assert.Equal(8, turkeyTime.Minute);
            Assert.Equal(42, turkeyTime.Second);
        }

        [Fact]
        public void ConvertUtcToTurkeyTime_HandlesUnspecifiedKind()
        {
            // Arrange - DateTime with Unspecified kind (should be treated as UTC)
            var unspecifiedTime = new DateTime(2025, 8, 11, 1, 8, 42, DateTimeKind.Unspecified);
            
            // Act
            var turkeyTime = TimezoneService.ConvertUtcToTurkeyTime(unspecifiedTime);
            
            // Assert
            Assert.Equal(4, turkeyTime.Hour);
            Assert.Equal(8, turkeyTime.Minute);
            Assert.Equal(42, turkeyTime.Second);
        }

        [Fact]
        public void GetTurkeyDateTimeOffset_ReturnsCorrectOffset()
        {
            // Act
            var turkeyDateTime = TimezoneService.GetTurkeyDateTimeOffset();
            
            // Assert
            Assert.Equal(TimeSpan.FromHours(3), turkeyDateTime.Offset); // Turkey is UTC+3
        }

        [Fact]
        public void GetTurkeyTime_ReturnsCurrentTurkeyTime()
        {
            // Arrange
            var utcNow = DateTime.UtcNow;
            
            // Act
            var turkeyTime = TimezoneService.GetTurkeyTime();
            
            // Assert - Should be approximately 3 hours ahead of UTC
            var expectedTurkeyTime = utcNow.AddHours(3);
            Assert.Equal(expectedTurkeyTime.Hour, turkeyTime.Hour);
        }

        [Fact]
        public void FormatAsTurkeyTime_FormatsCorrectly()
        {
            // Arrange
            var utcTime = new DateTime(2025, 8, 11, 1, 8, 42, DateTimeKind.Utc);
            
            // Act
            var formatted = TimezoneService.FormatAsTurkeyTime(utcTime);
            
            // Assert
            Assert.Equal("11.08.2025 04:08:42", formatted);
        }

        [Theory]
        [InlineData("2025-01-15T01:08:42Z", 4, 8, 42)] // Winter time
        [InlineData("2025-06-15T01:08:42Z", 4, 8, 42)] // Summer time
        [InlineData("2025-12-31T21:00:00Z", 0, 0, 0)] // New Year's Eve in Turkey
        public void ConvertUtcToTurkeyTime_VariousScenarios(string utcTimeString, int expectedHour, int expectedMinute, int expectedSecond)
        {
            // Arrange
            var utcTime = DateTime.Parse(utcTimeString).ToUniversalTime();
            
            // Act
            var turkeyTime = TimezoneService.ConvertUtcToTurkeyTime(utcTime);
            
            // Assert
            Assert.Equal(expectedHour, turkeyTime.Hour);
            Assert.Equal(expectedMinute, turkeyTime.Minute);
            Assert.Equal(expectedSecond, turkeyTime.Second);
        }
    }
}