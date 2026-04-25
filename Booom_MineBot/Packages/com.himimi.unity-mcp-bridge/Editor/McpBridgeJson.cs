using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace McpBridge.Editor
{
    public static class McpBridgeJson
    {
        public static string Serialize(object value)
        {
            var builder = new StringBuilder(256);
            WriteValue(builder, value);
            return builder.ToString();
        }

        public static object Deserialize(string json)
        {
            return Parser.Parse(json);
        }

        public static T DeserializeObject<T>(string json) where T : class
        {
            return JsonUtilityAdapter.FromJson<T>(json);
        }

        public static string SerializeObject<T>(T value) where T : class
        {
            return JsonUtilityAdapter.ToJson(value);
        }

        private static void WriteValue(StringBuilder builder, object value)
        {
            switch (value)
            {
                case null:
                    builder.Append("null");
                    return;
                case string stringValue:
                    WriteString(builder, stringValue);
                    return;
                case bool boolValue:
                    builder.Append(boolValue ? "true" : "false");
                    return;
                case IDictionary dictionary:
                    WriteObject(builder, dictionary);
                    return;
                case IEnumerable enumerable when value is not string:
                    WriteArray(builder, enumerable);
                    return;
                case char charValue:
                    WriteString(builder, charValue.ToString());
                    return;
                case Enum enumValue:
                    WriteString(builder, enumValue.ToString());
                    return;
                case float or double or decimal:
                    builder.Append(Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture));
                    return;
                case sbyte or byte or short or ushort or int or uint or long or ulong:
                    builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    return;
                default:
                    if (TrySerializeCustomObject(value, out var customSerialized))
                    {
                        builder.Append(customSerialized);
                        return;
                    }

                    WriteString(builder, value.ToString());
                    return;
            }
        }

        private static bool TrySerializeCustomObject(object value, out string json)
        {
            switch (value)
            {
                case BridgeEnvelope envelope:
                    json = Serialize(envelope.ToDictionary());
                    return true;
                case ToolDescriptor descriptor:
                    json = Serialize(descriptor.ToDictionary());
                    return true;
                case ToolCallResult result:
                    json = Serialize(result.ToDictionary());
                    return true;
                case CompileDiagnostic diagnostic:
                    json = Serialize(diagnostic.ToDictionary());
                    return true;
                default:
                    json = null;
                    return false;
            }
        }

        private static void WriteObject(StringBuilder builder, IDictionary dictionary)
        {
            builder.Append('{');
            var first = true;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                WriteString(builder, Convert.ToString(entry.Key, CultureInfo.InvariantCulture));
                builder.Append(':');
                WriteValue(builder, entry.Value);
                first = false;
            }

            builder.Append('}');
        }

        private static void WriteArray(StringBuilder builder, IEnumerable enumerable)
        {
            builder.Append('[');
            var first = true;
            foreach (var item in enumerable)
            {
                if (!first)
                {
                    builder.Append(',');
                }

                WriteValue(builder, item);
                first = false;
            }

            builder.Append(']');
        }

        private static void WriteString(StringBuilder builder, string value)
        {
            builder.Append('"');
            foreach (var character in value)
            {
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (character < ' ')
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(character);
                        }

                        break;
                }
            }

            builder.Append('"');
        }

        private sealed class Parser
        {
            private readonly string m_Json;
            private int m_Index;

            private Parser(string json)
            {
                m_Json = json;
            }

            public static object Parse(string json)
            {
                var parser = new Parser(json);
                return parser.ParseValue();
            }

            private IDictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>(StringComparer.Ordinal);
                m_Index++;
                var parsing = true;

                while (parsing)
                {
                    switch (NextToken)
                    {
                        case Token.None:
                            return null;
                        case Token.CurlyClose:
                            m_Index++;
                            return table;
                    }

                    var name = ParseString();
                    if (name == null)
                    {
                        return null;
                    }

                    if (NextToken != Token.Colon)
                    {
                        return null;
                    }

                    m_Index++;
                    table[name] = ParseValue();

                    switch (NextToken)
                    {
                        case Token.Comma:
                            m_Index++;
                            break;
                        case Token.CurlyClose:
                            m_Index++;
                            parsing = false;
                            break;
                        case Token.None:
                            return null;
                        default:
                            parsing = false;
                            break;
                    }
                }

                return table;
            }

            private List<object> ParseArray()
            {
                var array = new List<object>();
                m_Index++;

                var parsing = true;
                while (parsing)
                {
                    var token = NextToken;
                    switch (token)
                    {
                        case Token.None:
                            return null;
                        case Token.SquaredClose:
                            m_Index++;
                            break;
                        default:
                            array.Add(ParseValue());
                            break;
                    }

                    switch (NextToken)
                    {
                        case Token.Comma:
                            m_Index++;
                            break;
                        case Token.SquaredClose:
                            m_Index++;
                            parsing = false;
                            break;
                        default:
                            parsing = false;
                            break;
                    }
                }

                return array;
            }

            private object ParseValue()
            {
                switch (NextToken)
                {
                    case Token.String:
                        return ParseString();
                    case Token.Number:
                        return ParseNumber();
                    case Token.CurlyOpen:
                        return ParseObject();
                    case Token.SquaredOpen:
                        return ParseArray();
                    case Token.True:
                        m_Index += 4;
                        return true;
                    case Token.False:
                        m_Index += 5;
                        return false;
                    case Token.Null:
                        m_Index += 4;
                        return null;
                    default:
                        return null;
                }
            }

            private string ParseString()
            {
                var builder = new StringBuilder();
                m_Index++;

                var parsing = true;
                while (parsing)
                {
                    if (m_Index >= m_Json.Length)
                    {
                        break;
                    }

                    var character = NextChar;
                    switch (character)
                    {
                        case '"':
                            parsing = false;
                            break;
                        case '\\':
                            if (m_Index >= m_Json.Length)
                            {
                                parsing = false;
                                break;
                            }

                            character = NextChar;
                            switch (character)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    builder.Append(character);
                                    break;
                                case 'b':
                                    builder.Append('\b');
                                    break;
                                case 'f':
                                    builder.Append('\f');
                                    break;
                                case 'n':
                                    builder.Append('\n');
                                    break;
                                case 'r':
                                    builder.Append('\r');
                                    break;
                                case 't':
                                    builder.Append('\t');
                                    break;
                                case 'u':
                                    var hex = new char[4];
                                    for (var index = 0; index < 4; index++)
                                    {
                                        hex[index] = NextChar;
                                    }

                                    builder.Append((char)Convert.ToInt32(new string(hex), 16));
                                    break;
                            }

                            break;
                        default:
                            builder.Append(character);
                            break;
                    }
                }

                return builder.ToString();
            }

            private object ParseNumber()
            {
                var number = NextWord;
                if (number.IndexOf('.') == -1 && number.IndexOf('e') == -1 && number.IndexOf('E') == -1)
                {
                    if (long.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedLong))
                    {
                        return parsedLong;
                    }
                }

                if (double.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedDouble))
                {
                    return parsedDouble;
                }

                return 0d;
            }

            private void EatWhitespace()
            {
                while (m_Index < m_Json.Length && char.IsWhiteSpace(PeekChar))
                {
                    m_Index++;
                }
            }

            private char PeekChar => m_Json[m_Index];

            private char NextChar => m_Json[m_Index++];

            private string NextWord
            {
                get
                {
                    var builder = new StringBuilder();
                    while (m_Index < m_Json.Length && !IsWordBreak(PeekChar))
                    {
                        builder.Append(NextChar);
                    }

                    return builder.ToString();
                }
            }

            private Token NextToken
            {
                get
                {
                    EatWhitespace();
                    if (m_Index >= m_Json.Length)
                    {
                        return Token.None;
                    }

                    switch (PeekChar)
                    {
                        case '{':
                            return Token.CurlyOpen;
                        case '}':
                            return Token.CurlyClose;
                        case '[':
                            return Token.SquaredOpen;
                        case ']':
                            return Token.SquaredClose;
                        case ',':
                            return Token.Comma;
                        case '"':
                            return Token.String;
                        case ':':
                            return Token.Colon;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-':
                            return Token.Number;
                    }

                    if (m_Json.IndexOf("false", m_Index, StringComparison.Ordinal) == m_Index) return Token.False;
                    if (m_Json.IndexOf("true", m_Index, StringComparison.Ordinal) == m_Index) return Token.True;
                    if (m_Json.IndexOf("null", m_Index, StringComparison.Ordinal) == m_Index) return Token.Null;
                    return Token.None;
                }
            }

            private static bool IsWordBreak(char character)
            {
                return char.IsWhiteSpace(character) || character is ',' or ':' or ']' or '}' or '[' or '{' or '"';
            }

            private enum Token
            {
                None,
                CurlyOpen,
                CurlyClose,
                SquaredOpen,
                SquaredClose,
                Colon,
                Comma,
                String,
                Number,
                True,
                False,
                Null
            }
        }
    }
}
