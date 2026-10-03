using System.Text.RegularExpressions;
using GymPlatform.BusinessLogic.Interfaces;
using GymPlatform.DataAccess.Entities;
using GymPlatform.DataAccess.Repositories;
using GymPlatform.Shared.DTOs;
using GymPlatform.Shared.Exceptions;

namespace GymPlatform.BusinessLogic.Services;

public class SocioService : ISocioService
{
    private readonly ISocioRepository _socioRepository;
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public SocioService(ISocioRepository socioRepository)
    {
        _socioRepository = socioRepository;
    }

    public async Task<IEnumerable<SocioResponseDTO>> GetAllAsync()
    {
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
        var socio = await _socioRepository.GetByIdAsync(id);
        if (socio is null)
        {
            throw new NotFoundException($"No se encontró el socio con identificador {id}.");
        }
        return MapToResponseDTO(socio);
    }

    public async Task<SocioResponseDTO> CreateAsync(SocioCreateDTO dto)
    {
        // 1. Validaciones básicas (HTTP 400)
        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new ValidationException("El nombre del socio es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Dni))
        {
            throw new ValidationException("El DNI del socio es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.Email) || !EmailRegex.IsMatch(dto.Email))
        {
            throw new ValidationException("El formato del correo electrónico es inválido.");
        }

        // RN-01: Mayoría de edad (18 años o más)
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var edad = hoy.Year - dto.FechaNacimiento.Year;
        if (dto.FechaNacimiento > hoy.AddYears(-edad))
        {
            edad--;
        }

        if (edad < 18)
        {
            throw new ValidationException("El socio debe ser mayor de 18 años para registrarse.");
        }

        // RN-01: Verificación de duplicados (HTTP 409)
        var socioExistenteDni = await _socioRepository.GetByDniAsync(dto.Dni.Trim());
        if (socioExistenteDni is not null)
        {
            throw new DuplicateResourceException("El DNI o correo electrónico ya se encuentran registrados.");
        }

        var socioExistenteEmail = await _socioRepository.GetByEmailAsync(dto.Email.Trim().ToLower());
        if (socioExistenteEmail is not null)
        {
            throw new DuplicateResourceException("El DNI o correo electrónico ya se encuentran registrados.");
        }

        var socio = new Socio
        {
            Nombre = dto.Nombre.Trim(),
            Dni = dto.Dni.Trim(),
            Email = dto.Email.Trim().ToLower(),
            Telefono = dto.Telefono?.Trim() ?? string.Empty,
            FechaNacimiento = dto.FechaNacimiento,
            Activo = true
        };

        var creado = await _socioRepository.CreateAsync(socio);
        return MapToResponseDTO(creado);
    }

    public async Task<SocioResponseDTO> UpdateAsync(Guid id, SocioUpdateDTO dto)
    {
        var socio = await _socioRepository.GetByIdAsync(id);
        if (socio is null)
        {
            throw new NotFoundException($"No se encontró el socio con identificador {id}.");
        }

        if (string.IsNullOrWhiteSpace(dto.Nombre))
        {
            throw new ValidationException("El nombre del socio es obligatorio.");
        }

        socio.Nombre = dto.Nombre.Trim();
        socio.Telefono = dto.Telefono?.Trim() ?? string.Empty;
        socio.Activo = dto.Activo;

        await _socioRepository.UpdateAsync(socio);
        return MapToResponseDTO(socio);
    }

    public async Task DeleteAsync(Guid id)
    {
        var socio = await _socioRepository.GetByIdAsync(id);
        if (socio is null)
        {
            throw new NotFoundException($"No se encontró el socio con identificador {id}.");
        }

        await _socioRepository.DeleteAsync(socio);
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
