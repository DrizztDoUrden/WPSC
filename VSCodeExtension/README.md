# WPSC README

This extensions provides ability to inject custom lua code with or without a module system to a WC3 map.

## Features

DISCLAIMER: I don't give you any guaranty that nothing will ever get corrupted. I have only tested it on some of 1.32 versions of WC3. I can only tell that I have polished the stuff untill it became usable for my map.
I can tell for sure I haven't implemented any stuff compatible with pre-1.31 WC3.

Currently implemented:
* Module system
* Lua files content is placed in separate triggers preserving folder structure and existing triggers.

Plans:
* GUI triggers support (I have to parse that part of .wtg, sorry, haven't got time yet)
* Updating wc3map.lua so it would be possible to launch map without resaving it.
* F5 WC3 launch

## Requirements

Requires [dotnet core 3.1](https://dotnet.microsoft.com/download)

## Extension Settings

This extension contributes the following settings:

* `wpsc.map`: path to map directory.
* `wpsc.excludes`: exclude some files from injecting.
* `wpsc.moduleSystem`: override default module system or disable it completely. Documentation for overriding will be made later. 

## Known Issues

* You need to save map in editor after injecting code to generate wc3map.lua.
* Maps with ANY GUI triggers are not supported for now.

## Release Notes

### 0.1.0
Initial release

-----------------------------------------------------------------------------------------------------------

**Enjoy!**
