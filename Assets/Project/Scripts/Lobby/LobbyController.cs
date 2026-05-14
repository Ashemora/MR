using Cysharp.Threading.Tasks;
using Project.Scripts.Services.AppFlow;
using UnityEngine;
using VContainer;

namespace Project.Scripts.Lobby
{
    public class LobbyController : MonoBehaviour
    {
        [SerializeField] private LobbyView _view;


        private IAppStateMachine _appStateMachine;


        [Inject]
        public void Construct(IAppStateMachine appStateMachine)
        {
            _appStateMachine = appStateMachine;
        }
        

        private void Start()
        {
            _view.Bind(StartBattle);
        }
        

        private void StartBattle()
        {
            _appStateMachine.StartBattleAsync().Forget();
        }
    }
}