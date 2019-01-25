using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewModdingAPI.Framework.Networking {
    internal class Lease {
        private LeaseStatus _status;
        private DateTime timeStamp;

        public GameLocation Location { get; }

        public LeaseStatus Status {
            get {
                if (this.Location == null) return LeaseStatus.Invalid;
                return this._status;
            }
        }

        public Lease(GameLocation Location) {
            this.Location = Location;
            this._status = LeaseStatus.Pending;
        }

        public void Renew() {

        }
    }
}
