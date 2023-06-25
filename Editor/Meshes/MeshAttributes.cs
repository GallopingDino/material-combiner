using System;
using UnityEngine;

namespace Dino.MaterialCombiner.Meshes {
    internal readonly struct MeshAttributes : IEquatable<MeshAttributes> {
        public readonly Mesh Mesh;
        public readonly int SubMeshIndex;
        
        public static MeshAttributes FromMesh(Mesh mesh) => new MeshAttributes(mesh, -1);
        
        public static MeshAttributes FromSubMesh(Mesh mesh, int subMeshIndex) => new MeshAttributes(mesh, subMeshIndex);

        private MeshAttributes(Mesh mesh, int subMeshIndex) {
            Mesh = mesh;
            SubMeshIndex = subMeshIndex;
        }

        public bool Equals(MeshAttributes other) {
            return Equals(Mesh, other.Mesh) && SubMeshIndex == other.SubMeshIndex;
        }

        public override bool Equals(object obj) {
            return obj is MeshAttributes other && Equals(other);
        }

        public override int GetHashCode() {
            return unchecked(Mesh.GetHashCode() * 23 + SubMeshIndex);
        }
    }
}