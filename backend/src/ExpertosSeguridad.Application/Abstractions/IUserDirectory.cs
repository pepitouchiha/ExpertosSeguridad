using ExpertosSeguridad.Domain.ValueObjects;

namespace ExpertosSeguridad.Application.Abstractions;

/// <summary>
/// Fixed catalogue of test users, standing in for the identity provider the brief
/// explicitly rules out. Behind an interface so a real directory can replace it
/// without touching the use cases.
/// </summary>
public interface IUserDirectory
{
    IReadOnlyList<Actor> GetAll();

    Actor? Find(Guid id);
}
