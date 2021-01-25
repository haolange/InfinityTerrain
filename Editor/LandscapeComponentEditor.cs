using UnityEditor;

namespace Landscape.Editor.Terrain
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LandscapeProxy))]
    public class LandscapeProxyEditor : UnityEditor.Editor
    {
        LandscapeProxy LandscapeTarget { get { return target as LandscapeProxy; } }


        public LandscapeProxyEditor()
        {

        }

        void OnEnable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += PreSave;
        }

        void OnValidate()
        {

        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            serializedObject.ApplyModifiedProperties();
        }

        void OnDisable()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= PreSave;
        }

        void PreSave(UnityEngine.SceneManagement.Scene InScene, string InPath)
        {
            LandscapeTarget.SerializeTerrain();
        }
    }
}
