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
- **The pull reaches the spreadsheet one of two ways**: by its link, when it is shared as "anyone with the link can
  view", or through a web app deployed from it, which keeps it private. [Reaching the spreadsheet](#reaching-the-spreadsheet)
  has both.

---

## Setting up

Create a master asset with **Create > RPG Framework > Localisation > Master** and give it:

- **Access**: how the pull reaches the spreadsheet, **Shared Link** or **Web App** (below).
- **Sheet Id**, for a shared link: the spreadsheet's id, or a link to it.
- **Web App URL**, for a web app: the URL its deployment gives.
- **Default Namespace** and **Default Keys Class Folder**, for the generated keys classes (below).
- **Version**: how the files are laid out (below).

Once it can reach the spreadsheet, use the two buttons below its fields, **Pull spreadsheet** and **Generate files**.

### Reaching the spreadsheet

**By its shared link.** Share the spreadsheet as "anyone with the link can view" and put its id, or its link, in
**Sheet Id**. Nothing more to set up, but anyone who has the id can read the spreadsheet, and the id is saved in the
master, so it is in your project and its repository.

**Through a web app**, for a spreadsheet shared with no one but your team. A short script bound to the spreadsheet
reads it as you and hands the pull the same file the shared link gives, so the two ways pull identical text. One person
sets it up once:

1. Open the spreadsheet and choose **Extensions > Apps Script**. This makes a script that belongs to the spreadsheet.
2. On the master, set **Access** to **Web App** and press **Copy web app script**. Paste it over everything in the Apps
   Script editor, and save. The script is short:

   ```js
   function doGet() {
     var url = 'https://docs.google.com/spreadsheets/d/' + SpreadsheetApp.getActiveSpreadsheet().getId() + '/export?format=xlsx';
     var xlsx = UrlFetchApp.fetch(url, { headers: { Authorization: 'Bearer ' + ScriptApp.getOAuthToken() } });

     return ContentService.createTextOutput(Utilities.base64Encode(xlsx.getContent()));
   }
   ```

3. Choose **Deploy > New deployment**, select the type **Web app**, and set **Execute as: Me** and **Who has access:
   Anyone**, then **Deploy**. "Anyone" is needed because Unity's request carries no Google sign-in; the spreadsheet
   itself stays shared as it was.
4. Authorise it when Google asks. As the script is yours rather than a published app, Google warns that it hasn't
   verified it: choose **Advanced**, then **Go to** the project. It asks to see your spreadsheets and to connect to an
   external service, which it uses to read this spreadsheet's export; it reads no other.
5. Copy the **Web app URL**, ending `/exec`, into the master's **Web App URL**, and pull.

After changing the script, publish it with **Deploy > Manage deployments**, editing the deployment and choosing a new
version: the URL stays the same. A new deployment would get a new one.

#### Keeping it private

- **Share the spreadsheet only with the people who edit it.** The web app reads it as whoever deployed it, so nobody
  needs access to the spreadsheet to pull, and it needs no link sharing.
- **Keep the web app's URL as you would a password.** Anyone holding it can read the spreadsheet, and it is saved in
  the master, so it is committed with your project. That suits a private repository.
- **In a public repository, keep the master out of it**: add the master and its `.meta` to `.gitignore`. Nothing at
  run time uses the master, and everything pulling and generating make is still committed, so only whoever pulls needs
  one. A master made again in the same folder, with the same settings, finds the sheet assets by name when it pulls.
- **If the URL gets out, replace it.** In **Deploy > Manage deployments**, archive the deployment, which stops its URL
  working, then deploy again and put the new URL in the master.
- **The web app reads as the person who deployed it.** If they lose access to the spreadsheet, or leave the team,
  someone else deploys it from their own account and the master takes the new URL.

### Pull spreadsheet

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
  loaded in it, and raises `OnLanguageChanged`; it throws for a language the game does not ship. A regional language whose file is missing falls back to its neutral
  language's file.
- **Sheets are loaded for a screen and unloaded after it**, so only one screen's text is in memory. Loading a sheet that
  is already loaded throws; unload it first. Unloading one that isn't loaded does nothing, and
  `UnloadAllLocalisationData` clears everything.
- **`Get` returns the text**, or a marker naming what is missing — `MISSING KEY [...]`, `MISSING SHEET [...]` — so a
  mistake shows on screen. `TryGet` says whether the key was found instead. Both take the key as text or as its
  FNV-1a 64 hash, for keys kept as data.
- Files are read from StreamingAssets directly, or with a web request on Android and WebGL, where StreamingAssets is
  not a folder.

---

## Using it on its own

Nothing here needs the rest of the RPG Framework: the package depends only on RPGFramework.Hashing, and the service is a
plain C# object your game creates and keeps.

**Make one service for the game's life, and initialise it before any text is shown.** A small first scene that does so
and then loads the next is the simplest way to be sure nothing asks for text too early:

