using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace VoxEdit.ViewModel
{
    public class SelectionViewModel : INotifyPropertyChanged
    {
        private Point3D position;
        private Vector3D size;
        private bool pointOneSet;

        public bool PointOneSet
        {
            get { return pointOneSet; }
            set { pointOneSet = value; }
        }
        //Points
        public Point3D Position
        {
            get { return position; }
            set
            {
                position = value;
                Invalidate();
            }
        }

        public Vector3D Size
        {
            get { return size; }
            set
            {
                size = value;
                Invalidate();
            }
        }
        //Helpers

        public void AddPosition(Point3D pRound)
        {
            if(pointOneSet)
            {
                int minX = (int)Math.Min(pRound.X, position.X);
                int minY = (int)Math.Min(pRound.Y, position.Y);
                int minZ = (int)Math.Min(pRound.Z, position.Z);

                int maxX = (int)Math.Max(pRound.X, position.X);
                int maxY = (int)Math.Max(pRound.Y, position.Y);
                int maxZ = (int)Math.Max(pRound.Z, position.Z);

                position = new Point3D(minX, minY, minZ);
                size = new Vector3D(maxX - minX, maxY - minY, maxZ - minZ);
                Invalidate();
            }
            else
            {
                Position = pRound;
                pointOneSet = true;
            }
        }
        private void Invalidate([System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
            SelectionChanged?.Invoke(position, size);
        }
        public delegate void SelectionChangedHandler(Point3D x1, Vector3D x2);
        public event SelectionChangedHandler SelectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
