![Build status](https://github.com/Hefaistos68/VSLauncherX/actions/workflows/dotnet.yml/badge.svg)

# VSLauncherX

This application is a helper application for developers who use Visual Studio as their main tool for building applications.

![Main screen](https://github.com/Hefaistos68/VSLauncherX/blob/master/docs/MainScreen-sample.jpg)

At its core, it is a list of recently used solutions and projects in any currently installed Visual Studio version. It can detect all installed versions from 2017 to 2022 currently, including all profiles created in any of them.

Note: will use SoP as abbreviation for "Solutions and/or Projects"

## Main Features:

- Make groups of SoP's that can be used all at once
- Define item specific settings like "Run as Admin", execute another application or command before and/or after Visual Studio
- Use a specific VS Profile for each SoP
- Import all SoP's from a folder as a group
- Launch each SoP on a defined display or monitor
- Quick search and execute
- Automatically synchronize the list(s) with Visual Studios recent list
- No limit on "recent files" list, can add an almost unlimited amount of SoPs
- Grouping with subgroups
- Favorites that can be invoked directly from the taskbar context menu
- Show git status of solutions/projects (Clean/Modified)
- Show admin privileges and elevation status of application
- Autostart and always run as Admin

## The Why

As enterprise developer one is often in the situation to run multiple Visual Studio instances side by side, with different SoP's and configurations. 

Doing so manually is most often a tedious and time consuming process.

Also, as of late, the "Start Window" of Visual Studio is not the most helpful, besides its inherent limitations of managing its recently used list.

## Working Features
- Importing from folders
- Importing from any Visual Studio version and profile
- Support for VS 2017, 2019 and 2022
- Launching an entire group with subgroups of SoP's, each on its defined VS version, working folder and VS Profile
- execute other apps before and/or after each group or SoP
- Launch each SoP on a specific monitor (not 100% verified on different setups)
- Quick search and execute
- Detection of non-existing SoP's when importing from VS
- User elevation for Run as Admin
- Run other commands before and/or after an SoP or group, optionally wait for completion before the next action

## Planned Features
- Automatically synchronize with VS
- Support for Visual Studio Code
- better layout and graphics
- Taskbar integration for even quicker launching
- Support for dark mode
- Support for non-Windows environments 

## How to use
First either import your whole recent list from VS by using the toolbar button with the VS symbol, or import from folder directly with the folder-up-arrow toolbar button. You can also drag solutions or projects from the explorer directly into it.

After you have at least one SoP in the list, you can start it by pressing enter on the selected item or double clicking it. When you double click on a folder/group then the whole folder will be launched, each item in its own VS instance as defined in the items settings.

As expected, you can use drag and drop to order items, alt+enter to open the items settings, del to delete an item, etc. Remember that the settings of a folder/group item are applied before all items. This means that when you set a folder to execute as admin, all the contained SoPs will run as admin too. 
You can set for each SoP which (installed) version of Visual Studio you want to open it with. Recomended version is shown for each as it is defined in the solution file.
There are also context menus that you can invoke with right clicking on an item.
When you "Favorite" an item, it is added to the Tasks context menue in the Taskbar app icon.


![SoP Settings](https://github.com/Hefaistos68/VSLauncherX/blob/master/docs/Settings-sample.jpg)


