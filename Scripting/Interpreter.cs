using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StrPrsL.Utility;

namespace StrPrsL.Scripting
{
    public static class Interpreter
    {
        /// <summary>
        /// The syntax parameters for scripting.
        /// </summary>
        public static class Syntax
        {
            /// <summary>
            /// The sequence to determine the beginning of a command.
            /// </summary>
            public const string CommandBegin = "<";
            /// <summary>
            /// The sequence to determine the ending of a command.
            /// </summary>
            public const string CommandEnd = ">";

            /// <summary>
            /// The sequence to determine the beginning of a function.
            /// </summary>
            public const string FunctionBegin = "[";
            /// <summary>
            /// The sequence to determine the ending of a function.
            /// </summary>
            public const string FunctionEnd = "]";

            /// <summary>
            /// The sequence to determine the beginning of a parameter field.
            /// </summary>
            public const string ParameterFieldBegin = "(";
            /// <summary>
            /// The sequence to determine the ending of a parameter field.
            /// </summary>
            public const string ParameterFieldEnd = ")";
            /// <summary>
            /// The sequence to determine the seperation of parameters inside <see cref="ParameterFieldBegin"/>...<see cref="ParameterFieldEnd"/>.
            /// </summary>
            public const string ParameterSeperator = ",";

            /// <summary>
            /// The sequence to determine the beginning of a block.
            /// </summary>
            public const string BlockBegin = "{";
            /// <summary>
            /// The sequence to determine the ending of a block.
            /// </summary>
            public const string BlockEnd = "}";

            /// <summary>
            /// The sequence to toggle the capturing of a string.
            /// </summary>
            public const string StringSwitch = "\"";
            /// <summary>
            /// The sequence to determine the escape from the toggling of string capturing.
            /// </summary>
            public const string StringEscape = "\\";
        }

        /// <summary>
        /// An intermediate representation of a script before being converted to C# instructions.
        /// </summary>
        public class Intermediate
        {
            /// <summary>
            /// Name of the instruction to be executed. The value of the string in the case that <see cref="InstructionType"/> is <see cref="InstructionType.String"/>.
            /// </summary>
            public string InstructionName;
            /// <summary>
            /// Optional parameters to be passed to the C# instruction at execution-time.
            /// </summary>
            public List<Intermediate> Parameters;
            /// <summary>
            /// The type of this instruction.
            /// </summary>
            public InstructionType Type;
            /// <summary>
            /// The block associated with this instruction.
            /// </summary>
            public List<Intermediate> Block;
            /// <summary>
            /// The raw script input of this instruction.
            /// </summary>
            public string Raw;
            /// <summary>
            /// The line number of this instruction inside the script.
            /// </summary>
            public int Line;
            /// <summary>
            /// The amount of characters that offset this instruction from its line number.
            /// </summary>
            public int LineOffset;
            /// <summary>
            /// C# instructions for this intermediate instruction.
            /// </summary>
            public Execution Execution;
            /// <summary>
            /// Unique identifier for this instruction.
            /// </summary>
            public int GUID;

            private string aggregationBuffer;

            /// <summary>
            /// A new blank intermediate instruction.
            /// </summary>
            /// <param name="type">Type of this instruction.</param>
            public Intermediate(InstructionType type)
            {
                InstructionName = "";
                Parameters = new List<Intermediate>();
                Type = type;
                Block = new List<Intermediate>();
                Execution = null;
            }

            /// <summary>
            /// A new intermediate instruction. Intended for use with <see cref="InstructionType.String"/> declarations.
            /// </summary>
            /// <param name="instructionName">Name of this instruction.</param>
            /// <param name="type">Type of this instruction.</param>
            public Intermediate(string instructionName, InstructionType type)
            {
                InstructionName = instructionName;
                Parameters = new List<Intermediate>();
                Type = type;
                Block = new List<Intermediate>();
                Execution = null;
            }

            /// <summary>
            /// Add information to the script instruction.
            /// </summary>
            /// <param name="raw"><see cref="Raw"/></param>
            /// <param name="line"><see cref="Line"/></param>
            /// <param name="lineoffset"><see cref="LineOffset"/></param>
            /// <returns>Returns itself for one-line constructor additional information.</returns>
            public Intermediate Info(string raw = "", int? line = null, int? lineoffset = null)
            {
                if (raw != "")
                {
                    Raw = raw;
                }
                if (line.HasValue)
                {
                    Line = line.Value;
                }
                if (lineoffset.HasValue)
                {
                    LineOffset = lineoffset.Value;
                }
                return this;
            }

