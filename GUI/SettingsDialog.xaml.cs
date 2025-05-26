using System;
using System.Windows;

namespace ZpaqGUI
{
    public partial class SettingsDialog : Window
    {
        public int ThreadCount { get; private set; }

        // Properties for all zpaq options
        public bool NoAttributes { get; private set; }
        public bool Force { get; private set; }
        public bool Test { get; private set; }
        public int? Summary { get; private set; }
        public int? FragmentSize { get; private set; }
        public bool Streaming { get; private set; }
        public string MethodString { get; private set; } = string.Empty;
        public string IndexFile { get; private set; } = string.Empty;
        public string RepackArchive { get; private set; } = string.Empty;
        public string RepackPassword { get; private set; } = string.Empty;
        public string AllVersions { get; private set; } = string.Empty;
        public string ToPath { get; private set; } = string.Empty;
        public string NotPattern { get; private set; } = string.Empty;
        public string OnlyPattern { get; private set; } = string.Empty;

        public SettingsDialog()
            : this(ZpaqGUI.Properties.Settings.Default.ThreadCount > 0 ? ZpaqGUI.Properties.Settings.Default.ThreadCount : Environment.ProcessorCount, Math.Max(2, Environment.ProcessorCount))
        {
        }

        public SettingsDialog(int currentThreads, int maxThreads)
        {
            InitializeComponent();
            ThreadSlider.Maximum = Math.Max(2, maxThreads);
            ThreadSlider.Value = currentThreads;
            ThreadCountText.Text = currentThreads.ToString();
            ThreadSlider.ValueChanged += (s, e) =>
            {
                ThreadCountText.Text = ((int)ThreadSlider.Value).ToString();
            };
            // Load settings
            NoAttributesCheckBox.IsChecked = ZpaqGUI.Properties.Settings.Default.NoAttributes;
            ForceCheckBox.IsChecked = ZpaqGUI.Properties.Settings.Default.Force;
            TestCheckBox.IsChecked = ZpaqGUI.Properties.Settings.Default.Test;
            SummaryTextBox.Text = ZpaqGUI.Properties.Settings.Default.Summary;
            FragmentTextBox.Text = ZpaqGUI.Properties.Settings.Default.FragmentSize;
            StreamingCheckBox.IsChecked = ZpaqGUI.Properties.Settings.Default.Streaming;
            MethodTextBox.Text = ZpaqGUI.Properties.Settings.Default.MethodString;
            IndexTextBox.Text = ZpaqGUI.Properties.Settings.Default.IndexFile;
            RepackTextBox.Text = ZpaqGUI.Properties.Settings.Default.RepackArchive;
            RepackPasswordBox.Password = ZpaqGUI.Properties.Settings.Default.RepackPassword;
            AllTextBox.Text = ZpaqGUI.Properties.Settings.Default.AllVersions;
            ToTextBox.Text = ZpaqGUI.Properties.Settings.Default.ToPath;
            NotTextBox.Text = ZpaqGUI.Properties.Settings.Default.NotPattern;
            OnlyTextBox.Text = ZpaqGUI.Properties.Settings.Default.OnlyPattern;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ThreadCount = (int)ThreadSlider.Value;
            NoAttributes = NoAttributesCheckBox.IsChecked == true;
            Force = ForceCheckBox.IsChecked == true;
            Test = TestCheckBox.IsChecked == true;
            Summary = int.TryParse(SummaryTextBox.Text, out int s) ? s : (int?)null;
            FragmentSize = int.TryParse(FragmentTextBox.Text, out int f) ? f : (int?)null;
            Streaming = StreamingCheckBox.IsChecked == true;
            MethodString = MethodTextBox.Text.Trim();
            IndexFile = IndexTextBox.Text.Trim();
            RepackArchive = RepackTextBox.Text.Trim();
            RepackPassword = RepackPasswordBox.Password;
            AllVersions = AllTextBox.Text.Trim();
            ToPath = ToTextBox.Text.Trim();
            NotPattern = NotTextBox.Text.Trim();
            OnlyPattern = OnlyTextBox.Text.Trim();
            // Save settings
            var settings = ZpaqGUI.Properties.Settings.Default;
            settings.ThreadCount = ThreadCount;
            settings.NoAttributes = NoAttributes;
            settings.Force = Force;
            settings.Test = Test;
            settings.Summary = SummaryTextBox.Text;
            settings.FragmentSize = FragmentTextBox.Text;
            settings.Streaming = Streaming;
            settings.MethodString = MethodString;
            settings.IndexFile = IndexFile;
            settings.RepackArchive = RepackArchive;
            settings.RepackPassword = RepackPassword;
            settings.AllVersions = AllVersions;
            settings.ToPath = ToPath;
            settings.NotPattern = NotPattern;
            settings.OnlyPattern = OnlyPattern;
            settings.Save();
            DialogResult = true;
            Close();
        }
    }
}
