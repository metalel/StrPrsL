using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit;
using StrPrsL.Scripting;
using StrPrsL.Utility;
using System.Collections.Concurrent;

namespace StrPrsL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Setup
        public static MainWindow Instance;

        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Normal);
        private Microsoft.Win32.OpenFileDialog OpenFileDialog;
        private bool? OpenFileResult;

        private int StopLoopKeyID = 93;
        private Key StopLoopKey = Key.F4;

        private WaitingManager WaitingManager = new WaitingManager();

        #region ScriptExecution
        public bool LoopEnabled;
        public int LineInterval;
        public bool BlockThreadForUI;

        public bool ScopedInThisTick = false;

        private bool regressedOnThisTick = false;

        public BlockingCollection<Message> MessageBlockedCollection = new BlockingCollection<Message>(1);
        public ConcurrentQueue<Message> MessageQueue = new ConcurrentQueue<Message>();
        private Message currentMessage;
        public ConcurrentQueue<Action> ThreadEndedActionQueue = new ConcurrentQueue<Action>();
        private Action currentAction;
        public List<Action> PostInterruptionList = new List<Action>();

        private const string scopeRegressMessage = "End of line scope regress.";
        private const string mainScopeName = "MainScope";
        public int MainScopeID
        {
            get;
            private set;
        } = mainScopeName.GetHashCode();
        private const string startScopeName = "StartScope";
        public int StartScopeID
        {
            get;
            private set;
        } = startScopeName.GetHashCode();
        private const string stopScopeName = "StopScope";
        public int StopScopeID
        {
            get;
            private set;
        } = stopScopeName.GetHashCode();

        private Thread scriptExecutionThread;
        public bool cancelExecutionThread;
        public Action ScriptExecutionThreadEndedCallback;
        public bool awaitingThreadEnd;

        private List<Interpreter.Scope> Scopes = new List<Interpreter.Scope>();
        private Stack<int> ScopeStack = new Stack<int>();
        private Interpreter.Scope CurrentScope = null;
        private Interpreter.Scope StartScope;
        private Interpreter.Scope StopScope;

        private List<Interpreter.Variable> Variables = new List<Interpreter.Variable>();

        public bool ProgressToNextLine
        {
            get;
            private set;
        } = true;

        public bool LineExecution
        {
            get;
            private set;
        } = true;
        #endregion

        public Logger Logger
        {
            get;
            private set;
        }

        public int MessageLimit
        {
            get;
            private set;
        } = 64;

        public MainWindow()
        {
            if (Instance != null)
            {
                this.Close();
            }
            else
            {
                Instance = this;
            }

            InitializeComponent();

            Logger = new Logger(this);

            OpenFileDialog = new Microsoft.Win32.OpenFileDialog();
            OpenFileDialog.FileName = "Script";
            OpenFileDialog.DefaultExt = ".str";
            OpenFileDialog.Filter = "Str scripts (.str)|*.str";

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dispatcherTimer.Start();
        }

        public class Message
        {
            public string Text;
            public Action OnClick;

            public Message(string text, Action onClick)
            {
                Text = text;
                OnClick = onClick;
            }
        }
        #endregion

        #region UI Callbacks
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StrPrsL.Scripting.SyntaxHighlighting.xshd"))
            {
                System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(stream);
                this.scriptEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
            }
            DisplayStopLoopKeyName();
            LoadScriptFile("C:\\Users\\oyunz\\Desktop\\debug.str");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (scriptExecutionThread != null && scriptExecutionThread.IsAlive)
            {
                StopScriptExecution();
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileResult = OpenFileDialog.ShowDialog();

            if (OpenFileResult.HasValue && OpenFileResult.Value == true)
            {
                LoadScriptFile(OpenFileDialog.FileName);
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleScriptExecution();
        }

        private void ClearOutputButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Clear();
        }

        private void StopLoopButton_KeyUp(object sender, KeyEventArgs e)
        {
            StopLoopKeyID = (int)e.Key;
            StopLoopKey = e.Key;
            DisplayStopLoopKeyName();
        }
        #endregion

        #region Private Methods
        private void LoadScript(string script)
        {
            this.scriptEditor.Text = script;
        }

        private void LoadScriptFile(string filename)
        {
            LoadScript(File.ReadAllText(filename));
        }

        private void ToggleScriptExecution()
        {
            if (awaitingThreadEnd)
            {
                return;
            }

            if (scriptExecutionThread != null && scriptExecutionThread.IsAlive)
            {
                StopScriptExecution();
            }
            else
            {
                StartScriptExecution();
            }
        }

        private void ScriptExecutionTick()
        {
            while (!cancelExecutionThread)
            {
                if (WaitingManager.WaitInitialized)
                {
                    if (WaitingManager.IsWaitComplete(LineInterval * TimeSpan.TicksPerMillisecond))
                    {
                        SetLineProgression(true);
                        SetLineExecution(true);
                        WaitingManager.StopWait();
                    }
                    else
                    {
                        continue;
                    }
                }

                if (CurrentScope.LineNumber >= CurrentScope.Executions.Length)
                {
                    CurrentScope.LineNumber = 0;
                    if (ScopeStack.Count > 1 && ProgressToNextLine)
                    {
                        RegressScope(scopeRegressMessage);
                        regressedOnThisTick = true;
                    }
                    else
                    {
                        if (!LoopEnabled && ProgressToNextLine)
                        {
                            break;
                        }
                    }
                }

                if (!regressedOnThisTick)
                {
                    if (LineExecution)
                    {
                        CurrentScope.CurrentLine.Instruction.Invoke();
                    }
                    if (ProgressToNextLine && !ScopedInThisTick)
                    {
                        CurrentScope.LineNumber++;
                    }
                }

                if (ProgressToNextLine)
                {
                    if (LineInterval > 0 && !regressedOnThisTick)
                    {
                        if (!WaitingManager.WaitInitialized)
                        {
                            SetLineProgression(false);
                            SetLineExecution(false);
                            WaitingManager.StartWait();
                        }
                    }
                }

                if (ScopedInThisTick)
                {
                    ScopedInThisTick = false;
                }

                if (regressedOnThisTick)
                {
                    regressedOnThisTick = false;
                }
            }
            ThreadEndedActionQueue.Enqueue(() =>
            {
                ScriptExecutionThreadEndedCallback.Invoke();
                awaitingThreadEnd = false;
            });
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (BlockThreadForUI)
            {
                if (MessageBlockedCollection.TryTake(out currentMessage))
                {
                    Logger.Log(currentMessage.Text, currentMessage.OnClick);
                }
            }
            else
            {
                if (MessageQueue.TryDequeue(out currentMessage))
                {
                    Logger.Log(currentMessage.Text, currentMessage.OnClick);
                }
            }
            if (ThreadEndedActionQueue.TryDequeue(out currentAction))
            {
                currentAction.Invoke();
            }

            if (Functions.TestKey(StopLoopKey))
            {
                if (scriptExecutionThread != null && scriptExecutionThread.IsAlive)
                {
                    StopScriptExecution();
                }
            }
        }

        private void DisplayStopLoopKeyName()
        {
            StopLoopButton.Text = ((Key)StopLoopKeyID).ToString();
            scriptEditor.Focus();
        }

        private void InterruptThread()
        {
            cancelExecutionThread = true;
            awaitingThreadEnd = true;

            for (int i = 0; i < PostInterruptionList.Count; i++)
            {
                PostInterruptionList[i].Invoke();
            }
        }
        #endregion

        #region Public Methods
        public void ProgressCurrentScopeOnce()
        {
            CurrentScope.LineNumber++;
        }

        public bool VariableExists(string identifier)
        {
            return Variables.Exists(v => v.Identifier == identifier);
        }

        public void CreateVariable(Interpreter.Variable variable)
        {
            Variables.Add(variable);
        }

        public Interpreter.Variable GetVariable(string identifier)
        {
            return Variables.FirstOrDefault(v => v.Identifier == identifier);
        }

        public void AddScope(Interpreter.Scope scope)
        {
            Scopes.Add(scope);
        }

        public void SwitchScope(int scopeID, bool resetProgression = false, bool pushOnStack = true)
        {
            if (pushOnStack)
            {
                ScopeStack.Push(scopeID);
            }
            CurrentScope = Scopes.First(s => s.Identifier == scopeID);
            if (resetProgression)
            {
                CurrentScope.LineNumber = 0;
            }
        }

        public void RegressScope(object senderRaw)
        {
            if (ScopeStack.Count > 1)
            {
                ScopeStack.Pop();
                SyncScope();
            }
            else
            {
                Exceptions.Throw(new Exceptions.RuntimeException("Tried to regress scope when already at base scope!", "Scope level could not be regressed.", senderRaw.ToString()));
            }
        }

        public void SyncScope()
        {
            SwitchScope(ScopeStack.Peek(), false, false);
        }

        public void StartScriptExecution()
        {
            //Clear buffers
            ScopeStack.Clear();
            Scopes.Clear();
            Variables.Clear();
            StartScope = null;
            StopScope = null;

            //Compile
            Interpreter.Intermediate[] intermediates = Interpreter.Interpret(this.scriptEditor.Text);
            Interpreter.Execution[] executions = Interpreter.Compile(intermediates);
            Scopes.Add(new Interpreter.Scope(MainScopeID, executions, "Compilation"));
            SwitchScope(MainScopeID);

            if (StartScope != null)
            {
                Scopes.Add(StartScope);
                SwitchScope(StartScopeID);
            }

            //Thread setup
            if (scriptExecutionThread == null || scriptExecutionThread.ThreadState == ThreadState.Stopped)
            {
                scriptExecutionThread = new Thread(ScriptExecutionTick);
                scriptExecutionThread.IsBackground = true;
                if (BlockThread.IsChecked.HasValue)
                {
                    BlockThreadForUI = BlockThread.IsChecked.Value;
                }
                else
                {
                    BlockThreadForUI = true;
                }
                scriptExecutionThread.SetApartmentState(ApartmentState.STA);
            }

            //Execution setup
            ScopedInThisTick = false;
            ProgressToNextLine = true;
            UpdateLoopState();
            UpdateLineInterval();
            ScriptExecutionThreadEndedCallback = () => { StartButton.Content = "Start"; };
            StartButton.Content = "Stop";
            cancelExecutionThread = false;
            scriptExecutionThread.Start();
        }

        public void StopScriptExecution()
        {
            if (StopScope != null)
            {
                if (StopScope.Executions != null && StopScope.Executions.Length > 0)
                {
                    StopScope.Executions[StopScope.Executions.Length - 1].Instruction += () =>
                    {
                        InterruptThread();
                        return null;
                    };
                }
                else
                {
                    StopScope.Executions = new Interpreter.Execution[]
                    {
                        new Interpreter.Execution(() =>
                        {
                            InterruptThread();
                            return null;
                        },
                        null)
                    };
                }

                Scopes.Add(StopScope);
                SwitchScope(StopScopeID);
            }
            else
            {
                InterruptThread();
            }
        }

        public void SetLineProgression(bool enabled)
        {
            ProgressToNextLine = enabled;
        }

        public void SetLineExecution(bool enabled)
        {
            LineExecution = enabled;
        }

        public void FocusScript(int line, int lineoffset)
        {
            this.scriptEditor.ScrollToLine(line);

            int lineTracker = 0;
            int foundCharIndex = 0;
            for (int i = 0; i < this.scriptEditor.Text.Length; i++)
            {
                if (this.scriptEditor.Text[i] == '\n' || this.scriptEditor.Text[i] == '\r')
                {
                    lineTracker++;
                }
                if (lineTracker == line - 1)
                {
                    foundCharIndex = i + lineoffset;
                    break;
                }
            }

            this.scriptEditor.Select(foundCharIndex, 0);

            scriptEditor.Focus();
        }

        public void NotifyException(Exceptions.ScriptException exception)
        {
            MessageBox.Show(exception.ToString());
        }

        public void UpdateLoopState(bool? overrideState = null)
        {
            if (overrideState.HasValue)
            {
                LoopEnabled = overrideState.Value;
                Loop.IsChecked = overrideState.Value;
            }
            else
            {
                if (Loop.IsChecked.HasValue)
                {
                    LoopEnabled = Loop.IsChecked.Value;
                }
                else
                {
                    LoopEnabled = false;
                }
            }
        }

        public void UpdateLineInterval()
        {
            if (Interval.Value.HasValue)
            {
                LineInterval = Interval.Value.Value;
            }
            else
            {
                LineInterval = 0;
            }
        }

        public void RegisterStartScope(Interpreter.Scope scope)
        {
            StartScope = scope;
        }

        public void RegisterStopScope(Interpreter.Scope scope)
        {
            StopScope = scope;
        }
        #endregion
    }
}