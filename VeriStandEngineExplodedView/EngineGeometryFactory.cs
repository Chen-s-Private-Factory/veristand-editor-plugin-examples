using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace NationalInstruments.VeriStand.EngineExplodedView
{
    /// <summary>
    /// Procedurally builds a simplified high bypass turbofan engine as a set of <see cref="EngineComponent"/>s.
    /// The engine axis is the X axis, the intake points to -X and the exhaust to +X.
    /// Profiles are expressed as 2D points where X is the axial position and Y is the radius.
    /// </summary>
    public static class EngineGeometryFactory
    {
        private const int FullSweepSegments = 72;
        private const int HalfSweepSegments = 36;

        /// <summary>
        /// Axial position around which the core parts spread apart when exploding.
        /// </summary>
        private const double ExplodePivotX = -0.3;

        /// <summary>
        /// How much the axial distance between parts grows when fully exploded.
        /// </summary>
        private const double AxialSpread = 1.0;

        /// <summary>
        /// Creates all engine components in assembly order.
        /// </summary>
        /// <returns>The engine components.</returns>
        public static IList<EngineComponent> CreateEngine()
        {
            return new List<EngineComponent>
            {
                CreateSpinner(),
                CreateFan(),
                CreateBooster(),
                CreateHighPressureCompressor(),
                CreateCombustor(),
                CreateHighPressureTurbine(),
                CreateLowPressureTurbine(),
                CreateExhaust(),
                CreateShaft(),
                CreateNacelleHalf(upper: true),
                CreateNacelleHalf(upper: false),
                CreateCoreCowlHalf(upper: true),
                CreateCoreCowlHalf(upper: false)
            };
        }

        private static EngineComponent CreateSpinner()
        {
            var component = new EngineComponent(
                "Spinner",
                "Aerodynamic nose cone that guides air smoothly into the fan.",
                AxialOffset(-5.1),
                new Point3D(-5.1, 1.0, 0),
                EngineSpool.LowPressure);
            var builder = new MeshBuilder(false, false);
            AddRevolvedStrip(builder, Profile(-5.6, 0.0, -5.4, 0.3, -5.15, 0.52, -4.85, 0.65, -4.6, 0.7), 0, 360, FullSweepSegments, false);
            AddRevolvedStrip(builder, Profile(-4.6, 0.7, -4.6, 0.0), 0, 360, FullSweepSegments, false);
            component.AddMesh(builder.ToMesh(true), CreateMaterial(Color.FromRgb(0x3A, 0x3F, 0x47)));
            return component;
        }

        private static EngineComponent CreateFan()
        {
            var component = new EngineComponent(
                "Fan",
                "Wide chord fan. Produces most of the thrust by accelerating the bypass flow.",
                AxialOffset(-4.3),
                new Point3D(-4.3, 2.4, 0),
                EngineSpool.LowPressure);
            var hub = new MeshBuilder(false, false);
            AddDrum(hub, -4.6, 0.7, -4.0, 0.75, 0.2);
            component.AddMesh(hub.ToMesh(true), CreateMaterial(Color.FromRgb(0x8A, 0x94, 0x9E)));

            var blades = new MeshBuilder(false, false);
            AddBladeRing(blades, 22, -4.3, 0.72, 1.95, 0.5, 0.04, 62, 22, 0);
            component.AddMesh(blades.ToMesh(true), CreateMaterial(Color.FromRgb(0xB8, 0xC2, 0xCC)));
            return component;
        }

        private static EngineComponent CreateBooster()
        {
            var component = new EngineComponent(
                "LP Compressor",
                "Booster stages on the low pressure spool that pre-compress the core flow.",
                AxialOffset(-3.45),
                new Point3D(-3.45, 1.7, 0),
                EngineSpool.LowPressure);
            var drum = new MeshBuilder(false, false);
            AddDrum(drum, -3.9, 0.72, -3.0, 0.8, 0.2);
            component.AddMesh(drum.ToMesh(true), CreateMaterial(Color.FromRgb(0x5E, 0x6B, 0x78)));

            var blades = new MeshBuilder(false, false);
            double[] stages = { -3.75, -3.45, -3.15 };
            for (int i = 0; i < stages.Length; i++)
            {
                double hubRadius = Lerp(0.73, 0.79, i / (double)(stages.Length - 1));
                AddBladeRing(blades, 36, stages[i], hubRadius, 1.28, 0.16, 0.025, 45, 35, i * 3.0);
            }
            component.AddMesh(blades.ToMesh(true), CreateMaterial(Color.FromRgb(0x7F, 0xA7, 0xC9)));
            return component;
        }

        private static EngineComponent CreateHighPressureCompressor()
        {
            var component = new EngineComponent(
                "HP Compressor",
                "Multi-stage axial compressor on the high pressure spool.",
                AxialOffset(-1.95),
                new Point3D(-1.95, 1.6, 0),
                EngineSpool.HighPressure);
            var drum = new MeshBuilder(false, false);
            AddDrum(drum, -2.9, 0.8, -1.0, 0.95, 0.25);
            component.AddMesh(drum.ToMesh(true), CreateMaterial(Color.FromRgb(0x6E, 0x7B, 0x88)));

            var blades = new MeshBuilder(false, false);
            const int stageCount = 7;
            for (int i = 0; i < stageCount; i++)
            {
                double t = i / (double)(stageCount - 1);
                double x = Lerp(-2.75, -1.15, t);
                AddBladeRing(blades, 44, x, Lerp(0.82, 0.94, t), Lerp(1.28, 1.05, t), 0.13, 0.02, 40, 30, i * 2.0);
            }
            component.AddMesh(blades.ToMesh(true), CreateMaterial(Color.FromRgb(0xA9, 0xB7, 0xC6)));
            return component;
        }

        private static EngineComponent CreateCombustor()
        {
            var component = new EngineComponent(
                "Combustor",
                "Annular combustion chamber where fuel is injected and burned.",
                AxialOffset(-0.3),
                new Point3D(-0.3, 1.7, 0),
                EngineSpool.None);
            var liner = new MeshBuilder(false, false);
            var outerWall = Profile(-0.9, 1.0, -0.7, 1.18, -0.3, 1.22, 0.3, 1.15);
            var innerWall = Profile(-0.9, 0.95, -0.7, 0.78, -0.3, 0.74, 0.3, 0.86);
            AddRevolvedStrip(liner, outerWall, 0, 360, FullSweepSegments, false);
            AddRevolvedStrip(liner, innerWall, 0, 360, FullSweepSegments, true);
            AddRevolvedStrip(liner, Profile(-0.9, 0.95, -1.0, 0.975, -0.9, 1.0), 0, 360, FullSweepSegments, false);
            AddRevolvedStrip(liner, Profile(0.3, 0.86, 0.3, 1.15), 0, 360, FullSweepSegments, true);
            component.AddMesh(liner.ToMesh(true), CreateMaterial(Color.FromRgb(0xD9, 0x82, 0x2B)));

            var nozzles = new MeshBuilder(false, false);
            const int nozzleCount = 20;
            for (int i = 0; i < nozzleCount; i++)
            {
                double angle = 2 * Math.PI * i / nozzleCount;
                nozzles.AddCylinder(OnRadius(-1.3, 0.975, angle), OnRadius(-0.97, 0.975, angle), 0.035, 12, true, true);
            }
            component.AddMesh(nozzles.ToMesh(true), CreateMaterial(Color.FromRgb(0xB0, 0xB0, 0xB0)));
            return component;
        }

        private static EngineComponent CreateHighPressureTurbine()
        {
            var component = new EngineComponent(
                "HP Turbine",
                "Extracts energy from the hot gas to drive the HP compressor.",
                AxialOffset(0.675),
                new Point3D(0.675, 1.6, 0),
                EngineSpool.HighPressure);
            var disc = new MeshBuilder(false, false);
            AddDrum(disc, 0.4, 0.9, 0.95, 0.92, 0.25);
            component.AddMesh(disc.ToMesh(true), CreateMaterial(Color.FromRgb(0x7A, 0x6A, 0x50)));

            var blades = new MeshBuilder(false, false);
            AddBladeRing(blades, 52, 0.55, 0.91, 1.22, 0.14, 0.03, -35, -25, 0);
            AddBladeRing(blades, 52, 0.82, 0.92, 1.24, 0.14, 0.03, -35, -25, 3.5);
            component.AddMesh(blades.ToMesh(true), CreateMaterial(Color.FromRgb(0xE0, 0xB0, 0x40)));
            return component;
        }

        private static EngineComponent CreateLowPressureTurbine()
        {
            var component = new EngineComponent(
                "LP Turbine",
                "Multi-stage turbine that drives the fan and booster through the LP shaft.",
                AxialOffset(1.75),
                new Point3D(1.75, 2.0, 0),
                EngineSpool.LowPressure);
            var drum = new MeshBuilder(false, false);
            AddDrum(drum, 1.05, 0.95, 2.45, 1.0, 0.2);
            component.AddMesh(drum.ToMesh(true), CreateMaterial(Color.FromRgb(0x6B, 0x55, 0x40)));

            var blades = new MeshBuilder(false, false);
            const int stageCount = 5;
            for (int i = 0; i < stageCount; i++)
            {
                double t = i / (double)(stageCount - 1);
                AddBladeRing(blades, 60, Lerp(1.2, 2.3, t), Lerp(0.96, 1.0, t), Lerp(1.32, 1.58, t), 0.14, 0.025, -40, -30, i * 2.5);
            }
            component.AddMesh(blades.ToMesh(true), CreateMaterial(Color.FromRgb(0xB8, 0x73, 0x3A)));
            return component;
        }

        private static EngineComponent CreateExhaust()
        {
            var component = new EngineComponent(
                "Exhaust Nozzle",
                "Core exhaust nozzle and plug that expand the hot gas to produce thrust.",
                AxialOffset(3.5),
                new Point3D(3.8, 1.8, 0),
                EngineSpool.None);
            var nozzle = new MeshBuilder(false, false);
            AddShell(nozzle, Profile(2.5, 1.72, 3.5, 1.55, 4.4, 1.25), Profile(2.5, 1.63, 3.5, 1.47, 4.4, 1.18), 0, 360, FullSweepSegments);
            component.AddMesh(nozzle.ToMesh(true), CreateMaterial(Color.FromRgb(0x4A, 0x4F, 0x55)));

            var plug = new MeshBuilder(false, false);
            AddRevolvedStrip(plug, Profile(2.45, 0.0, 2.45, 0.95), 0, 360, FullSweepSegments, false);
            AddRevolvedStrip(plug, Profile(2.45, 0.95, 3.4, 0.88, 4.2, 0.5, 4.75, 0.15, 4.9, 0.0), 0, 360, FullSweepSegments, false);
            component.AddMesh(plug.ToMesh(true), CreateMaterial(Color.FromRgb(0x8A, 0x72, 0x5C)));
            return component;
        }

        private static EngineComponent CreateShaft()
        {
            var component = new EngineComponent(
                "LP Shaft",
                "Connects the LP turbine to the fan and booster.",
                new Vector3D(0, 0, 0),
                new Point3D(-1.0, -0.6, 0),
                EngineSpool.LowPressure);
            var builder = new MeshBuilder(false, false);
            builder.AddCylinder(new Point3D(-4.6, 0, 0), new Point3D(2.45, 0, 0), 0.18, 36, true, true);
            component.AddMesh(builder.ToMesh(true), CreateMaterial(Color.FromRgb(0xC0, 0xC0, 0xC0)));
            return component;
        }

        private static EngineComponent CreateNacelleHalf(bool upper)
        {
            // y = r * cos(theta), so [-90, 90] is the upper half and [90, 270] the lower half.
            double start = upper ? -90 : 90;
            var component = new EngineComponent(
                upper ? "Nacelle (upper)" : "Nacelle (lower)",
                "Fan cowl and bypass duct outer wall. Split to show the engine core.",
                new Vector3D(-0.5, upper ? 4.6 : -4.6, 0),
                new Point3D(-2.2, upper ? 2.75 : -2.75, 0),
                EngineSpool.None);
            var builder = new MeshBuilder(false, false);
            AddShell(
                builder,
                Profile(-4.95, 2.12, -4.75, 2.28, -4.3, 2.36, -2.0, 2.34, 0.6, 2.08),
                Profile(-4.95, 2.08, -4.75, 2.04, -4.3, 2.02, -2.0, 2.04, 0.6, 1.98),
                start,
                start + 180,
                HalfSweepSegments);
            component.AddMesh(builder.ToMesh(true), CreateMaterial(Color.FromRgb(0xDD, 0xE3, 0xE8)));
            return component;
        }

        private static EngineComponent CreateCoreCowlHalf(bool upper)
        {
            // Split like the nacelle but with a smaller offset, so the two casings open in nested layers.
            double start = upper ? -90 : 90;
            var component = new EngineComponent(
                upper ? "Core Casing (upper)" : "Core Casing (lower)",
                "Casing around the gas generator, holding the stator vanes.",
                new Vector3D(0, upper ? 2.3 : -2.3, 0),
                new Point3D(-1.2, upper ? 2.0 : -2.0, 0),
                EngineSpool.None);
            var builder = new MeshBuilder(false, false);
            AddShell(
                builder,
                Profile(-3.95, 1.55, -3.0, 1.7, 1.0, 1.76, 2.5, 1.72),
                Profile(-3.95, 1.32, -3.0, 1.33, 1.0, 1.66, 2.5, 1.64),
                start,
                start + 180,
                HalfSweepSegments);
            component.AddMesh(builder.ToMesh(true), CreateMaterial(Color.FromRgb(0x59, 0x63, 0x6E)));
            return component;
        }

        private static Vector3D AxialOffset(double assembledCenterX)
        {
            return new Vector3D((assembledCenterX - ExplodePivotX) * AxialSpread, 0, 0);
        }

        /// <summary>
        /// Adds a rotor drum: an outer hub surface with front and rear discs down to the bore radius.
        /// </summary>
        private static void AddDrum(MeshBuilder builder, double x0, double r0, double x1, double r1, double boreRadius)
        {
            AddRevolvedStrip(builder, Profile(x0, r0, x1, r1), 0, 360, FullSweepSegments, false);
            AddRevolvedStrip(builder, Profile(x0, boreRadius, x0, r0), 0, 360, FullSweepSegments, false);
            AddRevolvedStrip(builder, Profile(x1, r1, x1, boreRadius), 0, 360, FullSweepSegments, false);
        }

        /// <summary>
        /// Adds a thin walled body of revolution defined by an outer and an inner profile with the same number of points.
        /// When the sweep is partial the cut faces are closed.
        /// </summary>
        private static void AddShell(MeshBuilder builder, IList<Point> outer, IList<Point> inner, double startDegrees, double endDegrees, int segments)
        {
            AddRevolvedStrip(builder, outer, startDegrees, endDegrees, segments, false);
            AddRevolvedStrip(builder, inner, startDegrees, endDegrees, segments, true);
            AddRevolvedStrip(builder, new[] { inner[0], outer[0] }, startDegrees, endDegrees, segments, false);
            int last = outer.Count - 1;
            AddRevolvedStrip(builder, new[] { outer[last], inner[last] }, startDegrees, endDegrees, segments, false);

            if (Math.Abs(endDegrees - startDegrees) < 360)
            {
                foreach (double angleDegrees in new[] { startDegrees, endDegrees })
                {
                    double angle = angleDegrees * Math.PI / 180;
                    for (int i = 0; i < last; i++)
                    {
                        builder.AddQuad(
                            OnRadius(outer[i].X, outer[i].Y, angle),
                            OnRadius(outer[i + 1].X, outer[i + 1].Y, angle),
                            OnRadius(inner[i + 1].X, inner[i + 1].Y, angle),
                            OnRadius(inner[i].X, inner[i].Y, angle));
                    }
                }
            }
        }

        /// <summary>
        /// Revolves an open 2D profile around the X axis. Vertices are shared inside the strip so WPF generates smooth normals
        /// around the circumference, while separate strips keep crisp edges between them.
        /// </summary>
        private static void AddRevolvedStrip(MeshBuilder builder, IList<Point> profile, double startDegrees, double endDegrees, int segments, bool flip)
        {
            var positions = builder.Positions;
            var indices = builder.TriangleIndices;
            int baseIndex = positions.Count;
            int columns = profile.Count;
            for (int s = 0; s <= segments; s++)
            {
                double angle = (startDegrees + ((endDegrees - startDegrees) * s / segments)) * Math.PI / 180;
                foreach (Point p in profile)
                {
                    positions.Add(OnRadius(p.X, p.Y, angle));
                }
            }

            for (int s = 0; s < segments; s++)
            {
                for (int j = 0; j < columns - 1; j++)
                {
                    int a = baseIndex + (s * columns) + j;
                    int b = a + 1;
                    int c = a + columns;
                    int d = c + 1;
                    if (flip)
                    {
                        AddTriangle(indices, a, b, c);
                        AddTriangle(indices, b, d, c);
                    }
                    else
                    {
                        AddTriangle(indices, a, c, b);
                        AddTriangle(indices, b, c, d);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a ring of twisted blades. Each blade is a thin box swept from hub to tip whose chord rotates
        /// linearly from <paramref name="hubTwistDegrees"/> to <paramref name="tipTwistDegrees"/>.
        /// </summary>
        private static void AddBladeRing(
            MeshBuilder builder,
            int count,
            double x,
            double hubRadius,
            double tipRadius,
            double chord,
            double thickness,
            double hubTwistDegrees,
            double tipTwistDegrees,
            double phaseDegrees)
        {
            const int spanSections = 3;
            for (int k = 0; k < count; k++)
            {
                double bladeAngle = (phaseDegrees * Math.PI / 180) + (2 * Math.PI * k / count);
                var sections = new Point3D[spanSections + 1][];
                for (int s = 0; s <= spanSections; s++)
                {
                    double t = s / (double)spanSections;
                    double radius = Lerp(hubRadius, tipRadius, t);
                    double twist = Lerp(hubTwistDegrees, tipTwistDegrees, t) * Math.PI / 180;
                    double halfChord = chord * Lerp(1.0, 1.15, t) / 2;
                    double halfThickness = thickness / 2;

                    // Blade local frame: X axial, Y radial, Z tangential. The chord is rotated around the radial axis.
                    var chordDir = new Vector3D(Math.Cos(twist), 0, Math.Sin(twist));
                    var normalDir = new Vector3D(-Math.Sin(twist), 0, Math.Cos(twist));
                    var center = new Point3D(x, radius, 0);
                    sections[s] = new[]
                    {
                        RotateAboutX(center - (chordDir * halfChord) - (normalDir * halfThickness), bladeAngle),
                        RotateAboutX(center + (chordDir * halfChord) - (normalDir * halfThickness), bladeAngle),
                        RotateAboutX(center + (chordDir * halfChord) + (normalDir * halfThickness), bladeAngle),
                        RotateAboutX(center - (chordDir * halfChord) + (normalDir * halfThickness), bladeAngle)
                    };
                }

                for (int s = 0; s < spanSections; s++)
                {
                    Point3D[] lower = sections[s];
                    Point3D[] upper = sections[s + 1];
                    for (int e = 0; e < 4; e++)
                    {
                        int n = (e + 1) % 4;
                        builder.AddQuad(lower[e], lower[n], upper[n], upper[e]);
                    }
                }

                Point3D[] tip = sections[spanSections];
                builder.AddQuad(tip[3], tip[2], tip[1], tip[0]);
            }
        }

        private static void AddTriangle(IList<int> indices, int a, int b, int c)
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }

        private static Point3D RotateAboutX(Point3D p, double angle)
        {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return new Point3D(p.X, (p.Y * cos) - (p.Z * sin), (p.Y * sin) + (p.Z * cos));
        }

        private static Point3D OnRadius(double x, double radius, double angle)
        {
            return new Point3D(x, radius * Math.Cos(angle), radius * Math.Sin(angle));
        }

        private static IList<Point> Profile(params double[] xr)
        {
            var points = new List<Point>(xr.Length / 2);
            for (int i = 0; i < xr.Length; i += 2)
            {
                points.Add(new Point(xr[i], xr[i + 1]));
            }
            return points;
        }

        private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

        private static Material CreateMaterial(Color color)
        {
            var material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            material.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF)), 40));
            material.Freeze();
            return material;
        }
    }
}
