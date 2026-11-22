using FluentValidation;

namespace Api.Filters;

/// <summary>
/// Endpoint filter: знаходить серед аргументів обробника модель типу T, перевіряє її
/// валідатором із DI і при помилках повертає 400 з ValidationProblemDetails.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        T? model = context.Arguments.OfType<T>().FirstOrDefault();
        if (model is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["Тіло запиту обов'язкове."]
            });
        }

        IValidator<T> validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var result = await validator.ValidateAsync(model, context.HttpContext.RequestAborted);

        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());

        return await next(context);
    }
}
