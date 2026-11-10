namespace Microsoft.EntityFrameworkCore;

// Compile-only stand-in for the unrelated delete catch; no database provider is loaded.
public class DbUpdateException : Exception;
