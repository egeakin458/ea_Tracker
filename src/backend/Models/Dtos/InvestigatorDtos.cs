using System;

namespace ea_Tracker.Models.Dtos
{
    /// <summary>
    /// DTO representing the public state of an investigator.
    /// </summary>
    public record InvestigatorStateDto(Guid Id, string Name, bool IsRunning, int ResultCount);

    /// <summary>
    /// DTO representing a log entry from an investigator.
    /// </summary>
    public record InvestigatorResultDto(Guid InvestigatorId, DateTime Timestamp, string Message, string? Payload);

    /// <summary>
    /// DTO representing a completed investigation for the results panel.
    /// Contains summary information about finished investigations.
    /// </summary>
    public record CompletedInvestigationDto(
        int ExecutionId,
        Guid InvestigatorId,
        string InvestigatorName,
        DateTime StartedAt,
        DateTime CompletedAt,
        string Duration,
        int ResultCount,
        int AnomalyCount,
        bool IsHighlighted = false
    );

    /// <summary>
    /// DTO containing detailed information about a specific investigation execution.
    /// Includes both summary and detailed results for modal display and export.
    /// </summary>
    public record InvestigationDetailDto(
        CompletedInvestigationDto Summary,
        IEnumerable<InvestigatorResultDto> DetailedResults
    );

    /// <summary>
    /// DTO for investigation export operations.
    /// Contains the exported data and metadata.
    /// </summary>
    public record InvestigationExportDto(
        byte[] Data,
        string ContentType,
        string FileName
    );

    /// <summary>
    /// DTO for clear investigations operation result.
    /// </summary>
    public record ClearInvestigationsResultDto(
        string Message,
        int ResultsDeleted,
        int ExecutionsDeleted
    );

    /// <summary>
    /// DTO for delete investigation operation result.
    /// </summary>
    public record DeleteInvestigationResultDto(
        string Message,
        int ResultsDeleted
    );

    /// <summary>
    /// DTO for bulk export request containing execution IDs and desired format.
    /// </summary>
    public record BulkExportRequestDto(
        List<int> ExecutionIds,
        string Format // "json", "csv", "excel"
    );
}
