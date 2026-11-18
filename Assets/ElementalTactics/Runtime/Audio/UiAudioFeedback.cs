using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    [DisallowMultipleComponent]
    public sealed class UiAudioFeedback : MonoBehaviour
    {
        private SoundCue cue;

        public static void Bind(Button button, SoundCue sound = SoundCue.Click)
        {
            if (!Application.isPlaying || button == null) return;
            if (sound == SoundCue.Click && (button.name.StartsWith("Close") || button.name.StartsWith("Back")
                || button.name.StartsWith("Return") || button.name == "Done")) sound = SoundCue.Back;
            var feedback = button.GetComponent<UiAudioFeedback>();
            if (feedback == null) feedback = button.gameObject.AddComponent<UiAudioFeedback>();
            feedback.cue = sound;
            button.onClick.RemoveListener(feedback.Play);
            button.onClick.AddListener(feedback.Play);
        }

        private void Play() { GameAudio.Play(cue); }
    }
}
