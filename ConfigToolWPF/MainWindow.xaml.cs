using ConfigToolLibrary2;
using ConfigToolWPF.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ConfigToolLibrary2.Model;

namespace ConfigToolWPF
{


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ExcelHelper _excelHelper;
        private static GithubHelper _githubHelper;
        private static MergeFile _mergeFile;

        public static List<FileDetail> FileDetails { get; set; }
        public static List<RepositoryDetail> RepositoryDetails { get; set; }
        private List<ExcelSheet> ExcelSheets { get; set; }
        public Visibility ShouldVisible { get; set; }
        public MainWindow()
        {
            ShouldVisible = Visibility.Hidden;
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            _excelHelper = new ExcelHelper();
            _mergeFile = new MergeFile();
            FileDetails = new List<FileDetail>();

            RepositoryDetails = new List<RepositoryDetail>();

            var config = ConfigurationManager.GetSection("repositoryDetails")
                as RepositoryConfigSection;

            foreach (RepositoryConfigInstanceElement repoConfig in config.Instances)
            {
                RepositoryDetails.Add(new RepositoryDetail()
                {
                    Id = repoConfig.Id,
                    Name = repoConfig.Name,
                    DisplayName = repoConfig.DisplayName
                });
            }

            CmbRepository.ItemsSource = RepositoryDetails;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ShowLoading();
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".xlsx"; // Default file extension
            dlg.Filter = "Excel file (.xlsx)|*.xlsx"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string excelFileName = dlg.FileName;
                TxtPRNumber.Text = Path.GetFileNameWithoutExtension(excelFileName);
                _excelHelper.LoadWorkBook(excelFileName);
                var t = _excelHelper.GetAllWorkSheetNames();
                LblExcelFilePath.Content = excelFileName;
                ExcelSheets = t.Select((value, index) => new ExcelSheet() { Id = ++index, SheetName = value }).ToList();
                DataGridExcel.ItemsSource = ExcelSheets;
            }

            HideLoading();
        }

        private async void CmbRepository_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRepository.SelectedIndex != -1)
            {
                ShowLoading();
                string githubUserToken = GetGithubToken();
                //string githubUserToken = ConfigurationManager.AppSettings["GithubUserToken"];
                _githubHelper = new GithubHelper((CmbRepository.SelectedItem as RepositoryDetail).Id, githubUserToken);
                List<string> allBranches = await _githubHelper.GetAllBranches();
                CmbBranches.ItemsSource = allBranches;
                List<string> reviewers = await _githubHelper.GetAllCollaborators();
                ListReviewers.ItemsSource = reviewers;
                HideLoading();
            }
        }

        private async void BtnCreatePR_OnClick(object sender, RoutedEventArgs e)
        {
            ShowLoading();

            if (FileDetails.Count != 0)
            {
                string headBranchName = CmbBranches.SelectedValue.ToString();
                string newBranchName = TxtNewBranchName.Text;
                List<string> reviewerNames = new List<string>();
                foreach (var reviewer in ListReviewers.SelectedItems)
                {
                    reviewerNames.Add(reviewer.ToString());
                }

                var t = await _githubHelper.CreateBranch(headBranchName, newBranchName);
                foreach (var fileDetail in FileDetails)
                {
                    await _githubHelper.UpdateFile(fileDetail.GithubFilePath, string.Join("\n", fileDetail.MergedFileContentList), newBranchName);
                }

                int prNumber = await _githubHelper.CreatePullRequest(TxtPRNumber.Text, headBranchName, newBranchName);
                if (reviewerNames.Count != 0)
                {
                    await _githubHelper.AddReviewerToPullRequest(prNumber, reviewerNames);
                }

                MessageBox.Show("PR created successfully with number : " + prNumber, "Success");
            }

            else throw new Exception("Error creating PR no file merged.");

            HideLoading();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            _excelHelper.CloseExcel();
        }

        private void ShowLoading()
        {
            LoaderGrid.Visibility = Visibility.Visible;
        }
        private void HideLoading()
        {
            LoaderGrid.Visibility = Visibility.Hidden;
        }

        private void BtnExit_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnReset_OnClick(object sender, RoutedEventArgs e)
        {
            CmbRepository.SelectedIndex = -1;
            CmbBranches.SelectedIndex = -1;

            TxtPRNumber.Text = string.Empty;
            TxtNewBranchName.Text = string.Empty;
            LblExcelFilePath.Content = string.Empty;
            ListReviewers.ItemsSource = null;
            DataGridExcel.ItemsSource = null;

            _excelHelper = new ExcelHelper();
            _mergeFile = new MergeFile();
            FileDetails = new List<FileDetail>();
        }

        private void BtnShowMergedFile(object sender, RoutedEventArgs e)
        {
            ExcelSheet sheet = ((FrameworkElement)sender).DataContext as ExcelSheet;

            SubWindow window = new SubWindow(sheet.SheetName);
            window.Show();
        }

        private async void BtnStartMerge_OnClick(object sender, RoutedEventArgs e)
        {
            string headBranchName = CmbBranches.SelectedValue.ToString();
            //remove code
            FileDetails = new List<FileDetail>();
            foreach (var sheet in ExcelSheets.Where(sheet => sheet.IsSelected))
            {
                FileDetail fd = new FileDetail();
                sheet.MergeStatus = "In Progress";
                DataGridExcel.Items.Refresh();

                string tableName = _excelHelper.SelectWorkSheet(sheet.Id);
                fd.TableName = tableName;
                fd.GithubFilePath = await _githubHelper.GetGithubFilePath(tableName, CmbRepository.SelectedValue.ToString());

                List<string> contentGithubFile = await _githubHelper.GetContentOfFile(fd.GithubFilePath, headBranchName);
                fd.GithubFileContentList = contentGithubFile.Select(c => c.Replace(Constants.ReplaceCharsForComma, ",")).ToList();

                List<string> sql = _githubHelper.GetColumnNames();
                List<string> excelCol = _excelHelper.GetColumnNames();
                Dictionary<string, int> columnMappings = Common.GetColumnMappings(sql, excelCol);

                List<string> sqlFromExcel = _excelHelper.GetSqlFromCurrentSheet(columnMappings);

                try
                {
                    List<string> mergedFile = _mergeFile.Merge(contentGithubFile, sqlFromExcel);
                    fd.MergedFileContentList = mergedFile;
                    sheet.IsMerged = true;
                    sheet.MergeStatus = "Done";
                    DataGridExcel.Items.Refresh();
                    FileDetails.Add(fd);
                }
                catch (Exception ex)
                {
                    sheet.MergeStatus = "Failed";
                    sheet.IsMerged = false;
                    sheet.ErrorMessage = ex.Message;
                    DataGridExcel.Items.Refresh();
                }
            }
        }

        private string GetGithubToken()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Config Admin POC\\Config Admin Automation"))
            {
                if (key != null)
                {
                    return key.GetValue("GithubToken").ToString();

                }
            }

            throw new Exception("Github token was not found");

        }

        private void BtnCreatePublishFile_OnClick(object sender, RoutedEventArgs e)
        {
            PublishFIle publishWindow = new PublishFIle(CmbRepository.SelectedValue.ToString(),
                CmbBranches.SelectedValue.ToString(), _githubHelper, FileDetails);
            publishWindow.Show();
        }
    }

}
