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

        #region Default import settings
        private const string createAnimationsTooltip =
            "Whether to automatically create animation clips when importing. If you turn this on, you will need to manually trigger a reimport of any assets which were imported while it was off.";

        [UserSetting("Default Import Settings - Animations", "Create Animation Clips", createAnimationsTooltip)]
        public static readonly UserSetting<bool> createAnimations = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "createAnimations", true, SettingsScope.Project);

        private const string placeAnimationsInSubfoldersTooltip =
            "If true, whenever multiple animation clips are created based on the same source animation, a subfolder will be created to hold those animations together. This can "
          + "be helpful to maintain an organized asset hierarchy.\n\n"
          + "For example, if you have an animation 'walk' which is rendered from 8 different angles, then a subfolder named 'walk' will be created and all 8 animations "
          + "will be placed in it. Subfolders are always relative to the directory where the .ssdata and image files are at.\n\n"
          + "No subfolder will be created with only a single animation in it.";

        [UserSetting("Default Import Settings - Animations", "Group Animations In Subfolders", placeAnimationsInSubfoldersTooltip)]
        public static readonly UserSetting<bool> placeAnimationsInSubfolders = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "placeAnimationsInSubfolders", true, SettingsScope.Project);

#if SECONDARY_TEXTURES_AVAILABLE
        private const string sliceSecondaryTexturesTooltip =
            "If true, secondary textures will be sliced into individual sprites in the same way as the main image texture.\n\n"
          + "For most applications, this is unnecessary; when used only as secondary textures, these images do not need to be sliced. "
          + "Slicing is therefore usually avoided for simplicity, and to reduce the total number of assets in the project.\n\n"
          + "Turning this off will not impact existing textures, even if reimported; you will need to change their Sprite Mode to Single manually.";

        [UserSetting("Default Import Settings - Slicing", "Slice Secondary Textures", sliceSecondaryTexturesTooltip)]
        public static readonly UserSetting<bool> sliceSecondaryTextures = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "sliceSecondaryTextures", false, SettingsScope.Project);
#else
        private const string sliceSecondaryTexturesTooltip =
            "If true, secondary textures will be sliced into individual sprites in the same way as the main image texture.\n\n"
          + "Your Unity version does not have built-in secondary texture support, which was added in 2019.2. Slicing is therefore enabled by default, "
          + "since otherwise the secondary textures will not be processed at all. You will need your own solution for actually using the secondary textures.\n\n"
          + "Turning this off will not impact existing textures, even if reimported; you will need to change their Sprite Mode to Single manually.";

        [UserSetting("Textures", "Slice Secondary Textures", sliceSecondaryTexturesTooltip)]
        public static readonly UserSetting<bool> sliceSecondaryTextures = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "sliceSecondaryTextures", true, SettingsScope.Project);
#endif

        private const string sliceUnidentifiedTexturesTooltip =
            "Whether to slice unidentified textures (textures that aren't marked with a known role, such as albedo, mask, or normal).\n\n"
          + "Since the importer doesn't know what these textures are used for, it can't make an intelligent decision. Given that they're "
          + "part of a spritesheet, they will be sliced by default.";

        [UserSetting("Default Import Settings - Slicing", "Slice Unidentified Textures", sliceUnidentifiedTexturesTooltip)]
        public static readonly UserSetting<bool> sliceUnidentifiedTextures = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "sliceUnidentifiedTextures", true, SettingsScope.Project);

        private const string trimSpritesTooltip = "Whether sprites should be trimmed (have all rows/columns of solely transparent pixels removed from their exteriors) after being sliced.";

        [UserSetting("Default Import Settings - Slicing", "Trim Sprites", trimSpritesTooltip)]
        public static readonly UserSetting<bool> trimSprites = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "trimSprites", true, SettingsScope.Project);

        private const string trimAlphaThresholdTooltip = "When trimming sprites, pixels with an alpha less than or equal to this value will be treated as empty.";

        [UserSetting("Default Import Settings - Slicing", "Trim Alpha Threshold", trimAlphaThresholdTooltip)]
        public static readonly UserSetting<float> trimAlphaThreshold = new UserSetting<float>(SpritesheetImporterSettingsManager.Instance, "trimAlphaThreshold", 0.0f, SettingsScope.Project);

        private const string spritePivotTooltip = "Where the pivot point should be for each sprite.";

        [UserSetting("Default Import Settings - Slicing", "Default Sprite Pivot", spritePivotTooltip)]
        public static readonly UserSetting<SpriteAlignment> spritePivot = new UserSetting<SpriteAlignment>(SpritesheetImporterSettingsManager.Instance, "spritePivot", SpriteAlignment.Center, SettingsScope.Project);

        private const string customPivotModeTooltip = "How the pivot should be determined when the pivot point is set to Custom.\n\n" +
                                                      "• Tilemap - The pivot will be placed in such a way as to make the sprite fit smoothly into a tilemap grid.";

        [UserSetting("Default Import Settings - Slicing", "Custom Pivot Mode", customPivotModeTooltip)]
        public static readonly UserSetting<CustomPivotMode> customPivotMode = new UserSetting<CustomPivotMode>(SpritesheetImporterSettingsManager.Instance, "customPivotMode", CustomPivotMode.Tilemap, SettingsScope.Project);

        private const string tilemapGridSizeTooltip = "The size of the tilemap grid the sprites will be used in. Used to calculate the pivot point.";

        [UserSetting("Default Import Settings - Tilemaps", "Tilemap Grid Size", tilemapGridSizeTooltip)]
        public static readonly UserSetting<Vector2> tilemapGridSize = new UserSetting<Vector2>(SpritesheetImporterSettingsManager.Instance, "tilemapGridSize", Vector2.one, SettingsScope.Project);
        #endregion

        private const string animationSubfolderNameFormatTooltip =
            "Format string when creating subfolders.\n\n"
          + "• The string '{anim}' will be replaced by the animation name.\n"
          + "• The string '{obj}' will be replaced by the object name.";

        [UserSetting("Miscellaneous", "Animation Subfolder Name Format", animationSubfolderNameFormatTooltip)]
        public static readonly UserSetting<string> animationSubfolderNameFormat = new UserSetting<string>(SpritesheetImporterSettingsManager.Instance, "animationSubfolderNameFormat", "Animation - {anim}", SettingsScope.Project);

        private const string formatFileNamesTooltip =
            "Whether to apply some simple formatting to file names for sprites, animations, and directories, such as removing spaces.";

        [UserSetting("Miscellaneous", "Format Asset File Names", formatFileNamesTooltip)]
        public static readonly UserSetting<bool> formatFileNames = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "formatFileNames", true, SettingsScope.Project);

        private const string logLevelTooltip =
            "How much logging to output to the Console window. Each level includes the levels below it.\n\n"
          + "• Verbose - Way too much. Should only be used if you're trying to figure out an issue with the importer.\n"
          + "• Info - Informative messages that let you know when import is taking place, without being overwhelming.\n"
          + "• Warning - Warnings occur if something unusual is noticed during import, but it doesn't prevent import. This is the recommended log level.\n"
          + "• Error - Only prints errors that occur in import, which will need to be addressed.\n";

        [UserSetting("Miscellaneous", "Log Level", logLevelTooltip)]
        public static readonly UserSetting<LogLevel> logLevel = new UserSetting<LogLevel>(SpritesheetImporterSettingsManager.Instance, "logLevel", LogLevel.Warning, SettingsScope.Project);
    }

}