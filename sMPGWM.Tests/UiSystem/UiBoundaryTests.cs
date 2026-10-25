using System.Reflection;
using System.Text.RegularExpressions;
using sMPGWM.Scripts.UiSystem;
using sMPGWM.Scripts.UiSystem.OverlayMenuSystem;

namespace sMPGWM.Tests.UiSystem;

[TestFixture]
public class UiBoundaryTests
{
    [Test]
    public void Canvas_ExposesOnlyFourMenuOperationsBeyondGodotOverrides()
    {
        var methods = GetPublicOperations(typeof(UiCanvas));

        Assert.That(methods, Is.EquivalentTo(new[]
        {
            nameof(UiCanvas.OpenMenu),
            nameof(UiCanvas.CloseMenu),
            nameof(UiCanvas.ToggleMenu),
            nameof(UiCanvas.CloseAllMenus)
        }));
    }

    [Test]
    public void WindowManagerAndStateHelpers_AreHiddenBehindCanvas()
    {
        Assert.That(GetPublicOperations(typeof(UiWindowManager)), Is.Empty);
        Assert.That(typeof(UiWindowManager).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Empty);
        Assert.That(typeof(UiWindowManager).GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), Is.Empty);
        Assert.That(typeof(UiWindowRegistry).IsNotPublic, Is.True);
        Assert.That(typeof(GameOverlayProvider).IsNotPublic, Is.True);
    }

    [Test]
    public void EveryWindowId_HasAnIdenticallyNamedInputMapAction()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "../../../../sMPGWM.Game/project.godot"));
        var projectText = File.ReadAllText(projectPath);
        var inputSection = Regex.Match(projectText, @"(?ms)^\[input\]\s*(.*?)(?=^\[|\z)").Groups[1].Value;
        var actionNames = Regex.Matches(inputSection, @"(?m)^(\w+)=\{")
            .Select(match => match.Groups[1].Value);

        Assert.That(actionNames, Is.SupersetOf(Enum.GetNames<UiWindowId>()));
    }

    private static IEnumerable<string> GetPublicOperations(Type type) =>
        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsVirtual && !method.IsSpecialName)
            .Select(method => method.Name);
}
