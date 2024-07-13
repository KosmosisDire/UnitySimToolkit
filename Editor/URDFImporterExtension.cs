using System.IO;
using System.Linq;
using Codice.Client.Common;
using UnityEditor;
using UnityEngine;
using UrdfToolkit;

[DefaultExecutionOrder(-100000)]
public class URDFImporterExtension
{
    [MenuItem("Assets/Import Robot from this .urdf", true)]
    public static bool ImportURDFValid()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        return Path.GetExtension(assetPath)?.ToLower() == ".urdf";
    }

    [MenuItem("Assets/Import Robot from this .urdf")]
    public static async void ImportURDF()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (Path.GetExtension(assetPath)?.ToLower() == ".urdf")
        {
            if (assetPath != "")
            {
                await URDFBuilder.Build(assetPath);
            }
        }
        else
        {
            EditorUtility.DisplayDialog("URDF Import", "The file you selected was not a URDF file. Please select a valid URDF file.", "Ok");
        }
    }

    [MenuItem("Assets/Import Robot from this .xacro", true)]
    public static bool ImportXACROValid()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        return Path.GetExtension(assetPath)?.ToLower() == ".xacro";
    }

    [MenuItem("Assets/Import Robot from this .xacro")]
    public static async void ImportXACRO()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        var outputDir = PathHelper.GetParentPath(PathHelper.GetParentPath(assetPath));
        var outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(assetPath) + ".urdf");
        Xacro2URDF.ConvertToURDF(assetPath, outputPath);
        await URDFBuilder.Build(outputPath);

        File.Delete(outputPath);
    }

    [MenuItem("Assets/Clean URDF Assets", true)]
    private static bool CleanAssetsCheck()
    {
        var projectFolder = Directory.GetCurrentDirectory();
        var path = Path.Combine(projectFolder, AssetDatabase.GetAssetPath(Selection.activeObject));
        if (File.Exists(path))
        {
            return false;
        }
        var urdfFolder = Path.Combine(path, "urdf");
        var meshesFolder = Path.Combine(path, "meshes");
        var materialsFolder = Path.Combine(path, "Materials");
        var urdfFile = Directory.GetFiles(path, "*.urdf").FirstOrDefault();
        
        if (!Directory.Exists(urdfFolder) && 
            !Directory.Exists(meshesFolder) && 
            (!Directory.Exists(materialsFolder) || 
            string.IsNullOrEmpty(urdfFile)))
        {
            return false;
        }

        return true;
    }

    [MenuItem("Assets/Clean URDF Assets")]
    private static void CleanAssets()
    {
        var projectFolder = PathHelper.GetParentPath(Application.dataPath);
        var path = Path.Combine(projectFolder, AssetDatabase.GetAssetPath(Selection.activeObject));

        if (!CleanAssetsCheck())
        {
            return;
        }

        // recursive search and delete all .prefab and .asset files
        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            if (file.EndsWith(".prefab") || file.EndsWith(".asset"))
            {
                File.Delete(file);
                File.Delete(file + ".meta");
            }
        }

        // delete Materials folder
        var materialsFolder = Path.Combine(path, "Materials");
        if (Directory.Exists(materialsFolder))
        {
            Directory.Delete(materialsFolder, true);
            File.Delete(materialsFolder + ".meta");
        }
    }
    

    [MenuItem("Assets/Parse URDF")]
    private static void ParseURDFValid()
    {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (Path.GetExtension(assetPath)?.ToLower() == ".urdf")
        {
            if (assetPath != "")
            {
                var urdf = URDF.Parse(assetPath);
                Debug.Log(urdf.Stringify(2));
            }
        }
        else
        {
            EditorUtility.DisplayDialog("URDF Import", "The file you selected was not a URDF file. Please select a valid URDF file.", "Ok");
        }
    }
}