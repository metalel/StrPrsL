using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CoreAudioApi;
using System.Diagnostics;
using System.Threading;
using System.Media;
using System.Windows.Input;
using StrPrsL.Utility;
using WindowsInput;

namespace StrPrsL.Scripting
{
    public static class Functions
    {
        [DllImport("user32.dll")] private static extern IntPtr GetMessageExtraInfo();
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("user32.dll")] static extern int GetSystemMetrics(SystemMetric smIndex);
        [DllImport("gdi32.dll")] static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(Int32 vKeys);
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)] private static extern short GetKeyState(int keyCode);
        [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();

        private static DataTable mathDataTable = new DataTable();

        //Shared Buffers
        private static int intBuffer;
        private static bool boolBuffer;
        private static string stringBuffer;
        private static InputSimulator InputSimulator = new InputSimulator();
        private static int forLoopIndexBuffer;
        private static Interpreter.Variable variableSearchBuffer;
        private static MainWindow.Message messageBuffer;
        private static System.Windows.Media.Color mediaColorBuffer;

        //Sleep-Wait Variables
        public static WaitingManager WaitingManager = new WaitingManager();

        //Pixel Variables
        private static IntPtr hdc;
        private static uint pixel;

        //Audio Player Variables
        private static SoundPlayer SoundPlayer;
        private static bool SoundPlayerCreated = false;
        private static MMDevice currentDevice = null;

        //Delta Variabled
        private static long lastTickTime;
        private static long lastDelta;

        public static bool GetActionTrigger()
        {
            //return (GetAsyncKeyState(0x04) & 0x01) == 1;
            return ((GetKeyState(0x04) & 0x8000) == 0x8000);
        }

        public static string GetUniqueID()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static void ScriptTicked()
        {
            lastDelta = DateTime.Now.Ticks - lastTickTime;
            lastTickTime = DateTime.Now.Ticks;
        }

        #region Void-Types
        public static void Sleep(int length)
        {
            Thread.Sleep(length);
        }

        public static void MoveMouse(int x, int y)
        {
            InputSimulator.Mouse.MoveMouseBy(x, y);
        }

        public static void SetMouse(int x, int y)
        {
            InputSimulator.Mouse.MoveMouseTo(x * 65536 / Screen.PrimaryScreen.Bounds.Width, y * 65536 / Screen.PrimaryScreen.Bounds.Height);
        }

        public static void LeftClick()
        {
            InputSimulator.Mouse.LeftButtonClick();
        }

        public static void RightClick()
        {
            InputSimulator.Mouse.RightButtonClick();
        }

        public static void MiddleClick()
        {
            InputSimulator.Mouse.MiddleButtonClick();
        }

        public static void LeftDown()
        {
            InputSimulator.Mouse.LeftButtonDown();
        }

        public static void LeftUp()
        {
            InputSimulator.Mouse.LeftButtonUp();
        }

        public static void RightDown()
        {
            InputSimulator.Mouse.RightButtonDown();
        }

        public static void RightUp()
        {
            InputSimulator.Mouse.RightButtonUp();
        }

        public static void MiddleDown()
        {
            InputSimulator.Mouse.MiddleButtonDown();
        }

        public static void MiddleUp()
        {
            InputSimulator.Mouse.MiddleButtonUp();
        }

        public static void ScrollUp(int scroll = 150)
        {
            InputSimulator.Mouse.VerticalScroll(scroll);
        }

        public static void ScrollDown(int scroll = 150)
        {
            InputSimulator.Mouse.VerticalScroll(-scroll);
        }

        public static void KeyDown(int keyID)
        {
            InputSimulator.Keyboard.KeyDown((WindowsInput.Native.VirtualKeyCode)KeyInterop.VirtualKeyFromKey((Key)keyID));
        }

        public static void KeyUp(int keyID)
        {
            InputSimulator.Keyboard.KeyUp((WindowsInput.Native.VirtualKeyCode)KeyInterop.VirtualKeyFromKey((Key)keyID));
        }

        public static void KeyPress(int keyID)
        {
            InputSimulator.Keyboard.KeyPress((WindowsInput.Native.VirtualKeyCode)KeyInterop.VirtualKeyFromKey((Key)keyID));
        }

        public static void Print(string message, Interpreter.Intermediate sender)
        {
            messageBuffer = new MainWindow.Message(message, MainWindow.Message.MessageType.Output, null);
            if (sender != null)
            {
                messageBuffer.OnClick = () => MainWindow.Instance.FocusScript(sender.Line, sender.LineOffset);
            }

            MainWindow.Instance.EnqueueMessage(messageBuffer);
        }

        public static void Status(string status, Interpreter.Intermediate sender)
        {
            MainWindow.Instance.EnqueueMessage(new MainWindow.Message(status, MainWindow.Message.MessageType.Status, null));
        }

        public static void PlayAudio(string fileLocation)
        {
            if (!SoundPlayerCreated)
            {
                SoundPlayer = new SoundPlayer();
                SoundPlayerCreated = true;
            }
            SoundPlayer.SoundLocation = fileLocation;
            SoundPlayer.Play();
        }

        public static void GetDCCommand()
        {
            hdc = GetDC(IntPtr.Zero);
        }

        public static void ReleaseDCCommand()
        {
            ReleaseDC(IntPtr.Zero, hdc);
        }

        public static void SendKeysCommand(string keys)
        {
            SendKeys.Send(keys);
        }

        public static void ChangeWindowState(IntPtr handle, int state)
        {
            ShowWindow(handle, state);
        }

        public static void CreateVariable(string identifier, object value, Interpreter.Intermediate caller)
        {
            if (MainWindow.Instance.VariableExists(identifier))
            {
                Exceptions.Throw(new Exceptions.RuntimeException("Variable already exists!", $"Variable \"{identifier}\" already exists.", caller.Raw, caller.Line, caller.LineOffset));
            }
            else
            {
                MainWindow.Instance.CreateVariable(new Interpreter.Variable(identifier, value));
            }
        }
        #endregion

        #region Return-Types
        public static long GetTickDelta()
        {
            ScriptTicked();
            return lastDelta;
        }

        public static Interpreter.Variable GetVariable(string identifier, Interpreter.Intermediate caller)
        {
            variableSearchBuffer = MainWindow.Instance.GetVariable(identifier);
            if (variableSearchBuffer == null)
            {
                Exceptions.Throw(new Exceptions.RuntimeException("Variable not found!", $"Variable \"{identifier}\" could not be found.", caller.Raw, caller.Line, caller.LineOffset));
            }
            return variableSearchBuffer;
        }

        public static object Math(string expression, Interpreter.Intermediate caller)
        {
            try
            {
                return mathDataTable.Compute(expression, "");
            }
            catch (Exception e)
            {
                Exceptions.Throw(new Exceptions.RuntimeException(e.Message, $"\"{expression}\" could not be computed.", e.Source, caller.Line, caller.LineOffset));
            }
            return -1;
        }

        public static IntPtr GetCurrentWindow()
        {
            return GetForegroundWindow();
        }

        public static long CurrentTimeTicks()
        {
            return DateTime.Now.Ticks;
        }

        public static int AudioPeak()
        {
            if (currentDevice == null)
            {
                MMDeviceEnumerator audioEnum = new MMDeviceEnumerator();
                currentDevice = audioEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            }
            return (int)System.Math.Round(currentDevice.AudioMeterInformation.MasterPeakValue * 100f);
        }

        public static int ClampInt(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static int Add(int arg1, int arg2)
        {
            return arg1 + arg2;
        }

        public static int Sub(int arg1, int arg2)
        {
            return arg1 - arg2;
        }

        public static char GetChar(int id)
        {
            return Convert.ToChar(id);
        }

        public static bool IsEqual(object value1, object value2)
        {
            if (value1.GetType() == value2.GetType())
            {
                return value1.Equals(value2);
            }
            else
            {
                return value1.ToString() == value2.ToString();
            }
        }

        public static bool And(bool value1, bool value2)
        {
            return value1 & value2;
        }

        public static string Stringify(object value)
        {
            mediaColorBuffer = (System.Windows.Media.Color)value;
            if (value.GetType() == Helpers.mediaColorType)
            {
                return $"(A: {mediaColorBuffer.A}, R: {mediaColorBuffer.R}, G: {mediaColorBuffer.G}, B: {mediaColorBuffer.B})";
            }
            return value.ToString();
        }

        public static bool Boolify(object value)
        {
            boolBuffer = false;
            bool.TryParse(value.ToString(), out boolBuffer);
            return boolBuffer;
        }

        public static int Intify(object value)
        {
            intBuffer = int.MinValue;
            int.TryParse(value.ToString(), out intBuffer);
            return intBuffer;
        }

        public static System.Windows.Media.Color GetColor(int R, int G, int B)
        {
            return System.Windows.Media.Color.FromArgb(255, (byte)R, (byte)G, (byte)B);
        }

        public static System.Windows.Media.Color GetPixelColor(int x, int y)
        {
            pixel = GetPixel(hdc, x, y);
            return System.Windows.Media.Color.FromArgb(
                255,
                (byte)((int)(pixel & 0x000000FF)),
                (byte)((int)(pixel & 0x0000FF00) >> 8),
                (byte)((int)(pixel & 0x00FF0000) >> 16));
        }

        public static int GetMousePosition(char axis)
        {
            if (char.ToLower(axis) == 'x')
            {
                return System.Windows.Forms.Cursor.Position.X;
            }
            else if (char.ToLower(axis) == 'y')
            {
                return System.Windows.Forms.Cursor.Position.Y;
            }
            else
            {
                return -1;
            }
        }

        public static int GetScreenSize(char dimension)
        {
            if (char.ToLower(dimension) == 'w')
            {
                return Screen.PrimaryScreen.Bounds.Width;
            }
            else if (char.ToLower(dimension) == 'h')
            {
                return Screen.PrimaryScreen.Bounds.Height;
            }
            else
            {
                return -1;
            }
        }

        public static bool GetAsyncHotkey(int keyID)
        {
            if (GetAsyncKeyState(keyID) == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool GetKey(Key key)
        {
            return Keyboard.IsKeyDown(key);
        }

        public static bool GetMouseButton(int buttonID)
        {
            /*
            0 Left
            1 Right
            2 Midle
            3 XButton1
            4 XButton2
            */
            if (buttonID == 0)
            {
                return (Control.MouseButtons & MouseButtons.Left) != 0;
            }
            else if (buttonID == 1)
            {
                return (Control.MouseButtons & MouseButtons.Right) != 0;
            }
            else if (buttonID == 2)
            {
                return (Control.MouseButtons & MouseButtons.Middle) != 0;
            }
            else if (buttonID == 3)
            {
                return (Control.MouseButtons & MouseButtons.XButton1) != 0;
            }
            else if (buttonID == 4)
            {
                return (Control.MouseButtons & MouseButtons.XButton2) != 0;
            }
            return false;
        }

        public static string GetExecutablePath()
        {
            return Application.StartupPath;
        }

        public static string GetDesktopPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        public static string GetScriptPath()
        {
            return System.IO.Path.GetDirectoryName(MainWindow.Instance.LoadedFile);
        }
        #endregion

        private static void WrapAroundAddition(ref int n, int increment, int min, int max)
        {
            if (n + increment > max)
                n += increment - max - 1;
            else if (n + increment < min)
                n += max + 1 + increment;
            else
                n += increment;
        }

        #region Api_Definitions
        enum SystemMetric
        {
            SM_CXSCREEN = 0,
            SM_CYSCREEN = 1,
        }
        #endregion

        static int CalculateAbsoluteCoordinateX(int x)
        {
            return (x * 65536) / (GetSystemMetrics(SystemMetric.SM_CXSCREEN) - 1);
        }

        static int CalculateAbsoluteCoordinateY(int y)
        {
            return (y * 65536) / (GetSystemMetrics(SystemMetric.SM_CYSCREEN) - 1);
        }

        public static string WriteStringValue(string StringValue, string Contents, string Identifier = "<,>,</")
        {
            string[] Identifiers = Identifier.Split(',');
            string Result = null;
            if (Identifiers.Count() == 3)
            {
                Result = Identifiers[0] + StringValue + Identifiers[1] + Contents + Identifiers[2] + StringValue + Identifiers[1];
            }
            return Result;
        }

        public static string GetStringValue(string StringValue, string SourceString, string Identifier = "<,>,</")
        {
            string[] Identifiers = Identifier.Split(',');
            string Result = null;
            if (Identifiers.Count() == 3)
            {
                int StarterIndex = SourceString.LastIndexOf(Identifiers[0] + StringValue + Identifiers[1]);
                if (StarterIndex != -1)
                {
                    SourceString = SourceString.Remove(0, StarterIndex + Identifiers[0].Length + Identifiers[1].Length + StringValue.Length);
                    int EnderIndex = SourceString.IndexOf(Identifiers[2] + StringValue + Identifiers[1]);
                    if (EnderIndex != -1)
                    {
                        SourceString = SourceString.Remove(EnderIndex);
                        Result = SourceString;
                    }
                }
            }
            return Result;
        }

        public static string RemoveStringValue(string StringValue, string SourceString, string Identifier = "<,>,</")
        {
            string[] Identifiers = Identifier.Split(',');
            string Result = null;
            if (Identifiers.Count() == 3)
            {
                int StarterIndex = SourceString.LastIndexOf(Identifiers[0] + StringValue + Identifiers[1]);
                if (StarterIndex != -1)
                {
                    int EnderIndex = SourceString.IndexOf(Identifiers[2] + StringValue + Identifiers[1]);
                    if (EnderIndex != -1)
                    {

                        SourceString = SourceString.Remove(StarterIndex, (EnderIndex + Identifiers[0].Length + Identifiers[2].Length + StringValue.Length) - StarterIndex);
                        Result = SourceString;
                    }
                }
            }
            return Result;
        }
    }
}