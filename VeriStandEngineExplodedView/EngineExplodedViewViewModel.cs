using NationalInstruments.Design;

namespace NationalInstruments.VeriStand.EngineExplodedView
{
    /// <summary>
    /// View model of the exploded view control. All interaction is local to the view, so it only creates the view.
    /// </summary>
    public class EngineExplodedViewViewModel : VisualViewModel
    {
        /// <summary>
        /// Constructs a new instance of the view model.
        /// </summary>
        /// <param name="model">The model associated with this view model.</param>
        public EngineExplodedViewViewModel(EngineExplodedViewModel model)
            : base(model)
        {
        }

        /// <inheritdoc/>
        public override object CreateView()
        {
            return new EngineExplodedViewControl();
        }
    }
}