            /// <summary>
            /// Quick access to <see cref="Parameters"/> of this instruction.
            /// </summary>
            /// <param name="index">The index of the parameter.</param>
            /// <returns>Parameter's <see cref="Interpreter.Execution"/>. <see cref="Func{object}"/></returns>
            public Func<object> GetParam(int index)
            {
                return Parameters[index].Execution.Instruction;
            }

            public string AggregateParams()
            {
                aggregationBuffer = "";
                for (int i = 0; i < Parameters.Count; i++)
                {
                    aggregationBuffer += Parameters[i].Execution.Instruction.Invoke()?.ToString();
                }
                return aggregationBuffer;
            }

            /// <summary>
            /// Summarize the intermediate instruction back to script-form for ease of readability.
            /// </summary>
            /// <returns>Script-form reconstruction of intermediate instruction.</returns>
            public override string ToString()
            {
                string result = "";

                if (Type == InstructionType.Command)
                {
                    result += Indent();
                    result += "<";
                    result += $"{InstructionName}(";
                }
                else if (Type == InstructionType.Function)
                {
                    result += "[";
                    result += $"{InstructionName}(";
                }
                else if (Type == InstructionType.String)
                {
                    result += "\"";
                    result += $"{InstructionName}";
                }
                else if (Type == InstructionType.None)
                {
                    result += "ERRORPARSINGTYPE";
                    result += $"({InstructionName}";
                }

                for (int i = 0; i < Parameters.Count; i++)
                {
                    result += Parameters[i].ToString();
                    if (i != Parameters.Count - 1)
                    {
                        result += ", ";
                    }
                }

                if (Type == InstructionType.Command)
                {
                    result += ")>";
                }
                else if (Type == InstructionType.Function)
                {
                    result += ")]";
                }
                else if (Type == InstructionType.String)
                {
                    result += "\"";
                }
                else if (Type == InstructionType.None)
                {
                    result += ")";
                }

                if (Block.Count > 0)
                {
                    IndentationLevel++;
                    result += $"{Environment.NewLine}{Indent(-1)}{{{Environment.NewLine}";
                    for (int i = 0; i < Block.Count; i++)
                    {
                        result += $"{Block[i]}";
                        if (i < Block.Count - 1)
                        {
                            result += Environment.NewLine;
                        }
                    }
                    result += $"{Environment.NewLine}{Indent(-1)}}}";
                    IndentationLevel--;
                }

                return result;
            }

            private static int IndentationLevel = 0;
            private string indentLocal;
            private string Indent(int modifier = 0)
            {
                indentLocal = "";
                for (int i = 0; i < IndentationLevel + modifier; i++)
                {
                    indentLocal += "\t";
                }
                return indentLocal;
            }

            /// <summary>
            /// Type of an intermediate instruction.
            /// </summary>
            public enum InstructionType
            {
                /// <summary>
                /// Indicates a parsing error since no <see cref="InstructionType"/> was successfully assigned in interpretation-time.
                /// </summary>
                None,
                /// <summary>
                /// An instruction to be executed without a return value nor further processing after returning.
                /// </summary>
                Command,
                /// <summary>
                /// An instruction to be executed to provide a return value to another instruction's parameter position.
                /// </summary>
                Function,
                /// <summary>
                /// An instruction that returns itself without any interpretation-time filtering applied. E.g.: Preserving empty characters, space, new lines, etc.
                /// </summary>
                String
            }
        }

        /// <summary>
        /// Represents a scope of the script. Respectively consisting of <see cref="Intermediate.Block"/>s and the script itself as the base <see cref="Scope"/>.
        /// </summary>
        public class Scope
        {
            public int Identifier;
            public Execution[] Executions;
            public int LineNumber;
            public string Initiator;

            public Scope(int identifier, Execution[] executions, string initiator)
            {
                Identifier = identifier;
                Executions = executions;
                Initiator = initiator;
                LineNumber = 0;
            }

