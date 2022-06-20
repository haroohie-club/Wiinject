using System;

namespace Wiinject
{
    public class WiinjectException : Exception
    {
        public WiinjectException() : base("Unknown Wiinject exception!")
        {
        }

        public WiinjectException(string message) : base($"Wiinject exception: {message}")
        {
        }
    }

    public class GccNotFoundException : WiinjectException
    {
        public GccNotFoundException(string gccPath) : base($"GCC executable not found on path ({gccPath})!")
        {
        }
    }

    public class ObjdumpNotFoundException : WiinjectException
    {
        public ObjdumpNotFoundException(string objdumpPath) : base($"Objdump executable not found on path ({objdumpPath})!")
        {
        }
    }

    public class AddressCountMismatchException : WiinjectException
    {
        public AddressCountMismatchException() : base("You must provide the same number of injection addresses and end addresses!")
        {
        }
    }

    public class InjectionSitesTooSmallException : WiinjectException
    {
        public InjectionSitesTooSmallException(string message) : base(message)
        {
        }
    }

    public class DuplicateVariableNameException : WiinjectException
    {
        public DuplicateVariableNameException(string variableName) : base($"Duplicate variables '{variableName}' detected.")
        {
        }
    }
}
