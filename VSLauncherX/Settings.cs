using Microsoft.Win32;

namespace VSLauncher.Properties
{


    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings
    {

        public Settings()
        {
            // // To add event handlers for saving and changing settings, uncomment the lines below:
            //
            // this.SettingChanging += this.SettingChangingEventHandler;
            //
            // this.SettingsSaving += this.SettingsSavingEventHandler;
            //
        }

        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e)
        {
            // Add code to handle the SettingChangingEvent event here.
        }

        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Add code to handle the SettingsSaving event here.
        }

		[global::System.Configuration.UserScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("False")]
		public bool DarkMode
		{
			get
			{
        return ((bool)(this["DarkMode"] ?? IsSystemInDarkMode()));
			}
			set
			{
				this["DarkMode"] = value;
			}
		}

		[global::System.Configuration.UserScopedSettingAttribute()]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		[global::System.Configuration.DefaultSettingValueAttribute("")]
		public System.Windows.Forms.FormWindowState AppWindow
		{
			get
			{
				return ((System.Windows.Forms.FormWindowState)(this["AppWindow"]));
			}
			set
			{
				this["AppWindow"] = value;
			}
		}

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public System.Windows.Forms.FormWindowState AppWindow
        {
            get
            {
                return ((System.Windows.Forms.FormWindowState)(this["AppWindow"]));
            }
            set
            {
                this["AppWindow"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public Point AppLocation
        {
            get
            {
                return ((Point)(this["AppLocation"]));
            }
            set
            {
                this["AppLocation"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public Size AppSize
        {
            get
            {
                return ((Size)(this["AppSize"]));
            }
            set
            {
                this["AppSize"] = value;
            }
        }

        public static bool IsSystemInDarkMode()
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    object registryValueObject = key.GetValue("AppsUseLightTheme");
                    if (registryValueObject != null)
                    {
                        int registryValue = (int)registryValueObject;
                        // If value is 0, system is in dark mode; if 1, light mode
                        return registryValue == 0;
                    }
                }
            }
            // Default to light mode if key is missing
            return false;
        }
    }
}
