using HellPoker.Core.Cards;
using HellPoker.Core.Draw;
using HellPoker.Core.Game;
using HellPoker.Presentation.Ui;
using HellPoker.Presentation.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace HellPoker.Presentation
{
    /// <summary>
    /// Composition root: reads Inspector settings, builds the game, the table and menu with their presenters, and connects input.
    /// It is the only Unity-side class that knows concrete implementations.
    /// </summary>
    public sealed class HellPokerBootstrap : MonoBehaviour
    {
        [Header("Sentence")]
        [SerializeField] private int _startingYears = 1000;
        [SerializeField] private int _damnationYears = 2000;

        [Tooltip("At or below this many years left, passing is forbidden and every decision must raise or fold.")]
        [SerializeField] private int _forcedRaiseYears = 250;

        [Header("Stakes")]
        [Tooltip("Ante choices. Every raise adds the ante again.")]
        [SerializeField] private int[] _stakeOptions = { 10, 25, 50, 100, 200 };

        [Header("Cards")]
        [SerializeField, Range(0, Hand.Size)] private int _maxDiscards = MaxDiscardPolicy.ClassicLimit;
        [Tooltip("How many house cards are followed by a bet decision; the rest flip straight into the showdown.")]
        [SerializeField, Range(0, Hand.Size - 1)] private int _houseRevealDecisions = 3;

        [Header("Randomness")]
        [Tooltip("Use a fixed seed for reproducible shuffles while debugging.")]
        [SerializeField] private bool _useFixedSeed;
        [SerializeField] private int _seed = 666;

        private TablePresenter _tablePresenter;
        private MainMenuPresenter _menuPresenter;

        private void Awake()
        {
            EnsureEventSystem();

            var rules = new GameRules(_startingYears, _damnationYears, Mathf.Min(_stakeOptions), Mathf.Max(_stakeOptions), _maxDiscards,
                _forcedRaiseYears, _houseRevealDecisions);
            PayoutTable payouts = PayoutTable.CreateDefault();
            HellPokerGame game = HellPokerGameFactory.Create(rules, payouts, _useFixedSeed ? _seed : (int?)null);

            TableView table = TableView.Create(transform, payouts, _stakeOptions);
            _tablePresenter = new TablePresenter(game, table, _stakeOptions);

            MainMenuView menu = MainMenuView.Create(transform,
                string.Format(UiText.MenuTaglineFormat, rules.StartingYears),
                string.Format(UiText.RulesFormat, rules.StartingYears, rules.DamnationYears, rules.MaxDiscards, rules.ForcedRaiseYears));
            _menuPresenter = new MainMenuPresenter(menu, table, _tablePresenter, new UnityApplicationQuitter());

            gameObject.AddComponent<KeyboardInput>().Bind(_tablePresenter, _menuPresenter);
        }

        private void OnDestroy()
        {
            _menuPresenter?.Dispose();
            _tablePresenter?.Dispose();
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystem);
        }
    }
}
