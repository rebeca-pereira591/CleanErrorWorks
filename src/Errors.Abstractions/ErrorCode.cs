namespace Errors.Abstractions;

/// <summary>
/// Represents a stable identifier for a domain or infrastructure error, including human-readable metadata.
/// </summary>
public readonly record struct ErrorCode
{
    /// <summary>
    /// Gets the short code for the error (e.g., <c>AUTH-001</c>).
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the display title associated with the error code.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the optional URI that links to documentation for the error.
    /// </summary>
    public string? TypeUri { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorCode"/> struct.
    /// </summary>
    /// <param name="code">Unique identifier for the error.</param>
    /// <param name="title">User-facing title that describes the error.</param>
    /// <param name="typeUri">Optional URI with additional error context.</param>
    public ErrorCode(string code, string title, string? typeUri = null)
    { Code = code; Title = title; TypeUri = typeUri; }

    /// <inheritdoc />
    public override string ToString() => Code;
}
