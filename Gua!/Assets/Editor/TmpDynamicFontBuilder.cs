using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace FrogCamp.Editor
{
    [InitializeOnLoad]
    public static class TmpDynamicFontBuilder
    {
        private const string DefaultSourcePath =
            "Assets/Fonts/DingTalk JinBuTi.ttf";
        private const string SessionKey =
            "FrogCamp.TmpDynamicFontBuilderV1";
        private const string TutorialCharacters =
            "新手规则伪装呱模仿移动轨迹混入大军听从口令完成节奏动作" +
            "抓住间隙执行任务全部即可获胜军官仔细观察蛙群寻找可疑目标" +
            "在旗帜下吹哨集合命令蛙群完成动作吐舌抓捕误伤士兵会陷入眩晕" +
            "试玩阶段不计胜负结束后所有状态都会重置准备进入正式游戏" +
            "说明将在数秒后自动关闭秒任务与抓捕移动动作和0123456789：，。、！“”";

        static TmpDynamicFontBuilder()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += BuildDefaultAndRepairScene;
        }

        [MenuItem("Assets/Frog Camp/生成可靠的动态 TMP 字体", true)]
        private static bool CanBuildSelectedFont()
        {
            return Selection.activeObject is Font;
        }

        [MenuItem("Assets/Frog Camp/生成可靠的动态 TMP 字体")]
        private static void BuildSelectedFont()
        {
            Font source = Selection.activeObject as Font;
            if (source == null) return;
            TMP_FontAsset asset = Build(source);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private static void BuildDefaultAndRepairScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Font source = AssetDatabase.LoadAssetAtPath<Font>(
                DefaultSourcePath);
            TMP_FontAsset rebuilt = Build(source);
            if (rebuilt == null) return;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != "游戏界面") return;
            bool changed = false;
            TextMeshProUGUI[] texts =
                Object.FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in texts)
            {
                if (text.font == null ||
                    text.font.name != "DingTalk JinBuTi SDF")
                    continue;
                text.font = rebuilt;
                EditorUtility.SetDirty(text);
                changed = true;
            }
            if (!changed) return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static TMP_FontAsset Build(Font source)
        {
            if (source == null) return null;
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string folder = Path.GetDirectoryName(sourcePath)
                ?.Replace('\\', '/');
            string assetPath = folder + "/" + source.name +
                               " Dynamic SDF.asset";
            TMP_FontAsset existing =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                source, 72, 9, GlyphRenderMode.SDFAA,
                2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                Debug.LogError("无法从源字体生成 TMP 字体：" + sourcePath);
                return null;
            }

            fontAsset.name = source.name + " Dynamic SDF";
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(
                    fontAsset.material, fontAsset);
            }
            foreach (Texture2D atlas in fontAsset.atlasTextures)
            {
                if (atlas == null || AssetDatabase.Contains(atlas)) continue;
                atlas.name = fontAsset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }

            string missing;
            fontAsset.TryAddCharacters(TutorialCharacters, out missing);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                assetPath, ImportAssetOptions.ForceUpdate);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning(fontAsset.name +
                                 " 缺少字形：" + missing);
            else
                Debug.Log("已生成可靠动态 TMP 字体：" + assetPath);
            return fontAsset;
        }
    }
}
