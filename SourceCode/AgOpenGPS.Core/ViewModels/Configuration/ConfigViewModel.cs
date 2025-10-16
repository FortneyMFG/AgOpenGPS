namespace AgOpenGPS.Core.ViewModels
{
    public class ConfigViewModel : DayNightAndUnitsViewModel
    {
        private readonly ApplicationModel _appModel;

        public ConfigViewModel(ApplicationModel appModel)
        {
            _appModel = appModel;
        }

        public void UpdateFromSettings()
        {
            if (_appModel.Settings != null)
            {
                IsMetric = _appModel.Settings.IsMetric;
                IsDay = _appModel.Settings.IsDay;
            }
        }

    }
}
