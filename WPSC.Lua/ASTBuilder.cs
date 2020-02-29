using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WPSC.Lua.Exceptions;
using WPSC.Lua.Instructions;
using WPSC.Lua.Tokens;

namespace WPSC.Lua
{
    public class ASTBuilder
    {
        public async Task<Function> Process(IAsyncEnumerable<Token> tokens)
        {
            var ret = new Function();
            var stream = await TokenStream.Create(tokens);

            await ParseInstructions(stream, ret.Body);

            if (!stream.Finished)
                throw new SyntaxerException($"Unexpected end: {stream.Current}");

            return ret;
        }

        private async Task ParseInstructions(TokenStream stream, List<IInstruction> target)
        {
            while (!stream.Finished && !stream.Peek<KeywordToken>("end"))
            {
                switch (await stream.Pop())
                {
                    case KeywordToken local when local.Value == "local":
                        target.Add(await ParseLocal(stream));
                        break;
                    case KeywordToken func when func.Value == "function":
                        target.Add(await ParseFunctionDeclaration(stream, false));
                        break;
                    default:
                        throw new NotImplementedException($"Unimplemented syntax construct: {TokenStream.TokenWithPos(stream.Last!)}.");
                }
            }
        }

        private async Task<IInstruction> ParseLocal(TokenStream stream)
        {
            if (stream.Finished)
                throw new SyntaxerException($"Unexpected EOF at {stream.Last!.FileName}:{stream.Last.End}.");

            switch (await stream.Pop())
            {
                case KeywordToken func when func.Value == "function":
                    return await ParseFunctionDeclaration(stream, true);
                case IdentifierToken id:
                    var lefts = new List<ILValue> { new Identifier(id), };

                    if (stream.Finished)
                        return new Set(lefts.ToArray(), Array.Empty<IRValue>(), true);

                    while (await stream.Match<OperatorToken>(","))
                    {
                        if (stream.Finished)
                            throw new SyntaxerException($"Unexpected EOF at {stream.Last!.FileName}:{stream.Last!.End}.");
                        lefts.Add(new Identifier(await stream.Expect<IdentifierToken>()));
                        if (stream.Finished)
                            return new Set(lefts.ToArray(), Array.Empty<IRValue>(), true);
                    }

                    if (!await stream.Match<OperatorToken>("="))
                        return new Set(lefts.ToArray(), Array.Empty<IRValue>(), true);

                    var firstRight = await ParseExpression(stream);

                    if (stream.Finished)
                        return new Set(lefts.ToArray(), new[] { firstRight, }, true);

                    var rights = new List<IRValue> { firstRight, };

                    while (await stream.Match<OperatorToken>(","))
                    {
                        if (stream.Finished)
                            throw new SyntaxerException($"Unexpected EOF at {stream.Last!.FileName}:{stream.Last!.End}.");
                        rights.Add(await ParseExpression(stream));
                        if (stream.Finished)
                            return new Set(lefts.ToArray(), Array.Empty<IRValue>(), true);
                    }

                    return new Set(lefts.ToArray(), rights.ToArray(), true);
                default:
                    throw new TokenizerException($"Unexpected token in local declaration: {TokenStream.TokenWithPos(stream.Last!)}.");
            }
        }

        private async Task<IRValue> ParseExpression(TokenStream stream)
        {
            var left = await TryParseAsBrackets(stream)
                ?? await TryParseAsUnary(stream);

            throw new NotImplementedException();
        }

        private async Task<IRValue?> TryParseAsBrackets(TokenStream stream)
        {
            if (!await stream.Match<OperatorToken>("("))
                return null;

            var brackets = new UnaryOperation(UnaryOperator.Brackets, await ParseExpression(stream));
            await stream.Expect<OperatorToken>(")");
            return brackets;
        }

        private static readonly string[] unaryOperators = new[]
        {
            "-",
            "~",
            "#",
        };

        private static readonly string[] unaryKws = new[]
        {
            "not",
        };

        private static readonly Dictionary<string, UnaryOperator> unaryOperatorsMap = new Dictionary<string, UnaryOperator>
        {
            {"-", UnaryOperator.UnaryMinus},
            {"~", UnaryOperator.BitwiseNot},
            {"#", UnaryOperator.Length},
            {"not", UnaryOperator.Not},
        };

