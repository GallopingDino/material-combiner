namespace Dino.MaterialCombiner.AssetManagement {
    internal class AssetDirectoryPath {
        public readonly string FullPath;
        public readonly string ShortPath;

        public AssetDirectoryPath(string fullPath, string shortPath) {
            FullPath = fullPath;
            ShortPath = shortPath;
        }
        
        public AssetPath ToAssetPath(string name) => new AssetPath(this, name);
    }
}