using System;
using UnityEditor;
using UnityEngine;
using Landscape.Utils;
using UnityEditor.SceneManagement;

namespace Landscape.Editor.Terrain
{
    public enum ESectionNum
    {
        Section_2X = 2,
        Section_4X = 4,
        Section_8X = 8,
        Section_16X = 16
    };

    public enum ETerrainSize
    {
        Sector_128X = 128,
        Sector_256X = 256,
        Sector_512X = 512,
        Sector_1024X = 1024
    };

    [Serializable]
    public class TerrainCreator : EditorWindow
    {
        int ToolbarID_Manager;

        bool bTerrainFlipY = false;
        ETerrainSize TerrainSize = ETerrainSize.Sector_1024X;
        Vector3 TerrainScale = new Vector3(1, 1, 1);
        Vector3 TerrainPosition = new Vector3(0, 0, 0);

        LandscapeResource ResourceProfile;


        [MenuItem("Tools/Landscape/TerrainCreator")]
        static void OpenWindow()
        {
            TerrainCreator GPUProfileWind = (TerrainCreator)EditorWindow.GetWindow(typeof(TerrainCreator), false, "TerrainCreator", true);
            GPUProfileWind.Show();
        }

        void OnEnable()
        {
            ResourceProfile = Resources.Load<LandscapeResource>("LandscapeResourceProfile");
        }

        void OnDisable()
        {
            ResourceProfile = null;
        }

        void OnGUI()
        {
            DrawManageGUI();
        }

        void DrawCreateGUI(string InGrouName, bool DrawFlipToggle)
        {
            EditorGUI.indentLevel++;

            if(DrawFlipToggle)
                bTerrainFlipY = EditorGUILayout.Toggle("Flip", bTerrainFlipY);

            TerrainSize = (ETerrainSize)EditorGUILayout.EnumPopup("Size", TerrainSize);
            TerrainScale = EditorGUILayout.Vector3Field("Scale", TerrainScale);
            TerrainPosition = EditorGUILayout.Vector3Field("Position", TerrainPosition);

            EditorGUI.indentLevel--;
        }
        
        void DrawManageGUI()
        {
            ToolbarID_Manager = GUILayout.Toolbar(ToolbarID_Manager, new GUIContent[] { new GUIContent(ResourceProfile.CreateIcon, "Create"),  
                                                                                    new GUIContent(ResourceProfile.ImportIcon, "Import"), 
                                                                                    new GUIContent(ResourceProfile.ExportIcon, "Export") }, GUILayout.MaxHeight(32));
            switch (ToolbarID_Manager)
            {
                case 0:
                    {
                        DrawCreateGUI("Create Settings", false);

                        if (GUILayout.Button("CreateNew")) {
                            CreateTerrain();
                        }
                        break;
                    }

                case 1:
                    {
                        DrawCreateGUI("Import Settings", true);

                        if (GUILayout.Button("ImportNew")) {
                            ImportTerrain();
                        }
                        break;
                    }

                case 2:
                    {
                        break;
                    }
            }
        }
        
        void CreateTerrain()
        {
            string SaveLocation = EditorUtility.SaveFilePanelInProject("Save TerrainData", "New TerrainData", "asset", "Please enter a file name to save the TerrainData");
            if (SaveLocation != "")
            {
                Vector3 SpawonPosition = TerrainPosition - new Vector3((int)TerrainSize * 0.5f, 0, (int)TerrainSize * 0.5f);

                TerrainData terrainData = new TerrainData();
                terrainData.heightmapResolution = (int)TerrainSize + 1;
                terrainData.baseMapResolution = (int)TerrainSize;
                terrainData.size = new Vector3((int)TerrainSize, TerrainScale.y, (int)TerrainSize);
                terrainData.SetDetailResolution((int)TerrainSize, terrainData.detailResolutionPerPatch);
                HeightmapLoader.InitTerrainData((int)TerrainSize + 1, terrainData);
                AssetDatabase.CreateAsset(terrainData, AssetDatabase.GenerateUniqueAssetPath(SaveLocation));

                GameObject LandscapeObject = UnityEngine.Terrain.CreateTerrainGameObject(terrainData);
                LandscapeObject.name = "Landscape";
                LandscapeObject.layer = 8;
                LandscapeObject.transform.position = SpawonPosition;

                UnityEngine.Terrain TerrainProxy = LandscapeObject.GetComponent<UnityEngine.Terrain>();
                TerrainProxy.drawInstanced = true;
                TerrainProxy.heightmapPixelError = 12;

                StageUtility.PlaceGameObjectInCurrentStage(LandscapeObject);
                GameObjectUtility.EnsureUniqueNameForSibling(LandscapeObject);
                Selection.activeObject = LandscapeObject;
                Undo.RegisterCreatedObjectUndo(LandscapeObject, "Create Landscape");

                LandscapeObject.AddComponent<TerrainComponent>();
            }
        }

        public void ImportTerrain()
        {
            string ImportLocation = EditorUtility.OpenFilePanel("Import Heightmap", "", "r16,raw");
            if (ImportLocation != "")
            {
                string SaveLocation = EditorUtility.SaveFilePanelInProject("Save TerrainData", "New TerrainData", "asset", "Please enter a file name to save the TerrainData");
                if (SaveLocation != "")
                {
                    Vector3 SpawonPosition = TerrainPosition - new Vector3((int)TerrainSize * 0.5f, 0, (int)TerrainSize * 0.5f);

                    TerrainData terrainData = new TerrainData();
                    terrainData.baseMapResolution = (int)TerrainSize;
                    terrainData.heightmapResolution = (int)TerrainSize + 1;
                    terrainData.size = new Vector3((int)TerrainSize, TerrainScale.y, (int)TerrainSize);
                    terrainData.SetDetailResolution((int)TerrainSize, terrainData.detailResolutionPerPatch);
                    HeightmapLoader.ImportRaw(ImportLocation, 2, (int)TerrainSize + 1, bTerrainFlipY, terrainData);
                    AssetDatabase.CreateAsset(terrainData, AssetDatabase.GenerateUniqueAssetPath(SaveLocation));

                    GameObject LandscapeObject = UnityEngine.Terrain.CreateTerrainGameObject(terrainData);
                    LandscapeObject.name = "Landscape";
                    LandscapeObject.layer = 8;
                    LandscapeObject.transform.position = SpawonPosition;

                    UnityEngine.Terrain TerrainProxy = LandscapeObject.GetComponent<UnityEngine.Terrain>();
                    TerrainProxy.drawInstanced = true;
                    TerrainProxy.heightmapPixelError = 10;

                    StageUtility.PlaceGameObjectInCurrentStage(LandscapeObject);
                    GameObjectUtility.EnsureUniqueNameForSibling(LandscapeObject);
                    Selection.activeObject = LandscapeObject;
                    Undo.RegisterCreatedObjectUndo(LandscapeObject, "Create Landscape");

                    LandscapeObject.AddComponent<TerrainComponent>();
                }
            }
        }
    }
}
