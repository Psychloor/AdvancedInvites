namespace AdvancedInvites
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    using MelonLoader;

    using UnityEngine;
    using UnityEngine.Networking;

    using Object = UnityEngine.Object;

    public class SoundPlayer
    {

        public enum NotificationType
        {

            Default,

            Invite,

            InviteRequest,

            FriendRequest,

            VoteToKick

        }

        private const string AudioResourceFolder = "UserData/AdvancedInvites/";

        private static SoundPlayer instance;

        public static float Volume;

        private readonly Dictionary<NotificationType, AudioClip> audioClipDictionary;

        private GameObject audioGameObject;

        private AudioSource audioSource;

        private SoundPlayer()
        {
            audioClipDictionary = new Dictionary<NotificationType, AudioClip>();
        }

        private static string GetAudioPath(NotificationType notificationType)
        {
            return Path.GetFullPath(Path.Combine(AudioResourceFolder, $"{notificationType}.ogg"));
        }

        public static void PlayNotificationSound(NotificationType notificationType)
        {
            if (instance == null
                || instance.audioSource == null) return;
            if (instance.audioSource.isPlaying) return;

            instance.audioSource.outputAudioMixerGroup = null;

            if (notificationType != NotificationType.Default
                && instance.audioClipDictionary.ContainsKey(notificationType)
                && instance.audioClipDictionary[notificationType].loadState == AudioDataLoadState.Loaded)
                instance.audioSource.PlayOneShot(instance.audioClipDictionary[notificationType], Volume);
            else if (instance.audioClipDictionary.ContainsKey(NotificationType.Default)
                     && instance.audioClipDictionary[NotificationType.Default].loadState == AudioDataLoadState.Loaded)
                instance.audioSource.PlayOneShot(instance.audioClipDictionary[NotificationType.Default], Volume);
        }

        internal static IEnumerator LoadNotificationSounds()
        {
            // in case we're reloading
            if (instance.audioClipDictionary.Count > 0)
            {
                foreach (NotificationType notificationType in instance.audioClipDictionary.Keys)
                    Object.DestroyImmediate(instance.audioClipDictionary[notificationType]);

                // Give it a little time to update
                yield return new WaitForSeconds(.2f);
                instance.audioClipDictionary.Clear();
            }

            MelonLogger.Msg("Loading Notification Sound(s)");

            // Legacy Convert
            if (File.Exists(Path.GetFullPath(Path.Combine(AudioResourceFolder, "Notification.ogg"))))
            {
                MelonLogger.Msg("Found old notification file. renaming to Default.ogg");
                File.Move(Path.GetFullPath(Path.Combine(AudioResourceFolder, "Notification.ogg")), GetAudioPath(NotificationType.Default));
            }

            if (!File.Exists(GetAudioPath(NotificationType.Default)))
            {
                MelonLogger.Msg("Default Notification sound not found. Creating default one");
                try
                {
                    using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AdvancedInvites.Notification.ogg");
                    if (stream != null)
                    {
                        using FileStream fs = new FileStream(GetAudioPath(NotificationType.Default), FileMode.Create);
                        stream.CopyTo(fs);
                        fs.Close();
                        stream.Close();
                    }
                    else
                    {
                        MelonLogger.Error("Failed to open Resource Stream for Notification.ogg");
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Error("Something went wrong writing the default notification file to UserData/AdvancedInvites Folder:\n" + e);
                    yield break;
                }
            }

            // look through all types and see if the soundfile exists
            foreach (string name in Enum.GetNames(typeof(NotificationType)).Where(
                name => File.Exists(GetAudioPath((NotificationType)Enum.Parse(typeof(NotificationType), name)))))
                yield return LoadAudioClip((NotificationType)Enum.Parse(typeof(NotificationType), name));
        }

        // Thanks Loukylor and Knah
        private static IEnumerator LoadAudioClip(NotificationType notificationType)
        {
            UnityWebRequest request = UnityWebRequest.Get(GetAudioPath(notificationType));
            request.SendWebRequest();
            while (!request.isDone)
                yield return null;

            AudioClip audioClip = WebRequestWWW.InternalCreateAudioClipUsingDH(request.downloadHandler, request.url, false, false, AudioType.UNKNOWN);

            request.Dispose();

            if (audioClip.loadState == AudioDataLoadState.Loaded)
            {
                instance.audioClipDictionary.Add(notificationType, audioClip);
                instance.audioClipDictionary[notificationType].hideFlags = HideFlags.HideAndDontSave;
                MelonLogger.Msg($"{notificationType} Notification Sound Loaded");
            }
            else if (audioClip.loadState == AudioDataLoadState.Failed)
            {
                MelonLogger.Error($"Failed To Load {notificationType} Notification Sound");
            }
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

            MelonCoroutines.Start(LoadNotificationSounds());
        }

    }

}