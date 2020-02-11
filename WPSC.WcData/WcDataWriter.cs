using System.IO;
using System.Text;

namespace WPSC.WcData
{
    public class WcDataWriter : BinaryWriter
    {
        public WcDataWriter(Stream output) : base(output) { }
        public WcDataWriter(Stream output, Encoding encoding) : base(output, encoding) { }
        public WcDataWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen) { }
        protected WcDataWriter() { }

        public override void Write(bool value) => base.Write((uint)(value ? 1 : 0));

        public override void Write(string value)
        {
            Write(Encoding.UTF8.GetBytes(value));
            Write(char.MinValue);
        }

        public void WritePrefixed(string value)
        {
            Write(value.Length + 1);
            Write(value);
        }
    }
}
