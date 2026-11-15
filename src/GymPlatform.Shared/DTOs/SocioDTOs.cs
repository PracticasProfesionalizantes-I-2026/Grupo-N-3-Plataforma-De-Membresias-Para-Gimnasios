using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace GymPlatform.Shared.DTOs;

public record SocioCreateDTO(
    [Required(ErrorMessage = "El nombre del socio es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "El DNI del socio es obligatorio.")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "El DNI debe tener exactamente 8 dígitos.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "El DNI debe contener únicamente 8 dígitos numéricos.")]
    string Dni,

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico es inválido.")]
    [MaxLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
    string Email,

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [MaxLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    string Telefono,

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    DateOnly FechaNacimiento
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var minDate = new DateOnly(1920, 1, 1);
        if (FechaNacimiento < minDate)
            yield return new ValidationResult($"La fecha de nacimiento no puede ser anterior al {minDate:yyyy-MM-dd}.", new[] { nameof(FechaNacimiento) });

        var age = today.Year - FechaNacimiento.Year;
        if (FechaNacimiento > today.AddYears(-age)) age--;

        if (age < 12)
            yield return new ValidationResult("El socio debe ser mayor de 12 años para registrarse.", new[] { nameof(FechaNacimiento) });

        if (age > 110)
            yield return new ValidationResult("La fecha de nacimiento indica una edad superior a 110 años, lo cual no es válido.", new[] { nameof(FechaNacimiento) });

        var localPart = Email.Split('@')[0].ToLowerInvariant();
        var nameParts = Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                              .Select(p => p.ToLowerInvariant());
        if (!nameParts.Any(p => localPart.Contains(p)))
            yield return new ValidationResult("El correo electrónico debe contener parte del nombre o apellido del socio.", new[] { nameof(Email), nameof(Nombre) });
    }
}

public record SocioUpdateDTO(
    [Required(ErrorMessage = "El nombre del socio es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    string Nombre,

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [MaxLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    string Telefono,

    bool Activo
);

public record SocioResponseDTO(
    Guid Id,
    string Nombre,
    string Dni,
    string Email,
    string Telefono,
    DateOnly FechaNacimiento,
    DateTime FechaRegistro,
    bool Activo
);
