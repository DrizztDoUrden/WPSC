namespace WPSC.Lua
{
    public struct FilePosition
    {
        public int line;
        public int @char;

        public FilePosition(int line, int @char)
        {
            this.line = line;
            this.@char = @char;
        }

        public override string ToString() => $"{line + 1}:{@char + 1}";
    }
}