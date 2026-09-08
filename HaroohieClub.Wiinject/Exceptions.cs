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
/// An exception thrown when devkitPPC's objcopy is not found
/// </summary>
/// <param name="objcopyPath">The path that the program attempted to find objcopy at</param>
public class ObjcopyNotFoundException(string objcopyPath) : WiinjectException($"Objcopy executable not found on path ({objcopyPath})!")
{
}

/// <summary>
/// An exception thrown when the number of injection and end addresses do not match
/// </summary>
public class ArenaLoMissingException : WiinjectException
{
    /// <summary>
    /// Generic constructor for AddressCountMismatchException
    /// </summary>
    public ArenaLoMissingException() : base("You must provide the arena lo address!")
    {
    }
}