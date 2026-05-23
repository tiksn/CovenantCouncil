using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using Microsoft.Reactive.Testing;
using ReactiveUI;
using ReactiveUI.Testing;

namespace CovenantCouncil.FunctionalTests.ViewModels;

internal static class ViewModelTestHelpers
{
  public static async Task WithTestSchedulerAsync(Func<Task> test)
  {
    await new TestScheduler()
      .With(_ => test())
      .WaitAsync(TimeSpan.FromSeconds(5));
  }

  public static Task ExecuteAsync(ReactiveCommand<Unit, Unit> command)
  {
    return command.Execute().ToTask().WaitAsync(TimeSpan.FromSeconds(5));
  }

  public static Task ExecuteAsync<TInput>(ReactiveCommand<TInput, Unit> command, TInput input)
  {
    return command.Execute(input).ToTask().WaitAsync(TimeSpan.FromSeconds(5));
  }

  public static async Task ExecuteIgnoringCommandExceptionAsync(ReactiveCommand<Unit, Unit> command)
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

  public static async Task ExecuteIgnoringCommandExceptionAsync<TInput>(ReactiveCommand<TInput, Unit> command, TInput input)
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
