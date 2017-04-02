using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        public Model3DGroup RootModel { get; set; }
        public Dictionary<(int x,int y,int z),VoxelData> VoxelMap { get; private set; }
        public Dictionary<VoxelData, Model3D> VoxelToModel { get; private set; }
        public Dictionary<Model3D, VoxelData> ModelToVoxel { get; private set; }
        public Dictionary<Model3D, Material> OriginalMaterial { get; private set; }
        public List<Model3D> Highlighted { get; set; }
        public Model3D CursorModel { get; set; }
        public List<ColorInfo> Palette { get; set; }

        public int PaletteIndex { get; set; }

        private Color previewColor;

        public Color PreviewColor
        {
            get { return previewColor; }
        }

        public Color CurrentColor
        {
            get { return currentColor; }
            set
            {
                currentColor = value;
                previewColor = Color.FromArgb(0x80, currentColor.R, currentColor.G, currentColor.B);
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
            set
            {
                if(selection != null) { selection.SelectionChanged -= Selection_SelectionChanged; }
                selection = value;
                if(selection != null) { selection.SelectionChanged += Selection_SelectionChanged; };
            }
        }


        public MainViewModel()
        {
            Palette = (from PropertyInfo property in typeof(Colors).GetProperties()
                orderby property.Name
                select new ColorInfo(
                    property.Name,
                    (Color)property.GetValue(null, null))).ToList();
            CurrentColor = Palette[0].Color;
            RootModel = new Model3DGroup();
            Highlighted = new List<Model3D>();
            ModelToVoxel = new Dictionary<Model3D, VoxelData>();
            OriginalMaterial = new Dictionary<Model3D, Material>();
            VoxelMap = new Dictionary<(int x, int y, int z), VoxelData>();
            VoxelToModel = new Dictionary<VoxelData, Model3D>();
            VoxelMap.Add((0,0,0),new VoxelData(new Point3D(0, 0, 0), CurrentColor));
            UpdateModel();
        }

        private readonly XmlSerializer serializer = new XmlSerializer(typeof(List<VoxelData>), new[] { typeof(VoxelData) });

        public void Save(string fileName)
        {
            using (var w = XmlWriter.Create(fileName, new XmlWriterSettings { Indent = true }))
                serializer.Serialize(w, VoxelMap.Values.ToList());
        }

        public bool TryLoad(string fileName)
        {
            VoxelMap.Clear();
            try
            {
                using (var r = XmlReader.Create(fileName))
                {
                    var v = serializer.Deserialize(r);
                    List<VoxelData> voxels = (v as List<VoxelData>)?.Distinct().ToList();
                    int it = 0;
                    foreach(VoxelData voxel in voxels)
                    {
                        var key = ((int)voxel.Position.X, (int)voxel.Position.Y, (int)voxel.Position.Z);
                        if(VoxelMap.ContainsKey(key))
                        {
                            Debug.WriteLine("Duplicate Voxel at: " + it);
                            continue;
                        }
                        VoxelMap.Add(key, voxel);
                        it++;
                    }
                }
                UpdateModel();
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal void HandleClick(Point3D pRound)
        {
            if(isSelecting)
            {
                AddToSelection(pRound);
            }
            else
            {
                AddVoxel(pRound);
            }
        }

        private void Selection_SelectionChanged(Point3D x1, Vector3D x2)
        {
            ClearSelection();

            int xl = (int)Math.Max(x2.X,1);
            int yl = (int)Math.Max(x2.Y,1);
            int zl = (int)Math.Max(x2.Z,1);

            for (int x = 0; x < xl; x++)
            {
                for (int y = 0; y < yl; y++)
                {
                    for (int z = 0; z < zl; z++)
                    {
                        int posX = (int)x1.X + x;
                        int posY = (int)x1.Y + y;
                        int posZ = (int)x1.Z + z;
                        //If this position currently has a voxel, highlight it
                        if(VoxelMap.TryGetValue((posX,posY,posZ), out VoxelData voxel))
                        {
                            HighlightVoxel(VoxelToModel[voxel]);
                        }
                        //Create a preview
                        else
                        {
                            PreviewVoxel(new Point3D(posX, posY, posZ));
                        }
                    }
                }
            }
        }
        private void ClearSelection()
        {
            foreach(Model3D highlight in Highlighted)
            {
                if (ModelToVoxel.ContainsKey(highlight))
                {
                    if (highlight is GeometryModel3D geometry && OriginalMaterial.TryGetValue(highlight, out Material om))
                    {
                        geometry.Material = om;
                    }
                }
                else
                {
                    RootModel.Children.Remove(highlight);
                }
            }
        }
        
        private void AddToSelection(Point3D pRound)
        {
            if (CurrentSelection == null)
            {
                CurrentSelection = new SelectionViewModel();
            }
            CurrentSelection.AddPosition(pRound);
        }

        public void UpdateModel()
        {
            RootModel.Children.Clear();
            ModelToVoxel.Clear();
            OriginalMaterial.Clear();
            VoxelToModel.Clear();
            foreach (var v in VoxelMap)
            {
                var m = CreateVoxelModel3D(v.Value);
                OriginalMaterial.Add(m, m.Material);
                RootModel.Children.Add(m);
                ModelToVoxel.Add(m, v.Value);
                VoxelToModel.Add(v.Value, m);
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
        public void HandleNormalClick(Model3D source, Vector3D normal)
        {  
            if (!ModelToVoxel.ContainsKey(source))
                return;
            var v = ModelToVoxel[source];
            Point3D voxelPosition = v.Position + normal;
            if(isSelecting)
            {
                AddToSelection(voxelPosition);
            }
            else
            {
                AddVoxel(voxelPosition);
            }
        }

        /// <summary>
        /// Adds a voxel at the specified position.
        /// </summary>
        /// <param name="p">The p.</param>
        public void AddVoxel(Point3D p)
        {
            VoxelMap.Add(((int)p.X, (int)p.Y, (int)p.Z),new VoxelData(p, CurrentColor));
            UpdateModel();
        }

        /// <summary>
        /// Highlights the specified voxel model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void HighlightVoxel(Model3D model)
        {
            //foreach (GeometryModel3D m in Model.Children)
            //{
            //    if (!ModelToVoxel.ContainsKey(m))
            //        continue;
            //    var v = ModelToVoxel[m];
            //    var om = OriginalMaterial[m];

            //    // highlight color
            //    var hc = Color.FromArgb(0x80, v.Colour.R, v.Colour.G, v.Colour.B);
            //    m.Material = m == model ? MaterialHelper.CreateMaterial(hc) : om;
            //}


        }
        public void PreviewVoxel(Point3D position, bool isCursor = false)
        {
            var pv = new VoxelData(position, previewColor);

            if (isCursor)
            {
                if(CursorModel!= null)
                {
                    RootModel.Children.Remove(CursorModel);
                }

                CursorModel = CreateVoxelModel3D(pv);
                RootModel.Children.Add(CursorModel);
            }
            else
            {
                var model = CreateVoxelModel3D(pv);
                Highlighted.Add(model);
                RootModel.Children.Add(model);
            }
        }
        /// <summary>
        /// Shows a preview voxel adjacent to the specified model (source).
        /// If source is null, hide the preview.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="normal">The normal.</param>
        public void PreviewCursorVoxel(Model3D source, Vector3D normal = default(Vector3D))
        {
            if(source == null)
            {
                if(CursorModel != null)
                {
                    RootModel.Children.Remove(CursorModel);
                }
                return;
            }
            //Dont continue if we do not have the voxel
            if (!ModelToVoxel.ContainsKey(source))
                return;
            var v = ModelToVoxel[source];
            var pos = v.Position + normal;
            PreviewVoxel(pos, true);

        }

        public void Remove(Model3D model)
        {
            if (!ModelToVoxel.ContainsKey(model))
                return;
            var v = ModelToVoxel[model];
            RootModel.Children.Remove(model);
            VoxelMap.Remove(((int)v.Position.X, (int)v.Position.Y, (int)v.Position.Z));
        }

        public void Clear()
        {
            VoxelMap.Clear();
            UpdateModel();
        }
    }
}
