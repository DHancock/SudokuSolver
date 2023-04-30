namespace SudokuSolver.Utilities;

internal static class IntegrityLevel
{
    // using lazy initialization isn't strictly necessary, but it does guarantee 
    // exactly when it occurs, even if new static members are added
    private static readonly Lazy<bool> IsElevatedProvider = new Lazy<bool>(() => GetIsElevated());

    private static bool GetIsElevated()
    {
        try
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }

        return false;
    }

    public static bool IsElevated => IsElevatedProvider.Value;
}
