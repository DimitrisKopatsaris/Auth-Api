using System.Text.Json;
using AuthApi.Models;

namespace AuthApi.Middleware
{
    public class ResponseWrapperMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseWrapperMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var originalBody = context.Response.Body;
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            try
            {
                // Continue through the pipeline
                await _next(context);

                // Read the controller's response
                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
                memoryStream.Seek(0, SeekOrigin.Begin);
                context.Response.Body = originalBody;

                // Skip wrapping for already wrapped or empty responses
                if (string.IsNullOrWhiteSpace(responseBody) ||
                    (responseBody.Contains("\"success\"") && responseBody.Contains("\"data\"")))
                {
                    await context.Response.WriteAsync(responseBody);
                    return;
                }

                // ✅ Determine automatic message based on HTTP method
                string message = context.Request.Method switch
                {
                    "GET"    => "Data retrieved successfully.",
                    "POST"   => "Resource created successfully.",
                    "PUT"    => "Resource updated successfully.",
                    "DELETE" => "Resource deleted successfully.",
                    _        => "Request completed successfully."
                };

                // ✅ Wrap the response in a consistent format
                var wrapped = new ApiResponse<object>(
                    true,
                    message,
                    JsonSerializer.Deserialize<object>(responseBody)
                );

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(wrapped));
            }
            catch (Exception ex)
            {
                // ✅ Handle unexpected errors gracefully
                context.Response.Body = originalBody;
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                var error = new ApiResponse<string>(
                    false,
                    "An unexpected error occurred.",
                    null,
                    new { ex.Message }
                );

                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            }
        }
    }

    // ✅ Extension method for clean registration
    public static class ResponseWrapperMiddlewareExtensions
    {
        public static IApplicationBuilder UseResponseWrapper(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ResponseWrapperMiddleware>();
        }
    }
}
