namespace Errors.Abstractions;

/// <summary>
/// Represents the outcome of an operation that can succeed with a value or fail with an <see cref="IAppError"/>.
/// </summary>
/// <typeparam name="T">Type of the success value.</typeparam>
public readonly struct Result<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the value produced on success; otherwise <c>null</c>.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error associated with a failed result.
    /// </summary>
    public IAppError? Error { get; }

    private Result(bool isSuccess, T? value, IAppError? error)
    { IsSuccess = isSuccess; Value = value; Error = error; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">Returned value.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Ok(T value) => new(true, value, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">Associated application error.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Fail(IAppError error) => new(false, default, error);
}
