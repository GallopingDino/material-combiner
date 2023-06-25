namespace Dino.MaterialCombiner.AssetManagement {
    internal readonly struct AssetPath {
        private readonly AssetDirectoryPath _directory;
        private readonly string _name;
        
        public string FullPath => _directory.FullPath + _name;
        public string ShortPath => _directory.ShortPath + _name;

        public AssetPath(AssetDirectoryPath directory, string name) {
            _directory = directory;
            _name = name;
        }
    }
}