#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Empathia.Editor
{
    /// <summary>
    /// Fija la ventana Game a 1920×1080 (el desplegable suele estar en Free Aspect).
    /// </summary>
    [InitializeOnLoad]
    public static class EmpathiaGameView1080
    {
        const string SizeName = "EmpathIA 1920x1080";

        static EmpathiaGameView1080()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode
                    || state == PlayModeStateChange.EnteredEditMode)
                    TrySelect();
            };
            EditorApplication.delayCall += TrySelect;
        }

        [MenuItem("EmpathIA/Fijar Game view 1920x1080")]
        public static void MenuFix()
        {
            TrySelect();
            Debug.Log("[Empathia] Game view fijado a 1920×1080. Mira el desplegable arriba de Game.");
        }

        static void TrySelect()
        {
            try
            {
                var index = EnsureSize(1920, 1080);
                SelectSize(index);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Empathia] No pude fijar Game view 1920×1080: " + e.Message);
            }
        }

        static int EnsureSize(int width, int height)
        {
            var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleton.GetProperty("instance").GetValue(null, null);
            var group = sizesType.GetProperty("currentGroup").GetValue(instance, null);
            var groupType = group.GetType();

            var getBuiltin = groupType.GetMethod("GetBuiltinCount");
            var getCustom = groupType.GetMethod("GetCustomCount");
            var getSize = groupType.GetMethod("GetGameViewSize");
            var builtin = (int)getBuiltin.Invoke(group, null);
            var custom = (int)getCustom.Invoke(group, null);
            var total = builtin + custom;

            for (var i = 0; i < total; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                var w = (int)size.GetType().GetProperty("width").GetValue(size, null);
                var h = (int)size.GetType().GetProperty("height").GetValue(size, null);
                if (w == width && h == height)
                    return i;
            }

            var sizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
            var sizeTypeEnum = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
            var fixedRes = Enum.Parse(sizeTypeEnum, "FixedResolution");
            var ctor = sizeType.GetConstructor(new[] { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            var created = ctor.Invoke(new object[] { fixedRes, width, height, SizeName });
            groupType.GetMethod("AddCustomSize").Invoke(group, new[] { created });
            return builtin + custom;
        }

        static void SelectSize(int index)
        {
            var gvType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var windows = Resources.FindObjectsOfTypeAll(gvType);
            foreach (EditorWindow win in windows)
            {
                var prop = gvType.GetProperty(
                    "selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop != null)
                    prop.SetValue(win, index, null);

                var callback = gvType.GetMethod(
                    "SizeSelectionCallback",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                callback?.Invoke(win, new object[] { index, null });
                win.Repaint();
            }
        }
    }
}
#endif
