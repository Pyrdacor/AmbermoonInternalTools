namespace AmbermoonServer.Interfaces
{
	public interface ITimestampProvider
	{
		DateTime CreateTimestamp { get; set; }

		DateTime UpdateTimestamp { get; set; }
	}
}
