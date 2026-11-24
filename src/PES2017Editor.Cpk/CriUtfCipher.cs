namespace PES2017Editor.Cpk;

/// <summary>
/// Decrypts the masked CRI UTF packet used by the PES CPKs in the local
/// installation. This is the small reversible stream transformation documented
/// by CriPakTools' DecryptUTF routine; it is kept as a format primitive so the
/// rest of the editor does not depend on that external tool.
/// </summary>
internal static class CriUtfCipher
{
    public static byte[] Decrypt(ReadOnlySpan<byte> input)
    {
        var result = new byte[input.Length];
        var multiplier = 0x0000655F;
        const int factor = 0x00004115;
        for (var index = 0; index < input.Length; index++)
        {
            result[index] = (byte)(input[index] ^ (byte)multiplier);
            multiplier = unchecked(multiplier * factor);
        }
        return result;
    }
}
