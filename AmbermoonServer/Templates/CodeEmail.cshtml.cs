namespace AmbermoonServer.Templates
{
    public class CodeEmailModel
    {
        public required string Email { get; set; }

        public required string Code { get; set; }

        public required string QRCode { get; set; }
    }
}
