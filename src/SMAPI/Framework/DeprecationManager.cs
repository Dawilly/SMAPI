using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewModdingAPI.Framework
{
    /// <summary>Manages deprecation warnings.</summary>
    internal class DeprecationManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>The deprecations which have already been logged (as 'mod name::noun phrase::version').</summary>
        private readonly HashSet<string> LoggedDeprecations = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>Encapsulates monitoring and logging for a given module.</summary>
        private readonly IMonitor Monitor;

        /// <summary>Tracks the installed mods.</summary>
        private readonly ModRegistry ModRegistry;

        /// <summary>The queued deprecation warnings to display.</summary>
        private readonly IList<DeprecationWarning> QueuedWarnings = new List<DeprecationWarning>();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging for a given module.</param>
        /// <param name="modRegistry">Tracks the installed mods.</param>
        public DeprecationManager(IMonitor monitor, ModRegistry modRegistry)
        {
            this.Monitor = monitor;
            this.ModRegistry = modRegistry;
        }

        /// <summary>Log a deprecation warning for the old-style events.</summary>
        public void WarnForOldEvents()
        {
            this.Warn("legacy events", "2.9", DeprecationLevel.Info);
        }

        /// <summary>Log a deprecation warning.</summary>
        /// <param name="nounPhrase">A noun phrase describing what is deprecated.</param>
        /// <param name="version">The SMAPI version which deprecated it.</param>
        /// <param name="severity">How deprecated the code is.</param>
        public void Warn(string nounPhrase, string version, DeprecationLevel severity)
        {
            this.Warn(this.ModRegistry.GetFromStack()?.DisplayName, nounPhrase, version, severity);
        }

        /// <summary>Log a deprecation warning.</summary>
        /// <param name="source">The friendly mod name which used the deprecated code.</param>
        /// <param name="nounPhrase">A noun phrase describing what is deprecated.</param>
        /// <param name="version">The SMAPI version which deprecated it.</param>
        /// <param name="severity">How deprecated the code is.</param>
        public void Warn(string source, string nounPhrase, string version, DeprecationLevel severity)
        {
            // ignore if already warned
            if (!this.MarkWarned(source ?? "<unknown>", nounPhrase, version))
                return;

            // queue warning
            this.QueuedWarnings.Add(new DeprecationWarning(source, nounPhrase, version, severity, Environment.StackTrace));
        }

        /// <summary>Print any queued messages.</summary>
        public void PrintQueued()
        {
            foreach (DeprecationWarning warning in this.QueuedWarnings.OrderBy(p => p.ModName).ThenBy(p => p.NounPhrase))
            {
                // build message
#if SMAPI_3_0_STRICT
                string message = $"{warning.ModName ?? "An unknown mod"} uses deprecated code ({warning.NounPhrase} is deprecated since SMAPI {warning.Version}).";
#else
                string message = warning.NounPhrase == "legacy events"
                    ? $"{warning.ModName ?? "An unknown mod"} uses deprecated code (legacy events are deprecated since SMAPI {warning.Version})."
                    : $"{warning.ModName ?? "An unknown mod"} uses deprecated code ({warning.NounPhrase} is deprecated since SMAPI {warning.Version}).";
#endif
                if (warning.ModName == null)
                    message += $"{Environment.NewLine}{warning.StackTrace}";

                // log message
                switch (warning.Level)
                {
                    case DeprecationLevel.Notice:
                        this.Monitor.Log(message, LogLevel.Trace);
                        break;

                    case DeprecationLevel.Info:
                        this.Monitor.Log(message, LogLevel.Debug);
                        break;

                    case DeprecationLevel.PendingRemoval:
                        this.Monitor.Log(message, LogLevel.Warn);
                        break;

                    default:
                        throw new NotSupportedException($"Unknown deprecation level '{warning.Level}'.");
                }
            }
            this.QueuedWarnings.Clear();
        }

        /// <summary>Mark a deprecation warning as already logged.</summary>
        /// <param name="nounPhrase">A noun phrase describing what is deprecated (e.g. "the Extensions.AsInt32 method").</param>
        /// <param name="version">The SMAPI version which deprecated it.</param>
        /// <returns>Returns whether the deprecation was successfully marked as warned. Returns <c>false</c> if it was already marked.</returns>
        public bool MarkWarned(string nounPhrase, string version)
        {
            return this.MarkWarned(this.ModRegistry.GetFromStack()?.DisplayName, nounPhrase, version);
        }

        /// <summary>Mark a deprecation warning as already logged.</summary>
        /// <param name="source">The friendly name of the assembly which used the deprecated code.</param>
        /// <param name="nounPhrase">A noun phrase describing what is deprecated (e.g. "the Extensions.AsInt32 method").</param>
        /// <param name="version">The SMAPI version which deprecated it.</param>
        /// <returns>Returns whether the deprecation was successfully marked as warned. Returns <c>false</c> if it was already marked.</returns>
        public bool MarkWarned(string source, string nounPhrase, string version)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new InvalidOperationException("The deprecation source cannot be empty.");

            string key = $"{source}::{nounPhrase}::{version}";
            if (this.LoggedDeprecations.Contains(key))
                return false;
            this.LoggedDeprecations.Add(key);
            return true;
        }
    }
}
