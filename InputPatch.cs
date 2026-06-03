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
}
