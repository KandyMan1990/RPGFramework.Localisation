# RPGFramework.Localisation

Localisation for Unity driven from a Google spreadsheet. The editor pulls the spreadsheet's tabs and generates compact
binary files into StreamingAssets; at run time the game loads the sheets a screen needs and looks text up by key.

Requires Unity 6000.0 or newer and RPGFramework.Hashing, and is built against the .NET Standard 2.1 API compatibility
level, Unity's default.

---

## The spreadsheet

Each tab is a **sheet**, laid out as:

| Key | Key Comments | en-GB | fr-FR | … |
| --- | --- | --- | --- | --- |
| Game_Title | The name of the game | My Game | Mon Jeu | … |
| Yes | | Yes | Oui | … |

- **Row 1** names the columns: the key, a comment for translators (never shipped), then a language code per column from
  C onward, such as `en-GB` or `fr-FR`. Every language a sheet lists is one the game ships.
- **Every other row** is a key and its text in each language. A row with no key is ignored. An empty translation is
  reported when generating and shows as empty text.
- **A tab's name is the sheet's name**, and must be a valid C# identifier: letters, digits and underscores, not starting
  with a digit. A tab whose
  name starts with `#` is not a sheet and is skipped, for notes or a glossary beside the text.
- **A key is written with its sheet**, as `Sheet/Key` (`Generic/Yes`), so keys only need to be unique within a sheet.
- **The spreadsheet must be shared as "anyone with the link can view"**, because the pull uses Google's anonymous export.
  Anyone with its id can read it, and the id is kept in the project.

---

## Setting up

Create a master asset with **Create > RPG Framework > Localisation > Master** and give it:

- **Sheet Id**: the spreadsheet's id, or a link to it.
- **Default Namespace** and **Default Keys Class Folder**, for the generated keys classes (below).
- **Version**: how the files are laid out (below).

Then use the two buttons below its fields.

### Pull

Downloads the whole spreadsheet in one request and brings the sheet assets in line with its tabs:

- **Each tab gets a sheet asset**, named after it, in a `Sheets` folder beside the master. An existing asset is updated
  in place, so anything referencing it keeps working. These assets are the pull's: renaming one renames its sheet, so
  don't rename or move them by hand.
- **Each asset records its sheet's keys**, for editor tools to offer as choices.
- **An asset whose tab has gone is offered for deletion**, with its generated keys class. A renamed tab is a new sheet,
  since its name begins every key.
- **The download is kept** in the project's `Library` folder, for Generate files to build from. It is per machine and
  never committed, so each machine pulls once before generating.

### Generate files

Builds the files from the last pull, without the network, into `StreamingAssets/Localisation`: a binary file per sheet
or per language, and a manifest naming the languages and the format. Everything is checked first, and nothing is
written unless every sheet passes:

- tab names, and keys that would collide once hashed;
- for the file-per-language format, the same languages in the same order in every sheet;
- the text itself, by every `ILocalisationTextValidator` in the project. Implement it in your own editor code to check
  text that means something to your game, such as markup; each implementation is found and run.

Generate again after changing a sheet's settings or the format. After changing the spreadsheet, pull and then generate.

### Keys classes

A sheet can generate a C# class of its keys as constants, for code that names them:

```csharp
string title = localisation.Get(LocalisationKeys.Generic.GAME_TITLE);   // "Generic/Game_Title"
```

It is off until **Generate Keys Class** is ticked on the sheet asset, since code that reads keys from data needs no
class. The class goes in the master's **Default Keys Class Folder** with its **Default Namespace**, unless the sheet
overrides either; the folder decides which assembly compiles the class, so put it beside the code that uses it. A sheet
that stops generating has its old class deleted at the next Generate.

### File formats

| | File Per Sheet | File Per Language |
| --- | --- | --- |
| Files | one per sheet per language, in a folder per language | one per language, holding every sheet behind a table of contents |
| Loading a sheet | reads its file | reads just that sheet from the language's file; on Android and WebGL, which cannot read part of a file, the whole language file is fetched once and kept while that language is in use |
| Adding a sheet | adds files; nothing already shipped changes | rewrites every language's file |

File Per Sheet suits content added after release, which can bring its own sheets as files without replacing any the
game shipped. File Per Language means fewer files, and one to open per language.

---

## At run time

```csharp
ILocalisationService localisation = new LocalisationService();

await localisation.InitialiseAsync();                                     // reads the manifest

await localisation.LoadNewLocalisationDataAsync(new[] { "Generic", "TitleMenu" });

string yes = localisation.Get("Generic/Yes");

localisation.UnloadLocalisationData(new[] { "TitleMenu" });
```

- **Language**: the service starts in the device's language if the game ships it. Otherwise it uses the neutral
  language (`en` for `en-US`), then any language sharing that neutral (`en-GB`), then the first language the game ships.
  `GetAllLanguages` lists what ships, and `SetCurrentLanguage` switches to one of them, reloads the sheets already
  loaded in it, and raises `OnLanguageChanged`. A regional language whose file is missing falls back to its neutral
  language's file.
- **Sheets are loaded for a screen and unloaded after it**, so only one screen's text is in memory. Loading a sheet that
  is already loaded throws; unload it first. `UnloadAllLocalisationData` clears everything.
- **`Get` returns the text**, or a marker naming what is missing — `MISSING KEY [...]`, `MISSING SHEET [...]` — so a
  mistake shows on screen. `TryGet` says whether the key was found instead. Both take the key as text or as its
  FNV-1a 64 hash, for keys kept as data.
- **`ILocalisationArgs`** carries the sheet names a screen needs (`DataSheetsToLoad`), for code that opens a screen to
  hand to it.
- Files are read from StreamingAssets directly, or with a web request on Android and WebGL, where StreamingAssets is
  not a folder.

---

## Not in this version

- **A private spreadsheet can't be pulled**; it must be link-shared.
- **Google Sheets is the only source.**
- **Renaming a tab doesn't carry its sheet over**: the renamed tab is a new sheet, and every key that began with the old
  name has to change wherever it is used.
