namespace WPSC.Lua
{
    public static class StringUtils
    {
        public static bool HasEvenAmmountOfEscapesBefore(this string line, int pos)
        {
            var count = 0;
            while (--pos > -1 && line[pos] == '\\')
                ++count;
            return count % 2 == 1;
        }

    }
}
