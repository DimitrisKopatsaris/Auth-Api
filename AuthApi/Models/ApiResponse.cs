namespace AuthApi.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public object? Errors { get; set; }

        public ApiResponse(bool success, string message, T? data = default, object? errors = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Errors = errors;
        }

        // ✅ Shortcut static methods for convenience
        public static ApiResponse<T> SuccessResponse(T data, string message = "Request successful.")
            => new(true, message, data);

        public static ApiResponse<T> FailureResponse(string message, object? errors = null)
            => new(false, message, default, errors);
    }
}
