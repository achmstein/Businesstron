namespace Businesstron.Infrastructure.ExternalClients.Asic;

/// <summary>
/// Thrown when a run trips the circuit breaker: enough consecutive enrichment failures
/// that ASIC is clearly not answering, so continuing would only burn requests. Carries
/// the triggering error so the run's recorded reason names the actual cause rather than
/// just "the run failed".
/// </summary>
public sealed class AsicUnavailableException(int consecutiveFailures, Exception lastError)
    : Exception(BuildMessage(consecutiveFailures, lastError), lastError)
{
    public int ConsecutiveFailures { get; } = consecutiveFailures;

    private static string BuildMessage(int consecutiveFailures, Exception lastError) =>
        $"Stopped after {consecutiveFailures} consecutive enrichment failures — ASIC is not " +
        $"responding successfully, so the rest of the run was abandoned rather than sending " +
        $"thousands more requests that would also fail. Check Settings → ASIC connection and " +
        $"use Test connection to confirm. Last error: {lastError.Message}";
}
