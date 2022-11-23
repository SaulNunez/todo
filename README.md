# ✏️ todo an unofficial multiplatform CLI for Microsoft To-Do

⚠️ **Warning:** This tool is super early in development, commands and flags might change between updates!

## Install
### Windows
#### Standalone

#### winget

### Linux
#### Snap

#### Flatpack

### MacOS
#### Brew

## Setup
```bash
todo login
```
Logins to your Microsoft account.

```bash
todo logout
```

## Tasks
### Creating a task
```bash
todo add
```
Use `stdin` for title of task. In interactive UI, a CLI wizard will help filling all available fields.

```bash
todo add "Buy milk" --date "" --notes "" --add-to-my-day --remind --repeat weekly --checklist "" --checklist "" --file "" --file ""
```

### Marking completion
```bash
todo check "Buy milk"
```
Marks a task as completed.

### Delete task
```bash
todo delete "Buy milk"
```

### Show all tasks
```bash
todo
todo tasks
```
Todo without arguments will show tasks in the default list.

## Lists
### Create list
```bash
todo list add "My cool project"
```

### Delete list
```bash
todo list delete "My cool project"
```

### Show all lists
```bash
todo list show
```

### Work with tasks in a list
```bash
todo list "My cool project" add "Design cool easter egg"
```

```bash
todo list "My cool project" delete "Design cool easter egg"
```

```bash
todo list "My cool project"
todo list "My cool project" tasks
```
Show tasks in "My Cool project" list.

### Aliases
Rather than typing `list "My day"` or any other list created by default. The following aliases can be used:

```bash
todo myday
```

```bash
todo important
```

```bash
todo planned
```

```bash
todo assigned
```