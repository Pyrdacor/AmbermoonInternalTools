using System.ComponentModel.DataAnnotations;

namespace AmbermoonServer.Data.Entities;

public class Source
{
	[Key]
	public required Guid Id { get; set; }

	[Required]
	[StringLength(100)]
	public required string Name { get; set; }
}
