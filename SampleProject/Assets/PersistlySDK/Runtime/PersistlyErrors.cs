#nullable enable
using System;

namespace Persistly.Unity
{
    public enum PersistlyErrorCode
    {
        InvalidRequest,
        Unauthorized,
        Forbidden,
        NotFound,
        Conflict,
        SlotAlreadyExists,
        CharacterArchived,
        ProfileDeleted,
        CharacterDeleted,
        RateLimited,
        MonthlyQuotaExceeded,
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

    public sealed class PersistlyForbiddenError : PersistlyApiError
    {
        public PersistlyForbiddenError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.Forbidden, message, detailsJson)
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

    public sealed class PersistlySlotAlreadyExistsError : PersistlyApiError
    {
        public PersistlySlotAlreadyExistsError(int statusCode, string message, string? slotKey, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.SlotAlreadyExists, message, detailsJson)
        {
            SlotKey = slotKey;
        }

        public string? SlotKey { get; }
    }

    public sealed class PersistlyArchivedCharacterError : PersistlyApiError
    {
        public PersistlyArchivedCharacterError(int statusCode, string message, string? characterSaveId, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.CharacterArchived, message, detailsJson)
        {
            CharacterSaveId = characterSaveId;
        }

        public string? CharacterSaveId { get; }
    }

    public sealed class PersistlyProfileDeletedError : PersistlyApiError
    {
        public PersistlyProfileDeletedError(int statusCode, string message, string? profileSaveId, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.ProfileDeleted, message, detailsJson)
        {
            ProfileSaveId = profileSaveId;
        }

        public string? ProfileSaveId { get; }
    }

    public sealed class PersistlyCharacterDeletedError : PersistlyApiError
    {
        public PersistlyCharacterDeletedError(int statusCode, string message, string? characterSaveId, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.CharacterDeleted, message, detailsJson)
        {
            CharacterSaveId = characterSaveId;
        }

        public string? CharacterSaveId { get; }
    }

    public sealed class PersistlyRateLimitedError : PersistlyApiError
    {
        public PersistlyRateLimitedError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.RateLimited, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyMonthlyQuotaExceededError : PersistlyApiError
    {
        public PersistlyMonthlyQuotaExceededError(int statusCode, string message, string? planTier, long? used, long? limit, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.MonthlyQuotaExceeded, message, detailsJson)
        {
            PlanTier = planTier;
            Used = used;
            Limit = limit;
        }

        public string? PlanTier { get; }

        public long? Used { get; }

        public long? Limit { get; }
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
