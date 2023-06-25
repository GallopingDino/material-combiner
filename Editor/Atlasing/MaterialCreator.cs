using System.Collections.Generic;
using Dino.MaterialCombiner.AssetManagement;
using Dino.MaterialCombiner.Meshes;
using UnityEngine;

namespace Dino.MaterialCombiner.Atlasing {
    internal class Materials {
        private readonly IReadOnlyDictionary<MaterialAttributes, Material> _materials;
        public Material this[MaterialAttributes attributes] => _materials[attributes];
        public Materials(IReadOnlyDictionary<MaterialAttributes, Material> materials) => _materials = materials;
    }
    
    internal class MaterialCreator {
        private readonly AssetEditor _assetEditor;
        private readonly DirectoryProvider _directories;

        public MaterialCreator(AssetEditor assetEditor, DirectoryProvider directories) {
            _assetEditor = assetEditor;
            _directories = directories;
        }

        public Materials CreateMaterials(IReadOnlyList<GameObject> objects, Atlas atlas) {
            var materials = new Dictionary<MaterialAttributes, Material>();
            for (var objectIndex = 0; objectIndex < objects.Count; objectIndex++) {
                var gameObject = objects[objectIndex];
                foreach (var meshRenderer in gameObject.GetComponentsInChildren<MeshRenderer>()) {
                    CreateMeshMaterials(meshRenderer, atlas, materials);
                }
            }
            return new Materials(materials);
        }

        private void CreateMeshMaterials(MeshRenderer meshRenderer, Atlas atlas, Dictionary<MaterialAttributes, Material> newMaterials) {
            var meshFilter = meshRenderer.GetComponent<MeshFilter>();
            for (var materialIndex = 0; materialIndex < meshRenderer.sharedMaterials.Length; materialIndex++) {
                var originalMaterial = meshRenderer.sharedMaterials[materialIndex];

                var meshAttributes = MeshAttributes.FromSubMesh(meshFilter.sharedMesh, materialIndex);
                var isAtlased = atlas.Metadata.MeshesUsingAtlas.Contains(meshAttributes);
                var materialAttributes = MaterialAttributes.FromMaterial(originalMaterial);
                if (isAtlased) {
                    materialAttributes = materialAttributes.WithTexture(atlas.Texture);
                }

                if (newMaterials.ContainsKey(materialAttributes)) {
                    continue;
                }
                
                Texture newMaterialTexture;
                if (isAtlased) {
                    newMaterialTexture = atlas.Texture;
                }
                else {
                    var newTextureName = $"{originalMaterial.mainTexture.name}_{originalMaterial.mainTexture.GetInstanceID()}.png";
                    newMaterialTexture = _assetEditor.GetOrCreateDuplicateTexture((Texture2D) originalMaterial.mainTexture,
                        _directories.TexturesDir.ToAssetPath(newTextureName));
                }

                var newMaterial = CreateMaterial(materialAttributes, newMaterialTexture);
                newMaterials[materialAttributes] = newMaterial;
            }
        }

        private Material CreateMaterial(MaterialAttributes materialAttributes, Texture newMaterialTexture) {
            // Create new atlased material
            var newMaterial = new Material(materialAttributes.Shader) {
                color = materialAttributes.Color,
                mainTexture = newMaterialTexture
            };

            var materialName = CreateMaterialName(materialAttributes);
            return _assetEditor.CreateAsset(newMaterial, _directories.MaterialsDir.ToAssetPath(materialName));
        }

        private string CreateMaterialName(MaterialAttributes materialAttributes) {
            // ReSharper disable once Unity.NoNullPropagation
            return $"Material-{materialAttributes.Shader.name.Replace('/', '-')}-" +
                   $"{(materialAttributes.MainTexture == null ? "empty" : materialAttributes.MainTexture.name + "-" + materialAttributes.MainTexture.GetInstanceID())}.mat";
        }
    }
}