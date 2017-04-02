using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace VoxEdit.Messages
{
    public class GridClicked
    {
        public Point3D Position { get; private set; }
        public GridClicked(Point3D point)
        {
            Position = point;
        }
    }
}
