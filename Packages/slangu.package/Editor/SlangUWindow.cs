using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using System;

public class SlangUWindow : EditorWindow
{
    private List<string> entryPointList;

    private int entryPointIndex = 0;

    private string filePath = "";
    private string outputname = "";
    private bool bUseVulkanSDK = false;

    private string kernelname = "";
    private bool bLoadSuccess = false;

    [MenuItem("SlangU/SlangU")]
    private static void SlangU()
    {
        GetWindow<SlangUWindow>("Slang for Unity (compute only)");
    }

    public void Awake()
    {

    }

    private void OnGUI()
    {
        if (GUILayout.Button("Open Slang file"))
        {
            filePath = EditorUtility.OpenFilePanel("Select .slang file", Application.dataPath, "slang");

            // Initial load
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {

                // entry points
                string content = File.ReadAllText(filePath);
                Regex regex = new Regex(@"\[shader\(""compute""\)\][\s\S]*?void\s+(\w+)\s*\(");

                var matches = regex.Matches(content);
                if (matches.Count > 0)
                {
                    entryPointList = new List<string>();

                    foreach (Match match in matches)
                    {
                        entryPointList.Add(match.Groups[1].Value);
                    }

                    bLoadSuccess = true;
                }
                else
                {
                    UnityEngine.Debug.Log("Entry point is not found.");
                    return;
                }

                kernelname = entryPointList[entryPointIndex];
                outputname = Path.GetFileNameWithoutExtension(filePath) + "_" + kernelname + ".compute";
            }
            else
            {
                UnityEngine.Debug.Log("Load slang file failed.");
                return;
            }
        }

        if (!bLoadSuccess) return;

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            UnityEngine.Debug.Log("Slang file is not found.");
            return;
        }
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            UnityEngine.Debug.Log("Slang file is not found.");
            return;
        }

        EditorGUILayout.LabelField("input slang:", filePath);
        entryPointIndex = EditorGUILayout.Popup("Entry Point", entryPointIndex, entryPointList.ToArray());
        outputname = EditorGUILayout.TextField("Output file name:", outputname);
        bUseVulkanSDK = EditorGUILayout.Toggle("Use slangc in Vulkan SDK:", bUseVulkanSDK);

        var outputPath = Path.GetDirectoryName(filePath) + "\\" + outputname;

        GUILayout.Space(10);
        if (GUILayout.Button("Generate"))
        {
            if(outputname == "")
            {
                UnityEngine.Debug.Log("Output file name is not set.");
                return;
            }

            var exePath = bUseVulkanSDK ? "slangc.exe"
                                        : "Packages\\slangu.package\\bin";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "slangc.exe",
                Arguments = string.Format("{0} -entry {1} -target hlsl -o {2}", filePath, kernelname, outputPath),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    // コマンドの出力を読み取る
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        UnityEngine.Debug.LogError(error);
                    }
                    else
                    {
                        // success
                        var appendline = "#pragma kernel " + kernelname + "\r\n\r\n";
                        InsertTextAtBeginning(outputPath, appendline);

                        UnityEngine.Debug.Log("Saved : " + outputPath);

                        AssetDatabase.Refresh();
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("execution failed.: " + ex.Message);
            }
        }
    }
    public static void InsertTextAtBeginning(string filePath, string textToInsert)
    {
        string originalContent = File.ReadAllText(filePath);
        string newContent = textToInsert + originalContent;
        File.WriteAllText(filePath, newContent);
    }
}
