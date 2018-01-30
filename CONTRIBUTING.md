# Contributing

Looking to contribute something? **Here's how you can help.**

## Pull requests

Good pull requests, patches, improvements and new features are a fantastic
help. They should remain focused in scope and avoid containing unrelated
commits.

**Please ask first** before embarking on any significant pull request (e.g.
implementing features, refactoring code), otherwise you risk spending a lot 
of time working on something that the project's developers might not want 
to merge into the project.

Please adhere to the [coding guidelines](#code-guidelines) used throughout the
project (indentation, accurate comments, etc.) and any other requirements
(such as test coverage).

Adhering to the following process is the best way to get your work
included in the project:

1. [Fork](http://help.github.com/fork-a-repo/) the project, clone your fork,
   and configure the remotes:

   ```bash
   # Clone your fork of the repo into the current directory
   git clone https://github.com/your_username/QLNet
   # Navigate to the newly cloned directory
   cd <folder-name>
   # Assign the original repo to a remote called "upstream"
   git remote add upstream https://github.com/amaggiulli/QLNet
   ```

2. If you cloned a while ago, get the latest changes from upstream:

   ```bash
   git checkout develop
   git pull upstream develop
   ```

3. Create a new topic branch (off the main project development branch) to
   contain your feature, change, or fix:

   ```bash
   git checkout -b <topic-branch-name> upstream/develop
   ```

4. Commit your changes in logical chunks. Please adhere to these [git commit
   message guidelines](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html)
   or your code is unlikely be merged into the main project. Use Git's
   [interactive rebase](https://help.github.com/articles/interactive-rebase)
   feature to tidy up your commits before making them public. 

5. Locally merge (or rebase) the upstream development branch into your topic branch:

   ```bash
   git pull [--rebase] upstream develop
   ```

6. Push your topic branch up to your fork:

   ```bash
   git push origin <topic-branch-name>
   ```

7. [Open a Pull Request](https://help.github.com/articles/using-pull-requests/)
    with a clear title and description against the `master` branch.


## Visual Studio set-up
- In Visual Studio under `Tools > Options > Text Editor > C# > Advanced`, make sure
  `Place 'System' directives first when sorting usings` option is disabled (unchecked).
- Install the [CodeMaid](http://www.codemaid.net) extension.
- If you are using Visual Studio 2015 or lower, ensure that you have installed the [EditorConfig](http://editorconfig.org) extension.

## Code guidelines
- Base all pull requests on the `develop` branch.
- Ensure that all unit tests pass.
- Before committing your changes, do a CodeMaid clean-up (`Ctrl+M, Space`) of all the affected files. This action will sort and remove unnecessary `using` directives, correct code indentation and put braces (`{`, `}`) on the appropriate lines.
- Line endings of the `.cs` files should be `CRLF`.
