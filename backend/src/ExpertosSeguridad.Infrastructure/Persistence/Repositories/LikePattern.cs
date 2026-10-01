namespace ExpertosSeguridad.Infrastructure.Persistence.Repositories;

/// <summary>
/// Compartido por todos los repositorios que ofrecen búsqueda de texto, para escribir una sola
/// vez la regla de escape.
/// </summary>
internal static class LikePattern
{
    /// <summary>
    /// Neutraliza los comodines de LIKE que escriba el usuario, para que buscar "50%" sea una búsqueda
    /// literal. Se usa junto con <c>'\'</c> como carácter de escape en <c>EF.Functions.ILike</c>.
    /// </summary>
    public static string Escape(string term) => term
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_");
}
