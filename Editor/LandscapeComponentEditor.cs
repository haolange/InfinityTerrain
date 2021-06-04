using UnityEditor;

namespace Landscape.Editor.Terrain
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TerrainComponent))]
    public class TerrainComponentEditor : UnityEditor.Editor
    {
        TerrainComponent terrainTarget { get { return target as TerrainComponent; } }

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
            terrainTarget.SerializeTerrain();
        }
    }
}
