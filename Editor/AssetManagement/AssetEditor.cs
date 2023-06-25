using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dino.MaterialCombiner.AssetManagement {
    internal class AssetEditor {
        private readonly HashSet<string> _filesToKeep = new HashSet<string>();

        public void BeginAssetDatabaseChanges() {
            AssetDatabase.StartAssetEditing();
        }

        public void FinalizeAssetDatabaseChanges() {
            AssetDatabase.StopAssetEditing();
        }

        public void ApplyAssetDatabaseChanges() {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
            AssetDatabase.StartAssetEditing();
        }

        public void ClearDirectory(string path) {
            if (Directory.Exists(path) == false) {
                return;
            }
            var subDirectories = Directory.GetDirectories(path);
            foreach (var directory in subDirectories) {
                Directory.Delete(directory);
            }
            var files = Directory.GetFiles(path);
            foreach (var file in files) {
                if (_filesToKeep.Contains(file.Replace(".meta", ""))) {
                    continue;
                }
                File.Delete(file);
            }
        }

        public Texture2D CreateTextureAsset(Texture2D asset, AssetPath path) {
            KeepFile(path);
            File.WriteAllBytes(path.FullPath, asset.EncodeToPNG());
            ApplyAssetDatabaseChanges();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path.ShortPath);
        }

        public T CreateAsset<T>(T asset, AssetPath path) where T : Object {
            KeepFile(path);
            AssetDatabase.CreateAsset(asset, path.ShortPath);
            return asset;
        }

        public GameObject CreatePrefabAsset(GameObject prefab, AssetPath path) {
            KeepFile(path);
            PrefabUtility.SaveAsPrefabAsset(prefab, path.ShortPath);
            return prefab;
        }

        public T GetOrCreateDuplicateAsset<T>(T asset, AssetPath path) where T : Object {
            if (_filesToKeep.Contains(path.FullPath) && !File.Exists(path.FullPath)) {
                Debug.LogWarning("OK WE REALLY DO NEED TO APPLY CHANGES SINCE AssetDatabase.CreateAsset GETS SUSPENDED BY StartAssetEditing");
                ApplyAssetDatabaseChanges();
            }

            KeepFile(path);
            if (File.Exists(path.FullPath)) {
                return AssetDatabase.LoadAssetAtPath<T>(path.ShortPath);
            }
            var duplicate = Object.Instantiate(asset);
            AssetDatabase.CreateAsset(duplicate, path.ShortPath);
            return duplicate;
        }

        public T GetOrCreateDuplicateFile<T>(T asset, AssetPath path, DirectoryProvider directories) where T : Object {
            if (_filesToKeep.Contains(path.FullPath) && !File.Exists(path.FullPath)) {
                Debug.LogWarning("OK WE REALLY DO NEED TO APPLY CHANGES SINCE AssetDatabase.CreateAsset GETS SUSPENDED BY StartAssetEditing");
                ApplyAssetDatabaseChanges();
            }
            KeepFile(path);
            if (File.Exists(path.FullPath) == false) {
                // AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(asset), path.ShortPath);
                var origPath = directories.ShortPathToFull(AssetDatabase.GetAssetPath(asset));
                var origBytes = File.ReadAllBytes(origPath);
                File.WriteAllBytes(path.FullPath, origBytes);
                ApplyAssetDatabaseChanges();
            }
            return AssetDatabase.LoadAssetAtPath<T>(path.ShortPath);
        }

        public Texture2D GetOrCreateDuplicateTexture(Texture2D texture, AssetPath path) {
            if (_filesToKeep.Contains(path.FullPath) && !File.Exists(path.FullPath)) {
                Debug.LogWarning("OK WE REALLY DO NEED TO APPLY CHANGES SINCE AssetDatabase.CreateAsset GETS SUSPENDED BY StartAssetEditing");
                ApplyAssetDatabaseChanges();
            }

            KeepFile(path);
            if (File.Exists(path.FullPath) == false) {
                File.WriteAllBytes(path.FullPath, texture.EncodeToPNG());
                ApplyAssetDatabaseChanges();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path.ShortPath);
        }

        public void PrepareDirectory(AssetDirectoryPath dir) {
            if (Directory.Exists(dir.FullPath) == false) {
                Directory.CreateDirectory(dir.FullPath);
            }
        }

        private void KeepFile(AssetPath path) => _filesToKeep.Add(path.FullPath);
    }
}