using System;
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

namespace StrPrsL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Microsoft.Win32.OpenFileDialog OpenFileDialog;
        public bool? OpenFileResult;

        private Logger Logger;

        public MainWindow()
        {
            InitializeComponent();
            Logger = new Logger(this);
            OpenFileDialog = new Microsoft.Win32.OpenFileDialog();
            OpenFileDialog.FileName = "Script";
            OpenFileDialog.DefaultExt = ".str";
            OpenFileDialog.Filter = "Str scripts (.str)|*.str";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StrPrsL.Scripting.SyntaxHighlighting.xshd"))
            {
                System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(stream);
                this.scriptEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
            }
        }

        private void LoadScript(string script)
        {
            this.scriptEditor.Text = script;
        }

        private void LoadScriptFile(string filename)
        {
            LoadScript(File.ReadAllText(filename));
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileResult = OpenFileDialog.ShowDialog();

            if (OpenFileResult.HasValue && OpenFileResult.Value == true)
            {
                LoadScriptFile(OpenFileDialog.FileName);
            }
        }
    }
}