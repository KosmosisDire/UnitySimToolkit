using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Codice.Client.Common;
using Microsoft.Win32;
using NUnit.Framework.Internal;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Xacro2URDF
{
    public static void ConvertToURDF(string inputPath, string outputPath)
    {
        if (inputPath.StartsWith("Assets/"))
        {
            inputPath = Path.Combine(Directory.GetCurrentDirectory(), inputPath);
        }
        inputPath = inputPath.Replace("\\", "/");

        if (!File.Exists(inputPath))
        {
            Debug.LogError("xacro file not found");
            return;
        }

        if (outputPath.StartsWith("Assets/"))
        {
            outputPath = Path.Combine(Directory.GetCurrentDirectory(), outputPath);
        }
        outputPath = outputPath.Replace("\\", "/");

        var originalDir = Directory.GetCurrentDirectory();

        var xacroPython = Path.Combine(Application.dataPath, "../Packages/com.kosmosisdire.simtoolkit/Runtime/Resources/ROS/xacro.py");
        xacroPython = xacroPython.Replace("\\", "/");
        if (!File.Exists(xacroPython))
        {
            Debug.LogError("xacro.py not found at " + xacroPython);
            return;
        }

        var workingDir = PathHelper.GetParentPath(PathHelper.GetParentPath(inputPath));
        workingDir = workingDir.Replace("\\", "/");
        if (!Directory.Exists(workingDir))
        {
            Debug.LogError("Working directory not found");
            return;
        }
        Debug.Log(workingDir);

        Directory.SetCurrentDirectory(workingDir);

        try 
        {
            if (!Directory.Exists("./urdf"))
            {
                Debug.LogError("urdf folder not found");
                Directory.SetCurrentDirectory(originalDir);
                return;
            }

            // copy xacro.py beside the xacro file
            var pythonName = Path.GetFileName(xacroPython);
            Debug.Log(pythonName);
            File.Copy(xacroPython, pythonName, true);
            // Debug.Log("Copied xacro.py");

            // replace radians in xacro file
            var tempPath = "./" + Path.GetFileName(inputPath).Replace(".xacro", "_temp.xacro");
            Debug.Log(tempPath);
            ReplaceRadiansInFile(inputPath, tempPath);
            Debug.Log("Replaced radians");

            // replace package name with a valid path
            FixPackageInFile(tempPath, tempPath);
            Debug.Log("Fixed package name");

            // find references to other xacro files
            var includes = HandleXacroIncludes(tempPath);
            Debug.Log(string.Join(", ", includes));

            // convert files
            var relativeOutputPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), outputPath);
            Debug.Log(relativeOutputPath);
            var relativeInputPath = tempPath;
            var args = $"-o {relativeOutputPath} {relativeInputPath}";
            RunPythonScript(pythonName, args);
            // Debug.Log("Converted xacro to urdf");

            // delete xacro.py
            File.Delete(pythonName);
            File.Delete(pythonName + ".meta");
            Debug.Log("Deleted xacro.py");

            // delete temp xacro file
            File.Delete(tempPath);
            File.Delete(tempPath + ".meta");
            Debug.Log("Deleted temp xacro file");

            // delete xacro_includes folder
            var includesFolder = "./xacro_includes";
            if (Directory.Exists(includesFolder))
            {
                Directory.Delete(includesFolder, true);
                File.Delete(includesFolder + ".meta");
            }

            // check if the urdf file is empty
            if (new FileInfo(relativeOutputPath).Length == 0)
            {
                Debug.LogError("Failed to convert xacro to urdf");
            }

        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        // set working directory back to the original
        Directory.SetCurrentDirectory(originalDir);
    }

    [MenuItem("Assets/Convert Xacro to URDF")]
    private static void Convert()
    {
        // only on windows
        if (Application.platform != RuntimePlatform.WindowsEditor)
        {
            Debug.LogError("This feature is only available on Windows");
            return;
        }

        // need python
        var pythonPath = FindPythonPath();
        if (string.IsNullOrEmpty(pythonPath.Trim()))
        {
            EditorUtility.DisplayDialog("Python not found!", "Please install python 3.10.0 or higher and add python to your PATH", "OK");
            return;
        }

        var originalPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        var outputDir = PathHelper.GetParentPath(PathHelper.GetParentPath(originalPath));
        var outputPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(originalPath) + ".urdf");
        ConvertToURDF(originalPath, outputPath);
    }

    [MenuItem("Assets/Convert Xacro to URDF", true)]
    private static bool ValidateConvert()
    {
        return Selection.activeObject != null && AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".xacro");
    }



    [MenuItem("Assets/Repair URDF")]
    private static async void RepairURDF()
    {
        var urdfPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        var urdfFullPath = PathHelper.GetParentPath(Application.dataPath) + "/" + urdfPath;
        if (!File.Exists(urdfFullPath))
        {
            Debug.LogError("urdf file not found");
            return;
        }

        var originalDir = Directory.GetCurrentDirectory();
        var workingDir = PathHelper.GetParentPath(urdfFullPath);
        Directory.SetCurrentDirectory(workingDir);

        try
        {
            FixPackageInFile(urdfFullPath, urdfFullPath);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        Directory.SetCurrentDirectory(originalDir);
    }

    [MenuItem("Assets/Repair URDF", true)]
    private static bool ValidateRepair()
    {
        return Selection.activeObject != null && AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".urdf");
    }

    private static void RunPythonScript(string pythonFilename, string args)
    {
        var pythonPath = FindPythonPath();
        if (string.IsNullOrEmpty(pythonPath.Trim()))
        {
            Debug.LogError("Python not found, please install python 3");
            return;
        }

        pythonPath = Path.Combine(pythonPath, "python.exe");

        Debug.Log($"Running python script {pythonFilename} with args {args}");
        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $" {pythonFilename} {args}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };

        using(Process process = Process.Start(start))
        {
            process.OutputDataReceived += (sender, e) => Debug.Log(e.Data);
            process.BeginOutputReadLine();
            process.WaitForExit();
        }

    }


    private static Dictionary<string, string> GetPropertyValues(string filePath)
    {
        var text = File.ReadAllText(filePath);
        var properties = new Dictionary<string, string>();
        var propertyPattern = @"<xacro:property\s*?name\s*?=\s*?""([\w_\-]+?)""\s*?value\s*?=\s*?""([\w\W]+?)""\s*?/>";
        var propertyMatches = Regex.Matches(text, propertyPattern);
        foreach (Match match in propertyMatches)
        {
            // property name -> value
            properties.Add(match.Groups[1].Value, match.Groups[2].Value);
        }

        return properties;
    }

    public static string[] HandleXacroIncludes(string filePath)
    {
        var text = File.ReadAllText(filePath);

        // remove all <xacro:material> tags
        string materialPattern = @"<xacro:material[\w\W]+?\s*?/>";
        text = Regex.Replace(text, materialPattern, "");

        string pattern = @"<xacro:include\s*?filename=""(\$\(find ([\w\W]+?)\)([\w\W]+?))""\s*?/>";
        var includeMatches = Regex.Matches(text, pattern);
        var includePaths = includeMatches.Select(m => m.Groups[2].Value + m.Groups[3].Value).ToArray();
        var workingDir = Directory.GetCurrentDirectory().Replace("\\", "/");

        var successfulPaths = new List<string>();
        var successfulMatches = new List<string>();

        // search backwards until each path is found or we reach the assets folder
        var assetsPath = Application.dataPath.Replace("\\", "/");
        int depth = 10;
        while (true)
        {
            if (includePaths.Length == 0 || workingDir == assetsPath || depth == 0)
            {
                break;
            }

            var includeCopy = includePaths.ToArray();
            // Debug.Log("working dir: " + workingDir);
            int i = 0;
            foreach (var include in includeCopy)
            {
                // Debug.Log("include: " + include);
                var includePath = Path.Combine(workingDir, include).Replace("\\", "/");
                if (File.Exists(includePath))
                {
                    successfulPaths.Add(includePath);
                    successfulMatches.Add(includeMatches[i].Groups[1].Value);
                    includePaths = includePaths.Where(e => e != include).ToArray();
                }
                i++;
            }

            workingDir = PathHelper.GetParentPath(workingDir).Replace("\\", "/");
            depth--;
        }

        workingDir = Directory.GetCurrentDirectory().Replace("\\", "/");

        // copy the succesful paths to a new folder in the working directory
        var newFolder = Path.Combine(workingDir, "xacro_includes");
        if (!Directory.Exists(newFolder))
        {
            Directory.CreateDirectory(newFolder);
        }

        // get property values that can be filled in
        var properties = new Dictionary<string, string>();
        var propertyPattern = @"""(\$\{([\w_\-]+?)\})""";
        var propertyMatches = Regex.Matches(text, propertyPattern);
        foreach (Match match in propertyMatches)
        {
            // property name -> full match
            if (!properties.ContainsKey(match.Groups[2].Value))
                properties.Add(match.Groups[2].Value, match.Groups[1].Value);
            else
                properties[match.Groups[2].Value] = match.Groups[1].Value;
        }

        for (int i = 0; i < successfulPaths.Count; i++)
        {
            var newPath = Path.Combine(newFolder, Path.GetFileName(successfulPaths[i]));
            Debug.Log("copying " + successfulPaths[i] + " to " + newPath);
            File.Copy(successfulPaths[i], newPath, true);

            // process the new files like the original
            ReplaceRadiansInFile(newPath, newPath);
            FixPackageInFile(newPath, newPath);
            HandleXacroIncludes(newPath);

            // replace properties in this file with property values
            var externalProperties = GetPropertyValues(newPath);
            foreach (var property in externalProperties)
            {
                if (properties.ContainsKey(property.Key))
                {
                    var fullPropMatch = properties[property.Key];
                    var propValue = property.Value;
                    text = text.Replace(fullPropMatch, propValue);
                }
            }

            successfulPaths[i] = newPath;
        }

        var relativePaths = successfulPaths.Select(p => Path.GetRelativePath(Directory.GetCurrentDirectory(), p)).ToArray();

        // replace successful matches with the relative paths
        for (int i = 0; i < successfulMatches.Count; i++)
        {
            text = text.Replace(successfulMatches[i], relativePaths[i]);
        }

        File.WriteAllText(filePath, text);

        return relativePaths.ToArray();
    }

    public static void ReplaceRadiansInFile(string filePath, string outputPath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        string text = File.ReadAllText(filePath);
        string replacedText = ReplaceRadians(text);
        File.WriteAllText(outputPath, replacedText);
    }

    private static string ReplaceRadians(string text)
    {
        string pattern = @"radians\((-?\d+(?:\.\d+)?)\)";
        return Regex.Replace(text, pattern, delegate (Match match)
        {
            float degrees = float.Parse(match.Groups[1].Value);
            double radians = Mathf.Deg2Rad * degrees;
            return radians.ToString();
        });
    }

    private static string ReplaceWithValidPackage(string text)
    {
        string pattern = @"package:[/\\]+([\w\W]+?)""";
        return Regex.Replace(text, pattern, delegate (Match match)
        {
            var package = match.Groups[1].Value.Replace("\\", "/");
            // Debug.Log("package: " + package);
            var workingParent = Directory.GetCurrentDirectory().Replace("\\", "/");
            // Debug.Log("working parent: " + workingParent);
            var packagePath = Path.Combine(workingParent, package);
            // Debug.Log("package path: " + packagePath);

            var lastSuccess = package;
            // loop up the directory tree one at a time until the package is found
            while (true)
            {
                if (package.Split('/').Length == 1)
                {
                    break;
                }

                // remove the first directory from the package path
                package = package.Substring(package.Split('/').First().Length + 1).Replace("\\", "/");
                packagePath = Path.Combine(workingParent, package).Replace("\\", "/");
                if (File.Exists(packagePath))
                {
                    lastSuccess = package;
                    break;
                }
                // Debug.Log("try package path: " + packagePath);
            }
            
            // add working parent directory name on the front
            lastSuccess = "package://" + lastSuccess.Replace("\\", "/") + "\"";
            // Debug.Log("last success: " + lastSuccess);

            return lastSuccess;
        });
    }

    private static void FixPackageInFile(string filePath, string outputPath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        string text = File.ReadAllText(filePath);
        // Debug.Log("read text: " + filePath);
        string replacedText = ReplaceWithValidPackage(text);
        File.WriteAllText(outputPath, replacedText);
    }
    
    public static string FindPythonPath()
    {
        using (var hiveKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
        using (var subkey = hiveKey?.OpenSubKey(@"SOFTWARE\Python\PythonCore"))
        {
            var subkeys = subkey?.GetSubKeyNames();
            if (subkeys.Length == 0) return string.Empty;

            var key = hiveKey.OpenSubKey(@"SOFTWARE\Python\PythonCore\"+subkeys.Last());
            key = key.OpenSubKey("InstallPath");
            return Path.GetDirectoryName((string) key.GetValue("ExecutablePath", string.Empty));
        }
    }
}
