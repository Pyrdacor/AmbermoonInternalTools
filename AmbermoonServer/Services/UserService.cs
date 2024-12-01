using AmbermoonServer.Data;
using AmbermoonServer.Data.Entities;
using AmbermoonServer.Templates;
using AmbermoonServer.Enums;
using Microsoft.EntityFrameworkCore;

namespace AmbermoonServer.Services;

using Templates = Templates.Templates;

public class UserService
(
	AppDbContext context,
	EmailService emailService,
	TemplateService templateService,
	IHttpContextAccessor httpContextAccessor
) : BaseService(context)
{
	public async Task RegisterUser(string email)
	{
        if (!EmailService.IsEmailValid(email))
			throw new ArgumentException("Invalid email address.");

        var user = await Context.User.FirstOrDefaultAsync(user => user.Email == email);

		if (user != null)
			return;

		string code = CodeService.GenerateCode();
		Guid verificationGuid = Guid.NewGuid();
		var now = DateTime.UtcNow;

		user = new User
		{
			Email = email,
			Code = code,
			VerificationGuid = verificationGuid,
			CreateTimestamp = now,
			UpdateTimestamp = now,
			StateId = (int)UserStates.Created,
		};

		var request = httpContextAccessor.HttpContext!.Request;
		string baseAddress = $"{request.Scheme}://{request.Host}{request.PathBase}";
		baseAddress = baseAddress.TrimEnd('/');

		await emailService.SendEmailAsync(email, Subjects.Verification, Templates.Verification, new VerificationEmailModel
		{
			VerificationUrl = $"{baseAddress}/user/verify?email={email}&token={verificationGuid}"
		});

        await Context.User.AddAsync(user);
        await Context.SaveChangesAsync();
    }

    public async Task<string> VerifyUser(string email, Guid verificationGuid)
    {
        var user = await Context.User
            .FirstOrDefaultAsync(user => user.Email == email);

        async Task<string> GetDefaultResponse()
		{
            return await templateService.RenderTemplateAsync(Templates.VerificationInProgress);
        }

        if (user == null)
            return await GetDefaultResponse();

		if (user.StateId != (int)UserStates.Created)
            return await GetDefaultResponse();

        // After 15 minutes the verification guid/token expires
        if (user.CreateTimestamp < DateTime.UtcNow.AddMinutes(-15))
		{
            Context.User.Remove(user);
            await Context.SaveChangesAsync();

            return await templateService.RenderTemplateAsync(Templates.VerificationLinkExpired);
        }            

        if (user.VerificationGuid != verificationGuid)
            return await GetDefaultResponse();

        await emailService.SendEmailAsync(email, Subjects.CodeRequest, Templates.Code, new CodeEmailModel
		{
            Email = email,
            Code = user.Code,
            QRCode = CodeService.GenerateQRCode(user.Code)
        });

        user.StateId = (int)UserStates.Verified;
        await Context.SaveChangesAsync();

		return await GetDefaultResponse();
    }

    public async Task<bool> IsAllowedToRequest(string email, string token)
	{
		var user = await Context.User
			.Include(user => user.State)
			.FirstOrDefaultAsync(user => user.Email == email);

		if (user == null)
			return false;

		var code = CodeService.DecodeToken(token);

		return user.State.Id switch
		{
			(int)UserStates.Verified or (int)UserStates.Active => user.Code == code,
			_ => false,
		};
	}

    public async Task RequestCode(string email)
    {
        if (!EmailService.IsEmailValid(email))
            throw new ArgumentException("Invalid email address.");

        var user = await Context.User.FirstOrDefaultAsync(user => user.Email == email);

        if (user == null)
            return;

        var now = DateTime.UtcNow;

        if (user.LastCodeRequest != null && now - user.LastCodeRequest.Value < TimeSpan.FromMinutes(15))
            return; // limit code requests to once every 15 minutes

        Guid requestCodeGuid = Guid.NewGuid();

        var request = httpContextAccessor.HttpContext!.Request;
        string baseAddress = $"{request.Scheme}://{request.Host}{request.PathBase}";
        baseAddress = baseAddress.TrimEnd('/');

        await emailService.SendEmailAsync(email, Subjects.CodeRequest, Templates.CodeRequest, new CodeRequestEmailModel
        {
            RequestCodeUrl = $"{baseAddress}/user/code?email={email}&token={requestCodeGuid}"
        });

        user.LastCodeRequest = now;
        user.VerificationGuid = requestCodeGuid;

        await Context.SaveChangesAsync();
    }

    public async Task<string> CodeRequest(string email, Guid codeRequestGuid)
    {
        var user = await Context.User
            .FirstOrDefaultAsync(user => user.Email == email);

        async Task<string> GetDefaultResponse()
        {
            return await templateService.RenderTemplateAsync(Templates.CodeRequestInProgress);
        }

        if (user == null)
            return await GetDefaultResponse();

        if (user.StateId < (int)UserStates.Verified)
            return await GetDefaultResponse();

        if (user.LastCodeRequest == null)
            return await GetDefaultResponse();

        // After 15 minutes the code request guid/token expires
        if (user.LastCodeRequest < DateTime.UtcNow.AddMinutes(-15))
        {
            return await templateService.RenderTemplateAsync(Templates.CodeRequestLinkExpired);
        }

        if (user.VerificationGuid != codeRequestGuid)
            return await GetDefaultResponse();

        await emailService.SendEmailAsync(email, Subjects.Code, Templates.Code, new CodeEmailModel
        {
            Email = email,
            Code = user.Code,
            QRCode = CodeService.GenerateQRCode(user.Code)
        });

        return await GetDefaultResponse();
    }
}
