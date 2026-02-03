using OPC.Preview.Portable;
using System;
using System.Collections.Generic;
using System.Text;

namespace QueryFilterList.Portable.Demo
{
    public enum Radio
    {
        First, Second, Third,
    }

    public enum StdInfo
    {
        [InfoText(message: @"
Try these simple searches:
    - animal
    - color

💡 After that, compare:
    - app 
    - [app]
    - gre app 
    - gre [app]")]
        InfoTextQueryPrompt,


        [InfoText(message: @"
# CheckBox Extended Features
Long press the checkbox to see additional options.

- Filter list by check box state.
- Check or uncheck all items.")]
        CheckBoxPrompt,


        [InfoText(message: @"
# Filter Cleared
- Enter characters to filter the current list.
💡Tap [X] again to exit filter.")]
        FilterClearPrompt,


        [InfoText(message: @"
# No Items Match Filter
Tap filter icon to show all items.")]
        AdaptiveShowAllPrompt,
    }
}
