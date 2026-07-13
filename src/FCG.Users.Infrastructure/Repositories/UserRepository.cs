using FCG.Users.Domain.Entities;
using FCG.Users.Domain.Interfaces.Repositories;
using FCG.Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FCG.Users.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly UsersDbContext _context;

    public UserRepository(UsersDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(
    string email,
    CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _context.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Email.Address.ToLower() == normalizedEmail,
                cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Email.Address.ToLower() == normalizedEmail,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<User>> GetAllAsync(
    CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Email.Address)
            .ToListAsync(cancellationToken);


    }

}