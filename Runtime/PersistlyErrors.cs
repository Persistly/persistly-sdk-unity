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
        SlotArchived,
        AccountDeleted,
        SlotDeleted,
        RateLimited,
        MonthlyQuotaExceeded,
        PayloadTooLarge,
        TransferCodeInvalid,
        TransferCodeExpired,
        TransferCodeConsumed,
        TransferCodeRateLimited,
        TransferCodeDisabled,
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
        public PersistlySlotAlreadyExistsError(int statusCode, string message, string? slotId, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.SlotAlreadyExists, message, detailsJson)
        {
            SlotId = slotId;
        }

        public string? SlotId { get; }
    }

    public sealed class PersistlySlotArchivedError : PersistlyApiError
    {
        public PersistlySlotArchivedError(int statusCode, string message, string? slotId, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.SlotArchived, message, detailsJson)
        {
            SlotId = slotId;
        }

        public string? SlotId { get; }
    }

    public sealed class PersistlyAccountDeletedError : PersistlyApiError
    {
        public PersistlyAccountDeletedError(int statusCode, string message, string? accountId, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.AccountDeleted, message, detailsJson)
        {
            AccountId = accountId;
        }

        public string? AccountId { get; }
    }

    public sealed class PersistlySlotDeletedError : PersistlyApiError
    {
        public PersistlySlotDeletedError(int statusCode, string message, string? slotId, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.SlotDeleted, message, detailsJson)
        {
            SlotId = slotId;
        }

        public string? SlotId { get; }
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

    public abstract class PersistlyTransferCodeError : PersistlyApiError
    {
        protected PersistlyTransferCodeError(int statusCode, PersistlyErrorCode code, string message, string? detailsJson = null)
            : base(statusCode, code, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyTransferCodeInvalidError : PersistlyTransferCodeError
    {
        public PersistlyTransferCodeInvalidError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.TransferCodeInvalid, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyTransferCodeExpiredError : PersistlyTransferCodeError
    {
        public PersistlyTransferCodeExpiredError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.TransferCodeExpired, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyTransferCodeConsumedError : PersistlyTransferCodeError
    {
        public PersistlyTransferCodeConsumedError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.TransferCodeConsumed, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyTransferCodeRateLimitedError : PersistlyTransferCodeError
    {
        public PersistlyTransferCodeRateLimitedError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.TransferCodeRateLimited, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyTransferCodeDisabledError : PersistlyTransferCodeError
    {
        public PersistlyTransferCodeDisabledError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.TransferCodeDisabled, message, detailsJson)
        {
        }
    }

    public sealed class PersistlyServerError : PersistlyApiError
    {
        public PersistlyServerError(int statusCode, string message, string? detailsJson = null)
            : base(statusCode, PersistlyErrorCode.ServerError, message, detailsJson)
        {
        }
    }
}
