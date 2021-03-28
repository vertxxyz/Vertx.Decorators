# Vertx.Decorators
Attributes and Property Decorators for Unity that have access to the SerializedProperty used to draw the field.  

⚠️ This package requires 2020.3+ ⚠️

## Attributes

- **[TypeProvider(typeof(Example))]**  
Decorates a `[SerializeReference]` field, providing instances of a type that can easily be added via a dropdown.  
  

## Installation

<details>
<summary>Add from OpenUPM <em>| via scoped registry, recommended</em></summary>

This package is available on OpenUPM: https://openupm.com/packages/com.vertx.decorators

To add it the package to your project:

- open `Edit/Project Settings/Package Manager`
- add a new Scoped Registry:
  ```
  Name: OpenUPM
  URL:  https://package.openupm.com/
  Scope(s): com.vertx
            com.needle
  ```
- click <kbd>Save</kbd>
- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `com.vertx.decorators`
- click <kbd>Add</kbd>  
</details>

<details>
<summary>Add from GitHub | <em>not recommended, no updates through UPM</em></summary>

You can also add it directly from GitHub on Unity 2019.4+. Note that you won't be able to receive updates through Package Manager this way, you'll have to update manually.

- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `https://github.com/vertxxyz/Vertx.Decorators.git`
- click <kbd>Add</kbd>  
  **or**
- Edit your `manifest.json` file to contain `"com.vertx.decorators": "https://github.com/vertxxyz/Vertx.Decorators.git"`,

⚠️ decorators has a dependency on [Editor Patching](https://github.com/needle-tools/editorpatching) so ensure that is referenced into your project to use this package successfully. ⚠️

To update the package with new changes, remove the lock from the `packages-lock.json` file.
</details>