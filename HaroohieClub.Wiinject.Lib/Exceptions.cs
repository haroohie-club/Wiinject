using System;

namespace Wiinject.Lib
{
    public class WiinjectException : Exception
    {
        public WiinjectException() : base("Unknown HaroohieClub.Wiinject exception!")
        {
        }

        public WiinjectException(string message) : base($"HaroohieClub.Wiinject exception: {message}")
        {
        }
    }

    public class GccNotFoundException(string gccPath) : WiinjectException($"GCC executable not found on path ({gccPath})!")
    {
    }

    public class ObjdumpNotFoundException(string objdumpPath) : WiinjectException($"Objdump executable not found on path ({objdumpPath})!")
    {
    }

    public class AddressCountMismatchException : WiinjectException
    {
        public AddressCountMismatchException() : base("You must provide the same number of injection addresses and end addresses!")
        {
        }
    }

    public class InjectionSitesTooSmallException(string message) : WiinjectException(message)
    {
    }

    public class DuplicateVariableNameException(string variableName) : WiinjectException($"Duplicate variables '{variableName}' detected.")
    {
    }

    public class FailedToReplaceBlException(string blInstruction) : WiinjectException($"Failed to replace bl in instruction `{blInstruction}`")
    {
    }
}
