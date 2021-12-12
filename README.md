# Wiinject

Wiinject is a tool for injecting ASM hacks into Wii games using Riivolution memory patches. Pass it a folder containing `.s` PowerPC assembly files
and an injection site and it will assemble the files and give you a memory file and a series of memory patches for Riivolution XML.

## Usage

`Wiinject -f [ASSEMBLY_DIRECTORY] -i [INJECTION_SITE] -e [LENGTH_OF_INJECTION_SITE]`