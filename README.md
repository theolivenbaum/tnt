# TNT - The .NET Translation Tool

A command line tool for managing translations based on strings extracted from .NET assemblies. `tnt` supports translation roundtrips via Excel or XLIFF and Google machine translations.

`tnt` lets you mark literal strings in C# or F# source code, extracts them from the compiled IL code, and organizes the translation processes. At runtime, the NuGet [TNT.T][TNT.T] translates the marked strings.

`tnt` is very similar to translation solutions like [gettext][gettext] and was created to provide an alternative to .NET resource files.

[gettext]: https://en.wikipedia.org/wiki/Gettext

## Installation & Update

To install `tnt`, download the [latest dotnet framework](https://www.microsoft.com/net/download) and enter:

```bash
dotnet tool install tnt-cli -g
```

After that, `tnt` can be invoked from the command line. For example, `tnt version` shows its current version. To update `tnt` enter:

```bash
dotnet tool update tnt-cli -g
```

## Concepts & Walkthrough

### Projects, Initialization, and Subdirectories

`tnt` works in a project's directory, preferable the directory of your application's primary project. Change into this directory an initialize it with `tnt init`. This creates the subdirectory `.tnt/` and the file `.tnt/sources.json`. The `.tnt/` directory contains all the _important_ files that are managed for you: these are the list of sources and the translation files.

> Note that some `tnt` commands act somewhat unforgiving and do not offer an undo option, therefore it's recommended to put the `.tnt/` directory under revision control.

> `tnt init` sets the source language of your project's strings to `en-US` by default. If your original strings are in a different language, you can change that anytime with `tnt init -l [your language tag or name]`.

### Sources

A source is something `tnt` retrieves original strings from. Currently, `tnt` supports .NET assemblies only.

All sources are listed in the `.tnt/sources.json` file and can be added by entering `tnt add -a [relative path to the assembly]` from within your project's directory. For example `tnt add -a bin/Release/netcoreapp2.1/MyAwesomeApp.exe` would add an assembly to the list of sources.

> `tnt` does not read or modify any of your other project files, it accesses only the compiled assemblies.

### Language & Assembly Extraction

`tnt` extracts marked strings from .NET assemblies. For C#, each string that needs to be translated must be marked with an extension method `.t()` that does two things: First, it marks the literal string that comes before it, and second, it translates the string. For example: `"Hello World".t()`.

> For more extraction options take a look at [Methods of Extraction](#methods-of-extraction).

To use the `.t()` function in your projects, add the [TNT.T][TNT.T] NuGet package to your project and insert a `using TNT;` to the top of the source file you want to mark strings in. Lucky owners of Resharper may just type `.t()` after a literal string and insert `using TNT;` by pressing ALT+Enter. 

Before extracting, you need to add at least one target language to the project. Add one, say "Spanisch" with `tnt add -l Spanish`. 

> `tnt add -l` and other commands accept language names or language tags. If you are not sure what languages are supported, use `tnt show languages` to list all .NET language tags and language names.

To be sure the sources are available, build your project and then enter `tnt extract` to extract the strings and to create the appropriate language files. 

While `tnt` extracts the strings, it shows what it does and prints a status for each language file generated. After the extraction, the language files are saved to `.tnt/translation-[tag].json`.

> The status consists of the translation's language tag, its counters, its language name, and the filename of the translation file.

> Of particular interest are the counters that count the states the individual strings are in. If you extracted, say 5 strings, and haven't translated them yet, you'll see a `[5n]`. Later, counters for additional states will appear. If you are interested now, the section [Translation States](#translation-states) explains them all.

### Translating Strings

`tnt` itself does not support the interactive translation of your strings, [yet](https://github.com/pragmatrix/tnt/issues/46). 

Of course, editing the translation files is possible, but there are other ways `tnt` can help you with:

#### Machine Translations

`tnt` supports Google machine translations, which should be a starting point for newly extracted strings. For the English to German machine translations I tried so far, the results were of good quality and Google's translation algorithm positioned .NET placeholders like `{0}` at the locations expected. I don't know if the resulting quality will be the same for your translations, but with `tnt translate` you can try your luck with the Google Cloud Translation API. For more information, skip to the section that explains [`tnt translate`](#tnt-translate).

#### Excel Roundtrips

`tnt export` exports languages to a Excel file that can be modified by a translator and imported back with `tnt import`.

> Although `tnt` tries hard to do its best, `tnt import` is one of the commands unexpected things may happen. So please be sure that the `.tnt/` directory is under revision control.

#### XLIFF Roundtrips

Similar to the Excel roundtrips, `tnt` supports the traditional translation process that is comprised of exporting the translation files to the [XLIFF][XLIFF] format, using an XLIFF tool to edit them, and importing back the changes. With `tnt export`,  XLIFF files are generated and sent to translators, who can then use their favorite tool (like the [Multilingual App Toolkit](https://developer.microsoft.com/en-us/windows/develop/multilingual-app-toolkit), for example) to translate these strings and send them back to the developer. After that, `tnt import` is used to update the strings in the translation files.

#### Translation Verification

`tnt` supports a number of simple verification rules it applies to the translated strings. `tnt` verifies

- if the same .NET placeholders (for example `{0}`) reappear in the translated text.
- if the number of lines match.
- if the indents match.

These rules are verified with each `tnt status` invocation for translations that are in the `needs-review` state only (`r` for short). If one of the rules fail to verify, `tnt status` increases the warning counter (abbreviated with `w`) and `tnt show warnings` may be used to show the messages in detail.


### Deployment & Translation Loading

`tnt` maintains a second directory named `.tnt-content/` where it puts translation files intended to be loaded by your application via the NuGet [TNT.T][TNT.T].

> These files *do not need* to be under revision control, because they can be regenerated with `tnt sync` any time. They contain *distilled* translations for each language optimized for your application to pick up.
>
> The format of these files might change in the future and should not be relied on.

To make the application aware of the translation files, add them to the project and change their build action to `Content`.

> After adding the files to the project, you can change the build action in the properties dialog of Visual Studio or by changing the XML element of the files from `<Compile ...` to `<Content ...`.

Now, when you start your application with another default user interface language configured, [TNT.T][TNT.T] loads the matching translation file and translates the strings marked with `.t()`.

## Reference

### Command Line, Parameters, and Examples

The `tnt` command line tool uses the first argument to decide what task it executes. Options are specified either with single character prefixed with `-` or a word prefixed with `--`. Some tasks take additional arguments.

For a list of available tasks use `tnt help`, and for options of a specific task, use `tnt [task] --help`. For example, `tnt init --help` shows what `tnt init` has to offer.

#### `tnt init`

Creates and initializes the `.tnt/` directory. This is the first command that needs to be used to start using `tnt`. 

- `-l`, `--language` (default `en-US`) Sets the source language the strings extracted are in.

  >  `tnt init -l [language]` can be used to change the source language later on.

Examples:

`tnt init -l en-US` initializes `.tnt/` directory and sets the source language to `en-US`.

#### `tnt add`

Adds a new assembly to the list of sources, or a new language to the list of translations.

- `-a`, `--assembly` adds an assembly to the list of sources. The option's argument _must_ be a relative path to the assembly.

- `-l`, `--language` adds a new translation language.

Examples:

`tnt add -a bin/Release/netcoreapp2.1/MyAwesomApp.dll` adds the assembly to the list of sources.

`tnt add -l de` adds German to the list of translation languages.

#### `tnt remove`

Removes an assembly from the list of sources.

- `-a`, `--assembly` removes an assembly from the list of sources. The option's argument may be the assembly's name or a sub-path of the assembly. As long one of the sources matches unambiguously, it's going to be removed. Use `tnt status -v` to list all the current sources.

> Intentionally, `tnt` has no option for removing translation languages and expects that you delete the language file under `.tnt/` manually. To update the `.tnt-content/` directory, use `tnt sync` after that.

#### `tnt extract`

Extracts original strings from all sources and updates the translation files.

If an original string's record exists in all the translation files already, nothing changes. If it doesn't, a record is added to the translation file with its state set to `new`.

Records that exist in the translation files, but their original string is missing from the sources, will be set to the state `unused`. 

> If an original string reappears, for example by marking it with a `.t()` again, it's record will change from the state `unused` to `needs-review` indicating the need for further attention.
>
> Note that `unused` records are never used to translate strings in the application. They exist to preserve translated strings for original strings that were changed or removed.
>
> To get rid of `unused` records, for example after all translations were completed, use `tnt gc`. 

#### `tnt gc`

Deletes all the translation records that are in the state `unused`. 

#### `tnt status`

Shows the states of all translations. See also [Translation States](#translation-states).

- `-v`, `--verbose` shows the formatted contents of the `sources.json` file.

#### `tnt export`

Exports translations for use with a language translation tool or Excel.

This command exports translations to one file per each language. The files are named after the name of the project project and the translation's language tag. By default, the project's name is set to the name of the current directory.

`tnt export` never overwrites any files. If files do exist at the designated locations, `tnt` warns and exits.

The exported languages can be selected either by `-l`, or by passing them as arguments. To select all languages, use `--all`. 

- `--all` selects all existing languages. By default, no languages are exported.
- `--to` specifies the directory where the files are exported to. The default is the current directory.

- `--for` Specifies the export format and tool that will be used for editing. The default is `excel` and the supported formats are:

  - `excel` exports the translation records into an Excel file that contains one workbook for each  translation state. The workbooks consist of the following columns:

    - The original strings.
    - An empty column that is meant to be filled with the translated strings.
    - A column that contains the translated strings at the time of the export.
    - The state of the string.
    - Contextual information that describes where the original strings appeared in the source.
    - Notes.

    The empty column and the state column are meant to be modified by the translator. 

    If there is a translated string available in the third column (for example a machine translated suggestion), the translator can copy it to the second and change it.

  - `xliff` exports the translations to an XLIFF 1.2 formatted file that should be compatible with most XLIFF tools.

  - `xliff-mat` exports the translations to an XLIFF 1.2 formatted file so that it's compatible with the Multilingual App Toolkit.

    > The [Multilingual App Toolkit needs](https://multilingualapptoolkit.uservoice.com/forums/231158-general/suggestions/10172562-fix-crash-when-opening-xliff-file-without-group) the `<trans-unit>` elements to be surrounded with a `<group>` element, otherwise it will fail to open the exported files. 
    >
    > `tnt` supports this as an option, because other tools may fail if they encounter `<group>` elements.

- `-l`, `--language` specifies the language tags or names that should be exported. Alternatively, the languages can be passed directly as arguments.

Examples:

`tnt export German` exports an Excel file to the current directory that is named after the current directory's name and the language tag `de` or `de-*`.

`tnt export en` exports all translations with a language tag `en` or  `en-*`.

`tnt export --all --for xliff-mat --to /tmp` exports all translations to the directory `/tmp` for the use with the Multilingual App Toolkit.

#### `tnt import`

Imports Excel or XLIFF translation files. 

`tnt import` imports files specified by filename or language tag, or all files that are found in the import directory.

To import languages, use `tnt import [language tag or name]`. To import files, use `tnt import [filename]`. To import all files that look like they were previously exported with [`tnt export`](#tnt-export), use `tnt import --all`.

- `--from` is directory to import the files from. Default is the current directory.
- `--all` imports all files that are in the import directory.
- `-l`, `--language` specifies additional language tags or names to import. This is an alternative to passing the languages as arguments.

> The importer matches the original strings in the import files with the ones in the language files in `.tnt/`. If an original string is found, the importer _will overwrite_ the translation record with the one imported. So before using `tnt import`, make sure the contents of the `.tnt/` directory is commited to your revision control system.

#### `tnt translate`

Machine-translates strings that are in the state `new`.

- `--all` translates all new string of all languages.
- `-l`, `--language` specifies the languages to which new strings are translated.

> Before `tnt` can machine-translate strings, it must be configured to use a translation service. For now, only the Google Cloud Translation API is supported. 
>
> To configure `tnt` to work with the Google Cloud Translation API, follow the steps 1. to 3. in [Quickstart](https://cloud.google.com/translate/docs/quickstart) and then use `tnt translate` to translate all new strings. 

#### `tnt sync`

Rebuilds the `.tnt-content/` directory and its files. Usually, `tnt` takes care of updating the final translation files automatically, but in case of errors or if the `.tnt-content/` directory does not exist, it may be useful to ensure that the final translation files match the translations in the `.tnt/` directory.

> If you decide not to check in the `.tnt-content/` directory, `tnt sync` _must_ be part of your build process.

#### `tnt show`

Lists the .NET supported languages or shows interesting details of the translations.

- `tnt show languages`

  Lists the currently supported language names and tags of the .NET framework `tnt` runs on.

- `tnt show new`

  Shows original strings that are not translated yet.

- `tnt show unused`

  Shows the strings that are not used anymore.

- `tnt show shared`

  Shows the strings that were extracted from more than one source location.

- `tnt show warnings` 

  Shows the strings that are in the state [`needs-review`](#translation-states) and caused one or more verification warnings.

The details `new` and `warnings` can be restricted to specific translations only. Use `-l` or `--language` to filter their results.

> The results of `tnt show unused` and `tnt show shared` depend on the original strings only and are therefore independent of the individual translation languages.

#### `tnt add-command`

`tnt` can execute shell commands in certain situations. Currently, only one command trigger named `before-extract` is supported, for example:

```bash
tnt add-command before-extract "dotnet build"
```

Adds a shell command `dotnet build` that is executed before the text extraction.

#### `tnt remove-commands`

Removes commands that are assigned to a specific trigger. For example:

```bash
tnt remove-command before-extract
```

Removes all commands that were assigned to the trigger `before-extract`.

#### `tnt list-commands`

Lists all commands that were added.

#### `tnt edit-commands`

Opens the system's default editor for `.json` files to edit the file that stores the list of commands.

#### `tnt help`

Shows useful information about how to use the command line arguments. To show help for specific tasks, use `tnt [task] --help`.

#### `tnt version`

Shows the current version of `tnt`.

### Methods of Extraction

`tnt` extracts strings that are marked with a function that also translates them. This is the `t()` method that is located in `TNT.T` static class. 

There are a number of ways to mark strings that need to be translated:

#### Simple Strings

Constant and literal strings can be marked by appending `.t()` to them. When the `t()` function is called, the string gets translated and is returned to caller. To use `t()`, add `using TNT;` to the top of your C# source file.

Examples:

`"Hello World".t()`

`string.Format("Hello {0}".t(), userName)`

#### Interpolated Strings

In C# 6, [interpolated strings](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated) were introduced by prefixing strings with the `$` character. To mark an interpolated string as translatable, the `t()` function is used, but - for technical reason - not invoked as an extension method.

Bringing the static `TNT.T` class into scope mitigates that:

```csharp
// bring the static t() function into scope.
using static TNT.T;
... 
... t($"Hello {World}") ...
```

> Note that the extracted string will result into `Hello {0}` for the example above.

#### Specific Translations

If strings need to be translated to a language defined by the application, the `t()` function can be invoked with an additional argument that specifies the translation's language tag. For example `"Hello".t("es")` will translate the string "Hello" to Spanish if the translation is available.

> Original strings are extracted by through the generated IL code. If an invocation to the `.t()` function is found but the extraction attempt fails, [`tnt extract`](#tnt-extract) will warn about that.

### Translation States

A translation state defines the state of a translated string. In the translation files, the states are stored in their long form, when listed as a counter, they are shortened to a single character:

- `new`, `n` 

  A string yet untranslated.

- `needs-review`, `r` 

  Either a machine or imported translated string indicating that the translation is not final and should be reviewed.

- `final`, `f` 

  Imported translated strings that were marked "translated" or "final".

- `unused`, `u` 

  A translated string that disappeared after a recent `tnt extract`.

In addition to the states above, the `w` counter shows the number of analysis warnings. To list the strings that contain warnings, use [`tnt show warnings`](#tnt-show).

### `tnt` Managed Directories and Files

- `.tnt/` directory

  The directory where the configuration and the translation files are stored, created with [`tnt init`](#tnt-init).
  - `.tnt/sources.json`

    This file configures the language the original strings are authored in and the sources from where they are extracted.

    Use [`tnt init -l`](#tnt-init) to change the language, [`tnt add`](#tnt-add) to add sources, and [`tnt remove`](#tnt-remove) to remove them.

  - `.tnt/translation-[tag].json` 

    One translation file for each language that contain the original strings, their translated counterparts, states, extraction contexts, and notes.
    
  - `.tnt/commands.json`

    Stores shell commands that run at specific times.

- `.tnt-content/` directory

  This directory contains translation files optimized for the application to load.

  - `.tnt-content/[tag].tnt`

    The optimized language specific translation files. Currently, they contain the original and the translated strings only.

    > `tnt` tries to keep these files up to date, but in case they are missing, or language files in `.tnt/` were changed manually, [`tnt sync`](#tnt-sync) can be used to regenerate them.

## License & Contribution & Copyright

[MIT](LICENSE) 

Contributions are welcome, please comply to the `.editorconfig` file.

(c) 2020 Armin Sander

[TNT.T]: https://www.nuget.org/packages/TNT.T
[XLIFF]: https://en.wikipedia.org/wiki/XLIFF



