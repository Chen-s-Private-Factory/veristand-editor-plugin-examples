using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Xml.Linq;
using NationalInstruments.Hmi.Core.Controls.Models;
using NationalInstruments.SourceModel;
using NationalInstruments.SourceModel.Persistence;
using NationalInstruments.VeriStand.SourceModel.Screen;

namespace NationalInstruments.VeriStand.EngineExplodedView
{
    /// <summary>
    /// Registers the control in the VeriStand screen palette.
    /// </summary>
    [Export(typeof(ICustomVeriStandControl))]
    public class EngineExplodedViewModelExporter : ICustomVeriStandControl
    {
        /// <summary>
        /// MergeScript which defines what to drop on the screen from the palette, including the default size.
        /// </summary>
        public string Target =>
            "<pf:MergeScript xmlns:pf=\"http://www.ni.com/PlatformFramework\">" +
                "<pf:MergeItem>" +
                    "<" + EngineExplodedViewModel.XmlName + " xmlns=\"" + EngineNamespaceSchema.ParsableNamespaceName + "\" Width=\"[float]560\" Height=\"[float]400\"/>" +
                "</pf:MergeItem>" +
            "</pf:MergeScript>";

        /// <summary>
        /// Name of the control as it will appear in the palette
        /// </summary>
        public string Name => "Turbofan Exploded View";

        /// <summary>
        /// Path to the image to use in the palette
        /// </summary>
        public string ImagePath => "/NationalInstruments.VeriStand.EngineExplodedView;component/Resources/EngineIcon_32x32.png";

        /// <summary>
        /// Tool tip to display in the palette
        /// </summary>
        public string ToolTip => "Interactive 3D exploded view of a turbofan engine (HelixToolkit).";

        /// <summary>
        /// Unique id for the control. Must not overlap with existing controls or other custom controls.
        /// </summary>
        public string UniqueId => "TurbofanExplodedView";

        /// <summary>
        /// Puts the control in its own "3D Views" palette folder.
        /// </summary>
        public IList<PaletteElementCategory> PaletteHierarchy =>
            new List<PaletteElementCategory> { new PaletteElementCategory("3D Views", ImagePath, "EngineExplodedView3DViews", .1) };
    }

    /// <summary>
    /// Model of the exploded view control. The control does not bind to any channels, so the model only
    /// provides serialization (position and size come from <see cref="VisualModel"/>) and the view type.
    /// </summary>
    public class EngineExplodedViewModel : VisualModel
    {
        /// <summary>
        /// XML element name. Must match the name used in <see cref="EngineExplodedViewModelExporter.Target"/>.
        /// </summary>
        public const string XmlName = "TurbofanExplodedView";

        /// <inheritdoc/>
        public override XName XmlElementName => XName.Get(XmlName, EngineNamespaceSchema.ParsableNamespaceName);

        /// <inheritdoc/>
        public override XamlGenerationHelper XamlGenerationHelper => new EngineExplodedViewXamlHelper();

        /// <summary>
        /// Factory method used by the VeriStand parser to create the model.
        /// </summary>
        /// <param name="info">Information required to create the model, such as the parser.</param>
        /// <returns>A constructed and initialized model.</returns>
        [XmlParserFactoryMethod(XmlName, EngineNamespaceSchema.ParsableNamespaceName)]
        public static EngineExplodedViewModel Create(IElementCreateInfo info)
        {
            var model = new EngineExplodedViewModel();
#if MUTATE2020
            model.Initialize(info);
#else
            model.Init(info);
#endif
            return model;
        }

        private class EngineExplodedViewXamlHelper : XamlGenerationHelper
        {
            public override Type ControlType => typeof(EngineExplodedViewControl);
        }
    }
}
