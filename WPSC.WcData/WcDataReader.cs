using System;
using System.IO;
using System.Text;

namespace WPSC.WcData
{
    public class WcDataReader : BinaryReader
    {
        public WcDataReader(Stream input) : base(input) { }
        public WcDataReader(Stream input, Encoding encoding) : base(input, encoding) { }
        public WcDataReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

        public override string ReadString()
        {
            var c = ReadChar();
            var sb = new StringBuilder();

            while (c != '\0')
            {
                sb.Append(c);
                c = ReadChar();
            }

            return sb.ToString();
        }

        public override bool ReadBoolean()
        {
            var b = ReadUInt32();
            return b != 0;
        }

        public string ReadPrefixedString()
        {
            var size = ReadUInt32();
            var read = ReadBytes((int)size);
            if (read[^1] != 0)
                throw new Exception("Null terminator expected.");
            return Encoding.UTF8.GetString(new Span<byte>(read, 0, read.Length - 1));
        }
    }
}
