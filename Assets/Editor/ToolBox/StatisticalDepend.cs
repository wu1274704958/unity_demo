using LitJson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Editor
{
    class StatisticalDepend : EditorWindow
    {
        [MenuItem("Assets/StatisticalDependence")]
        public static void StatisticalDependence()
        {
            UnityEngine.Object[] selection = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
            var map = new Dictionary<string, HashSet<string>>();
            int Num1 = 0;
            foreach (var o in selection)
            {
                string fullpath = AssetDatabase.GetAssetPath(o);
                string filename = Path.GetFileNameWithoutExtension(fullpath);
                if (File.Exists(fullpath) && o is GameObject gameObj)
                {
                    var dep = GetDependences(gameObj);
                    foreach (var obj in dep)
                    {
                        if (map.TryGetValue(obj, out var set))
                        {
                            if (!set.Contains(filename))
                                set.Add(filename);
                        }
                        else
                        {
                            var hashSet = new HashSet<string>();
                            hashSet.Add(filename);
                            map.Add(obj, hashSet);
                        }
                        Num1 += 1;
                        EditorUtility.DisplayCancelableProgressBar("执行中...", filename, (float)(Num1 / selection.Length));
                    }
                }
            }

            var sorted = new List<(string, HashSet<string>)>();
            foreach (var set in map)
            {
                sorted.Add((set.Key, set.Value));
            }

            sorted.Sort((a, b) => { return b.Item2.Count - a.Item2.Count; });
            Num1 = 0;
            StreamWriter sw = new StreamWriter("D:\\Depend.txt", false);
            foreach (var set in sorted)
            {
                sw.WriteLine(set.Item1 + " [" + set.Item2.Count + "]");
                foreach (var f in set.Item2)
                {
                    sw.WriteLine("\t" + f);
                }
                Num1 += 1;
                EditorUtility.DisplayCancelableProgressBar("写入中...", set.Item1, (float)(Num1 / sorted.Count));
            }
            sw.Flush();
            sw.Close();
            EditorUtility.ClearProgressBar();
        }

        private static List<string> GetDependences(GameObject gameObj, bool excludeNearest = false)
        {
            var selfPath = "";
            var res = AssetDatabase.GetDependencies(selfPath = AssetDatabase.GetAssetPath(gameObj));
            var deps = new List<string>();
            foreach (var s in res)
            {
                if (!s.EndsWith(".cs") && !s.Equals(selfPath))
                {
                    //var o = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(s);
                    deps.Add(s);
                }
            }
            return deps;
        }

        private static HashSet<string> CollectChildren(GameObject gameObj)
        {
            HashSet<string> res = new HashSet<string>();
            var assetsPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObj);
            var nearest = new HashSet<string>();
            CollectChildrenEx(ref res, gameObj, assetsPath, ref nearest);
            return res;
        }

        private static void CollectChildrenEx(ref HashSet<string> res, GameObject gameObj, string assetsPath,
            ref HashSet<string> nearest)
        {
            int l = gameObj.transform.childCount;
            for (int i = 0; i < l; ++i)
            {
                var c = gameObj.transform.GetChild(i);
                var a = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(c.gameObject);
                if (!a.Equals(assetsPath))
                {
                    if (nearest.Contains(a))
                    {
                        res.Add(c.name);
                    }
                    else
                    {
                        nearest.Add(a);
                    }
                }
                else
                {
                    res.Add(c.name);
                }
                CollectChildrenEx(ref res, c.gameObject, assetsPath, ref nearest);
            }
        }

        public static void RunWithProgress<T>(T[] list, Func<T, string> on, string t1 = "")
        {
            int a = 0, l = list.Length;
            foreach (var o in list)
            {
                var msg = on(o);
                a += 1;
                EditorUtility.DisplayCancelableProgressBar(t1, msg, (float)(a / l));
            }
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Assets/StatisticalMissImage")]
        public static void StatisticalMissImage()
        {
            UnityEngine.Object[] selection = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);
            StreamWriter sw = new StreamWriter("D:\\MissImage.json", false);

            Dictionary<string, List<string>> dic = new Dictionary<string, List<string>>();
            RunWithProgress<UnityEngine.Object>(selection, (o) =>
            {
                string fullpath = AssetDatabase.GetAssetPath(o);
                string filename = Path.GetFileNameWithoutExtension(fullpath);
                if (File.Exists(fullpath) && o is GameObject gameObj)
                {
                    var res = new List<string>();
                    GetMissedImagesEx(gameObj, ref res);
                    if (res.Count > 0)
                    {
                        dic.Add(fullpath, res);
                    }
                }
                return filename;
            });

            LitJson.JsonData json = JsonMapper.ToJson(dic);

            sw.Write(json.ToString().ToArray());
            sw.Flush();
            sw.Close();

        }

        private static void GetMissedImagesEx(GameObject gameObj, ref List<string> res, string parentName = "")
        {
            var name = parentName + "/" + gameObj.name;
            var img = gameObj.GetComponent<Image>();
            if (img != null && img.sprite == null)
            {
                SerializedObject so = new SerializedObject(img);
                var iter = so.GetIterator();//拿到迭代器
                var f = false;
                while (iter.NextVisible(true))//如果有下一个属性
                {
                    //如果这个属性类型是引用类型的
                    if (iter.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        //引用对象是null 并且 引用ID不是0 说明丢失了引用
                        if (iter.objectReferenceValue == null && iter.objectReferenceInstanceIDValue != 0)
                        {
                            f = true;
                            break;
                        }
                    }
                }
                if (f)
                {
                    res.Add(name);
                }
            }
            for (int i = 0; i < gameObj.transform.childCount; ++i)
            {
                var c = gameObj.transform.GetChild(i);
                GetMissedImagesEx(c.gameObject, ref res, name);
            }
        }

        public class SvnFile
        {
            public string Status;
            public string FullPath;
        }

        [MenuItem("Assets/StatisticalChangedMetaFile")]
        public static void StatisticalChangedMetaFile()
        {
            var obj = Selection.activeObject;
            var dir = AssetDatabase.GetAssetPath(obj);
            if (!Directory.Exists(dir)) return;
            if (EditorUtility.DisplayCancelableProgressBar("执行中...", "", 0.0f))
            {
                EditorUtility.ClearProgressBar();
                return;
            }
            var p = ExecCmd("svn", "status", dir);
            List<SvnFile> modify = new List<SvnFile>();
            while (!p.StandardOutput.EndOfStream)
            {
                var line = p.StandardOutput.ReadLine();
                //if (line.StartsWith("M ") && line.EndsWith(".meta"))
                {
                    var file = line.Remove(0, 1).TrimStart();
                    if (EditorUtility.DisplayCancelableProgressBar("执行中...", file, 0.0f))
                    {
                        EditorUtility.ClearProgressBar();
                        return;
                    }

                    var svn = new SvnFile();
                    svn.FullPath = Path.Combine(dir, file);
                    svn.Status = line.Substring(0, 1);
                    modify.Add(svn);
                }
            }

            if (EditorUtility.DisplayCancelableProgressBar("执行中...", "", 0.5f))
            {
                EditorUtility.ClearProgressBar();
                return;
            }
            p.Close();
            var index = 0;
            var total = modify.Count;
            Dictionary<int, SvnFile> dic = new Dictionary<int, SvnFile>();
            foreach (var s in modify)
            {
                ++index;
                var per = 0.5f + (index * 0.5f / total);
                var news = s.FullPath;
                if (EditorUtility.DisplayCancelableProgressBar("执行中...", news, per))
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }

                if (s.Status == "M")
                {
                    if (!isOnlyModifyAssetsBundleName(s.FullPath))
                    {
                        dic[dic.Count + 1] = s;
                    }
                }
                else
                {
                    dic[dic.Count + 1] = s;
                }
            }
            TableShower.ShowSvnFile(dic);
            EditorUtility.ClearProgressBar();
        }

        [MenuItem("Assets/RevertAbChangedMetaFile")]
        public static void RevertAbChangedMetaFile()
        {
            var obj = Selection.assetGUIDs;
            if (obj == null || obj.Length == 0) return;
            var dir = AssetDatabase.GUIDToAssetPath(obj[0]);
            if (!Directory.Exists(dir)) return;
            if (EditorUtility.DisplayCancelableProgressBar("执行中...", "", 0.0f))
            {
                EditorUtility.ClearProgressBar();
                return;
            }
            var p = ExecCmd("svn", "status", dir);
            List<SvnFile> modify = new List<SvnFile>();
            while (!p.StandardOutput.EndOfStream)
            {
                var line = p.StandardOutput.ReadLine();
                if (line.StartsWith("M ") && line.EndsWith(".meta"))
                {
                    var file = line.Remove(0, 1).TrimStart();
                    if (EditorUtility.DisplayCancelableProgressBar("执行中...", file, 0.0f))
                    {
                        EditorUtility.ClearProgressBar();
                        return;
                    }

                    var svn = new SvnFile();
                    svn.FullPath = Path.Combine(dir, file);
                    svn.Status = line.Substring(0, 1);
                    modify.Add(svn);
                }
            }

            if (EditorUtility.DisplayCancelableProgressBar("执行中...", "", 0.5f))
            {
                EditorUtility.ClearProgressBar();
                return;
            }
            p.Close();
            var index = 0;
            var total = modify.Count;
            foreach (var s in modify)
            {
                ++index;
                var per = 0.5f + (index * 0.5f / total);
                var news = s.FullPath;
                if (EditorUtility.DisplayCancelableProgressBar("执行中...", news, per))
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }

                if (s.Status == "M" && isOnlyModifyAssetsBundleName(s.FullPath))
                    RevertFile(s.FullPath);
            }
            //TableShower.ShowSvnFile(dic);
            EditorUtility.ClearProgressBar();
        }

        private static bool isOnlyModifyAssetsBundleName(string path)
        {
            var p = ExecCmd("svn", "diff " + path);
            List<string> modify = new List<string>();
            int step = 0;
            int st = 0;
            while (!p.StandardOutput.EndOfStream)
            {
                var line = p.StandardOutput.ReadLine();
                if (line.StartsWith("@@") && line.LastIndexOf("@@") > 0)
                {
                    step += 1;
                    continue;
                }
                if (step > 0)
                {
                    if (step > 1) { p.Close(); return false; }
                    var arr = line.ToCharArray();
                    if (arr[0] == '+' || arr[0] == '-')
                    {
                        if (line.IndexOf("assetBundleName") >= 0)
                        {
                            st |= arr[0] == '+' ? 1 : 2;
                        }
                        else
                        {
                            p.Close(); return false;
                        }
                    }
                }
            }
            p.Close();
            return step <= 1;
        }

        public static System.Diagnostics.Process ExecCmd(string cmd, string args, string dir = "")
        {
            string output = null;
            System.Diagnostics.Process p = new Process();
            p.StartInfo.FileName = cmd;//可执行程序路径
            if (!string.IsNullOrEmpty(dir))
                p.StartInfo.WorkingDirectory = dir;
            p.StartInfo.Arguments = args;//参数以空格分隔，如果某个参数为空，可以传入""
            p.StartInfo.UseShellExecute = false;//是否使用操作系统shell启动
            p.StartInfo.CreateNoWindow = true;//不显示程序窗口
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
            p.Start();
            //正常运行结束放回代码为0
            return p;
        }

        public static string ExecCmdSample(string cmd, string args, string dir = "")
        {
            var p = ExecCmd(cmd, args,dir);
            StringBuilder sb = new StringBuilder();
            while (!p.StandardOutput.EndOfStream)
            {
                sb.Append(p.StandardOutput.ReadLine());
            }
            p.Close();
            return sb.ToString();
        }

        public static bool RevertFile(string path,string depth = "infinity")
        {
            var p = ExecCmdSample("svn", string.Format("revert --depth={0} " + path,depth));
            return p != null && p.StartsWith("Reverted");
        }

        public static Type getTypeByPath(string s)
        {
            if (s.EndsWith(".png"))
            {
                return typeof(Texture);
            }
            if (s.EndsWith(".mat"))
            {
                return typeof(Material);
            }
            if (s.EndsWith(".shader"))
            {
                return typeof(Shader);
            }
            if (s.EndsWith(".prefab"))
            {
                return typeof(GameObject);
            }
            return typeof(UnityEngine.Object);
        }

        [MenuItem("Assets/ChangeToNewCutoutAdapter")]
        public static void ChangeToNewCutoutAdapter()
        {
            UnityEngine.Object[] selection = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.DeepAssets);

            Dictionary<string, List<string>> dic = new Dictionary<string, List<string>>();
            RunWithProgress<UnityEngine.Object>(selection, (o) =>
            {
                string fullpath = AssetDatabase.GetAssetPath(o);
                string filename = Path.GetFileNameWithoutExtension(fullpath);
                if (File.Exists(fullpath) && o is GameObject gameObj)
                {
                    if (changeComp<UIAdaptor, yoyohan.getnotchsize.NotchSizeMono>(gameObj, (c) =>
                     {
                         c.isLandscape = true;
                     }))
                    {
                    }
                }
                return filename;
            });
            AssetDatabase.SaveAssets();
        }

        private static bool changeComp<T, T2>(GameObject o, Action<T2> on = null, bool parent_has = false)
            where T : Component
            where T2 : Component
        {
            var old = o.GetComponent<T>();
            bool res = false;
            if (old != null)
            {
                GameObject.DestroyImmediate(old, true);
                if (!parent_has)
                {
                    var n = o.AddComponent<T2>();
                    on?.Invoke(n);
                }
                res = true;
                parent_has = true;
            }

            for (int i = 0; i < o.transform.childCount; ++i)
            {
                if (changeComp<T, T2>(o.transform.GetChild(i).gameObject, on, parent_has))
                    res = true;
            }
            return res;
        }
    }
}
