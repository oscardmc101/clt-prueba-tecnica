using FluentValidation;

namespace Clt.Api.Application.Common;

public static class RequestValidation
{
    public static Task EnsureValidAsync<T>(this IValidator<T> validator, T request,
        CancellationToken cancellationToken) => validator.ValidateAndThrowAsync(request, cancellationToken);
}
