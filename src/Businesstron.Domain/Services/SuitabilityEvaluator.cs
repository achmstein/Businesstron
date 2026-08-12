namespace Businesstron.Domain.Services;

/// <summary>
/// Pure, dependency-free rule that decides whether a business name is a suitable
/// lead. Mirrors the client's manual step: "filter out non-suitable ones based on
/// certain words in their names like gov and legal". Kept in the domain so it is
/// trivially unit-testable and reused by the processing service.
/// </summary>
public static class SuitabilityEvaluator
{
    /// <summary>Sensible starting blacklist; editable in the UI once seeded.</summary>
    public static readonly IReadOnlyList<string> DefaultKeywords = new[]
    {
        "gov", "government", "council", "legal", "law", "lawyer", "solicitor",
        "attorney", "barrister", "police", "court", "ministry", "department"
    };

    public readonly record struct Result(bool IsSuitable, string? MatchedKeyword);

    /// <summary>
    /// Returns unsuitable if <paramref name="businessName"/> contains any active
    /// keyword as a case-insensitive substring.
    /// </summary>
    public static Result Evaluate(string? businessName, IEnumerable<string> activeKeywords)
    {
        if (string.IsNullOrWhiteSpace(businessName))
        {
            return new Result(true, null);
        }

        foreach (var keyword in activeKeywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (businessName.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return new Result(false, keyword.Trim());
            }
        }

        return new Result(true, null);
    }
}
