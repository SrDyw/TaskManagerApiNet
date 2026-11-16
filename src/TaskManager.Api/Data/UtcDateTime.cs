namespace TaskManager.Api.Data;

public static class UtcDateTime
{
    /// <summary>
    /// Converts a value to UTC. Values without an offset are treated as UTC so the
    /// result never depends on the server's local time zone.
    /// </summary>
    public static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
