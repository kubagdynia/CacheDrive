using System;
using System.Security.Cryptography;

namespace CacheDrive.Services;

internal static class HashString
{
    public static string HashKey(string key, string salt = "")
    {
        // Convert the string to a byte array first, to be processed
        var textBytes = System.Text.Encoding.UTF8.GetBytes(key + salt);
        var hashBytes = SHA256.HashData(textBytes);

        // Convert back to a string, removing the '-' that BitConverter adds
        return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
    }
}