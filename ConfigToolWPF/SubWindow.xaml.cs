using System;
using System.Collections.Generic;
using System.Linq;
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
        private string _tableName;
        public SubWindow(string tableName)
        {
            _tableName = tableName;
            this.Title = $"Merge_{_tableName}.sql";
            InitializeComponent();
            LoadTextBlock();
        }

        private void LoadTextBlock()
        {
            var fileDetail = MainWindow.FileDetails.SingleOrDefault(fd => fd.TableName == _tableName);

            string githubFileWithoutSpace = string.Join("\n", fileDetail.GithubFileContentList);
            githubFileWithoutSpace = githubFileWithoutSpace.Replace(" ", string.Empty);

            var mergedFileContent = fileDetail.MergedFileContentList;
            TxtBoxMergedFile.Text = string.Join("\n", mergedFileContent);
            TxtMergedFile.Text = string.Empty;
            foreach (var mergedLine in mergedFileContent)
            {
                if (fileDetail.GithubFileContentList.Contains(mergedLine))
                    TxtMergedFile.Inlines.Add(new Run(mergedLine + "\n") { Foreground = Brushes.Blue });
                else
                {
                    if (githubFileWithoutSpace.Contains(mergedLine.Replace(" ", string.Empty)))
                        TxtMergedFile.Inlines.Add(new Run(mergedLine + "\n") { Foreground = Brushes.DarkOrange });
                    else if (githubFileWithoutSpace.ToLower().Contains(mergedLine.ToLower().Replace(" ", string.Empty)))
                        TxtMergedFile.Inlines.Add(new Run(mergedLine + "\n") { Foreground = Brushes.CadetBlue });
                    else
                        TxtMergedFile.Inlines.Add(new Run(mergedLine + "\n") { Foreground = Brushes.Red });
                }
            }
        }

        private void BtnEdit_OnClick(object sender, RoutedEventArgs e)
        {
            if (BtnEdit.Content.Equals("Edit"))
            {
                TxtMergedFile.Visibility = Visibility.Hidden;
                TxtBoxMergedFile.Visibility = Visibility.Visible;
                BtnEdit.Content = "Save";
            }
            else
            {
                List<string> updatedMergeFile = TxtBoxMergedFile.Text.Split(new[] { "\n" }, StringSplitOptions.None).ToList();
                MainWindow.FileDetails.Single(fd => fd.TableName == _tableName).MergedFileContentList = updatedMergeFile;
                LoadTextBlock();

                TxtMergedFile.Visibility = Visibility.Visible;
                TxtBoxMergedFile.Visibility = Visibility.Hidden;
                BtnEdit.Content = "Edit";
            }
        }
    }
}
