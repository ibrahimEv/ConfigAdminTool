using ConfigToolLibrary2.Model;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ConfigToolWPF
{
    /// <summary>
    /// Interaction logic for SubWindow.xaml
    /// </summary>
    public partial class SubWindow : Window
    {
        private static FileDetail _fileDetail;
        public SubWindow(FileDetail fileDetail)
        {
            _fileDetail = fileDetail;
            this.Title = $"Merge_{_fileDetail.TableName}.sql";
            InitializeComponent();
            LoadTextBlock();
        }

        private void LoadTextBlock()
        {
            foreach (var mergedLine in _fileDetail.MergedFileContentList)
            {
                if (_fileDetail.GithubFileContentList.Contains(mergedLine))
                    TxtMergedFile.Inlines.Add(new Run(mergedLine + "\n") { Foreground = Brushes.Blue });
                else
                {
                    TxtMergedFile.Inlines.Add(new Run(mergedLine + "\n") { Foreground = Brushes.Red });
                }
            }


        }

        private void BtnClose_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
