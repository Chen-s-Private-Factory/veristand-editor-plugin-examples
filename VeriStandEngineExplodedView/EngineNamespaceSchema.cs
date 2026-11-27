using System.ComponentModel.Composition;
using System.Reflection;
using NationalInstruments.DynamicProperties;
using NationalInstruments.SourceModel.Persistence;

namespace NationalInstruments.VeriStand.EngineExplodedView
{
    /// <summary>
    /// Defines the serialization namespace and version of this plugin and allows for mutation if it is upgraded.
    /// </summary>
    [ParsableNamespaceSchema(ParsableNamespaceName, CurrentVersion)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class EngineNamespaceSchema : NamespaceSchema
    {
        /// <summary>
        /// This must be unique among all plugins.
        /// </summary>
        public const string ParsableNamespaceName = "http://www.your-company.com/VeriStandEngineExplodedView";

        /// <summary>
        /// The current version
        /// </summary>
        private const string CurrentVersion = "1.0.0f0";

        /// <summary>
        /// Default Constructor
        /// </summary>
        public EngineNamespaceSchema() : base(Assembly.GetExecutingAssembly())
        {
            Version = VersionExtensions.Parse(CurrentVersion);
            OldestCompatibleVersion = VersionExtensions.Parse(CurrentVersion);
        }

        /// <inheritdoc/>
        public override string FeatureSetName => "Engine Exploded View Control";

        /// <inheritdoc/>
        public override string NamespaceName => ParsableNamespaceName;
    }
}