            /// <summary>
            /// Gets the current <see cref="Execution"/> from <see cref="Executions"/> with the index <see cref="LineNumber"/>.
            /// </summary>
            public Execution CurrentLine
            {
                get
                {
                    return Executions[LineNumber];
                }
            }
        }

        /// <summary>
        /// A script variable in memory.
        /// </summary>
        public class Variable
        {
            public string Identifier;
            public object Value;

            public Variable(string identifier, object value)
            {
                Identifier = identifier;
                Value = value;
            }
        }

        /// <summary>
        /// Interpret the entirety of the script into an <see cref="Intermediate"/> array to be processed to C# instructions.
        /// </summary>
        /// <param name="script">The entire script in string form.</param>
        /// <returns><see cref="Intermediate"/> array.</returns>
        public static Intermediate[] Interpret(string script)
        {
            List<Intermediate> result = new List<Intermediate>();

            Stack<Intermediate> captureStack = new Stack<Intermediate>();
            Intermediate currentIntermediate = null;
            Intermediate lastBlockCandidate = null;

            string currentCapture = "";
            char currentChar;
            bool capturingString = false;
            bool escapeNextChar = false;
            int lineNumber;
            //Error checking and exception handling
            string captureSinceSuccess = "";

            //Iterate through all characters in script
            for (int i = 0; i < script.Length; i++)
            {
                currentChar = script[i];

                //Skip all visual syntax for user convenience unless capturing string
                if (!escapeNextChar && !capturingString && (currentChar == ' ' || currentChar == '\t' || currentChar == '\n' || currentChar == '\r'))
                {
                    continue;
                }

                lineNumber = GetLineNumber(script, i);

                //Capture currently processed character
                currentCapture += currentChar;
                captureSinceSuccess += currentChar;

                //Handle string operations
                if (SequenceLast(currentCapture, Syntax.StringSwitch) && !escapeNextChar)
                {
                    currentCapture = RemoveSequenceLast(currentCapture, Syntax.StringSwitch);
                    capturingString = !capturingString;
                }
                else if (SequenceLast(currentCapture, Syntax.StringEscape) && !escapeNextChar)
                {
                    currentCapture = RemoveSequenceLast(currentCapture, Syntax.StringEscape);
                    escapeNextChar = true;
                }
                else
                {
                    //Account for string operations
                    if (!escapeNextChar)
                    {
                        if (!capturingString)
                        {
                            //Parse based on previously captured character
                            if (SequenceLast(currentCapture, Syntax.CommandBegin))
                            {
                                if (lastBlockCandidate != null)
                                {
                                    lastBlockCandidate.Block.Add(new Intermediate(Intermediate.InstructionType.Command).Info(captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                    captureStack.Push(lastBlockCandidate.Block[lastBlockCandidate.Block.Count - 1]);
                                }
                                else
                                {
                                    captureStack.Push(new Intermediate(Intermediate.InstructionType.Command).Info(captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                }
                                currentCapture = RemoveSequenceLast(currentCapture, Syntax.CommandBegin);
                            }
                            else if (SequenceLast(currentCapture, Syntax.ParameterFieldBegin))
                            {
                                currentCapture = RemoveSequenceLast(currentCapture, Syntax.ParameterFieldBegin);
                                if (currentCapture == "")
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No name provided for instruction.", "Instruction name must be provided.", captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                    return null;
                                }
                                if (captureStack.TryPeek(out currentIntermediate))
                                {
                                    currentIntermediate.InstructionName = currentCapture;
                                }
                                else
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No previous instruction found to attach this parameter field.", "Parameter fields must be attached to an instruction. E.g.: a command or a function", captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                    return null;
                                }
                                currentCapture = "";
                            }
                            else if (SequenceLast(currentCapture, Syntax.ParameterSeperator))
                            {
                                currentCapture = RemoveSequenceLast(currentCapture, Syntax.ParameterSeperator);

                                //Skip parameter addition if the provided parameter is null. Solution to edge-case where a function is already parsed for the paramter slot before the seperator
                                if (currentCapture != "")
                                {
                                    if (captureStack.TryPeek(out currentIntermediate))
                                    {
                                        currentIntermediate.Parameters.Add(new Intermediate(currentCapture, Intermediate.InstructionType.String).Info(captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                    }
                                    else
                                    {
                                        Exceptions.Throw(new Exceptions.SyntaxException("No previous parameter field found to attach this parameter.", "Parameters must be attached to parameter field.", captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                        return null;
                                    }
                                }
                                currentCapture = "";
                            }
                            else if (SequenceLast(currentCapture, Syntax.ParameterFieldEnd))
                            {
                                currentCapture = RemoveSequenceLast(currentCapture, Syntax.ParameterFieldEnd);

                                //Skip parameter addition if the provided parameter is null. Solution to edge-case where a function is already parsed for the paramter slot before the seperator
                                if (currentCapture != "")
                                {
                                    if (captureStack.TryPeek(out currentIntermediate))
                                    {
                                        currentIntermediate.Parameters.Add(new Intermediate(currentCapture, Intermediate.InstructionType.String).Info(captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                    }
                                    else
                                    {
                                        Exceptions.Throw(new Exceptions.SyntaxException("No previous parameter field to close.", "A parameter field must begin before a parameter field ending.", captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                        return null;
                                    }
                                }
                                currentCapture = "";
                            }
                            else if (SequenceLast(currentCapture, Syntax.CommandEnd))
                            {
                                currentCapture = RemoveSequenceLast(currentCapture, Syntax.CommandEnd);
                                if (captureStack.TryPop(out currentIntermediate))
                                {
                                    if (lastBlockCandidate == null)
                                    {
                                        result.Add(currentIntermediate);
                                    }
                                    currentIntermediate.Info(captureSinceSuccess, null, null);
                                    captureSinceSuccess = "";
                                }
                                else
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No command was provided before ending command.", "Expected \"<\" before \">\".", captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                    return null;
                                }
                                currentCapture = "";
                            }
                            else if (SequenceLast(currentCapture, Syntax.FunctionBegin))
                            {
                                currentCapture = RemoveSequenceLast(currentCapture, Syntax.FunctionBegin);
                                if (captureStack.TryPeek(out currentIntermediate))
                                {
                                    captureStack.Push(new Intermediate(Intermediate.InstructionType.Function).Info(captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                    currentIntermediate.Parameters.Add(captureStack.Peek());
                                }
                                else
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No instruction was provided for function to be assigned to.", "Functions must be attached to the parameter field of another instruction.", captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                    return null;
                                }
                                currentCapture = "";
                            }
                            else if (SequenceLast(currentCapture, Syntax.FunctionEnd))
                            {
                                currentCapture = RemoveSequenceLast(currentCapture, Syntax.FunctionEnd);
                                if (!captureStack.TryPop(out currentIntermediate))
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No function was provided before ending function.", "Expected \"[\" before \"]\".", captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                    return null;
                                }
                                currentIntermediate.Info(captureSinceSuccess, null, null);
                                currentCapture = "";
                            }
                            else if (SequenceLast(currentCapture, Syntax.BlockBegin))
                            {
                                currentCapture = RemoveSequenceLast(currentCapture, Syntax.BlockBegin);
                                if (currentIntermediate == null)
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No command was provided to attach this block.", "Expected a command before the block was declared.", captureSinceSuccess, lineNumber, GetLineOffset(script, i)));
                                    return null;
                                }
                                lastBlockCandidate = currentIntermediate;
                                currentCapture = "";
                            }
                            else if (SequenceLast(currentCapture, Syntax.BlockEnd))
                            {
                                currentCapture = RemoveSequenceLast(currentCapture, Syntax.BlockEnd);
                                lastBlockCandidate = null;
                                currentCapture = "";
                            }
                        }
                    }
                    else
                    {
                        escapeNextChar = false;
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// C# instruction wrapper.
        /// </summary>
        public class Execution
        {
            public Func<object> Instruction;

            public Execution(Func<object> instruction)
            {
                Instruction = instruction;
            }
        }

        /// <summary>
        /// Compile an array of <see cref="Intermediate"/> objects to C# instructions (<see cref="Execution"/>).
        /// </summary>
        /// <param name="intermediates"><see cref="Intermediate"/> array.<br/><seealso cref="Interpret(string)"/></param>
        /// <returns><c>Func&lt;object&gt;[]</c><br/><see cref="Func{object}"/></returns>
        public static Execution[] Compile(Intermediate[] intermediates)
        {
            List<Execution> result = new List<Execution>();

            for (int i = 0; i < intermediates.Length; i++)
            {
                Execution buffer = Compile(intermediates[i]);
                if (buffer != null)
                {
                    result.Add(buffer);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Compile an <see cref="Intermediate"/> object to C# instructions (<see cref="Execution"/>).
        /// </summary>
        /// <param name="intermediates"><see cref="Intermediate"/> object.<br/><seealso cref="Interpret(string)"/></param>
        /// <returns><c>Func&lt;object&gt;</c><br/><see cref="Func{object}"/></returns>
        public static Execution Compile(Intermediate intermediate)
        {
            Func<object> result = null;

            Execution executionBuffer;

            for (int i = 0; i < intermediate.Parameters.Count; i++)
            {
                if (intermediate.Parameters[i].Execution == null)
                {
                    executionBuffer = Compile(intermediate.Parameters[i]);
                    if (executionBuffer != null)
                    {
                        intermediate.Parameters[i].Execution = executionBuffer;
                    }
                }
            }

            for (int i = 0; i < intermediate.Block.Count; i++)
            {
                if (intermediate.Block[i].Execution == null)
                {
                    executionBuffer = Compile(intermediate.Block[i]);
                    if (executionBuffer != null)
                    {
                        intermediate.Block[i].Execution = executionBuffer;
                    }
                }
            }
            
            if (intermediate.Type == Intermediate.InstructionType.String)
            {
                result = () => { return intermediate.InstructionName; };
            }
            else if (intermediate.Type == Intermediate.InstructionType.Command)
            {
                switch (intermediate.InstructionName.ToLower())
                {
                    case "setmouse":
                        result = () =>
                        {
                            Functions.SetMouse(intermediate.GetParam(0).Invoke().HandledCast<int>(intermediate), intermediate.GetParam(1).Invoke().HandledCast<int>(intermediate));
                            return null;
                        };
                        break;
                    case "print":
                        result = () =>
                        {
                            Functions.Print(intermediate);
                            return null;
                        };
                        break;
                    case "wait":
                        result = () =>
                        {
                            MainWindow.Instance.SetLineProgression(false);
                            if (!Functions.WaitInitialized)
                            {
                                Functions.StartWait();
                                MainWindow.Instance.PostInterruptionList.Add(Functions.StopWait);
                            }
                            if (Functions.IsWaitComplete(intermediate.GetParam(0).Invoke().HandledCast<long>(intermediate) * TimeSpan.TicksPerMillisecond))
                            {
                                Functions.StopWait();
                                MainWindow.Instance.SetLineProgression(true);
                            }
                            return null;
                        };
                        break;
                    case "if":
                        int guid = intermediate.GetHashCode();
                        intermediate.GUID = guid;
                        MainWindow.Instance.AddScope(new Scope(guid, intermediate.Block.Select(i => i.Execution).ToArray(), intermediate.Raw));
                        result = () =>
                        {
                            if (intermediate.GetParam(0).Invoke().HandledCast<bool>(intermediate) == true)
                            {
                                MainWindow.Instance.ProgressCurrentScope();
                                MainWindow.Instance.SwitchScope(guid, true);
                                MainWindow.Instance.ScopedInThisTick = true;
                            }
                            return null;
                        };
                        break;
                    case "else":
                        result = () =>
                        {
                            //Hook to the last "if" compiled? Store last "if" compiled in a variable then hook to it here
                            return null;
                        };
                        break;
                    case "cvar":
                        result = () =>
                        {
                            Functions.CreateVariable(intermediate.GetParam(0).Invoke().ToString(), intermediate.GetParam(1).Invoke(), intermediate);
                            return null;
                        };
                        break;
                    case "svar":
                        result = () =>
                        {
                            Functions.GetVariable(intermediate.GetParam(0).Invoke().ToString(), intermediate).Value = intermediate.GetParam(1).Invoke();
                            return null;
                        };
                        break;
                    case "start":
                        MainWindow.Instance.RegisterStartScope(new Scope(MainWindow.Instance.StartScopeID, intermediate.Block.Select(i => i.Execution).ToArray(), intermediate.Raw));
                        result = null;
                        break;
                    case "stop":
                        MainWindow.Instance.RegisterStopScope(new Scope(MainWindow.Instance.StopScopeID, intermediate.Block.Select(i => i.Execution).ToArray(), intermediate.Raw));
                        result = null;
                        break;
                    default:
                        Exceptions.Throw(new Exceptions.ParsingException($"No command found by name \"{intermediate.InstructionName}\".", $"Command \"{intermediate.InstructionName}\" does not exist.", intermediate.Raw, intermediate.Line, intermediate.LineOffset));
                        break;
                }
            }
            else if (intermediate.Type == Intermediate.InstructionType.Function)
            {
                switch (intermediate.InstructionName.ToLower())
                {
                    case "hotkey":
                        result = () =>
                        {
                            return Functions.TestKey(intermediate.GetParam(0).Invoke().HandledCast<int>(intermediate).HandledCast<System.Windows.Input.Key>(intermediate));
                        };
                        break;
                    case "math":
                        result = () =>
                        {
                            return Functions.Math(intermediate.AggregateParams(), intermediate);
                        };
                        break;
                    case "var":
                        result = () =>
                        {
                            return Functions.GetVariable(intermediate.GetParam(0).Invoke().ToString(), intermediate)?.Value;
                        };  
                        break;
                    case "equals":
                        result = () =>
                        {
                            return Functions.IsEqual(intermediate.GetParam(0).Invoke(), intermediate.GetParam(1).Invoke());
                        };
                        break;
                    case "long":
                        result = () =>
                        {
                            return intermediate.GetParam(0).Invoke().HandledCast<long>(intermediate);
                        };
                        break;
                    default:
                        Exceptions.Throw(new Exceptions.ParsingException($"No function found by name \"{intermediate.InstructionName}\".", $"Function \"{intermediate.InstructionName}\" does not exist.", intermediate.Raw, intermediate.Line, intermediate.LineOffset));
                        break;
                }
            }
            if (result == null)
            {
                return null;
            }
            return new Execution(result);
        }

        /// <summary>
        /// Get the line number of the character at <paramref name="index"/> in <paramref name="input"/>.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="index">The character's index.</param>
        /// <returns>The line number of character at <paramref name="index"/> in <paramref name="input"/>.</returns>
        private static int GetLineNumber(string input, int index)
        {
            return input.Substring(0, index).Count(c => c == '\n' || c == '\r') + 1;
        }

        /// <summary>
        /// Get the amount of characters that offset the character at <paramref name="index"/> in its line.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="index">The character's index.</param>
        /// <returns>The amount of characters that offset the character at <paramref name="index"/> in <paramref name="input"/>.</returns>
        private static int GetLineOffset(string input, int index)
        {
            input = input.Substring(0, index);
            int nlIndex = input.LastIndexOf("\n");
            int crIndex = input.LastIndexOf("\r");
            if (nlIndex >= 0 && crIndex >= 0)
            {
                if (nlIndex > crIndex)
                {
                    return index - nlIndex;
                }
                else
                {
                    return index - crIndex;
                }
            }
            return index;
        }

        /// <summary>
        /// Check if the <paramref name="sequence"/> exists in the end of the <paramref name="input"/>.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="sequence">The sequence to be searched.</param>
        /// <returns>Returns <see langword="true"/> if the <paramref name="sequence"/> exists in the end of the <paramref name="input"/>, <see langword="false"/> otherwise.</returns>
        private static bool SequenceLast(string input, string sequence)
        {
            if (input.Length < sequence.Length)
            {
                return false;
            }
            return (input.Substring(input.Length - sequence.Length) == sequence);
        }

        /// <summary>
        /// Remove the <paramref name="sequence"/> from the <paramref name="input"/>. Assumes the <paramref name="sequence"/> already exists in the ending of the <paramref name="input"/>.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="sequence">The sequence to be removed from <paramref name="input"/>.</param>
        /// <returns>The remainder of <paramref name="input"/> with the <paramref name="sequence"/> removed from the end.</returns>
        private static string RemoveSequenceLast(string input, string sequence)
        {
            if (input.Length < sequence.Length)
            {
                return input;
            }
            return input.Remove(input.Length - sequence.Length);
        }
    }
}