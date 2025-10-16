using AgOpenGPS.Core.Interfaces;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Core.Streamers;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Input;

namespace AgOpenGPS.Core.ViewModels
{
    public class CreateFromExistingFieldViewModel : FieldTableViewModel
    {
        private readonly ISelectFieldPanelPresenter _selectFieldPanelPresenter;
        private string _newFieldName = string.Empty;

        public CreateFromExistingFieldViewModel(
            ApplicationModel appModel,
            FieldDescriptionStreamer fieldDescriptionStreamer,
            FieldStreamer fieldStreamer,
            ISelectFieldPanelPresenter selectFieldPanelPresenter
        )
            : base(appModel, fieldDescriptionStreamer, fieldStreamer)
        {
            _selectFieldPanelPresenter = selectFieldPanelPresenter;
            AddVehicleCommand = new RelayCommand(AddVehicle);
            AddDateCommand = new RelayCommand(AddDate);
            AddTimeCommand = new RelayCommand(AddTime);
            BackSpaceCommand = new RelayCommand(BackSpace);
        }

        public new FieldDescriptionViewModel LocalSelectedField
        {
            get { return _localSelectedField; }
            set
            {
                if (value != _localSelectedField)
                {
                    _localSelectedField = value;
                    NotifyPropertyChanged();
                    NewFieldName = _localSelectedField?.FieldName ?? string.Empty;
                }
            }
        }

