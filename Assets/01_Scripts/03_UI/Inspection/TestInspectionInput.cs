using UnityEngine;
using UnityEngine.InputSystem;

public class TestInspectionInput : MonoBehaviour
{
    [SerializeField] private InspectionManager inspectionManager;
    [SerializeField] private TestInspectable testTarget;

    private PlayerInputs playerInputs;

    private void Start()
    {
        PlayerController player = GetComponent<PlayerController>();
        playerInputs = player.playerInput;
    }

    private void Update()
    {
        if (playerInputs == null) return;

        if (playerInputs.Player.Interaction.WasPressedThisFrame())
        {
            Debug.Log("[TEST] Interact pressed → EnterInspection");

            inspectionManager.EnterInspection(testTarget);
        }
    }
}
