using UnityEngine;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        private void OpenDialogueInteraction(V24Action action)
        {
            if (action.Action == "open_map")
            {
                _floor = Mathf.Clamp(action.InitialFloor, 1, 2);
                _selectedRoom = 0;
                Map();
            }
            else if (action.Action == "open_entry_detail")
            {
                Journal(1, action.Entry, true);
                Toast(action.Hint ?? "使用邀请函");
            }
        }
    }
}
