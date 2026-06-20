using OpenTK.Mathematics;
using SharpOpenGl.Audio;
using SharpOpenGl.Engine.Audio;
using SharpOpenGl.Engine.Events;

namespace SharpOpenGl;

public partial class EngineWindow
{
    private void InitializeAudio()
    {
        try
        {
            _audio = new OpenAlAudioManager();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Audio] Falling back to silent mode: {ex.Message}");
            _audio = new NullAudioManager();
        }

        _eventBus.Subscribe<SoundRequestedEvent>(e =>
            _audio.PlaySound(e.EventType, e.WorldPosition));
    }

    private void UpdateAudioListener()
    {
        Vector3 pos = _rtsCamera.Target + new Vector3(0f, _rtsCamera.Height, 0f);
        Vector3 forward = Vector3.Normalize(_rtsCamera.Target - pos);
        _audio.SetListenerTransform(pos, forward, Vector3.UnitY);
    }

    private void PlayUiClick() => _audio.PlaySound(AudioEventType.UIClick);

    private void PlayBuildingPlaced(Vector3 worldPos) =>
        _eventBus.Publish(new SoundRequestedEvent(AudioEventType.BuildingPlaced, worldPos));

    private void DisposeAudio()
    {
        _audio.Dispose();
        _audio = new NullAudioManager();
    }
}