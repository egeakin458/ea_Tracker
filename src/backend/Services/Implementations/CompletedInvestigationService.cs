using ea_Tracker.Data;
using ea_Tracker.Enums;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Repositories;
using ea_Tracker.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Text;

namespace ea_Tracker.Services.Implementations
{
    /// <summary>
    /// Service implementation for completed investigation operations.
    /// Refactored to use Repository Pattern for consistent data access across all services.
    /// </summary>
    public class CompletedInvestigationService : ICompletedInvestigationService
    {
        private readonly IGenericRepository<InvestigationExecution> _executionRepository;
        private readonly IGenericRepository<InvestigationResult> _resultRepository;
        private readonly IGenericRepository<InvestigatorInstance> _investigatorRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CompletedInvestigationService> _logger;

        public CompletedInvestigationService(
            IGenericRepository<InvestigationExecution> executionRepository,
            IGenericRepository<InvestigationResult> resultRepository,
            IGenericRepository<InvestigatorInstance> investigatorRepository,
            IMapper mapper,
            ILogger<CompletedInvestigationService> logger)
        {
            _executionRepository = executionRepository;
            _resultRepository = resultRepository;
            _investigatorRepository = investigatorRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // Constants for include properties to avoid magic strings
        private const string INCLUDE_INVESTIGATOR = "Investigator";

        public async Task<IEnumerable<CompletedInvestigationDto>> GetAllCompletedAsync()
        {
            _logger.LogDebug("Retrieving all completed investigations");

            // Get executions with includes using repository pattern
            var completedExecutions = await _executionRepository.GetAsync(
                filter: e => e.ResultCount > 0,
                orderBy: q => q.OrderByDescending(e => e.StartedAt),
                includeProperties: INCLUDE_INVESTIGATOR
            );

            // Process results with proper null checking
            var results = new List<CompletedInvestigationDto>();
            foreach (var execution in completedExecutions)
            {
                // Get anomaly count using result repository
                var anomalyCount = await _resultRepository.CountAsync(
                    r => r.ExecutionId == execution.Id && 
                         (r.Severity == ResultSeverity.Anomaly || 
                          r.Severity == ResultSeverity.Critical)
                );

                results.Add(new CompletedInvestigationDto(
                    ExecutionId: execution.Id,
                    InvestigatorId: execution.InvestigatorId,
                    InvestigatorName: execution.Investigator?.CustomName ?? "Investigation",
                    StartedAt: execution.StartedAt,
                    CompletedAt: execution.CompletedAt ?? execution.StartedAt,
                    Duration: CalculateDuration(execution.StartedAt, execution.CompletedAt ?? execution.StartedAt),
                    ResultCount: execution.ResultCount,
                    AnomalyCount: anomalyCount
                ));
            }

            _logger.LogInformation("Retrieved {Count} completed investigations", results.Count);
            return results;
        }

        public async Task<InvestigationDetailDto?> GetInvestigationDetailAsync(int executionId)
        {
            _logger.LogDebug("Retrieving investigation detail for execution {ExecutionId}", executionId);

            // Use repository to get execution with includes
            var executions = await _executionRepository.GetAsync(
                filter: e => e.Id == executionId,
                includeProperties: INCLUDE_INVESTIGATOR
            );
            
            var execution = executions.FirstOrDefault();
            if (execution == null)
            {
                _logger.LogWarning("Investigation execution {ExecutionId} not found", executionId);
                return null;
            }

            // Get anomaly count using repository
            var anomalyCount = await _resultRepository.CountAsync(
                r => r.ExecutionId == executionId && 
                     (r.Severity == ResultSeverity.Anomaly || 
                      r.Severity == ResultSeverity.Critical)
            );

            // Create summary DTO
            var summary = new CompletedInvestigationDto(
                ExecutionId: execution.Id,
                InvestigatorId: execution.InvestigatorId,
                InvestigatorName: execution.Investigator?.CustomName ?? "Investigation",
                StartedAt: execution.StartedAt,
                CompletedAt: execution.CompletedAt ?? execution.StartedAt,
                Duration: CalculateDuration(execution.StartedAt, execution.CompletedAt ?? execution.StartedAt),
                ResultCount: execution.ResultCount,
                AnomalyCount: anomalyCount
            );

            // Get detailed results using repository
            var results = await _resultRepository.GetAsync(
                filter: r => r.ExecutionId == executionId,
                orderBy: q => q.OrderBy(r => r.Timestamp)
            );

            // Take only first 100 results (maintaining existing behavior) and map to DTOs
            var detailedResults = results.Take(100).Select(r => new InvestigatorResultDto(
                execution.InvestigatorId,
                r.Timestamp,
                r.Message,
                r.Payload
            ));

            return new InvestigationDetailDto(summary, detailedResults);
        }

        public async Task<ClearInvestigationsResultDto> ClearAllCompletedInvestigationsAsync()
        {
            _logger.LogInformation("Clearing all completed investigations");

            // Get all results and executions for counting before deletion
            var allResults = await _resultRepository.GetAllAsync();
            var allExecutions = await _executionRepository.GetAllAsync();
            
            var resultsCount = allResults.Count();
            var executionsCount = allExecutions.Count();

            // Remove all results using repository pattern
            if (resultsCount > 0)
            {
                _resultRepository.RemoveRange(allResults);
                await _resultRepository.SaveChangesAsync();
            }

            // Remove all executions using repository pattern
            if (executionsCount > 0)
            {
                _executionRepository.RemoveRange(allExecutions);
                await _executionRepository.SaveChangesAsync();
            }

            _logger.LogInformation("Cleared {Results} results and {Executions} executions", 
                resultsCount, executionsCount);

            return new ClearInvestigationsResultDto(
                Message: "All investigation results cleared successfully",
                ResultsDeleted: resultsCount,
                ExecutionsDeleted: executionsCount
            );
        }

        public async Task<DeleteInvestigationResultDto> DeleteInvestigationExecutionAsync(int executionId)
        {
            _logger.LogInformation("Deleting investigation execution {ExecutionId}", executionId);

            // Get and remove related results using repository
            var results = await _resultRepository.GetAsync(
                filter: r => r.ExecutionId == executionId
            );
            
            var resultsCount = results.Count();
            if (resultsCount > 0)
            {
                _resultRepository.RemoveRange(results);
                await _resultRepository.SaveChangesAsync();
            }

            // Remove execution using repository
            var execution = await _executionRepository.GetByIdAsync(executionId);
            if (execution != null)
            {
                _executionRepository.Remove(execution);
                await _executionRepository.SaveChangesAsync();
            }

            _logger.LogInformation("Deleted execution {ExecutionId} with {Results} results", 
                executionId, resultsCount);

            return new DeleteInvestigationResultDto(
                Message: $"Investigation execution {executionId} deleted successfully",
                ResultsDeleted: resultsCount
            );
        }

        public async Task<InvestigationExportDto?> ExportInvestigationsAsync(BulkExportRequestDto request)
        {
            // Validate input
            if (request?.ExecutionIds == null || !request.ExecutionIds.Any())
            {
                _logger.LogWarning("Export request with empty execution IDs");
                return null;
            }

            // Validate format
            var validFormats = new[] { "json", "csv", "excel" };
            var format = request.Format.ToLowerInvariant();
            if (!validFormats.Contains(format))
            {
                throw new ArgumentException($"Invalid format '{request.Format}'. Supported formats: {string.Join(", ", validFormats)}");
            }

            _logger.LogInformation("Processing bulk export for {Count} investigations in {Format} format", 
                request.ExecutionIds.Count, format);

            // Fetch investigation details using repositories
            var investigations = new List<InvestigationDetailDto>();
            
            // Batch fetch executions for efficiency
            var executions = await _executionRepository.GetAsync(
                filter: e => request.ExecutionIds.Contains(e.Id),
                includeProperties: INCLUDE_INVESTIGATOR
            );

            foreach (var execution in executions)
            {
                // Get anomaly count
                var anomalyCount = await _resultRepository.CountAsync(
                    r => r.ExecutionId == execution.Id && 
                         (r.Severity == ResultSeverity.Anomaly || 
                          r.Severity == ResultSeverity.Critical)
                );

                // Create summary
                var summary = new CompletedInvestigationDto(
                    ExecutionId: execution.Id,
                    InvestigatorId: execution.InvestigatorId,
                    InvestigatorName: execution.Investigator?.CustomName ?? "Investigation",
                    StartedAt: execution.StartedAt,
                    CompletedAt: execution.CompletedAt ?? execution.StartedAt,
                    Duration: CalculateDuration(execution.StartedAt, execution.CompletedAt ?? execution.StartedAt),
                    ResultCount: execution.ResultCount,
                    AnomalyCount: anomalyCount
                );

                // Get detailed results
                var results = await _resultRepository.GetAsync(
                    filter: r => r.ExecutionId == execution.Id,
                    orderBy: q => q.OrderBy(r => r.Timestamp)
                );

                var detailedResults = results.Take(100).Select(r => new InvestigatorResultDto(
                    execution.InvestigatorId,
                    r.Timestamp,
                    r.Message,
                    r.Payload
                ));

                investigations.Add(new InvestigationDetailDto(summary, detailedResults));
            }

            if (!investigations.Any())
            {
                _logger.LogWarning("No valid investigations found for export");
                return null;
            }

            // Generate export based on format
            return format switch
            {
                "json" => await GenerateJsonExportAsync(investigations),
                "csv" => await GenerateCsvExportAsync(investigations),
                "excel" => await GenerateExcelExportAsync(investigations),
                _ => throw new ArgumentException($"Unsupported format: {request.Format}")
            };
        }

        private Task<InvestigationExportDto> GenerateJsonExportAsync(List<InvestigationDetailDto> investigations)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(investigations, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            
            var bytes = Encoding.UTF8.GetBytes(json);
            var fileName = $"investigations_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            
            _logger.LogInformation("Generated JSON export with {Count} investigations", investigations.Count);
            return Task.FromResult(new InvestigationExportDto(bytes, "application/json", fileName));
        }

        private Task<InvestigationExportDto> GenerateCsvExportAsync(List<InvestigationDetailDto> investigations)
        {
            var csv = new StringBuilder();
            
            // Add UTF-8 BOM for Excel compatibility
            csv.Append('\uFEFF');
            
            // Add header
            csv.AppendLine("ExecutionId,InvestigatorId,InvestigatorName,Timestamp,Message,Payload");
            
            // Add data rows
            foreach (var investigation in investigations)
            {
                foreach (var result in investigation.DetailedResults)
                {
                    csv.AppendLine($"{investigation.Summary.ExecutionId}," +
                                  $"{EscapeCsvField(result.InvestigatorId.ToString())}," +
                                  $"{EscapeCsvField(investigation.Summary.InvestigatorName)}," +
                                  $"{EscapeCsvField(result.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"))}," +
                                  $"{EscapeCsvField(result.Message)}," +
                                  $"{EscapeCsvField(result.Payload ?? "")}");
                }
            }
            
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"investigations_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            
            _logger.LogInformation("Generated CSV export with {Count} investigations", investigations.Count);
            return Task.FromResult(new InvestigationExportDto(bytes, "text/csv", fileName));
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";
            
