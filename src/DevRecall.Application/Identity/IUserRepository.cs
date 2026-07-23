using DevRecall.Domain.Identity;

namespace DevRecall.Application.Identity;

public interface IUserRepository
{
    Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    void Add(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
