![Build status](https://github.com/Hefaistos68/VSLauncherX/actions/workflows/dotnet.yml/badge.svg)

Note: for some yet unknown reason, the Visual Studio WMI classes may be removed from the WMi catalog after installing VS 2022 17.7.5

# VSLauncherX

This application is a helper application for developers who use Visual Studio and/or Visual Studio Code as their main tools for building applications.

![Main screen](https://github.com/Hefaistos68/VSLauncherX/blob/master/docs/MainScreen-sample.jpg)

At its core, it is a list of recently used solutions, projects, and workspaces across any currently installed Visual Studio and Visual Studio Code edition. It can detect all installed Visual Studio versions from 2017 to 2026 as well as VS Code (Stable and Insiders, User and System installs), including all profiles created in any of them.

Note: will use SoP as abbreviation for "Solutions and/or Projects"

## Main Features:

- Make groups of SoP's that can be used all at once
- Multi-IDE support for both Visual Studio and Visual Studio Code (works even when only one IDE is installed)
- Define item specific settings like "Run as Admin", execute another application or command before and/or after Visual Studio or VS Code
- Use a specific Visual Studio profile or VS Code profile (`--profile`) for each item
- Import all SoP's, `.code-workspace` files, and VS Code project folders from a directory as a group
- Launch each SoP on a defined display or monitor
- Quick search and execute (automatically expands matching groups)
- Automatically synchronize the list(s) with Visual Studio's and VS Code's recent lists
- Built-in VS Code sidebar quick actions and daily update check with one-click installer/updater
- No limit on "recent files" list, can add an almost unlimited amount of SoPs
- Grouping with subgroups
- Favorites that can be invoked directly from the taskbar context menu
- Show git status of solutions/projects (Clean/Modified)
- Show admin privileges and elevation status of application
- Autostart and always run as Admin
- Light and Dark mode support

## The Why

As enterprise developer one is often in the situation to run multiple Visual Studio instances side by side, with different SoP's and configurations. 

Doing so manually is most often a tedious and time consuming process.

Also, as of late, the "Start Window" of Visual Studio is not the most helpful, besides its inherent limitations of managing its recently used list.

## Working Features
- Importing from folders (Visual Studio `.sln`/`.slnx`/projects, `.code-workspace` files, and Node.js, Python, Rust, Go, Java, CMake, Flutter, and PHP project folders)
- Importing recent items from any Visual Studio version/profile and Visual Studio Code edition (`state.vscdb`)
- Support for VS 2017, 2019, 2022 and 2026 (including `.slnx` solutions)
- Support for Visual Studio Code (Stable & Insiders, User & System installs, and VS Code profiles)
- Tabbed IDE sidebar for Visual Studio and VS Code quick actions (Start, Run as Admin, New Window, Start without Extensions, Open ActivityLog, and VS Code Installer/Updater with daily update checks)
- Dedicated VS Code item settings (edition, profile, window mode, `--disable-extensions`, custom arguments, monitor, elevation, Before/After commands)
- Automatic pinning of newly added and folder-scanned `.code-workspace` entries to VS Code
- Direct VS Code launching for folders configured for VS Code (even if empty) and `Open in VS Code` context menu action
- Launching an entire group with subgroups of SoP's, each on its defined IDE version, working folder and profile
- execute other apps before and/or after each group or SoP
- Launch each SoP on a specific monitor (not 100% verified on different setups)
- Quick search and execute
- Detection of non-existing SoP's when importing from VS or VS Code
- User elevation for Run as Admin
- Run other commands before and/or after an SoP or group, optionally wait for completion before the next action
- Light and Dark mode theming

## Planned Features
- Automatically synchronize with VS
- better layout and graphics
- Taskbar integration for even quicker launching
- Support for non-Windows environments 

## How to use
First either import your recent lists from Visual Studio and VS Code using the toolbar import button, or scan a folder directly with the folder-up-arrow toolbar button. You can also drag solutions, projects, `.code-workspace` files, or folders from Explorer directly into the list.

After you have at least one SoP or workspace in the list, you can start it by pressing Enter on the selected item or double clicking it. When you double click on a folder/group, the whole folder will be launched, each item in its own IDE instance as defined in the item's settings — unless the folder itself is configured for VS Code (or contains a `.code-workspace` file), in which case it opens directly in VS Code even if the folder has no child items.

Use the tabbed right sidebar to switch between Visual Studio and VS Code quick actions, pick default editions, open logs, or install/update VS Code when an update is detected.

As expected, you can use drag and drop to order items, Alt+Enter to open the item's settings (Visual Studio or VS Code settings depending on the item), Del to delete an item, etc. Remember that the settings of a folder/group item are applied before all items. This means that when you set a folder to execute as admin, all the contained SoPs will run as admin too.
You can set for each item which installed Visual Studio version or VS Code edition (and profile) to open it with. The recommended Visual Studio version is shown as defined in the solution file, and newly added or folder-scanned `.code-workspace` entries automatically select your preferred installed VS Code edition.
There are also context menus that you can invoke by right clicking on an item, including `Open in VS Code` to open any item or folder in VS Code on demand.
When you "Favorite" an item, it is added to the Tasks context menu in the Taskbar app icon.


![SoP Settings](https://github.com/Hefaistos68/VSLauncherX/blob/master/docs/Settings-sample.jpg)


