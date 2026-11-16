using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics
{
    public class BattleController : MonoBehaviour
    {
        public List<CharacterDefinition> playerRoster;
        public List<CharacterDefinition> enemyRoster;
        public CardView[] playerCards;
        public CardView[] enemyCards;
        public Button startButton;
        public Button backButton;
        public Button resetButton;
        public Button speedButton;
        public Button enemyButton;
        public Text phaseLabel;
        public Text actionLabel;
        public Text detailLabel;
        public Text roundLabel;
        public Text formationHint;
        public FormationController formation;
        public List<CharacterDefinition> playerLineup;
        public GameObject combatRoot;
        public GameObject resultPanel;
        public Text resultTitle;
        public Text resultDetails;
        public Button retryButton;
        public Button mapButton;
        public float animationSpeed = 1f;
        public int Round { get; private set; }
        public bool IsBattling { get; private set; }
        public bool Finished { get; private set; }
        public string Outcome { get; private set; }
        private bool paused;
        public bool IsPaused { get => paused || TutorialGuide.BlocksCombat || (fighterDetails != null && fighterDetails.IsOpen); private set => paused = value; }
        public BattleFighterDetails FighterDetails => fighterDetails;
        public IReadOnlyList<string> LogEntries => battleLog;
        private int selectedSlot = -1;
        private readonly List<Fighter> players = new List<Fighter>();
        private readonly List<Fighter> enemies = new List<Fighter>();
        private readonly Dictionary<Fighter, CardView> views = new Dictionary<Fighter, CardView>();
        private readonly HashSet<Fighter> actedThisRound = new HashSet<Fighter>();
        private GameObject roundNotice;
        private Text roundNoticeText;
        private BattleHud hud;
        private BattlePresentation presentation;
        private BattleFighterDetails fighterDetails;
        public string PresentationStage => presentation == null ? "Idle" : presentation.Stage;
        public float PresentationProgress => presentation == null ? 0 : presentation.Progress;
        private readonly List<string> battleLog = new List<string>();
        private List<Fighter> turnOrder = new List<Fighter>();
        private int mapNodeId = -1;
        private Text livesLabel;

        private void Awake()
        {
            PrototypeUI.font = roundLabel.font;
            BattlePresentation.ApplyLayout(this);
            if (MapRun.Pending < 0 && MapRun.Current < 0) MapRun.TryEnter(0);
            mapNodeId = MapRun.Pending;
            var chapter = combatRoot.transform.Find("Combat Chapter");
            if (chapter != null)
            {
                livesLabel = chapter.GetComponent<Text>();
                livesLabel.text = "冒险生命 " + RunState.Lives + " / " + RunState.InitialLives;
                livesLabel.fontSize = 22;
            }
            if (formation != null)
            {
                formation.battle = this;
                string battleId = mapNodeId >= 0 && MapRun.Nodes[mapNodeId].Definition.type == NodeKind.Battle
                    ? MapRun.Nodes[mapNodeId].Definition.contentId : "BATTLE_001";
                enemyRoster = mapNodeId >= 0 && MapRun.Nodes[mapNodeId].Definition.type == NodeKind.Battle
                    ? GameContent.EnemyLineup(MapRun.Nodes[mapNodeId]) : GameContent.EnemyLineup(GameContent.Battles.Get(battleId));
            }
            startButton.onClick.AddListener(StartBattle);
            resetButton.onClick.AddListener(ResetFormation);
            backButton.onClick.AddListener(OpenMap);
            speedButton.onClick.AddListener(ToggleSpeed);
            retryButton.onClick.AddListener(Retry);
            mapButton.onClick.AddListener(OpenMap);
        }

        private void Start()
        {
            if (RewardFlow.Pending == null) return;
            Finished = true;
            Outcome = "胜利";
            EncounterUI.OpenReward(FindObjectOfType<PortraitLayout>().content);
        }

        public void SelectPlayerSlot(int slot)
        {
            if (IsBattling || Finished || slot < 0 || slot >= playerRoster.Count) return;
            Describe(playerRoster[slot], "我方");
            if (selectedSlot == -1)
            {
                selectedSlot = slot;
                formationHint.text = "已选择「" + playerRoster[slot].displayName + "」· 点击另一张己方卡片交换位置";
            }
            else
            {
                if (selectedSlot != slot)
                {
                    var previous = playerRoster[selectedSlot];
                    playerRoster[selectedSlot] = playerRoster[slot];
                    playerRoster[slot] = previous;
                }
                selectedSlot = -1;
                formationHint.text = "点击己方卡片，再点另一张交换站位";
            }
            RefreshFormation();
        }

        public void InspectEnemy(int slot)
        {
            if (IsBattling || Finished || slot < 0 || slot >= enemyRoster.Count) return;
            Describe(enemyRoster[slot], "敌方");
        }

        private void Describe(CharacterDefinition definition, string side)
        {
            if (definition == null) return;
            detailLabel.text = side + " · " + definition.displayName + "    攻击 " + definition.attack
                + " / 防御 " + definition.defense + " / 速度 " + definition.speed + "\n"
                + definition.SkillName + "：" + definition.SkillDescription;
        }

        private void RefreshFormation()
        {
            for (int index = 0; index < playerCards.Length; index++)
            {
                playerCards[index].Show(playerRoster[index]);
                playerCards[index].Select(index == selectedSlot);
                enemyCards[index].gameObject.SetActive(enemyRoster[index] != null);
                if (enemyRoster[index] != null) enemyCards[index].Show(enemyRoster[index]);
            }
        }

        public void ResetFormation()
        {
            if (IsBattling || Finished) return;
            if (formation != null)
            {
                formation.ResetFormation();
                return;
            }
            playerRoster = CombatRules.DefaultTeam(false);
            selectedSlot = -1;
            formationHint.text = "点击己方卡片，再点另一张交换站位";
            RefreshFormation();
        }

        public void StartBattle()
        {
            if (IsBattling || Finished || RunState.IsDefeated) return;
            if (formation != null)
            {
                playerLineup = formation.LockLineup();
                if (playerLineup == null) return;
            }
            if (playerLineup == null || playerLineup.Count != 6 || !playerLineup.Any(definition => definition != null))
            {
                Debug.LogError("开始战斗前，至少需要在阵位上布置一名角色。");
                return;
            }
            IsBattling = true;
            IsPaused = false;
            selectedSlot = -1;
            startButton.interactable = false;
            resetButton.interactable = false;
            backButton.interactable = false;
            startButton.GetComponentInChildren<Text>().text = "战斗进行中";
            formationHint.text = "初始技力2/3 · 普攻蓄满后，下次行动释放技能";
            phaseLabel.text = "自动战斗";
            detailLabel.gameObject.SetActive(false);
            detailLabel.text = "前排优先承伤 · 角色按速度依次行动\n火克草 · 草克水 · 水克火 · 克制伤害 ×1.25";
            if (formation != null)
            {
                formation.informationWindow.SetActive(false);
                formation.enemyOverlay.SetActive(false);
                formation.EnterBattleMode();
            }
            if (combatRoot != null) combatRoot.SetActive(true);
            players.Clear();
            enemies.Clear();
            views.Clear();
            actedThisRound.Clear();
            battleLog.Clear();
            hud = new BattleHud(this, combatRoot.transform, roundLabel.font);
            for (int index = 0; index < playerLineup.Count; index++)
            {
                playerCards[index].gameObject.SetActive(playerLineup[index] != null);
                if (playerLineup[index] != null)
                {
                    playerCards[index].transform.SetParent(combatRoot.transform, false);
                    PrepareCombatCard(playerCards[index], playerLineup[index], BattlePresentation.SlotPosition(false, index));
                    AddFighter(playerLineup[index], false, index, playerCards[index]);
                }
                enemyCards[index].gameObject.SetActive(enemyRoster[index] != null);
                if (enemyRoster[index] != null)
                {
                    enemyCards[index].transform.SetParent(combatRoot.transform, false);
                    PrepareCombatCard(enemyCards[index], enemyRoster[index], BattlePresentation.SlotPosition(true, index));
                    AddFighter(enemyRoster[index], true, index, enemyCards[index]);
                }
            }
            presentation = new BattlePresentation(this);
            fighterDetails = BattleFighterDetails.Create(this, combatRoot.transform.parent);
            enemyButton.interactable = false;
            var shuffled = players.Concat(enemies).OrderBy(fighter => Random.value).ToList();
            for (int index = 0; index < shuffled.Count; index++) shuffled[index].tieOrder = index;
            StartCoroutine(BattleLoop());
        }

        private void AddFighter(CharacterDefinition definition, bool enemy, int slot, CardView view)
        {
            var fighter = new Fighter(definition, enemy, slot);
            (enemy ? enemies : players).Add(fighter);
            views.Add(fighter, view);
            view.Select(false);
            // Battle cards are separate from formation cards. Replace only their battle click handlers.
            view.button.onClick.RemoveAllListeners();
            view.button.onClick.AddListener(() => InspectFighter(fighter));
            view.button.interactable = true;
            view.Show(definition, fighter);
            if (fighter.shield > 0) AddLog(definition.displayName + "：开战护盾 +" + fighter.shield);
        }

        private IEnumerator BattleLoop()
        {
            for (int round = 1; round <= CombatRules.MaxRounds; round++)
            {
                Round = round;
                roundLabel.text = "第 " + round.ToString("00") + " 回合";
                actedThisRound.Clear();
                turnOrder = players.Concat(enemies).Where(fighter => fighter.Alive)
                    .OrderByDescending(fighter => fighter.definition.speed).ThenBy(fighter => fighter.tieOrder).ToList();
                RefreshCombatCards();
                hud.SetOrder(turnOrder);
                AddLog("第 " + round + " 回合开始");
                yield return ShowRoundNotice();
                foreach (var actor in turnOrder)
                {
                    if (!actor.Alive) continue;
                    if (!players.Any(fighter => fighter.Alive) || !enemies.Any(fighter => fighter.Alive)) break;
                    yield return Act(actor);
                }
                if (!players.Any(fighter => fighter.Alive) || !enemies.Any(fighter => fighter.Alive)) break;
            }
            bool playerAlive = players.Any(fighter => fighter.Alive);
            bool enemyAlive = enemies.Any(fighter => fighter.Alive);
            Outcome = playerAlive && !enemyAlive ? "胜利" : !playerAlive && enemyAlive ? "失败" : "平局";
            Finished = true;
            if (fighterDetails != null) fighterDetails.Close();
            if (Outcome == "胜利") RewardFlow.Victory(mapNodeId);
            else if (Outcome == "失败")
            {
                RunState.RecordBattleDefeat(mapNodeId);
                if (livesLabel != null) livesLabel.text = "冒险生命 " + RunState.Lives + " / " + RunState.InitialLives;
                AdventurePersistence.SaveNow();
            }
            IsBattling = false;
            IsPaused = false;
            hud.Finish();
            AddLog("战斗结束：" + Outcome);
            phaseLabel.text = "战斗结束";
            actionLabel.text = Outcome;
            resultTitle.text = Outcome == "胜利" ? "关卡胜利" : Outcome == "失败" ? "挑战失败" : "战斗平局";
            resultDetails.text = "历经 " + Round + " 回合\n我方存活 " + players.Count(fighter => fighter.Alive) + " / " + players.Count
                + (Outcome == "平局" ? "\n已达到50回合上限" : "\n调整阵容，尝试不同的战术。");
            if (Outcome == "失败")
            {
                resultDetails.text = "历经 " + Round + " 回合\n战败扣除1点冒险生命 · 剩余 " + RunState.Lives + " / " + RunState.InitialLives
                    + (RunState.IsDefeated ? "\n生命耗尽，本次冒险结束。" : "\n可免费重新布阵，调整后再次挑战。");
                retryButton.interactable = !RunState.IsDefeated;
                retryButton.GetComponentInChildren<Text>().text = RunState.IsDefeated ? "生命已耗尽" : "重新布阵";
                mapButton.GetComponentInChildren<Text>().text = RunState.IsDefeated ? "返回主城" : "返回地图";
            }
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
            if (RewardFlow.Pending != null)
            {
                retryButton.interactable = false;
                mapButton.GetComponentInChildren<Text>().text = "选择战利品";
                EncounterUI.OpenReward(FindObjectOfType<PortraitLayout>().content);
            }
        }

        private static void PrepareCombatCard(CardView view, CharacterDefinition definition, Vector2 position)
        {
            view.gameObject.SetActive(true);
            PrototypeUI.SetPortraitCard(view, 145);
            view.Show(definition);
            view.Rect.anchoredPosition = position;
            view.homePosition = position;
        }

        private IEnumerator Act(Fighter actor)
        {
            while (IsPaused) yield return null;
            var allies = actor.enemy ? enemies : players;
            var opponents = actor.enemy ? players : enemies;
            var action = CombatRules.BeginAction(actor, allies, opponents, actor.energy >= CombatRules.SkillEnergyCost);
            if (action.openingEffects.Count > 0)
            {
                RefreshCombatCards();
                AddLog(actor.definition.displayName + " · 状态结算：" + string.Join("；", action.openingEffects.Select(effect => effect.status
                    + (effect.damage > 0 ? " 伤害" + effect.damage : "") + (effect.absorbed > 0 ? " 护盾吸收" + effect.absorbed : ""))));
                yield return presentation.Impact(action.openingEffects, views, false);
                foreach (var pair in views) pair.Value.RestoreTint();
                presentation.Reset();
            }
            bool skill = action.skill;
            int strikes = action.canAct ? CombatRules.ActionHitCount(actor, skill) : 0;
            for (int strike = 0; strike < strikes; strike++)
            {
                if (!actor.Alive || !opponents.Any(target => target.Alive)) break;
                yield return ActStrike(actor, skill, strike, strikes);
            }
            CombatRules.EndAction(action);
            actedThisRound.Add(actor);
            RefreshCombatCards();
            hud.SetOrder(turnOrder.Where(fighter => !actedThisRound.Contains(fighter)));
        }

        private IEnumerator ActStrike(Fighter actor, bool skill, int strike, int strikes)
        {
            CardView view = views[actor];
            hud.SetOrder(turnOrder.Where(fighter => !actedThisRound.Contains(fighter)), actor);
            var allies = actor.enemy ? enemies : players;
            var opponents = actor.enemy ? players : enemies;
            var targets = CombatRules.FindTargets(actor, allies, opponents, skill);
            foreach (var target in targets) views[target].MarkTarget(true, skill && actor.definition.SupportSkill);
            actionLabel.text = actor.definition.displayName + " · "
                + (skill ? actor.definition.SkillName : strikes > 1 ? "二连击 " + (strike + 1) + " / 2" : "普通攻击");
            actionLabel.color = skill ? BattlePresentation.Gold : BattlePresentation.Ivory;
            int sibling = view.transform.GetSiblingIndex();
            view.transform.SetAsLastSibling();
            view.Select(true);
            yield return presentation.Charge(actor, view, skill);
            yield return Move(view, view.homePosition + new Vector2(0, actor.enemy ? -18 : 18), .12f);
            yield return presentation.Travel(actor, view, targets.Select(target => views[target]).ToList(), skill);
            List<CombatEffect> effects = CombatRules.ResolveStrike(actor, allies, opponents, skill, strike == 0);
            actedThisRound.Add(actor);
            RefreshCombatCards();
            RecordAction(actor, skill, effects);
            if (strikes > 1) AddLog(actor.definition.displayName + "：二连击第" + (strike + 1) + "击");
            yield return presentation.Impact(effects, views, skill);
            foreach (var pair in views)
            {
                pair.Value.RestoreTint();
                pair.Value.feedback.text = "";
                pair.Value.MarkTarget(false);
            }
            yield return Move(view, view.homePosition, 0.20f);
            view.transform.SetSiblingIndex(sibling);
            view.Select(false);
            view.Rect.localScale = Vector3.one;
            presentation.Reset();
            actionLabel.text = "";
            hud.SetOrder(turnOrder.Where(fighter => !actedThisRound.Contains(fighter)));
            yield return Delay(0.10f);
        }

        private void RefreshCombatCards()
        {
            foreach (var pair in views) pair.Value.Show(pair.Key.definition, pair.Key, actedThisRound.Contains(pair.Key));
        }

        private IEnumerator ShowRoundNotice()
        {
            if (roundNotice == null)
            {
                var backdrop = PrototypeUI.Panel("Round Notice", combatRoot.transform, Vector2.zero,
                    new Vector2(900, 1600), new Color(0,.02f,.06f,.25f));
                backdrop.raycastTarget = false;
                roundNotice = backdrop.gameObject;
                var plaque = PrototypeUI.Panel("Round Plaque", backdrop.transform, new Vector2(0, 15), new Vector2(610, 142), BattlePresentation.Navy);
                PrototypeUI.Border(plaque.transform, new Vector2(610,142), BattlePresentation.Gold);
                roundNoticeText = PrototypeUI.Label("Round Announcement", plaque.transform, "", Vector2.zero,
                    new Vector2(570, 78), 36, BattlePresentation.Gold);
                roundNoticeText.font = roundLabel.font;
            }
            roundNoticeText.text = "第 " + Round.ToString("00") + " 回合 · 开始";
            roundNotice.transform.SetAsLastSibling();
            roundNotice.SetActive(true);
            yield return Delay(0.5f, false);
            roundNotice.SetActive(false);
        }

        private IEnumerator Move(CardView view, Vector2 destination, float duration)
        {
            Vector2 origin = view.Rect.anchoredPosition;
            float elapsed = 0;
            while (elapsed < duration)
            {
                if (IsPaused) { yield return null; continue; }
                elapsed += Time.unscaledDeltaTime * animationSpeed;
                float fraction = Mathf.SmoothStep(0, 1, elapsed / duration);
                view.Rect.anchoredPosition = Vector2.Lerp(origin, destination, fraction);
                yield return null;
            }
            while (IsPaused) yield return null;
            view.Rect.anchoredPosition = destination;
        }

        private IEnumerator Delay(float duration, bool scaleWithSpeed = true)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                if (!IsPaused) elapsed += Time.unscaledDeltaTime * (scaleWithSpeed ? animationSpeed : 1f);
                yield return null;
            }
            while (IsPaused) yield return null;
        }

        private void AddLog(string entry)
        {
            battleLog.Add(entry);
            if (battleLog.Count > 80) battleLog.RemoveAt(0);
            hud.SetLog(battleLog);
        }

        private void RecordAction(Fighter actor, bool skill, List<CombatEffect> effects)
        {
            var details = effects.Select(effect => (effect.target.enemy ? "敌·" : "我·") + effect.target.definition.displayName
                + (effect.damage > 0 ? " −" + effect.damage : "")
                + (effect.absorbed > 0 ? " 格挡" + effect.absorbed : "")
                + (effect.healing > 0 ? " 治疗+" + effect.healing : "")
                + (effect.shielding > 0 ? " 护盾+" + effect.shielding : "")
                + (effect.energyLost > 0 ? " 技力−" + effect.energyLost : "")
                + (string.IsNullOrEmpty(effect.status) ? "" : " [" + effect.status + "]")
                + (!effect.target.Alive ? "（阵亡）" : ""));
            AddLog((actor.enemy ? "敌·" : "我·") + actor.definition.displayName + "「"
                + (skill ? actor.definition.SkillName : "普攻") + "」：" + string.Join("；", details));
        }

        public void InspectFighter(Fighter fighter)
        {
            if (!IsBattling || Finished || fighter == null || fighterDetails == null) return;
            fighterDetails.Open(fighter);
        }

        public void PauseBattle()
        {
            if (!IsBattling || Finished) return;
            IsPaused = true;
            if (fighterDetails != null) fighterDetails.Close();
            hud.ShowPause();
        }

        public void ResumeBattle()
        {
            if (!IsBattling || !IsPaused) return;
            IsPaused = false;
            hud.HidePause();
        }

        public void ConfirmExitBattle()
        {
            if (!IsBattling || !IsPaused) return;
            StopAllCoroutines();
            presentation?.Reset();
            IsBattling = false;
            IsPaused = false;
            SceneManager.LoadScene("MapScene");
        }

        private void Update()
        {
            if (InventoryUI.BlocksBattleInput) return;
            if (!IsBattling || !Input.GetKeyDown(KeyCode.Escape)) return;
            if (fighterDetails != null && fighterDetails.IsOpen) { fighterDetails.Close(); return; }
            if (IsPaused) ResumeBattle();
            else PauseBattle();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && IsBattling && !paused) PauseBattle();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && IsBattling && !this.paused) PauseBattle();
        }

        public void ToggleSpeed()
        {
            animationSpeed = animationSpeed < 1.5f ? 2f : 1f;
            speedButton.GetComponentInChildren<Text>().text = animationSpeed.ToString("0") + "×";
        }

        public void Retry() { if (!RunState.IsDefeated && RewardFlow.Pending == null && !IsBattling) SceneManager.LoadScene("LevelScene"); }
        public void OpenMap()
        {
            if (IsBattling) return;
            if (RunState.IsDefeated)
            {
                if (TutorialRun.Active) TutorialRun.Exit(false);
                else AdventureRun.FinishIfNeeded();
                AdventurePersistence.SaveNow();
                SceneManager.LoadScene("MainScene");
                return;
            }
            if (RewardFlow.Pending != null) EncounterUI.OpenReward(FindObjectOfType<PortraitLayout>().content);
            else SceneManager.LoadScene("MapScene");
        }
    }
}
