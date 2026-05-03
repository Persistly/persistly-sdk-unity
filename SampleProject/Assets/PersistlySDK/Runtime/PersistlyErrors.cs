#nullable enable
using System;

namespace Persistly.Unity
{
    public enum PersistlyErrorCode
    {
        InvalidRequest,
        Unauthorized,
        NotFound,
        Conflict,
        RateLimited,
        PayloadTooLarge,
        ServerError
    }

    public abstract class PersistlyError : Exception
    {
        protected PersistlyError(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    public class PersistlyConfigurationError : PersistlyError
    {
        public PersistlyConfigurationError(string message)
            : base(message)
        {
        }
    }

    public class PersistlyTransportError : PersistlyError
    {
        public PersistlyTransportError(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    public class PersistlyApiError : PersistlyError
    {
        public PersistlyApiError(int statusCode, PersistlyErrorCode code, string message, string? detailsJson = null)
            : base(message)
        {
            StatusCode = statusCode;
            Code = code;
            DetailsJson = detailsJson;
        }

        public int StatusCode { get; }

        public PersistlyErrorCode Code { get; }

        public string? DetailsJson { get; }
    }

    public sealed class PersistlyInvalidRequestError : PersistlyApiError
    {
        public PersistlyInvalidRequestError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.InvalidRequest, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyUnauthorizedError : PersistlyApiError
    {
        public PersistlyUnauthorizedError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.Unauthorized, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyNotFoundError : PersistlyApiError
    {
        public PersistlyNotFoundError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.NotFound, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyConflictError : PersistlyApiError
    {
        public PersistlyConflictError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.Conflict, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyRateLimitedError : PersistlyApiError
    {
        public PersistlyRateLimitedError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.RateLimited, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyPayloadTooLargeError : PersistlyApiError
    {
        public PersistlyPayloadTooLargeError(int statusCode, string message, string? field, int? maxBytes, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.PayloadTooLarge, message, detailsJson)
        {
            Field = field;
            MaxBytes = maxBytes;
        }

        public string? Field { get; }

        public int? MaxBytes { get; }
    }

    public sealed class PersistlyServerError : PersistlyApiError
    {
        public PersistlyServerError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.ServerError, message, detailsJson)
        {
        }
    }
}