        public string NewFieldName
        {
            get { return _newFieldName; }
            set
            {
                if (value != _newFieldName)
                {
                    _newFieldName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public ICommand AddVehicleCommand { get; }
        public ICommand AddDateCommand { get; }
        public ICommand AddTimeCommand { get; }
        public ICommand BackSpaceCommand { get; }

        public bool MustCopyFlags { get; set; }
        public bool MustCopyMapping { get; set; }
        public bool MustCopyHeadland { get; set; }
        public bool MustCopyLines { get; set; }

        private void AddVehicle()
        {
            NewFieldName += " " + "Vehicle"; // TODO RegistrySettings.vehicleFileName;
        }

        private void AddDate()
        {
            NewFieldName += " " + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private void AddTime()
        {
            NewFieldName += " " + DateTime.Now.ToString("HH-mm", CultureInfo.InvariantCulture);
        }

        private void BackSpace()
        {
            if (NewFieldName.Length > 0) NewFieldName = NewFieldName.Remove(NewFieldName.Length - 1);
        }

        protected override void SelectField()
        {
            var templateField = LocalSelectedField;
            string requestedName = NewFieldName?.Trim() ?? string.Empty;

            if (templateField == null || string.IsNullOrWhiteSpace(requestedName))
            {
                return;
            }

            _selectFieldPanelPresenter.CloseCreateFromExistingFieldDialog();

            try
            {
                string sanitizedName = SanitizeFieldName(requestedName);
                if (string.IsNullOrEmpty(sanitizedName))
                {
                    sanitizedName = SanitizeFieldName(templateField.FieldName);
                }

                DirectoryInfo targetDirectory = CreateUniqueTargetDirectory(sanitizedName);
                NewFieldName = targetDirectory.Name;

                CloneField(templateField.DirectoryInfo, targetDirectory);

                _appModel.Fields.OpenField(targetDirectory);
                if (_appModel.Fields.ActiveField != null)
                {
                    _fieldStreamer.ReadFlagList(_appModel.Fields.ActiveField);
                }

                LocalSelectedField = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create field from template: {ex}");
            }
        }

        private DirectoryInfo CreateUniqueTargetDirectory(string baseName)
        {
            string candidateName = baseName;
            int suffix = 1;

            while (true)
            {
                var directory = new DirectoryInfo(Path.Combine(_appModel.FieldsDirectory.FullName, candidateName));
                directory.Refresh();
                if (!directory.Exists)
                {
                    directory.Create();
                    directory.Refresh();
                    return directory;
                }

                candidateName = $"{baseName}-{suffix++}";
            }
        }

        private void CloneField(DirectoryInfo templateDirectory, DirectoryInfo targetDirectory)
        {
            CreateFieldFile(templateDirectory, targetDirectory);

            CopyIfExists(templateDirectory, targetDirectory, "BackPic.txt");
            CopyIfExists(templateDirectory, targetDirectory, "BackPic.png");
            CopyIfExists(templateDirectory, targetDirectory, "Boundary.txt");
            CopyIfExists(templateDirectory, targetDirectory, "Elevation.txt");

            if (!CopyIfExists(templateDirectory, targetDirectory, "Headlines.txt"))
            {
                WriteLines(targetDirectory, "Headlines.txt", "$Headlines");
            }

            if (MustCopyMapping)
            {
                CopyIfExists(templateDirectory, targetDirectory, "Contour.txt");
                CopyIfExists(templateDirectory, targetDirectory, "Sections.txt");
            }
            else
            {
                WriteLines(targetDirectory, "Sections.txt");
                WriteLines(targetDirectory, "Contour.txt", "$Contour");
            }

            if (MustCopyFlags)
            {
                CopyIfExists(templateDirectory, targetDirectory, "Flags.txt");
            }
            else
            {
                WriteLines(targetDirectory, "Flags.txt", "$Flags", "0");
            }

            if (MustCopyLines)
            {
                CopyIfExists(templateDirectory, targetDirectory, "ABLines.txt");
                CopyIfExists(templateDirectory, targetDirectory, "RecPath.txt");
                CopyIfExists(templateDirectory, targetDirectory, "CurveLines.txt");
                CopyIfExists(templateDirectory, targetDirectory, "Tram.txt");
                CopyIfExists(templateDirectory, targetDirectory, "TrackLines.txt");
            }
            else
            {
                WriteLines(targetDirectory, "RecPath.txt", "$RecPath", "0");
            }

            if (MustCopyHeadland)
            {
                CopyIfExists(templateDirectory, targetDirectory, "Headland.txt");
            }
        }

        private void CreateFieldFile(DirectoryInfo templateDirectory, DirectoryInfo targetDirectory)
        {
            string templatePath = Path.Combine(templateDirectory.FullName, "Field.txt");
            string offsets = "0,0,0";
            string convergence = "0";
            string startFix = "0,0,0";

            if (File.Exists(templatePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(templatePath);
                    offsets = ExtractSection(lines, "$Offsets") ?? offsets;
                    convergence = ExtractSection(lines, "$Convergence") ?? convergence;
                    startFix = ExtractSection(lines, "StartFix") ?? startFix;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unable to read template field file: {ex}");
                }
            }

            string destinationPath = Path.Combine(targetDirectory.FullName, "Field.txt");
            using StreamWriter writer = new StreamWriter(destinationPath, false);
            writer.WriteLine(DateTime.Now.ToString("yyyy-MMMM-dd hh:mm:ss tt", CultureInfo.InvariantCulture));
            writer.WriteLine("$FieldDir");
            writer.WriteLine("FromExisting");
            writer.WriteLine("$Offsets");
            writer.WriteLine(offsets);
            writer.WriteLine("$Convergence");
            writer.WriteLine(convergence);
            writer.WriteLine("StartFix");
            writer.WriteLine(startFix);
        }

        private static string? ExtractSection(string[] lines, string token)
        {
            for (int index = 0; index < lines.Length - 1; index++)
            {
                if (string.Equals(lines[index], token, StringComparison.OrdinalIgnoreCase))
                {
                    return lines[index + 1];
                }
            }

            return null;
        }

        private static void WriteLines(DirectoryInfo directory, string fileName, params string[] lines)
        {
            string path = Path.Combine(directory.FullName, fileName);
            using StreamWriter writer = new StreamWriter(path, false);
            if (lines.Length == 0)
            {
                return;
            }

            foreach (string line in lines)
            {
                writer.WriteLine(line);
            }
        }

        private static bool CopyIfExists(DirectoryInfo templateDirectory, DirectoryInfo targetDirectory, string fileName)
        {
            string sourcePath = Path.Combine(templateDirectory.FullName, fileName);
            if (!File.Exists(sourcePath))
            {
                return false;
            }

            string destinationPath = Path.Combine(targetDirectory.FullName, fileName);
            File.Copy(sourcePath, destinationPath, true);
            return true;
        }

        private static string SanitizeFieldName(string fieldName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder();

            foreach (char character in fieldName.Trim())
            {
                builder.Append(Array.IndexOf(invalidChars, character) >= 0 ? '_' : character);
            }

            return builder.ToString().Trim('_', ' ');
        }
    }
}
