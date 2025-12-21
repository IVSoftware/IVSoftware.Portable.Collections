using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IVSoftware.Portable.Collections.MSTest.TestTargets
{
    class Setting
    {
        public Setting() { }
        public Setting(string name, object? value)
        {
            Name = name;
            Value = value;
        }
        public string Name { get; } = "New Setting";
        public object? Value { get; set; }
    }
}
