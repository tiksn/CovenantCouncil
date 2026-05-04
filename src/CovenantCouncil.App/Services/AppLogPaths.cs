namespace CovenantCouncil.App.Services;

public static class AppLogPaths
{
  public static string LogDirectory
  {
    get
    {
      try
      {
        return Path.Combine(FileSystem.AppDataDirectory, "logs");
      }
      catch (Exception)
      {
        return Path.Combine(
          Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
          "CovenantCouncil",
          "logs");
      }
    }
  }
}
