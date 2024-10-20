using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Wipeseals
{

    public class BuildUnityPackage
    {
        [MenuItem("Assets/Wipeseals/Build UdonKnuckelbonesSupportUdonChips Package")]
        public static void BuildPackage()
        {
            string packagePath = "Assets/UdonKnucklebonesSupportUdonChips";
            string exportPath = Path.GetFullPath("Packages/me.wipeseals.udon-knucklebones/Runtime/UnityPackages/UdonKnucklebonesSupportUdonChips.unitypackage");

            Debug.Log("Building package from " + packagePath + " to " + exportPath);

            if (Directory.Exists(packagePath))
            {
                AssetDatabase.ExportPackage(packagePath, exportPath, ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
                Debug.Log("Package built successfully at " + exportPath);
            }
            else
            {
                Debug.LogError("Directory does not exist: " + packagePath);
            }
        }
    }
}