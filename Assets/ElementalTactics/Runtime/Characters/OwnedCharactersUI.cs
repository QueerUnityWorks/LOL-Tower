using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class OwnedCharactersUI : MonoBehaviour
    {
        private RectTransform parent;
        private GameObject overlay;
        private ScrollRect scroll;
        private Text summary;
        private Text characterTitle;
        private Text stats;
        private Text skill;
        private Text classification;
        private ResourceArtwork selectedPortrait;
        private ScrollRect skillScroll;
        private Font font;
        private readonly List<CardView> cards = new List<CardView>();
        private readonly List<CharacterDefinition> definitions = new List<CharacterDefinition>();
        private FormationState formation;
        public bool IsOpen => overlay != null && overlay.activeSelf;

        public void Build(RectTransform mapRoot)
        {
            parent = (RectTransform)mapRoot.parent;
            font = PrototypeUI.font;
            var open = PrototypeUI.Button("Open Characters", mapRoot, "角\n色", new Vector2(-405, 140), new Vector2(68, 112), false);
            open.GetComponentInChildren<Text>().fontSize = 24;
            open.onClick.AddListener(Open);
        }

        public void Open()
        {
            if (IsOpen || InventoryUI.IsOpen) return;
            PrototypeUI.font = font;
            if (overlay == null) CreateOverlay();
            overlay.transform.SetAsLastSibling();
            overlay.SetActive(true);
            foreach (Transform child in scroll.content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            cards.Clear();
            definitions.Clear();
            formation = RunState.RestoreFormation();
            var owned = RunState.Characters;
            summary.text = "拥有 " + owned.Count + " 张　·　已上阵 " + formation.Count + " / 6　·　点击角色查看详情";
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(scroll.viewport.rect.height, Mathf.CeilToInt(owned.Count / 4f) * 310 + 24));
            for (int index = 0; index < owned.Count; index++)
            {
                int selected = index;
                var definition = ItemBonuses.Apply(owned[index].Definition);
                definitions.Add(definition);
                // A 168-wide portrait is 252 high: leave 20px above the first card and room for its status below.
                var position = new Vector2(-276 + index % 4 * 184, -146 - index / 4 * 310);
                var card = PrototypeUI.CreateFormationCard(scroll.content, "Owned Character " + index, position, definition, 168);
                card.Rect.anchorMin = card.Rect.anchorMax = new Vector2(0.5f, 1);
                card.button.onClick.AddListener(() => Select(selected));
                cards.Add(card);
                var status = PrototypeUI.Label("Owned Character Status " + index, scroll.content, Status(index),
                    position + new Vector2(0, -145), new Vector2(178, 28), 16, MapPresentation.Muted);
                status.rectTransform.anchorMin = status.rectTransform.anchorMax = new Vector2(0.5f, 1);
            }
            if (owned.Count > 0) Select(0);
            else { characterTitle.text = "暂无角色"; classification.text = ""; stats.text = "旅途中获得的角色会显示在这里。"; selectedPortrait.SetPath(null); InventoryPresentation.SetDescription(skill, skillScroll, ""); }
            scroll.StopMovement();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 1;
        }

        private void CreateOverlay()
        {
            var paper = InventoryPresentation.Shell(parent, "Characters", "角色背包", "role", Close, out overlay, out summary);
            InventoryPresentation.Panel("Character Detail Paper", paper, new Vector2(0,367), new Vector2(780,340), InventoryPresentation.Surface, InventoryPresentation.Border);
            InventoryPresentation.ArtworkFrame("Selected Character Frame", paper, new Vector2(-292,394), new Vector2(150,221), MapPresentation.Gold);
            selectedPortrait = ResourceArtwork.Create(paper, "Selected Character Portrait", null, new Vector2(-292,394), new Vector2(142,213), true);
            characterTitle = InventoryPresentation.Label("Owned Character Title", paper, "", new Vector2(86,495), new Vector2(538,47), 31, MapPresentation.Ivory, true);
            InventoryPresentation.FitLine(characterTitle,22);
            classification = InventoryPresentation.Label("Owned Character Classification", paper, "", new Vector2(86,451), new Vector2(538,32), 19, MapPresentation.Cyan);
            InventoryPresentation.FitLine(classification);
            stats = InventoryPresentation.Label("Owned Character Stats", paper, "", new Vector2(86,411), new Vector2(538,36), 21, MapPresentation.Ivory);
            InventoryPresentation.FitLine(stats);
            skill = InventoryPresentation.Description("Owned Character Skill", paper, new Vector2(86,299), new Vector2(538,170), out skillScroll);
            InventoryPresentation.Label("Characters Grid Heading", paper, "全部伙伴", new Vector2(-224,165), new Vector2(330,38), 24, MapPresentation.Gold, true);
            InventoryPresentation.Label("Characters Grid Hint", paper, "点击卡片查看技能", new Vector2(222,165), new Vector2(336,32), 18, MapPresentation.Muted, false, TextAnchor.MiddleRight);
            scroll = InventoryPresentation.Scroll("Characters", paper, new Vector2(0,-219), new Vector2(780,700), 736);
            InventoryPresentation.Label("Characters Hint", paper, "每行四张 · 上下滑动查看全部伙伴", new Vector2(0,-622), new Vector2(770,40), 21, MapPresentation.Ivory, false, TextAnchor.MiddleCenter);
            InventoryPresentation.Label("Characters Footnote", paper, "属性已包含道具加成；进入战斗节点后可调整布阵。", new Vector2(0,-675), new Vector2(770,48), 19, MapPresentation.Muted, false, TextAnchor.MiddleCenter);
        }

        private string Status(int index)
        {
            int slot = formation.FindSlot(index);
            return slot < 0 ? "候补" : (slot < 3 ? "前排" : "后排") + new[] { "左", "中", "右" }[slot % 3] + " · 已上阵";
        }

        private void Select(int index)
        {
            var definition = definitions[index];
            characterTitle.text = definition.displayName;
            classification.text = RarityRules.Name(definition.rarity) + " · " + definition.ElementName + " / " + definition.RoleName + " · " + Status(index);
            classification.color = InventoryPresentation.Rarity(definition.rarity);
            string portraitPath = "CardPortraits/" + definition.imagePath;
            selectedPortrait.SetPath(!string.IsNullOrEmpty(definition.imagePath) && Resources.Load<Texture2D>(portraitPath) != null ? portraitPath : definition.imagePath);
            stats.text = "生命 " + definition.maxHealth + "　攻击 " + definition.attack + "　防御 " + definition.defense + "　速度 " + definition.speed;
            InventoryPresentation.SetDescription(skill, skillScroll, definition.SkillName + "\n" + definition.SkillDescription
                + (definition.doubleAttack ? "\n道具加成：普攻二连击，每次行动仅获得1点技力。" : "")
                + (string.IsNullOrEmpty(definition.biography) ? "" : "\n\n" + definition.biography));
            for (int card = 0; card < cards.Count; card++) cards[card].Select(card == index);
        }

        public void Close()
        {
            if (!IsOpen) return;
            scroll.StopMovement();
            skillScroll.StopMovement();
            overlay.SetActive(false);
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void OnDestroy()
        {
            if (overlay != null) Destroy(overlay);
        }
    }
}
