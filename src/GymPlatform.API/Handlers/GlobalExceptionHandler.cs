using GymPlatform.Shared.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GymPlatform.API.Handlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Error de validación"),
            AuthenticationException => (StatusCodes.Status401Unauthorized, "No autorizado"),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            DuplicateResourceException => (StatusCodes.Status409Conflict, "Conflicto por duplicidad"),
            ScheduleConflictException => (StatusCodes.Status409Conflict, "Conflicto de horarios"),
            DependencyConflictException => (StatusCodes.Status409Conflict, "Conflicto de dependencia"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto en la operación"),
            GymException => (StatusCodes.Status400BadRequest, "Error en la operación del gimnasio"),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Excepción no controlada capturada por el GlobalExceptionHandler en {Path}", httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Excepción de negocio controlada ({StatusCode} {Title}): {Message} en {Path}",
                statusCode, title, exception.Message, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        // Mantener compatibilidad con clientes existentes que esperan la propiedad "error"
        problemDetails.Extensions["error"] = exception.Message;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}

