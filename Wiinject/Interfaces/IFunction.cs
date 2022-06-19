using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiinject.Interfaces
{
    public interface IFunction
    {
        public string Name { get; set; }
        public uint EntryPoint { get; set; }
        bool Existing { get; }
    }
}
