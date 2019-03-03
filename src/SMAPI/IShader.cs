using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewModdingAPI {
    public interface IShader {
        Effect Apply { get; }
        void Add(Effect shader);
        void Set(int index);
    }
}
