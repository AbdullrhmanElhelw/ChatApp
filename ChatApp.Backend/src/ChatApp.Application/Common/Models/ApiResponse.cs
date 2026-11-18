namespace ChatApp.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully.")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> Fail(string message, T? data = default)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }
}
