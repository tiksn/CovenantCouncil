using System.Reactive;
using CovenantCouncil.ViewModels;
using ReactiveUI;
using Shouldly;
using Xunit;

namespace CovenantCouncil.FunctionalTests.ViewModels;

#pragma warning disable VSTHRD003

[Collection(ReactiveUiTestCollection.Name)]
public sealed class ViewModelBaseTests
{
  [Fact]
  public async Task RunWithBusyAsync_TogglesBusyAroundOperation()
  {
    var viewModel = new TestViewModel();
    var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    var operation = viewModel.RunWithBusyAsync(() => completion.Task);

    viewModel.IsBusy.ShouldBeTrue();
    completion.SetResult();
    await operation;
    viewModel.IsBusy.ShouldBeFalse();
  }

  [Fact]
  public async Task RunWithBusyAsync_KeepsBusyTrueUntilNestedOperationsFinish()
  {
    var viewModel = new TestViewModel();
    var innerCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var innerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    var operation = viewModel.RunWithBusyAsync(async () =>
    {
      var innerOperation = viewModel.RunWithBusyAsync(() => innerCompletion.Task);
      innerStarted.SetResult();
      await innerOperation;
    });

    await innerStarted.Task;
    viewModel.IsBusy.ShouldBeTrue();
    innerCompletion.SetResult();
    await operation;
    viewModel.IsBusy.ShouldBeFalse();
  }

  [Fact]
  public async Task ObservedCommandFailure_SetsErrorMessage()
  {
    var viewModel = new TestViewModel();

    await ViewModelTestHelpers.ExecuteIgnoringCommandExceptionAsync(viewModel.Fail);

    viewModel.ErrorMessage.ShouldBe("command failed");
    viewModel.IsBusy.ShouldBeFalse();
  }

  private sealed class TestViewModel : ViewModelBase
  {
    public TestViewModel()
    {
      Fail = ReactiveCommand.CreateFromTask(
        () => Task.FromException(new InvalidOperationException("command failed")),
        outputScheduler: RxSchedulers.MainThreadScheduler);
      ObserveCommandErrors(Fail);
    }

    public ReactiveCommand<Unit, Unit> Fail { get; }
  }
}

#pragma warning restore VSTHRD003
