namespace GymPlatform.Shared.Exceptions;

/// <summary>
/// Excepción lanzada cuando ocurre un error en el proceso de autenticación o credenciales inválidas.
/// </summary>
public class AuthenticationException : GymException
{
    public AuthenticationException(string message) : base(message) { }
    public AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
}
