using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using StrPrsL.Utility;

namespace StrPrsL
{
    public class Logger
    {
        private MainWindow owner;

        private bool autoScroll;
        private ScrollViewer outputScrollViewer;
        private ScrollBar outputScrollBar;

        private string aggregatedMessageBuffer;

        public Logger(MainWindow owner)
        {
            this.owner = owner;
            outputScrollViewer = GetScrollViewer(owner.scriptOutput);
            outputScrollBar = GetScrollBar(outputScrollViewer);
        }

        public void Log(string message, Action onClick = null)
        {
            autoScroll = outputScrollBar.Value >= outputScrollBar.Maximum * 0.95;

            owner.scriptOutput.Items.Add(new TestItemClass(message, onClick));

            if (autoScroll)
            {
                outputScrollViewer.ScrollToBottom();
            }

            if (owner.scriptOutput.Items.Count > MainWindow.Instance.MessageLimit)
            {
                owner.scriptOutput.Items.RemoveAt(0);
            }
        }

        public void AggregatedLog(Action onClick = null, params object[] parameters)
        {
            aggregatedMessageBuffer = "";
            for (int i = 0; i < parameters.Length; i++)
            {
                aggregatedMessageBuffer += parameters[i].ToString();
                if (i != parameters.Length - 1)
                {
                    aggregatedMessageBuffer += Environment.NewLine;
                }
            }

            Log(aggregatedMessageBuffer, onClick);
        }

        public void Clear()
        {
            owner.scriptOutput.Items.Clear();
        }

        private ScrollViewer GetScrollViewer(Control control)
        {
            return Helpers.GetDescendantByType(control, typeof(ScrollViewer)) as ScrollViewer;
        }

        private ScrollBar GetScrollBar(Control control)
        {
            return Helpers.GetDescendantByType(control, typeof(ScrollBar)) as ScrollBar;
        }

        public class TestItemClass
        {
            public string Message;
            public Action OnClick;

            public TestItemClass(string message, Action onClick)
            {
                Message = message;
                OnClick = onClick;
            }

            public override string ToString()
            {
                return Message;
            }
        }
    }
}
