namespace IVSoftware.Portable.Collections.Lists
{
    partial class ObservablePreviewCollection<T> : IOPAmbientBindingContext
    {
        /// <summary>
        /// Binding context to be injected and maintained into IOPAmbientBindingContext items.
        /// </summary>
        public object? AmbientBindingContext
        {
            get => _ambientBindingContext;
            set
            {
                if (!Equals(_ambientBindingContext, value))
                {
                    _ambientBindingContext = value;

                    // Initialize here. Then update on CollectionChanged.Add actions.
                    foreach (var cbc in this.OfType<IOPAmbientBindingContext>().ToArray())
                    {
                        cbc.AmbientBindingContext = value;
                    }
                    OnPropertyChanged();
                }
            }
        }
        object? _ambientBindingContext = default;
    }
}
