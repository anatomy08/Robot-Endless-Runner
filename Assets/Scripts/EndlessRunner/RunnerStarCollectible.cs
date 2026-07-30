using UnityEngine;

namespace EndlessRunner
{
    public class RunnerStarCollectible : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private AudioClip collectAudioClip;
        [SerializeField, Range(0f, 1f)] private float collectVolume = 0.8f;
        private AudioSource collectAudioSource;

        public void Collect(RunnerGameManager gameManager)
        {
            if (gameManager == null)
            {
                return;
            }

            gameManager.AddStar(value);

            if (collectAudioClip != null)
            {
                if (collectAudioSource != null)
                {
                    collectAudioSource.PlayOneShot(collectAudioClip, collectVolume);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(collectAudioClip, transform.position, collectVolume);
                }
            }

            gameObject.SetActive(false);
        }

        public void Configure(AudioClip audioClip, float audioVolume, AudioSource audioSource = null)
        {
            collectAudioClip = audioClip;
            collectVolume = Mathf.Clamp01(audioVolume);
            collectAudioSource = audioSource;
        }
    }
}
