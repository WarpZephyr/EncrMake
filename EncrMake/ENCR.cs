using EncrMake.Helpers;
using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EncrMake
{
    public class ENCR
    {
        private const int HeaderLength = 0x10;
        private const int NameLength = 0x100;
        private const int HashLength = 0x10;
        private const int Alignment = 0x10;
        private const int Magic = 0x454E4352; // "ENCR"

        public string Name { get; init; }
        public byte[] Bytes { get; init; }

        public ENCR(string name, byte[] bytes)
        {
            Name = name;
            Bytes = bytes;
        }

        #region Serialization

        public static ENCR Read(byte[] bytes)
        {
            Span<byte> payload = bytes;

            int offset = 0;
            byte[] hash = new byte[HashLength];
            for (int i = 0; i < HashLength; i++)
            {
                hash[i] = payload[offset++];
            }

            int magic = BinaryPrimitives.ReadInt32BigEndian(payload[offset..]);
            offset += sizeof(int);
            Assert(magic == Magic, "Unknown file signature.");

            int unk04 = BinaryPrimitives.ReadInt16LittleEndian(payload[offset..]);
            offset += sizeof(short);
            Assert(unk04 == 1, $"{unk04} != {1}");

            int unk06 = BinaryPrimitives.ReadInt16LittleEndian(payload[offset..]);
            offset += sizeof(short);
            Assert(unk06 == 1, $"{unk06} != {1}");

            int length = BinaryPrimitives.ReadInt32LittleEndian(payload[offset..]);
            offset += sizeof(int);
            byte[] data = new byte[length];

            int unk0C = BinaryPrimitives.ReadInt32LittleEndian(payload[offset..]);
            offset += sizeof(int);
            Assert(unk0C == 0, $"{unk0C} != {0}");

            int strlen = StringHelper.StrlenFixed(payload[offset..], NameLength);
            string name = Encoding.UTF8.GetString(bytes, offset, strlen);
            offset += NameLength;

            Assert((payload.Length - offset) >= length, "Not enough remaining length for specified payload size.");
            for (int i = 0; i < length; i++)
            {
                data[i] = payload[offset++];
            }

            byte[] dataHash = MD5.HashData(data);
            for (int i = 0; i < HashLength; i++)
            {
                if (hash[i] != dataHash[i])
                {
                    throw new InvalidDataException("Hashing failed, data is corrupt.");
                }
            }

            return new ENCR(name, data);
        }

        public byte[] Write()
        {
            if (Name.Length > NameLength)
            {
                throw new ArgumentException($"Name too long: {Name.Length} > {NameLength}", nameof(Name));
            }

            int payloadLength = HashLength + HeaderLength + NameLength + MathHelper.BinaryAlign(Bytes.Length, Alignment);
            Span<byte> payload = new byte[payloadLength];

            int offset = 0;
            byte[] hash = MD5.HashData(Bytes);
            for (int i = 0; i < hash.Length; i++)
            {
                payload[offset++] = hash[i];
            }

            BinaryPrimitives.WriteInt32BigEndian(payload[offset..], Magic);
            offset += sizeof(int);

            BinaryPrimitives.WriteInt16LittleEndian(payload[offset..], 1);
            offset += sizeof(short);

            BinaryPrimitives.WriteInt16LittleEndian(payload[offset..], 1);
            offset += sizeof(short);

            BinaryPrimitives.WriteInt32LittleEndian(payload[offset..], Bytes.Length);
            offset += sizeof(int);
            offset += sizeof(int); // Is just null

            byte[] nameBytes = Encoding.UTF8.GetBytes(Name);
            int remainingNameLength = NameLength - nameBytes.Length;
            Debug.Assert(remainingNameLength >= 0, "Name length should have been checked already.");
            for (int i = 0; i < nameBytes.Length; i++)
            {
                payload[offset++] = nameBytes[i];
            }

            offset += remainingNameLength;
            for (int i = 0; i < Bytes.Length; i++)
            {
                payload[offset++] = Bytes[i];
            }

            return payload.ToArray();
        }

        #endregion

        #region Assertions

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException($"Assert failed: {message}");
            }
        }

        #endregion
    }
}
