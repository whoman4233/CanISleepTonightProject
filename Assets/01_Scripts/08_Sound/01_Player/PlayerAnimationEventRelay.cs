using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private PlayerSfxController JumpLandingsfx;

    public void AE_PlayJumpLanding()
    {
        JumpLandingsfx?.PlayJumpLandingSfx();
    }
}
