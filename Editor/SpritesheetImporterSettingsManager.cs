using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SettingsManagement;
using UnityEditor;

namespace SpritesheetImporter {
    internal static class SpritesheetImporterSettingsManager {
        private const string packageName = "com.chrishayesmu.spritesheet-importer";

        private static Settings _instance;

        internal static Settings Instance {
            get {
                if (_instance == null) {
                    _instance = new Settings(packageName);
                }

                return _instance;
            }
        }

        [SettingsProvider]
        static SettingsProvider CreateSettingsProvider() {
            return new UserSettingsProvider(
                "Project/Spritesheet Importer",
                Instance,
                new[] { typeof(SpritesheetImporterSettingsManager).Assembly },
                SettingsScope.Project
            );
        }
    }
}