using System.Security.Cryptography;

namespace Odin.Core.Cryptography;

public static class AesEncryption
{
	private static readonly byte[] IV =
	{
		0x21, 0x07, 0x88, 0x04, 0x05, 0x06, 0x07, 0x08,
		0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
	};

	public static string Encrypt(string passphrase, string plainText)
		=> Encrypt(passphrase, Encoding.Unicode.GetBytes(plainText));

	public static string Encrypt(string passphrase, byte[] plainText)
	{
		using var aes = Aes.Create();
		aes.Key = Encoding.UTF8.GetBytes(passphrase);
		aes.IV = IV;

		using var output = new MemoryStream();
		using var cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
		cryptoStream.Write(plainText);
		cryptoStream.FlushFinalBlock();

		var array = output.ToArray();
		return Convert.ToBase64String(array);
	}


	public static Task<string> EncryptAsync(string passphrase, string plainText)
		=> EncryptAsync(passphrase, Encoding.Unicode.GetBytes(plainText));

	public static async Task<string> EncryptAsync(string passphrase, byte[] plainText)
	{
		using var aes = Aes.Create();
		aes.Key = Encoding.UTF8.GetBytes(passphrase);
		aes.IV = IV;

		using var output = new MemoryStream();
		await using var cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
		await cryptoStream.WriteAsync(plainText);
		await cryptoStream.FlushFinalBlockAsync();

		var array = output.ToArray();
		return Convert.ToBase64String(array);
	}

	public static string Decrypt(string passphrase, string cipherText, Encoding? encoding = null)
		=> Decrypt(passphrase, Convert.FromBase64String(cipherText), encoding);

	public static string Decrypt(string passphrase, byte[] cipherText, Encoding? encoding = null)
	{
		encoding ??= Encoding.Unicode;
		using var aes = Aes.Create();
		aes.Key = Encoding.UTF8.GetBytes(passphrase);
		//aes.Key = DeriveKeyFromPassword(passphrase);
		aes.IV = IV;
		using var input = new MemoryStream(cipherText);
		using var cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
		using var output = new MemoryStream();
		cryptoStream.CopyTo(output);
		return encoding.GetString(output.ToArray());
	}

	public static Task<string> DecryptAsync(string passphrase, string cipherText, Encoding? encoding = null)
		=> DecryptAsync(passphrase, Convert.FromBase64String(cipherText), encoding);

	public static async Task<string> DecryptAsync(string passphrase, byte[] cipherText, Encoding? encoding = null)
	{
		encoding ??= Encoding.Unicode;
		using var aes = Aes.Create();
		aes.Key = Encoding.UTF8.GetBytes(passphrase);
		//aes.Key = DeriveKeyFromPassword(passphrase);
		aes.IV = IV;
		using var input = new MemoryStream(cipherText);
		await using var cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
		using var output = new MemoryStream();
		await cryptoStream.CopyToAsync(output);
		return encoding.GetString(output.ToArray());
	}

	/// <summary>
	/// Converts text to encrypted.
	/// </summary>
	/// <param name="value">Value to encrypt.</param>
	/// <param name="passphrase">Passphrase for encryption.</param>
	/// <returns></returns>
	public static string ToEncrypted(this string value, string passphrase)
		=> Encrypt(passphrase, value);

	/// <summary>
	/// Converts from encrypted cipher text to text.
	/// </summary>
	/// <param name="value">Value to decrypt.</param>
	/// <param name="passphrase">Passphrase for decryption.</param>
	public static string FromEncrypted(this string value, string passphrase)
		=> Decrypt(passphrase, value);

	/// <summary>
	/// Converts object to encrypted string (using json for serialization).
	/// </summary>
	/// <param name="value">Value to encrypt.</param>
	/// <param name="passphrase">Passphrase for encryption.</param>
	public static string ToEncryptedString(this object value, string passphrase)
		=> Encrypt(passphrase, value.ToJsonUtf8Bytes());

	/// <summary>
	/// Converts from encrypted cipher text to object (using json for serialization).
	/// </summary>
	/// <param name="value">Value to decrypt.</param>
	/// <param name="passphrase">Passphrase for decryption.</param>
	public static T? FromEncrypted<T>(this string value, string passphrase)
	{
		var text = Decrypt(passphrase, value, Encoding.UTF8);
		return text.FromJson<T>();
	}
}
