using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrPrsL.Scripting.Completion
{
    public static class InstructionRegister
    {
        public static InstructionObject[] Instructions =
        {
            new InstructionObject(InstructionType.Command, "MoveMouse",  InstructionCategory.Input, "Moves the cursor by specified coordiante delta.", new InstructionParameter[] { new InstructionParameter("X", typeof(int)), new InstructionParameter("Y", typeof(int)) }),
            new InstructionObject(InstructionType.Command, "SetMouse", InstructionCategory.Input, "Sets the cursor position to specified coordinates.", new InstructionParameter[] { new InstructionParameter("X", typeof(int)), new InstructionParameter("Y", typeof(int)) }),
            new InstructionObject(InstructionType.Command, "Sleep", InstructionCategory.Performance, "Sleeps execution thread for specified duration (in miliseconds).", new InstructionParameter[] { new InstructionParameter("Duration", typeof(uint)) }),
            new InstructionObject(InstructionType.Command, "Wait", InstructionCategory.Performance, "Pauses execution for specified duration (in miliseconds).", new InstructionParameter[] { new InstructionParameter("Duration", typeof(uint)) }),
            new InstructionObject(InstructionType.Command, "Print", InstructionCategory.Debug, "Prints to the output.", new InstructionParameter[] { new InstructionParameter("Message", typeof(string)) }),
            new InstructionObject(InstructionType.Command, "ClearOutput", InstructionCategory.Debug, "Clears the output.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "LeftClick", InstructionCategory.Input,"Emulates a left click event where the cursor is.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "RightClick", InstructionCategory.Input, "Emulates a right click event where the cursor is.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "MiddleClick", InstructionCategory.Input, "Emulates a middle click event where the cursor is.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "LeftDown", InstructionCategory.Input, "Emulates a left mouse button being pressed event where the cursor is.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "LeftUp", InstructionCategory.Input, "Emulates a left mouse button being released event where the cursor is.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "RightDown", InstructionCategory.Input, "Emulates a right mouse button being pressed event where the cursor is.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "RightUp", InstructionCategory.Input, "Emulates a right mouse button being released event where the cursor is.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "MiddleDown", InstructionCategory.Input, "Emulates a middle mouse button being pressed event where the cursor is.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "MiddleUp", InstructionCategory.Input, "Emulates a middle mouse button being released event where the cursor is.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "ScrollUp", InstructionCategory.Input, "Emulates mouse wheel scrolling up where the cursor is.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "ScrollDown", InstructionCategory.Input, "Emulates mouse wheel scrolling down where the cursor is.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "CVar", InstructionCategory.Variable, "Creates a variable in memory.", new InstructionParameter[] { new InstructionParameter("Name", typeof(string)), new InstructionParameter("Value", typeof(object)) }),
            new InstructionObject(InstructionType.Command, "SVar", InstructionCategory.Variable, "Changes a variable's value.", new InstructionParameter[] { new InstructionParameter("Name", typeof(string)), new InstructionParameter("Value", typeof(object)) }),
            //new InstructionObject(InstructionType.Command, "Waiter", InstructionCategory.Performance, "Repeatedly executes an action until the expected result is achieved before progressing.", new InstructionParameter[] { new InstructionParameter("Action", typeof(bool)), new InstructionParameter("Result", typeof(bool)) }),
            //new InstructionObject(InstructionType.Command, "MessageBox", InstructionCategory.Performance, "Pauses execution to show a message box and resumes execution when the user clicks \"OK\".", new InstructionParameter[] { new InstructionParameter("Title", typeof(string)), new InstructionParameter("Text", typeof(string)) }),
            new InstructionObject(InstructionType.Command, "GetScreen", InstructionCategory.Performance, $"Gets the virtual screen device. Must be released when done using {Interpreter.Syntax.FunctionBegin}Pixel(...){Interpreter.Syntax.FunctionEnd}.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "ReleaseScreen", InstructionCategory.Performance, "Releases the virtual screen device.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "SendStrokes", InstructionCategory.Input, "Sends virtual input messages.", new InstructionParameter[] { new InstructionParameter("Message", typeof(string)) }),
            new InstructionObject(InstructionType.Command, "If", InstructionCategory.Evaluation, "Executes code inside its block if parameter retuns true.", new InstructionParameter[] { new InstructionParameter("Condition", typeof(bool)) }),
            new InstructionObject(InstructionType.Command, "Else", InstructionCategory.Evaluation, "Executes code inside its block if the previous if command retuns false.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "Start", InstructionCategory.Performance, "Executes code inside its block only when script is started.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "Stop", InstructionCategory.Performance, "Executes code inside its block only when script is stopped.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "KeyDown", InstructionCategory.Input, "Emulates a key being pressed.", new InstructionParameter[] { new InstructionParameter("KeyID", typeof(string)) }),
            new InstructionObject(InstructionType.Command, "KeyUp", InstructionCategory.Input, "Emulates a key being released.", new InstructionParameter[] { new InstructionParameter("KeyID", typeof(string)) }),
            new InstructionObject(InstructionType.Command, "KeyPress", InstructionCategory.Input, "Emulates a key being pressed.", new InstructionParameter[] { new InstructionParameter("KeyID", typeof(string)) }),
            new InstructionObject(InstructionType.Command, "WindowState", InstructionCategory.Operation, "Changes a window's state.", new InstructionParameter[] { new InstructionParameter("WindowHandle", typeof(IntPtr)), new InstructionParameter("WindowState", typeof(int)) }),
            new InstructionObject(InstructionType.Command, "PlayAudio", InstructionCategory.Operation, "Plays an audio file.", new InstructionParameter[] { new InstructionParameter("FileLocation", typeof(string)) }),
            new InstructionObject(InstructionType.Command, "ClearMessages", InstructionCategory.Operation, "Clears the message queue for the output.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "StopExecution", InstructionCategory.Properties, "Stops execution.", new InstructionParameter[] { }),
            //new InstructionObject(InstructionType.Command, "EnableLoop", InstructionCategory.Properties, "Enables looping.", new InstructionParameter[] { }),
            //new InstructionObject(InstructionType.Command, "DisableLoop", InstructionCategory.Properties, "Disables looping.", new InstructionParameter[] { }),
            //new InstructionObject(InstructionType.Command, "EnableMMB", InstructionCategory.Properties, "Enables stopping the loop with the abort hotkey.", new InstructionParameter[] { }),
            //new InstructionObject(InstructionType.Command, "DisableMMB", InstructionCategory.Properties, "Disables stopping the loop with the abort hotkey.", new InstructionParameter[] { }),
            //new InstructionObject(InstructionType.Command, "DisableStatus", InstructionCategory.Properties, "Disables status updates.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Command, "SetStatus", InstructionCategory.Properties, "Sets whether status updates are enabled.", new InstructionParameter[] { new InstructionParameter("Enabled", typeof(bool)) }),
            new InstructionObject(InstructionType.Command, "SetInterval", InstructionCategory.Operation, "Sets the interval for script execution.", new InstructionParameter[] { new InstructionParameter("Interval", typeof(int)) }),
            new InstructionObject(InstructionType.Command, "UIBlock", InstructionCategory.Operation, "Sets whether the script thread is blocked for UI operations such as printing to script output.", new InstructionParameter[] { new InstructionParameter("Enabled", typeof(bool)) }),
            new InstructionObject(InstructionType.Command, "AbortHotkey", InstructionCategory.Operation, "Sets the key for aborting the script execution.", new InstructionParameter[] { new InstructionParameter("KeyID", typeof(int)) }),
            new InstructionObject(InstructionType.Command, "SetLoop", InstructionCategory.Operation, "Sets looping enabled/disabled for script execution.", new InstructionParameter[] { new InstructionParameter("Enabled", typeof(bool)) }),

            new InstructionObject(InstructionType.Startup, "ThemeColor", InstructionCategory.StartupParameters, "Changes the color of the app's theme.", new InstructionParameter[] { new InstructionParameter("R", typeof(int)), new InstructionParameter("G", typeof(int)), new InstructionParameter("B", typeof(int)) }),
            //new InstructionObject(InstructionType.Startup, "CompactView", InstructionCategory.StartupParameters, "Puts the app into compact view, hiding advanced controls.", new InstructionParameter[] { new InstructionParameter("Applied", typeof(bool)) }),

            new InstructionObject(InstructionType.Function, "Math", InstructionCategory.Evaluation, "Evaluates mathematical expressions. Accepts multiple parameters.", new InstructionParameter[] { new InstructionParameter("RepeatingParameters", typeof(object)) }),
            new InstructionObject(InstructionType.Function, "Clamp", InstructionCategory.Variable, "Clamps a number between min and max.", new InstructionParameter[] { new InstructionParameter("Value", typeof(double)), new InstructionParameter("Min", typeof(double)), new InstructionParameter("Max", typeof(double)) }),
            new InstructionObject(InstructionType.Function, "Var", InstructionCategory.Variable, "Returns the value of a variable.", new InstructionParameter[] { new InstructionParameter("Name", typeof(string)) }),
            new InstructionObject(InstructionType.Function, "Chr", InstructionCategory.Manupilation, "Converts unicode character ID to a unicode character.", new InstructionParameter[] { new InstructionParameter("ID", typeof(int)) }),
            new InstructionObject(InstructionType.Function, "Equals", InstructionCategory.Evaluation, "Returns true or false depending on if parameter1 is equal to parameter2 or not.", new InstructionParameter[] { new InstructionParameter("Parameter1", typeof(object)), new InstructionParameter("Parameter2", typeof(object)) }),
            new InstructionObject(InstructionType.Function, "ToString", InstructionCategory.Conversion, "Converts the value of the parameter to string.", new InstructionParameter[] { new InstructionParameter("Parameter", typeof(object)) }),
            new InstructionObject(InstructionType.Function, "ToBool", InstructionCategory.Conversion, "Converts the value of the parameter to boolean.", new InstructionParameter[] { new InstructionParameter("Parameter", typeof(object)) }),
            new InstructionObject(InstructionType.Function, "ToInt", InstructionCategory.Conversion, "Converts the value of the parameter to integer.", new InstructionParameter[] { new InstructionParameter("Parameter", typeof(object)) }),
            new InstructionObject(InstructionType.Function, "ToDec", InstructionCategory.Conversion, "Converts the value of the parameter to decimal.", new InstructionParameter[] { new InstructionParameter("Parameter", typeof(object)) }),
            new InstructionObject(InstructionType.Function, "ToLong", InstructionCategory.Conversion, "Converts the value of the parameter to long.", new InstructionParameter[] { new InstructionParameter("Parameter", typeof(object)) }),
            new InstructionObject(InstructionType.Function, "Color", InstructionCategory.Evaluation, "Returns a Color object of RGB (0-255).", new InstructionParameter[] { new InstructionParameter("R", typeof(byte)), new InstructionParameter("G", typeof(byte)), new InstructionParameter("B", typeof(byte)) }),
            new InstructionObject(InstructionType.Function, "Pixel", InstructionCategory.Operation, $"Returns the color of the pixel in specified coordinates in screen. Must use {Interpreter.Syntax.CommandBegin}GetScreen to get screen device first{Interpreter.Syntax.CommandEnd}.", new InstructionParameter[] { new InstructionParameter("X", typeof(int)), new InstructionParameter("Y", typeof(int)) }),
            new InstructionObject(InstructionType.Function, "Cursor", InstructionCategory.Operation, "Returns the cursor's location in the specified axis.", new InstructionParameter[] { new InstructionParameter("X|Y", typeof(char)) }),
            new InstructionObject(InstructionType.Function, "Conc", InstructionCategory.Evaluation, "Concatenates the values of all parameters into a string.", new InstructionParameter[] { new InstructionParameter("RepeatingParameters", typeof(object)) }),
            new InstructionObject(InstructionType.Function, "Time", InstructionCategory.Evaluation, "Returns current time ticks.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Function, "AudioPeak", InstructionCategory.Manupilation, "Returns the default audio end point's peak 0-100.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Function, "ScreenSize", InstructionCategory.Manupilation, "Returns the size of the primary screen.", new InstructionParameter[] { new InstructionParameter("W|H", typeof(char)) }),
            new InstructionObject(InstructionType.Function, "Hotkey", InstructionCategory.Evaluation, "Returns true or false depending on if the key is pressed or not.", new InstructionParameter[] { new InstructionParameter("KeyID", typeof(int)) }),
            //new InstructionObject(InstructionType.Function, "Hotkey", InstructionCategory.Evaluation, "Returns true or false depending on if the key is pressed or not.", new InstructionParameter[] { new InstructionParameter("VKeyName", typeof(string)) }),
            new InstructionObject(InstructionType.Function, "GetMouseButton", InstructionCategory.Evaluation, "Returns true or false depending on if the mouse button is pressed or not.", new InstructionParameter[] { new InstructionParameter("ButtonID", typeof(int)) }),
            //new InstructionObject(InstructionType.Function, "AsyncHotkey", InstructionCategory.Evaluation, "Returns true or false depending on if the key was pressed or not since last call.", new InstructionParameter[] { new InstructionParameter("KeyID", typeof(int)) }),
            new InstructionObject(InstructionType.Function, "And", InstructionCategory.Evaluation, "Returns true if both parameters are true.", new InstructionParameter[] { new InstructionParameter("Parameter1", typeof(bool)), new InstructionParameter("Parameter2", typeof(bool)) }),
            new InstructionObject(InstructionType.Function, "Or", InstructionCategory.Evaluation, "Returns true if either of the parameters are true.", new InstructionParameter[] { new InstructionParameter("Parameter1", typeof(bool)), new InstructionParameter("Parameter2", typeof(bool)) }),
            new InstructionObject(InstructionType.Function, "ActiveWindow", InstructionCategory.Performance, "Returns the handle of the active foreground window.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Function, "AppPath", InstructionCategory.Properties, "Returns the working path for the executable of the app.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Function, "Desktop", InstructionCategory.Properties, "Returns the user desktop path.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Function, "Script", InstructionCategory.Properties, "Returns the loaded script file's path.", new InstructionParameter[] { }),
            new InstructionObject(InstructionType.Function, "DeltaTime", InstructionCategory.Performance, "Returns the time ticks between the last and current call to this function.", new InstructionParameter[] { })
        };

        public static InstructionObject GetByName(string name)
        {
            return Instructions.FirstOrDefault(i => i.Name.ToLower() == name.ToLower());
        }
    }

    public class InstructionParameter
    {
        public string parameterName;
        public Type parameterType;

        public InstructionParameter(string _parameterName, Type _parameterType)
        {
            parameterName = _parameterName;
            parameterType = _parameterType;
        }
    }

    public class InstructionObject
    {
        public InstructionType InstructionType;
        public string Name;
        public InstructionParameter[] Parameters;
        public InstructionCategory InstructionCategory;
        public string Description;

        public InstructionObject(InstructionType _commandType, string _Name, InstructionCategory _commandCategory, string _Description, InstructionParameter[] _parameters)
        {
            InstructionType = _commandType;
            Name = _Name;
            Parameters = _parameters;
            InstructionCategory = _commandCategory;
            Description = _Description;
        }

        public string GetString(bool getUsage = true, bool getParamTypes = true)
        {
            string result = Description;
            if (getUsage)
            {
                result += $"{Environment.NewLine}{Environment.NewLine}Usage:{Environment.NewLine}  ";
                result += GetUsage(true, true, getParamTypes);
            }
            return result;
        }

        public string GetUsage(bool getParamSeperators, bool getParamNames, bool getParamTypes, string[] paramOverride = null)
        {
            string result = "";
            if (InstructionType == InstructionType.Command)
            {
                result = $"{Interpreter.Syntax.CommandBegin}{Name}{Interpreter.Syntax.ParameterFieldBegin}";
            }
            else if (InstructionType == InstructionType.Function)
            {
                result = $"{Interpreter.Syntax.FunctionBegin}{Name}{Interpreter.Syntax.ParameterFieldBegin}";
            }

            if (getParamSeperators)
            {
                for (int i = 0; i < Parameters.Length; i++)
                {
                    if (getParamNames)
                    {
                        result += Parameters[i].parameterName;
                    }
                    else
                    {
                        if (paramOverride != null)
                        {
                            result += paramOverride[i];
                        }
                    }
                    if (getParamTypes)
                    {
                        result += $":{Parameters[i].parameterType.Name}:";
                    }
                    if (i < Parameters.Length - 1)
                    {
                        result += $"{Interpreter.Syntax.ParameterSeperator} ";
                    }
                }
            }

            result += Interpreter.Syntax.ParameterFieldEnd;
            if (InstructionType == InstructionType.Command)
            {
                result += Interpreter.Syntax.CommandEnd;
            }
            else if (InstructionType == InstructionType.Function)
            {
                result += Interpreter.Syntax.FunctionEnd;
            }
            return result;
        }

        public string GetFilled(params string[] parameters)
        {
            return GetUsage(true, false, false, parameters);
        }
    }

    public enum InstructionType
    {
        Command,
        Function,
        Startup
    }

    public enum InstructionCategory
    {
        Input,
        Performance,
        Debug,
        Variable,
        Manupilation,
        Evaluation,
        Operation,
        Conversion,
        StartupParameters,
        Properties
    }
}
