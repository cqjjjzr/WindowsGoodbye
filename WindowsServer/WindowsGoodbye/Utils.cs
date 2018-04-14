using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace WindowsGoodbye
{
    static class Utils
    {
        public const int GuidLength = 16;

        public static void Write(this Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }
    }

    static class CryptoTools
    {
        public const int AesKeyLength = 256;

        public static byte[] GenerateAESKey()
        {
            var buf = CryptographicBuffer.GenerateRandom(AesKeyLength);
            CryptographicBuffer.CopyToByteArray(buf, out var ret);
            return ret;
        }
    }
}
