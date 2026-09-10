namespace AssetsManagement.Application;

public sealed record ApiResponse<T>(bool Success, string Message, T? Data, IReadOnlyCollection<ApiError> Errors)
{
    public static ApiResponse<T> Ok(T data, string message = "Records retrieved successfully.") =>
        new(true, message, data, []);
}

public sealed record ApiError(string? Field, string Message, string? Code = null);
