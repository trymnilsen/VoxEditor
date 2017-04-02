using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml;
using System.Xml.Serialization;
using VoxEdit.Voxel;

namespace VoxEdit.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Color currentColor;
        private bool isSelecting;
        public List<VoxelData> Voxels { get; set; }
        public Model3DGroup Model { get; set; }

        public Dictionary<Model3D, VoxelData> ModelToVoxel { get; private set; }
        public Dictionary<Model3D, Material> OriginalMaterial { get; private set; }

        public List<Model3D> Highlighted { get; set; }
        public Model3D PreviewModel { get; set; }

        public List<ColorInfo> Palette { get; set; }

        public int PaletteIndex { get; set; }

        public Color CurrentColor
        {
            get { return currentColor; }
            set
            {
                currentColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentColor)));
            }
        }

        public bool IsSelecting
        {
            get { return isSelecting; }
            set
            {
                isSelecting = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelecting)));
            }
        }
        private SelectionViewModel selection;

        public SelectionViewModel CurrentSelection
        {
            get { return selection; }
            set { selection = value; }
        }


        public MainViewModel()
        {
            Palette = (from PropertyInfo property in typeof(Colors).GetProperties()
                orderby property.Name
                select new ColorInfo(
                    property.Name,
                    (Color)property.GetValue(null, null))).ToList();
            CurrentColor = Palette[0].Color;
            Model = new Model3DGroup();
            Voxels = new List<VoxelData>();
            Highlighted = new List<Model3D>();
            ModelToVoxel = new Dictionary<Model3D, VoxelData>();
            OriginalMaterial = new Dictionary<Model3D, Material>();
            Voxels.Add(new VoxelData(new Point3D(0, 0, 0), CurrentColor));
            UpdateModel();
        }

        private readonly XmlSerializer serializer = new XmlSerializer(typeof(List<VoxelData>), new[] { typeof(VoxelData) });

        public void Save(string fileName)
        {
            using (var w = XmlWriter.Create(fileName, new XmlWriterSettings { Indent = true }))
                serializer.Serialize(w, Voxels);
        }

        public bool TryLoad(string fileName)
        {
            try
            {
                using (var r = XmlReader.Create(fileName))
                {
                    var v = serializer.Deserialize(r);
                    Voxels = v as List<VoxelData>;
                }
                UpdateModel();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void UpdateModel()
        {
            Model.Children.Clear();
            ModelToVoxel.Clear();
            OriginalMaterial.Clear();
            foreach (var v in Voxels)
            {
                var m = CreateVoxelModel3D(v);
                OriginalMaterial.Add(m, m.Material);
                Model.Children.Add(m);
                ModelToVoxel.Add(m, v);
            }
            RaisePropertyChanged("Model");
        }

        private static GeometryModel3D CreateVoxelModel3D(VoxelData v)
        {
            const double size = 0.98;
            var m = new GeometryModel3D();
            var mb = new MeshBuilder();
            mb.AddBox(new Point3D(0, 0, 0), size, size, size);
            m.Geometry = mb.ToMesh();
            m.Material = MaterialHelper.CreateMaterial(v.Colour);
            m.Transform = new TranslateTransform3D(v.Position.X, v.Position.Y, v.Position.Z);
            return m;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        /// <summary>
        /// Adds the a voxel adjacent to the specified model.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="normal">The normal.</param>
        public void Add(Model3D source, Vector3D normal)
        {
            if (!ModelToVoxel.ContainsKey(source))
                return;
            var v = ModelToVoxel[source];
            AddVoxel(v.Position + normal);
        }

        /// <summary>
        /// Adds a voxel at the specified position.
        /// </summary>
        /// <param name="p">The p.</param>
        public void AddVoxel(Point3D p)
        {
            Voxels.Add(new VoxelData(p, CurrentColor));
            UpdateModel();
        }

        /// <summary>
        /// Highlights the specified voxel model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void HighlightVoxel(Model3D model)
        {
            foreach (GeometryModel3D m in Model.Children)
            {
                if (!ModelToVoxel.ContainsKey(m))
                    continue;
                var v = ModelToVoxel[m];
                var om = OriginalMaterial[m];

                // highlight color
                var hc = Color.FromArgb(0x80, v.Colour.R, v.Colour.G, v.Colour.B);
                m.Material = m == model ? MaterialHelper.CreateMaterial(hc) : om;
            }
        }

        /// <summary>
        /// Shows a preview voxel adjacent to the specified model (source).
        /// If source is null, hide the preview.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="normal">The normal.</param>
        public void PreviewVoxel(Model3D source, Vector3D normal = default(Vector3D))
        {
            if (PreviewModel != null)
                Model.Children.Remove(PreviewModel);
            PreviewModel = null;
            if (source == null)
                return;
            if (!ModelToVoxel.ContainsKey(source))
                return;
            var v = ModelToVoxel[source];
            var previewColor = Color.FromArgb(0x80, CurrentColor.R, CurrentColor.G, CurrentColor.B);
            var pv = new VoxelData(v.Position + normal, previewColor);
            PreviewModel = CreateVoxelModel3D(pv);
            Model.Children.Add(PreviewModel);
        }

        public void Remove(Model3D model)
        {
            if (!ModelToVoxel.ContainsKey(model))
                return;
            var v = ModelToVoxel[model];
            Voxels.Remove(v);
            UpdateModel();
        }

        public void Clear()
        {
            Voxels.Clear();
            UpdateModel();
        }
    }
}
