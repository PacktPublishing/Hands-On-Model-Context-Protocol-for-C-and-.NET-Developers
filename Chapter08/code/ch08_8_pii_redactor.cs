// Chapter 8 — Section 8.3.4
// PII redactor that strips passport numbers, dates of birth, email addresses,
// and phone numbers from text before it is written to logs or conversation history.
// Call Redact() before every logger.LogInformation that includes conversation content.

using System.Text.RegularExpressions;

namespace TravelBooking.Orchestration;

public static class PiiRedactor
{
    private static readonly Regex Passport =
        new(@"\b[A-Z]{1,2}\d{6,9}\b", RegexOptions.Compiled);

    private static readonly Regex Dob =
        new(@"\b\d{4}-\d{2}-\d{2}\b", RegexOptions.Compiled);

    private static readonly Regex Email =
        new(@"\b[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Matches international formats: +44 7911 123456, 07911 123456, +1-800-555-0199
    private static readonly Regex Phone =
        new(@"\b(\+\d{1,3}[\s\-]?)?\(?\d{2,4}\)?[\s\-]?\d{3,4}[\s\-]?\d{4}\b",
            RegexOptions.Compiled);

    public static string Redact(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = Passport.Replace(text, "[PASSPORT]");
        text = Dob.Replace(text, "[DOB]");
        text = Email.Replace(text, "[EMAIL]");
        text = Phone.Replace(text, "[PHONE]");
        return text;
    }
}