```csharp
using System.Threading.Tasks;
using RPGFramework.Localisation;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Boot : MonoBehaviour
{
    public static ILocalisationService Localisation { get; private set; }

    private void Start()
    {
        // Start cannot be awaited, so the work runs as a task, and anything it throws is logged rather than lost.
        BootAsync().ContinueWith(task => Debug.LogException(task.Exception), TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task BootAsync()
    {
        Localisation = new LocalisationService();

        await Localisation.InitialiseAsync();
        await Localisation.LoadNewLocalisationDataAsync("Generic");

        await SceneManager.LoadSceneAsync("Title");
    }
}
```

Hold it however your game holds its services: a static as here, an object that is never destroyed, or your own
dependency injection.

Some suggestions for fitting it in:

- **Load a screen's sheets as it opens and unload them as it closes**, so only the text in use is in memory, and keep a
  sheet every screen uses, such as `Generic`, loaded throughout. A sheet already loaded throws if loaded again, so give
  each screen sheets of its own.
- **Redraw text when the language changes.** `OnLanguageChanged` is raised once every loaded sheet has been reloaded in
  the new language. In a component showing a UI Toolkit `Label`, `m_Title`:

  ```csharp
  private void OnEnable()
  {
      Boot.Localisation.OnLanguageChanged += Redraw;
      Redraw(Boot.Localisation.CurrentLanguage);
  }

  private void OnDisable()
  {
      Boot.Localisation.OnLanguageChanged -= Redraw;
  }

  private void Redraw(string language)
  {
      m_Title.text = Boot.Localisation.Get(LocalisationKeys.TitleMenu.NEW_GAME);
  }
  ```

- **Keep the player's choice of language** wherever your game keeps its settings, and pass it to `SetCurrentLanguage`
  after `InitialiseAsync`. `GetAllLanguages` lists what can be chosen.
- **Name keys in code with a keys class**, and **store keys kept as data** — in a save, or on a ScriptableObject — as
  their hash, `Fnv1a64.Hash("Generic/Yes")` from RPGFramework.Hashing, read back with `Get(ulong)`: eight bytes, the same
  size for every key.
- **Choose File Per Sheet** if content added after release will bring sheets of its own.

---

## In the RPG Framework

- **One service for the whole game**, bound by the global installer, whose `Bootstrap` initialises it, so Core enters
  the first module only once the manifest has been read:

  ```csharp
  container.BindSingleton<ILocalisationService, LocalisationService>().AsNonLazy();
  ```

  ```csharp
  public override Task Bootstrap(IDIResolver resolver)
  {
      ILocalisationService localisation = resolver.Resolve<ILocalisationService>();

      return localisation.InitialiseAsync();
  }
  ```

- **Each menu names its sheets** in its localisation args, an **`ILocalisationArgs`** (`DataSheetsToLoad`): they are
  loaded as the menu opens and unloaded as it closes.
- **Each field names its sheets** in the Field Designer: they are loaded as the field is entered and unloaded as it is
  left. Its dialogue keys are compiled into its scripts as hashes, and its location name is kept the same way.
- **The player's language** is kept in Core's settings, chosen in the Language and Config menus, and applied by the
  title screen at start-up.
- **The editors read the sheet assets**: the Field Designer offers dialogue keys from the sheets a field loads, Field's
  export refuses a key none of them has, and Field checks dialogue markup when the files are generated, through an
  `ILocalisationTextValidator`.

---

## Sample

**Localisation Sample**: a title screen and a settings panel in five languages, its text already built (File Per
Sheet) and its keys classes generated, so it runs without a spreadsheet.

1. Import it from the Package Manager.
2. Choose **RPG Framework > Localisation > Install Sample Text**, which copies its text into
   `StreamingAssets/Localisation`: a sample cannot put files there itself. It refuses a project that already has
   localisation files there rather than replace them.
3. Open the **Localisation Sample** scene and press Play.

It shows `Generic` loaded for as long as it runs and each panel loading its own sheet as it opens and unloading it as it
closes; the language button stepping through every language the sample ships, with every label redrawn on
`OnLanguageChanged`; keys named through the generated `LocalisationKeys` classes; and New Game looking its text up by
its key's hash.

### Pulling the sample's spreadsheet

The sample's text has [a spreadsheet of its own](https://docs.google.com/spreadsheets/d/1tedGRzeY6bvF7wFReRbFRLVwgOcJQz6gBVak_ZtP8cQ/edit?usp=sharing),
shared by link, and a master for it in the sample's `Spreadsheet` folder. Select **Sample Master** and press **Pull spreadsheet**:
the sheet assets appear in a `Sheets` folder beside it, each with its keys recorded. It generates no keys classes, so
the sample's own are left as they are.

**Generate files** writes into `StreamingAssets/Localisation`, replacing whatever is there, as your own master would,
so try it in a project without localisation of its own.

To try a web app, make a copy of the spreadsheet with **File > Make a copy**, set the master's **Access** to **Web
App**, and follow [Through a web app](#reaching-the-spreadsheet) on your copy.

---

## Not in this version

- **Google Sheets is the only source.**
- **Renaming a tab doesn't carry its sheet over**: the renamed tab is a new sheet, and every key that began with the old
  name has to change wherever it is used.
