# Wiinject

Wiinject is a tool for injecting ASM hacks into Wii games using Riivolution memory patches. Pass it a folder containing `.s` PowerPC assembly files
and an injection site and it will assemble the files and give you a memory file and a series of memory patches for Riivolution XML.

Wiinject relies on the [Keystone Engine](https://www.keystone-engine.org/) to assemble code.

## Usage

### CLI Options
* `-f|--folder` &ndash; The folder where your `.s` ASM files live
* `-i|--injection-address` &ndash; The address where the assembly code will be injected. Find a portion of the ROM that is safe to overwrite and put its address here
* `-e|--end-injection` &ndash; The address where the above injection section ends. If the code goes past this point, an error will be thrown.
* `-o|--output-folder` &ndash; The folder to output the Riivolution patch.xml & assembled ASM bin file.
* `-n|--patch-name` &ndash; The name of the patch to output. The patch will be output to `{output_folder}/Riivolution/{patch_name}.xml`
                            and the ASM bin will be output to `{output_folder}/{patch_name}/patch.bin`.
* `-p|--input-patch` &ndash; The base Riivolution patch that will be modified by Wiinject to contain the memory patches. A blank base template will be created if this is not provided.
* `--console-output` &ndash; Rather than producing an ASM patch, simply output the XML to the console. This will still save the ASM bin, however.

### Writing ASM
Wiinject uses the Keystone Engine to assemble standard PowerPC assembly. To write an assembly file that Wiinject can parse, however, you need to use hooks.

Here is a sample Wiinject-compatible assembly file:

```assembly
hook_80017250:
    add 5,5,0
    mr 26,3
    blr

hook_80017254:
    mr 3,26
    blr
```

The hooks indicate which instructions to replace with a branch instruction to the function provided.

## Limitations

* Wiinject does not currently support the `bl` command within assembly functions, e.g. `bl =hook_blah` will not currently work.
* Wiinject currently only supports base patches with a single `<patch>` element.

## Source & Building

Wiinject.sln can be opened in Visual Studio 2019 or (presumably) later. You can easily build it from there.

You can also build Wiinject.sln from the command line on any platform that supports .NET 5.0 with `dotnet build` in the root directory.