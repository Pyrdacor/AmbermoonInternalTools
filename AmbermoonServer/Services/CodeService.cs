using QRCoder;
using System.Globalization;
using System.Text;

namespace AmbermoonServer.Services;

public static class CodeService
{
	private static readonly string CodeAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    internal static string GenerateCode()
	{
		var code = new StringBuilder();
		var random = new Random();

		for (int i = 0; i < 10; i++)
		{
			code.Append(CodeAlphabet[random.Next(0, CodeAlphabet.Length)]);
		}

		return code.ToString();
	}

    internal static string GenerateQRCode(string code)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(code, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new Base64QRCode(qrCodeData);
        string base64 = qrCode.GetGraphic(20);

        return $"data:image/png;base64,{base64}";
    }

    internal static string EncodeToken(string code)
    {
        var tokenBuilder = new List<string>();

        foreach (var ch in code)
        {
            int index = 1 + CodeAlphabet.IndexOf(ch) * 7;
            int left = index ^ 0xA5;
            int right = index ^ 0x5A;

            tokenBuilder.Insert(0, left.ToString("X2"));
            tokenBuilder.Add(right.ToString("X2"));
        }

        return tokenBuilder.Aggregate((a, b) => a + b);
    }

    internal static string DecodeToken(string token)
    {
        if (token.Length != 40)
            throw new ArgumentException("Invalid token length");

        var code = new char[10];

        for (int i = 0; i < 10; i++)
        {
            int leftValue = byte.Parse(token.Substring(i * 2, 2), NumberStyles.HexNumber);
            int rightValue = byte.Parse(token.Substring(40 - 2 - i * 2, 2), NumberStyles.HexNumber);

            if ((leftValue ^ 0xA5) != (rightValue ^ 0x5A))
                throw new ArgumentException("Invalid token");

            int index = (leftValue ^ 0xA5) - 1;

            if (index % 7 != 0)
                throw new ArgumentException("Invalid token");

            index /= 7;

            if (index < 0 || index >= CodeAlphabet.Length)
                throw new ArgumentException("Invalid token");

            code[9 - i] = CodeAlphabet[index];
        }

        return new string(code);
    }
}
