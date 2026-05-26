using ReactiveUI;
using ReactiveUI.Builder;
using Xunit;

namespace CovenantCouncil.FunctionalTests.ViewModels;

[CollectionDefinition(Name)]
public sealed class ReactiveUiTestCollection : ICollectionFixture<ReactiveUiTestFixture>
{
  public const string Name = "ReactiveUI view model tests";
}

public sealed class ReactiveUiTestFixture
{
  public ReactiveUiTestFixture()
  {
    RxAppBuilder.CreateReactiveUIBuilder()
      .WithCoreServices()
      .BuildApp();
  }
}
