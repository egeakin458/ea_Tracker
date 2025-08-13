namespace ea_Tracker.Models.Common
{
    /// <summary>
    /// Represents the result of a validation operation with error details.
    /// Used by services to return validation status and error messages.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets a value indicating whether the validation passed (no errors).
        /// </summary>
        public bool IsValid => !Errors.Any();

        /// <summary>
        /// Gets or sets the collection of validation error messages.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class with no errors.
        /// </summary>
        public ValidationResult() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class with the specified errors.
        /// </summary>
        /// <param name="errors">The validation error messages.</param>
        public ValidationResult(IEnumerable<string> errors)
        {
            Errors = errors.ToList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class with a single error.
        /// </summary>
        /// <param name="error">The validation error message.</param>
        public ValidationResult(string error)
        {
            Errors = new List<string> { error };
        }
    }
}