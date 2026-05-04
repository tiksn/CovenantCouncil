using CovenantCouncil.UseCases.Certificates;
using CovenantCouncil.ViewModels.Certificates;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CovenantCouncil.FunctionalTests.ViewModels;

[Collection(ReactiveUiTestCollection.Name)]
public sealed class CertificatesViewModelTests
{
  [Fact]
  public async Task Load_PopulatesRoots()
  {
    var certificateService = Substitute.For<ICertificateService>();
    var root = CreateNode("root", [CreateNode("child", [])]);
    certificateService.GetTreeAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<IReadOnlyList<CertificateTreeNode>>([root]));
    var viewModel = new CertificatesViewModel(certificateService);
    var collectionChanges = ViewModelTestHelpers.ObserveCollection(viewModel.Roots);

    await ViewModelTestHelpers.ExecuteIgnoringCommandExceptionAsync(viewModel.Load);

    viewModel.Roots.ShouldBe([root]);
    viewModel.Roots[0].Children.Count.ShouldBe(1);
    collectionChanges.ShouldContain(System.Collections.Specialized.NotifyCollectionChangedAction.Add);
  }

  [Fact]
  public async Task ImportChain_CallsServiceAndReloads()
  {
    var certificateService = Substitute.For<ICertificateService>();
    var root = CreateNode("imported", []);
    certificateService.GetTreeAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<IReadOnlyList<CertificateTreeNode>>([root]));
    var viewModel = new CertificatesViewModel(certificateService);
    string[] paths = ["leaf.cer", "root.cer"];

    await ViewModelTestHelpers.ExecuteAsync(viewModel.ImportChain, paths);

    await certificateService.Received(1).ImportPublicChainAsync(paths, Arg.Any<CancellationToken>());
    await certificateService.Received(1).GetTreeAsync(Arg.Any<CancellationToken>());
    viewModel.Roots.ShouldBe([root]);
  }

  [Fact]
  public async Task Delete_CallsServiceAndReloads()
  {
    var certificateService = Substitute.For<ICertificateService>();
    certificateService.GetTreeAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromResult<IReadOnlyList<CertificateTreeNode>>([]));
    var viewModel = new CertificatesViewModel(certificateService);
    var id = Guid.NewGuid();

    await ViewModelTestHelpers.ExecuteAsync(viewModel.Delete, id);

    await certificateService.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    await certificateService.Received(1).GetTreeAsync(Arg.Any<CancellationToken>());
    viewModel.Roots.ShouldBeEmpty();
  }

  [Fact]
  public async Task CommandFailure_SetsErrorMessageAndStopsBusy()
  {
    var certificateService = Substitute.For<ICertificateService>();
    certificateService.GetTreeAsync(Arg.Any<CancellationToken>())
      .Returns(_ => Task.FromException<IReadOnlyList<CertificateTreeNode>>(new InvalidOperationException("tree failed")));
    var viewModel = new CertificatesViewModel(certificateService);

    await ViewModelTestHelpers.ExecuteIgnoringCommandExceptionAsync(viewModel.Load);

    viewModel.ErrorMessage.ShouldBe("tree failed");
    viewModel.IsBusy.ShouldBeFalse();
  }

  private static CertificateTreeNode CreateNode(string thumbprint, IReadOnlyList<CertificateTreeNode> children)
  {
    return new CertificateTreeNode(
      new CertificateSummary(
        Guid.NewGuid(),
        thumbprint,
        $"CN={thumbprint}",
        "CN=issuer",
        "serial",
        DateTimeOffset.UtcNow.AddDays(-1),
        DateTimeOffset.UtcNow.AddDays(1),
        null),
      children);
  }
}
