using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AmbermoonServer.Data.Entities;

[Index(nameof(Code), IsUnique = true)]
public class UserState
{
	[Key]
	public required int Id { get; set; }

	[Required]
	[StringLength(100)]
	public required string Name { get; set; }

	[Required]
	[StringLength(10)]
	public required string Code { get; set; }
}