            // Check if escaping is needed
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                // Escape quotes by doubling them and wrap in quotes
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            
            return field;
        }

        private Task<InvestigationExportDto> GenerateExcelExportAsync(List<InvestigationDetailDto> investigations)
        {
            using var workbook = new XLWorkbook();
            
            try
            {
                // Create Summary sheet
                var summarySheet = workbook.Worksheets.Add("Summary");
                CreateSummarySheet(summarySheet, investigations);
                
                // Create All Results sheet
                var resultsSheet = workbook.Worksheets.Add("All Results");
                CreateResultsSheet(resultsSheet, investigations);
                
                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                
                var fileName = $"investigations_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                
                _logger.LogInformation("Generated Excel export with {Count} investigations", investigations.Count);
                return Task.FromResult(new InvestigationExportDto(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel export");
                throw new InvalidOperationException("Failed to generate Excel export", ex);
            }
        }

        private void CreateSummarySheet(IXLWorksheet worksheet, List<InvestigationDetailDto> investigations)
        {
            // Headers
            worksheet.Cell(1, 1).Value = "Execution ID";
            worksheet.Cell(1, 2).Value = "Investigator ID";
            worksheet.Cell(1, 3).Value = "Investigator Name";
            worksheet.Cell(1, 4).Value = "Started At";
            worksheet.Cell(1, 5).Value = "Completed At";
            worksheet.Cell(1, 6).Value = "Duration";
            worksheet.Cell(1, 7).Value = "Result Count";
            worksheet.Cell(1, 8).Value = "Anomaly Count";
            
            // Style headers
            var headerRange = worksheet.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            
            // Data
            int row = 2;
            foreach (var investigation in investigations)
            {
                var summary = investigation.Summary;
                worksheet.Cell(row, 1).Value = summary.ExecutionId;
                worksheet.Cell(row, 2).Value = summary.InvestigatorId.ToString();
                worksheet.Cell(row, 3).Value = summary.InvestigatorName;
                worksheet.Cell(row, 4).Value = summary.StartedAt;
                worksheet.Cell(row, 5).Value = summary.CompletedAt;
                worksheet.Cell(row, 6).Value = summary.Duration;
                worksheet.Cell(row, 7).Value = summary.ResultCount;
                worksheet.Cell(row, 8).Value = summary.AnomalyCount;
                row++;
            }
            
            // Auto-fit columns
            worksheet.Columns().AdjustToContents();
        }

        private void CreateResultsSheet(IXLWorksheet worksheet, List<InvestigationDetailDto> investigations)
        {
            // Headers
            worksheet.Cell(1, 1).Value = "Execution ID";
            worksheet.Cell(1, 2).Value = "Investigator ID";
            worksheet.Cell(1, 3).Value = "Investigator Name";
            worksheet.Cell(1, 4).Value = "Timestamp";
            worksheet.Cell(1, 5).Value = "Message";
            worksheet.Cell(1, 6).Value = "Payload";
            
            // Style headers
            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            
            // Data
            int row = 2;
            foreach (var investigation in investigations)
            {
                foreach (var result in investigation.DetailedResults)
                {
                    worksheet.Cell(row, 1).Value = investigation.Summary.ExecutionId;
                    worksheet.Cell(row, 2).Value = result.InvestigatorId.ToString();
                    worksheet.Cell(row, 3).Value = investigation.Summary.InvestigatorName;
                    worksheet.Cell(row, 4).Value = result.Timestamp;
                    worksheet.Cell(row, 5).Value = result.Message;
                    worksheet.Cell(row, 6).Value = result.Payload ?? "";
                    row++;
                }
            }
            
            // Auto-fit columns (with max width)
            worksheet.Columns().AdjustToContents(1, 75);
        }

        private static string CalculateDuration(DateTime startedAt, DateTime completedAt)
        {
            var duration = completedAt - startedAt;
            if (duration.TotalDays >= 1)
                return $"{duration.Days}d {duration.Hours}h {duration.Minutes}m";
            if (duration.TotalHours >= 1)
                return $"{duration.Hours}h {duration.Minutes}m {duration.Seconds}s";
            if (duration.TotalMinutes >= 1)
                return $"{duration.Minutes}m {duration.Seconds}s";
            return $"{duration.TotalSeconds:F1}s";
        }
    }
}