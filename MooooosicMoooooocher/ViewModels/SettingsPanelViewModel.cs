using MooooosicMoooooocher.Models;

namespace MooooosicMoooooocher.ViewModels
{
    public class SettingsPanelViewModel : ViewModelBase
    {
        private bool _suppressChanges;
        private string _outputFolder = string.Empty;
        private string _authToken = string.Empty;
        private AudioFormat _preferredFormat = AudioFormat.MP3;

        public event Action? SettingsChanged;
        public IReadOnlyList<AudioFormat> Formats { get; } = Enum.GetValues<AudioFormat>();

        public string OutputFolder
        {
            get => _outputFolder;
            set
            {
                if (SetProperty(ref _outputFolder, value) && !_suppressChanges)
                {
                    SettingsChanged?.Invoke();
                }
            }
        }

        public string AuthToken
        {
            get => _authToken;
            set
            {
                if (SetProperty(ref _authToken, value) && !_suppressChanges)
                {
                    SettingsChanged?.Invoke();
                }
            }
        }

        public AudioFormat PreferredFormat
        {
            get => _preferredFormat;
            set
            {
                if (SetProperty(ref _preferredFormat, value) && !_suppressChanges)
                {
                    SettingsChanged?.Invoke();
                }
            }
        }

        public void ApplySettings(AppSettings settings)
        {
            _suppressChanges = true;

            OutputFolder = settings.OutputFolder;
            AuthToken = settings.AuthToken;
            PreferredFormat = settings.PreferredFormat;

            _suppressChanges = false;
        }

        public void UpdateSettings(AppSettings settings)
        {
            settings.OutputFolder = OutputFolder;
            settings.AuthToken = AuthToken;
            settings.PreferredFormat = PreferredFormat;
        }
    }
}
