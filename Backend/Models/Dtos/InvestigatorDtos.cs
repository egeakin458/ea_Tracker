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
}
