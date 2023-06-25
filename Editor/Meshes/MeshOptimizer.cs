using Dino.MaterialCombiner.AssetManagement;
using Dino.MaterialCombiner.Atlasing;
using UnityEngine;

namespace Dino.MaterialCombiner.Meshes {
    internal class MeshOptimizer {
        private readonly AssetEditor _assetEditor;
        private readonly DirectoryProvider _directories;
        
        public int MeshesOptimized { get; private set; }

        public MeshOptimizer(AssetEditor assetEditor, DirectoryProvider directories) {
            _assetEditor = assetEditor;
            _directories = directories;
        }

        public void ResetOptimizedCount() {
            MeshesOptimized = 0;
        }

        public void ReplaceMeshes(GameObject prefab, Materials materials, Atlas atlas, PackingSettings packingSettings) {
            var meshFilters = prefab.GetComponentsInChildren<MeshFilter>();
            foreach (var meshFilter in meshFilters) {
                var meshAttributes = MeshAttributes.FromMesh(meshFilter.sharedMesh);

                if (atlas.Metadata.MeshesUsingAtlas.Contains(meshAttributes) == false) {
                    DuplicateOriginalMesh(meshFilter, materials);
                    continue;
                }

                OptimizeMesh(meshFilter, materials, atlas, prefab, packingSettings);
            }
        }

        public void ReplaceAvatars(GameObject prefab) {
            foreach (var animator in prefab.GetComponentsInChildren<Animator>()) {
                var avatar = animator.avatar;
                if (avatar == null) {
                    continue;
                }

                var avatarName = $"{avatar.name}_{avatar.GetInstanceID()}.asset";
                animator.avatar = _assetEditor.GetOrCreateDuplicateAsset(avatar, _directories.MiscDir.ToAssetPath(avatarName));
            }
        }

        private void DuplicateOriginalMesh(MeshFilter meshFilter, Materials materials) {
            var origMesh = meshFilter.sharedMesh;
            var newMeshName =  $"{origMesh.name}_{origMesh.GetInstanceID()}.asset";
            meshFilter.sharedMesh = _assetEditor.GetOrCreateDuplicateAsset(origMesh, _directories.MeshesDir.ToAssetPath(newMeshName));
            
            var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            var sharedMaterials = meshRenderer.sharedMaterials;
            for (var materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++) {
                var materialAttributes = MaterialAttributes.FromMaterial(sharedMaterials[materialIndex]);
                sharedMaterials[materialIndex] = materials[materialAttributes];
            }
            meshRenderer.sharedMaterials = sharedMaterials;
        }

        private void OptimizeMesh(MeshFilter meshFilter, Materials materials, Atlas atlas, GameObject prefab, PackingSettings packingSettings) {
            var origRenderer = meshFilter.GetComponent<Renderer>();
            var origMesh = meshFilter.sharedMesh;
            var useOriginalRenderer = origMesh.subMeshCount == 1;
            for (var subMeshIndex = 0; subMeshIndex < origMesh.subMeshCount; subMeshIndex++) {
                var origMaterial = origRenderer.sharedMaterials[subMeshIndex];
                
                var newMeshData = MeshData.FromSubMesh(origMesh, subMeshIndex);
                var subMeshAttributes = MeshAttributes.FromSubMesh(meshFilter.sharedMesh, subMeshIndex);
                var usesAtlasedTexture = atlas.Metadata.MeshesUsingAtlas.Contains(subMeshAttributes);

                var materialAttributes = MaterialAttributes.FromMaterial(origMaterial);
                if (usesAtlasedTexture) {
                    materialAttributes = materialAttributes.WithTexture(atlas.Texture);
                }

                var newSubMeshMaterial = materials[materialAttributes];

                if (usesAtlasedTexture && atlas.TryGetRegion(origMaterial, out var region)) {
                    AdjustAtlasedMeshUVs(newMeshData.UV, region, packingSettings);
                }

                // Clone the SubMesh into a new Mesh
                var newSubMesh = newMeshData.ToMesh();

                // Create a new Asset of the modified Mesh and assign it to the MeshFilter
                var meshName = $"Mesh_{prefab.name}_{meshFilter.GetInstanceID()}_{subMeshIndex}.asset";
                newSubMesh = _assetEditor.CreateAsset(newSubMesh, _directories.MeshesDir.ToAssetPath(meshName));

                var newSubMeshFilter = meshFilter;
                var newSubMeshRenderer = origRenderer;
                if (useOriginalRenderer == false) {
                    var subMeshGo = new GameObject("SubMesh_" + subMeshIndex);
                    subMeshGo.transform.SetParent(meshFilter.transform);
                    subMeshGo.transform.localPosition = Vector3.zero;
                    subMeshGo.transform.localScale = Vector3.one;
                    subMeshGo.transform.localRotation = Quaternion.identity;
                    newSubMeshFilter = subMeshGo.AddComponent<MeshFilter>();
                    newSubMeshRenderer = subMeshGo.AddComponent<MeshRenderer>();
                }

                newSubMeshFilter.sharedMesh = newSubMesh;
                newSubMeshRenderer.material = newSubMeshMaterial;
            }

            if (useOriginalRenderer == false) {
                Object.DestroyImmediate(meshFilter);
                Object.DestroyImmediate(origRenderer);
            }
        }

