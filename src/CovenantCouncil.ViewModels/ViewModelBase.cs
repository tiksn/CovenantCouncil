using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Concurrency;
using ReactiveUI.Primitives.Extensions;
using TIKSN.Concurrency;

namespace CovenantCouncil.ViewModels;

public abstract class ViewModelBase(ISequencers sequencers) : ReactiveObject
{
  protected ISequencers Sequencers { get; } = sequencers;

  private int _busyOperationCount;
  private string? _errorMessage;
  private bool _isBusy;

  public string? ErrorMessage
  {
    get => _errorMessage;
    protected set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
  }

  public bool IsBusy
  {
    get => _isBusy;
    protected set => this.RaiseAndSetIfChanged(ref _isBusy, value);
  }

  protected void ObserveCommandErrors<TInput, TOutput>(ReactiveCommand<TInput, TOutput> command)
  {
    System.ObservableExtensions.Subscribe(
      command.ThrownExceptions.ObserveOn(Sequencers.MainThreadSequencer),
      HandleException);
    System.ObservableExtensions.Subscribe(
      command.IsExecuting.ObserveOn(Sequencers.MainThreadSequencer),
      isExecuting =>
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
    _busyOperationCount++;
    IsBusy = _busyOperationCount > 0;
  }

  private void EndBusy()
  {
    _busyOperationCount = Math.Max(0, _busyOperationCount - 1);
    IsBusy = _busyOperationCount > 0;
  }
}
