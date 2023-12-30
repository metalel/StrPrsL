using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using StrPrsL.Scripting;

namespace StrPrsL.Utility
{
    public static class Extensions
    {
        public static T HandledCast<T>(this object input, Interpreter.Intermediate intermediate)
        {
            try
            {
                if (typeof(T) == Helpers.intType)
                {
                    input = int.Parse((string)input);
                }
                else if (typeof(T) == Helpers.longType)
                {
                    input = long.Parse((string)input);
                }
                else if (typeof(T) == Helpers.boolType)
                {
                    if (input.GetType() == Helpers.stringType)
                    {
                        input = bool.Parse((string)input);
                    }
                    else
                    {
                        if (input.GetType() != Helpers.boolType)
                        {
                            input = input != null;
                        }
                    }
                }
                return (T)input;
            }
            catch (Exception)
            {
                Exceptions.Throw(new Exceptions.TypeException($"{input} could not be casted to {typeof(T)}!", $"Variable type casting failed for instruction \"{intermediate.InstructionName}\".", intermediate.Raw, intermediate.Line, intermediate.LineOffset));
                return default(T);
            }
        }
    }

    public static class Helpers
    {
        public static Type intType = typeof(int);
        public static Type longType = typeof(long);
        public static Type boolType = typeof(bool);
        public static Type stringType = typeof(string);

        public static Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null)
            {
                return null;
            }
            if (element.GetType() == type)
            {
                return element;
            }
            Visual foundElement = null;
            if (element is FrameworkElement)
            {
                (element as FrameworkElement).ApplyTemplate();
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                Visual visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = GetDescendantByType(visual, type);
                if (foundElement != null)
                {
                    break;
                }
            }
            return foundElement;
        }
    }
}