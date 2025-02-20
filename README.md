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

## Lists
### Create list
```bash
todo lists add "My cool project"
```

### Delete list
```bash
todo lists delete "My cool project"
```

### Show all lists
```bash
todo lists
```

### Show all tasks
```bash
todo tasks "Shopping List"
```

## Tasks
### Creating a task
```bash
todo tasks "Shopping List" add "Buy milk" --due-date "2025-02-19" --reminder-date "2025-02-18 11:59:00" --notes "Whole milk"
```

### Marking completion
```bash
todo tasks "Shopping List" check "Buy milk"
```
Marks a task as completed.

```bash
todo tasks "Shopping List" uncheck "Buy milk"
```
Mark a task as not done.

### Delete task
```bash
todo tasks "Shopping List" delete "Buy milk"
```