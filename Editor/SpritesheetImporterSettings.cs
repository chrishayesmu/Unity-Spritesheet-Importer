#if UNITY_2019_2_OR_NEWER
#define SECONDARY_TEXTURES_AVAILABLE
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace SpritesheetImporter {

    internal static class SpritesheetImporterSettings {

        private const string createAnimationsTooltip =
            "Whether to automatically create animations when importing. If you turn this on, you will need to manually trigger a reimport of any assets which were imported while it was off.";

        [UserSetting("Animations", "Create Animations", createAnimationsTooltip)]
        public static readonly UserSetting<bool> createAnimations = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "createAnimations", true, SettingsScope.Project);

        private const string placeAnimationsInSubfoldersTooltip =
            "If true, whenever multiple animation clips are created based on the same source animation, a subfolder will be created to hold those animations together. This can "
          + "be helpful to maintain an organized asset hierarchy.\n\n"
          + "For example, if you have an animation 'walk' which is rendered from 8 different angles, then a subfolder named 'walk' will be created and all 8 animations "
          + "will be placed in it. Subfolders are always relative to the directory where the .ssdata and image files are at.\n\n"
          + "No subfolder will be created with only a single animation in it.";

        [UserSetting("Animations", "Group Animations In Subfolders", placeAnimationsInSubfoldersTooltip)]
        public static readonly UserSetting<bool> placeAnimationsInSubfolders = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "placeAnimationsInSubfolders", true, SettingsScope.Project);

        private const string animationSubfolderNameFormatTooltip =
            "Format string when creating subfolders.\n\n"
          + "* The string '{anim}' will be replaced by the animation name.\n"
          + "* The string '{obj}' will be replaced by the object name.";

        [UserSetting("Animations", "Subfolder Name Format", animationSubfolderNameFormatTooltip)]
        public static readonly UserSetting<string> animationSubfolderNameFormat = new UserSetting<string>(SpritesheetImporterSettingsManager.Instance, "animationSubfolderNameFormat", "Animation - {anim}", SettingsScope.Project);

#if SECONDARY_TEXTURES_AVAILABLE
        private const string sliceSecondaryTexturesTooltip =
            "If true, secondary textures will be sliced into individual sprites in the same way as the main image texture.\n\n"
          + "For most applications, this is unnecessary; when used only as secondary textures, these images do not need to be sliced. "
          + "Slicing is therefore usually avoided for simplicity, and to reduce the total number of assets in the project.\n\n"
          + "Turning this off will not impact existing textures, even if reimported; you will need to change their Sprite Mode to Single manually.";

        [UserSetting("Textures", "Slice Secondary Textures", sliceSecondaryTexturesTooltip)]
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

        [UserSetting("Textures", "Slice Unidentified Textures", sliceUnidentifiedTexturesTooltip)]
        public static readonly UserSetting<bool> sliceUnidentifiedTextures = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "sliceUnidentifiedTextures", true, SettingsScope.Project);

        private const string formatFileNamesTooltip =
            "Whether to apply some simple formatting to file names for sprites, animations, and directories, such as removing spaces.";

        [UserSetting("Miscellaneous", "Format File Names", formatFileNamesTooltip)]
        public static readonly UserSetting<bool> formatFileNames = new UserSetting<bool>(SpritesheetImporterSettingsManager.Instance, "formatFileNames", true, SettingsScope.Project);
    }

}