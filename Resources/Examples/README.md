# Example data

This folder ships a few sample categories and questions so a fresh clone is playable.

- `Categories/` contains four example categories.
- `Questions/Pool1/` contains four example questions.

The game does **not** read these files directly during a normal session. On first run it copies them into the user data folder:

- `user://categories`
- `user://questions`

If the legacy local folders `Resources/Categories` or `Resources/Questions` still exist, they are migrated instead of the examples. After the first run, edit the user data copies. The Question Creator writes there as well. To keep data on a shared drive or outside the user profile, start the game with `--questions-dir=<path>` and `--categories-dir=<path>`, or set `MINDSTAKES_QUESTIONS_DIR` and `MINDSTAKES_CATEGORIES_DIR`.

Do not commit real quiz data here. Only examples belong in `Resources/Examples`.
