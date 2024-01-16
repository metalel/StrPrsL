using System;
using System.Collections.Concurrent;
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
        private static string stringBuffer;
        public static T HandledCast<T>(this object input, Interpreter.Intermediate intermediate)
        {
            try
            {
                if (typeof(T) == Helpers.intType)
                {
                    input = (int)Math.Round(decimal.Parse(input.ToString()));
                }
                if (typeof(T) == Helpers.decType)
                {
                    input = decimal.Parse(input.ToString());
                }
                else if (typeof(T) == Helpers.longType)
                {
                    input = long.Parse(input.ToString());
                }
                else if (typeof(T) == Helpers.byteType)
                {
                    input = byte.Parse(input.ToString());
                }
                else if (typeof(T) == Helpers.charType)
                {
                    input = char.Parse(input.ToString());
                }
                else if (typeof(T) == Helpers.boolType)
                {
                    if (input.GetType() == Helpers.stringType)
                    {
                        stringBuffer = ((string)input).ToLower();
                        if (stringBuffer == "enabled")
                        {
                            input = true;
                        }
                        else if (stringBuffer == "disabled")
                        {
                            input = false;
                        }
                        else
                        {
                            input = bool.Parse(input.ToString());
                        }
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
                Exceptions.Throw(new Exceptions.TypeException($"\"{input}\" could not be casted to {typeof(T)}!", $"Variable type casting failed for instruction \"{intermediate.InstructionName}\".", intermediate.Raw, intermediate.Line, intermediate.LineOffset));
                return default(T);
            }
        }

        public static System.Windows.Media.Color Invert(this System.Windows.Media.Color color)
        {
            color.R = (byte)(255 - color.R);
            color.G = (byte)(255 - color.G);
            color.B = (byte)(255 - color.B);
            return color;
        }

        public static int WrapAround(this int v, int delta, int minval, int maxval)
        {
            int mod = maxval + 1 - minval;
            v += delta - minval;
            v += (1 - v / mod) * mod;
            return v % mod + minval;
        }

        public static double WrapAround(this double v, double delta, double minval, double maxval)
        {
            v += delta;
            double d = maxval - minval;

            while (v < minval)
            {
                v += d;
            }
            while (v > maxval)
            {
                v -= d;
            }

            return v;
        }

        public static System.Windows.Media.Color ModifyWrap(this System.Windows.Media.Color color, int modify, bool clamp = false)
        {
            if (clamp)
            {
                return System.Windows.Media.Color.FromArgb
                (
                    255,
                    (byte)(Math.Clamp((int)color.R + modify, 0, 255)),
                    (byte)(Math.Clamp((int)color.G + modify, 0, 255)),
                    (byte)(Math.Clamp((int)color.B + modify, 0, 255))
                );
            }
            return System.Windows.Media.Color.FromArgb
            (
                255,
                (byte)((int)color.R).WrapAround(modify, 0, 255),
                (byte)((int)color.G).WrapAround(modify, 0, 255),
                (byte)((int)color.B).WrapAround(modify, 0, 255)
            );
        }

        public static System.Windows.Media.Color ModifyWrap(this System.Windows.Media.Color color, int modifyR, int modifyG, int modifyB, bool clamp = false)
        {
            if (clamp)
            {
                return System.Windows.Media.Color.FromArgb
                (
                    255,
                    (byte)(Math.Clamp((int)color.R + modifyR, 0, 255)),
                    (byte)(Math.Clamp((int)color.G + modifyG, 0, 255)),
                    (byte)(Math.Clamp((int)color.B + modifyB, 0, 255))
                );
            }
            return System.Windows.Media.Color.FromArgb
            (
                255,
                (byte)((int)color.R).WrapAround(modifyR, 0, 255),
                (byte)((int)color.G).WrapAround(modifyG, 0, 255),
                (byte)((int)color.B).WrapAround(modifyB, 0, 255)
            );
        }

        public static System.Windows.Media.Color ToMediaColor(this System.Drawing.Color color)
        {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static System.Drawing.Color ToFormsColor(this System.Windows.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static System.Windows.Media.Color Subtract(this System.Windows.Media.Color a, System.Windows.Media.Color b)
        {
            return System.Windows.Media.Color.FromArgb(
                a.A,
                (byte)((int)a.R).WrapAround(-b.R, 0, 255),
                (byte)((int)a.G).WrapAround(-b.G, 0, 255),
                (byte)((int)a.B).WrapAround(-b.B, 0, 255)
                );
        }

        public static System.Windows.Media.Color Add(this System.Windows.Media.Color a, System.Windows.Media.Color b)
        {
            return System.Windows.Media.Color.FromArgb(
                a.A,
                (byte)((int)a.R).WrapAround(b.R, 0, 255),
                (byte)((int)a.G).WrapAround(b.G, 0, 255),
                (byte)((int)a.B).WrapAround(b.B, 0, 255)
                );
        }

        public static System.Windows.Media.Color Alpha(this System.Windows.Media.Color color, byte alpha)
        {
            color.A = alpha;
            return color;
        }

        public static void Clear<T>(this BlockingCollection<T> blockingCollection)
        {
            if (blockingCollection == null)
            {
                throw new ArgumentNullException("blockingCollection");
            }

            while (blockingCollection.Count > 0)
            {
                T item;
                blockingCollection.TryTake(out item);
            }
        }
    }

    public static class Helpers
    {
        public static Type intType = typeof(int);
        public static Type decType = typeof(decimal);
        public static Type longType = typeof(long);
        public static Type boolType = typeof(bool);
        public static Type stringType = typeof(string);
        public static Type charType = typeof(char);
        public static Type byteType = typeof(byte);

        public static string WriteStringTag(string Tag, string Value, string Identifier = "<,>,</")
        {
            string[] Identifiers = Identifier.Split(',');
            string Result = null;
            if (Identifiers.Count() == 3)
            {
                Result = Identifiers[0] + Tag + Identifiers[1] + Value + Identifiers[2] + Tag + Identifiers[1];
            }
            return Result;
        }

        public static string RemoveStringTag(string Tag, string Source, string Identifier = "<,>,</")
        {
            string[] Identifiers = Identifier.Split(',');
            string Result = null;
            if (Identifiers.Count() == 3)
            {
                int StarterIndex = Source.LastIndexOf(Identifiers[0] + Tag + Identifiers[1]);
                if (StarterIndex != -1)
                {
                    int EnderIndex = Source.IndexOf(Identifiers[2] + Tag + Identifiers[1]);
                    if (EnderIndex != -1)
                    {

                        Source = Source.Remove(StarterIndex, (EnderIndex + Identifiers[0].Length + Identifiers[2].Length + Tag.Length) - StarterIndex);
                        Result = Source;
                    }
                }
            }
            return Result;
        }

        public static string GetStringTag(string Tag, string Source, string Identifier = "<,>,</")
        {
            string[] Identifiers = Identifier.Split(',');
            string Result = null;
            if (Identifiers.Count() == 3)
            {
                int StarterIndex = Source.LastIndexOf(Identifiers[0] + Tag + Identifiers[1]);
                if (StarterIndex != -1)
                {
                    Source = Source.Remove(0, StarterIndex + Identifiers[0].Length + Identifiers[1].Length + Tag.Length);
                    int EnderIndex = Source.IndexOf(Identifiers[2] + Tag + Identifiers[1]);
                    if (EnderIndex != -1)
                    {
                        Source = Source.Remove(EnderIndex);
                        Result = Source;
                    }
                }
            }
            return Result;
        }

        public static void ColorToHSV(System.Drawing.Color color, out double hue, out double saturation, out double value)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            hue = color.GetHue();
            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
        }

        public static System.Drawing.Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return System.Drawing.Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return System.Drawing.Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return System.Drawing.Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return System.Drawing.Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return System.Drawing.Color.FromArgb(255, t, p, v);
            else
                return System.Drawing.Color.FromArgb(255, v, p, q);
        }

        public static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

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

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield return (T)Enumerable.Empty<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                if (ithChild == null) continue;
                if (ithChild is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(ithChild)) yield return childOfChild;
            }
        }

        public class VerboseColor
        {
            public byte A
            {
                get;
                private set;
            }
            public byte R
            {
                get;
                private set;
            }
            public byte G
            {
                get;
                private set;
            }
            public byte B
            {
                get;
                private set;
            }

            private double H;
            private double S;
            private double V;

            public Color MediaColor
            {
                get;
                private set;
            }
            public System.Drawing.Color DrawingColor
            {
                get;
                private set;
            }

            public VerboseColor(Color mediaColor)
            {
                A = mediaColor.A;
                R = mediaColor.R;
                G = mediaColor.G;
                B = mediaColor.B;

                MediaColor = mediaColor;
                DrawingColor = mediaColor.ToFormsColor();
            }

            public VerboseColor(System.Drawing.Color drawingColor)
            {
                A = drawingColor.A;
                R = drawingColor.R;
                G = drawingColor.G;
                B = drawingColor.B;

                DrawingColor = drawingColor;
                MediaColor = drawingColor.ToMediaColor();
            }

            public HSV_Color HSVColor
            {
                get
                {
                    Helpers.ColorToHSV(System.Drawing.Color.FromArgb(A, R, G, B), out H, out S, out V);
                    return new HSV_Color(H, S, V);
                }
            }
            
            public class HSV_Color
            {
                public double H;
                public double S;
                public double V;

                public HSV_Color(double h, double s, double v)
                {
                    H = h;
                    S = s;
                    V = v;
                }

                public Color PreserveAdd(double h, double s, double v)
                {
                    H += h;
                    S = S.WrapAround(s, 0.0d, 1.0d);
                    V += v;

                    if (H > 360.0d)
                    {
                        V = V.WrapAround(H - 360.0d, 0.0d, 1.0d);
                        H = 360.0d;
                    }
                    else if (H < 0.0d)
                    {
                        S = S.WrapAround(-H, 0.0d, 1.0d);
                        H = 0.0d;
                    }

                    if (V > 1.0d)
                    {
                        S = S.WrapAround(V - 1.0d, 0.0, 1.0d);
                        V = V.WrapAround(-(V - 1.0d), 0.0d, 1.0d);
                    }
                    else if (V < 0.0d)
                    {
                        S = S.WrapAround(-(V - 1.0d), 0.0, 1.0d);
                        V = V.WrapAround(V, 0.0d, 1.0d);
                    }

                    return Helpers.ColorFromHSV(H, S, V).ToMediaColor();
                }

                public void Out(out double H, out double S, out double V)
                {
                    H = this.H;
                    S = this.S;
                    V = this.V;
                }
            }
        }
    }
}