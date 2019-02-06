using ConfigToolLibrary2;
using ConfigToolLibrary2.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace ConfigToolWPF
{
    /// <summary>
    /// Interaction logic for PublishFIle.xaml
    /// </summary>
    public partial class PublishFIle : Window
    {
        private static string _repoName;
        private static string _branchName;
        private static GithubHelper _githubHelper;
        private static List<FileDetail> _fileDetails;
        public PublishFIle(string repoName, string branchName, GithubHelper githubHelper, List<FileDetail> fileDetails)
        {
            InitializeComponent();
            _repoName = repoName;
            _branchName = branchName;
            _githubHelper = githubHelper;
            _fileDetails = fileDetails;
            CmbDatabaseName.ItemsSource = new[] { "UserAdmin", "UtilizationMgmt" };
        }

        private async void BtnPublish_OnClick(object sender, RoutedEventArgs e)
        {
            ShowLoading();
            PublishFileDetails publishFile = new PublishFileDetails();

            publishFile.LocalRepoPath = TxtLocalRepo.Text;
            publishFile.OutputFilePath = TxtFilePath.Text;
            publishFile.GithubFilePath = await _githubHelper.GetGithubFilePath("PostDeploy", _repoName, false);
            Progress<string> progressOperation = new Progress<string>(value => LblLoader.Content = value);

            string databaseName = CmbDatabaseName.SelectedValue.ToString();
            string defaultFilePrefix = Path.GetFileNameWithoutExtension(TxtFilePath.Text);
            string defaultDataPath = TxtDefaultDataPath.Text;
            string defaultLogPath = TxtDefaultLogPath.Text;
            string environment = TxtEnvironment.Text;
            string subscription = TxtSubscription.Text;

            string fileContent = File.ReadAllText(@"PublishFileTemplate.txt");

            publishFile.DefaultContent = string.Format(fileContent, new[] { databaseName, defaultDataPath, defaultFilePrefix, defaultLogPath, environment, subscription });
            

            await _githubHelper.CreatePublishFile2(publishFile, _branchName, _fileDetails, progressOperation);
            HideLoading();
            OnComplete();
        }

        public void OnComplete()
        {
            var res = MessageBox.Show($"Publish File Successfully created at {TxtFilePath.Text}", "Success", MessageBoxButton.OK);
            if (res == MessageBoxResult.OK) this.Close();
        }

        private void ShowLoading()
        {
            LoaderGrid.Visibility = Visibility.Visible;
        }
        private void HideLoading()
        {
            LoaderGrid.Visibility = Visibility.Hidden;
        }

        private void BtnBrowseRepo_OnClick(object sender, RoutedEventArgs e)
        {

            using (var fbd = new FolderBrowserDialog())
            {


                DialogResult result = fbd.ShowDialog();

                if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    TxtLocalRepo.Text = fbd.SelectedPath;
                }
                else
                {
                    throw new Exception("Error selecting folder.");
                }
            }
        }

        private void CmbDatabaseName_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbDatabaseName.SelectedValue.ToString() == "UserAdmin")
            {
                TxtEnvironment.Visibility = Visibility.Visible;
                TxtSubscription.Visibility = Visibility.Visible;
            }
            else
            {
                TxtEnvironment.Visibility = Visibility.Hidden;
                TxtSubscription.Visibility = Visibility.Hidden;
            }

        }
    }
}
