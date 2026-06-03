using System;
using UnityEngine;

public static class InputPatch
{
    public static void HandleControls(Game game)
    {
        if (game == null || game.isPaused || game.isDead)
        {
            return;
        }

        // Native Keyboard Controls (Arrows and WASD)
        if (game.CharacterState != null)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                game.CharacterState.HandleSwipe(SwipeDir.Up);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                game.CharacterState.HandleSwipe(SwipeDir.Down);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                game.CharacterState.HandleSwipe(SwipeDir.Left);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                game.CharacterState.HandleSwipe(SwipeDir.Right);
            }
        }

        // Spacebar triggers hoverboard activation
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (game.running != null)
            {
                game.running.HandleDoubleTap();
            }
        }
    }

    public static float GetGameDuration()
    {
        return Game.Instance != null ? Game.Instance.GetDuration() : 0f;
    }

    public static bool IsGamePaused()
    {
        return Game.Instance != null ? Game.Instance.isPaused : false;
    }

    public static void HandleDailyWordFallback(DailyWord dailyWord)
    {
        if (object.ReferenceEquals(dailyWord, null)) return;

        PlayerInfo player = PlayerInfo.Instance;
        if (!object.ReferenceEquals(player, null))
        {
            if (string.IsNullOrEmpty(player.dailyWord) || player.dailyWordExpireTime < DateTime.UtcNow)
            {
                var type = typeof(DailyWord);
                
                var wordDField = type.GetField("wordD", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (!object.ReferenceEquals(wordDField, null)) wordDField.SetValue(dailyWord, "SURF");

                var expireSecondsDField = type.GetField("expireSecondsD", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (!object.ReferenceEquals(expireSecondsDField, null)) expireSecondsDField.SetValue(dailyWord, 86400);

                var gmtTimeSField = type.GetField("GMTTimeS", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (!object.ReferenceEquals(gmtTimeSField, null)) gmtTimeSField.SetValue(dailyWord, DateTime.UtcNow);

                var sendMethod = type.GetMethod("SendWordAndExpireTime", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (!object.ReferenceEquals(sendMethod, null)) sendMethod.Invoke(dailyWord, null);

                Debug.Log("Set fallback daily word: SURF via reflection");
            }
        }
    }
}
