using AmbermoonServer.Data;
using AmbermoonServer.Data.Entities;
using AmbermoonServer.Enums;
using Microsoft.EntityFrameworkCore;
using AmbermoonServer.Controllers;

namespace AmbermoonServer.Services;

public class AdminService
(
    AppDbContext context
) : BaseService(context)
{
    public async Task CreateGameVersion(string gameVersion)
    {
        if (gameVersion.Length > 100)
            throw new ArgumentException("Game version is too long.");

        var entity = await Context.GameVersion.FirstOrDefaultAsync(v => v.Name == gameVersion);

        if (entity != null)
            return;

        entity = new GameVersion
        {
            Id = Guid.NewGuid(),
            Name = gameVersion
        };

        await Context.GameVersion.AddAsync(entity);
        await Context.SaveChangesAsync();
    }

    public async Task CreateLanguage(string language)
    {
        if (language.Length > 100)
            throw new ArgumentException("Language is too long.");

        var entity = await Context.Language.FirstOrDefaultAsync(l => l.Name == language);

        if (entity != null)
            return;

        entity = new Language
        {
            Id = Guid.NewGuid(),
            Name = language
        };

        await Context.Language.AddAsync(entity);
        await Context.SaveChangesAsync();
    }

    public async Task CreateSource(string source)
    {
        if (source.Length > 100)
            throw new ArgumentException("Source is too long.");

        var entity = await Context.Source.FirstOrDefaultAsync(s => s.Name == source);

        if (entity != null)
            return;

        entity = new Source
        {
            Id = Guid.NewGuid(),
            Name = source
        };

        await Context.Source.AddAsync(entity);
        await Context.SaveChangesAsync();
    }

    public async Task BanUser(string email)
    {
        var user = await Context.User.FirstOrDefaultAsync(user => user.Email == email)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Email.Equals(AdminRequirementHandler.AdminUser, StringComparison.CurrentCultureIgnoreCase))
            return; // This is not allowed, but we won't tell the user as it would know the admin email then.

        if (user.StateId != (int)UserStates.Deleted)
        {
            user.StateId = (int)UserStates.Banned;

            await Context.SaveChangesAsync();
        }
    }

    public async Task DeleteUser(string email)
    {
        var user = await Context.User.FirstOrDefaultAsync(user => user.Email == email)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Email.Equals(AdminRequirementHandler.AdminUser, StringComparison.CurrentCultureIgnoreCase))
            return; // This is not allowed, but we won't tell the user as it would know the admin email then.

        user.StateId = (int)UserStates.Deleted;

        await Context.SaveChangesAsync();
    }
}
