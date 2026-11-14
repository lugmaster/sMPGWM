using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Tests.FileLoader;

public sealed class AtomicTextFileTests
{
    private string _directory = null!;
    private string _path = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "sMPGWM-atomic-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "definitions.yml");
    }

    [TearDown]
    public void TearDown()
    {
        var resolved = Path.GetFullPath(_directory);
        var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Test cleanup target is outside the temporary directory.");
        Directory.Delete(resolved, true);
    }

    [Test]
    public void OpenReaderRetainsOldContentsWhileNewReadersSeeReplacement()
    {
        AtomicTextFile.WriteAllText(_path, "old contents");
        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        using var reader = new StreamReader(stream);
        AtomicTextFile.WriteAllText(_path, "new contents");
        Assert.That(reader.ReadToEnd(), Is.EqualTo("old contents"));
        Assert.That(AtomicTextFile.ReadAllText(_path), Is.EqualTo("new contents"));
        Assert.That(Directory.GetFiles(_directory, "*.tmp"), Is.Empty);
    }

    [Test]
    [Platform("Win")]
    public void FailedReplacementPreservesDestinationAndRemovesTemporaryFile()
    {
        AtomicTextFile.WriteAllText(_path, "original");
        using (var locked = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.That(() => AtomicTextFile.WriteAllText(_path, "replacement"),
                Throws.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
        Assert.That(AtomicTextFile.ReadAllText(_path), Is.EqualTo("original"));
        Assert.That(Directory.GetFiles(_directory, "*.tmp"), Is.Empty);
    }

    [Test]
    public void ConcurrentWritersAndReadersNeverObservePartialContents()
    {
        var versions = new[] { new string('a', 32768), new string('b', 65536), new string('c', 98304) };
        AtomicTextFile.WriteAllText(_path, versions[0]);
        using var ready = new Barrier(versions.Length);
        var workers = new Task[versions.Length];
        for (var index = 0; index < workers.Length; index++)
        {
            var content = versions[index];
            workers[index] = Task.Factory.StartNew(() =>
            {
                Assert.That(ready.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
                for (var iteration = 0; iteration < 100; iteration++)
                {
                    AtomicTextFile.WriteAllText(_path, content);
                    Assert.That(AtomicTextFile.ReadAllText(_path), Is.AnyOf(versions));
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        Task.WaitAll(workers);
        Assert.That(Directory.GetFiles(_directory, "*.tmp"), Is.Empty);
    }
}
