using OPC.Preview.Portable;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConfigurationIntro.Maui.Demo
{

    public enum StdInfo
    {

        [InfoText(message: @"
Welcome
- Tap this message to close this view.
- Command bar will reconfigure for editing."

)]
        InfoTextConfigureT,

        [InfoText(message: @"
%info%
In this step:
- The clicked button will be hidden.
- Command bar will recenter."
)]
        InfoTextPreviewFirstClick,

        [InfoText(message: @"
In this step:
- The command bar is now empty.
- As a result it is now hidden.
- Next, all icons will be reconstituted."

)]
        InfoTextPreviewLastVisible,

        [InfoText(message: @"
In this step:
- The command bar is now empty.
- Next, all icons will be reconstituted."

)]
        InfoTextPreviewReconstitute,

        [InfoText(message: @"
Try these simple searches:
    - animal
    - color

Compare:
    - app vs [app]
    - gre app vs gre [app]

💡 Hint: Tap [X] twice to reset:
    - First clears filter text.
    - Second returns to query mode.")]
        InfoTextQueryPrompt,
    }
}
