namespace AmbermoonServer.DTO;

public class SavegameDto
{
    // Advanced or not, game data version, etc
    public required string GameVersion { get; set; }

    public required string Language { get; set; }

    // Operating system, app version, etc
    public required string Source { get; set; }

    public required int Slot { get; set; }

    public required byte[] Data { get; set; }
}

public class SavegamesDto
{
    public required SavegameDto[] Savegames { get; set; }
}
