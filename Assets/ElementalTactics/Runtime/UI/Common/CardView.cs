using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public class CardView : MonoBehaviour
    {
        public Button button;
        public Image body;
        public Image accent;
        public Image healthFill;
        public Image shieldFill;
        public Image selection;
        public Text title;
        public Text rarityLabel;
        public ElementBadge elementBadge;
        public Text classification;
        public Text healthText;
        public Text energyText;
        public Image energyTrack;
        public Image energyFill;
        public Text shieldText;
        public Text feedback;
        public RoleEmblem emblem;
        public ResourceArtwork portrait;
        public CanvasGroup group;
        public Vector2 homePosition;
        private Color restingColor;
        private Image targetHighlight;
        private BattleStatusStrip statusStrip;

        public RectTransform Rect => (RectTransform)transform;

        private void Awake()
        {
            if (healthFill != null) healthFill.color = PrototypeUI.HealthBarRed;
            if (energyFill != null) energyFill.color = PrototypeUI.SkillBarBlue;
        }

        public void Show(CharacterDefinition definition, Fighter fighter = null, bool actedThisRound = false)
        {
            bool alive = fighter == null || fighter.Alive;
            title.text = definition.displayName;
            classification.text = definition.ElementName + " · " + definition.RoleName;
            classification.color = PrototypeUI.Hex("C2D0DB");
            if (elementBadge == null) elementBadge = ElementBadge.Create(transform, Vector2.zero, 28, definition.element);
            elementBadge.SetElement(definition.element);
            LayoutElementBadge();
            Color elementColor = PrototypeUI.ElementColor(definition.element);
            restingColor = PrototypeUI.Hex("101E30");
            body.color = restingColor;
            accent.color = elementColor;
            emblem.role = definition.role;
            emblem.color = elementColor;
            emblem.SetVerticesDirty();
            if (portrait != null)
            {
                string cardPath = "CardPortraits/" + definition.imagePath;
                portrait.SetPath(!string.IsNullOrEmpty(definition.imagePath) && Resources.Load<Texture2D>(cardPath) != null
                    ? cardPath : definition.imagePath);
                portrait.color = Color.white;
            }
            int health = fighter == null ? definition.maxHealth : fighter.health;
            healthText.text = alive ? "生命 " + health + "/" + definition.maxHealth : "已阵亡";
            healthFill.fillAmount = Mathf.Clamp01((float)health / Mathf.Max(1, definition.maxHealth));
            healthFill.rectTransform.localScale = new Vector3(healthFill.fillAmount, 1, 1);
            healthFill.color = PrototypeUI.HealthBarRed;
            int energy = fighter == null ? CombatRules.InitialEnergy : fighter.energy;
            energyText.text = "技力 " + energy + "/" + CombatRules.SkillEnergyCost + (alive && energy >= CombatRules.SkillEnergyCost ? " · 就绪" : "");
            energyText.color = energy >= CombatRules.SkillEnergyCost ? PrototypeUI.Hex("F1D391") : PrototypeUI.Hex("BBCDDD");
            if (energyFill != null)
            {
                float progress = alive ? Mathf.Clamp01(energy / (float)CombatRules.SkillEnergyCost) : 0;
                energyFill.rectTransform.localScale = new Vector3(progress, 1, 1);
                energyFill.color = PrototypeUI.SkillBarBlue;
            }
            shieldText.text = fighter != null && fighter.shield > 0 ? "护盾 " + fighter.shield : "";
            RefreshShieldBar(alive && fighter != null ? fighter.shield : 0, definition.maxHealth);
            group.alpha = alive ? 1f : 0.24f;
            if (fighter != null && statusStrip == null)
                statusStrip = PrototypeUI.Rect("Current Effects", transform, Vector2.zero, Vector2.zero).gameObject.AddComponent<BattleStatusStrip>();
            if (statusStrip != null) statusStrip.Bind(fighter, title.font);
        }

        public void Select(bool selected) { selection.enabled = selected; }

        public void LayoutElementBadge()
        {
            // Keep old serialized references compatible while hiding their text and plate.
            if (rarityLabel != null) { rarityLabel.text = ""; rarityLabel.gameObject.SetActive(false); }
            foreach (string name in new[] { "Rarity", "Rarity Plate" })
            {
                var old = transform.Find(name);
                if (old != null) old.gameObject.SetActive(false);
            }
            if (elementBadge == null) return;
            float width = Rect.rect.width, height = Rect.rect.height;
            float size = Mathf.Clamp(width * .20f, 24, 44);
            float inset = width * .05f;
            elementBadge.rectTransform.sizeDelta = Vector2.one * size;
            elementBadge.rectTransform.anchoredPosition = new Vector2(-width / 2 + inset + size / 2, height / 2 - inset - size / 2);
            elementBadge.transform.SetAsLastSibling();
        }

        public void RefreshShieldBar(int shield, int maxHealth)
        {
            if (healthFill == null) return;
            if (shieldFill == null)
            {
                shieldFill = PrototypeUI.Panel("Shield Overlay", healthFill.transform.parent,
                    Vector2.zero, Vector2.one, PrototypeUI.Hex("B1B5BC"));
                shieldFill.raycastTarget = false;
            }
            AlignShieldBar();
            shieldFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(shield / (float)Mathf.Max(1, maxHealth)), 1, 1);
            shieldFill.color = new Color(0.694f, 0.710f, 0.737f, .85f);
            shieldFill.gameObject.SetActive(shield > 0);
        }

        public void AlignShieldBar()
        {
            if (healthFill == null || shieldFill == null) return;
            var healthRect = healthFill.rectTransform;
            var shieldRect = shieldFill.rectTransform;
            shieldRect.anchorMin = healthRect.anchorMin;
            shieldRect.anchorMax = healthRect.anchorMax;
            shieldRect.pivot = healthRect.pivot;
            shieldRect.anchoredPosition = healthRect.anchoredPosition;
            shieldRect.sizeDelta = healthRect.sizeDelta;
            shieldRect.localRotation = healthRect.localRotation;
            // Same full track geometry; retain shield ratio when the layout changes.
            shieldFill.transform.SetSiblingIndex(healthFill.transform.GetSiblingIndex() + 1);
        }

        public void MarkTarget(bool marked, bool beneficial = false)
        {
            if (targetHighlight == null && marked)
            {
                targetHighlight = PrototypeUI.Panel("Target Highlight", transform, Vector2.zero, Rect.sizeDelta + new Vector2(12, 12), Color.white);
                targetHighlight.transform.SetAsFirstSibling();
                targetHighlight.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3, 3);
            }
            if (targetHighlight == null) return;
            Color tint = beneficial ? PrototypeUI.Hex("548C62") : PrototypeUI.HealthBarRed;
            targetHighlight.color = new Color(tint.r, tint.g, tint.b, 0.35f);
            targetHighlight.GetComponent<Outline>().effectColor = tint;
            targetHighlight.gameObject.SetActive(marked);
        }

        public void EnableSkillBar()
        {
            if (energyTrack == null)
                energyTrack = PrototypeUI.Panel("Skill Track", transform, new Vector2(0, -80), new Vector2(186, 6), PrototypeUI.Background);
            if (energyFill == null)
            {
                energyFill = PrototypeUI.Panel("Skill Fill", transform, new Vector2(-93, -80), new Vector2(186, 6), PrototypeUI.SkillBarBlue);
                energyFill.rectTransform.pivot = new Vector2(0, 0.5f);
            }
            energyTrack.gameObject.SetActive(true);
            energyFill.gameObject.SetActive(true);
            energyText.gameObject.SetActive(true);
        }
        public void Tint(Color tint)
        {
            body.color = tint;
            if (portrait != null) portrait.color = Color.Lerp(Color.white, tint, .5f);
        }
        public void RestoreTint()
        {
            body.color = restingColor;
            if (portrait != null) portrait.color = Color.white;
        }
    }
}
