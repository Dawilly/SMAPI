using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace StardewModdingAPI.Framework {
    internal class Shader : IShader {

        private IList<Effect> effects;
        private int selected;

        public Effect Apply { get {
                return this.effects[this.selected];
            }
        }

        public Shader() {
            this.effects = new List<Effect> {
                null
            };
            this.selected = 0;
        }

        public void Add(Effect shader) {
            this.effects.Add(shader);
            return;
        }

        public void Set(int index) {
            this.selected = (index < 0 || index >= this.effects.Count) ? 0 : index;
        }
    }
}
