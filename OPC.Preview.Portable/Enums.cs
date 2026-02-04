using IVSoftware.Portable;
using IVSoftware.Portable.Collections;
using IVSoftware.Portable.Collections.Common;
using static IVSoftware.Portable.GlyphProvider;

namespace OPC.Preview.Portable
{
    public enum OPReserved
    {
        DefaultId = -1,

        Uninitialized = -2,
    }
    public enum HelpCommand
    {
        [Glyph(typeof(IconBasics), nameof(IconBasics.HelpCircledAlt))]
        Help,
    }

    /// <summary>
    /// Group where [+] stays centered regardless of the visibility of other icons.
    /// </summary>
    public enum EditingCommands
    {
        [Glyph(typeof(IconBasics), "Edit")]
        [VisibilityPredicate(VisibilityPredicateFlag.Single)]
        Edit,

        [Glyph(typeof(IconBasics), "Add")]
        [VisibilityPredicate(VisibilityPredicateFlag.Always)]
        Add,

        [Glyph(typeof(IconBasics), "Delete")]
        [VisibilityPredicate(VisibilityPredicateFlag.Single | VisibilityPredicateFlag.Multiple)]
        Delete,
    }

    [Group("Check Actions")]
    public enum SetCheckedGroup
    {
        CheckAll,
        UncheckAll,
    }

    [Group("Filter by Checked", GroupBoxItemStyle.Radio)]
    public enum ShowCheckedStateGroup
    {
        All,
        Checked,
        Unchecked,
    }

    public enum StdModalView
    {
        ActivityIndicator,
        CheckBoxOptions,
    }
    /// <summary>
    ///  Specifies identifiers to indicate the return value of a modal overlay
    /// </summary>
    public enum ModalResult
    {
        /// <summary>
        ///  Nothing is returned from the dialog box. This means that the modal dialog continues running.
        /// </summary>
        None = 0,

        /// <summary>
        ///  The dialog box return value is OK (usually sent from a button labeled OK).
        /// </summary>
        OK = 1,

        /// <summary>
        ///  The dialog box return value is Cancel (usually sent from a button labeled Cancel).
        /// </summary>
        Cancel = 2,

        /// <summary>
        ///  The dialog box return value is Abort (usually sent from a button labeled Abort).
        /// </summary>
        Abort = 3,

        /// <summary>
        ///  The dialog box return value is Retry (usually sent from a button labeled Retry).
        /// </summary>
        Retry = 4,

        /// <summary>
        ///  The dialog box return value is Ignore (usually sent from a button labeled Ignore).
        /// </summary>
        Ignore = 5,

        /// <summary>
        ///  The dialog box return value is Yes (usually sent from a button labeled Yes).
        /// </summary>
        Yes = 6,

        /// <summary>
        ///  The dialog box return value is No (usually sent from a button labeled No).
        /// </summary>
        No = 7,

        /// <summary>
        ///  The dialog box return value is Try Again (usually sent from a button labeled Try Again).
        /// </summary>
        TryAgain = 10,

        /// <summary>
        ///  The dialog box return value is Continue (usually sent from a button labeled Continue).
        /// </summary>
        Continue = 11,
    }

    [LayoutOptions(LayoutOptionFlag.Vertical | LayoutOptionFlag.Text)]
    public enum ApplyCancel
    {
        /// <summary>
        ///  The dialog box return value is OK.
        /// </summary>
        [VisibilityPredicate(VisibilityPredicateFlag.Single | VisibilityPredicateFlag.Multiple)]
        Apply = 1,

        /// <summary>
        ///  The dialog box return value is Cancel (usually sent from a button labeled Cancel).
        /// </summary>
        Cancel = 2,
    }

    public enum InfoOverlayRowStyle
    {
        Header,
        Text,
        BulletText,
        Separator,
    }

    [CssName("icon-radio")]
    public enum IconRadio
    {
        [CssName("circle-thin")]
        CircleThin,

        [CssName("dot-circled")]
        DotCircled,

        [CssName("circle-empty")]
        CircleEmpty,

        [CssName("circle")]
        Circle
    }

    public enum BusyMinimumDelay
    {
        Disabled,
        Enabled,
    }

    public enum StdInfoCommon
    {
        [InfoText(
            message: @"
# Welcome!
- Tips like this will guide you through the demo.
- Tap anywhere to close this view."
)]
        InfoTextDefault,
        [InfoText(message: @"
# Welcome!
Tips like this will guide you through the demo.
- Disable 'this' message by tapping the checkbox.
- To close this view, tap anywhere else.
- Turn off or restore ALL messages in Settings."
)]
        InfoTextDefaultDSA,
    }
}
