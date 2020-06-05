using Caliburn.Micro;
using Microsoft.Win32;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TableToStringConverter.ViewModels
{
    public class MainViewModel : PropertyChangedBase
    {
        private const string XlsFileExtension = ".xls";
        private const string XlsxFileExtension = ".xlsx";

        private string _selecteFilePath;

        public MainViewModel()
        {
            SelectFileCommand = new RelayCommand(_ => Task.Factory.StartNew(SelectFile));
            GenerateOutputCommand = new RelayCommand(_ => Task.Factory.StartNew(() => GenerateOutput(_selecteFilePath)));

            AddReplacementCommand = new RelayCommand(_ => ReplacementViewModels.Add(new ReplacementViewModel()));
            RemoveReplacementCommand = new RelayCommand(_ => ReplacementViewModels.RemoveAt(ReplacementViewModels.Count - 1), _ => ReplacementViewModels.Count > 0);

            ReplacementViewModels = new ObservableCollection<ReplacementViewModel>()
            {
                new ReplacementViewModel() { ReplacementKey = "{Content}" },
                new ReplacementViewModel() { ReplacementKey = "{ContentTwo}" }
            };
        }

        public ICommand RemoveReplacementCommand { get; }
        public ICommand AddReplacementCommand { get; }
        public ICommand GenerateOutputCommand { get; }
        public ICommand SelectFileCommand { get; }
        public ObservableCollection<ReplacementViewModel> ReplacementViewModels { get; }

        public int WorkingSteps { get; set; }
        public int CurrentWorkingStep { get; set; }

        public string SelectedFile { get; private set; }
        public int Offset { get; set; }
        public string OutputText { get; private set; } = "Click 'Generate Output'";

        public string RawTextSequence { get; set; }
            = "<Outer>" + Environment.NewLine +
               "    <Inner>" + Environment.NewLine +
               "        {Content}" + Environment.NewLine +
               "    <Inner>" + Environment.NewLine +
               "    <Inner>" + Environment.NewLine +
               "        {ContentTwo}" + Environment.NewLine +
               "    <Inner>" + Environment.NewLine +
               "<Outer>" + Environment.NewLine;

        private void GenerateOutput(string filePath)
        {
            try
            {
                CurrentWorkingStep = 0;

                IWorkbook workbook = GetWorkbookFromFile(filePath);
                ISheet sheet = workbook.GetSheetAt(0);

                WorkingSteps = ReplacementViewModels.Count * sheet.LastRowNum;
                OutputText = string.Empty;

                for (int rowIndex = Offset; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null)
                        continue;

                    string textSequence = RawTextSequence;
                    foreach (var replacement in ReplacementViewModels)
                    {
                        textSequence = ReplaceTextByReplacementViewModel(replacement, row, textSequence);
                        CurrentWorkingStep++;
                    }

                    OutputText += textSequence;
                }
                CurrentWorkingStep = WorkingSteps;
            }
            catch (Exception ex)
            {
                OutputText = "Failed with: " + ex.Message;
            }
        }

        private IWorkbook GetWorkbookFromFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            if (extension.Equals(XlsxFileExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    return new XSSFWorkbook(file);
            }
            else if (extension.Equals(XlsFileExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    return new NPOI.HSSF.UserModel.HSSFWorkbook(file);
            }
            else
                throw new NotSupportedException($"*.{extension} files are not supported.");
        }

        private string ReplaceTextByReplacementViewModel(ReplacementViewModel replacement, IRow row, string textSequence)
        {
            var cell = row.GetCell(replacement.ColumnIndex);
            string replacementText = string.Empty;

            if (cell?.CellType == CellType.Numeric)
                replacementText = cell.NumericCellValue.ToString(CultureInfo.InvariantCulture);

            if (cell?.CellType == CellType.String)
                replacementText = cell.StringCellValue.Split(replacement.SplitOn)[0];

            if (cell != null && cell.CellType != CellType.Blank && cell.CellType != CellType.Numeric && cell.CellType != CellType.String)
                throw new NotSupportedException("Only numeric and string cell types supported.");

            return textSequence.Replace(replacement.ReplacementKey, replacementText);
        }

        private void SelectFile()
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "XLS|*.xls";
            fileDialog.Filter += "|XLSX|*.xlsx";

            if (fileDialog.ShowDialog() == true)
            {
                SelectedFile = Path.GetFileName(fileDialog.FileName);
                _selecteFilePath = fileDialog.FileName;
            }
        }
    }
}