using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class Build 
{
    private static string s_BunleTargetPath = Application.dataPath + "/../AssetBundle";

    [MenuItem("Assets/BuildAssetsBundle")]
    static void BuildAssetBundle()
    {
        AssetBundleBuild assets = new AssetBundleBuild();
        assets.assetBundleName = "shader";
        assets.assetBundleVariant = "ab";
        UnityEngine.Object[] selection = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
        if (selection.Length == 0) return;

        List<string> paths = new List<string>();
        for (int i = 0; i < selection.Length;++i)
        {
            string fullpath = AssetDatabase.GetAssetPath(selection[i]);
            paths.Add(fullpath);
        }

        assets.assetNames = paths.ToArray();

        DirectoryInfo directoryInfo = new DirectoryInfo(s_BunleTargetPath);
        if(!directoryInfo.Exists)
        {
            directoryInfo.Create();
        }
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(s_BunleTargetPath,new AssetBundleBuild[] { assets } ,BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        if (manifest == null)
        {
            Debug.LogError("AssetBundle 打包失败！");
            //DeleteMainfest();
        }
        else
        {
            Debug.Log(string.Format("<color=green>{0}</color>", "AssetBundle打包成功"));
            //DeleteMainfest();
        }
    }

    static void DeleteMainfest()
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(s_BunleTargetPath);
        FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (files[i].Name.EndsWith(".manifest"))
            {
                File.Delete(files[i].FullName);
            }
        }
    }
}
