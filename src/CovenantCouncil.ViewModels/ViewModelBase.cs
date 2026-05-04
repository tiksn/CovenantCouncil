using System.Reactive.Linq;
using ReactiveUI;

namespace CovenantCouncil.ViewModels;

public abstract class ViewModelBase : ReactiveObject
{
  private int busyOperationCount;
  private string? errorMessage;
  private bool isBusy;

  public string? ErrorMessage
  {
    get => errorMessage;
    protected set => this.RaiseAndSetIfChanged(ref errorMessage, value);
  }

  public bool IsBusy
  {
    get => isBusy;
    protected set => this.RaiseAndSetIfChanged(ref isBusy, value);
  }

  protected void ObserveCommandErrors<TInput, TOutput>(ReactiveCommand<TInput, TOutput> command)
  {
    _ = command.ThrownExceptions
      .ObserveOn(RxSchedulers.MainThreadScheduler)
      .Subscribe(HandleException);
    _ = command.IsExecuting
      .ObserveOn(RxSchedulers.MainThreadScheduler)
      .Subscribe(isExecuting =>
    {
      if (isExecuting)
      {
        BeginBusy();
      }
      else
      {
        EndBusy();
      }
    });
  }

  public async Task RunWithBusyAsync(Func<Task> operation)
  {
    BeginBusy();
    try
    {
      await operation();
    }
    finally
    {
      EndBusy();
    }
  }

  protected void HandleException(Exception exception)
  {
    ErrorMessage = null;
    ErrorMessage = exception.Message;
  }

  private void BeginBusy()
  {
    busyOperationCount++;
    IsBusy = busyOperationCount > 0;
  }

  private void EndBusy()
  {
    busyOperationCount = Math.Max(0, busyOperationCount - 1);
    IsBusy = busyOperationCount > 0;
  }
}
