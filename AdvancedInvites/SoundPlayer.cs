namespace AdvancedInvites
{

    using System;
    using System.Collections;
    using System.IO;
    using System.Reflection;

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
            if (instance != null
                && instance.audioSource != null
                && instance.notificationSound != null
                && instance.notificationSound.loadState == AudioDataLoadState.Loaded)
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
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AdvancedInvites.Notification.ogg");
                try
                {
                    using (var fs = new FileStream(AudioPath, FileMode.Create))
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        stream.CopyTo(fs);
                        fs.Close();
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.LogError("Something went wrong writing the file to UserData/AdvancedInvites/\n" + e);
                    yield break;
                }
                stream.Close();
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