namespace IVSoftware.Portable.Collections.MSTest.Mutator
{
    public class BeforeItemCreateEventArgs : EventArgs
    {
        public BeforeItemCreateEventArgs(Type type, RandomOrNull rando, int index = -1)
        {
            if (Nullable.GetUnderlyingType(type) is { } uType)
            {
                NullableType = type;
                Type = uType;
            }
            else
            {
                Type = type;
            }
            switch (type)
            {
                case Type t when t == typeof(string):
                    Item =
                        rando.Next(26) is { } @int
                        ? NATO[@int]
                        : null;
                    break;
                case Type t when t == typeof(int?):
                    Item = rando.Next(maxValue: byte.MaxValue, inclusive: true);
                    break;
                case Type t when t == typeof(Guid):
                    Item = Guid.NewGuid();
                    break;
                default:
                    break;
            }
            Index = index;
        }
        public Type Type { get; }
        public Type? NullableType { get; }

        public bool IsNullable => NullableType is not null;
        public object? Item { get; set; }

        public int Index { get; }

        List<string> NATO = new List<string>
        {
            "Alpha",
            "Bravo",
            "Charlie",
            "Delta",
            "Echo",
            "Foxtrot",
            "Golf",
            "Hotel",
            "India",
            "Juliett",
            "Kilo",
            "Lima",
            "Mike",
            "November",
            "Oscar",
            "Papa",
            "Quebec",
            "Romeo",
            "Sierra",
            "Tango",
            "Uniform",
            "Victor",
            "Whiskey",
            "Xray",
            "Yankee",
            "Zulu"
        };

    }
}
