using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics
{
    public static class EncounterUI
    {
        private static RectTransform Shell(Transform parent, string name, string title, string subtitle)
        {
            var root = JourneyPresentation.Shell(parent, name, title, subtitle, "符文远征 · 每一次选择，都将改变旅程");
            var layout = Object.FindObjectOfType<PortraitLayout>();
            var inventory = layout == null ? null : layout.content.Find("Inventory Interface");
            if (inventory != null) inventory.SetAsLastSibling();
            return root;
        }
        private static void ReturnMap() { SceneManager.LoadScene("MapScene"); }
        public static void OpenEvent(Transform parent)
        {
            var node = MapRun.Nodes[MapRun.Pending];
            var record = GameContent.Events.Get(node.Definition.contentId);
            var root = Shell(parent, "Event Encounter", record.name, "旅途奇遇 / 事件");
            JourneyPresentation.Panel("Event Artwork Frame", root, new Vector2(0, 398), new Vector2(810, 348), InventoryPresentation.Field, MapPresentation.Gold);
            ResourceArtwork.Create(root, "Encounter CG", record.cg, new Vector2(0, 398), new Vector2(794, 332), true);
            var scroll = InventoryPresentation.Scroll("Event Choices", root, new Vector2(0, -174), new Vector2(810, 750), 766);
            var story = JourneyPresentation.Label("Event Story", scroll.content, record.description, Vector2.zero, new Vector2(710, 80), 25, MapPresentation.Ivory, false, TextAnchor.UpperLeft);
            float storyHeight = Mathf.Max(80, story.preferredHeight + 12);
            story.rectTransform.sizeDelta = new Vector2(710, storyHeight);
            story.rectTransform.anchorMin = story.rectTransform.anchorMax = story.rectTransform.pivot = new Vector2(.5f, 1);
            story.rectTransform.anchoredPosition = new Vector2(0, -24);
            float top = 24 + storyHeight + 30;
            foreach (var option in record.options)
            {
                var effects = RunState.Effects(option);
                string outcome = string.Join("　/　", effects.Select(effect => RunState.Describe(effect)
                    + (effect.type == EffectKind.GiveItem ? " · " + ItemBonuses.Summary(GameContent.Items.Get(effect.targetId), effect.amount) : "")));
                bool allowed = RunState.CanApply(effects);
                var button = JourneyPresentation.Button("Event Option " + option.id, scroll.content, "", Vector2.zero, new Vector2(738, 160));
                var title = JourneyPresentation.Label("Option Title", button.transform, option.text, Vector2.zero, new Vector2(670, 40), 25, allowed ? MapPresentation.Gold : MapPresentation.Dim, true, TextAnchor.UpperLeft);
                float titleHeight = Mathf.Max(40, title.preferredHeight);
                var body = JourneyPresentation.Label("Option Outcome", button.transform, outcome + (allowed ? "" : "\n条件不足，暂不可选择"), Vector2.zero, new Vector2(670, 60), 21, allowed ? MapPresentation.Muted : MapPresentation.Dim, false, TextAnchor.UpperLeft);
                float bodyHeight = Mathf.Max(34, body.preferredHeight);
                float height = 48 + titleHeight + bodyHeight + 12;
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(738, height);
                button.transform.Find("Frame").GetComponent<RectTransform>().sizeDelta = new Vector2(738, height);
                JourneyPresentation.TopAnchor(button.GetComponent<RectTransform>());
                button.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -top - height / 2);
                title.rectTransform.sizeDelta = new Vector2(670, titleHeight);
                title.rectTransform.anchoredPosition = new Vector2(0, height / 2 - 24 - titleHeight / 2);
                body.rectTransform.sizeDelta = new Vector2(670, bodyHeight);
                body.rectTransform.anchoredPosition = new Vector2(0, height / 2 - 36 - titleHeight - bodyHeight / 2);
                button.interactable = allowed;
                button.onClick.AddListener(() =>
                {
                    int previousLevel = Progression.Level;
                    if (!RunState.ChooseEvent(option.id)) { GameAudio.Play(SoundCue.Error); return; }
                    GameAudio.PlayReward(effects, previousLevel, SoundCue.Confirm);
                    ReturnMap();
                });
                top += height + 18;
            }
            scroll.content.sizeDelta = new Vector2(766, Mathf.Max(scroll.viewport.rect.height, top + 10));
            InventoryPresentation.Top(scroll);
            JourneyPresentation.Label("Event Choice Hint", root, "选择一项 · 结果立即生效", new Vector2(0, -612), new Vector2(810, 42), 22, MapPresentation.Gold, false, TextAnchor.MiddleCenter);
        }
        public static void OpenShop(Transform parent)
        {
            GameAudio.PlayMusic(MusicTrack.Home);
            int nodeId = MapRun.Pending;
            var shop = GameContent.Shops.Get(MapRun.Nodes[nodeId].Definition.contentId);
            var root = Shell(parent, "Shop Encounter", shop.name, "旅途补给 / 商店");
            root.gameObject.AddComponent<ShopUI>().Build(nodeId, shop);
        }
        public static void OpenReward(Transform parent)
        {
            if (RewardFlow.Pending == null || parent.Find("Victory Rewards") != null) return;
            var reward = RewardFlow.Pending;
            var selected = new HashSet<int>();
            var root = Shell(parent, "Victory Rewards", "胜利战利品", "战斗胜利 / 选择奖励");
            var summary = JourneyPresentation.Panel("Reward Summary", root, new Vector2(0, 468), new Vector2(810, 206), null, MapPresentation.Gold);
            MapPresentation.Art("Victory Crown", summary.transform, "Crown", new Vector2(-327, 30), new Vector2(96, 68), MapPresentation.Gold);
            JourneyPresentation.Label("Reward Gold", summary.transform, "金币 +" + reward.Gold + "  ·  确认后领取", new Vector2(52, 53), new Vector2(620, 42), 27, MapPresentation.Gold, true);
            JourneyPresentation.Label("Reward Experience", summary.transform, "经验 +" + reward.Experience + " 已获得  ·  " + Progression.Label, new Vector2(52, 7), new Vector2(620, 40), 21, MapPresentation.Cyan);
            string guaranteed = reward.GuaranteedDrops.Length == 0 ? "战利品可自由选择，未选部分将放弃" : "已获得：" + string.Join(" / ", reward.GuaranteedDrops.Select(RunState.Describe));
            var grant = JourneyPresentation.Label("Reward Guaranteed", summary.transform, guaranteed, new Vector2(0, -56), new Vector2(738, 60), 20, MapPresentation.Muted);
            InventoryPresentation.FitLine(grant, 17);
            var hint = JourneyPresentation.Label("Reward Choice Hint", root, "", new Vector2(0, 307), new Vector2(798, 78), 23, MapPresentation.Ivory, true);
            var scroll = InventoryPresentation.Scroll("Reward Candidates", root, new Vector2(0, -156), new Vector2(810, 798), 766);
            var buttons = new List<Button>(); var states = new List<Text>();
            float top = 16;
            for (int index = 0; index < reward.Candidates.Length; index++)
            {
                var effect = reward.Candidates[index];
                var rarity = InventoryPresentation.Rarity(RarityRules.Of(effect));
                var button = JourneyPresentation.Button("Reward Candidate " + index, scroll.content, "", Vector2.zero, new Vector2(738, 216));
                var detail = JourneyPresentation.Label("Candidate Detail", button.transform, JourneyPresentation.EffectDetail(effect), Vector2.zero, new Vector2(492, 68), 20, MapPresentation.Muted, false, TextAnchor.UpperLeft);
                float detailHeight = Mathf.Max(68, detail.preferredHeight);
                float rowHeight = Mathf.Max(216, 126 + detailHeight);
                var rect = button.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(738, rowHeight);
                JourneyPresentation.TopAnchor(rect);
                rect.anchoredPosition = new Vector2(0, -top - rowHeight / 2);
                button.transform.Find("Frame").GetComponent<RectTransform>().sizeDelta = rect.sizeDelta;
                float upper = rowHeight / 2;
                JourneyPresentation.EffectArtwork(button.transform, effect, new Vector2(-264, upper - 108), new Vector2(174, 174));
                var title = JourneyPresentation.Label("Candidate Name", button.transform, JourneyPresentation.EffectName(effect), new Vector2(20, upper - 42), new Vector2(330, 44), 26, null, true);
                InventoryPresentation.FitLine(title, 20);
                JourneyPresentation.Label("Candidate Kind", button.transform, RarityRules.Name(RarityRules.Of(effect)) + " · " + JourneyPresentation.EffectKindLabel(effect) + " ×" + effect.amount, new Vector2(101, upper - 84), new Vector2(492, 32), 20, rarity);
                detail.rectTransform.sizeDelta = new Vector2(492, detailHeight);
                detail.rectTransform.pivot = new Vector2(.5f, 1);
                detail.rectTransform.anchoredPosition = new Vector2(101, upper - 110);
                var state = JourneyPresentation.Label("Selection State", button.transform, "点击选择", new Vector2(274, upper - 42), new Vector2(146, 34), 20, MapPresentation.Gold, true, TextAnchor.MiddleRight);
                buttons.Add(button); states.Add(state);
                int candidate = index;
                UiAudioFeedback.Bind(button, SoundCue.CardSelect);
                button.onClick.AddListener(() => { if (!selected.Remove(candidate) && selected.Count < reward.ChooseCount) selected.Add(candidate); RefreshReward(reward, selected, buttons, states, hint); });
                top += rowHeight + 18;
            }
            if (reward.Candidates.Length == 0) JourneyPresentation.Label("No Candidates", scroll.viewport, "本次没有可选战利品\n确认即可领取金币", Vector2.zero, new Vector2(700, 120), 27, MapPresentation.Muted, false, TextAnchor.MiddleCenter);
            scroll.content.sizeDelta = new Vector2(766, Mathf.Max(scroll.viewport.rect.height, top + 8));
            InventoryPresentation.Top(scroll);
            RefreshReward(reward, selected, buttons, states, hint);
            JourneyPresentation.Button("Claim Rewards", root, "确认领取  ›", new Vector2(0, -618), new Vector2(810, 82), () =>
            {
                int previousFloor = MapRun.CurrentFloor;
                int previousLevel = Progression.Level;
                var effects = selected.Select(index => reward.Candidates[index]).ToArray();
                if (!RewardFlow.Claim(selected)) { GameAudio.Play(SoundCue.Error); return; }
                if (MapRun.CurrentFloor > previousFloor) GameAudio.Play(SoundCue.NodeUnlock);
                else GameAudio.PlayReward(effects, previousLevel);
                if (MapRun.Finished && !TutorialRun.Active)
                {
                    AdventurePersistence.SaveNow();
                    if (SettlementController.TryOpen(parent))
                    {
                        root.gameObject.SetActive(false);
                        Object.Destroy(root.gameObject);
                        return;
                    }
                }
                ReturnMap();
            }, true);
        }
        private static void RefreshReward(PendingReward reward, HashSet<int> selected, List<Button> buttons, List<Text> states, Text hint)
        {
            hint.text = reward.DifficultyName + " · 敌方战力 " + reward.EnemyPower.ToString("F0") + "\n最多选 " + reward.ChooseCount + " 项 · 已选 " + selected.Count + " 项（可不选）";
            for (int i = 0; i < buttons.Count; i++)
            {
                bool chosen = selected.Contains(i);
                buttons[i].interactable = chosen || selected.Count < reward.ChooseCount;
                buttons[i].GetComponent<Image>().color = chosen ? InventoryPresentation.Selected : InventoryPresentation.Surface;
                buttons[i].transform.Find("Frame").GetComponent<Image>().color = chosen ? MapPresentation.Gold : InventoryPresentation.Border;
                states[i].text = chosen ? "✓ 已选择" : "点击选择";
            }
        }
    }
}

