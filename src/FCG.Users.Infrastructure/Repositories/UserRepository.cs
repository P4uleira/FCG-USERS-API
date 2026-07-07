using FCG.Users.Domain.Entities;
using FCG.Users.Domain.Interfaces;
using FCG.Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FCG.Users.Infrastructure.Repositories;

public class UserRepository : IUserRepository
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

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();

        return await _context.Users
            .FirstOrDefaultAsync(user => user.Email.Address == normalizedEmail, cancellationToken);
    }
}