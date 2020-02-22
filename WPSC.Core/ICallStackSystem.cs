using System.IO;

namespace WPSC.Core
{
    public interface ICallStackSystem
    {
        public void IncludeLibrary(TextWriter target);
        public void ProcessFile(string fileName, TextReader source, TextWriter target);
    }
}
