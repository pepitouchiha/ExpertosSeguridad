namespace ExpertosSeguridad.Domain.Enums;

/// <summary>
/// El orden de los miembros importa: cuando dos eventos comparten timestamp, el historial se
/// ordena por este valor. Asignar una solicitud pendiente la inicia, así que ambas entradas se
/// registran en el mismo instante, y la asignación debe leerse antes que el cambio de estado que
/// provocó.
///
/// Reordenar los miembros es seguro porque la columna guarda el nombre, no el número.
/// </summary>
public enum HistoryEventType
{
    Created = 0,
    ResponsibleChanged = 1,
    StatusChanged = 2
}
