using Godot; // TEST-SUPPORT: MESSAGE-BINDING-001 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
using sMPGWM.Scripts.GamePlaySystem; // TEST-SUPPORT: MESSAGE-BINDING-002 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
using sMPGWM.Scripts.MessageSystem; // TEST-SUPPORT: MESSAGE-BINDING-003 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
using sMPGWM.Scripts.PlayerControllerSystem; // TEST-SUPPORT: MESSAGE-BINDING-004 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
using sMPGWM.Scripts.UiSystem; // TEST-SUPPORT: MESSAGE-BINDING-005 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync

namespace sMPGWM.Tests.GamePlaySystem; // TEST-SUPPORT: MESSAGE-BINDING-006 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync

[Category("Godot")] // TEST-SUPPORT: MESSAGE-BINDING-007 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
public sealed class MessagePresentationBindingTests // TEST-SUPPORT: MESSAGE-BINDING-008 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
{
    [Test] // TEST-SUPPORT: MESSAGE-BINDING-009 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
    public void ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync() // TEST-SUPPORT: MESSAGE-BINDING-010 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
    {
        var host = ((SceneTree)Engine.GetMainLoop()).Root.GetNode("Tests"); // TEST-SUPPORT: MESSAGE-BINDING-011 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
        var canvas = GD.Load<PackedScene>("res://scenes/game/ui_canvas.tscn").Instantiate<UiCanvas>(); // TEST-SUPPORT: MESSAGE-BINDING-012 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
        host.AddChild(canvas); // TEST-SUPPORT: MESSAGE-BINDING-013 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
        try // TEST-SUPPORT: MESSAGE-BINDING-014 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
        {
            using var messages = new MessageController(); // TEST-SUPPORT: MESSAGE-BINDING-015 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            var input = new PlayerInputPolicy(); // TEST-SUPPORT: MESSAGE-BINDING-016 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            input.SetSessionActive(true); // TEST-SUPPORT: MESSAGE-BINDING-017 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            using var binding = new MessagePresentationBinding(messages, canvas, input); // TEST-SUPPORT: MESSAGE-BINDING-018 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            messages.RegisterSender(new MessageSender("sender", "Sender")); // TEST-SUPPORT: MESSAGE-BINDING-019 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            messages.Receive("first", "sender", "First", "", MessageType.Blocking); // TEST-SUPPORT: MESSAGE-BINDING-020 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            Assert.That(input.CanControlPlayer, Is.False); // TEST-SUPPORT: MESSAGE-BINDING-021 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            Assert.That(canvas.HasBlockingWindow, Is.False); // TEST-SUPPORT: MESSAGE-BINDING-022 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            canvas.SetNavigationEnabled(true); // TEST-SUPPORT: MESSAGE-BINDING-040 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            messages.SetActive(true); // TEST-SUPPORT: MESSAGE-BINDING-023 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            Assert.That(canvas.HasBlockingWindow, Is.True); // TEST-SUPPORT: MESSAGE-BINDING-024 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            messages.MarkRead("first"); // TEST-SUPPORT: MESSAGE-BINDING-025 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            Assert.That(input.CanControlPlayer, Is.False); // TEST-SUPPORT: MESSAGE-BINDING-026 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            messages.AcknowledgeCurrent(); // TEST-SUPPORT: MESSAGE-BINDING-027 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            Assert.That(input.CanControlPlayer, Is.True); // TEST-SUPPORT: MESSAGE-BINDING-028 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            Assert.That(canvas.HasBlockingWindow, Is.False); // TEST-SUPPORT: MESSAGE-BINDING-029 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            messages.Receive("second", "sender", "Second", "", MessageType.Blocking); // TEST-SUPPORT: MESSAGE-BINDING-030 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            Assert.That(canvas.HasBlockingWindow, Is.True); // TEST-SUPPORT: MESSAGE-BINDING-031 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            binding.Dispose(); // TEST-SUPPORT: MESSAGE-BINDING-032 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            Assert.That(input.CanControlPlayer, Is.True); // TEST-SUPPORT: MESSAGE-BINDING-033 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            Assert.That(canvas.GetNode<Control>("Root/MenuLayer/ModalBlocker").Visible, Is.False); // TEST-SUPPORT: MESSAGE-BINDING-034 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            messages.Receive("third", "sender", "Third", "", MessageType.Blocking); // TEST-SUPPORT: MESSAGE-BINDING-035 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            Assert.That(input.CanControlPlayer, Is.True); // TEST-SUPPORT: MESSAGE-BINDING-036 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
            Assert.That(canvas.GetNode<Control>("Root/MenuLayer/ModalBlocker").Visible, Is.False); // TEST-SUPPORT: MESSAGE-BINDING-037 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
        }
        finally // TEST-SUPPORT: MESSAGE-BINDING-038 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
        {
            canvas.Free(); // TEST-SUPPORT: MESSAGE-BINDING-039 | Tests: sMPGWM.Tests.GamePlaySystem.MessagePresentationBindingTests.ActivationAcknowledgementAndDisposalKeepInputAndPresentationInSync
        }
    }
}

