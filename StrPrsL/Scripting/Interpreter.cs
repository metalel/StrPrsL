using StrPrsL.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

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

            /// <summary>
            /// The command name for a special conditional command which has a block attached.
            /// </summary>
            public const string ConditionalBlockCommandName = "if";
            /// <summary>
            /// The command name for a special negative conditional command which has a block attached.
            /// </summary>
            public const string ConditionalNegativeBlockCommandName = "else";
            /// <summary>
            /// Lower case version of <see cref="ConditionalBlockCommandName"/>.
            /// </summary>
            public const string ConditionalBlockCommandName_Lower = "if";
            /// <summary>
            /// Lower case version of <see cref="ConditionalNegativeBlockCommandName"/>.
            /// </summary>
            public const string ConditionalNegativeBlockCommandName_Lower = "else";
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
            /// <summary>
            /// The <code>&lt;Else()&gt;</code> block for <code>&lt;If()&gt;</code> connected at compile-time. When an Else command is encountered, it is connected to the <see cref="Else"/> property of the last compiled If command.
            /// </summary>
            public Intermediate Else;
            /// <summary>
            /// Determines whether this instruction should be automatically added to the compiled script or if it should only be callable from elsewhere in the script.
            /// </summary>
            public bool Automatic;
            /// <summary>
            /// The If-stack for the <see cref="Block"/> that belongs to this <see cref="Intermediate"/>.
            /// </summary>
            public Stack<Intermediate> BlockIfStack = new Stack<Intermediate>();

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
                Else = null;
                Automatic = true;
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
                Else = null;
                Automatic = true;
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

            /// <summary>
            /// Quick access to <see cref="Parameters"/> of this instruction by casting them via <see cref="Utility.Extensions.HandledCast{T}(object, Intermediate)"/>.
            /// </summary>
            /// <typeparam name="T">The <see cref="System.Type"/> that the result of the <see cref="Parameters"/>'s <see cref="Func{object}"/> result will be casted to.</typeparam>
            /// <param name="index">The index of the parameter from <see cref="Parameters"/>.</param>
            /// <returns><see cref="Parameters"/>'s <see cref="Func{object}"/> result casted to <typeparamref name="T"/>.</returns>
            public T InvokeParamCast<T>(int index)
            {
                return GetParam(index).Invoke().HandledCast<T>(this);
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

            public override string ToString()
            {
                if (Executions != null && Executions.Length > 0)
                {
                    return $"({Identifier}){Environment.NewLine}{string.Join(Environment.NewLine, Executions as IEnumerable<Execution>)}";
                }
                return base.ToString();
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
            Stack<Intermediate> blockStack = new Stack<Intermediate>();
            Intermediate currentBlock = null;

            string currentCapture = "";
            char currentChar;
            bool capturingString = false;
            bool escapeNextChar = false;
            int lineNumber;
            //Error checking and exception handling
            string captureSinceSuccess = "";
            //Special interpretation
            Stack<Intermediate> mainIfStack = new Stack<Intermediate>();
            Intermediate currentIf;

            //Iterate through all characters in script
            for (int i = 0; i < script.Length; i++)
            {
                currentChar = script[i];

                //Skip all visual syntax for user convenience unless capturing string
                if (!escapeNextChar && !capturingString && (currentChar == ' ' || currentChar == '\t' || currentChar == '\n' || currentChar == '\r'))
                {
                    continue;
                }

                lineNumber = Helpers.GetLineNumber(script, i);

                //Capture currently processed character
                currentCapture += currentChar;
                captureSinceSuccess += currentChar;

                //Handle string operations
                if (Helpers.SequenceLast(currentCapture, Syntax.StringSwitch) && !escapeNextChar)
                {
                    currentCapture = Helpers.RemoveSequenceLast(currentCapture, Syntax.StringSwitch);
                    capturingString = !capturingString;
                }
                else if (Helpers.SequenceLast(currentCapture, Syntax.StringEscape) && !escapeNextChar)
                {
                    currentCapture = Helpers.RemoveSequenceLast(currentCapture, Syntax.StringEscape);
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
                            if (Helpers.SequenceLast(currentCapture, Syntax.CommandBegin))
                            {
                                if (blockStack.TryPeek(out currentBlock))
                                {
                                    currentBlock.Block.Add(new Intermediate(Intermediate.InstructionType.Command).Info(captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                    captureStack.Push(currentBlock.Block[currentBlock.Block.Count - 1]);
                                }
                                else
                                {
                                    captureStack.Push(new Intermediate(Intermediate.InstructionType.Command).Info(captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                }
                                currentCapture = Helpers.RemoveSequenceLast(currentCapture, Syntax.CommandBegin);
                            }
                            else if (Helpers.SequenceLast(currentCapture, Syntax.ParameterFieldBegin))
                            {
                                currentCapture = Helpers.RemoveSequenceLast(currentCapture, Syntax.ParameterFieldBegin);
                                if (currentCapture == "")
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No name provided for instruction.", "Instruction name must be provided.", captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                    return null;
                                }
                                if (captureStack.TryPeek(out currentIntermediate))
                                {
                                    currentIntermediate.InstructionName = currentCapture;
                                }
                                else
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No previous instruction found to attach this parameter field.", "Parameter fields must be attached to an instruction. E.g.: a command or a function", captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                    return null;
                                }
                                currentCapture = "";
                            }
                            else if (Helpers.SequenceLast(currentCapture, Syntax.ParameterSeperator))
                            {
                                currentCapture = Helpers.RemoveSequenceLast(currentCapture, Syntax.ParameterSeperator);

                                //Skip parameter addition if the provided parameter is null. Solution to edge-case where a function is already parsed for the paramter slot before the seperator
                                if (currentCapture != "")
                                {
                                    if (captureStack.TryPeek(out currentIntermediate))
                                    {
                                        currentIntermediate.Parameters.Add(new Intermediate(currentCapture, Intermediate.InstructionType.String).Info(captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                    }
                                    else
                                    {
                                        Exceptions.Throw(new Exceptions.SyntaxException("No previous parameter field found to attach this parameter.", "Parameters must be attached to parameter field.", captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                        return null;
                                    }
                                }
                                currentCapture = "";
                            }
                            else if (Helpers.SequenceLast(currentCapture, Syntax.ParameterFieldEnd))
                            {
                                currentCapture = Helpers.RemoveSequenceLast(currentCapture, Syntax.ParameterFieldEnd);

                                //Skip parameter addition if the provided parameter is null. Solution to edge-case where a function is already parsed for the paramter slot before the seperator
                                if (currentCapture != "")
                                {
                                    if (captureStack.TryPeek(out currentIntermediate))
                                    {
                                        currentIntermediate.Parameters.Add(new Intermediate(currentCapture, Intermediate.InstructionType.String).Info(captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                    }
                                    else
                                    {
                                        Exceptions.Throw(new Exceptions.SyntaxException("No previous parameter field to close.", "A parameter field must begin before a parameter field ending.", captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                        return null;
                                    }
                                }
                                currentCapture = "";
                            }
                            else if (Helpers.SequenceLast(currentCapture, Syntax.CommandEnd))
                            {
                                currentCapture = Helpers.RemoveSequenceLast(currentCapture, Syntax.CommandEnd);
                                if (captureStack.TryPop(out currentIntermediate))
                                {
                                    currentIntermediate.Info(captureSinceSuccess, null, null);
                                    captureSinceSuccess = "";

                                    if (currentIntermediate.InstructionName.ToLower() == Syntax.ConditionalNegativeBlockCommandName.ToLower())
                                    {
                                        currentIf = null;
                                        if (blockStack.TryPeek(out currentBlock))
                                        {
                                            currentBlock.BlockIfStack.TryPop(out currentIf);
                                        }
                                        else
                                        {
                                            mainIfStack.TryPop(out currentIf);
                                        }

                                        if (currentIf != null)
                                        {
                                            currentIf.Else = currentIntermediate;
                                        }
                                        else
                                        {
                                            Exceptions.Throw(new Exceptions.SyntaxException("No conditional command was provided before negative conditional command.", $"Expected \"{Syntax.ConditionalBlockCommandName}\" before \"{Syntax.ConditionalNegativeBlockCommandName}\".", captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                        }
                                    }
                                    else
                                    {
                                        if (currentIntermediate.InstructionName.ToLower() == Syntax.ConditionalBlockCommandName.ToLower())
                                        {
                                            if (blockStack.TryPeek(out currentBlock))
                                            {
                                                currentBlock.BlockIfStack.Push(currentIntermediate);
                                            }
                                            else
                                            {
                                                mainIfStack.Push(currentIntermediate);
                                            }
                                        }

                                        if (!blockStack.TryPeek(out currentBlock))
                                        {
                                            result.Add(currentIntermediate);
                                        }
                                    }
                                }
                                else
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No command was provided before ending command.", "Expected \"<\" before \">\".", captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                    return null;
                                }
                                currentCapture = "";
                            }
                            else if (Helpers.SequenceLast(currentCapture, Syntax.FunctionBegin))
                            {
                                currentCapture = Helpers.RemoveSequenceLast(currentCapture, Syntax.FunctionBegin);
                                if (captureStack.TryPeek(out currentIntermediate))
                                {
                                    captureStack.Push(new Intermediate(Intermediate.InstructionType.Function).Info(captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                    currentIntermediate.Parameters.Add(captureStack.Peek());
                                }
                                else
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No instruction was provided for function to be assigned to.", "Functions must be attached to the parameter field of another instruction.", captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                    return null;
                                }
                                currentCapture = "";
                            }
                            else if (Helpers.SequenceLast(currentCapture, Syntax.FunctionEnd))
                            {
                                currentCapture = Helpers.RemoveSequenceLast(currentCapture, Syntax.FunctionEnd);
                                if (!captureStack.TryPop(out currentIntermediate))
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No function was provided before ending function.", "Expected \"[\" before \"]\".", captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                    return null;
                                }
                                currentIntermediate.Info(captureSinceSuccess, null, null);
                                currentCapture = "";
                            }
                            else if (Helpers.SequenceLast(currentCapture, Syntax.BlockBegin))
                            {
                                currentCapture = Helpers.RemoveSequenceLast(currentCapture, Syntax.BlockBegin);
                                if (currentIntermediate == null)
                                {
                                    Exceptions.Throw(new Exceptions.SyntaxException("No command was provided to attach this block.", "Expected a command before the block was declared.", captureSinceSuccess, lineNumber, Helpers.GetLineOffset(script, i)));
                                    return null;
                                }
                                blockStack.Push(currentIntermediate);
                                currentCapture = "";
                            }
                            else if (Helpers.SequenceLast(currentCapture, Syntax.BlockEnd))
                            {
                                currentCapture = Helpers.RemoveSequenceLast(currentCapture, Syntax.BlockEnd);
                                blockStack.Pop();
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

            public Intermediate Origin
            {
                get;
                private set;
            }

            public Execution(Func<object> instruction, Intermediate origin)
            {
                Instruction = instruction;
                Origin = origin;
            }

            public override string ToString()
            {
                if (Origin == null)
                {
                    return base.ToString();
                }
                return Origin.ToString();
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
                if (intermediates[i].Execution == null)
                {
                    intermediates[i].Execution = buffer;
                }
                if (buffer != null && intermediates[i].Automatic)
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

            #region Pre-Compilation
            //Compile conditional
            if (intermediate.Else != null)
            {
                intermediate.Else.Execution = Compile(intermediate.Else);
            }

            //Compile parameters
            for (int i = 0; i < intermediate.Parameters.Count; i++)
            {
                if (intermediate.Parameters[i].Execution == null)
                {
                    Execution executionBuffer = Compile(intermediate.Parameters[i]);
                    if (executionBuffer != null)
                    {
                        intermediate.Parameters[i].Execution = executionBuffer;
                    }
                }
            }

            //Clean up parameters
            for (int i = intermediate.Parameters.Count - 1; i >= 0; i--)
            {
                if (intermediate.Parameters[i].Execution == null || !intermediate.Parameters[i].Automatic)
                {
                    intermediate.Parameters.RemoveAt(i);
                }
            }

            //Compile block
            for (int i = 0; i < intermediate.Block.Count; i++)
            {
                if (intermediate.Block[i].Execution == null)
                {
                    Execution executionBuffer = Compile(intermediate.Block[i]);
                    if (executionBuffer != null)
                    {
                        intermediate.Block[i].Execution = executionBuffer;
                    }
                }
            }

            //Clean up block
            for (int i = intermediate.Block.Count - 1; i >= 0; i--)
            {
                if (intermediate.Block[i].Execution == null || !intermediate.Block[i].Automatic)
                {
                    intermediate.Block.RemoveAt(i);
                }
            }
            #endregion

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
                            Functions.SetMouse(intermediate.InvokeParamCast<int>(0), intermediate.InvokeParamCast<int>(1));
                            return null;
                        };
                        break;
                    case "leftclick":
                        result = () =>
                        {
                            Functions.LeftClick();
                            return null;
                        };
                        break;
                    case "rightclick":
                        result = () =>
                        {
                            Functions.RightClick();
                            return null;
                        };
                        break;
                    case "middleclick":
                        result = () =>
                        {
                            Functions.MiddleClick();
                            return null;
                        };
                        break;
                    case "leftdown":
                        result = () =>
                        {
                            Functions.LeftDown();
                            return null;
                        };
                        break;
                    case "leftup":
                        result = () =>
                        {
                            Functions.LeftUp();
                            return null;
                        };
                        break;
                    case "rightdown":
                        result = () =>
                        {
                            Functions.RightDown();
                            return null;
                        };
                        break;
                    case "rightup":
                        result = () =>
                        {
                            Functions.RightUp();
                            return null;
                        };
                        break;
                    case "middledown":
                        result = () =>
                        {
                            Functions.MiddleDown();
                            return null;
                        };
                        break;
                    case "middleup":
                        result = () =>
                        {
                            Functions.MiddleUp();
                            return null;
                        };
                        break;
                    case "scrollup":
                        result = () =>
                        {
                            Functions.ScrollUp();
                            return null;
                        };
                        break;
                    case "scrolldown":
                        result = () =>
                        {
                            Functions.ScrollDown();
                            return null;
                        };
                        break;
                    case "movemouse":
                        result = () =>
                        {
                            Functions.MoveMouse(intermediate.GetParam(0).Invoke().HandledCast<int>(intermediate), intermediate.GetParam(1).Invoke().HandledCast<int>(intermediate));
                            return null;
                        };
                        break;
                    case "print":
                        result = () =>
                        {
                            Functions.Print(intermediate.AggregateParams(), intermediate);
                            return null;
                        };
                        break;
                    case "wait":
                        result = () =>
                        {
                            MainWindow.Instance.SetLineProgression(false);
                            if (!Functions.WaitingManager.WaitInitialized)
                            {
                                Functions.WaitingManager.StartWait();
                                MainWindow.Instance.PostInterruptionList.Add(Functions.WaitingManager.StopWait);
                            }
                            if (Functions.WaitingManager.IsWaitComplete(intermediate.GetParam(0).Invoke().HandledCast<long>(intermediate) * TimeSpan.TicksPerMillisecond))
                            {
                                Functions.WaitingManager.StopWait();
                                MainWindow.Instance.SetLineProgression(true);
                            }
                            return null;
                        };
                        break;
                    case "sleep":
                        result = () =>
                        {
                            Functions.Sleep(intermediate.GetParam(0).Invoke().HandledCast<int>(intermediate));
                            return null;
                        };
                        break;
                    case Syntax.ConditionalBlockCommandName_Lower:
                        int guid = intermediate.GetHashCode();
                        intermediate.GUID = guid;
                        MainWindow.Instance.AddScope(new Scope(guid, intermediate.Block.Select(i => i.Execution).ToArray(), intermediate.Raw));
                        result = () =>
                        {
                            if (intermediate.GetParam(0).Invoke().HandledCast<bool>(intermediate) == true)
                            {
                                MainWindow.Instance.ProgressCurrentScopeOnce();
                                MainWindow.Instance.SwitchScope(guid, true);
                                MainWindow.Instance.ScopedInOnThisTick = true;
                            }
                            else if (intermediate.Else != null)
                            {
                                intermediate.Else.Execution.Instruction.Invoke();
                            }
                            return null;
                        };
                        break;
                    case Syntax.ConditionalNegativeBlockCommandName_Lower:
                        int elseGuid = intermediate.GetHashCode();
                        intermediate.GUID = elseGuid;
                        MainWindow.Instance.AddScope(new Scope(elseGuid, intermediate.Block.Select(i => i.Execution).ToArray(), intermediate.Raw));
                        intermediate.Automatic = false;
                        result = () =>
                        {
                            MainWindow.Instance.ProgressCurrentScopeOnce();
                            MainWindow.Instance.SwitchScope(elseGuid, true);
                            MainWindow.Instance.ScopedInOnThisTick = true;
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
                        intermediate.Automatic = false;
                        result = null;
                        break;
                    case "stop":
                        MainWindow.Instance.RegisterStopScope(new Scope(MainWindow.Instance.StopScopeID, intermediate.Block.Select(i => i.Execution).ToArray(), intermediate.Raw));
                        intermediate.Automatic = false;
                        result = null;
                        break;
                    case "clearmessages":
                        result = () =>
                        {
                            MainWindow.Instance.ClearMessageQueue();
                            return null;
                        };
                        break;
                    case "clearoutput":
                        result = () =>
                        {
                            MainWindow.Instance.CrossThreadOperationBlockedCollection.Add(() => MainWindow.Instance.ClearOutput());
                            return null;
                        };
                        break;
                    case "setinterval":
                        result = () =>
                        {
                            MainWindow.Instance.CrossThreadOperationBlockedCollection.Add(() => MainWindow.Instance.UpdateLineInterval(intermediate.GetParam(0).Invoke().HandledCast<int>(intermediate)));
                            return null;
                        };
                        break;
                    case "uiblock":
                        result = () =>
                        {
                            MainWindow.Instance.CrossThreadOperationBlockedCollection.Add(() => MainWindow.Instance.UpdateUIThreadBlock(intermediate.GetParam(0).Invoke().HandledCast<bool>(intermediate)));
                            return null;
                        };
                        break;
                    case "aborthotkey":
                        result = () =>
                        {
                            MainWindow.Instance.CrossThreadOperationBlockedCollection.Add(() => MainWindow.Instance.UpdateAbortLoopKey(intermediate.GetParam(0).Invoke().HandledCast<int>(intermediate)));
                            return null;
                        };
                        break;
                    case "setloop":
                        result = () =>
                        {
                            MainWindow.Instance.CrossThreadOperationBlockedCollection.Add(() => MainWindow.Instance.UpdateLoopState(intermediate.GetParam(0).Invoke().HandledCast<bool>(intermediate)));
                            return null;
                        };
                        break;
                    case "getscreen":
                        result = () =>
                        {
                            Functions.GetDCCommand();
                            return null;
                        };
                        break;
                    case "releasescreen":
                        result = () =>
                        {
                            Functions.ReleaseDCCommand();
                            return null;
                        };
                        break;
                    case "sendstrokes":
                        result = () =>
                        {
                            Functions.SendKeysCommand(intermediate.GetParam(0).Invoke().ToString());
                            return null;
                        };
                        break;
                    case "keydown":
                        result = () =>
                        {
                            Functions.KeyDown(intermediate.GetParam(0).Invoke().HandledCast<int>(intermediate));
                            return null;
                        };
                        break;
                    case "keyup":
                        result = () =>
                        {
                            Functions.KeyUp(intermediate.GetParam(0).Invoke().HandledCast<int>(intermediate));
                            return null;
                        };
                        break;
                    case "keypress":
                        result = () =>
                        {
                            Functions.KeyPress(intermediate.GetParam(0).Invoke().HandledCast<int>(intermediate));
                            return null;
                        };
                        break;
                    case "windowstate":
                        result = () =>
                        {
                            Functions.ChangeWindowState(intermediate.InvokeParamCast<IntPtr>(0), intermediate.InvokeParamCast<int>(1));
                            return null;
                        };
                        break;
                    case "playaudio":
                        result = () =>
                        {
                            Functions.PlayAudio(intermediate.GetParam(0).Invoke().ToString());
                            return null;
                        };
                        break;
                    case "stopexecution":
                        result = () =>
                        {
                            MainWindow.Instance.StopScriptExecution(true);
                            return null;
                        };
                        break;
                    case "setstatus":
                        result = () =>
                        {
                            MainWindow.Instance.CrossThreadOperationBlockedCollection.Add(() => MainWindow.Instance.UpdateStatusEnabled(intermediate.InvokeParamCast<bool>(0)));
                            return null;
                        };
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
                            return Functions.GetKey(intermediate.GetParam(0).Invoke().HandledCast<int>(intermediate).HandledCast<System.Windows.Input.Key>(intermediate));
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
                    case "clamp":
                        result = () =>
                        {
                            return Functions.ClampDecimal(intermediate.InvokeParamCast<decimal>(0), intermediate.InvokeParamCast<decimal>(1), intermediate.InvokeParamCast<decimal>(2));
                        };
                        break;
                    case "chr":
                        result = () =>
                        {
                            return Functions.GetChar(intermediate.InvokeParamCast<int>(0));
                        };
                        break;
                    case "tostring":
                        result = () =>
                        {
                            return Functions.Stringify(intermediate.GetParam(0).Invoke());
                        };
                        break;
                    case "tobool":
                        result = () =>
                        {
                            return intermediate.InvokeParamCast<bool>(0);
                        };
                        break;
                    case "toint":
                        result = () =>
                        {
                            return intermediate.InvokeParamCast<int>(0);
                        };
                        break;
                    case "todec":
                        result = () =>
                        {
                            return intermediate.InvokeParamCast<decimal>(0);
                        };
                        break;
                    case "tolong":
                        result = () =>
                        {
                            return intermediate.GetParam(0).Invoke().HandledCast<long>(intermediate);
                        };
                        break;
                    case "color":
                        result = () =>
                        {
                            return Color.FromArgb(255, intermediate.InvokeParamCast<byte>(0), intermediate.InvokeParamCast<byte>(1), intermediate.InvokeParamCast<byte>(2));
                        };
                        break;
                    case "pixel":
                        result = () =>
                        {
                            return Functions.GetPixelColor(intermediate.InvokeParamCast<int>(0), intermediate.InvokeParamCast<int>(1));
                        };
                        break;
                    case "cursor":
                        result = () =>
                        {
                            return Functions.GetMousePosition(intermediate.InvokeParamCast<char>(0));
                        };
                        break;
                    case "conc":
                        result = () =>
                        {
                            return intermediate.AggregateParams();
                        };
                        break;
                    case "time":
                        result = () =>
                        {
                            return Functions.CurrentTimeTicks();
                        };
                        break;
                    case "audiopeak":
                        result = () =>
                        {
                            return Functions.AudioPeak();
                        };
                        break;
                    case "screensize":
                        result = () =>
                        {
                            return Functions.GetScreenSize(intermediate.InvokeParamCast<char>(0));
                        };
                        break;
                    case "getmousebutton":
                        result = () =>
                        {
                            return Functions.GetMouseButton(intermediate.InvokeParamCast<int>(0));
                        };
                        break;
                    case "and":
                        result = () =>
                        {
                            return intermediate.InvokeParamCast<bool>(0) && intermediate.InvokeParamCast<bool>(1);
                        };
                        break;
                    case "or":
                        result = () =>
                        {
                            return intermediate.InvokeParamCast<bool>(0) || intermediate.InvokeParamCast<bool>(1);
                        };
                        break;
                    case "activewindow":
                        result = () =>
                        {
                            return Functions.GetCurrentWindow();
                        };
                        break;
                    case "apppath":
                        result = () =>
                        {
                            return Functions.GetExecutablePath();
                        };
                        break;
                    case "desktop":
                        result = () =>
                        {
                            return Functions.GetDesktopPath();
                        };
                        break;
                    case "script":
                        result = () =>
                        {
                            return Functions.GetScriptPath();
                        };
                        break;
                    case "deltatime":
                        result = () =>
                        {
                            return Functions.GetTickDelta();
                        };
                        break;
                    case "keyname":
                        result = () =>
                        {
                            return intermediate.GetParam(0).Invoke().HandledCast<int>(intermediate).HandledCast<System.Windows.Input.Key>(intermediate).ToString();
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
            return new Execution(result, intermediate);
        }

        /// <summary>
        /// Provides access to helper methods.
        /// </summary>
        public static class Helpers
        {
            /// <summary>
            /// Get the line number of the character at <paramref name="index"/> in <paramref name="input"/>.
            /// </summary>
            /// <param name="input">The input string.</param>
            /// <param name="index">The character's index.</param>
            /// <returns>The line number of character at <paramref name="index"/> in <paramref name="input"/>.</returns>
            public static int GetLineNumber(string input, int index)
            {
                return input.Substring(0, index).Count(c => c == '\n' || c == '\r') + 1;
            }

            /// <summary>
            /// Get the amount of characters that offset the character at <paramref name="index"/> in its line.
            /// </summary>
            /// <param name="input">The input string.</param>
            /// <param name="index">The character's index.</param>
            /// <returns>The amount of characters that offset the character at <paramref name="index"/> in <paramref name="input"/>.</returns>
            public static int GetLineOffset(string input, int index)
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
            public static bool SequenceLast(string input, string sequence)
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
            public static string RemoveSequenceLast(string input, string sequence)
            {
                if (input.Length < sequence.Length)
                {
                    return input;
                }
                return input.Remove(input.Length - sequence.Length);
            }
        }
    }
}