using AmbermoonServer.Data;

namespace AmbermoonServer.Services;

public abstract class BaseService
{
	public AppDbContext Context { get; }

	protected internal BaseService(AppDbContext context)
	{
		Context = context;
	}
}
