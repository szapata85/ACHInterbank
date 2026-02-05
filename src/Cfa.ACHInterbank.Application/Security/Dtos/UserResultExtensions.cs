using Cfa.ACHInterbank.Application.Common;

namespace Cfa.ACHInterbank.Application.Security.Dtos;

public static class UserResultExtensions
{
    public static Result<UserSummaryDto?> ToResult(this UserOperationResult operation)
    {
        return operation.Status switch
        {
            UserOperationStatus.Success => Result<UserSummaryDto?>.Success(operation.User),
            UserOperationStatus.NotFound => Result<UserSummaryDto?>.Failure("USER_NOT_FOUND", operation.Message ?? "Usuario no encontrado", ErrorType.NotFound),
            UserOperationStatus.Conflict => Result<UserSummaryDto?>.Failure("USER_CONFLICT", operation.Message ?? "Conflicto de usuario", ErrorType.Conflict),
            UserOperationStatus.ValidationError => Result<UserSummaryDto?>.Failure("USER_VALIDATION", operation.Message ?? "Error de validación", ErrorType.Validation),
            _ => Result<UserSummaryDto?>.Failure("USER_UNEXPECTED", "Error inesperado", ErrorType.Unexpected)
        };
    }

    public static Result ToResult(this UserOperationStatus status)
    {
        return status switch
        {
            UserOperationStatus.Success => Result.Success(),
            UserOperationStatus.NotFound => Result.Failure("USER_NOT_FOUND", "Usuario no encontrado", ErrorType.NotFound),
            UserOperationStatus.ValidationError => Result.Failure("USER_VALIDATION", "Error de validación", ErrorType.Validation),
            UserOperationStatus.Conflict => Result.Failure("USER_CONFLICT", "Conflicto de usuario", ErrorType.Conflict),
            _ => Result.Failure("USER_UNEXPECTED", "Error inesperado", ErrorType.Unexpected)
        };
    }
}
