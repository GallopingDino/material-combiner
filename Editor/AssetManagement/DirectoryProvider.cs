using UnityEngine;

namespace Dino.MaterialCombiner.AssetManagement {
    internal class DirectoryProvider {
        private readonly string _path;
            
        private string ProjectFullPath => Application.dataPath + "/";
        private string GeneratedAssetsPathFull => $"{ProjectFullPath}{_path.Trim('/')}/";
        private string GeneratedAssetsPathShort => $"Assets/{_path.Trim('/')}/";

        public AssetDirectoryPath MaterialsDir => new AssetDirectoryPath(GeneratedAssetsPathFull + "Materials/", GeneratedAssetsPathShort + "Materials/");
        public AssetDirectoryPath MeshesDir => new AssetDirectoryPath(GeneratedAssetsPathFull + "Meshes/", GeneratedAssetsPathShort + "Meshes/");
        public AssetDirectoryPath TexturesDir => new AssetDirectoryPath(GeneratedAssetsPathFull + "Textures/", GeneratedAssetsPathShort + "Textures/");
        public AssetDirectoryPath PrefabsDir => new AssetDirectoryPath(GeneratedAssetsPathFull + "Prefabs/", GeneratedAssetsPathShort + "Prefabs/");
        public AssetDirectoryPath MiscDir => new AssetDirectoryPath(GeneratedAssetsPathFull + "Misc/", GeneratedAssetsPathShort + "Misc/");


        public DirectoryProvider(string path) {
            _path = path;
        }

        public string ShortPathToFull(string path) {
            return ProjectFullPath + path.Replace("Assets/", string.Empty);
        }
    }
}