        private void AdjustAtlasedMeshUVs(Vector2[] uv, AtlasRegion region, PackingSettings packingSettings) {
            MeshesOptimized++;

            var negativeTilingOffset = Vector2Int.zero;
            for (var j = 0; j < uv.Length; j++) {
                if (uv[j].x < negativeTilingOffset.x - packingSettings.UvError) {
                    negativeTilingOffset.x = Mathf.FloorToInt(uv[j].x);
                }

                if (uv[j].y < negativeTilingOffset.y - packingSettings.UvError) {
                    negativeTilingOffset.y = Mathf.FloorToInt(uv[j].y);
                }
            }

            for (var j = 0; j < uv.Length; j++) {
                // Adjust the UVs based on the position and size of the texture in the atlas
                var rect = region.Rect;
                var tiling = region.Tiling;
                rect.size = new Vector2(rect.size.x / tiling.x, rect.size.y / tiling.y);
                uv[j] = Vector2.Scale(uv[j], rect.size) + rect.position - Vector2.Scale(negativeTilingOffset, rect.size);
            }
        }

        private readonly struct MeshData {
            public readonly int[] Triangles;
            public readonly Vector3[] Vertices;
            public readonly Vector3[] Normals;
            public readonly Vector4[] Tangents;
            public readonly Color[] Colors;
            public readonly Color32[] Colors32;
            public readonly Vector2[] UV;

            private MeshData(int[] triangles, Vector3[] vertices, Vector3[] normals, Vector4[] tangents,
                Color[] colors, Color32[] colors32, Vector2[] uv) {
                Triangles = triangles;
                Vertices = vertices;
                Normals = normals;
                Tangents = tangents;
                Colors = colors;
                Colors32 = colors32;
                UV = uv;
            }

            public Mesh ToMesh() {
                return new Mesh {
                    vertices = Vertices,
                    triangles = Triangles,
                    normals = Normals,
                    tangents = Tangents,
                    colors = Colors,
                    colors32 = Colors32,
                    uv = UV
                };
            }

            public static MeshData FromSubMesh(Mesh mesh, int index) {
                var triangles = mesh.GetTriangles(index);
                var vertices = new Vector3[triangles.Length];
                var normals = new Vector3[triangles.Length];
                var tangents = new Vector4[triangles.Length];
                var colors = new Color[triangles.Length];
                var colors32 = new Color32[triangles.Length];
                var uv = new Vector2[triangles.Length];

                for (var vertexIndex = 0; vertexIndex < triangles.Length; vertexIndex++) {
                    var origVertexIndex = triangles[vertexIndex];
                    vertices[vertexIndex] = mesh.vertices[origVertexIndex];
                    normals[vertexIndex] = mesh.normals[origVertexIndex];
                    tangents[vertexIndex] = mesh.tangents[origVertexIndex];
                    colors[vertexIndex] = origVertexIndex < mesh.colors.Length ? mesh.colors[origVertexIndex] : Color.white;
                    colors32[vertexIndex] = origVertexIndex < mesh.colors32.Length ? mesh.colors32[origVertexIndex] : new Color32(255, 255, 255, 255);
                    uv[vertexIndex] = mesh.uv[origVertexIndex];
                    triangles[vertexIndex] = vertexIndex;
                }
                
                return new MeshData(triangles, vertices, normals, tangents, colors, colors32, uv);
            }
        }
    }
}