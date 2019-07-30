using System;

namespace StardewModdingAPI.Web.Framework.ConfigModels
{
    /// <summary>The config settings for mod compatibility list.</summary>
    internal class MongoDbConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The MongoDB hostname.</summary>
        public string Host { get; set; }

        /// <summary>The MongoDB username (if any).</summary>
        public string Username { get; set; }

        /// <summary>The MongoDB password (if any).</summary>
        public string Password { get; set; }

        /// <summary>The database name.</summary>
        public string Database { get; set; }


        /*********
        ** Public method
        *********/
        /// <summary>Get the MongoDB connection string.</summary>
        public string GetConnectionString()
        {
            bool isLocal = this.Host == "localhost";
            bool hasLogin = !string.IsNullOrWhiteSpace(this.Username) && !string.IsNullOrWhiteSpace(this.Password);

            return $"mongodb{(isLocal ? "" : "+srv")}://"
                + (hasLogin ? $"{Uri.EscapeDataString(this.Username)}:{Uri.EscapeDataString(this.Password)}@" : "")
                + $"{this.Host}/{this.Database}?retryWrites=true&w=majority";
        }
    }
}
