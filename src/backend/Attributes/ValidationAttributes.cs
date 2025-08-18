using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ea_Tracker.Attributes
{
    /// <summary>
    /// Custom validation attribute to sanitize and validate string inputs against XSS attacks.
    /// Prevents malicious script injection while preserving legitimate content.
    /// </summary>
    public class SanitizedStringAttribute : ValidationAttribute
    {
        private readonly bool _allowHtml;
        private readonly int _maxLength;

        /// <summary>
        /// Initializes a new instance of the SanitizedStringAttribute.
        /// </summary>
        /// <param name="allowHtml">Whether to allow basic HTML tags</param>
        /// <param name="maxLength">Maximum allowed string length</param>
        public SanitizedStringAttribute(bool allowHtml = false, int maxLength = 1000)
        {
            _allowHtml = allowHtml;
            _maxLength = maxLength;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is not string stringValue)
                return new ValidationResult("Value must be a string.");

            // Check length
            if (stringValue.Length > _maxLength)
                return new ValidationResult($"String length cannot exceed {_maxLength} characters.");

            // Check for potentially dangerous patterns
            if (ContainsDangerousContent(stringValue))
                return new ValidationResult("Input contains potentially dangerous content.");

            // If HTML is not allowed, check for HTML tags
            if (!_allowHtml && ContainsHtml(stringValue))
                return new ValidationResult("HTML content is not allowed in this field.");

            return ValidationResult.Success;
        }

        private static bool ContainsDangerousContent(string input)
        {
            // Check for script tags, javascript: protocol, and other dangerous patterns
            var dangerousPatterns = new[]
            {
                @"<script[^>]*>.*?</script>",
                @"javascript:",
                @"vbscript:",
                @"onload\s*=",
                @"onerror\s*=",
                @"onclick\s*=",
                @"onmouseover\s*=",
                @"<iframe[^>]*>",
                @"<object[^>]*>",
                @"<embed[^>]*>",
                @"<link[^>]*>",
                @"<meta[^>]*>",
                @"data:text/html"
            };

            return dangerousPatterns.Any(pattern => 
                Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline));
        }

        private static bool ContainsHtml(string input)
        {
            return Regex.IsMatch(input, @"<[^>]+>", RegexOptions.IgnoreCase);
        }
    }

    /// <summary>
    /// Validation attribute for safe file names to prevent path traversal attacks.
    /// </summary>
    public class SafeFileNameAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is not string fileName)
                return new ValidationResult("File name must be a string.");

            // Check for dangerous characters and patterns
            var dangerousPatterns = new[] { "..", "\\", "/", ":", "*", "?", "\"", "<", ">", "|" };
            
            if (dangerousPatterns.Any(pattern => fileName.Contains(pattern)))
                return new ValidationResult("File name contains invalid characters.");

            // Check for reserved Windows file names
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", 
                "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", 
                "LPT6", "LPT7", "LPT8", "LPT9" };
            
            if (reservedNames.Any(name => 
                string.Equals(name, Path.GetFileNameWithoutExtension(fileName), StringComparison.OrdinalIgnoreCase)))
                return new ValidationResult("File name uses a reserved system name.");

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validation attribute for SQL injection prevention in search terms.
    /// </summary>
    public class SqlSafeAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is not string stringValue)
                return new ValidationResult("Value must be a string.");

            // Check for common SQL injection patterns
            var sqlPatterns = new[]
            {
                @"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UNION( +ALL)?|UPDATE)\b)",
                @"(\b(AND|OR)\b.{1,6}?(=|>|<|\!=|<>|<=|>=))",
                @"(\b(AND|OR)\b.{1,6}?\b(IS( +NOT)?( +NULL)?|(NOT )?IN|EXISTS)\b)",
                @"(--|#|/\*|\*/|@@|@)",
                @"(\bCAST\s*\()",
                @"(\bCONVERT\s*\()",
                @"(\bUNION\b.*\bSELECT\b)",
                @"(\b1\s*=\s*1\b)",
                @"('(\s)*('|;|\||#|--|\*))",
                @"(;\s*shutdown\s*--)",
                @"(;\s*xp_cmdshell\s*\()"
            };

            foreach (var pattern in sqlPatterns)
            {
                if (Regex.IsMatch(stringValue, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    return new ValidationResult("Input contains potentially unsafe SQL patterns.");
                }
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validation attribute for decimal ranges with precision control.
    /// </summary>
    public class DecimalRangeAttribute : RangeAttribute
    {
        public DecimalRangeAttribute(double minimum, double maximum) : base(minimum, maximum)
        {
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return true;

            if (decimal.TryParse(value.ToString(), out decimal decimalValue))
            {
                return decimalValue >= Convert.ToDecimal(Minimum) && decimalValue <= Convert.ToDecimal(Maximum);
            }

            return false;
        }
    }

    /// <summary>
    /// Validation attribute for future date restrictions.
    /// </summary>
    public class NotFutureDateAttribute : ValidationAttribute
    {
        private readonly int _maxDaysInFuture;

        public NotFutureDateAttribute(int maxDaysInFuture = 0)
        {
            _maxDaysInFuture = maxDaysInFuture;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is not DateTime dateValue)
                return new ValidationResult("Value must be a valid date.");

            var maxAllowedDate = DateTime.UtcNow.AddDays(_maxDaysInFuture);

            if (dateValue > maxAllowedDate)
            {
                if (_maxDaysInFuture == 0)
                    return new ValidationResult("Date cannot be in the future.");
                else
                    return new ValidationResult($"Date cannot be more than {_maxDaysInFuture} days in the future.");
            }

            return ValidationResult.Success;
        }
    }

    /// <summary>
    /// Validation attribute for business hour restrictions.
    /// </summary>
    public class BusinessDateRangeAttribute : ValidationAttribute
    {
        private readonly int _maxYearsPast;

        public BusinessDateRangeAttribute(int maxYearsPast = 10)
        {
            _maxYearsPast = maxYearsPast;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is not DateTime dateValue)
                return new ValidationResult("Value must be a valid date.");

            var minDate = DateTime.UtcNow.AddYears(-_maxYearsPast);
            var maxDate = DateTime.UtcNow.AddDays(30); // Allow up to 30 days in future for business dates

            if (dateValue < minDate)
                return new ValidationResult($"Date cannot be more than {_maxYearsPast} years in the past.");

            if (dateValue > maxDate)
                return new ValidationResult("Date cannot be more than 30 days in the future for business records.");

            return ValidationResult.Success;
        }
    }
}