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
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Document;
using StrPrsL.Scripting.Completion;

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
        private Microsoft.Win32.SaveFileDialog SaveFileDialog;
        private bool? SaveFileResult;
        private bool changesMade = false;
        private System.Windows.Forms.ColorDialog ColorPickerDialog = new System.Windows.Forms.ColorDialog();
        private Color themeColor = Color.FromRgb(26, 26, 26);
        private bool windowLoaded = false;
        private List<Action> afterWindowLoaded = new List<Action>();

        public int StopLoopKeyID
        {
            get;
            private set;
        } = 93;
        public Key StopLoopKey
        {
            get;
            private set;
        } = Key.F4;

        public string LoadedFile
        {
            get;
            private set;
        } = "";

        private WaitingManager WaitingManager = new WaitingManager();

        private InsertCodeWindow InsertCodeWindow;

        private ICSharpCode.AvalonEdit.Search.SearchPanel searchPanel;

        #region Theme
        private Helpers.VerboseColor themeVerboseColor;
        private Helpers.VerboseColor baseColor = new Helpers.VerboseColor(Color.FromArgb(255, 26, 26, 26));
        private int customColorIndexer = 0;
        private List<int> customColorsBuffer = new List<int>();
        private List<ColorDifference> themeColoringMembers = new List<ColorDifference>();

        private ColorDifference baseCommand;
        private ColorDifference baseCommandBrace;
        private ColorDifference baseFunction;
        private ColorDifference baseFunctionBrace;
        private ColorDifference baseString;
        private ColorDifference baseNumberLiteral;
        private ColorDifference baseSeperator;
        private ColorDifference baseParantheses;
        private ColorDifference baseBlock;

        private string loadedTheme;
        public class ColorDifference
        {
            public Helpers.VerboseColor BG_VC;
            public bool DoBG;
            public double BG_H_D;
            public double BG_S_D;
            public double BG_V_D;

            public Helpers.VerboseColor FG_VC;
            public bool DoFG;
            public double FG_H_D;
            public double FG_S_D;
            public double FG_V_D;

            public string AdditionalIdentifier;

            public FrameworkElement Element;

            public Helpers.VerboseColor BaseColor;
            private Helpers.VerboseColor.HSV_Color BaseHSV;

            public ColorDifference(Helpers.VerboseColor baseColor, FrameworkElement element)
            {
                this.Element = element;
                this.BaseColor = baseColor;

                BaseHSV = baseColor.HSVColor;


                object bgP = element.GetValue(Control.BackgroundProperty);
                object fgP = element.GetValue(Control.ForegroundProperty);

                if (bgP != null)
                {
                    Color bg = ((SolidColorBrush)bgP).Color;

                    BG_VC = new Helpers.VerboseColor(bg);
                    BG_VC.HSVColor.Out(out BG_H_D, out BG_S_D, out BG_V_D);

                    BG_H_D -= BaseHSV.H;
                    BG_S_D -= BaseHSV.S;
                    BG_V_D -= BaseHSV.V;
                    DoBG = true;
                }

                if (fgP != null)
                {
                    Color fg = ((SolidColorBrush)fgP).Color;

                    FG_VC = new Helpers.VerboseColor(fg);
                    FG_VC.HSVColor.Out(out FG_H_D, out FG_S_D, out FG_V_D);

                    FG_H_D -= BaseHSV.H;
                    FG_S_D -= BaseHSV.S;
                    FG_V_D -= BaseHSV.V;
                    DoFG = true;
                }
            }

            public ColorDifference(Helpers.VerboseColor baseColor, Color currentColor, string additionalIdentifier)
            {
                this.BaseColor = baseColor;
                BaseHSV = baseColor.HSVColor;

                BG_VC = new Helpers.VerboseColor(currentColor); ;
                BG_VC.HSVColor.Out(out BG_H_D, out BG_S_D, out BG_V_D);

                BG_H_D -= BaseHSV.H;
                BG_S_D -= BaseHSV.S;
                BG_V_D -= BaseHSV.V;
                DoBG = true;
                DoFG = false;
            }

            public void SetThemeColor(Helpers.VerboseColor themeColor)
            {
                if (DoBG)
                {
                    Element.SetValue(Control.BackgroundProperty, new SolidColorBrush(GetThemeColor_BG(themeColor)));
                }

                if (DoFG)
                {
                    Element.SetValue(Control.ForegroundProperty, new SolidColorBrush(themeColor.HSVColor.PreserveAdd(FG_H_D, FG_S_D, FG_V_D).Alpha((byte)FG_VC.A)));
                }
            }

            public Color GetThemeColor_BG(Helpers.VerboseColor themeColor)
            {
                return themeColor.HSVColor.PreserveAdd(BG_H_D, BG_S_D, BG_V_D).Alpha((byte)BG_VC.A);
            }
        }
        #endregion

        #region AutoCompletion
        private CustomCompletionWindow AutoCompletionWindow;
        private IList<ICompletionData> commandCompletionData = new List<ICompletionData>();
        private IList<ICompletionData> functionCompletionData = new List<ICompletionData>();

        private int caretLastPosition;
        private string currentLine;
        private string lineRemaining;
        private DocumentLine line;
        private string insertedCode;
        private int currentIndentation = 0;
        private int spaceCount = 0;
        private string indentationInsert = "";

        private class AutoCompletionData : ICompletionData
        {
            private string _Description;
            private string fillData;
            public Interpreter.Intermediate.InstructionType InstructionType;
            public InstructionParameter[] Parameters;

            private int caretOffset;

            public AutoCompletionData(string text, string description, string fillData, Interpreter.Intermediate.InstructionType instructionType, InstructionParameter[] parameters)
            {
                this.Text = text;
                _Description = description;
                this.fillData = fillData;
                InstructionType = instructionType;
                Parameters = parameters;
            }

            public System.Windows.Media.ImageSource Image
            {
                get { return null; }
            }

            public string Text { get; private set; }

            // Use this property if you want to show a fancy UIElement in the list.
            public object Content
            {
                get { return this.Text; }
            }

            public object Description => _Description;

            public double Priority => 1.0d;

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                textArea.Document.Replace(completionSegment, this.fillData);

                caretOffset = Interpreter.Syntax.ParameterFieldEnd.Length +
                        (Interpreter.Syntax.ParameterSeperator.Length * Math.Max(Parameters.Length - 1, 0)) +
                        Math.Max(Parameters.Length - 1, 0) +
                        string.Join("", Parameters.Select(p => p.parameterName)).Length;

                if (InstructionType == Interpreter.Intermediate.InstructionType.Command)
                {
                    caretOffset += Interpreter.Syntax.CommandEnd.Length;
                    textArea.Caret.Offset -= caretOffset;
                }
                else if (InstructionType == Interpreter.Intermediate.InstructionType.Function)
                {
                    caretOffset += Interpreter.Syntax.FunctionEnd.Length;
                    textArea.Caret.Offset -= caretOffset;
                }
            }
        }
        #endregion

        #region ScriptExecution
        public bool LoopEnabled;
        public int LineInterval;
        public bool BlockThreadForUI;
        public bool StatusEnabled;
        public bool AbortStart;

        public bool ScriptThreadAlive
        {
            get
            {
                return scriptExecutionThread != null && scriptExecutionThread.IsAlive;
            }
        }

        public bool ScopedInOnThisTick = false;

        private bool regressedOnThisTick = false;

        public BlockingCollection<Message> MessageBlockedCollection = new BlockingCollection<Message>(1);
        public ConcurrentQueue<Message> MessageQueue = new ConcurrentQueue<Message>();
        private Message currentMessage;
        public BlockingCollection<Action> CrossThreadOperationBlockedCollection = new BlockingCollection<Action>(1);
        private Action currentCrossThreadAction;
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

        /// <summary>
        /// Causes <see cref="ScriptExecutionTick"/> to pause script execution temporarily.
        /// Primarily used to allow the UI thread to manipulate cross-thread varaibles without interruption from <see cref="scriptExecutionThread"/>.
        /// E.g.: Enqueue the Stop block if one exists when the UI thread calls <see cref="StopScriptExecution"/>.
        /// </summary>
        private bool scriptExecutionTempPause;

        private List<Interpreter.Variable> Variables = new List<Interpreter.Variable>();

        private List<Interpreter.Execution> stopBlockExecutions = new List<Interpreter.Execution>();

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
        } = 128;

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

            SaveFileDialog = new Microsoft.Win32.SaveFileDialog();
            SaveFileDialog.DefaultExt = ".str";
            SaveFileDialog.Filter = "Str scripts (.str)|*.str";

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dispatcherTimer.Start();
        }

        public class Message
        {
            public string Text;
            public MessageType Type;
            public Action OnClick;

            public Message(string text, MessageType type, Action onClick)
            {
                Text = text;
                Type = type;
                OnClick = onClick;
            }

            public enum MessageType
            {
                Output,
                Status
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

            for (int i = 0; i < InstructionRegister.Instructions.Length; i++)
            {
                AutoCompletionData data = new AutoCompletionData
                (
                    InstructionRegister.Instructions[i].Name,
                    InstructionRegister.Instructions[i].GetString(true, true),
                    InstructionRegister.Instructions[i].GetUsage(true, true, false).Remove(0, 1),
                    Interpreter.Intermediate.InstructionType.None,
                    InstructionRegister.Instructions[i].Parameters
                );
                if (InstructionRegister.Instructions[i].InstructionType == InstructionType.Command)
                {
                    data.InstructionType = Interpreter.Intermediate.InstructionType.Command;
                    commandCompletionData.Add(data);
                }
                else if (InstructionRegister.Instructions[i].InstructionType == InstructionType.Function)
                {
                    data.InstructionType = Interpreter.Intermediate.InstructionType.Function;
                    functionCompletionData.Add(data);
                }
            }

            scriptEditor.TextArea.TextEntering += TextEntering;
            scriptEditor.TextArea.TextEntered += TextEntered;

            searchPanel = ICSharpCode.AvalonEdit.Search.SearchPanel.Install(scriptEditor.TextArea);

            scriptEditor.Options.ShowBoxForControlCharacters = true;
            scriptEditor.Options.HideCursorWhileTyping = false;
            scriptEditor.Options.HighlightCurrentLine = true;

            scriptEditor.TextArea.TextView.ContextMenu = (ContextMenu)this.Resources["scriptEdtiorContextMenu"];

            baseCommand = new ColorDifference(baseColor, Color.FromArgb(255, 217, 152, 169), "Command");
            baseCommandBrace = new ColorDifference(baseColor, Color.FromArgb(255, 173, 42, 77), "CommandBrace");
            baseFunction = new ColorDifference(baseColor, Color.FromArgb(255, 179, 255, 224), "Function");
            baseFunctionBrace = new ColorDifference(baseColor, Color.FromArgb(255, 47, 148, 108), "FunctionBrace");
            baseString = new ColorDifference(baseColor, Color.FromArgb(255, 214, 109, 91), "String");
            baseNumberLiteral = new ColorDifference(baseColor, Color.FromArgb(255, 123, 165, 219), "NumberLiteral");
            baseSeperator = new ColorDifference(baseColor, Color.FromArgb(255, 121, 98, 189), "Seperator");
            baseParantheses = new ColorDifference(baseColor, Color.FromArgb(255, 99, 62, 122), "Parantheses");
            baseBlock = new ColorDifference(baseColor, Color.FromArgb(255, 242, 2, 142), "Block");

            ColorizeWindow();

#if DEBUG
            string debugFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "debug.str");
            if (File.Exists(debugFile))
            {
                LoadScriptFile(debugFile);
            }
#endif
            if (!string.IsNullOrEmpty(App.OpenedWithFile))
            {
                LoadScriptFile(App.OpenedWithFile);
            }

            windowLoaded = true;
            for (int i = 0; i < afterWindowLoaded.Count; i++)
            {
                afterWindowLoaded[i]?.Invoke();
            }

            afterWindowLoaded.Clear();
        }

        private void scriptEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Functions.GetKey(Key.LeftCtrl) || Functions.GetKey(Key.RightCtrl))
            {
                if (e.Key == Key.K)
                {
                    InsertCodeWindow = new InsertCodeWindow(this, InsertCodeWindow.InsertMode.KeyID, new Key[] { Key.LeftCtrl, Key.RightCtrl, Key.K });
                    InsertCodeWindow.Show();
                }
                else if (e.Key == Key.P)
                {
                    InsertCodeWindow = new InsertCodeWindow(this, InsertCodeWindow.InsertMode.AutoPixel, null);
                    InsertCodeWindow.Show();
                }
            }
            else if (Functions.GetKey(Key.LeftShift) || Functions.GetKey(Key.RightShift))
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;

                    line = scriptEditor.Document.GetLineByOffset(scriptEditor.CaretOffset);
                    currentLine = scriptEditor.Text.Substring(line.Offset, line.Length);

                    UpdateIndentation();

                    indentationInsert = "";
                    for (int i = 0; i < currentIndentation; i++)
                    {
                        indentationInsert += "\t";
                    }

                    caretLastPosition = line.EndOffset;
                    insertedCode = $"{Environment.NewLine}{indentationInsert}{{{Environment.NewLine}{indentationInsert}\t{Environment.NewLine}{indentationInsert}}}";
                    scriptEditor.Text = scriptEditor.Text.Insert(scriptEditor.Document.GetLineByOffset(scriptEditor.CaretOffset).EndOffset, insertedCode);
                    scriptEditor.CaretOffset = caretLastPosition + (insertedCode.Length - 2) - indentationInsert.Length;
                    scriptEditor.TextArea.Caret.BringCaretToView();
                }
            }
            else
            {
                if (e.Key == Key.Enter)
                {
                    line = scriptEditor.Document.GetLineByOffset(scriptEditor.CaretOffset);
                    currentLine = scriptEditor.Text.Substring(line.Offset, scriptEditor.CaretOffset - line.Offset);
                    if (Interpreter.Helpers.SequenceLast(currentLine, Interpreter.Syntax.BlockBegin))
                    {
                        e.Handled = true;
                        lineRemaining = scriptEditor.Text.Substring(scriptEditor.CaretOffset, line.Length - (scriptEditor.CaretOffset - line.Offset));

                        UpdateIndentation();

                        caretLastPosition = scriptEditor.CaretOffset;
                        insertedCode = $"{Environment.NewLine}{indentationInsert}\t";
                        scriptEditor.Text = scriptEditor.Text.Insert(caretLastPosition, insertedCode);
                        scriptEditor.CaretOffset = caretLastPosition + insertedCode.Length;
                        scriptEditor.TextArea.Caret.BringCaretToView();
                    }
                }
            }
        }

        private void TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0)
            {
                changesMade = true;
                if (AutoCompletionWindow != null)
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    if (!char.IsLetterOrDigit(e.Text[0]))
                    {
                        AutoCompletionWindow.CompletionList.RequestInsertion(e);
                    }
                }
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }

        private void TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == Interpreter.Syntax.CommandBegin || e.Text == Interpreter.Syntax.FunctionBegin)
            {
                changesMade = true;
                AutoCompletionWindow = new CustomCompletionWindow(scriptEditor.TextArea);
                AutoCompletionWindow.Background = Background;
                AutoCompletionWindow.ResizeMode = ResizeMode.NoResize;
                AutoCompletionWindow.WindowStyle = WindowStyle.None;

                AutoCompletionWindow.CompletionList.CompletionData.Clear();
                if (e.Text == Interpreter.Syntax.CommandBegin)
                {
                    for (int i = 0; i < commandCompletionData.Count; i++)
                    {
                        AutoCompletionWindow.CompletionList.CompletionData.Add(commandCompletionData[i]);
                    }
                }
                else if (e.Text == Interpreter.Syntax.FunctionBegin)
                {
                    for (int i = 0; i < functionCompletionData.Count; i++)
                    {
                        AutoCompletionWindow.CompletionList.CompletionData.Add(functionCompletionData[i]);
                    }
                }

                AutoCompletionWindow.Show();
                AutoCompletionWindow.Closed += delegate {
                    AutoCompletionWindow = null;
                };
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = false;
            if (scriptExecutionThread != null && scriptExecutionThread.IsAlive)
            {
                StopScriptExecution();
            }
            SaveChanges(null, () => e.Cancel = true);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges(() =>
            {
                OpenFileResult = OpenFileDialog.ShowDialog();

                if (OpenFileResult.HasValue && OpenFileResult.Value == true)
                {
                    LoadScriptFile(OpenFileDialog.FileName);
                }
            });
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            PromptSave();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleScriptExecution();
        }

        private void ClearOutputButton_Click(object sender, RoutedEventArgs e)
        {
            ClearOutput();
        }

        private void StopLoopButton_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateAbortLoopKey((int)e.Key);
        }

        private void scriptEditor_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                LoadScriptFile(((string[])e.Data.GetData(DataFormats.FileDrop))[0]);
            }
        }

        private void scriptOutput_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ((Logger.TestItemClass)scriptOutput.SelectedItem).OnClick?.Invoke();
        }

        private void scriptEdtiorContextMenuCut_Click(object sender, RoutedEventArgs e)
        {
            scriptEditor.Cut();
            changesMade = true;
        }

        private void scriptEdtiorContextMenuCopy_Click(object sender, RoutedEventArgs e)
        {
            scriptEditor.Copy();
        }

        private void scriptEdtiorContextMenuPaste_Click(object sender, RoutedEventArgs e)
        {
            scriptEditor.Paste();
            changesMade = true;
        }

        private void scriptEdtiorContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            //0 = Cut, 1 = Copy, 2 = Paste
            if (scriptEditor.SelectionLength <= 0)
            {
                ((MenuItem)scriptEditor.TextArea.TextView.ContextMenu.Items[0]).IsEnabled = false;
                ((MenuItem)scriptEditor.TextArea.TextView.ContextMenu.Items[1]).IsEnabled = false;
            }
            else
            {
                ((MenuItem)scriptEditor.TextArea.TextView.ContextMenu.Items[0]).IsEnabled = true;
                ((MenuItem)scriptEditor.TextArea.TextView.ContextMenu.Items[1]).IsEnabled = true;
            }
            ((MenuItem)scriptEditor.TextArea.TextView.ContextMenu.Items[2]).IsEnabled = Clipboard.ContainsText();
        }

        private void ThemeColorButton_Click(object sender, RoutedEventArgs e)
        {
            PickThemeColor();
        }

        private void CopyOutput_Click(object sender, RoutedEventArgs e)
        {
            string result = "";
            for (int i = 0; i < scriptOutput.Items.Count; i++)
            {
                result += scriptOutput.Items[i].ToString();
                if (i != scriptOutput.Items.Count - 1)
                {
                    result += Environment.NewLine;
                    result += Environment.NewLine;
                }
            }
            Clipboard.SetText(result);
        }
        #endregion

        #region Private Methods
        private void UpdateIndentation()
        {
            currentIndentation = 0;
            spaceCount = 0;

            for (int i = 0; i < currentLine.Length; i++)
            {
                if (currentLine[i] == '\t')
                {
                    currentIndentation++;
                }
                else if (currentLine[i] == ' ')
                {
                    spaceCount++;
                }
                else
                {
                    break;
                }
            }

            currentIndentation += (int)Math.Floor((double)spaceCount / scriptEditor.Options.IndentationSize);

            indentationInsert = "";
            for (int i = 0; i < currentIndentation; i++)
            {
                indentationInsert += "\t";
            }
        }

        private void LoadScript(string script)
        {
            this.scriptEditor.Text = "";

            loadedTheme = Helpers.GetStringTag("ThemeColor", script);

            if (!string.IsNullOrEmpty(loadedTheme))
            {
                script = Functions.RemoveStringValue("ThemeColor", script);

                string RText = loadedTheme.Remove(loadedTheme.IndexOf(",")).Trim();
                string GText = loadedTheme.Remove(0, loadedTheme.IndexOf(",") + 1).Trim();
                GText = GText.Remove(GText.IndexOf(","));
                string BText = loadedTheme.Remove(0, loadedTheme.LastIndexOf(",") + 1).Trim();

                themeColor = Color.FromArgb
                    (
                        255,
                        byte.Parse(RText),
                        byte.Parse(GText),
                        byte.Parse(BText)
                    );
                DoTheme();
            }

            this.scriptEditor.Text = script;
        }

        private void LoadScriptFile(string filename)
        {
            LoadScript(File.ReadAllText(filename));
            LoadedFile = filename;
            UpdateTitleForFile(filename);
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
                if (scriptExecutionTempPause)
                {
                    continue;
                }

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
                        if (StatusEnabled)
                        {
                            if (CurrentScope.CurrentLine.Origin != null)
                            {
                                Functions.Status(CurrentScope.CurrentLine.Origin.ToString(), CurrentScope.CurrentLine.Origin);
                            }
                        }
                        CurrentScope.CurrentLine.Instruction.Invoke();
                    }
                    if (ProgressToNextLine && !ScopedInOnThisTick)
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

                if (ScopedInOnThisTick)
                {
                    ScopedInOnThisTick = false;
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
                    ProcessMessage(currentMessage);
                }
            }
            else
            {
                if (MessageQueue.TryDequeue(out currentMessage))
                {
                    ProcessMessage(currentMessage);
                }
            }
            if (ThreadEndedActionQueue.TryDequeue(out currentAction))
            {
                currentAction.Invoke();
            }

            if (CrossThreadOperationBlockedCollection.TryTake(out currentCrossThreadAction))
            {
                currentCrossThreadAction.Invoke();
            }

            if (Functions.GetKey(StopLoopKey))
            {
                if (scriptExecutionThread != null && scriptExecutionThread.IsAlive)
                {
                    StopScriptExecution();
                }
            }
        }

        private void ProcessMessage(Message message)
        {
            if (message.Type == Message.MessageType.Output)
            {
                Logger.Log(message.Text, message.OnClick);
            }
            else if (message.Type == Message.MessageType.Status)
            {
                StatusDisplay.Text = message.Text;
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

        private void PromptSave()
        {
            if (!string.IsNullOrEmpty(LoadedFile))
            {
                MessageBoxResult questionResult = MessageBox.Show($"Do you want to overwrite the contents of the file at {LoadedFile}?", "Overwrite file", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (questionResult == MessageBoxResult.Yes)
                {
                    File.WriteAllText(LoadedFile, GetScriptForSaving());
                    changesMade = false;
                }
                else if (questionResult == MessageBoxResult.No)
                {
                    ShowSaveFile();
                }
                else if (questionResult == MessageBoxResult.Cancel)
                {

                }
            }
            else
            {
                ShowSaveFile();
            }
        }

        private void ShowSaveFile()
        {
            SaveFileResult = SaveFileDialog.ShowDialog();
            if (SaveFileResult.HasValue && SaveFileResult.Value)
            {
                File.WriteAllText(SaveFileDialog.FileName, GetScriptForSaving());
                LoadedFile = SaveFileDialog.FileName;
                UpdateTitleForFile(LoadedFile);
                changesMade = false;
            }
        }

        private void SaveChanges(Action post, Action onCancel = null)
        {
            if (changesMade)
            {
                MessageBoxResult questionResult = MessageBox.Show($"You have unsaved changes.{Environment.NewLine}Would you like to save the changes you made?", "Save changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (questionResult == MessageBoxResult.Yes)
                {
                    PromptSave();
                    post?.Invoke();
                }
                else if (questionResult == MessageBoxResult.No)
                {
                    post?.Invoke();
                }
                else if (questionResult == MessageBoxResult.Cancel)
                {
                    onCancel?.Invoke();
                }
            }
            else
            {
                post?.Invoke();
            }
        }

        private void UpdateTitleForFile(string filePath)
        {
            this.Title = $"StrPrsL - {System.IO.Path.GetFileName(filePath)}";
        }

        private void ColorizeWindow()
        {
            themeVerboseColor = new Helpers.VerboseColor(themeColor);

            this.Background = new SolidColorBrush(themeColor);

            scriptEditor.TextArea.TextView.CurrentLineBorder = new Pen(new SolidColorBrush(themeVerboseColor.HSVColor.PreserveAdd(0, 0.0191d, 0.079d)), 2.5d);
            scriptEditor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(themeVerboseColor.HSVColor.PreserveAdd(0, 0.006d, 0.027d));
            scriptEditor.TextArea.SelectionBrush = new SolidColorBrush(themeVerboseColor.HSVColor.PreserveAdd(0, 0.117d, 0.47d));
            scriptEditor.TextArea.SelectionBorder = new Pen(new SolidColorBrush(themeVerboseColor.HSVColor.PreserveAdd(0, 0.175d, 0.7d)), 1.5d);

            foreach (FrameworkElement control in Helpers.FindVisualChildren<FrameworkElement>(this))
            {
                if (!themeColoringMembers.Exists(m => m.Element == control))
                {
                    themeColoringMembers.Add(new ColorDifference(baseColor, control));
                }

                themeColoringMembers.First(m => m.Element == control).SetThemeColor(themeVerboseColor);
            }

            searchPanel.Loaded += (s, e) =>
            {
                foreach (Border control in Helpers.FindVisualChildren<Border>(searchPanel))
                {
                    control.SetValue(Border.BackgroundProperty, new SolidColorBrush(themeColor));
                }

                FieldInfo[] fields = typeof(ICSharpCode.AvalonEdit.Search.SearchPanel).GetFields(
                                         BindingFlags.NonPublic |
                                         BindingFlags.Instance);
                System.Windows.Controls.Primitives.Popup popup = ((System.Windows.Controls.Primitives.Popup)fields.First(f => f.Name == "dropdownPopup").GetValue(searchPanel));
                ((Border)popup.Child).Background = new SolidColorBrush(themeColor);
            };

            scriptEditor.SyntaxHighlighting.GetNamedColor("Command").Foreground = new ICSharpCode.AvalonEdit.Highlighting.SimpleHighlightingBrush
            (
                baseCommand.GetThemeColor_BG(themeVerboseColor)
            );
            scriptEditor.SyntaxHighlighting.GetNamedColor("CommandBrace").Foreground = new ICSharpCode.AvalonEdit.Highlighting.SimpleHighlightingBrush
            (
                baseCommandBrace.GetThemeColor_BG(themeVerboseColor)
            );
            scriptEditor.SyntaxHighlighting.GetNamedColor("Function").Foreground = new ICSharpCode.AvalonEdit.Highlighting.SimpleHighlightingBrush
            (
                baseFunction.GetThemeColor_BG(themeVerboseColor)
            );
            scriptEditor.SyntaxHighlighting.GetNamedColor("FunctionBrace").Foreground = new ICSharpCode.AvalonEdit.Highlighting.SimpleHighlightingBrush
            (
                baseFunctionBrace.GetThemeColor_BG(themeVerboseColor)
            );
            scriptEditor.SyntaxHighlighting.GetNamedColor("String").Foreground = new ICSharpCode.AvalonEdit.Highlighting.SimpleHighlightingBrush
            (
                baseString.GetThemeColor_BG(themeVerboseColor)
            );
            scriptEditor.SyntaxHighlighting.GetNamedColor("NumberLiteral").Foreground = new ICSharpCode.AvalonEdit.Highlighting.SimpleHighlightingBrush
            (
                baseNumberLiteral.GetThemeColor_BG(themeVerboseColor)
            );
            scriptEditor.SyntaxHighlighting.GetNamedColor("Seperator").Foreground = new ICSharpCode.AvalonEdit.Highlighting.SimpleHighlightingBrush
            (
                baseSeperator.GetThemeColor_BG(themeVerboseColor)
            );
            scriptEditor.SyntaxHighlighting.GetNamedColor("Parantheses").Foreground = new ICSharpCode.AvalonEdit.Highlighting.SimpleHighlightingBrush
            (
                baseParantheses.GetThemeColor_BG(themeVerboseColor)
            );
            scriptEditor.SyntaxHighlighting.GetNamedColor("Block").Foreground = new ICSharpCode.AvalonEdit.Highlighting.SimpleHighlightingBrush
            (
                baseBlock.GetThemeColor_BG(themeVerboseColor)
            );
        }
        #endregion

        #region Public Methods
        public void PickThemeColor()
        {
            customColorsBuffer.Clear();
            if (ColorPickerDialog.CustomColors == null || ColorPickerDialog.CustomColors.Length == 0)
            {
                ColorPickerDialog.CustomColors = new int[16];
                ColorPickerDialog.CustomColors[0] = System.Drawing.ColorTranslator.ToOle(themeColor.ToFormsColor());
            }
            else
            {
                customColorsBuffer.AddRange(ColorPickerDialog.CustomColors);
                if (customColorIndexer >= ColorPickerDialog.CustomColors.Length)
                {
                    customColorIndexer = 0;
                }
                customColorsBuffer[customColorIndexer] = System.Drawing.ColorTranslator.ToOle(themeColor.ToFormsColor());
                ColorPickerDialog.CustomColors = customColorsBuffer.ToArray();
            }

            ColorPickerDialog.Color = themeColor.ToFormsColor();

            if (ColorPickerDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                themeColor = ColorPickerDialog.Color.ToMediaColor();
                DoTheme();
                customColorIndexer++;
            }
        }

        public void DoTheme()
        {
            if (!windowLoaded)
            {
                afterWindowLoaded.Add(ColorizeWindow);
            }
            else
            {
                ColorizeWindow();
            }
        }

        public void ClearMessageQueue()
        {
            if (BlockThreadForUI)
            {
                MessageBlockedCollection.Clear();
            }
            else
            {
                MessageQueue.Clear();
            }
        }

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
#if DEBUG
            if (Scopes.Exists(s => s.Identifier == scope.Identifier))
            {
                throw new Exception("Error: Scope identifier already exists!");
            }
#endif
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
            AbortStart = false;
            ScopeStack.Clear();
            Scopes.Clear();
            Variables.Clear();
            StartScope = null;
            StopScope = null;

            //Compile
            Interpreter.Intermediate[] intermediates = Interpreter.Interpret(this.scriptEditor.Text);
            Interpreter.Execution[] executions = Interpreter.Compile(intermediates);

            if (AbortStart)
            {
                return;
            }

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
                scriptExecutionThread.SetApartmentState(ApartmentState.STA);
            }

            //Execution setup
            scriptExecutionTempPause = false;
            ScopedInOnThisTick = false;
            ProgressToNextLine = true;
            UpdateLoopState();
            UpdateLineInterval();
            UpdateUIThreadBlock();
            UpdateStatusEnabled();
            ScriptExecutionThreadEndedCallback = () => { StartButton.Content = "Start"; scriptEditor.IsReadOnly = false; };
            StartButton.Content = "Stop";
            cancelExecutionThread = false;
            scriptEditor.IsReadOnly = true;
            scriptExecutionThread.Start();
        }

        public void StopScriptExecution(bool setScopedIn = false)
        {
            SetLineProgression(true);
            SetLineExecution(true);
            WaitingManager.StopWait();

            if (StopScope != null)
            {
                scriptExecutionTempPause = true;
                stopBlockExecutions.Clear();

                if (StopScope.Executions != null && StopScope.Executions.Length > 0)
                {
                    stopBlockExecutions.AddRange(StopScope.Executions);
                }

                stopBlockExecutions.Add(new Interpreter.Execution
                (
                    () =>
                    {
                        InterruptThread();
                        return null;
                    },
                    null
                ));

                StopScope.Executions = stopBlockExecutions.ToArray();
                Scopes.Add(StopScope);
                SwitchScope(StopScopeID, true);
                if (setScopedIn)
                {
                    ScopedInOnThisTick = true;
                }
                scriptExecutionTempPause = false;
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

            scriptEditor.TextArea.Caret.BringCaretToView();

            scriptEditor.TextArea.Focus();
        }

        public void NotifyException(Exceptions.ScriptException exception)
        {
            Action onClick = null;
            MessageBox.Show(exception.ToString(), exception.GetErrorType(), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            if (exception.Line.HasValue)
            {
                onClick = () => FocusScript(exception.Line.Value, exception.LineOffset.HasValue ? exception.LineOffset.Value : 0);
            }

            Logger.Log(exception.ToString(), onClick);
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

        public void UpdateLineInterval(int? overrideInterval = null)
        {
            if (overrideInterval.HasValue)
            {
                Interval.Value = overrideInterval.Value;
            }

            if (Interval.Value.HasValue)
            {
                LineInterval = Interval.Value.Value;
            }
            else
            {
                LineInterval = 0;
            }
        }

        public void UpdateUIThreadBlock(bool? overrideState = null)
        {
            if (overrideState.HasValue)
            {
                BlockThreadForUI = overrideState.Value;
                BlockThread.IsChecked = overrideState.Value;
            }
            else
            {
                if (BlockThread.IsChecked.HasValue)
                {
                    BlockThreadForUI = BlockThread.IsChecked.Value;
                }
                else
                {
                    BlockThreadForUI = true;
                }
            }
        }

        public void UpdateAbortLoopKey(int? overrideInterval = null)
        {
            if (overrideInterval.HasValue)
            {
                StopLoopKeyID = overrideInterval.Value;
                StopLoopKey = (Key)overrideInterval.Value;
                DisplayStopLoopKeyName();
            }
        }

        public void UpdateStatusEnabled(bool? overrideState = null)
        {
            if (overrideState.HasValue)
            {
                Status.IsChecked = overrideState.Value;
            }
            if (Status.IsChecked.HasValue)
            {
                StatusEnabled = Status.IsChecked.Value;
            }
            else
            {
                StatusEnabled = false;
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

        public void ClearOutput()
        {
            Logger.Clear();
        }

        public void InsertCode(string code, int? selectionOffset = null)
        {
            changesMade = true;

            caretLastPosition = scriptEditor.CaretOffset;
            scriptEditor.Text = scriptEditor.Text.Insert(caretLastPosition, code);
            if (selectionOffset.HasValue)
            {
                scriptEditor.CaretOffset = caretLastPosition + selectionOffset.Value;
            }
            else
            {
                scriptEditor.CaretOffset = caretLastPosition + code.Length;
            }
        }

        public void EnqueueMessage(Message message)
        {
            if (BlockThreadForUI)
            {
                MessageBlockedCollection.Add(message);
            }
            else
            {
                if (MessageQueue.Count <= MessageLimit)
                {
                    MessageQueue.Enqueue(message);
                }
            }
        }

        public string GetScriptForSaving()
        {
            string result = scriptEditor.Text;
            if (themeColor != baseColor.MediaColor)
            {
                result += $"{Helpers.WriteStringTag("ThemeColor", $"{themeColor.R},{themeColor.G},{themeColor.B}")}";
            }
            return result;
        }
        #endregion
    }
}