# ZeroEngine.EditorTools

Project-neutral editor tool registries, task execution, windows and source guards.
Projects provide their own profiles, commands, production paths and validation policy.

## Exact file rollback

`EditorFileSnapshot(path, absoluteProjectRoot)` captures bytes and existence for one
explicit file. `MatchesCurrent`, `Restore` and `AssertRestored` preserve unchanged
bytes without rewriting timestamps. Restoring a missing parent fails closed unless
the caller explicitly opts into recreation. Absolute temporary paths are supported;
relative paths cannot escape the given project root. Directories are not snapshots.

The snapshot does not discover paths, pair `.meta` files, delete Unity assets, import,
clear dirty state, restore scenes/Undo, acquire write permission or catch an owner's
transaction failure. Capture the reviewed exact asset and metadata list before writing;
keep those policies and error reporting in the authoring owner. Asset-path hints only
help the owner choose its existing import route and never execute it automatically.
