namespace IVSoftware.Portable.Collections
{
    public class BOOL
    {
        public static implicit operator bool(BOOL @this)
        => @this.Value;

         public BOOL(bool @bool, object? args = null)
        {
            Value = @bool;
            Args = args;
        }

        public bool Value { get; }
        public object? Args { get; }
    }
}
