using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrPrsL.Scripting
{
    /// <summary>
    /// Provides methods and datasets for script exception handling.
    /// </summary>
    public static class Exceptions
    {
        /// <summary>
        /// Throw a custom exception.
        /// </summary>
        /// <param name="exception">The exception data.</param>
        public static void Throw(ScriptException exception)
        {
            MainWindow.Instance.StopScriptExecution();
            MainWindow.Instance.NotifyException(exception);
        }

        /// <summary>
        /// A general script exception. This is the base class for other script exception types.
        /// </summary>
        public class ScriptException
        {
            /// <summary>
            /// The message describing the problem that was caused.
            /// </summary>
            public string Message;
            /// <summary>
            /// Additional information for user the handle the exception. E.g.: Tips, suggestions, snippets.
            /// </summary>
            public string Information;
            /// <summary>
            /// The raw input that lead to this exception being created.
            /// </summary>
            public string Raw;
            /// <summary>
            /// Optional. The number of line that this exception occurred on.
            /// </summary>
            public int? Line;
            /// <summary>
            /// Optional. The offset of characters from the beginning of the line that this exception occurred on, pointing to the exact character the exception was raised at.
            /// </summary>
            public int? LineOffset;

            private string result;

            public ScriptException(string message, string information, string raw, int? line = null, int? lineOffset = null)
            {
                Message = message;
                Information = information;
                Raw = raw;
                Line = line;
                LineOffset = lineOffset;
            }

            public virtual string Type()
            {
                return "General";
            }

            public override string ToString()
            {
                result = $"{Type()} exception!";
                result += $"{Environment.NewLine}\t{Message}";
                result += $"{Environment.NewLine}\t\t{Information}";
                if (Line.HasValue)
                {
                    result += $"{Environment.NewLine}\t\t\tAt line {Line.Value}";
                    if (LineOffset.HasValue)
                    {
                        result += $" starting at character #{LineOffset}";
                    }
                }
                result += $".{Environment.NewLine}{Environment.NewLine}{Raw}.";
                return result;
            }
        }

        /// <summary>
        /// A general runtime script exception. This is the base class for other runtime script exception types.
        /// </summary>
        public class RuntimeException : ScriptException
        {
            public override string Type()
            {
                return "Runtime general";
            }

            public RuntimeException(string message, string information, string raw, int? line = null, int? lineOffset = null) : base(message, information, raw, line, lineOffset)
            {

            }
        }

        /// <summary>
        /// A syntax-based exception. Indicates the user has made an error in writing the script.
        /// </summary>
        public class SyntaxException : ScriptException
        {
            public override string Type()
            {
                return "Syntax";
            }

            public SyntaxException(string message, string information, string raw, int? line = null, int? lineOffset = null) : base(message, information, raw, line, lineOffset)
            {

            }
        }

        /// <summary>
        /// A parsing-based exception. Indicates the provided script produces an exception in parsing-time (after syntax recognition).
        /// </summary>
        public class ParsingException : ScriptException
        {
            public override string Type()
            {
                return "Parsing";
            }

            public ParsingException(string message, string information, string raw, int? line = null, int? lineOffset = null) : base(message, information, raw, line, lineOffset)
            {

            }
        }

        /// <summary>
        /// A runtime type exception. Indicates that the provided variable type could not be interpreted as another type.
        /// </summary>
        public class TypeException : RuntimeException
        {
            public override string Type()
            {
                return "Variable type";
            }

            public TypeException(string message, string information, string raw, int? line = null, int? lineOffset = null) : base(message, information, raw, line, lineOffset)
            {

            }
        }
    }
}
