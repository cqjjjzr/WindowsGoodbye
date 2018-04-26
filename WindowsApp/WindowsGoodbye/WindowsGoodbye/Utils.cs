using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace WindowsGoodbye
{
    static class Utils
    {
        public const int GuidLength = 16;

        public static void Write(this Stream stream, byte[] buffer)
        {
            stream.Write(buffer, 0, buffer.Length);
        }

        public static string GetBackgroundI18n(string id)
        {
            return ResourceLoader.GetForCurrentView("BackgroundResources").GetString(id);
        }

        public static Brush Transparent = new SolidColorBrush {Opacity = 0.0};
        public static Brush Red = new SolidColorBrush(Colors.OrangeRed) { Opacity = 0.1 };
        public static Brush GetBackgroundBrushBuEnabled(bool x)
        {
            return x ? Transparent : Red;
        }

        public static double GetOpacityByEnabled(bool x)
        {
            return x ? 1.0 : 0.75;
        }
    }

    public class DeviceInfoBrushConverter: IValueConverter {
        public static Brush Transparent = new SolidColorBrush { Opacity = 0.0 };
        public static Brush Red = new SolidColorBrush(Colors.OrangeRed) { Opacity = 15.0 };

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is bool)) return null;
            return ((bool)value) ? Transparent : Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static class CryptoTools
    {
        public const int AESKeyLengthInBits = 256;
        public static readonly byte[] AESIV = { 0x43, 0x79, 0x43, 0x68, 0x61, 0x72, 0x6c, 0x69, 0x65, 0x4c, 0x61, 0x73, 0x6d, 0x43, 0x4c, 0x43 };

        public static byte[] GenerateAESKey()
        {
            var buf = CryptographicBuffer.GenerateRandom(AESKeyLengthInBits);
            CryptographicBuffer.CopyToByteArray(buf, out var ret);
            return ret;
        }

        public static byte[] DecryptAES(byte[] data, byte[] key)
        {
            using (var aes = CreateContext(key))
            {
                using (var memoryStream = new MemoryStream(data))
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        var ret = new byte[cryptoStream.Length];
                        cryptoStream.Read(ret, 0, (int) cryptoStream.Length);
                        return ret;
                    }
                }
            }
        }

        public static byte[] EncryptAES(byte[] data, byte[] key)
        {
            using (var aes = CreateContext(key))
            {
                using (var memoryStream = new MemoryStream(data))
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Read))
                    {
                        var ret = new byte[cryptoStream.Length];
                        cryptoStream.Read(ret, 0, (int)cryptoStream.Length);
                        return ret;
                    }
                }
            }
        }

        private static Aes CreateContext(byte[] key)
        {
            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = AESIV;
            return aes;
        }

        public static void Sync(this IAsyncAction oper)
        {
            var task = oper.AsTask();
            if (task.Status == TaskStatus.Created) task.Start();
            task.Wait();
        }
    }
}
