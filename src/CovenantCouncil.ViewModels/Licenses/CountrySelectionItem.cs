using ReactiveUI;

namespace CovenantCouncil.ViewModels.Licenses;

public sealed class CountrySelectionItem(string code, string name) : ReactiveObject
{
  private bool _isSelected;

  public string Code { get; } = code;

  public string Name { get; } = name;

  public string DisplayName => $"{Code} - {Name}";

  public string SelectionStateText => IsSelected ? "Selected" : "";

  public bool IsSelected
  {
    get => _isSelected;
    set
    {
      if (_isSelected != value)
      {
        this.RaiseAndSetIfChanged(ref _isSelected, value);
        this.RaisePropertyChanged(nameof(SelectionStateText));
      }
    }
  }
}
