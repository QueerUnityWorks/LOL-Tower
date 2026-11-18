using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class TutorialGuide : MonoBehaviour
    {
        private sealed class Step
        {
            public string Key, Title, Body, Target;
            public bool Action;
            public Step(string key, string title, string body, string target = null, bool action = false)
            { Key = key; Title = title; Body = body; Target = target; Action = action; }
        }

        private static TutorialGuide instance;
        private Step current;
        private RectTransform root, overlay, card, target, highlight, speechTail;
        private readonly RectTransform[] shades = new RectTransform[4];
        private string scene;
        public static bool BlocksCombat { get; private set; }
        public static bool WantsTraining => instance != null && instance.current != null && instance.current.Key == "home.start";
        public string CurrentStep => current == null ? "" : current.Key;

        private void Awake() { instance = this; }
        private void OnDestroy() { if (instance == this) { instance = null; BlocksCombat = false; } }

        public static void BuildHomeEntry(RectTransform parent)
        {
            HomeQuickEntryPresentation.BuildTutorial(parent, () => {
                if (TutorialRun.Active) OpenTraining();
                else { TutorialRun.Replay(); AdventurePersistence.SaveNow(); }
            });
        }

        public static void OpenTraining()
        {
            TutorialRun.Begin();
            AdventurePersistence.SaveNow();
            SceneManager.LoadSceneAsync(AdventureRun.ContinueScene);
        }

        private void LateUpdate()
        {
            string activeScene = SceneManager.GetActiveScene().name;
            if (scene != activeScene || root == null)
            {
                Hide(); scene = activeScene;
                var layout = FindObjectOfType<PortraitLayout>();
                root = layout == null ? null : layout.content;
            }
            if (root == null) return;
            Step next = Resolve();
            if (next != null && next.Target != null && FindTarget(next.Target) == null) next = null;
            if ((next == null ? "" : next.Key) != CurrentStep || overlay == null)
            {
                Hide(); current = next;
                if (next != null) Build(next);
                else if (TutorialRun.Active && scene != "StartScene" && scene != "MainScene") BuildQuickExit();
            }
            BlocksCombat = current != null && current.Key.StartsWith("combat.", StringComparison.Ordinal);
            if (overlay != null)
            {
                overlay.SetAsLastSibling();
                if (current != null) Position();
            }
        }

        private Step Unseen(string key, string title, string body, string targetName = null, bool action = false)
            => TutorialRun.HasSeen(key) ? null : new Step(key, title, body, targetName, action);

        private Step Resolve()
        {
            if (scene == "MainScene")
            {
                if (!TutorialRun.ShowHome) return null;
                return Unseen("home.welcome", "欢迎来到符文远征", "先认识主城，再完成一张只有四个节点的练习地图。\n战斗 → 事件 → 商店 → Boss\n教学中的伙伴、金币和道具只用于练习。")
                    ?? Unseen("home.profile", "主城与弈士信息", "上方显示你的弈士身份。真正参与战斗的是冒险中的伙伴；生命、攻击、防御、速度和技能会在布阵时逐项说明。", "Profile Bar")
                    ?? Unseen("home.codex", "三座图鉴，随时查阅", "角色图鉴查看伙伴属性与技能；敌人图鉴帮助判断敌情；道具图鉴介绍被动加成。点击对应建筑即可查阅。", "Hero Codex")
                    ?? Unseen("home.tools", "常用入口", "成就记录正式远征的历程；设置可调整声音与画面。右上角可返回标题，主城的“新手教程”入口可随时重新练习。", "Settings House")
                    ?? Unseen("home.difficulty", "开始前选择难度", "下方可选择正式远征难度，开始后本局固定。新手练习使用单独的低难度关卡，完成后再开启正式冒险。", "Difficulty Entry")
                    ?? new Step("home.start", "出发，进入新手地图", "请点击右下角的冒险按钮。\n这次会进入专用教学地图；如已有正式冒险，将为你保留原来的进度。", "Start Adventure", true);
            }
            if (!TutorialRun.Active) return null;
            if (RewardFlow.Pending != null)
                return Unseen("reward.intro", "胜利后领取战利品", "经验在胜利时已经获得；金币在确认领取时入账。可选择一件道具，也可以不选。被动道具持有即生效。", "Reward Summary")
                    ?? Unseen("reward.select", "选择奖励", "点击候选卡片选择或取消，注意“最多选”与“已选”的数量。确认后，本节点才会完成并解锁下一个节点。", "Reward Candidate 0")
                    ?? new Step("reward.claim", "确认领取，继续前进", "按需要选择奖励后，点击下方“确认领取”。", "Claim Rewards", true);
            if (scene == "LevelScene") return ResolveBattle();
            if (scene != "MapScene") return null;
            if (TutorialRun.Finished)
                return new Step("complete", "新手试炼完成", "你已掌握：伙伴属性与克制、布阵和自动战斗、事件抉择、商店与背包，以及地图常用按钮。\n返回主城后，即可开始或继续正式远征。");
            if (MapRun.Pending >= 0)
            {
                var node = MapRun.Nodes[MapRun.Pending];
                if (node.Definition.type == NodeKind.Event)
                    return Unseen("event.intro", "事件：阅读故事，作出选择", "每个选项下方都写明结果。邀请伙伴或领取补给都能继续，不需要战斗。选择后立即生效，并完成当前节点。", "Event Story")
                        ?? new Step("event.choose", "选择你需要的帮助", "请从两个选项中选择一个。\n想练习多角色布阵，可以邀请布隆同行。", "Event Choices Viewport", true);
                if (node.Definition.type == NodeKind.Shop) return ResolveShop();
            }
            var intro = Unseen("map.route", "认识你的练习路线", "这里依次是：战斗、事件、商店和Boss。\n连线箭头表示前进方向，完成当前节点才能解锁下一个。亮起的节点可以点击。", "Map Viewport")
                ?? Unseen("map.resources", "金币、伙伴与冒险等级", "顶部显示金币、伙伴数、已完成节点与等级经验。金币用于购物；升级改变奖励和商店抽取概率，不直接提高伙伴属性。", "Map Header")
                ?? Unseen("map.controls", "拖动与缩放地图", "单指拖动地图，双指捏合缩放。也可使用“＋ / −”；“全图”显示完整路线，“定位”回到当前探索位置。", "Map Zoom In")
                ?? Unseen("map.party", "角色、背包与返回主城", "“角色”查看本局伙伴；“背包”查看道具和加成。返回主城会保留本次练习，之后可以继续。", "Open Characters");
            if (intro != null) return intro;
            var available = MapRun.Nodes.FirstOrDefault(node => MapRun.CanEnter(node.Id));
            if (available == null) return null;
            string key = "map.select." + available.Id;
            string kind = available.Definition.isFinal ? "Boss" : available.Definition.TypeName;
            var map = FindObjectOfType<MapController>();
            if (map == null || map.SelectedNodeId != available.Id)
                return new Step(key, "下一站：" + kind, "点击高亮节点，查看这一关的详情。", "Map Node " + available.Id, true);
            return new Step("map.enter." + available.Id, "进入" + kind + "节点", "下方会显示已选节点。点击进入按钮，开始" + (available.Definition.isFinal ? "最后的首领练习。" : "这一站的练习。"), "Enter Level", true);
        }

        private Step ResolveShop()
        {
            var intro = Unseen("shop.intro", "商店：花费金币强化队伍", "商品会显示价格、效果和库存。道具购入后进入背包，伙伴加入候补；经验购买后立即获得。", "Shop Wallet");
            if (intro != null) return intro;
            if (!TutorialRun.HasSeen("shop.buy"))
                return new Step("shop.buy", "购买一把暴风大剑", "请点击暴风大剑的购买按钮，花费15金币。\n它会提高全队攻击力，下一场布阵即可看到加成。", "Buy " + TutorialContent.SwordGood, true);
            if (InventoryUI.IsOpen)
                return Unseen("shop.bag.info", "背包：道具持有即生效", "暴风大剑已经入包。点击道具可查看介绍和当前总加成；被动道具无需手动装备，同名道具可以叠加。", "Inventory Detail Paper")
                    ?? new Step("shop.bag.close", "返回商店", "点击背包的关闭按钮，继续学习商店操作。", "Close Inventory", true);
            if (!TutorialRun.HasSeen("shop.bag.open"))
                return new Step("shop.bag.open", "打开背包检查道具", "点击“背包”，查看刚购买的暴风大剑。", "Open Inventory", true);
            return Unseen("shop.refresh", "刷新与库存", "“刷新货物”会消耗金币，后续刷新费用递增。正式商店会重抽随机货物并补满库存；教学商店固定出售这三种商品，刷新会补货。", "Refresh Shop")
                ?? new Step("shop.leave", "补给完成，准备挑战Boss", "点击“结束购物”返回地图，解锁最后的Boss节点。", "Leave Shop", true);
        }

        private Step ResolveBattle()
        {
            var battle = FindObjectOfType<BattleController>();
            if (battle == null || battle.formation == null) return null;
            if (battle.IsBattling)
                return Unseen("combat.order", "角色会自动行动", "无需手动出招。角色按速度依次行动；同速的本场先后顺序在开战时确定，顶部显示行动队列。", "Turn Order Panel")
                    ?? Unseen("combat.energy", "生命、技力与技能", "红条是生命，蓝条是技力。初始技力2/3，普攻增加1点；满3点后，在下一次行动自动施放技能。生命归零则本场阵亡。", battle.playerCards.First(view => view.gameObject.activeInHierarchy).name)
                    ?? Unseen("combat.speed", "战斗倍速", "右上角“1× / 2×”切换动画速度，不改变战斗规则。点角色卡片可查看当前属性和状态。", "Combat Speed")
                    ?? Unseen("combat.pause", "暂停与战报", "“暂停”可暂停并继续战斗；退出需要再次确认，本场进度不会保留。“战报”查看行动记录；打开背包也会暂停战斗。\n点击“继续”后，观看这场练习。", "Pause Battle");
            if (battle.Finished)
                return new Step("battle.result", RunState.IsDefeated ? "生命耗尽，返回主城" : "战败扣命，重新布阵免费",
                    RunState.IsDefeated ? "3点冒险生命已用完。点击“返回主城”结束本次练习，可重新开始教程。"
                        : "初始有3点冒险生命，战败立即扣1点，节点不完成。重新布阵和重试免费；生命归零则本次冒险结束。胜利后领取战利品。",
                    battle.Outcome == "胜利" || RunState.IsDefeated ? battle.mapButton.name : battle.retryButton.name, true);
            var formation = battle.formation;
            if (MapRun.Pending == 3)
            {
                if (formation.informationWindow.activeInHierarchy)
                    return new Step("boss.inspect", "选择阵位，让伙伴上阵", "属性已包含背包道具的加成。\n点击空阵位可让选中的伙伴上阵；点击其他已占用阵位可交换或替换。也可取消选择，继续调整阵容。", "Preparation Interface", true);
                if (formation.enemyOverlay.activeInHierarchy)
                    return new Step("boss.close.enemy", "首领情报", "点击敌人查看其技能与护盾机制。准备好后返回布阵。", "Enemy Intelligence Popup", true);
                return Unseen("boss.intro", "Boss：综合运用所学", "林地守关者每两次行动生成25护盾。可先查看敌情，给高生命伙伴安排前排，再让输出伙伴在后排攻击。", "Enemy Intelligence")
                    ?? Unseen("boss.formation", "把新伙伴加入阵容", "事件或商店获得的伙伴会留在下方候补。点击伙伴，再点击阵位即可上阵；也可长按拖动。暴风大剑的全队攻击加成已应用。", formation.rosterScroll.viewport.name)
                    ?? new Step("boss.start", "挑战林地守关者", "布阵完成后点击“开始战斗”。如需调整，点击候补伙伴和阵位；也可以查看敌方情报。", "Preparation Interface", true);
            }
            if (!TutorialRun.HasSeen("prep.inspect"))
                return new Step("prep.inspect", "先认识你的伙伴", "上方是六个阵位。亚索已经上阵，请点击他的卡片，打开属性与技能详情。", formation.slotCards[0].name, true);
            if (formation.informationWindow.activeInHierarchy)
                return Unseen("prep.stats", "四项基础属性", "生命：能够承受多少伤害。\n攻击：影响输出；防御：降低受到的伤害。\n速度：决定每回合行动的先后顺序。", formation.informationStats.name)
                    ?? Unseen("prep.element", "属性克制与职业", "火克草、草克水、水克火。克制伤害×1.25，被克制时×0.8。战士、法师、盾卫和射手各有不同技能与职责。", formation.informationTitle.name)
                    ?? Unseen("prep.skill", "技能与被动加成", "这里写明伙伴的技能及状态效果。技能由技力驱动，在战斗中自动释放；道具加成在进入布阵时应用。", formation.informationSkill.name)
                    ?? new Step("prep.close", "返回布阵", "点击“收起”关闭详情，继续认识阵位与敌方情报。", formation.closeInformationButton.name, true);
            var arrange = Unseen("prep.formation", "前排、后排与候补", "敌人通常优先攻击前排。点击候补再点阵位可上阵；点击已上阵伙伴再点其他阵位可移动或交换。长按拖动也可以布阵。", formation.slotCards[0].name);
            if (arrange != null) return arrange;
            if (!TutorialRun.HasSeen("prep.enemy"))
                return new Step("prep.enemy", "先看敌方情报", "开战前点击“敌方情报”，了解对方阵容、属性和技能，再决定站位。", "Enemy Intelligence", true);
            if (formation.enemyOverlay.activeInHierarchy)
                return Unseen("prep.enemy.info", "知己知彼", "点击敌人卡片可以查看属性、技能和特性。第一场只有一名较弱的训练守卫，适合熟悉基本战斗。", formation.enemyDetails.name)
                    ?? new Step("prep.enemy.close", "返回并开始练习", "点击“返回布阵”，然后准备开战。", formation.closeEnemyButton.name, true);
            return new Step("prep.start", "开始第一场战斗", "至少保留一名伙伴上阵，然后点击“开始战斗”。战斗会自动进行。", "Start Battle", true);
        }

        private RectTransform FindTarget(string name)
        {
            return root.GetComponentsInChildren<RectTransform>(false).FirstOrDefault(rect => rect.name == name && (overlay == null || !rect.IsChildOf(overlay)));
        }

        private void Build(Step step)
        {
            overlay = PrototypeUI.Rect("Tutorial Guide", root, Vector2.zero, new Vector2(900, 1600));
            for (int index = 0; index < shades.Length; index++)
            {
                var shade = PrototypeUI.Panel("Tutorial Shade " + index, overlay, Vector2.zero, Vector2.zero, new Color(.015f, .035f, .055f, .76f));
                shade.raycastTarget = true; shades[index] = shade.rectTransform;
            }
            target = step.Target == null ? null : FindTarget(step.Target);
            highlight = PrototypeUI.Rect("Tutorial Highlight", overlay, Vector2.zero, Vector2.one);
            speechTail = PrototypeUI.Panel("Tutorial Speech Tail", overlay, Vector2.zero, new Vector2(24, 24), PrototypeUI.Hex("A68A52")).rectTransform;
            speechTail.localRotation = Quaternion.Euler(0, 0, 45);
            PrototypeUI.Panel("Tail Inset", speechTail, Vector2.zero, new Vector2(20, 20), PrototypeUI.Hex("102737"));
            card = PrototypeUI.Panel("Tutorial Card", overlay, Vector2.zero, new Vector2(790, 280), PrototypeUI.Hex("102737")).rectTransform;
            card.GetComponent<Image>().raycastTarget = true;
            string chapter = scene == "MainScene" ? "认识主城" : "新手远征 · " + Math.Min(4, MapRun.ClearedCount + 1) + " / 4";
            var body = Label("Tutorial Body", step.Body, Vector2.zero, new Vector2(590, 150), 22, MapPresentation.Ivory);
            body.alignment = TextAnchor.UpperLeft;
            float bodyHeight = Mathf.Max(80, body.preferredHeight + 8);
            float height = Mathf.Max(272, bodyHeight + 172);
            card.sizeDelta = new Vector2(790, height);
            PrototypeUI.Border(card, card.sizeDelta, PrototypeUI.Hex("A68A52"));
            ResourceArtwork.Create(card, "Tutorial Ahri Portrait", GameContent.Characters.Get("HERO_010").imagePath,
                new Vector2(-318, height / 2 - 82), new Vector2(112, 112), true);
            Label("Tutorial Speaker", "阿狸", new Vector2(-318, height / 2 - 157), new Vector2(126, 36), 23, MapPresentation.Gold);
            var chapterLabel = Label("Tutorial Chapter", "阿狸 · " + chapter, new Vector2(64, height / 2 - 28), new Vector2(590, 30), 17, MapPresentation.Gold);
            chapterLabel.alignment = TextAnchor.MiddleLeft;
            var titleLabel = Label("Tutorial Title", step.Title, new Vector2(64, height / 2 - 66), new Vector2(590, 44), 26, MapPresentation.Ivory);
            titleLabel.alignment = TextAnchor.MiddleLeft;
            body.rectTransform.pivot = new Vector2(.5f, 1);
            body.rectTransform.anchoredPosition = new Vector2(64, height / 2 - 98);
            body.rectTransform.sizeDelta = new Vector2(590, bodyHeight);
            float buttonsY = -height / 2 + 42;
            MainHubController.GoldButton("Tutorial Skip", card, TutorialRun.Active ? "退出教程" : "跳过教程", new Vector2(-238, buttonsY), new Vector2(206, 54)).onClick.AddListener(Skip);
            if (TutorialRun.Active)
                MainHubController.GoldButton("Tutorial Later", card, "稍后继续", new Vector2(0, buttonsY), new Vector2(190, 54)).onClick.AddListener(Pause);
            if (step.Action)
                Label("Tutorial Action Hint", "点击高亮处", new Vector2(245, buttonsY), new Vector2(206, 54), 22, MapPresentation.Gold);
            else
                MainHubController.GoldButton("Tutorial Next", card, step.Key == "complete" ? "完成教程" : "继续  ›", new Vector2(238, buttonsY), new Vector2(206, 54)).onClick.AddListener(Advance);
            Position();
        }

        private Text Label(string name, string value, Vector2 position, Vector2 size, int fontSize, Color tint)
            => PrototypeUI.Label(name, card, value, position, size, fontSize, tint);

        private void Position()
        {
            if (card == null) return;
            Rect hole = new Rect(0, 0, 0, 0);
            if (target != null && target.gameObject.activeInHierarchy)
            {
                var corners = new Vector3[4]; target.GetWorldCorners(corners);
                var lower = root.InverseTransformPoint(corners[0]); var upper = root.InverseTransformPoint(corners[2]);
                hole = Rect.MinMaxRect(Mathf.Clamp(lower.x - 10, -446, 446), Mathf.Clamp(lower.y - 10, -796, 796),
                    Mathf.Clamp(upper.x + 10, -446, 446), Mathf.Clamp(upper.y + 10, -796, 796));
            }
            SetRect(shades[0], Rect.MinMaxRect(-450, hole.yMax, 450, 800));
            SetRect(shades[1], Rect.MinMaxRect(-450, -800, 450, hole.yMin));
            SetRect(shades[2], Rect.MinMaxRect(-450, hole.yMin, hole.xMin, hole.yMax));
            SetRect(shades[3], Rect.MinMaxRect(hole.xMax, hole.yMin, 450, hole.yMax));
            bool hasTarget = target != null;
            highlight.gameObject.SetActive(hasTarget);
            if (hasTarget)
            {
                SetRect(highlight, hole);
                if (highlight.childCount == 0) PrototypeUI.Border(highlight, hole.size, MapPresentation.Gold);
            }
            speechTail.gameObject.SetActive(hasTarget);
            if (!hasTarget) { card.anchoredPosition = Vector2.zero; return; }
            var half = card.sizeDelta / 2;
            float gap = 22;
            var candidates = new[] {
                new Vector2(hole.center.x, hole.yMax + gap + half.y),
                new Vector2(hole.center.x, hole.yMin - gap - half.y),
                new Vector2(hole.xMin - gap - half.x, hole.center.y),
                new Vector2(hole.xMax + gap + half.x, hole.center.y)
            };
            float bestScore = float.PositiveInfinity;
            Vector2 best = Vector2.zero;
            foreach (var candidate in candidates)
            {
                var position = new Vector2(Mathf.Clamp(candidate.x, -434 + half.x, 434 - half.x),
                    Mathf.Clamp(candidate.y, -784 + half.y, 784 - half.y));
                var bounds = new Rect(position - half, card.sizeDelta);
                float overlapWidth = Mathf.Max(0, Mathf.Min(bounds.xMax, hole.xMax) - Mathf.Max(bounds.xMin, hole.xMin));
                float overlapHeight = Mathf.Max(0, Mathf.Min(bounds.yMax, hole.yMax) - Mathf.Max(bounds.yMin, hole.yMin));
                float score = overlapWidth * overlapHeight * 100 + Vector2.Distance(position, candidate);
                if (score < bestScore) { bestScore = score; best = position; }
            }
            card.anchoredPosition = best;
            var cardBounds = new Rect(best - half, card.sizeDelta);
            bool above = best.y >= hole.center.y;
            bool vertical = hole.height > 1400 || cardBounds.yMin >= hole.yMax || cardBounds.yMax <= hole.yMin;
            speechTail.anchoredPosition = vertical
                ? new Vector2(Mathf.Clamp(hole.center.x, cardBounds.xMin + 28, cardBounds.xMax - 28), above ? cardBounds.yMin + 3 : cardBounds.yMax - 3)
                : new Vector2(best.x >= hole.center.x ? cardBounds.xMin + 3 : cardBounds.xMax - 3,
                    Mathf.Clamp(hole.center.y, cardBounds.yMin + 28, cardBounds.yMax - 28));
        }

        private static void SetRect(RectTransform rect, Rect bounds)
        { rect.anchoredPosition = bounds.center; rect.sizeDelta = bounds.size; }

        private void BuildQuickExit()
        {
            overlay = PrototypeUI.Rect("Tutorial Status", root, Vector2.zero, new Vector2(900, 1600));
            var exit = MainHubController.GoldButton("Tutorial Quick Exit", overlay, "退出教程", new Vector2(315, 558), new Vector2(174, 48));
            exit.GetComponentInChildren<Text>().fontSize = 20;
            exit.onClick.AddListener(Skip);
        }

        private void Advance()
        {
            if (current == null || current.Action) return;
            if (current.Key == "complete") { Finish(true); return; }
            if (current.Key == "map.controls") FindObjectOfType<MapViewport>()?.Fit();
            TutorialRun.Mark(current.Key);
            AdventurePersistence.SaveNow();
        }

        private void Skip() { Finish(false); }
        private void Pause()
        {
            FindObjectOfType<BattleController>()?.StopAllCoroutines();
            Hide(); AdventurePersistence.SaveNow();
            SceneManager.LoadSceneAsync("MainScene");
        }
        private void Finish(bool completed)
        {
            bool active = TutorialRun.Active;
            if (active) FindObjectOfType<BattleController>()?.StopAllCoroutines();
            TutorialRun.Exit(completed);
            Hide(); AdventurePersistence.SaveNow();
            if (active) SceneManager.LoadSceneAsync("MainScene");
        }

        private void Hide()
        {
            if (overlay != null) { overlay.gameObject.SetActive(false); Destroy(overlay.gameObject); }
            overlay = card = target = highlight = speechTail = null; current = null; BlocksCombat = false;
        }
    }
}
