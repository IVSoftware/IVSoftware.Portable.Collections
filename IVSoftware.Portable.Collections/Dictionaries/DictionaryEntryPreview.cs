namespace IVSoftware.Portable.Collections.Dictionaries
{
    public class DictionaryEntryPreview
    {
        public DictionaryEntryPreview(object key, object? value)
        {
            Key = key;
            Value = value;
        }
        public object Key { get; set; }
        public object? Value { get; set; }

        public override string ToString()
            => $"Key={Key} Value={Value}";
    }
}