        private async Task<IRValue?> TryParseAsUnary(TokenStream stream)
        {
            if (!await stream.Match<OperatorToken>(unaryOperators) && !await stream.Match<OperatorToken>(unaryKws))
                return null;

            var opToken = stream.Last as Token<string>;
            var op = unaryOperatorsMap[opToken!.Value];
            return new UnaryOperation(op, await ParseExpression(stream));
        }

        private async Task<FunctionDeclaration> ParseFunctionDeclaration(TokenStream stream, bool isLocal)
        {
            var function = new Function();
            if (stream.Finished)
                throw new SyntaxerException($"Unexpected EOF at {stream.Last!.FileName}:{stream.Last!.End}.");

            function.Name = await stream.Expect<IdentifierToken>();
            await stream.Expect<OperatorToken>("(");
            var varArgFound = false;
            var firstArgPassed = false;
            while (!await stream.Match<OperatorToken>(")"))
            {
                if (firstArgPassed)
                    await stream.Expect<OperatorToken>(",");
                firstArgPassed = true;

                switch (await stream.Pop())
                {
                    case IdentifierToken id:
                        if (varArgFound)
                            throw new SyntaxerException($"Already found <...> in functon paramters, unexpected paramter: {TokenStream.TokenWithPos(stream.Last!)}");
                        function.Parameters.Add(id);
                        break;
                    case OperatorToken op when op.Value == "...":
                        function.Parameters.Add(op);
                        varArgFound = true;
                        break;
                    default:
                        throw new SyntaxerException($"Unexpected token in function parameters: {TokenStream.TokenWithPos(stream.Last!)}");
                }
            }

            await ParseInstructions(stream, function.Body);

            if (stream.Finished)
                throw new SyntaxerException($"Expected end at: {stream.Last!.FileName}:{stream.Last!.End}");

            return new FunctionDeclaration(function, isLocal);
        }
    }

    internal class TokenStream
    {
        public bool Finished { get; private set; }  = false;
        public Token? Last { get; private set; }
        public Token? Current => _enumerator.Current;

        public static async Task<TokenStream> Create(IAsyncEnumerable<Token> enumerable)
        {
            var stream = new TokenStream(enumerable);
            stream.Finished = !await stream._enumerator.MoveNextAsync();
            return stream;
        }

        public static string TokenWithPos(Token token) => $"<{token}> at {token.FileName}:[{token.Start},{token.End})";

        public async Task<bool> Match<TToken>()
        {
            var ret = !Finished && Current is TToken;
            if (ret)
                await Pop();
            return ret;
        }

        public async Task<bool> Match<TToken>(params string[] check) where TToken : Token<string>
        {
            var ret = !Finished && Current is TToken token && check.Any(str => token.Value == str);
            if (ret)
                await Pop();
            return ret;
        }

        public bool Peek<TToken>() where TToken : Token => Current is TToken;
        public bool Peek<TToken>(params string[] check) where TToken : Token<string> => Current is TToken token && check.Any(str => token.Value == str);

        public async Task<TToken> Expect<TToken>()
            where TToken : Token
        {
            var token = await Pop();
            if (token is TToken ttok)
                return ttok;
            throw new SyntaxerException($"Expected {typeof(TToken).Name} at {Last!.FileName}:{Last.Start}.");
        }

        public async Task Expect<TToken>(string check) where TToken : Token<string>
        {
            if (!await Match<TToken>(check))
                throw new SyntaxerException($"Expected {typeof(TToken).Name} of value <{check}> at {TokenWithPos(Last!)}");
        }

        public async Task<Token> Pop()
        {
            if (Finished)
            {
                if (Last != null)
                    throw new SyntaxerException($"Unexpected EOF at {TokenWithPos(Last!)}.");
                else
                    throw new InvalidOperationException();
            }
            Last = Current;
            Finished = !await _enumerator.MoveNextAsync();
            return Last;
        }


        private readonly IAsyncEnumerator<Token> _enumerator;

        private TokenStream(IAsyncEnumerable<Token> enumerable)
        {
            _enumerator = enumerable.GetAsyncEnumerator();
        }
    }
}
