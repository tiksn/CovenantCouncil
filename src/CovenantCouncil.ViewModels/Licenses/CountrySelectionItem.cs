using ReactiveUI;

namespace CovenantCouncil.ViewModels.Licenses;

public sealed class CountrySelectionItem(string code, string name) : ReactiveObject
{
  private bool isSelected;

  public string Code { get; } = code;

  public string Name { get; } = name;

  public string DisplayName => $"{Code} - {Name}";

  public string SelectionStateText => IsSelected ? "Selected" : "";

  public bool IsSelected
  {
    get => isSelected;
    set
    {
      if (isSelected != value)
      {
        this.RaiseAndSetIfChanged(ref isSelected, value);
        this.RaisePropertyChanged(nameof(SelectionStateText));
      }
    }
  }
}
