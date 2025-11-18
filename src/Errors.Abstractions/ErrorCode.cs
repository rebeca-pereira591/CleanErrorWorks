namespace Errors.Abstractions;

public readonly record struct ErrorCode
{
    public string Code { get; }
    public string Title { get; }
    public string? TypeUri { get; }

    public ErrorCode(string code, string title, string? typeUri = null)
    { Code = code; Title = title; TypeUri = typeUri; }

    public override string ToString() => Code;
}