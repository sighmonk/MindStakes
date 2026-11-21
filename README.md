<img width="1909" height="1071" alt="image" src="https://github.com/user-attachments/assets/59abccb0-1bba-4b69-a4fd-07afbf391f9b" />

# MindStakes

[![CI](https://github.com/sighmonk/MindStakes/actions/workflows/ci.yml/badge.svg)](https://github.com/sighmonk/MindStakes/actions/workflows/ci.yml)
[![Godot export](https://github.com/sighmonk/MindStakes/actions/workflows/godot-export.yml/badge.svg)](https://github.com/sighmonk/MindStakes/actions/workflows/godot-export.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)
[![Godot 4.7.2 .NET](https://img.shields.io/badge/Godot-4.7.2%20.NET-478cbf?logo=godotengine&logoColor=white)](https://godotengine.org)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512bd4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)

MindStakes is a tool for quiz show hosts, made in Godot.

# Features:
- track participants' scores
- get random categories
- get random, non-repeating questions
- track answer time
- use questions with audio and sound
- choose from three game modes
- use question pools
- switch between a Polish and an English interface

#  Game modes
## Betting
- The current player selects a category. They can either keep it or reroll it.
- Every player bets points, up to 20.
- The question and answers are revealed. Players have limited time to pick their answer.
- The correct answer is revealed, and players either lose or gain the amount of points they bet.
## Fraud
- The current player selects a category. They can either keep it or reroll it.
- Question and answers are revealed.
- The current player states whether they know the answer.
- If they say no, they lose only 10 points.
- If they say yes, other players can challenge them.
- If no one challenges them, the current player gets 20 points, whether they know the answer or not.
- If someone decides to challenge, the current player has to answer the question.
- If they answer correctly, they gain 20 points, and the challenging player loses 10.
- Otherwise, they lose 20 points, and the challenging player gets 10.
## Sabotage
- The current player selects a category. They can either keep it or reroll it.
- Question and answers are revealed.
- The current player can either answer the question or give it to another player.
- If they answer, they gain or lose 20 points based on their answer.
- If they give the question to another player, the selected player gains or loses 20 points based on their answer.
- If the selected player answers correctly, the current player loses 10 points. If the answer is incorrect, the current player gains 10 points.

## Game mode registry
Every mode is registered once in [Source/Data/GameMode/GameModeRegistry.cs](./Source/Data/GameMode/GameModeRegistry.cs). A `GameModeDefinition` carries the mode id, the localization key of the name shown in the mode pick screen, the scene opened after a category is picked and the filter deciding which questions the mode may use.

| Mode | Id | Scene |
|------|----|-------|
| Betting | `betting` | `Source/Screens/Betting/BettingScreen.tscn` |
| Sabotage | `sabotage` | `Source/Screens/Question/Sabotage/SabotageQuestionScreen.tscn` |
| Fraud | `fraud` | `Source/Screens/Question/Fraud/FraudQuestionScreen.tscn` |

[GameModeCard](./Source/Components/GameModeCard/GameModeCard.cs) shows the translated name and starts the mode, [CategoryButton](./Source/Components/Buttons/CategoryButton/CategoryButton.cs) opens the scene of the active mode, and [QuestionsManager](./Source/Managers/QuestionsManager/QuestionsManager.cs) only draws questions the active mode accepts.

The `GameModeType` export on `GameModeCard` and the `*ScenePath` exports on `CategoryButton` are the values already stored in the scenes. They win over the registry when set, so existing scenes keep working; `QuestionScenePath` on `CategoryButton` is legacy data that is no longer read. An index without a registration keeps the previous behaviour: the card shows `GAME_MODE_UNKNOWN`, nothing is opened after a category is picked and every question is accepted.

### Adding a mode
A new mode (for example Jeopardy) needs:

1. A question flag in [Question.cs](./Source/Data/Question/Question.cs) (for example `IsJeopardyGameQuestion`), set to `true` where the Question Creator and the importer build a question.
2. An entry in `GameModeRegistry`: a `GameModeDefinition` with the next free `Index`, the new id, a `GAME_MODE_JEOPARDY` entry in both `Resources/Locale/pl.po` and `en.po`, the scene path and the question filter.
3. A card in the editor — open `Source/Screens/GameModePick/GameModePickScreen.tscn`, select `Panel/PanelContainer/VBoxContainer/PanelContainer2/MarginContainer/HBoxContainer`, duplicate one of the `GameModeCard` instances in it, set `GameModeType` in the Inspector to the new index and assign `GameModeLabel`, `RoundCountSpinBox` and `StartButton`. Save the scene.

No C# switch has to change: the name and the scene path come from the registry and the question filter comes from the definition.

# Host debug tools
Live shows need to recover from a bad question or a wrong team without restarting, so the host gets a small panel that is hidden during normal play.

- Toggle: <kbd>F10</kbd> (input action `host_debug`, Project Settings → Input Map).
- Scene: `Source/Components/HostDebug/HostDebugPanel.tscn`, registered as an autoload after the managers, so it is present in every screen.
- `Reroll question` replaces the question on screen with another unused question from the same category and marks the discarded one as used, so it never comes back. The round, bets and scores stay unchanged; the current question scene is reloaded to render the new question.
- `Next answering team` switches `TeamsManager.CurrentTeam` and emits `SignalManager.AnsweringTeamChanged`. While the answering team is still the team that picked the category, both move together. The score screen, the category pick screen and the Sabotage/Fraud screens listen to that signal and refresh their team cards.
- `Close` (or <kbd>F10</kbd> again) hides the panel. The panel is not part of the normal player flow: it starts hidden, ignores the mouse outside its own box and is the only node on layer `100`.

Node tree and exports of `HostDebugPanel.tscn`:

| Node | Type | Notes |
|------|------|-------|
| `HostDebugPanel` | `CanvasLayer` | `layer = 100`, script `HostDebugPanel.cs` |
| `PanelContent` | `Control` | full rect, `mouse_filter = ignore`, `theme = MainTheme.tres` |
| `PanelContent/Panel` | `PanelContainer` | 640 px wide, placed at `(24, 24)` |
| `.../Panel/Margins` | `MarginContainer` | margins 16 |
| `.../Margins/VBox` | `VBoxContainer` | separation 12 |
| `.../VBox/Title` | `Label` | font size 32 |
| `.../VBox/RerollQuestionButton` | `Button` | 600 px wide |
| `.../VBox/NextTeamButton` | `Button` | 600 px wide |
| `.../VBox/StatusLabel` | `Label` | font size 26, word wrap |
| `.../VBox/CloseButton` | `Button` | 600 px wide |

The script exports bind `PanelContent`, `TitleLabel`, `RerollQuestionButton`, `NextTeamButton`, `StatusLabel` and `CloseButton`. The panel texts are `HOST_DEBUG_*` and `CLOSE` keys from the `.po` catalogs, so both languages work and are applied every time the panel opens.

To rebuild the scene by hand in the editor: create a `CanvasLayer` with the script attached, set `Layer` to `100`, add a full-rect `Control` named `PanelContent` with `Mouse Filter = Ignore` and `MainTheme.tres` as its theme, then add the `PanelContainer` → `MarginContainer` → `VBoxContainer` chain with the labels and buttons above, and assign the six exported nodes to the script in the Inspector.

# Save games

- The score screen calls `SaveManager.SaveCurrentGame()` after every round, which writes `user://saves/autosave.mssave`.
- `Load save` on the title screen opens the system file picker and restores a save, then goes to the game-mode screen, so a round is never restored in the middle of a question.
- `Export save` writes the same archive wherever the host points the native save dialog.
- The archive is a ZIP with `manifest.json` and the team pictures under `teams/`. It stores the team names, colours, scores and bets, the team pictures, the round and rounds limit, the reroll counters, the question time, the selected pool, the active game mode and the consumed question ids. Importing it on another machine copies the pictures to `user://team_images`.
- New Game is the title screen itself: entering it starts a fresh session, so scores and the consumed-question list reset.
- The manifest carries a `version`. A save with an unknown version is rejected with a translated error instead of being loaded partially.

# Configuration archive

- `Export configuration` on the title screen writes a `.mstakes` ZIP with `manifest.json`, the category list, every question from every pool (text, answers, type, difficulty, target modes, fun fact and the media files), the difficulty levels and the teams with their pictures.
- `Import configuration` validates the archive first: version, a manifest with categories and questions, and every referenced media file. Only then it writes anything, so a broken file changes nothing and reports a translated error.
- Questions are written by their stable id, so importing the same archive twice replaces the questions instead of duplicating them; categories are matched by name and overwritten.
- Imported media lands in `Assets/Media/Questions/...` (or `user://question_media/...`) and team pictures in `user://team_images`.

# Language
The interface ships in Polish (the default) and English. The `LanguageSwitcher` component ([Source/Components/LanguageSwitcher](./Source/Components/LanguageSwitcher/)) renders the **PL** / **EN** buttons and is instanced in the main menu. The choice is saved to `user://settings.cfg` and applied on the next start.

Translations live in `Resources/Locale/` as gettext catalogs (`pl.po`, `en.po`) and are registered in *Project Settings → Localization → Translations*. Text that comes from scenes uses the scene text as the translation key, while text created in C# uses keys such as `ROUND_COUNT`. A missing entry falls back to its key, so a label is never left empty.

To add a language, copy one of the catalogs, set the `Language:` header to the new locale code, translate the `msgstr` entries and add the file to the translations list.

Questions are separate data and are not part of the interface localization.

# Question Creator
- Create new questions.
- Edit existing questions.
- Save questions after creating or updating them.
- Modify existing question entries whenever needed.
- Bulk import questions from CSV or JSON.

The first answer field is always the correct answer; the game shuffles the answers when it displays them. All four answers are required, and two answers with the same text are rejected, because the correct answer is matched by its text.

# Assets
Application assets are tracked in the repository:

- `Assets/Fonts/`, `Assets/Icons/` and `Assets/Themes/` - fonts, UI icons and theme resources used by the scenes.

Media used by questions and the default team picture stay local, because they are quiz content rather than interface assets:

- `Assets/Media/Questions/Images/` - images attached to questions.
- `Assets/Media/Questions/Audio/` - audio attached to questions.
- `Assets/Images/` - local default team picture (`kawal.jpg`) referenced by the team card scene.

The Question Creator uses the operating system file picker, so a picture or a clip can come from anywhere on the disk. Selected files are copied into `Assets/Media/Questions/Images` and `Assets/Media/Questions/Audio` when the project folder is writable (the editor or a source checkout); otherwise they go to `user://question_media`. Both locations are local-only and never land in Git.

A question stores the copied media in `PhotoPath` and `AudioPath`. The game loads those paths from `res://`, `user://` or an absolute path, and falls back to the embedded `Photo`/`Audio` of older questions, so nothing has to be migrated.

Questions can also carry a video (`VideoPath`). The creator copies it to `Assets/Media/Questions/Video` (or `user://question_media/Video`) and the question screen plays it in place of the picture. Godot plays Ogg Theora natively, so the picker only accepts `.ogv` files; other containers need a codec extension.

The creator scene owns the dialogs: `ImageFileDialog`, `AudioFileDialog` and `VideoFileDialog` (`Access = Filesystem`, `UseNativeDialog = true`, `FileMode = Open File`, filters for PNG/JPG/WEBP, MP3/WAV/OGG and OGV) and `MediaErrorDialog` (`AcceptDialog`) for error messages. Assign them to the matching exports on `CreatorScreen` in the Inspector.

## Question metadata

Every question can carry a difficulty label and the game modes it is meant for. The `QuestionMetadataPanel` component (`Source/Components/QuestionMetadata/`) renders both and is instanced in the creator form as `QuestionMetadataPanel`.

- Difficulty levels live in `user://settings.cfg` under `[questions] difficulty_levels`; the defaults are `Easy`, `Medium`, `Hard`. The creator adds a level with the `New level` field and `Add`, removes the selected one with `Remove`, and the list survives restarts. The first option is `—`, which stores no label.
- Target modes are checkboxes built from `GameModeRegistry`, so a mode added to the registry appears without touching the scene. A question with no mode selected belongs to every mode.
- Saving keeps `IsBettingGameQuestion`, `IsSabotageGameQuestion` and `IsFraudGameQuestion` in sync with the selected modes, so older builds and hand-written `.tres` files still read the question the same way. Questions saved before the metadata existed have an empty mode list and load unchanged.
- `QuestionsManager` only offers questions the active mode accepts, and the validator warns about an unknown difficulty label or an unknown mode id.

### Question types

`Question.Type` selects how a question is asked and validated.

| Type | Answers | Validation |
|------|---------|------------|
| `MultipleChoice` (default) | 4 | four answers, the first one is correct, no empty or duplicate text |
| `TrueFalse` | 2 | exactly two answers, the first one is correct |
| `OpenEnded` | 1 or more | the first answer is the reference answer, the rest are accepted answers |

Questions saved before this field existed have no type and keep loading as `MultipleChoice`. The creator switches the type with the `Question type` option and only requires as many answer fields as the type needs. In the game the answer grid is hidden for open-ended questions until the host reveals the answers, so players never see the reference answer up front. The importer takes an optional `type` field (`multiple`, `truefalse`, `openended`).

# Question data
Question sets are **not** part of this repository. Only the examples under `Resources/Examples/` are tracked:

- `Resources/Examples/Questions/Pool1/` - four example questions.
- `Resources/Examples/Categories/` - four example categories.

On first run the examples are copied into the user data folder (`user://questions` and `user://categories`). If the legacy local folders `Resources/Questions` or `Resources/Categories` still exist, their contents are migrated instead, so existing local quiz data is preserved. After that, the game and the Question Creator work on the user copies, not on the repository files.

The question folder is resolved at startup, in this order:

1. `--questions-dir=<path>` user command line argument (passed after `--`).
2. `MINDSTAKES_QUESTIONS_DIR` environment variable.
3. `mindstakes/questions_dir` project setting (Project Settings → Mindstakes → Questions Dir).
4. `<folder of the executable>/questions`, in exported builds, when the folder exists.
5. `user://questions`, seeded from `Resources/Examples/Questions` on first run.
6. `res://Resources/Examples/Questions` (read-only fallback).

Categories use the same order with `--categories-dir`, `MINDSTAKES_CATEGORIES_DIR`, `mindstakes/categories_dir`, `<folder of the executable>/categories`, `user://categories` and `res://Resources/Examples/Categories`.

Options 1–3 accept any folder, including one outside the project. To keep answers out of the repository *and* out of exported builds, point them at a folder outside the project:

```powershell
$env:MINDSTAKES_QUESTIONS_DIR = "C:\MindStakes-data\questions"
$env:MINDSTAKES_CATEGORIES_DIR = "C:\MindStakes-data\categories"
godot --path "C:\Github\MindStakes"
```

Start Godot (or the exported game) from that shell. The Question Creator writes new questions to the same folder, so nothing lands in `Resources/Questions` while it is set.

If an external folder is not provided, the exported game seeds the examples into `user://`. The export presets already exclude `Resources/Questions/*` and `Resources/Categories/*`, so legacy local data is not packed into the game. If you keep data in another `res://` folder, add that folder to the export preset's *exclude filter*.

A client-side game has to know the correct answer in order to reveal it, so anyone with full access to the machine running the game can recover the data. Keeping it out of the repository and out of the exported `.pck` is what prevents accidental disclosure.

## Pool sizing

One game session consumes as many questions as `MaxRounds` (28 by default in `Source/Managers/GameManager/GameManager.tscn`), which is about 30 minutes per mode at the default answer time.

The pool is reloaded from disk every time the game-mode screen is opened, but questions already consumed in the current game session are skipped. Switching modes therefore does not repeat them; the consumed-question list is cleared only when a new game starts from the title screen. To play all three modes for 30 minutes each, use one pool per mode and select it in the game-mode screen:

| Pool | Mode | Questions |
|------|------|-----------|
| `Pool1` | Betting | 30-35 |
| `Pool2` | Sabotage | 30-35 |
| `Pool3` | Fraud | 30-35 |

That is roughly 90-105 questions for 90 minutes. Categories without unused questions are dropped from the random category draw, so no round is wasted on an exhausted category. A pool smaller than the round count still runs out and leaves a round without a question, so keep at least as many questions as rounds.

## Bulk import

The importer writes `.tres` files through `ResourceSaver`, so the output is identical to what the Question Creator saves. It validates every row first and writes nothing when any row is invalid.

```powershell
$env:MINDSTAKES_QUESTIONS_DIR = "C:\MindStakes-data\questions"
godot --headless --path "C:\Github\MindStakes" -- --import-questions=C:\MindStakes-data\questions.csv
```

Optional flags:

- `--import-pool=<n>` - fallback pool when the file has no pool column.
- `--import-dry-run` - validate the file and print the target paths without writing anything.

CSV accepts comma, semicolon or tab delimiters and requires a header row:

```csv
pool,category,question,correct,answer2,answer3,answer4,fun_fact,image,audio
1,Minecraft,"Jaki jest obecnie rekord świata w Minecraft (RSG)?",6m 50s,7m 01s,8m 18s,9m 33s,Rekord ustanowiono w 2024.,res://Assets/Media/Questions/Images/foo.png,
2,Anime,Kim jest Muhammad Avdol?,Postacią z JoJo,Nie wiem,Postacią z One Piece,Postacią z Naruto,,,
```

JSON is an array of objects with the same field names (camelCase works too):

```json
[
  {
    "pool": 1,
    "category": "Minecraft",
    "question": "Jaki jest obecnie rekord świata w Minecraft (RSG)?",
    "correct": "6m 50s",
    "answer2": "7m 01s",
    "answer3": "8m 18s",
    "answer4": "9m 33s",
    "funFact": "Rekord ustanowiono w 2024."
  }
]
```

`category` is either a name from the active categories folder or a `res://` / `user://` path. `image` and `audio` must currently be `res://` paths; keep those files under `Assets/Media/Questions/Images` and `Assets/Media/Questions/Audio` so they stay out of Git. `fun_fact`, `image`, `audio` and `video` (an `.ogv` path) are optional.

`difficulty` and `modes` are optional too. `modes` is a pipe-separated list of mode ids (`betting|sabotage|fraud`); an empty value means the question belongs to every mode, and the importer keeps the legacy per-mode flags in sync. `type` accepts `multiple`, `truefalse` or `openended` and defaults to multiple choice.

## Validating pools

```powershell
godot --headless --path "C:\Github\MindStakes" -- --validate-questions
```

Prints the question count and category split for every pool, and warns about pools below the session length, empty questions or answers, missing categories and duplicate answer text. The exit code is `1` when a pool contains invalid questions. The same summary is printed whenever a pool is loaded in game and after every save in the Question Creator.

### This project is still a work in progress.

What I want to add:
- custom category creator
- export for questions
- custom game modes creator

# Download

Every push to `main` builds Windows, Linux and macOS and publishes them as a pre-release named after an auto-bumped patch version (`v0.1.1`, `v0.1.2`, ...). Pushing a `v*` tag publishes the same three builds as a full (non pre-release) release. Both are produced by the [Godot export](https://github.com/sighmonk/MindStakes/actions/workflows/godot-export.yml) workflow, which also keeps the builds as downloadable artifacts for 14 days.

The version is taken from the latest `v*` tag. A merge to `main` bumps the patch number only, so a breaking or feature release still needs a `v*` tag with the intended minor or major version.

# Building from source

Requirements:

- [Godot 4.7.2 **.NET**](https://godotengine.org/download) (the standard build cannot run C#),
- [.NET 8 SDK](https://dotnet.microsoft.com/download),
- Git LFS, because any binary assets you add (images, audio) are committed through LFS.

```powershell
git lfs install
git clone https://github.com/sighmonk/MindStakes.git
cd MindStakes
dotnet build MindStakes.sln
```

Then open `project.godot` in the Godot .NET editor and press <kbd>F5</kbd>.

# Contributing

Bug reports, feature requests and pull requests are welcome. Start with the issues, follow [CONTRIBUTING.md](./CONTRIBUTING.md) for setup, branching and code style, and read [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md) before participating. Security issues go through [SECURITY.md](./SECURITY.md) - never a public issue.

# License

MindStakes is released under the [MIT License](./LICENSE).


<details>
<summary>Screenshots (WIP)</summary> 

<img width="1914" height="1076" alt="image" src="https://github.com/user-attachments/assets/9645f8fa-c4e3-4a6b-974a-2ef2a1938c06" />

<img width="1918" height="1085" alt="image" src="https://github.com/user-attachments/assets/653fb2a0-da0b-47b7-aed6-6a9c148ebd18" />

<img width="1909" height="1072" alt="image" src="https://github.com/user-attachments/assets/4cd0bc68-ae22-4cb2-8c51-f8e5666dfd6f" />

<img width="1921" height="1074" alt="image" src="https://github.com/user-attachments/assets/a81bf19f-57db-4cfe-958a-d8b835b8d86b" />
  
<img width="1910" height="1062" alt="image" src="https://github.com/user-attachments/assets/4c24d146-0e4f-453d-a7ff-4043006f2df0" />

<img width="1909" height="1068" alt="image" src="https://github.com/user-attachments/assets/1fa22950-4cb4-4cbf-ba11-37e3836d8dec" />

<img width="1911" height="1078" alt="image" src="https://github.com/user-attachments/assets/842f7a0e-1733-4582-85c1-55f13ac40934" />

</details>
