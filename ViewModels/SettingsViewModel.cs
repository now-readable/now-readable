using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;

using NowReadable.Utilities;
using NowReadable.Utilities.Commands;

namespace NowReadable.ViewModels
{
    public class Theme
    {
        public Theme(string foregroundColor, string backgroundColor, string name) 
        {
            ForegroundColor = foregroundColor;
            BackgroundColor = backgroundColor;
            Name = name;
        }

        public string ForegroundColor {get; set; }
        public string BackgroundColor {get; set; }
        public string Name { get; set; }
    }

    public class SettingsViewModel
    {
        public SettingsViewModel()
        {
            Typefaces = new List<string>();
            Typefaces.Add("Arial");
            Typefaces.Add("Cambria");
            Typefaces.Add("Courier New");
            Typefaces.Add("Georgia");
            Typefaces.Add("Segoe UI");
            Typefaces.Add("Tahoma");
            Typefaces.Add("Verdana");

            Themes = new ObservableCollection<Theme>();
            Themes.Add(new Theme("#d9d9d9", "#1c1b1d", "slate gray"));
            Themes.Add(new Theme("#1c1b1d", "#d9d9d9", "reverse slate"));
            Themes.Add(new Theme("#657b83", "#002b36", "solarized dark"));
            Themes.Add(new Theme("#839496", "#fdf6e3", "solarized light"));
            Themes.Add(new Theme("#ffffff", "#000000", "classic"));
            Themes.Add(new Theme("#000000", "#ffffff", "traditional"));

            LoadData();
        }
        
        private int _currentFontSize = 10;
        public int CurrentFontSize
        {
            get
            {
                return _currentFontSize;
            }
            set
            {
                //Coerce the values within our expected 10-50 range.
                if (value >= 10 && value <= 50)
                {
                    _currentFontSize = value;
                }
            }
        }

        private int _currentTypeface = 2;
        public int CurrentTypeface
        {
            get
            {
                return _currentTypeface;
            }
            set
            {
                _currentTypeface = value;
            }
        }

        private int _currentTheme = 0;
        public int CurrentTheme
        {
            get
            {
                return _currentTheme;
            }
            set
            {
                _currentTheme = value;
            }
        }

        private bool _autoSync = false;
        public bool AutoSync
        {
            get
            {
                return _autoSync;
            }
            set
            {
                _autoSync = value;
            }
        }

        public bool IsDataLoaded { get; private set; }

        public List<string> Typefaces { get; private set; }

        public ObservableCollection<Theme> Themes { get; set; }
     
        /// <summary>
        /// Saves the current settings.
        /// </summary>
        private SimpleCommand _saveCommand;
        public SimpleCommand SaveCommand {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new SimpleCommand((object parameter) =>
                    {
                        SaveCommand.IsEnabled = false;
                        IsolatedStorageSettings isss = IsolatedStorageSettings.ApplicationSettings;
                        isss.FuckingAdd("currenttypeface", CurrentTypeface);
                        isss.FuckingAdd("currentfontsize", CurrentFontSize);
                        isss.FuckingAdd("currenttheme", CurrentTheme);
                        isss.FuckingAdd("autosync", AutoSync);
                        isss.Save();

                        SaveCompleted(this, new SaveEventArgs(true));
                        IsUpdated = true;
                        SaveCommand.IsEnabled = true;
                    });
                }
                return _saveCommand;
            }
        }

        /// <summary>
        /// Load the current settings.
        /// </summary>
        public void LoadData()
        {
            IsolatedStorageSettings isss = IsolatedStorageSettings.ApplicationSettings;
            if (isss.Contains("currenttypeface"))
            {
                isss.TryGetValue<int>("currenttypeface", out _currentTypeface);
            }
            if (isss.Contains("currentfontsize"))
            {
                isss.TryGetValue<int>("currentfontsize", out _currentFontSize);
            }
            if (isss.Contains("currenttheme"))
            {
                isss.TryGetValue<int>("currenttheme", out _currentTheme);
            }
            if (isss.Contains("autosync"))
            {
                isss.TryGetValue<bool>("autosync", out _autoSync);
            }
            this.IsDataLoaded = true;
        }
    
        public event EventHandler<SaveEventArgs> SaveCompleted;

        public bool IsUpdated { get; set; }
    }
    public class SaveEventArgs : EventArgs
    {
        public SaveEventArgs(bool success)
        {
            Success = success;
        }
        public bool Success { get; set; }
    }
}