namespace ea_Tracker.Services
{
    /// <summary>
    /// Creates investigator instances by kind.
    /// </summary>
    public interface IInvestigatorFactory
    {
        /// <summary>
        /// Creates a new Investigator of the specified type.
        /// </summary>
        /// <param name="kind">The investigator kind (e.g. "invoice", "waybill").</param>
        /// <returns>A fresh Investigator instance.</returns>
        Investigator Create(string kind);
    }
}
