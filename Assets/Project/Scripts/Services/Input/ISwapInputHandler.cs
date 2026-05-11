using System;
using Project.Scripts.Shared.Grid;

namespace Project.Scripts.Services.Input
{
    public interface ISwapInputHandler
    {
        event Action<SwapRequest> OnSwapRequested;
        void NotifyBoardReady();
    }
}