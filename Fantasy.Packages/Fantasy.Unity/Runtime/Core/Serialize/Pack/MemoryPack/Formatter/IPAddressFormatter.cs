using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using Fantasy;
using MemoryPack;

public sealed class IPAddressFormatter : MemoryPackFormatter<IPAddress>
{
#if FANTASY_UNITY
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, ref IPAddress? value)
#else
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref IPAddress? value)
#endif
    {
        if (value == null)
        {
            writer.WriteUnmanaged((byte)0); // null flag
            return;
        }

        writer.WriteUnmanaged((byte)1); // not null

        byte[] bytes = value.GetAddressBytes();
        writer.WriteUnmanagedSpan(bytes.AsSpan());
    }

#if FANTASY_UNITY
    public override void Deserialize(ref MemoryPackReader reader, ref IPAddress? value)
#else
    public override void Deserialize(ref MemoryPackReader reader, scoped ref IPAddress? value)
#endif
    {
        byte nullFlag = reader.ReadUnmanaged<byte>();
        if (nullFlag == 0)
        {
            value = null;
            return;
        }

        if (!reader.TryReadCollectionHeader(out int length))
            throw new MemoryPackSerializationException("Invalid IP header");

        if (length != 4 && length != 16)
            throw new MemoryPackSerializationException($"Invalid IP length: {length}");

        ref byte spanRef = ref reader.GetSpanReference(length);
        ReadOnlySpan<byte> span = MemoryMarshal.CreateReadOnlySpan(ref spanRef, length);

        value = new IPAddress(span);
        reader.Advance(length);
    }
}

/// <summary>
/// 测试IPAddress序列化与反序列化
/// </summary>
public static class MemoryPackIPAdressTest
{
    public static void Run()
    {
        var list = new List<IPAddress>
        {
            IPAddress.Parse("127.0.0.1"),
            IPAddress.Parse("192.168.1.1")
        };

        Log.Debug("===== ORIGINAL =====");
        foreach (var ip in list)
        {
            Log.Debug(ip.ToString());
        }

        // 序列化
        var bytes = MemoryPackSerializer.Serialize(list);

        Log.Debug("===== SERIALIZED BYTES =====");
        Log.Debug("Length: " + bytes.Length);
        Log.Debug(BitConverter.ToString(bytes));

        // 反序列化
        var result = MemoryPackSerializer.Deserialize<List<IPAddress>>(bytes);

        Log.Debug("===== DESERIALIZED =====");
        if (result == null)
        {
            Log.Debug("Result is NULL");
            return;
        }

        foreach (var ip in result)
        {
            Log.Debug(ip.ToString());
        }
    }
}