using System;
using UnityEngine;

namespace Dino.MaterialCombiner.Atlasing {
    internal readonly struct MaterialAttributes : IEquatable<MaterialAttributes> {
        public readonly Shader Shader;
        public readonly Color Color;
        public readonly Texture MainTexture;

        public MaterialAttributes(Shader shader, Texture mainTexture, Color color) {
            Shader = shader;
            MainTexture = mainTexture;
            Color = color;
        }

        public static MaterialAttributes FromMaterial(Material material) => new MaterialAttributes(material.shader, material.mainTexture, material.color);

        public MaterialAttributes WithTexture(Texture texture) => new MaterialAttributes(Shader, texture, Color);

        public bool Equals(MaterialAttributes other) {
            return Equals(Shader, other.Shader) && ReferenceEquals(MainTexture, other.MainTexture) && Color.Equals(other.Color);
        }

        public override bool Equals(object obj) {
            return obj is MaterialAttributes other && Equals(other);
        }

        public override int GetHashCode() {
            return unchecked((Shader.GetHashCode() * 23 + MainTexture.GetHashCode()) * 23 + Color.GetHashCode());
        }
    }
}