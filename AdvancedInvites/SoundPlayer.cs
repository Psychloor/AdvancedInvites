namespace AdvancedInvites
{

    using System;
    using System.Collections;
    using System.IO;

    using MelonLoader;

    using UnityEngine;

    public class SoundPlayer
    {

        private static SoundPlayer instance;

        private GameObject audioGameObject;

        private AudioSource audioSource;

        private AudioClip notificationSound;

        private const string AudioPath = "UserData/AdvancedInvites/Notification.ogg";

        public static float Volume;
        
        private SoundPlayer()
        { }

        public static void PlayNotificationSound()
        {
            if (instance != null && instance.audioSource != null && instance.notificationSound != null && instance.notificationSound.loadState == AudioDataLoadState.Loaded)
            {
                instance.audioSource.outputAudioMixerGroup = null;
                instance.audioSource.PlayOneShot(instance.notificationSound, Volume);
            }
        }

        private static IEnumerator LoadNotificationSound()
        {
            MelonLogger.Log("Loading Notification Sound");
            
            if (!File.Exists(AudioPath))
            {
                MelonLogger.Log("Notification Sound Not Found. Creating default one");
                File.WriteAllBytes(AudioPath, Convert.FromBase64String(NotificationSound.NotificationBase64));
            }
            
            WWW request = new WWW(Path.GetFullPath(AudioPath));
            instance.notificationSound = request.GetAudioClip();
            instance.notificationSound.hideFlags = HideFlags.HideAndDontSave;
            
            while (!request.isDone || instance.notificationSound.loadState == AudioDataLoadState.Loading)
            {
                yield return new WaitForEndOfFrame();
            }
            request.Dispose();
            
            if (instance.notificationSound.loadState == AudioDataLoadState.Loaded)
                MelonLogger.Log("Notification Sound Loaded");
            else if (instance.notificationSound.loadState == AudioDataLoadState.Failed)
                MelonLogger.LogError("Failed To Load Notification Sound");
        }

        public static void Initialize()
        {
            if (instance != null) return;
            
            instance = new SoundPlayer();
            instance.audioGameObject = new GameObject { hideFlags = HideFlags.HideAndDontSave };
            instance.audioSource = instance.audioGameObject.AddComponent<AudioSource>();
            instance.audioSource.hideFlags = HideFlags.HideAndDontSave;
            instance.audioSource.dopplerLevel = 0f;
            instance.audioSource.spatialBlend = 0f;
            instance.audioSource.spatialize = false;
            
            MelonCoroutines.Start(LoadNotificationSound());
        }

    }

}