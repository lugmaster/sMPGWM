using Godot;
using sMPGWM.Scripts.GamePlaySystem;
using sMPGWM.Scripts.MessageSystem;
using sMPGWM.Scripts.PlayerControllerSystem;
using sMPGWM.Scripts.UiSystem;

namespace sMPGWM.Tests.GamePlaySystem;

[Category("Godot")]
public sealed class MessagePresentationBindingTests
{
    [Test]
    public void ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync()
    {
        var host = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests");
        var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate<UiCanvas>();
        host.AddChild(canvas);
        try
        {
            using var messages = new MessageController();
            var input = new PlayerInputPolicy();
            input.SetSessionActive(true);
            using var binding = new MessagePresentationBinding(messages, canvas, input);
            messages.RegisterSender(new MessageSender("sender", "Sender"));
            messages.Receive("first", "sender", "First", "", MessageType.Blocking);
            Assert.That(input.CanControlPlayer, Is.False);
            Assert.That(canvas.HasBlockingWindow, Is.False);
            canvas.SetNavigationEnabled(true);
            messages.SetActive(true);
            Assert.That(canvas.HasBlockingWindow, Is.True);
            messages.MarkRead("first");
            Assert.That(input.CanControlPlayer, Is.False);
            messages.AcknowledgeCurrent();
            Assert.That(input.CanControlPlayer, Is.True);
            Assert.That(canvas.HasBlockingWindow, Is.False);
            messages.Receive("second", "sender", "Second", "", MessageType.Blocking);
            Assert.That(canvas.HasBlockingWindow, Is.True);
            binding.Dispose();
            Assert.That(input.CanControlPlayer, Is.True);
            Assert.That(canvas.GetNode<Control>("Root/MenuLayer/ModalBlocker").Visible, Is.False);
            messages.Receive("third", "sender", "Third", "", MessageType.Blocking);
            Assert.That(input.CanControlPlayer, Is.True);
            Assert.That(canvas.GetNode<Control>("Root/MenuLayer/ModalBlocker").Visible, Is.False);
        }
        finally
        {
            canvas.Free();
        }
    }
}

