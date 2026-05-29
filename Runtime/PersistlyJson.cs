#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Persistly.Unity
{
    public static class PersistlyJson
    {
        private const long SlotInfoMaxBytes = 16384;
        private const long StateMaxBytes = 262144;
        private const string SlotInfoLabel = "slotInfo";
        private const string StateLabel = "state";

        public static string CanonicalizeObjectJson(string json, string label)
        {
            var parsed = ParseJsonValue(json, label);
            if (!(parsed is Dictionary<string, object?>))
            {
                throw new PersistlyConfigurationError(label + " must be a JSON object.");
            }

            return Serialize(parsed);
        }

        public static int Utf8ByteCount(string json)
        {
            return Encoding.UTF8.GetByteCount(json);
        }

        public static object? ParseJsonValue(string json, string label)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new PersistlyConfigurationError(label + " must be valid JSON.");
            }

            try
            {
                return PersistlyMiniJson.Deserialize(json);
            }
            catch (Exception exception) when (!(exception is PersistlyConfigurationError))
            {
                throw new PersistlyConfigurationError(label + " must be valid JSON. " + exception.Message);
            }
        }

        public static void ValidatePayloadSizes(string? slotInfoJson, string stateJson)
        {
            if (slotInfoJson != null)
            {
                EnsureWithinLimit(slotInfoJson, SlotInfoLabel, SlotInfoMaxBytes);
            }

            EnsureWithinLimit(stateJson, StateLabel, StateMaxBytes);
        }

        public static string Serialize(object? value)
        {
            return PersistlyMiniJson.Serialize(value);
        }

        public static string EscapeJsonString(string value)
        {
            return Serialize(value);
        }

        private static void EnsureWithinLimit(string json, string field, long maxBytes)
        {
            var size = Utf8ByteCount(json);
            if (size > maxBytes)
            {
                throw new PersistlyPayloadTooLargeError(
                    413,
                    field == StateLabel ? "State exceeds the maximum allowed size." : "SlotInfo exceeds the maximum allowed size.",
                    field,
                    (int)maxBytes);
            }
        }
    }

    internal static class PersistlyMiniJson
    {
        public static object? Deserialize(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var parser = new Parser(json);
            return parser.Parse();
        }

        public static string Serialize(object? value)
        {
            var builder = new StringBuilder(256);
            Serializer.SerializeValue(value, builder);
            return builder.ToString();
        }

        private sealed class Parser
        {
            private readonly string _json;
            private int _index;

            public Parser(string json)
            {
                _json = json;
            }

            public object? Parse()
            {
                var value = ParseValue();
                ConsumeWhitespace();
                if (_index != _json.Length)
                {
                    throw new FormatException("Unexpected trailing JSON content.");
                }

                return value;
            }

            private object? ParseValue()
            {
                ConsumeWhitespace();
                if (_index >= _json.Length)
                {
                    throw new FormatException("Unexpected end of JSON input.");
                }

                var token = _json[_index];
                switch (token)
                {
                    case '{':
                        return ParseObject();
                    case '[':
                        return ParseArray();
                    case '"':
                        return ParseString();
                    case 't':
                        ConsumeLiteral("true");
                        return true;
                    case 'f':
                        ConsumeLiteral("false");
                        return false;
                    case 'n':
                        ConsumeLiteral("null");
                        return null;
                    default:
                        if (token == '-' || char.IsDigit(token))
                        {
                            return ParseNumber();
                        }

                        throw new FormatException("Unexpected JSON token at index " + _index.ToString(CultureInfo.InvariantCulture) + ".");
                }
            }

            private Dictionary<string, object?> ParseObject()
            {
                var result = new Dictionary<string, object?>(StringComparer.Ordinal);
                Expect('{');
                ConsumeWhitespace();
                if (TryConsume('}'))
                {
                    return result;
                }

                while (true)
                {
                    ConsumeWhitespace();
                    var key = ParseString();
                    ConsumeWhitespace();
                    Expect(':');
                    result[key] = ParseValue();
                    ConsumeWhitespace();
                    if (TryConsume('}'))
                    {
                        return result;
                    }

                    Expect(',');
                }
            }

            private List<object?> ParseArray()
            {
                var result = new List<object?>();
                Expect('[');
                ConsumeWhitespace();
                if (TryConsume(']'))
                {
                    return result;
                }

                while (true)
                {
                    result.Add(ParseValue());
                    ConsumeWhitespace();
                    if (TryConsume(']'))
                    {
                        return result;
                    }

                    Expect(',');
                }
            }

            private string ParseString()
            {
                Expect('"');
                var builder = new StringBuilder();
                while (_index < _json.Length)
                {
                    var c = _json[_index++];
                    if (c == '"')
                    {
                        return builder.ToString();
                    }

                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (_index >= _json.Length)
                    {
                        throw new FormatException("Invalid escape sequence.");
                    }

                    var escaped = _json[_index++];
                    switch (escaped)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(escaped);
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
                            if (_index + 4 > _json.Length)
                            {
                                throw new FormatException("Invalid unicode escape.");
                            }

                            var hex = _json.Substring(_index, 4);
                            builder.Append((char)Convert.ToInt32(hex, 16));
                            _index += 4;
                            break;
                        default:
                            throw new FormatException("Unsupported escape sequence.");
                    }
                }

                throw new FormatException("Unterminated JSON string.");
            }

            private object ParseNumber()
            {
                var start = _index;
                if (_json[_index] == '-')
                {
                    _index += 1;
                }

                while (_index < _json.Length && char.IsDigit(_json[_index]))
                {
                    _index += 1;
                }

                var isFractional = false;
                if (_index < _json.Length && _json[_index] == '.')
                {
                    isFractional = true;
                    _index += 1;
                    while (_index < _json.Length && char.IsDigit(_json[_index]))
                    {
                        _index += 1;
                    }
                }

                if (_index < _json.Length && (_json[_index] == 'e' || _json[_index] == 'E'))
                {
                    isFractional = true;
                    _index += 1;
                    if (_index < _json.Length && (_json[_index] == '+' || _json[_index] == '-'))
                    {
                        _index += 1;
                    }

                    while (_index < _json.Length && char.IsDigit(_json[_index]))
                    {
                        _index += 1;
                    }
                }

                var token = _json.Substring(start, _index - start);
                if (!isFractional && long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                {
                    return longValue;
                }

                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    return doubleValue;
                }

                throw new FormatException("Invalid numeric JSON token.");
            }

            private void ConsumeLiteral(string literal)
            {
                if (_index + literal.Length > _json.Length || string.CompareOrdinal(_json, _index, literal, 0, literal.Length) != 0)
                {
                    throw new FormatException("Invalid JSON literal.");
                }

                _index += literal.Length;
            }

            private void ConsumeWhitespace()
            {
                while (_index < _json.Length && char.IsWhiteSpace(_json[_index]))
                {
                    _index += 1;
                }
            }

            private void Expect(char token)
            {
                ConsumeWhitespace();
                if (_index >= _json.Length || _json[_index] != token)
                {
                    throw new FormatException("Expected '" + token + "'.");
                }

                _index += 1;
            }

            private bool TryConsume(char token)
            {
                ConsumeWhitespace();
                if (_index < _json.Length && _json[_index] == token)
                {
                    _index += 1;
                    return true;
                }

                return false;
            }
        }

        private static class Serializer
        {
            public static void SerializeValue(object? value, StringBuilder builder)
            {
                if (value == null)
                {
                    builder.Append("null");
                    return;
                }

                if (value is string stringValue)
                {
                    SerializeString(stringValue, builder);
                    return;
                }

                if (value is bool boolValue)
                {
                    builder.Append(boolValue ? "true" : "false");
                    return;
                }

                if (value is IDictionary<string, object?> dictionary)
                {
                    SerializeObject(dictionary, builder);
                    return;
                }

                if (value is IDictionary<string, string> stringDictionary)
                {
                    var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
                    foreach (var pair in stringDictionary)
                    {
                        normalized[pair.Key] = pair.Value;
                    }

                    SerializeObject(normalized, builder);
                    return;
                }

                if (value is System.Collections.IList list)
                {
                    SerializeArray(list, builder);
                    return;
                }

                if (value is float || value is double || value is decimal)
                {
                    builder.Append(Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture));
                    return;
                }

                if (value is byte || value is sbyte || value is short || value is ushort ||
                    value is int || value is uint || value is long || value is ulong)
                {
                    builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                    return;
                }

                throw new PersistlyConfigurationError("Unsupported JSON value type: " + value.GetType().FullName);
            }

            private static void SerializeObject(IDictionary<string, object?> dictionary, StringBuilder builder)
            {
                builder.Append('{');
                var first = true;
                foreach (var pair in dictionary)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    SerializeString(pair.Key, builder);
                    builder.Append(':');
                    SerializeValue(pair.Value, builder);
                }

                builder.Append('}');
            }

            private static void SerializeArray(System.Collections.IList list, StringBuilder builder)
            {
                builder.Append('[');
                for (var index = 0; index < list.Count; index += 1)
                {
                    if (index > 0)
                    {
                        builder.Append(',');
                    }

                    SerializeValue(list[index], builder);
                }

                builder.Append(']');
            }

            private static void SerializeString(string value, StringBuilder builder)
            {
                builder.Append('"');
                foreach (var c in value)
                {
                    switch (c)
                    {
                        case '\\':
                            builder.Append("\\\\");
                            break;
                        case '"':
                            builder.Append("\\\"");
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
                            if (c < 0x20)
                            {
                                builder.Append("\\u");
                                builder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                builder.Append(c);
                            }

                            break;
                    }
                }

                builder.Append('"');
            }
        }
    }
}
