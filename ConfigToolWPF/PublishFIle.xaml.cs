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
            InitializeFields();
            _repoName = repoName;
            _branchName = branchName;
            _githubHelper = githubHelper;
            _fileDetails = fileDetails;
            CmbDatabaseName.ItemsSource = new[] { "UserAdmin", "UtilizationMgmt" };
        }

        private void InitializeFields()
        {
            TxtDefaultDataPath.Text = Common.GetRegistryKeyValue("DefaultDataPath");
            TxtDefaultLogPath.Text = Common.GetRegistryKeyValue("DefaultDataLogPath");
            TxtFilePath.Text = Common.GetRegistryKeyValue("PublishFilePath");
            TxtLocalRepo.Text = Common.GetRegistryKeyValue("LocalRepository");
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

        private void OnComplete()
        {
            SaveRegistryValue();
            var res = MessageBox.Show($"Publish File Successfully created at {TxtFilePath.Text}", "Success", MessageBoxButton.OK);
            if (res == MessageBoxResult.OK) this.Close();
        }

        private void SaveRegistryValue()
        {
            Common.SetRegistryKeyValue("DefaultDataPath", TxtDefaultDataPath.Text);
            Common.SetRegistryKeyValue("DefaultDataLogPath", TxtDefaultLogPath.Text);
            Common.SetRegistryKeyValue("PublishFilePath", TxtFilePath.Text);
            Common.SetRegistryKeyValue("LocalRepository", TxtLocalRepo.Text);
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
            TxtLocalRepo.Text = OpenAndSelectFolderBrowser() ?? TxtLocalRepo.Text;
        }

        private string OpenAndSelectFolderBrowser()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (!string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    return fbd.SelectedPath;
                }

                if (result == System.Windows.Forms.DialogResult.Cancel)
                    return null;
                throw new Exception("Error selecting folder.");
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

        private void BtnBrowseFilePath_OnClick(object sender, RoutedEventArgs e)
        {
            string filePath = OpenAndSelectFolderBrowser();
            TxtFilePath.Text = string.IsNullOrEmpty(filePath) ? TxtFilePath.Text : Path.Combine(filePath, "Publish.sql");
        }

        private void BtnBrowseDefaultLogPath_OnClick(object sender, RoutedEventArgs e)
        {
            TxtDefaultLogPath.Text = OpenAndSelectFolderBrowser() ?? TxtDefaultLogPath.Text;
        }

        private void BtnBrowseDefaultDataPath_OnClick(object sender, RoutedEventArgs e)
        {
            TxtDefaultDataPath.Text = OpenAndSelectFolderBrowser() ?? TxtDefaultDataPath.Text;
        }
    }
}
