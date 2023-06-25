using System.Collections.Generic;
using Dino.MaterialCombiner.AssetManagement;
using Dino.MaterialCombiner.Meshes;
using UnityEngine;

namespace Dino.MaterialCombiner.Atlasing {
    internal class AtlasPacker {
        private readonly AssetEditor _assetEditor;
        private readonly DirectoryProvider _directories;

        public AtlasPacker(AssetEditor assetEditor, DirectoryProvider directories) {
            _assetEditor = assetEditor;
            _directories = directories;
        }

        public Atlas CreateAtlas(IReadOnlyList<GameObject> objects, PackingSettings settings) {
            var metadata = new AtlasMetadata();
            AnalyzeOriginalTextures(objects, metadata, settings);
            PrepareTiledTextures(metadata);
            return PackTiledTextures(metadata, settings);
        }
        
        private void AnalyzeOriginalTextures(IReadOnlyList<GameObject> objects, AtlasMetadata metadata, PackingSettings settings) {
            for (var objectIndex = 0; objectIndex < objects.Count; objectIndex++) {
                var gameObject = objects[objectIndex];

                var meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
                foreach (var meshFilter in meshFilters) {
                    if (CanOptimizeSubMeshes(meshFilter.sharedMesh, settings) == false) {
                        continue;
                    }

                    AnalyzeMeshTextures(meshFilter, metadata, settings);
                }
            }
        }

        private void AnalyzeMeshTextures(MeshFilter meshFilter, AtlasMetadata metadata, PackingSettings settings) {
            var renderer = meshFilter.GetComponent<Renderer>();
            var materials = renderer.sharedMaterials;
            for (var subMeshIndex = 0; subMeshIndex < materials.Length; subMeshIndex++) {
                var material = materials[subMeshIndex];
                var texture = material.mainTexture as Texture2D;
                if (texture == null) {
                    continue;
                }

                var tiling = GetTiling(meshFilter.sharedMesh, subMeshIndex, settings);

                if (CanOptimizeTiling(texture, tiling, settings) == false) {
                    continue;
                }

                metadata.MeshesUsingAtlas.Add(MeshAttributes.FromMesh(meshFilter.sharedMesh));
                metadata.MeshesUsingAtlas.Add(MeshAttributes.FromSubMesh(meshFilter.sharedMesh, subMeshIndex));

                if (metadata.AtlasedTextureIndicesByMaterials.TryGetValue(material, out var existingIndex)) {
                    var existingTiling = metadata.Tilings[existingIndex];
                    metadata.Tilings[existingIndex] = new Vector2Int(
                        Mathf.Max(existingTiling.x, tiling.x),
                        Mathf.Max(existingTiling.y, tiling.y));
                    continue;
                }

                metadata.SubTextures.Add(texture);
                metadata.Tilings.Add(tiling);
                metadata.AtlasedTextureIndicesByMaterials[material] = metadata.SubTextures.Count - 1;
            }
        }        
        
        private void PrepareTiledTextures(AtlasMetadata metadata) {
            for (var textureIndex = 0; textureIndex < metadata.SubTextures.Count; textureIndex++) {
                metadata.SubTextures[textureIndex] = CreateTiledTexture(metadata.SubTextures[textureIndex], metadata.Tilings[textureIndex]);
            }
        }

        private Texture2D CreateTiledTexture(Texture2D orig, Vector2Int tiling) {
            if (tiling.x <= 1 && tiling.y <= 1) {
                return orig;
            }
            
            var result = new Texture2D(orig.width * tiling.x, orig.height * tiling.y);
            var origPixels = orig.GetPixels();
            var resultPixels = new Color[result.width * result.height];
            for (var y = 0; y < result.height; y++) {
                for (var x = 0; x < result.width; x++) {
                    var wrappedX = x % orig.width;
                    var wrappedY = y % orig.height;
                    resultPixels[y * result.width + x] = origPixels[wrappedY * orig.width + wrappedX];
                }
            }
            result.SetPixels(resultPixels);
            return result;
        }

        private Atlas PackTiledTextures(AtlasMetadata metadata, PackingSettings settings) {
            var texture = new Texture2D(settings.MaxAtlasSize, settings.MaxAtlasSize);
            metadata.Rects = texture.PackTextures(metadata.SubTextures.ToArray(), 0, settings.MaxAtlasSize);
            texture.Apply();
            texture = _assetEditor.CreateTextureAsset(texture, _directories.TexturesDir.ToAssetPath("Atlas.png"));
            return new Atlas(texture, metadata);
        }
        
        private bool CanOptimizeSubMeshes(Mesh mesh, PackingSettings settings) {
            return mesh.subMeshCount < 2 || mesh.subMeshCount * mesh.vertexCount < settings.MaxSplittedMeshVertices;
        }

        private bool CanOptimizeTiling(Texture texture, Vector2Int tiling, PackingSettings settings) {
            if (tiling.x == 1 && tiling.y == 1) {
                return true;
            }
            return tiling.x * texture.width <= settings.MaxTiledChunkSize && tiling.y * texture.height <= settings.MaxTiledChunkSize;
        }

        private Vector2Int GetTiling(Mesh mesh, int subMeshIndex, PackingSettings settings) {
            var maxX = 0f;
            var minX = 1f;
            var maxY = 0f;
            var minY = 1f;
            var indices = mesh.GetTriangles(subMeshIndex);
            var uv = mesh.uv;
            foreach (var index in indices) {
                if (uv[index].x < minX - settings.UvError) minX = uv[index].x;
                if (uv[index].x > maxX + settings.UvError) maxX = uv[index].x;
                if (uv[index].y < minY - settings.UvError) minY = uv[index].y;
                if (uv[index].y > maxY + settings.UvError) maxY = uv[index].y;
            }
            return new Vector2Int(Mathf.CeilToInt(maxX) - Mathf.FloorToInt(minX), Mathf.CeilToInt(maxY) - Mathf.FloorToInt(minY));
        }
    }
}