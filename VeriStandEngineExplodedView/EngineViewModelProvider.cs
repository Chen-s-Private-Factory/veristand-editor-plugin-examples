using System.ComponentModel.Composition;
using NationalInstruments.Composition;
using NationalInstruments.Shell;
using NationalInstruments.VeriStand.Design.Screen;

namespace NationalInstruments.VeriStand.EngineExplodedView
{
    /// <summary>
    /// Exports the view models (and associated models) of this assembly to the VeriStand screen surface.
    /// </summary>
    [PartMetadata(ExportIdentifier.RootContainerKey, "")]
    [ExportProvideViewModels(typeof(ScreenEditControl), supportedModels: "NationalInstruments.VeriStand.EngineExplodedView")]
    public class EngineViewModelProvider : ViewModelProvider
    {
        /// <inheritdoc />
        protected override void AddSupportedModels()
        {
            AddSupportedModel<EngineExplodedViewModel>(e => new EngineExplodedViewViewModel(e));
        }
    }
}
