namespace ea_Tracker.Services
{
    /// <summary>
    /// Generic interface for pure business logic components.
    /// Implements Single Responsibility Principle - focuses solely on business rule evaluation.
    /// Generic design enables reusability across different entity types.
    /// </summary>
    /// <typeparam name="T">The entity type to investigate (Invoice, Waybill, etc.)</typeparam>
    public interface IInvestigationLogic<T> where T : class
    {
        /// <summary>
        /// Evaluates business rules against a collection of entities.
        /// Pure business logic - no infrastructure dependencies.
        /// </summary>
        /// <param name="entities">Collection of entities to investigate</param>
        /// <param name="configuration">Business rule configuration</param>
        /// <returns>Collection of entities that violate business rules (anomalies)</returns>
        IEnumerable<T> FindAnomalies(IEnumerable<T> entities, IInvestigationConfiguration configuration);

        /// <summary>
        /// Evaluates business rules against a single entity.
        /// Useful for real-time validation during entity creation/modification.
        /// </summary>
        /// <param name="entity">Single entity to investigate</param>
        /// <param name="configuration">Business rule configuration</param>
        /// <returns>True if entity violates business rules, false otherwise</returns>
        bool IsAnomaly(T entity, IInvestigationConfiguration configuration);

        /// <summary>
        /// Gets detailed information about why an entity is considered anomalous.
        /// Useful for audit trails and user feedback.
        /// </summary>
        /// <param name="entity">Entity to analyze</param>
        /// <param name="configuration">Business rule configuration</param>
        /// <returns>Collection of rule violation descriptions</returns>
        IEnumerable<string> GetAnomalyReasons(T entity, IInvestigationConfiguration configuration);
    }

    /// <summary>
    /// Result of a business logic evaluation.
    /// Encapsulates both the anomaly status and detailed reasoning.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class InvestigationResult<T> where T : class
    {
        /// <summary>
        /// The entity that was evaluated.
        /// </summary>
        public T Entity { get; set; } = null!;

        /// <summary>
        /// Whether this entity is considered anomalous.
        /// </summary>
        public bool IsAnomaly { get; set; }

        /// <summary>
        /// Detailed reasons for the anomaly classification.
        /// Empty if IsAnomaly is false.
        /// </summary>
        public IList<string> Reasons { get; set; } = new List<string>();

        /// <summary>
        /// Timestamp when the evaluation was performed.
        /// </summary>
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    }
}