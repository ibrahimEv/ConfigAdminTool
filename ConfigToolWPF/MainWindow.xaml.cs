using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using ConfigToolLibrary2;

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
        public string GithubFilePath { get; set; }
        public List<string> MergedFile { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            _excelHelper = new ExcelHelper();
            _mergeFile = new MergeFile();
            CmbRepository.ItemsSource = new List<string>() { "IdentifiData", "user-admin-data", "Test" };
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
                TxtPRNumber.Text = Regex.Match(excelFileName, @"\d+").Value;
                _excelHelper.LoadWorkBook(excelFileName);
                CmbWorkSheet.ItemsSource = _excelHelper.GetAllWorkSheetNames();

            }

            HideLoading();
        }

        private async void CmbWorkSheet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowLoading();
            string headBranchName = ConfigurationManager.AppSettings["HeadBranchName"];
            int index = CmbWorkSheet.SelectedIndex;
            string tableName = _excelHelper.SelectWorkSheet(index + 1);

            GithubFilePath = await _githubHelper.GetGithubFilePath(tableName, CmbRepository.SelectedValue.ToString());

            List<string> contentGithubFile = await _githubHelper.GetContentOfFile(GithubFilePath, headBranchName);
            List<string> sql = _githubHelper.GetColumnNames(contentGithubFile);
            List<string> excelCol = _excelHelper.GetColumnNames();
            Dictionary<string, int> columnMappings = Common.GetColumnMappings(sql, excelCol);
            List<string> sqlFromExcel = _excelHelper.GetSqlFromCurrentSheet(columnMappings);

            MergedFile = _mergeFile.Merge(contentGithubFile, sqlFromExcel);
            HideLoading();
        }

        private async void CmbRepository_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowLoading();
            string githubUserToken = ConfigurationManager.AppSettings["GithubUserToken"];
            _githubHelper = new GithubHelper(CmbRepository.SelectedValue.ToString(), githubUserToken);
            List<string> allBranches = await _githubHelper.GetAllBranches();
            CmbBranches.ItemsSource = allBranches;
            List<string> reviewers = await _githubHelper.GetAllCollaborators();
            ListReviewers.ItemsSource = reviewers;
            HideLoading();
        }

        private async void BtnCreatePR_OnClick(object sender, RoutedEventArgs e)
        {
            ShowLoading();
            string headBranchName = CmbBranches.SelectedValue.ToString();
            string newBranchName = TxtNewBranchName.Text;
            List<string> reviewerNames = new List<string>();
            foreach (var reviewer in ListReviewers.SelectedItems)
            {
                reviewerNames.Add(reviewer.ToString());
            }

            //var t = await _githubHelper.CreateBranch(headBranchName, newBranchName);
            //var t1 = await _githubHelper.UpdateFile(GithubFilePath, string.Join("\n", MergedFile), newBranchName);
            int prNumber = await _githubHelper.CreatePullRequest(TxtPRNumber.Text, headBranchName, newBranchName);
            //int temp = await _githubHelper.AddReviewerToPullRequest(prNumber, reviewerNames);
            HideLoading();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            _excelHelper.CloseExcel();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SubWindow window = new SubWindow();

            window.TxtMergedFile.Text = string.Join("\n", MergedFile);
            window.Show();
        }

        private void ShowLoading()
        {
            this.Cursor = Cursors.Wait;
            this.Opacity = 0.8;
        }
        private void HideLoading()
        {
            this.Cursor = Cursors.Arrow;
            this.Opacity = 1;
        }

        private void BtnExit_OnClick(object sender, RoutedEventArgs e)
        {
           Application.Current.Shutdown();
        }
    }
}
