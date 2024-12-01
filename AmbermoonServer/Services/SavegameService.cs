using AmbermoonServer.Data;
using AmbermoonServer.Data.Entities;
using AmbermoonServer.DTO;
using AmbermoonServer.Enums;
using Microsoft.EntityFrameworkCore;

namespace AmbermoonServer.Services;

public class SavegameService
(
	AppDbContext context
) : BaseService(context)
{
	public async Task StoreSavegames(string email, SavegameDto[] savegames)
	{
        var existingSavegames = await Context.Savegame
            .Include(s => s.User)
            .Include(s => s.Source)
            .Include(s => s.GameVersion)
            .Include(s => s.Language)
            .Where(s => s.User.Email == email)
            .ToListAsync();

        bool SameSavegame(Savegame savegame, SavegameDto savegameDto)
        {
            return
                savegame.Slot == savegameDto.Slot &&
                savegame.Source.Name == savegameDto.Source &&
                savegame.Language.Name == savegameDto.Language &&
                savegame.GameVersion.Name == savegameDto.GameVersion;
        }

        var checkedSources = new Dictionary<string, Guid>();
        var checkedLanguages = new Dictionary<string, Guid>();
        var checkedGameVersions = new Dictionary<string, Guid>();

        async Task<Guid> EnsureSource(string source)
        {
            var existingSource = await Context.Source.FirstOrDefaultAsync(s => s.Name == source)
                ?? throw new KeyNotFoundException("Unknown source.");

            return existingSource.Id;
        }

        async Task<Guid> EnsureLanguage(string language)
        {
            var existingLanguage = await Context.Language.FirstOrDefaultAsync(l => l.Name == language)
                ?? throw new KeyNotFoundException("Unknown language.");

            return existingLanguage.Id;
        }

        async Task<Guid> EnsureGameVersion(string gameVersion)
        {
            var existingGameVersion = await Context.GameVersion.FirstOrDefaultAsync(v => v.Name == gameVersion)
                ?? throw new KeyNotFoundException("Unknown game version.");

            return existingGameVersion.Id;
        }

        User? user = null;
        var now = DateTime.UtcNow;

        foreach (var savegame in savegames)
        {
            var existingSavegame = existingSavegames.FirstOrDefault(s => SameSavegame(s, savegame));

            if (existingSavegame != null)
            {
                // update
                existingSavegame.Data = savegame.Data;
                existingSavegame.UpdateTimestamp = now;
            }
            else
            {
                // create sources, languages and game versions if they don't exist yet

                if (!checkedSources.TryGetValue(savegame.Source, out Guid source))
                {
                    source = await EnsureSource(savegame.Source);
                    checkedSources.Add(savegame.Source, source);
                }

                if (!checkedLanguages.TryGetValue(savegame.Language, out Guid language))
                {
                    language = await EnsureLanguage(savegame.Language);
                    checkedLanguages.Add(savegame.Language, language);
                }

                if (!checkedGameVersions.TryGetValue(savegame.GameVersion, out Guid gameVersion))
                {
                    gameVersion = await EnsureGameVersion(savegame.GameVersion);
                    checkedGameVersions.Add(savegame.GameVersion, gameVersion);
                }

                if (user == null)
                {
                    user = Context.User.First(u => u.Email == email);
                    user.StateId = (int)UserStates.Active;
                }

                // create
                await Context.Savegame.AddAsync(new Savegame
                {
                    CreateTimestamp = now,
                    UpdateTimestamp = now,
                    Data = savegame.Data,
                    Slot = savegame.Slot,
                    UserId = user.Id,
                    SourceId = source,
                    GameVersionId = gameVersion,
                    LanguageId = language,
                });
            }

            await Context.SaveChangesAsync();
        }
    }

    public async Task<SavegameDto[]> GetSavegames(string email)
    {
        var savegames = await Context.Savegame.Include(s => s.User).Where(savegame => savegame.User.Email == email).ToArrayAsync();

        if (savegames.Length == 0)
            return [];

        var sources = new Dictionary<Guid, string>();
        var gameVersions = new Dictionary<Guid, string>();
        var languages = new Dictionary<Guid, string>();

        string GetOrAddProperty(Dictionary<Guid, string> dict, Guid id, Func<Guid, string> add)
        {
            if (!dict.TryGetValue(id, out var value))
            {
                value = add(id);
                dict.Add(id, value);
            }

            return value;
        }

        SavegameDto Convert(Savegame savegame)
        {
            return new SavegameDto
            {
                Data = savegame.Data,
                Slot = savegame.Slot,
                GameVersion = GetOrAddProperty(gameVersions, savegame.GameVersionId, id => Context.GameVersion.Find(id)?.Name ?? string.Empty),
                Language = GetOrAddProperty(languages, savegame.LanguageId, id => Context.Language.Find(id)?.Name ?? string.Empty),
                Source = GetOrAddProperty(sources, savegame.SourceId, id => Context.Source.Find(id)?.Name ?? string.Empty),
            };
        }

        return savegames.Select(Convert).ToArray();
    }
}
