using System.Net;
using KeyCard.Core.Wrappers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// to suppress possibly null reference warnings.
#pragma warning disable CS8600, CS8604, CS8602, CS8625

namespace KeyCard.Core.Middlewares
{

    /// <summary>
    /// Response middleware class.
    /// </summary>
    public class ResponseMiddleware
    {
        private readonly RequestDelegate next;
        private readonly bool traceResponse;
        private readonly ILogger<ResponseMiddleware> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseMiddleware"/> class.
        /// </summary>
        /// <param name="next"> <see cref="RequestDelegate"/> Next.</param>
        /// <param name="logger">logger.</param>
        /// <param name="traceResponse"> Trace response.</param>
        public ResponseMiddleware(RequestDelegate next, ILogger<ResponseMiddleware> logger, bool traceResponse = false)
        {
            this.next = next;
            this.traceResponse = traceResponse;
            this.logger = logger;
        }

        /// <summary>
        /// Invoke the response.
        /// </summary>
        /// <param name="context"> <see cref="HttpContext"/> Context.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (context.Ignore())
            {
                await this.next(context);
            }
            else
            {
                var originalBodyStream = context.Response.Body;

                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                try
                {
                    await this.next.Invoke(context);

                    if (!context.Ignore())
                    {
                        if (context.Response.StatusCode == (int)HttpStatusCode.OK)
                        {
                            var body = await FormatResponse(context.Response);
                            await this.HandleSuccessRequestAsync(context, body, (HttpStatusCode)context.Response.StatusCode);
                        }
                        else
                        {
                            await HandleNotSuccessRequestAsync(context, (HttpStatusCode)context.Response.StatusCode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError($"Error occurred: {ex.StackTrace}");
                    await HandleExceptionAsync(context, ex);
                }
                finally
                {
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
        }

        /// <summary>
        /// Formate response.
        /// </summary>
        /// <param name="response"> <see cref="HttpResponse"/> Http Response.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private static async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var plainBodyText = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return plainBodyText;
        }

        /// <summary>
        /// Handle exceptions.
        /// </summary>
        /// <param name="context"> <see cref="HttpContext"/> representing Http context.</param>
        /// <param name="exception"> <see cref="Exception"/> exception.</param>
        /// <param name="localizer"> <see cref="IStringLocalizer"/> string localizer.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            ApiError apiError;
            int code;

            switch (exception)
            {
                case UnauthorizedAccessException _:
                    apiError = new ApiError("Unauthorized Access");
                    code = (int)HttpStatusCode.Unauthorized;
                    context.Response.StatusCode = code;

                    break;
                case CustomValidationException ex:

                    apiError = new ApiError(ex.Message)
                    {
                        ValidationErrors = ex.Errors,
                    };

                    code = ex.StatusCode;
                    context.Response.StatusCode = code;

                    break;
                default:
                    var msg = exception.GetBaseException().Message;

                    apiError = new ApiError(msg);
                    code = (int)HttpStatusCode.InternalServerError;
                    context.Response.StatusCode = code;

                    break;
            }

            context.Response.ContentType = "application/json";

            var response = new ApiResponse<string>(code, ResponseMessageEnum.Exception.GetDescription(), null, apiError);

            var json = JsonConvert.SerializeObject(response);

            context.Response.ContentLength = null;
            return context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Handle not success request.
        /// </summary>
        /// <param name="context"> <see cref="HttpContext"/> representing Http context.</param>
        /// <param name="code"> <see cref="HttpStatusCode"/> representing Http status code.</param>
        /// <param name="localizer"> <see cref="IStringLocalizer"/> string localizer.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        private static Task HandleNotSuccessRequestAsync(HttpContext context, HttpStatusCode code)
        {
            if (context.IsNoContent())
            {
                return Task.CompletedTask;
            }

            context.Response.ContentType = "application/json";

            var apiError = new ApiError("Cannot process request.");

            var response = new ApiResponse<string>((int)code, ResponseMessageEnum.Failure.GetDescription(), null, apiError);
            context.Response.StatusCode = (int)code;

            var json = JsonConvert.SerializeObject(response);

            // Core 3.1 Change
            context.Response.ContentLength = null;
            return context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Handle success request.
        /// </summary>
        /// <param name="context"> <see cref="HttpContext"/> representing Http context.</param>
        /// <param name="body"> Body.</param>
        /// <param name="code"> <see cref="HttpStatusCode"/> representing Http status code.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        private Task HandleSuccessRequestAsync(HttpContext context, object body, HttpStatusCode code)
        {
            ApiResponse<object> response;

            context.Response.ContentType = "application/json";

            ApiResponse<object>? convertedBody = null;

            try
            {
                convertedBody = bool.TryParse(body.ToString(), out _) ? null : JsonConvert.DeserializeObject<ApiResponse<object>>(body.ToString());
            }
            catch (Exception)
            {
                // ignored
            }

            if (convertedBody?.Result == null && (convertedBody?.StatusCode == null || convertedBody.StatusCode == 0))
            {
                var bodyText = !body.ToString().IsValidJson() ? JsonConvert.SerializeObject(body) : body.ToString();

                var bodyContent = JsonConvert.DeserializeObject<object>(bodyText);

                var type = bodyContent?.GetType();

                if (type == typeof(JObject))
                {
                    response = JsonConvert.DeserializeObject<ApiResponse<object>>(bodyText);

                    response = response.StatusCode != (int)code
                        ? new ApiResponse<object>((int)code, ((ResponseMessageEnum)response.StatusCode).GetDescription(), bodyContent)
                        : new ApiResponse<object>((int)code, ResponseMessageEnum.Success.GetDescription(), bodyContent);
                }
                else
                {
                    response = new ApiResponse<object>((int)code, ResponseMessageEnum.Success.GetDescription(), bodyContent);
                }
            }
            else
            {
                response = convertedBody;
            }

            var jsonString = JsonConvert.SerializeObject(response);

            if (!this.traceResponse)
            {
                context.Response.ContentLength = null;

                // Set length of response after the JSON string length
                context.Response.Body.SetLength(jsonString.Length);
                return context.Response.WriteAsync(jsonString);
            }

            context.Response.ContentLength = null;

            // Set length of response after the JSON string length
            context.Response.Body.SetLength(jsonString.Length);
            return context.Response.WriteAsync(jsonString);
        }
    }
}
