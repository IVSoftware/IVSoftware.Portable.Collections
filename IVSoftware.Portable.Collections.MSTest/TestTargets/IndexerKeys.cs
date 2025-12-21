using System.Reflection;

namespace IVSoftware.Portable.Collections.MSTest.TestTargets
{
    [AbsoluteKeySegment]
    enum StdAbsoluteKeyDefault
    {
        SimpleClass,

        ButtonWindowsForms,

        ButtonMauiControls,

        ButtonWPF,
    }

    [AbsoluteKeySegment("Level1")]
    enum StdAbsoluteKeyWithString
    {
        SimpleClass,

        ButtonWindowsForms,

        ButtonMauiControls,

        ButtonWPF,
    }

    [AbsoluteKeySegment(typeof(Type), typeof(Object))]
    enum StdAbsoluteKeyWithType
    {
        SimpleClass,

        ButtonWindowsForms,

        ButtonMauiControls,

        ButtonWPF,
    }

    [RelativeKeySegment(typeof(Type), typeof(Object))]
    enum StdCacheReflectionStrongTyped
    {
        [RelativeKeySegment("Classes")]
        [StrongTypedDictionary(typeof(string), typeof(PropertyInfo))]
        SimpleClass,

        [RelativeKeySegment("Platform,Buttons")]
        [StrongTypedDictionary(typeof(string), typeof(PropertyInfo))]
        ButtonWindowsForms,

        [RelativeKeySegment("Platform","Buttons")]
        [StrongTypedDictionary(typeof(string), typeof(PropertyInfo))]
        ButtonMauiControls,

        [RelativeKeySegment("Platform","Buttons")]
        [StrongTypedDictionary(typeof(string), typeof(PropertyInfo))]
        ButtonWPF,
    }
}
