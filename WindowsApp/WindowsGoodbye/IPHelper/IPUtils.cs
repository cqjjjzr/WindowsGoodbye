using System;
using System.Runtime.InteropServices;

namespace IPHelper
{
    // ReSharper disable once InconsistentNaming
    public class IPUtils
    {
        public static string IntIpToDecStr(int addr)
        {
            if (addr == 0) return null;
            var b = BitConverter.GetBytes(addr);
            return $"{b[0]}.{b[1]}.{b[2]}.{b[3]}";
        }

        public static string MacToHexStr(byte[] m)
        {
            return $"{m[0]:x2}:{m[1]:x2}:{m[2]:x2}:{m[3]:x2}:{m[4]:x2}:{m[5]:x2}".ToUpper();
        }

        public static int DecIpToInt(string addr)
        {
            return inet_addr(addr);
        }

        [DllImport("Ws2_32.dll")]
        static extern Int32 inet_addr(string ipaddr);
    }
}
