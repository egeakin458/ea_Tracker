using ea_Tracker.Models.Common;

namespace ea_Tracker.Exceptions
{
    /// <summary>
    /// Exception thrown when validation fails in service layer operations.
    /// Provides structured error information for controller error handling.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// Gets the collection of validation error messages.
        /// </summary>
        public List<string> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with the specified errors.
        /// </summary>
        /// <param name="errors">The validation error messages.</param>
        public ValidationException(IEnumerable<string> errors) 
            : base(string.Join("; ", errors))
        {
            Errors = errors.ToList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a validation result.
        /// </summary>
        /// <param name="result">The validation result containing error messages.</param>
        public ValidationException(ValidationResult result) 
            : this(result.Errors)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationException"/> class with a single error.
        /// </summary>
        /// <param name="error">The validation error message.</param>
        public ValidationException(string error) 
            : this(new[] { error })
        {
        }
    }
}