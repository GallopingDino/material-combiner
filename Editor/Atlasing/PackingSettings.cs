namespace Dino.MaterialCombiner.Atlasing {
    internal readonly struct PackingSettings {
        public readonly int MaxAtlasSize;
        public readonly int MaxSplittedMeshVertices;
        public readonly int MaxTiledChunkSize;
        public readonly double UvError;
        
        public PackingSettings(int maxAtlasSize, int maxSlittedMeshVertices, int maxTiledChunkSize, double uvError) {
            MaxAtlasSize = maxAtlasSize;
            MaxSplittedMeshVertices = maxSlittedMeshVertices; 
            MaxTiledChunkSize = maxTiledChunkSize;
            UvError = uvError;
        }
    }
}