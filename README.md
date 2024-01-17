# StrPrsL
WPF rewrite of my StrPrs macro scripting application.

# What is this?
This is a rewrite of one of my old applications I had made for personal use that later became the app my friends and I would use whenever we wanted to create quick and dirty moderately-complex macros for automating tasks.

It offers quick access to .NET functionality one would require when trying to automate tasks, provided that you have some programming knowledge.
I tried to keep the syntax and logic relatively beginner-friendly.
> [!WARNING]
> This app was not meant to, nor should be used to, create highly complex or reliable tasks.

> [!CAUTION]
> Per its nature, it does offer access to (relatively) lower level functions/methods, can take control of the user's keyboard and mouse and should be used with caution.
> **I do not take responsibility for the damage that may be caused through the use or including the use of this software. Make sure you write your scripts and use the app responsibly.**

# How does it work?
![Command graph](/StrPrsL/Page%20Assets/CommandGraph.png)
![Function graph](/StrPrsL/Page%20Assets/FunctionGraph.png)
## Syntax
In writing a script, you are supplying a set of instructions for the application to execute.
In order for the application to interpret the instructions you've given, you must follow the syntactic set of rules.

Every instruction starts and ends with its respective syntactic character(s) and requires a parameter field before closing off the command or the function.
E.g.: The word inside the `<` and `>` or `[` and `]` signs before the parameter field `()` is the command's or the function's name and the parameter field must be supplied inside the command or function declaration itself.
### The syntax is broken up into four main parts;
- **Commands**: These execute instructions. E.g.: `<Command()>`
- **Functions**: These execute instructions and return the value to be used in the parameters of other functions or commands. E.g.: `[Function()]`
- **Strings**: These are instructions that are interpreted literally without any filtering applied. E.g.: `"String! 6 is less than 10 6 < 10."`
> [!TIP]
> You may escape a character inside the string interpretation using `\`. E.g.: `"This is how you \"quote\" inside a string."`
- **Parameters**: These are the information supplied to commands and functions and are declared within parameter fields.
> [!IMPORTANT]
> Each parameter inside a parameter field is seperated with a comma `,`.
### Blocks
Commands may have blocks attached to them immediately following the command itself.
Code inside `{` and `}` are what are attached to a command's block. Not every command uses its attached block but certain commands like `<If()>`, `<Else()>`, `<Start()>`, and `<Stop()>` will.
```
<Start()>
{
	<Print("This will only be executed once the script starts")>
}

<If(ParameterCondition)>
{
	<Print("Condition was satisfied")>
}
<Else()>
{
	<Print("Condition was NOT satisfied")>
}

<Stop()>
{
	<Print("This will only be executed once the script stops")>
}
```

## Shortcuts
These shortcuts can be triggered when editing the script.<br/>
- `Ctrl + K` - Opens the window to allow inserting the pressed key's ID.
- `Ctrl + P` - Open the window to allow automatically inserting a True/False condition based on the pixel's color and sets the location to the cursor's position at the time.
- `Shift + Enter` - Insert a new block following the command at the caret's position.
- `Double click` on an item in the Script Output - Jumps the caret to the command that triggered the output.
- `<` - Opens the autocomplete list for the commands available.
- `[` - Opens the autocomplete list for the functions available.

# Examples
Example files can be accessed by browsing the files in the "[Examples](/Examples%20Scripts)" folder

- [Left and right click simulator](/StrPrsL/Examples%20Scripts/Left%20and%20right%20click%20simulator.str): Simulates using the Left Mouse Button with the F key and using the Right Mouse Button with the G key.
- [Right click toggle](/StrPrsL/Examples%20Scripts/Right%20click%20toggle.str): Simulates toggling holding down the Right Mouse Button with the F key.
- [Minimize active window](/StrPrsL/Examples%20Scripts/Minimize%20active%20window.str): Minimizes the currently active window with the F key.
- [Hotkey on key](/StrPrsL/Examples%20Scripts/Hotkey%20on%20key.str): Template for triggering actions when the hotkey (F) is pressed or released.
- [Hotkey toggle](/StrPrsL/Examples%20Scripts/Hotkey%20toggle.str): Template for toggling functionality in script when the hotkey (F) is pressed.
- [Q spam](/StrPrsL/Examples%20Scripts/Q%20spam.str): Toggles the spamming of the Q key when the F key is pressed.
- [Z spam](/StrPrsL/Examples%20Scripts/Z%20spam.str): Toggles the spamming of the Z key when the F key is pressed.
- [Toggle sprint walk](/StrPrsL/Examples%20Scripts/Toggle%20sprint%20walk.str): Toggles holding down the LSHIFT and W keys when the F key is pressed.
- [Dark Theme](/StrPrsL/Examples%20Scripts/Dark%20theme.str): Functionality toggle but with a Dark theme embedded into the script file.
- [Pink Theme](/StrPrsL/Examples%20Scripts/Pink%20theme.str): Functionality toggle but with a Pink theme embedded into the script file.
- [Green Theme](/StrPrsL/Examples%20Scripts/Green%20theme.str): Functionality toggle but with a Green theme embedded into the script file.
- [Pixel color condition](/StrPrsL/Examples%20Scripts/Pixel%20color%20condition.str): Executes instructions if the pixel at 500, 500 is green.
> [!TIP]
> These files offer quick templates to save you time for most of the common scripts you might write.

# Credits
[WPFDarkTheme](https://github.com/AngryCarrot789/WPFDarkTheme)<br/>
[AvalonEdit](http://avalonedit.net/)<br/>
[Extended Wpf Toolkit](https://github.com/xceedsoftware/wpftoolkit)<br/>
[ModernWpfUI](https://github.com/Kinnara/ModernWpf)<br/>
[InputSimulatorPlus](https://github.com/TChatzigiannakis/InputSimulatorPlus)