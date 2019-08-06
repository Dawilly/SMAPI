using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewModdingAPI {
    /// <summary>Provides Shader Utilities to mod entries.</summary>
    public interface IShader {

        /*********
        ** Methods
        *********/
        /// <summary>Applies the set shader for post-processing.</summary>
        Effect Apply { get; }
        /// <summary>Adds a shader to the asset list.</summary>
        /// <param name="shader">The shader to be added, eventually applied.</param>
        void Add(Effect shader);
        /// <summary>Sets which shader to use for post-processing. Currently only supports one.</summary>
        /// <param name="index">List index where the shader is.</param>
        /// TO-DO: Refactor where the shaders are no longer global.
        void Set(int index);
    }
}
