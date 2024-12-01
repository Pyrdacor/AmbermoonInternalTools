using AmbermoonServer.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmbermoonServer.Data.Entities;

public class Savegame : ITimestampProvider
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid Id { get; set; }

	[Required]
	public required Guid UserId { get; set; }

	[ForeignKey(nameof(UserId))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public User User { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

	[Required]
	// Advanced or not, game data version, etc
	public required Guid GameVersionId { get; set; }

    [ForeignKey(nameof(GameVersionId))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public GameVersion GameVersion { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    [Required]
	public required Guid LanguageId { get; set; }

    [ForeignKey(nameof(LanguageId))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Language Language { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    [Required]
	// Operating system, app version, etc
	public required Guid SourceId { get; set; }

    [ForeignKey(nameof(SourceId))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Source Source { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    [Required]
	public required int Slot { get; set; }

	[Required]
	public required DateTime CreateTimestamp { get; set; }

	[Required]
	public required DateTime UpdateTimestamp { get; set; }

	[Required]
	public required byte[] Data { get; set; }
}
