# ✏️ todo an unofficial multiplatform CLI for Microsoft To-Do

⚠️ **Warning:** This tool is super early in development, commands and flags might change between updates!

## Install
### Windows
#### Standalone
Please check in the Release section of this repository.

#### winget

### Linux
#### Snap

#### Flatpack

#### Standalone
Please check in the Release section of this repository.

### MacOS
#### Brew

## Setup

## Tasks
### Creating a task
```bash
todo add "Buy milk" --date "" --notes "" --add-to-my-day --remind --repeat weekly --checklist "" --checklist "" --file "" --file ""
```

### Marking completion
```bash
todo check "Buy milk"
```
Marks a task as completed.

```bash
todo uncheck "Buy milk"
```
Mark a task as not done.

### Delete task
```bash
todo delete "Buy milk"
```

### Show all tasks
```bash
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
todo lists
```

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