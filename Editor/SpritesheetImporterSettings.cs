#if UNITY_2019_2_OR_NEWER
#define SECONDARY_TEXTURES_AVAILABLE
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace SpritesheetImporter {

    public enum LogLevel {
        Verbose = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    public enum CustomPivotMode {
        Tilemap
    }

    internal static class LogLevelExtensions {
        internal static bool Includes(this LogLevel level, LogLevel other) {
            return other >= level;
        }
    }

    internal static class SpritesheetImporterSettings {

        private const string animationSubfolderNameFormatTooltip =
            "Format string when creating subfolders.\n\n"
          + "• The string '{anim}' will be replaced by the animation name.\n"
          + "• The string '{obj}' will be replaced by the object name.";

        [UserSetting("General", "Animation Subfolder Name Format", animationSubfolderNameFormatTooltip)]
        public static readonly UserSetting<string> animationSubfolderNameFormat = new UserSetting<string>(SpritesheetImporterSettingsManager.Instance, "animationSubfolderNameFormat", "Animation - {anim}", SettingsScope.Project);

        private const string formatFileNamesTooltip =
            "Whether to apply some simple formatting to file names for sprites, animations, and directories, such as removing spaces.";

        [UserSetting("General", "Format Asset File Names", formatFileNamesTooltip)]
        public static readonly UserSetting<bool> formatFileNames = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "formatFileNames", true, SettingsScope.Project);

        private const string logLevelTooltip =
            "How much logging to output to the Console window. Each level includes the levels below it.\n\n"
          + "• Verbose - Way too much. Should only be used if you're trying to figure out an issue with the importer.\n"
          + "• Info - Informative messages that let you know when import is taking place, without being overwhelming.\n"
          + "• Warning - Warnings occur if something unusual is noticed during import, but it doesn't prevent import. This is the recommended log level.\n"
          + "• Error - Only prints errors that occur in import, which will need to be addressed.\n";

        [UserSetting("Debug", "Log Level", logLevelTooltip)]
        public static readonly UserSetting<LogLevel> logLevel = new UserSetting<LogLevel>(SpritesheetImporterSettingsManager.Instance, "logLevel", LogLevel.Warning, SettingsScope.Project);
    }

}