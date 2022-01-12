# Wiinject

Wiinject is a cross-platform tool for injecting ASM hacks into Wii games using Riivolution memory patches. Pass it a folder containing `.s` PowerPC
assembly files (and, if you'd like, `.c` C files) and a series of injection sites and it will assemble the files and give you a memory file and a
series of Riivolution XML memory patches.

Wiinject relies on the [Keystone Engine](https://www.keystone-engine.org/) to assemble code and [devkitPro](https://devkitpro.org/) to compile C code.

## Usage

### Prerequisites
Wiinject requires the following to run:
* The [.NET 5.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/5.0)
* [devkitPro](https://devkitpro.org/wiki/Getting_Started) if compiling C code

### CLI Options
* `-f|--folder` &ndash; The folder where your source files live
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
```

The `hook`s indicate which instructions to replace with a branch instruction to the function provided. The `repl` indicates a location to start overwriting
instructions directly with the instructions provided.

### Writing C

For each ASM file, you may also provide a companion C file to compile and inject methods which may then be called from the ASM. In order to use this functionality, you must
install [devkitPro](https://devkitpro.org/wiki/Getting_Started) and provide Wiinject with the path to the devkitPro installation (e.g. `C:\devkitPro` or `/opt/devkitpro`).

Injected C methods are called from the ASM via `bl =method_name`. The assembly function caller will need to handle stack manipulation and inputs itself. Generally, you can expect
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

## Limitations

* Wiinject only supports the `bl` command in the context of C functions, i.e. `bl =hook_assemblyfunc` will not currently work.
* Multi-file C and headers and such are not supported. Define everything within a single file.
* Wiinject currently only supports base patches with a single `<patch>` element.
* The [paired single operators](https://wiibrew.org/wiki/Paired_single) are not available

## Source & Building

Wiinject.sln can be opened in Visual Studio 2019 or (presumably) later and built from there. You can also build Wiinject.sln from the command line on any platform that
supports .NET 5.0 with `dotnet build` in the root directory.