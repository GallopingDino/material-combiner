using System;
using System.Collections.Generic;
using Dino.MaterialCombiner.Meshes;
using UnityEngine;

namespace Dino.MaterialCombiner.Atlasing {
    internal class Atlas {
        public readonly Texture2D Texture;
        public readonly AtlasMetadata Metadata;

        public Atlas(Texture2D texture, AtlasMetadata metadata) {
            Texture = texture;
            Metadata = metadata;
        }

        public bool TryGetRegion(Material material, out AtlasRegion region) {
            if (Metadata.AtlasedTextureIndicesByMaterials.TryGetValue(material, out var index)) {
                region = new AtlasRegion(this, index);
                return true;
            }
            region = default;
            return false;
        }
    }
    
    internal class AtlasMetadata {
        public readonly Dictionary<Material, int> AtlasedTextureIndicesByMaterials = new Dictionary<Material, int>();
        public readonly List<Vector2Int> Tilings = new List<Vector2Int>();
        public readonly HashSet<MeshAttributes> MeshesUsingAtlas = new HashSet<MeshAttributes>();
        public readonly List<Texture2D> SubTextures = new List<Texture2D>();
        public Rect[] Rects;
    }

    internal readonly struct AtlasRegion : IEquatable<AtlasRegion> {
        private readonly Atlas _atlas;
        private readonly int _index;
        
        public bool IsEmpty => _atlas == null;
        public Rect Rect => _atlas.Metadata.Rects[_index];
        public Vector2Int Tiling => _atlas.Metadata.Tilings[_index];
        public Texture2D Atlas => _atlas.Texture;

        public AtlasRegion(Atlas atlas, int index) {
            _atlas = atlas;
            _index = index;
        }

        public bool Equals(AtlasRegion other) {
            return Equals(_atlas, other._atlas) && _index == other._index;
        }

        public override bool Equals(object obj) {
            return obj is AtlasRegion other && Equals(other);
        }

        public override int GetHashCode() {
            return unchecked(_atlas.GetHashCode() * 23 + _index);
        }
    }
}