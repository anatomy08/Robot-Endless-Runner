using UnityEngine;

namespace EndlessRunner
{
    public class RunnerStarCollectible : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private AudioClip collectAudioClip;
        [SerializeField, Range(0f, 1f)] private float collectVolume = 0.8f;

        public void Collect(RunnerGameManager gameManager)
        {
            if (gameManager == null)
            {
                return;
            }

            gameManager.AddStar(value);

            if (collectAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(collectAudioClip, transform.position, collectVolume);
            }

            gameObject.SetActive(false);
        }

        public void Configure(AudioClip audioClip, float audioVolume)
        {
            collectAudioClip = audioClip;
            collectVolume = Mathf.Clamp01(audioVolume);
        }
    }
}
