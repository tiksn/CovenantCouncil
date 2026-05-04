namespace CovenantCouncil.App;

public partial class AppShell : Shell
{
  public AppShell()
  {
    InitializeComponent();
    ApplyDatabaseState(isOpen: false);
  }

  public async Task ShowDatabaseWorkspaceAsync()
  {
    ApplyDatabaseState(isOpen: true);
    await GoToAsync("//licenses");
  }

  private void ApplyDatabaseState(bool isOpen)
  {
    DatabaseContent.IsVisible = !isOpen;
    PartiesContent.IsVisible = isOpen;
    CertificatesContent.IsVisible = isOpen;
    LicensesContent.IsVisible = isOpen;
  }
}
