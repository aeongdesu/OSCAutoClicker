using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OSCAutoClicker;

internal sealed class OscSender : IDisposable
{
    private readonly UdpClient _udp;

    public IPEndPoint Target { get; }

    public OscSender(string host, int port)
    {
        Target = new IPEndPoint(Resolve(host), port);
        _udp = new UdpClient(Target.AddressFamily);
        _udp.Connect(Target);
    }

    public void SendInt(string address, int value)
    {
        byte[] packet = Encode(address, value);
        _udp.Send(packet, packet.Length);
    }

    public static bool NothingListeningOn(IPEndPoint target)
    {
        IPAddress any = target.AddressFamily == AddressFamily.InterNetworkV6
            ? IPAddress.IPv6Any
            : IPAddress.Any;

        try
        {
            using var probe = new Socket(target.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            probe.Bind(new IPEndPoint(any, target.Port));
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static IPAddress Resolve(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new SocketException((int)SocketError.HostNotFound);
        if (IPAddress.TryParse(host, out IPAddress? parsed)) return parsed;

        IPAddress[] found = Dns.GetHostAddresses(host);
        foreach (IPAddress address in found)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork) return address;
        }
        if (found.Length > 0) return found[0];

        throw new SocketException((int)SocketError.HostNotFound);
    }

    private static byte[] Encode(string address, int value)
    {
        int addressLength = PaddedLength(address.Length + 1);
        int typeTagLength = PaddedLength(3);

        byte[] packet = new byte[addressLength + typeTagLength + 4];

        Encoding.ASCII.GetBytes(address, packet);
        packet[addressLength] = (byte)',';
        packet[addressLength + 1] = (byte)'i';

        int argument = addressLength + typeTagLength;
        packet[argument] = (byte)(value >> 24);
        packet[argument + 1] = (byte)(value >> 16);
        packet[argument + 2] = (byte)(value >> 8);
        packet[argument + 3] = (byte)value;

        return packet;
    }

    private static int PaddedLength(int length) => (length + 3) & ~3;

    public void Dispose() => _udp.Dispose();
}
