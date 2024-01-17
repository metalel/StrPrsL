using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using StrPrsL.Scripting.Completion;
using StrPrsL.Utility;

namespace StrPrsL.Scripting
{
    /// <summary>
    /// Interaction logic for InsertCodeWindow.xaml
    /// </summary>
    public partial class InsertCodeWindow : Window
    {
        private MainWindow owner;
        private bool closing = false;
        private InsertMode insertMode;
        private System.Windows.Media.Color currentPixel;
        private SolidColorBrush pixelShowcaseBrush = new SolidColorBrush();
        private int currentCursorPosX;
        private int currentCursorPosY;
        private bool captured = false;
        private bool ignoreKeys = true;
        private Key[] openingShortcuts;
        private List<Key> waitForKeysUp = new List<Key>();

        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal);

        public InsertCodeWindow(MainWindow owner, InsertMode insertMode, Key[] shortcuts)
        {
            InitializeComponent();
            this.owner = owner;
            this.insertMode = insertMode;
            openingShortcuts = shortcuts;
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!closing)
            {
                this.Close();
            }
        }

        private void KeyPicker_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ignoreKeys || captured)
            {
                return;
            }

            if (insertMode == InsertMode.KeyID)
            {
                owner.InsertCode(((int)e.Key).ToString());
                e.Handled = true;
                captured = true;
                if (!closing)
                {
                    this.Close();
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Left = (owner.Left + (owner.Width / 2)) - (this.Width / 2);
            this.Top = (owner.Top + (owner.Height / 2)) - (this.Height / 2);
            if (insertMode == InsertMode.KeyID)
            {
                Prompt.Text = "Insert Key ID";
                KeyPicker.Text = "Press a key...";
                KeyPicker.Focus();

                for (int i = 0; i < openingShortcuts.Length; i++)
                {
                    if (Functions.GetKey(openingShortcuts[i]))
                    {
                        waitForKeysUp.Add(openingShortcuts[i]);
                    }
                }

                captured = false;
            }
            else if (insertMode == InsertMode.AutoPixel)
            {
                KeyPicker.Text = $"Press {MainWindow.Instance.StopLoopKey} to insert pixel expression.";
                Functions.GetDCCommand();
                Prompt.Text = "Insert Automatic Pixel Expression";
                Prompt.Background = pixelShowcaseBrush;
            }

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (insertMode == InsertMode.KeyID)
            {
                if (waitForKeysUp.Count > 0)
                {
                    for (int i = waitForKeysUp.Count - 1; i >= 0 ; i--)
                    {
                        if (!Functions.GetKey(waitForKeysUp[i]))
                        {
                            waitForKeysUp.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    ignoreKeys = false;
                }
            }
            else if (insertMode == InsertMode.AutoPixel)
            {
                if (captured)
                {
                    return;
                }

                currentCursorPosX = System.Windows.Forms.Cursor.Position.X;
                currentCursorPosY = System.Windows.Forms.Cursor.Position.Y;

                currentPixel = Functions.GetPixelColor(currentCursorPosX, currentCursorPosY);

                pixelShowcaseBrush.Color = currentPixel;

                Prompt.Text = $"{ColorStringify(currentPixel)}{Environment.NewLine}{currentCursorPosX}, {currentCursorPosY}";

                if (Functions.GetKey(MainWindow.Instance.StopLoopKey))
                {
                    captured = true;
                    if (!closing)
                    {
                        string result =
                        InstructionRegister.GetByName("equals").GetFilled
                        (
                            InstructionRegister.GetByName("pixel").GetFilled(currentCursorPosX.ToString(), currentCursorPosY.ToString()),
                            InstructionRegister.GetByName("color").GetFilled(currentPixel.R.ToString(), currentPixel.G.ToString(), currentPixel.B.ToString())
                        );

                        owner.InsertCode
                        (
                            result
                        );
                        this.Close();
                    }
                }
            }
        }

        private string ColorStringify(System.Windows.Media.Color color)
        {
            return $"R: {color.R}, G: {color.G}, B: {color.B}";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
        }

        public enum InsertMode
        {
            KeyID,
            AutoPixel
        }
    }
}
