using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WPSC.Lua.Exceptions;
using WPSC.Lua.Tokens;

namespace WPSC.Lua
{
    public class Tokenizer
    {
        public Tokenizer(ILuaData data)
        {
            Keywords = data.Keywords;
            Operators = data.Operators;
        }

        public async IAsyncEnumerable<Token> Parse(string fileName, TextReader source)
        {
            string? line = await source.ReadLineAsync();
            var pos = new FilePosition(0, 0);

            while (line != null)
            {
                (line, pos) = await SkipSpaces(source, line!, pos);

                if (line == null)
                    break;

                var start = pos;
                var token = ParseComment(fileName, source, ref line, ref pos)
                    ?? ParseString(fileName, line, ref pos)
                    ?? ParseNumber(line, ref pos.@char)
                    ?? ParseOperator(line, ref pos.@char)
                    ?? (Token?)ParseIdentifier(line, ref pos.@char)
                    ?? throw new TokenizerException($"Unexpected token at {fileName}:{pos} - '{line[pos.@char]}'");

                token.FileName = fileName;
                token.Start = start;
                token.End = pos;

                yield return token;
            }
        }

        private IReadOnlyDictionary<string, KeywordToken> Keywords { get; }
        private IReadOnlyDictionary<string, OperatorToken> Operators { get; }

        private async Task<(string?, FilePosition)> SkipSpaces(TextReader source, string line, FilePosition pos)
        {
            while (line != null && (line.Length <= pos.@char || char.IsWhiteSpace(line[pos.@char])))
            {
                if (line.Length > pos.@char)
                    ++pos.@char;

                if (line.Length <= pos.@char)
                {
                    ++pos.line;
                    pos.@char = 0;
                    line = await source.ReadLineAsync();
                }
            }

            return (line, pos);
        }

        private CommentToken? ParseComment(string fileName, TextReader source, ref string line, ref FilePosition pos)
        {
            if (line.Length < pos.@char + 2)
                return null;

            var substr = line[pos.@char..];
            if (line[pos.@char] != '-' || line[pos.@char + 1] != '-')
                return null;

            if (line.Length < pos.@char + 4 || line[pos.@char + 2] != '[' || line[pos.@char + 3] != '[')
            {
                var text = line[(pos.@char + 2)..];
                line = source.ReadLine();
                pos = new FilePosition(pos.line + 1, 0);
                return new CommentToken(text, false);
            }

            var sb = new StringBuilder();
            pos.@char += 2;

            while (true)
            {
                var endPos = line.IndexOf("]]", pos.@char);

                if (endPos == -1)
                {
                    ++pos.line;
                    sb.AppendLine(line[pos.@char..]);
                    pos.@char = 0;
                    line = source.ReadLine();
                    if (line == null)
                        throw new TokenizerException($"Unexpected EOF at {fileName}:{pos}");
                    continue;
                }

                sb.Append(line[pos.@char..endPos]);
                pos.@char = endPos + 2;
                return new CommentToken(sb.ToString(), true);
            }
        }

        private StringToken? ParseString(string fileName, string line, ref FilePosition pos)
        {
            var start = line[pos.@char];

            if (start != '\'' && start != '"')
                return null;

            ++pos.@char;
            if (pos.@char >= line.Length)
                throw new TokenizerException($"Unexpected EOL at {fileName}:{pos}");
            var sb = new StringBuilder();

            while (line[pos.@char] != start || line.HasEvenAmmountOfEscapesBefore(pos.@char))
            {
                sb.Append(line[pos.@char++]);
                if (pos.@char >= line.Length)
                    throw new TokenizerException($"Unexpected EOL at {fileName}:{pos}");
            }

            ++pos.@char;
            return new StringToken(start, sb.ToString());
        }

        private NumberToken? ParseNumber(string line, ref int @char)
        {
            if (line[@char] != '.' && !char.IsDigit(line[@char]))
                return null;

            var sb = new StringBuilder(line[@char].ToString());
            var hasPoint = line[@char] == '.';

            if (hasPoint)
            {
                while (++@char < line.Length && char.IsDigit(line[@char]))
                    sb.Append(line[@char]);
            }
            else
            {
                while (++@char < line.Length && char.IsDigit(line[@char]))
                    sb.Append(line[@char]);
                if (@char < line.Length && line[@char] == '.')
                {
                    sb.Append('.');
                    while (++@char < line.Length && char.IsDigit(line[@char]))
                        sb.Append(line[@char]);
                }
            }

            return new NumberToken(sb.ToString());
        }

        private OperatorToken? ParseOperator(string line, ref int @char)
        {
            var substr = line[@char..];

            foreach (var op in Operators)
            {
                if (!substr.StartsWith(op.Key))
                    continue;

                if (substr.Length >= op.Key.Length
                    || char.IsWhiteSpace(substr[op.Key.Length])
                    || char.IsLetterOrDigit(substr[op.Key.Length])
                    || substr.Length >= op.Key.Length + 2 && substr.Substring(op.Key.Length, 2) == "--")
                {
                    @char += op.Key.Length;
                    return new OperatorToken(op.Key);
                }
            }

            return null;
        }

        private IdentifierToken? ParseIdentifier(string line, ref int @char)
        {
            if (!char.IsLetterOrDigit(line[@char]) && line[@char] != '_')
                return null;

            var sb = new StringBuilder(line[@char++].ToString());

            while (@char < line.Length && (char.IsLetterOrDigit(line[@char]) || line[@char] == '_'))
                sb.Append(line[@char++]);

            var str = sb.ToString();

            if (Keywords.TryGetValue(str, out var kw))
                return kw;

            return new IdentifierToken(str);
        }
    }
}
