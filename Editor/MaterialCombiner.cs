using System.Collections.Generic;
using Dino.MaterialCombiner.AssetManagement;
using Dino.MaterialCombiner.Atlasing;
using Dino.MaterialCombiner.Meshes;
using UnityEditor;
using UnityEngine;

namespace Dino.MaterialCombiner {
    internal class MaterialCombiner {
        private readonly AssetEditor _assetEditor;
        private readonly DirectoryProvider _directories;
        private readonly AtlasPacker _atlasPacker;
        private readonly MaterialCreator _materialCreator;
        private readonly MeshOptimizer _meshOptimizer;

        public MaterialCombiner(string path) {
            _assetEditor = new AssetEditor();
            _directories = new DirectoryProvider(path);
            _atlasPacker = new AtlasPacker(_assetEditor, _directories);
            _materialCreator = new MaterialCreator(_assetEditor, _directories);
            _meshOptimizer = new MeshOptimizer(_assetEditor, _directories);
        }

        public void Combine(IReadOnlyList<GameObject> prefabs, bool clearDirectory, PackingSettings packingSettings) {
            try {
                _assetEditor.BeginAssetDatabaseChanges();

                _assetEditor.PrepareDirectory(_directories.TexturesDir);
                _assetEditor.PrepareDirectory(_directories.MaterialsDir);
                _assetEditor.PrepareDirectory(_directories.MeshesDir);
                _assetEditor.PrepareDirectory(_directories.PrefabsDir);
                _assetEditor.PrepareDirectory(_directories.MiscDir);

                var atlas = _atlasPacker.CreateAtlas(prefabs, packingSettings);
                var materials = _materialCreator.CreateMaterials(prefabs, atlas);
                
                _meshOptimizer.ResetOptimizedCount();

                for (var prefabIndex = 0; prefabIndex < prefabs.Count; prefabIndex++) {
                    var originalPrefab = prefabs[prefabIndex];

                    var duplicatePrefab = (GameObject) PrefabUtility.InstantiatePrefab(originalPrefab);

                    _meshOptimizer.ReplaceMeshes(duplicatePrefab, materials, atlas, packingSettings);
                    _meshOptimizer.ReplaceAvatars(duplicatePrefab);

                    var prefabName = $"{originalPrefab.name}_{originalPrefab.GetInstanceID()}.prefab";

                    PrefabUtility.UnpackPrefabInstance(duplicatePrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    _assetEditor.CreatePrefabAsset(duplicatePrefab, _directories.PrefabsDir.ToAssetPath(prefabName));
                    Object.DestroyImmediate(duplicatePrefab);
                }

                if (clearDirectory) {
                    _assetEditor.ClearDirectory(_directories.PrefabsDir.FullPath);
                    _assetEditor.ClearDirectory(_directories.TexturesDir.FullPath);
                    _assetEditor.ClearDirectory(_directories.MaterialsDir.FullPath);
                    _assetEditor.ClearDirectory(_directories.MeshesDir.FullPath);
                    _assetEditor.ClearDirectory(_directories.MiscDir.FullPath);
                }
                
                var meshesOptimized = _meshOptimizer.MeshesOptimized;
                Debug.Log($"Meshes optimized: {meshesOptimized}");
            }
            finally {
                _assetEditor.FinalizeAssetDatabaseChanges();
            }
        }
    }
}