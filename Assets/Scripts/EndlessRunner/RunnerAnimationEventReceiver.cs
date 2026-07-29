using UnityEngine;

namespace EndlessRunner
{
    public class RunnerAnimationEventReceiver : MonoBehaviour
    {
        [SerializeField] private AudioClip landingAudioClip;
        [SerializeField] private AudioClip[] footstepAudioClips;
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.65f;
        [SerializeField, Range(0f, 1f)] private float landingVolume = 0.9f;

        private float nextLandingTime;
        private const float LandingCooldown = 0.1f;

        public void OnFootstep(AnimationEvent animationEvent)
        {
            if (!ShouldPlay(animationEvent) || footstepAudioClips == null || footstepAudioClips.Length == 0)
            {
                return;
            }

            int index = Random.Range(0, footstepAudioClips.Length);
            PlayClip(footstepAudioClips[index], footstepVolume);
        }

        public void OnLand(AnimationEvent animationEvent)
        {
            if (ShouldPlay(animationEvent))
            {
                PlayLanding();
            }
        }

        public void PlayLanding()
        {
            if (landingAudioClip == null || Time.time < nextLandingTime)
            {
                return;
            }

            nextLandingTime = Time.time + LandingCooldown;
            PlayClip(landingAudioClip, landingVolume);
        }

        public void Configure(AudioClip landingClip, AudioClip[] footstepClips, float audioFootstepVolume, float audioLandingVolume)
        {
            landingAudioClip = landingClip;
            footstepAudioClips = footstepClips;
            footstepVolume = Mathf.Clamp01(audioFootstepVolume);
            landingVolume = Mathf.Clamp01(audioLandingVolume);
        }

        private static bool ShouldPlay(AnimationEvent animationEvent)
        {
            return animationEvent == null || animationEvent.animatorClipInfo.weight > 0.5f;
        }

        private void PlayClip(AudioClip clip, float audioVolume)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, audioVolume);
        }
    }
}
