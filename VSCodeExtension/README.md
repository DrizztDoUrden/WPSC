# WPSC README

This extensions provides ability to inject custom lua code with or without a module system to a WC3 map.

## Features

Map to work with must be saved as a folder. In theory .w3m folders are supported too, but I haven't tested and I am not sure there is any benefit from such thing now.

`ctrl+i` injects code into the map.

`ctrl+shift+i` injects code into the map and starts WC3 loading the map.

### Disclaimer
I don't give you any guaranty that nothing will ever get corrupted. I have only tested it on some of 1.32 versions of WC3. I can only tell that I have polished the stuff untill it became usable for my map.

I can tell for sure that I haven't implemented any stuff compatible with pre-1.31 WC3.

### Currently implemented:
* Module system
* Lua files content is placed in separate triggers preserving folder structure and existing triggers.
* Updating wc3map.lua so it would be possible to launch map without resaving it in editor.
* WC3 launch from VSCode via `ctrl+shift+i`

### Module system
Default module system supports both `Require` and `require`. This is how to use it to reference file "WC3\Rect.lua": `local WCRect = Require("WC3.Rect")`. Every file injected to a map becomes a module. Every module is loaded unless it has unresolved dependancies.

### Plans:
* GUI triggers support (I have to parse that part of .wtg, sorry, haven't got time yet)
* F5 WC3 launch as alternative to `ctrl+shift+i`
* Defining libraries.
* Defining root namespace, mostly for libraries.
* Overlaping per-directory configurations, for dependencies in repo.
* Support for out of source tree dependecy referencing
* Support for defining entry point and only injecting files of entry point requirement tree.

### Plans for module system:
* Support for lazy loading mode for modules

## Requirements

Requires [dotnet core 3.1](https://dotnet.microsoft.com/download)

## Extension Settings

This extension contributes the following settings:

* `wpsc.map`: path to map directory.
* `wpsc.excludes`: exclude some files from injecting.
* `wpsc.moduleSystem`: override default module system or disable it completely. Documentation for overriding will be made later. For now you may check the implementation if you wish, it is trivial.
* `wpsc.warcraftRoot`: path to root of WC3 to launch map by.

## Known Issues

* Maps with ANY GUI triggers are not supported for now.
* Maps saved as a file are not supported and there is no immediate plan to do so as I don't see any point to save map as an archive while developing.

## Release Notes

### 0.2.7
Updated module system Log function to provide more valuable output and moved its log file to Logs directory.

### 0.2.6
Updated module system to support classic `require` as well.

### 0.2.0
Implemented war3map.lua updating and launching WC3 from VSCode.

### 0.1.0
Initial release.


-----------------------------------------------------------------------------------------------------------

**Enjoy!**
