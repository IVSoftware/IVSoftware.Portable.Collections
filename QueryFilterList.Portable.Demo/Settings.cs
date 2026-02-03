using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Dictionaries;
using IVSoftware.Portable.Xml.Linq.XBoundObject;
using IVSoftware.Portable.Xml.Linq.XBoundObject.Modeling;
using OPC.Preview.Portable;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryFilterList.Portable.Demo
{
    public enum StdSetting
    {
        AllowPluralize,
        ShowCheckedStateGroup,
    }
    public class Settings : TolerantDictionary<string, object>, ISettingsSource
    {
        // #{1F869F84-35E1-4345-B652-0063DFCC1F0A}
        public Settings() 
        {
            this[StdSetting.AllowPluralize] = true;
            this[StdSetting.ShowCheckedStateGroup] = ShowCheckedStateGroup.All;
        }
        public object? this[Enum key]
        {
            get => base[key.ToString()];
            set => base[key.ToString()] = value;
        }
        protected override void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
        }
    }
}
