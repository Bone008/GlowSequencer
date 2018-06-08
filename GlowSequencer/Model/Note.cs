using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.Model
{
    public class Note : Observable
    {
        private string _label = null;
        private string _description = null;
        private float _time = 0;

        public string Label { get { return _label; } set { SetProperty(ref _label, value); } }
        public string Description { get { return _description; } set { SetProperty(ref _description, value); } }
        public float Time { get { return _time; } set { SetProperty(ref _time, value); } }
    }
}
