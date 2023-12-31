using Dino.MaterialCombiner.Atlasing;
using UnityEditor;
using UnityEngine;

namespace Dino.MaterialCombiner {
    internal class MaterialCombinerWindow : EditorWindow {
        private const float UVError = 0.01f;
        private const string PrefsPrefix = "MaterialCombiner/";

        private static readonly GUILayoutOption LabelOptions = GUILayout.Width(110);
        
        [SerializeField] private GameObject[] Prefabs = new GameObject[0];

        private bool ClearDirectory {
            get => EditorPrefs.GetBool(PrefsPrefix + nameof(ClearDirectory), false);
            set => EditorPrefs.SetBool(PrefsPrefix + nameof(ClearDirectory), value);
        }

        private string Path {
            get => EditorPrefs.GetString(PrefsPrefix + nameof(Path), "Generated");
            set => EditorPrefs.SetString(PrefsPrefix + nameof(Path), value);
        }

        private int MaxAtlasSize {
            get => EditorPrefs.GetInt(PrefsPrefix + nameof(MaxAtlasSize), 2048);
            set => EditorPrefs.SetInt(PrefsPrefix + nameof(MaxAtlasSize), value);
        }
        
        private int MaxTiledChunkSize {
            get => EditorPrefs.GetInt(PrefsPrefix + nameof(MaxTiledChunkSize), 512);
            set => EditorPrefs.SetInt(PrefsPrefix + nameof(MaxTiledChunkSize), value);
        }
        
        private int MaxSplittedMeshVertices {
            get => EditorPrefs.GetInt(PrefsPrefix + nameof(MaxSplittedMeshVertices), 1000);
            set => EditorPrefs.SetInt(PrefsPrefix + nameof(MaxSplittedMeshVertices), value);
        }

        [MenuItem("Window/Material Combiner")]
        public static void ShowWindow() {
            GetWindow<MaterialCombinerWindow>("Material Combiner");
        }

        private void OnGUI() {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Clear directory", LabelOptions);
            ClearDirectory = EditorGUILayout.Toggle(ClearDirectory);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Path", LabelOptions);
            Path = EditorGUILayout.TextField(Path);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max atlas size", LabelOptions);
            MaxAtlasSize = EditorGUILayout.IntField(MaxAtlasSize);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max tiled chunk size", LabelOptions);
            MaxTiledChunkSize = EditorGUILayout.IntField(MaxTiledChunkSize);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max splitted mesh vertices", LabelOptions);
            MaxSplittedMeshVertices = EditorGUILayout.IntField(MaxSplittedMeshVertices);
            EditorGUILayout.EndHorizontal();

            ScriptableObject target = this;
            var so = new SerializedObject(target);
            var prefabsProperty = so.FindProperty(nameof(Prefabs));
            EditorGUILayout.PropertyField(prefabsProperty, includeChildren: true);
            so.ApplyModifiedProperties();
            
            GUI.enabled = Prefabs != null && Prefabs.Length > 0;
            if (GUILayout.Button("Combine Materials and Adjust Models")) {
                var materialCombiner = new MaterialCombiner(Path);
                var packingSettings = new PackingSettings(MaxAtlasSize, MaxSplittedMeshVertices, MaxTiledChunkSize, UVError);
                materialCombiner.Combine(Prefabs, ClearDirectory, packingSettings);
            }
        }
    }
}