#if __IOS__
using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UIKit;

namespace Zeroconf
{
	public enum SockAddrFamily
	{
		Inet = 2,
		Inet6 = 23
	}

	[StructLayout(LayoutKind.Explicit, Size = 28)]
	public struct Sockaddr
	{
		[FieldOffset(0)] public byte sin_len;
		[FieldOffset(1)] public byte sin_family;
		[FieldOffset(2)] public short sin_port;
		[FieldOffset(4)] public int sin_addr;

		// IPv6
		[FieldOffset(4)] public uint sin6_flowinfo;
		[FieldOffset(8)] [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] sin6_addr8;
		[FieldOffset(24)] public uint sin6_scope_id;

		public static Sockaddr CreateSockaddr(IntPtr bytes)
        {
			Sockaddr sock = (Sockaddr)Marshal.PtrToStructure(bytes, typeof(Sockaddr));
			return sock;
		}

		public static IPAddress CreateIPAddress(Sockaddr addr)
        {
			byte[] bytes = null;

			switch (addr.sin_family)
            {
				case (byte)SockAddrFamily.Inet:
					byte[] ipv4addr = new byte[4];
					ipv4addr[0] = (byte)(addr.sin_addr & 0x000000FF);
					ipv4addr[1] = (byte)((addr.sin_addr & 0x0000FF00) >> 8);
					ipv4addr[2] = (byte)((addr.sin_addr & 0x00FF0000) >> 16);
					ipv4addr[3] = (byte)((addr.sin_addr & 0xFF000000) >> 24);
					bytes = ipv4addr;
					break;
				case (byte)SockAddrFamily.Inet6:
					bytes = addr.sin6_addr8;
					break;
				default:
#if false
                    Console.WriteLine($"Unknown socket address family {addr.sin_family}");
#endif
                    bytes = null;
                    break;
            }

			return (bytes != null) ? new IPAddress(bytes): null;
        }
    }
}
#endif