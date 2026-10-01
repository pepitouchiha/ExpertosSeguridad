namespace ExpertosSeguridad.Domain.Enums;

/// <summary>
/// Los lados del proceso que modela la aplicación: quien reporta un problema, quien lo atiende y
/// quien decide quién es quién. Las reglas de autorización se expresan contra este enum, nunca
/// contra una lista de identificadores de usuario.
/// </summary>
public enum UserRole
{
    /// <summary>Reporta solicitudes de mantenimiento y sigue las que le pertenecen.</summary>
    Requester = 0,

    /// <summary>Personal de la empresa: ve todas las solicitudes, las asigna y maneja su ciclo de vida.</summary>
    Staff = 1,

    /// <summary>
    /// Gestiona cuentas y roles, y puede supervisar todas las solicitudes, pero no las opera. Quien
    /// otorga permisos se mantiene separado de quien los usa (separación de funciones).
    /// </summary>
    Admin = 2
}
