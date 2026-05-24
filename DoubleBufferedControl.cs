using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Game
{
    internal class DoubleBufferedControl: Control
    {
        public DoubleBufferedControl()
        {
            this.DoubleBuffered = true;
        }
    }
}
