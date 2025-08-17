using System;

namespace HaroohieClub.Wiinject;

/// <summary>
/// Base class for Wiinject exceptions
/// </summary>
public class WiinjectException : Exception
{
    /// <summary>
    /// Base constructor for unknown Wiinject exceptions
    /// </summary>
    public WiinjectException() : base("Unknown Wiinject exception!")
    {
    }

    /// <summary>
    /// Constructor for Wiinject exceptions with a message parameter
    /// </summary>
    /// <param name="message">The message to include in the Wiinject exception</param>
    public WiinjectException(string message) : base($"Wiinject exception: {message}")
    {
    }
}

/// <summary>
/// An exception thrown when devkitPPC's GCC is not found
/// </summary>
/// <param name="gccPath">The path that the program attempted to find GCC at</param>
public class GccNotFoundException(string gccPath) : WiinjectException($"GCC executable not found on path ({gccPath})!")
{
}

/// <summary>
/// An exception thrown when devkitPPC's objdump is not found
/// </summary>
/// <param name="objdumpPath">The path that the program attempted to find objdump at</param>
public class ObjdumpNotFoundException(string objdumpPath) : WiinjectException($"Objdump executable not found on path ({objdumpPath})!")
{
}

/// <summary>
/// An exception thrown when the number of injection and end addresses do not match
/// </summary>
public class AddressCountMismatchException : WiinjectException
{
    /// <summary>
    /// Generic constructor for AddressCountMismatchException
    /// </summary>
    public AddressCountMismatchException() : base("You must provide the same number of injection addresses and end addresses!")
    {
    }
}

/// <summary>
/// An exception thrown when the provided injection sites are not big enough to contain the code provided
/// </summary>
/// <param name="message"></param>
public class InjectionSitesTooSmallException(string message) : WiinjectException(message)
{
}