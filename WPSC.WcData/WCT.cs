using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WPSC.WcData
{
    public class WCT
    {
        public const uint MagicNumber = 0x80000004;

        public uint FormatVersion { get; set; } = 1;
        public string MainComment { get; set; } = "";
        public string MainCT { get; set; } = "";
        public List<string> Triggers { get; set; } = new List<string>();

        public WCT(Stream from)
        {
            using var reader = new WcDataReader(from);
            if (reader.ReadUInt32() != MagicNumber)
                throw new Exception("Invalid wct file: invalid magic number");

            FormatVersion = reader.ReadUInt32();
            if (FormatVersion != 0 && FormatVersion != 1)
                throw new Exception("Unexpected format version of wct file.");

            if (FormatVersion != 0)
            {
                MainComment = reader.ReadString();
                MainCT = reader.ReadPrefixedString();
            }

            var cts = new List<string>();
            while (from.Position < from.Length)
                cts.Add(reader.ReadPrefixedString());
            Triggers = cts.ToList();
        }

        public void Save(Stream to)
        {
            using var writer = new WcDataWriter(to);
            writer.Write(MagicNumber);
            writer.Write(FormatVersion);

            if (FormatVersion != 0)
            {
                writer.Write(MainComment);
                writer.WritePrefixed(MainCT);
            }

            foreach (var ct in Triggers)
                writer.WritePrefixed(ct);
        }
    }
}
