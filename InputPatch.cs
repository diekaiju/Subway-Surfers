using System;
using System.Collections;
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

    private static int _saveMeCount = 0;

    public static void ResetSaveMeCount()
    {
        _saveMeCount = 0;
        Debug.Log("Save Me count reset to 0 for a new run.");
    }

    public static int GetSaveMeCost()
    {
        return 300 * (_saveMeCount + 1);
    }

    public static void IncrementSaveMeCount()
    {
        _saveMeCount++;
        Debug.Log("Save Me count incremented to: " + _saveMeCount);
    }

    public static IEnumerator DieSequence(Game game)
    {
        Debug.Log("InputPatch: Intercepted DieSequence.");

        // 1. Wait for 2 seconds (or touch/click)
        float wait = Time.time + 2f;
        while (Time.time < wait)
        {
            if (Time.time > wait - 1.5f)
            {
                if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
                {
                    break;
                }
            }
            yield return null;
        }

        // 2. Offer "Save Me" option
        int cost = GetSaveMeCost();
        PlayerInfo player = PlayerInfo.Instance;
        
        if (player != null)
        {
            bool chosen = false;
            bool saveMe = false;

            // Spawn the SaveMe overlay
            GameObject overlayObj = new GameObject("SaveMeOverlay");
            SaveMeOverlay overlay = overlayObj.AddComponent<SaveMeOverlay>();
            overlay.Init(cost, player.amountOfCoins, (result) => {
                saveMe = result;
                chosen = true;
            });

            // Pause game timescale during choice
            float prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // Wait for user choice (TimeScale is 0, but IEnumerator keeps updating)
            while (!chosen)
            {
                yield return null;
            }

            // Restore timescale
            Time.timeScale = prevTimeScale;

            if (saveMe)
            {
                Debug.Log("InputPatch: Player chose Save Me. Reviving...");
                player.amountOfCoins -= cost;
                player.Save();
                IncrementSaveMeCount();

                // Revive the player
                game.isDead = false;

                // Reset enemies (FollowingGuard) using reflection
                var enemiesField = typeof(Game).GetField("enemies", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (!object.ReferenceEquals(enemiesField, null))
                {
                    FollowingGuard enemies = (FollowingGuard)enemiesField.GetValue(game);
                    if (!object.ReferenceEquals(enemies, null))
                    {
                        enemies.enabled = true; // RE-ENABLE ENEMIES!
                        enemies.MuteProximityLoop();
                        enemies.ResetCatchUp();
                        enemies.Restart(false);
                    }
                }

                // Reset character state, physics, and animations
                if (game.character != null)
                {
                    game.character.stumble = false;
                    game.character.verticalSpeed = 0f;
                    game.character.jumping = false;
                    game.character.falling = false;

                    // Trigger the smoke particle system
                    var crashParticlesField = typeof(Character).GetField("hoverboardCrashParticleSystem", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (!object.ReferenceEquals(crashParticlesField, null))
                    {
                        ParticleSystem ps = (ParticleSystem)crashParticlesField.GetValue(game.character);
                        if (ps != null)
                        {
                            ps.Play();
                        }
                    }

                    // Teleport back to safe checkpoint
                    game.character.SetBackToCheckPoint(0.5f);

                    // Reset rotation upright
                    game.character.characterAngle = 0f;
                    var charRotationField = typeof(Character).GetField("characterRotation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (!object.ReferenceEquals(charRotationField, null))
                    {
                        charRotationField.SetValue(game.character, 0f);
                    }
                    if (game.character.characterRoot != null)
                    {
                        game.character.characterRoot.localRotation = Quaternion.identity;
                    }

                    // Reset stopColliding via reflection
                    var stopCollidingField = typeof(Character).GetField("stopColliding", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (!object.ReferenceEquals(stopCollidingField, null))
                    {
                        stopCollidingField.SetValue(game.character, false);
                    }

                    // Reset animation
                    if (game.characterAnimation != null)
                    {
                        game.characterAnimation.Stop();
                    }
                    var setRunAnimMethod = typeof(Character).GetMethod("SetRunAnim", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (!object.ReferenceEquals(setRunAnimMethod, null))
                    {
                        setRunAnimMethod.Invoke(game.character, null);
                    }
                }

                // Change state back to running
                game.ChangeState(game.running);
                yield break;
            }
        }
        else
        {
            Debug.Log("InputPatch: PlayerInfo is null.");
        }

        // 3. Fallback to normal GameOver if not saved
        Debug.Log("InputPatch: Proceeding to GameOver.");
        game.ingameTouchDetection = false;
        UIScreenController.Instance.GameOverTriggered();
        
        // Invoke game.TopMenu() via reflection
        var topMenuMethod = typeof(Game).GetMethod("TopMenu", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (!object.ReferenceEquals(topMenuMethod, null))
        {
            IEnumerator topMenuEnum = (IEnumerator)topMenuMethod.Invoke(game, null);
            game.ChangeState(null, topMenuEnum);
        }
    }
}

public class SaveMeOverlay : MonoBehaviour
{
    private int cost;
    private int playerCoins;
    private Action<bool> callback;
    private float endTime;
    private float duration = 5f;

    public void Init(int cost, int playerCoins, Action<bool> callback)
    {
        this.cost = cost;
        this.playerCoins = playerCoins;
        this.callback = callback;
        this.endTime = Time.realtimeSinceStartup + duration;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (playerCoins >= cost)
            {
                ConfirmSave();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelSave();
        }
    }

    void OnGUI()
    {
        // 1. Dark semi-transparent background (dim game behind dialog)
        Texture2D bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
        bgTex.Apply();
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bgTex);

        // 2. Dialog box frame
        float width = 500f;
        float height = 350f;
        float x = (Screen.width - width) / 2f;
        float y = (Screen.height - height) / 2f;

        // Draw background box (Light/Off-white)
        Texture2D boxTex = new Texture2D(1, 1);
        boxTex.SetPixel(0, 0, new Color(0.96f, 0.96f, 0.94f, 0.98f)); // Clean off-white
        boxTex.Apply();
        GUI.DrawTexture(new Rect(x, y, width, height), boxTex);

        // Draw gold border (mission page style, dark gold)
        Texture2D borderTex = new Texture2D(1, 1);
        borderTex.SetPixel(0, 0, new Color(0.72f, 0.54f, 0.12f, 1f)); // Premium Dark Gold
        borderTex.Apply();
        GUI.DrawTexture(new Rect(x, y, width, 4), borderTex);
        GUI.DrawTexture(new Rect(x, y + height - 4, width, 4), borderTex);
        GUI.DrawTexture(new Rect(x, y, 4, height), borderTex);
        GUI.DrawTexture(new Rect(x + width - 4, y, 4, height), borderTex);

        // Custom styling
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 36;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.12f, 0.12f, 0.12f, 1f); // Charcoal

        GUIStyle timerStyle = new GUIStyle(GUI.skin.label);
        timerStyle.fontSize = 64;
        timerStyle.fontStyle = FontStyle.Bold;
        timerStyle.alignment = TextAnchor.MiddleCenter;
        timerStyle.normal.textColor = new Color(0.92f, 0.28f, 0.12f, 1f); // Vibrant orange-red

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 20;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.normal.textColor = new Color(0.18f, 0.18f, 0.18f, 1f); // Charcoal/Dark grey

        GUIStyle subTextStyle = new GUIStyle(GUI.skin.label);
        subTextStyle.fontSize = 16;
        subTextStyle.alignment = TextAnchor.MiddleCenter;
        subTextStyle.normal.textColor = new Color(0.45f, 0.45f, 0.45f, 1f); // Medium grey

        // Premium button styles with explicit text color
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 22;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;

        GUIStyle disabledButtonStyle = new GUIStyle(buttonStyle);
        disabledButtonStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        // Save original GUI background color to restore it afterwards
        Color origBgColor = GUI.backgroundColor;

        // Draw title
        GUI.Label(new Rect(x, y + 25, width, 50), "SAVE ME", titleStyle);

        // Draw countdown timer
        float timeLeft = Mathf.Max(0f, endTime - Time.realtimeSinceStartup);
        GUI.Label(new Rect(x, y + 80, width, 75), Mathf.CeilToInt(timeLeft).ToString(), timerStyle);

        // Draw coins info
        string coinsText = string.Format("Spend <color=#C85A00><b>{0}</b></color> Coins to continue?", cost);
        GUI.Label(new Rect(x, y + 165, width, 30), coinsText, textStyle);

        string balanceText = string.Format("Your Balance: {0} Coins", playerCoins);
        GUI.Label(new Rect(x, y + 195, width, 25), balanceText, subTextStyle);

        // Check if player can afford it
        bool canAfford = playerCoins >= cost;

        // Draw buttons
        if (canAfford)
        {
            GUI.backgroundColor = new Color(0.18f, 0.67f, 0.33f, 1f); // Premium Emerald Green
            if (GUI.Button(new Rect(x + 50, y + 245, 180, 55), "Save [S]", buttonStyle))
            {
                ConfirmSave();
            }
        }
        else
        {
            GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.8f); // Disabled grey
            GUI.Button(new Rect(x + 50, y + 245, 180, 55), "No Coins", disabledButtonStyle);
        }

        GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1f); // Premium Crimson Red
        if (GUI.Button(new Rect(x + 270, y + 245, 180, 55), "Skip [ESC]", buttonStyle))
        {
            CancelSave();
        }

        // Restore original background color
        GUI.backgroundColor = origBgColor;

        // Auto-cancel if timer runs out
        if (timeLeft <= 0f)
        {
            CancelSave();
        }
    }

    private void ConfirmSave()
    {
        if (playerCoins >= cost)
        {
            if (callback != null) callback(true);
            Destroy(gameObject);
        }
    }

    private void CancelSave()
    {
        if (callback != null) callback(false);
        Destroy(gameObject);
    }
}

