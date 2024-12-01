namespace AmbermoonServer.Templates
{
	public static class Templates
	{
		public const string Verification = $"{nameof(AmbermoonServer.Templates)}.VerificationEmail";
        public const string VerificationLinkExpired = $"{nameof(AmbermoonServer.Templates)}.VerificationLinkExpired";
        public const string VerificationInProgress = $"{nameof(AmbermoonServer.Templates)}.VerificationInProgress";
        public const string CodeRequest = $"{nameof(AmbermoonServer.Templates)}.CodeRequestEmail";
        public const string Code = $"{nameof(AmbermoonServer.Templates)}.CodeEmail";
        public const string CodeRequestInProgress = $"{nameof(AmbermoonServer.Templates)}.CodeRequestInProgress";
        public const string CodeRequestLinkExpired = $"{nameof(AmbermoonServer.Templates)}.CodeRequestLinkExpired";
    }
}
