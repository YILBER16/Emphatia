#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Empathia.Editor
{
    /// <summary>
    /// Crea el Canvas en Login.unity para que Isaac lo edite en Hierarchy.
    /// </summary>
    public static class EmpathiaLoginSceneBaker
    {
        const string ScenePath = "Assets/Scenes/Login.unity";

        [MenuItem("EmpathIA/Guardar UI en la escena Login")]
        public static void MenuBake()
        {
            Bake(save: true, force: false);
        }

        [InitializeOnLoadMethod]
        static void AutoBakeIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.path != ScenePath)
                    return;
                Bake(save: true, force: false);
            };
        }

        public static void Bake(bool save, bool force)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath);

            var ctrl = Object.FindFirstObjectByType<LoginScreenController>();
            if (ctrl == null)
            {
                Debug.LogWarning("[Empathia] No hay LoginScreenController en Login.unity.");
                return;
            }

            if (ctrl.HasSceneUi && !force)
            {
                ctrl.ApplyRuntimeSkin();
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(ctrl.gameObject, "Armar UI EmpathIA");
            ctrl.BakeUiInEditor();
            ctrl.ApplyRuntimeSkin();
            EditorUtility.SetDirty(ctrl);
            EditorSceneManager.MarkSceneDirty(scene);
            if (save)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[Empathia] Canvas guardado en Login.unity. En Hierarchy busca EmpathiaLoginCanvas.");
            }
        }
    }
}
#endif
