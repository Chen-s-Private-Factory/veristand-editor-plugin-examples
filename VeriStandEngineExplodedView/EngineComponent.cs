using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NationalInstruments.VeriStand.EngineExplodedView
{
    /// <summary>
    /// Identifies which spool a rotating component belongs to. Each spool spins at its own speed.
    /// </summary>
    public enum EngineSpool
    {
        /// <summary>Static part (casings, combustor, nozzle).</summary>
        None,

        /// <summary>Low pressure spool (fan, booster, LP turbine, shaft).</summary>
        LowPressure,

        /// <summary>High pressure spool (HP compressor, HP turbine).</summary>
        HighPressure
    }

    /// <summary>
    /// A single part of the engine in the exploded view. Owns its 3D visual and the transforms
    /// used to move it outward (explode) and spin it around the engine axis.
    /// This class has no dependency on VeriStand so the 3D view can be reused or tested on its own.
    /// </summary>
    public sealed class EngineComponent
    {
        private readonly List<GeometryModel3D> _geometries = new List<GeometryModel3D>();
        private readonly Dictionary<GeometryModel3D, Material> _originalMaterials = new Dictionary<GeometryModel3D, Material>();
        private readonly TranslateTransform3D _translation = new TranslateTransform3D();
        private readonly AxisAngleRotation3D _spin = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0);
        private bool _isHighlighted;

        /// <summary>
        /// Creates a component.
        /// </summary>
        /// <param name="name">Display name of the part.</param>
        /// <param name="description">Short description shown when the part is selected.</param>
        /// <param name="explodeOffset">Offset applied to the part when the explode factor is 1.</param>
        /// <param name="labelAnchor">Assembled position where the label of the part is drawn.</param>
        /// <param name="spool">Spool the part belongs to, which defines whether and how fast it spins.</param>
        public EngineComponent(string name, string description, Vector3D explodeOffset, Point3D labelAnchor, EngineSpool spool)
        {
            Name = name;
            Description = description;
            ExplodeOffset = explodeOffset;
            LabelAnchor = labelAnchor;
            Spool = spool;

            var transforms = new Transform3DGroup();
            transforms.Children.Add(new RotateTransform3D(_spin));
            transforms.Children.Add(_translation);
            Visual = new ModelVisual3D { Content = new Model3DGroup(), Transform = transforms };
        }

        /// <summary>Gets the display name of the part.</summary>
        public string Name { get; }

        /// <summary>Gets the description of the part.</summary>
        public string Description { get; }

        /// <summary>Gets the offset applied when fully exploded.</summary>
        public Vector3D ExplodeOffset { get; }

        /// <summary>Gets the assembled label position.</summary>
        public Point3D LabelAnchor { get; }

        /// <summary>Gets the spool of the part.</summary>
        public EngineSpool Spool { get; }

        /// <summary>Gets the visual to add to the 3D scene.</summary>
        public ModelVisual3D Visual { get; }

        /// <summary>Gets the translation applied by the explode factor. Can be shared with other visuals such as labels.</summary>
        public TranslateTransform3D Translation => _translation;

        /// <summary>
        /// Adds a mesh to the part.
        /// </summary>
        /// <param name="mesh">Mesh in assembled engine coordinates.</param>
        /// <param name="material">Material of the mesh.</param>
        public void AddMesh(MeshGeometry3D mesh, Material material)
        {
            var geometry = new GeometryModel3D(mesh, material) { BackMaterial = material };
            _geometries.Add(geometry);
            _originalMaterials[geometry] = material;
            ((Model3DGroup)Visual.Content).Children.Add(geometry);
        }

        /// <summary>
        /// Moves the part according to the explode factor.
        /// </summary>
        /// <param name="factor">0 means assembled, 1 means fully exploded.</param>
        public void ApplyExplodeFactor(double factor)
        {
            _translation.OffsetX = ExplodeOffset.X * factor;
            _translation.OffsetY = ExplodeOffset.Y * factor;
            _translation.OffsetZ = ExplodeOffset.Z * factor;
        }

        /// <summary>
        /// Sets the rotation angle of the part around the engine axis. Ignored for static parts.
        /// </summary>
        /// <param name="angleDegrees">Spin angle in degrees.</param>
        public void SetSpinAngle(double angleDegrees)
        {
            if (Spool != EngineSpool.None)
            {
                _spin.Angle = angleDegrees % 360.0;
            }
        }

        /// <summary>
        /// Gets or sets whether the part is drawn highlighted (selected).
        /// </summary>
        public bool IsHighlighted
        {
            get { return _isHighlighted; }
            set
            {
                if (_isHighlighted == value)
                {
                    return;
                }
                _isHighlighted = value;
                foreach (var geometry in _geometries)
                {
                    Material original = _originalMaterials[geometry];
                    Material material = value ? CreateHighlight(original) : original;
                    geometry.Material = material;
                    geometry.BackMaterial = material;
                }
            }
        }

        /// <summary>
        /// Returns true when the given visual belongs to this part.
        /// </summary>
        /// <param name="visual">Visual returned by a hit test.</param>
        public bool Owns(Visual3D visual) => ReferenceEquals(visual, Visual);

        private static Material CreateHighlight(Material original)
        {
            var group = new MaterialGroup();
            group.Children.Add(original);
            group.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(0x30, 0x70, 0xC0))));
            group.Freeze();
            return group;
        }
    }
}
