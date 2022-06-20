# Wiinject

Wiinject is a cross-platform tool for injecting ASM hacks into Wii games using Riivolution memory patches. Pass it a folder containing `.s` PowerPC
assembly files (and, if you'd like, `.c` C files) and a series of injection sites and it will assemble the files and give you a memory file and a
series of Riivolution XML memory patches.

Wiinject relies on the [Keystone Engine](https://www.keystone-engine.org/) to assemble code and [devkitPro](https://devkitpro.org/) to compile C code.

## Usage

### Prerequisites
Wiinject requires the following to run:
* The [.NET 6.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* [devkitPro](https://devkitpro.org/wiki/Getting_Started) if compiling C code

### CLI Options
* `-f|--folder` &ndash; The folder where your source files live
* `-m|--dolphin-map|--map|--symbols` &ndash; A Dolphin symbols map for any built-in functions you want to reference by name
* `-i|--injection-addresses` &ndash; The addresses to inject function code at, comma delimited. The code at these addresses should be safe to overwrite.
* `-e|--injection-ends` &ndash; The addresses at which the above injection sites end (are no longer safe to overwrite), comma delimited.
                                If the code is unable to fit in any of these injection sites, an error will be thrown.
* `-o|--output-folder` &ndash; The folder to output the Riivolution patch.xml & assembled ASM bin file to.
* `-n|--patch-name` &ndash; The name of the patch to output. The patch will be output to `{output_folder}/Riivolution/{patch_name}.xml`
                            and the ASM bin will be output to `{output_folder}/{patch_name}/patch.bin`.
* `-p|--input-patch` &ndash; The base Riivolution patch that will be modified by Wiinject to contain the memory patches. A blank base template will be created if this is not provided.
* `d|devkitpro-path=` &dash; The path to a devkitPro installation containing devkitPPC (e.g. `C:\devkitPro` or `/opt/devkitpro`)
* `--console-output` &ndash; Rather than producing an ASM patch, simply output the XML to the console. This will still save the ASM bin, however.
* `--emit-c` &ndash; Emits assembled C functions to the console so you can modify your assembly calls to those functions to work with the registries used by the compiler.

### Structuring Your Source Code and Preparing Your Initial Patch

Wiinject expects the `folder` where your source lives to have one subdirectory for each patch element you wish to generate. For example, if you'd like your final Riivolution patch
to contain one optional patch for translating the game and another for reducing monster spawns, name one subdirectory something like `Translation` and the other `ReduceMonsterSpawns`.
Then, place your source files relevant to those patches in those directories.

When preparing your input patch, make sure you set up the options yourself and ensure that the patch names in the options match the names of the subdirectories in your source `folder`.
Finally, add any patch elements that have non-memory patches. Wiinject will automatically create patch elements that don't exist and append to ones that do.

If you've followed along, your input patch should look something like this:

```xml
<wiidisc version="1">
  <id game="R42069" />
  <options>
    <section name="Translation">
      <option name="Translation">
        <choice name="Enabled">
          <patch id="Translation" />
        </choice>
      </option>
    </section>
    <section name="Quality of Life">
      <option name="Reduce Monster Spawns">
        <choice name="Enabled">
          <patch id="ReduceMonsterSpawns" />
        </choice>
      </option>
    </section>
  </options>
  <patch id="Translation">
    <folder external="/Game/files" recursive="true" disc="/" />
    <folder external="/Game/files" />
  </patch>
</wiidisc>
```

### Writing ASM
Wiinject uses the Keystone Engine to assemble standard PowerPC assembly. To write an assembly file that Wiinject can parse, however, you need to use special function names.

Here is a sample Wiinject-compatible assembly file:

```assembly
hook_80017250:
    start:
        add 5,5,0
        mr 26,3
        cmpwi 5,3
        beq end
        li 5,2
    end:
        blr

hook_80017254:
    mr 3,26
    blr

repl_80017260:
    mr 5,25
    li 6,7

ref_801BBB38:
    li 6,7
    blr
```

The `hook`s indicate which instructions to replace with a branch instruction to the function provided. The `repl` indicates a location to start overwriting
instructions directly with the instructions provided. The `ref` indicates a location to write a reference to the function provided (useful for hooking into functions
that use `bctrl`, etc.).

### Writing C

For each ASM file, you may also provide a companion C file to compile and inject methods which may then be called from the ASM. In order to use this functionality, you must
install [devkitPro](https://devkitpro.org/wiki/Getting_Started) and provide Wiinject with the path to the devkitPro installation (e.g. `C:\devkitPro` or `/opt/devkitpro`).

Injected C methods are called from the ASM via `bl =method_name`. The assembly function caller will need to handle stack manipulation and inputs itself. You can expect
a compiled C method to accept inputs sequentially starting with `r3` and to place its return value in `r3`. However, in order to verify this, you can use the `--emit-c` flag while
calling Wiinject to view the compiled C code's assembly so you can adjust your assembly caller appropriately.

Here is a sample Wiinject-compatible C file named *font_hack.c*:

```c
int font_offset(char character)
{
    switch (character)
    {
        case 'A':
            return 0x180;
        case 'I':
        case 'i':
        case 'l':
        case '!':
            return 0x48;
        default:
            return 0x90;
    }
}
```

And here is its companion ASM file named *font_hack.s*:

```assembly
hook_8001726C:
    stwu 1,-24(1)
    mflr 0
    stw 0,20(1)
    stw 31,16(1)
    mr 31,1
    stw 9,12(1)
    stw 3,8(1)
    mr 3,26
    cmpwi 3,75
    bl =font_offset
    lwz 0,20(1)
    mtlr 0
    mr 0,3
    lwz 9,12(1)
    lwz 3,8(1)
    addi 11,31,24
    lwz 31,-4(11)
    mr 1,11
    blr
```

### Using Symbols
You can provide Wiinject with a Dolphin symbols map and use that to reference functions existing in the ASM in the same way you would reference C functions.

Map file:
```
.text section layout
80004000 00000050 80004000 0 memcpy
```

Assembly:

```assembly
hook_8001726C:
    stwu 1,-24(1)
    mflr 0
    stw 0,20(1)
    bl =memcpy
    lwz 0,20(1)
    mtlr 0
    addi 1,24
    blr
```

## Limitations

* Wiinject only supports the `bl` command for C functions or functions defined in your Dolphin symbols map; functions defined in assembly cannot currently be branched to.
* The [paired single operators](https://wiibrew.org/wiki/Paired_single) are not available.

## Source & Building

Wiinject.sln can be opened in Visual Studio 2022 or (presumably) later and built from there. You can also build Wiinject.sln from the command line on any platform that
supports .NET 6.0 with `dotnet build` in the root directory. If you're struggling to get Wiinject to run properly after compilation, try explicitly running with the RID
of the platform you're building for (e.g. `dotnet build -r osx-64 Wiinject/Wiinject.csproj`).