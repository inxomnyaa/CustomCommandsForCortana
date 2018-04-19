# CustomCommandsForCortana
Add commands to Cortana using a simple UI
[![Build status](https://ci.appveyor.com/api/projects/status/4s45g3ntddqqks48?svg=true)](https://ci.appveyor.com/project/thebigsmileXD/customcommandsforcortana)

# Requirements
Windows Falls Creators update (1709, tested on 16299.192)

This is a Windows UWP Application coded in Visual Studio 2017 in C#

# Running/Compiling/Installing
Clone to Visual Studio 2017 & build. Windows 10 will then show it in your Start Menu. Run the App atleast once to initialize. Do not forget to press the "Save" button when you are done. This will install the VoiceCommandDefinitions to Cortana automatically.

# Adding commands

# Settings
You can change the command prefix from "Command" (default) to anything you like.

The prefix is the "AppName", with which Cortana knows which program is meant to be executed.

# Use a voice command
Say "Hey Cortana, <prefix> <listen for>"

For example: "Hey Cortana, Command lights off"
# Example
Example: "Hey Cortana? Command hello world"

Prefix: "Command"

Command: "echo "hello world""

Listen For: "hello world"
