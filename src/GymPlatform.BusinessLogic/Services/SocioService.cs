using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;
using Microsoft.Extensions.Logging;

namespace GymPlatform.BusinessLogic.Services;

public class SocioService : ISocioService
{
    private readonly ISocioRepository _socioRepository;
    private readonly ILogger<SocioService>? _logger;
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public SocioService(ISocioRepository socioRepository, ILogger<SocioService>? logger = null)
    {
        _socioRepository = socioRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<SocioResponseDTO>> GetAllAsync()
    {
        _logger?.LogInformation("Consultando todos los socios activos en el sistema.");
        var socios = await _socioRepository.GetAllAsync();
        var response = new List<SocioResponseDTO>();
        foreach (var socio in socios)
        {
            response.Add(MapToResponseDTO(socio));
        }
        return response;
    }

    public async Task<SocioResponseDTO> GetByIdAsync(Guid id)
    {
        _logger?.LogInformation("Buscando socio por ID: {SocioId}", id);
        var socio = await _socioRepository.GetByIdAsync(id);
        if (socio is null)
        {
            _logger?.LogWarning("No se encontró el socio con identificador {SocioId}", id);
            throw new NotFoundException($"No se encontró el socio con identificador {id}.");
        }
        return MapToResponseDTO(socio);
    }

    public async Task<SocioResponseDTO> CreateAsync(SocioCreateDTO dto)
    {
        _logger?.LogInformation("Iniciando registro de nuevo socio con DNI: {Dni}", dto.Dni);

        // 1. Validaciones básicas de campos obligatorios
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new GymPlatform.Shared.Exceptions.ValidationException("El nombre del socio es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Dni))
        {
            throw new GymPlatform.Shared.Exceptions.ValidationException("El DNI del socio es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Email) || !EmailRegex.IsMatch(dto.Email))
        {
            throw new GymPlatform.Shared.Exceptions.ValidationException("El formato del correo electrónico no es válido.");
        }

        // 2. Validación de anotaciones del DTO vía Validator
        var validationContext = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
        {
            var messages = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new GymPlatform.Shared.Exceptions.ValidationException(messages);
        }

        // 3. Validación de fecha de nacimiento y edad
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.FechaNacimiento > hoy)
        {
            _logger?.LogWarning("Fecha de nacimiento en el futuro rechazada: {Fecha}", dto.FechaNacimiento);
            throw new GymPlatform.Shared.Exceptions.ValidationException("La fecha de nacimiento no puede ser una fecha futura.");
        }

        var edad = hoy.Year - dto.FechaNacimiento.Year;
        if (dto.FechaNacimiento > hoy.AddYears(-edad))
        {
            edad--;
        }

        if (edad < 18)
        {
            _logger?.LogWarning("Intento de registro de menor de edad ({Edad} años) rechazado.", edad);
            throw new GymPlatform.Shared.Exceptions.ValidationException("El socio debe ser mayor de 18 años para registrarse.");
        }

        if (edad > 120)
        {
            _logger?.LogWarning("Fecha de nacimiento excede el límite razonable ({Edad} años): {Fecha}", edad, dto.FechaNacimiento);
            throw new GymPlatform.Shared.Exceptions.ValidationException("La fecha de nacimiento no puede ser anterior a 120 años.");
        }

        // 4. Verificación de duplicados por DNI y Correo (HTTP 409)
        var socioExistenteDni = await _socioRepository.GetByDniAsync(dto.Dni.Trim());
        if (socioExistenteDni is not null)
        {
            _logger?.LogWarning("Conflicto de duplicidad por DNI: {Dni}", dto.Dni);
            throw new DuplicateResourceException("El DNI o correo electrónico ya se encuentran registrados.");
        }

        var socioExistenteEmail = await _socioRepository.GetByEmailAsync(dto.Email.Trim());
        if (socioExistenteEmail is not null)
        {
            _logger?.LogWarning("Conflicto de duplicidad por Email: {Email}", dto.Email);
            throw new DuplicateResourceException("El DNI o correo electrónico ya se encuentran registrados.");
        }

        // 5. Creación del registro
        var socio = new Socio
        {
            Nombre = dto.Nombre.Trim(),
            Dni = dto.Dni.Trim(),
            Email = dto.Email.Trim(),
            Telefono = dto.Telefono?.Trim() ?? string.Empty,
            FechaNacimiento = dto.FechaNacimiento,
            Activo = true
        };

        var creado = await _socioRepository.CreateAsync(socio);
        _logger?.LogInformation("Socio registrado exitosamente con ID: {SocioId}", creado.Id);
        return MapToResponseDTO(creado);
    }

    public async Task<SocioResponseDTO> UpdateAsync(Guid id, SocioUpdateDTO dto)
    {
        _logger?.LogInformation("Actualizando socio ID: {SocioId}", id);
        var socio = await _socioRepository.GetByIdAsync(id);
        if (socio is null)
        {
            throw new NotFoundException($"No se encontró el socio con identificador {id}.");
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new GymPlatform.Shared.Exceptions.ValidationException("El nombre del socio es obligatorio.");
        }

        socio.Nombre = dto.Nombre.Trim();
        socio.Telefono = dto.Telefono?.Trim() ?? string.Empty;
        socio.Activo = dto.Activo;

        await _socioRepository.UpdateAsync(socio);
        _logger?.LogInformation("Socio ID {SocioId} actualizado.", id);
        return MapToResponseDTO(socio);
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger?.LogInformation("Ejecutando borrado lógico (Soft Delete) para el socio ID: {SocioId}", id);
        var socio = await _socioRepository.GetByIdAsync(id);
        if (socio is null)
        {
            throw new NotFoundException($"No se encontró el socio con identificador {id}.");
        }

        socio.Activo = false;
        await _socioRepository.UpdateAsync(socio);
        _logger?.LogInformation("Socio ID {SocioId} marcado como inactivo (soft deleted).", id);
    }

    private static SocioResponseDTO MapToResponseDTO(Socio socio)
    {
        return new SocioResponseDTO(
            socio.Id,
            socio.Nombre,
            socio.Dni,
            socio.Email,
            socio.Telefono,
            socio.FechaNacimiento,
            socio.FechaRegistro,
            socio.Activo
        );
    }
}