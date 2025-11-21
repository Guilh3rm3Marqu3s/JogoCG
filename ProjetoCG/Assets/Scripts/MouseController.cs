using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem; // Necessário para o novo sistema de input
#endif

public class MouseController : MonoBehaviour
{
private bool _isCursorVisible = false;

    void Start()
    {
        // Ao iniciar, esconde e trava o mouse
        LockCursor();
    }

    void Update()
    {
        // Verifica se apertou ESC (funciona tanto no input antigo quanto no novo)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleCursorState();
        }
        // Fallback para input antigo, caso necessário
        else if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            ToggleCursorState();
        }
    }

    private void ToggleCursorState()
    {
        _isCursorVisible = !_isCursorVisible;

        if (_isCursorVisible)
            UnlockCursor();
        else
            LockCursor();
    }

    private void LockCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked; // Trava no centro
        _isCursorVisible = false;
    }

    private void UnlockCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None; // Solta o mouse
        _isCursorVisible = true;
    }
}
