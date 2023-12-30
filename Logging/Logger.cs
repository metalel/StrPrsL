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

        private List<LogOnClickItem> onClickActions;
        private int indexTracker;
        private bool autoScroll;
        private ScrollViewer outputScrollViewer;
        private ScrollBar outputScrollBar;

        private string aggregatedMessageBuffer;

        public Logger(MainWindow owner)
        {
            this.owner = owner;
            onClickActions = new List<LogOnClickItem>();
            indexTracker = 0;
            outputScrollViewer = GetScrollViewer(owner.scriptOutput);
            outputScrollBar = GetScrollBar(outputScrollViewer);
        }

        public void Log(string message, Action onClick = null)
        {
            autoScroll = outputScrollBar.Value >= outputScrollBar.Maximum * 0.95;

            if (onClick != null)
            {
                onClickActions.Add(new LogOnClickItem(onClick, indexTracker));
            }
            owner.scriptOutput.Items.Add(message);
            indexTracker++;

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
            onClickActions.Clear();
            owner.scriptOutput.Items.Clear();
            indexTracker = 0;
        }

        private ScrollViewer GetScrollViewer(Control control)
        {
            return Helpers.GetDescendantByType(control, typeof(ScrollViewer)) as ScrollViewer;
        }

        private ScrollBar GetScrollBar(Control control)
        {
            return Helpers.GetDescendantByType(control, typeof(ScrollBar)) as ScrollBar;
        }

        public Action GetOnClick(int index)
        {
            return onClickActions.First(i => i.Index == index).Action;
        }

        private class LogOnClickItem
        {
            public Action Action;
            public int Index;

            public LogOnClickItem(Action action, int index)
            {
                Action = action;
                Index = index;
            }
        }
    }
}
