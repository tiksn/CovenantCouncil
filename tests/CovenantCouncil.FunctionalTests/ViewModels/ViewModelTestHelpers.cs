using System.Collections.Specialized;
using System.Reactive;
using Microsoft.Reactive.Testing;
using NSubstitute;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Testing;
using TIKSN.Concurrency;

namespace CovenantCouncil.FunctionalTests.ViewModels;

internal static class ViewModelTestHelpers
{
  public static ISequencers CreateMockSequencers()
  {
    var sequencers = Substitute.For<ISequencers>();
    sequencers.MainThreadSequencer.Returns(ReactiveUI.Primitives.Concurrency.Sequencer.Immediate);
    sequencers.TaskPoolSequencer.Returns(ReactiveUI.Primitives.Concurrency.Sequencer.Immediate);
    return sequencers;
  }


  public static Task ExecuteAsync(ReactiveCommand<RxVoid, RxVoid> command)
  {
    return command.Execute().ToTask().WaitAsync(TimeSpan.FromSeconds(5));
  }

  public static Task ExecuteAsync<TInput>(ReactiveCommand<TInput, RxVoid> command, TInput input)
  {
    return command.Execute(input).ToTask().WaitAsync(TimeSpan.FromSeconds(5));
  }

  public static async Task ExecuteIgnoringCommandExceptionAsync(ReactiveCommand<RxVoid, RxVoid> command)
  {
    try
    {
      await ExecuteAsync(command);
    }
    catch (Exception)
    {
      await Task.Delay(10);
    }
  }

  public static async Task ExecuteIgnoringCommandExceptionAsync<TInput>(ReactiveCommand<TInput, RxVoid> command, TInput input)
  {
    try
    {
      await ExecuteAsync(command, input);
    }
    catch (Exception)
    {
      await Task.Delay(10);
    }
  }

  public static List<string> ObserveProperties(ReactiveObject source)
  {
    var changes = new List<string>();
    source.PropertyChanged += (_, args) =>
    {
      if (args.PropertyName is not null)
      {
        changes.Add(args.PropertyName);
      }
    };
    return changes;
  }

  public static List<NotifyCollectionChangedAction> ObserveCollection(INotifyCollectionChanged collection)
  {
    var changes = new List<NotifyCollectionChangedAction>();
    collection.CollectionChanged += (_, args) => changes.Add(args.Action);
    return changes;
  }
}
