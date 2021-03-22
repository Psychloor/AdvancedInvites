

namespace AdvancedInvites
{

    using System;
    using System.Collections;
    using System.IO;
    using System.Reflection;

    using Il2CppSystem.Collections.Generic;
    
    using MelonLoader;

    using UnityEngine;

    public class SoundPlayer
    {

        private const string AudioPath = "UserData/AdvancedInvites/Notification.ogg";

        private static SoundPlayer instance;

        public static float Volume;

        private GameObject audioGameObject;

        private AudioSource audioSource;

        private AudioClip notificationSound;

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
            MelonLogger.Msg("Loading Notification Sound");

            if (!File.Exists(AudioPath))
            {
                MelonLogger.Msg("Notification Sound Not Found. Creating default one");
                try
                {
                    using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AdvancedInvites.Notification.ogg");
                    using FileStream fs = new FileStream(AudioPath, FileMode.Create);

                    // ReSharper disable once PossibleNullReferenceException
                    stream.CopyTo(fs);
                    fs.Close();
                    stream.Close();
                }
                catch (Exception e)
                {
                    MelonLogger.Error("Something went wrong writing the file to UserData/AdvancedInvites/\n" + e);
                    yield break;
                }
            }

            WWW request = new WWW(Path.GetFullPath(AudioPath), null, new Dictionary<string, string>());
            instance.notificationSound = request.GetAudioClip(false, false, AudioType.OGGVORBIS);
            instance.notificationSound.hideFlags = HideFlags.HideAndDontSave;

            while (!request.isDone || instance.notificationSound.loadState == AudioDataLoadState.Loading) yield return new WaitForEndOfFrame();

            request.Dispose();

            if (instance.notificationSound.loadState == AudioDataLoadState.Loaded)
                MelonLogger.Msg("Notification Sound Loaded");
            else if (instance.notificationSound.loadState == AudioDataLoadState.Failed)
                MelonLogger.Error("Failed To Load Notification Sound");
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