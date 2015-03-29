using UnityEngine;
using System.Collections;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
//using System.Threading.Tasks;

public static class Encrypter
{
    // This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
    // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
    // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
	private static readonly byte[] initVectorBytes = Encoding.ASCII.GetBytes("l1M6O#e2S*q8#fd%");
	private static readonly byte[] saltBytes = Encoding.ASCII.GetBytes("zmaCe$0x*E7C6^op");
	
    // This constant is used to determine the keysize of the encryption algorithm.
	private const int keysize = 256;
	
	public static string Encrypt(string plainText, string passPhrase)
	{
		byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
		Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(passPhrase, saltBytes);
		byte[] keyBytes = password.GetBytes(keysize / 8);
		using (RijndaelManaged symmetricKey = new RijndaelManaged())
		{
			symmetricKey.Mode = CipherMode.CBC;
			using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes))
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
					{
						cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
						cryptoStream.FlushFinalBlock();
						byte[] cipherTextBytes = memoryStream.ToArray();
						return Convert.ToBase64String(cipherTextBytes);
					}
				}
			}
		}
	}
	
	public static string Decrypt(string cipherText, string passPhrase)
	{
		byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
		Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(passPhrase, saltBytes);
		byte[] keyBytes = password.GetBytes(keysize / 8);
		using(RijndaelManaged symmetricKey = new RijndaelManaged())
		{
			symmetricKey.Mode = CipherMode.CBC;
			using(ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
			{
				using(MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
				{
					using(CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
					{
						byte[] plainTextBytes = new byte[cipherTextBytes.Length];
						int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
						return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
					}
				}
			}
		}
	}
}
