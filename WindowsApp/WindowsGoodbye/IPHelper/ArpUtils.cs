using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace IPHelper
{
    public class ArpUtils
    {
        public static string GetMacFromIp(string ip)
        {
            var ipInt = IPUtils.DecIpToInt(ip);
            Int32 length = 6;
            var mac = new byte[6];
            SendARP(ipInt, 0, mac, ref length);

            return IPUtils.MacToHexStr(mac);
        }

        public static string GetIpFromMac(string mac)
        {
            int size = 0;
            GetIpNetTable(IntPtr.Zero, ref size, true);
            var p = Marshal.AllocHGlobal(size);

            List<MIB_IPNETROW> arpList = new List<MIB_IPNETROW>();
            if (GetIpNetTable(p, ref size, true) == 0)
            {
                var num = Marshal.ReadInt32(p);
                var ptr = IntPtr.Add(p, 4);
                for (int i = 0; i < num; i++)
                {
                    arpList.Add((MIB_IPNETROW)Marshal.PtrToStructure(ptr, typeof(MIB_IPNETROW)));
                    ptr = IntPtr.Add(ptr, Marshal.SizeOf(typeof(MIB_IPNETROW)));
                }
            }
            Marshal.FreeHGlobal(p);

            return IPUtils.IntIpToDecStr(arpList
                .Find(ipnetrow => IPUtils.MacToHexStr(ipnetrow.PhysAddr) == mac.ToUpper().Replace(':', '-')).Addr);
        }

        [StructLayout(LayoutKind.Sequential)]
        // ReSharper disable once InconsistentNaming
        public struct MIB_IPNETROW
        {
            public int Index;
            public int PhysAddrLen;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] PhysAddr;
            public int Addr;
            public int Type;
        }

        [DllImport("Iphlpapi.dll")]
        static extern int SendARP(Int32 DestIP, Int32 SrcIP, byte[] mac, ref Int32 PhyAddrLen);
        
        [DllImport("iphlpapi.dll")]
        static extern int GetIpNetTable(IntPtr pTcpTable, ref int pdwSize, bool bOrder);
    }
}
