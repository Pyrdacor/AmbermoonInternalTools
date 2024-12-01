using AmbermoonServer.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmbermoonServer.Data.Entities;

[Index(nameof(Email), IsUnique = true)]
public class User : ITimestampProvider
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid Id { get; set; }

	[Required]
	[StringLength(100)]
	public required string Email { get; set; }

	[Required]
	[StringLength(10)]
	public required string Code { get; set; }

	[Required]
	public required DateTime CreateTimestamp { get; set; }

	[Required]
	public required DateTime UpdateTimestamp { get; set; }

	public DateTime? LastCodeRequest { get; set; }

	public DateTime? LastCodeChangeRequest { get; set; }

	[Required]
	public required Guid VerificationGuid { get; set; }

	[Required]
	public required int StateId { get; set; }

	[ForeignKey(nameof(StateId))]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public UserState State { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}
