namespace GymPlatform.Shared.Enums;

/// <summary>
/// Roles disponibles en el sistema GymUp.
/// </summary>
public enum RolUsuario
{
    /// <summary>Acceso total: gestión de usuarios, planes, reportes y configuración del sistema.</summary>
    Administrador,

    /// <summary>Gestión de rutinas, asistencias y horarios de clases.</summary>
    Entrenador,

    /// <summary>Registro de socios, cobros, renovaciones y atención al público.</summary>
    Recepcionista,

    /// <summary>Consulta de su propio perfil, plan y turnos asignados.</summary>
    Socio
}
