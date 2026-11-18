using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElementalTactics
{
    public class FormationController : MonoBehaviour
    {
        public BattleController battle;
        public List<CharacterDefinition> availableRoster;
        public CardView[] slotCards;
        public Button[] slotButtons;
        public CardView[] reserveCards;
        public Text[] reserveStatus;
        public ScrollRect rosterScroll;
        public GameObject informationWindow;
        public Text informationTitle;
        public Text informationStats;
        public Text informationSkill;
        public RoleEmblem informationEmblem;
        public Image informationAccent;
        public Button closeInformationButton;
        public Button removeButton;
        public Button refreshButton;
        public Text teamCount;
        public Text hint;
        public GameObject enemyOverlay;
        public GameObject preparationRoot;
        public GameObject battleRoot;
        public Button enemyButton;
        public Button closeEnemyButton;
        public CardView[] previewCards;
        public Text enemyDetails;
        public FormationState State { get; private set; }
        public int SelectedIndex { get; private set; } = -1;
        public bool InformationOpen { get; private set; }
        private bool locked;
        private int draggedCharacter = -1;
        private CardView dragSource;
        private CardView dragPreview;
        private Image dropHighlight;
        private Vector2 dragOffset;
        private FormationPresentation presentation;
        public bool CanDrag => isActiveAndEnabled && preparationRoot.activeInHierarchy
            && !locked && !battle.IsBattling && !enemyOverlay.activeSelf && draggedCharacter < 0;

        private void Awake()
        {
            PrototypeUI.font = informationTitle.font;
            availableRoster = RunState.Characters.Select(character => ItemBonuses.Apply(character.Definition)).ToList();
            foreach (Transform child in rosterScroll.content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            reserveCards = new CardView[availableRoster.Count];
            reserveStatus = new Text[availableRoster.Count];
            for (int index = 0; index < availableRoster.Count; index++)
            {
                reserveCards[index] = PrototypeUI.CreateFormationCard(rosterScroll.content, "Reserve Card " + (index + 1), Vector2.zero, availableRoster[index], 180);
                reserveCards[index].Rect.anchorMin = reserveCards[index].Rect.anchorMax = new Vector2(0, 1);
                reserveStatus[index] = PrototypeUI.Label("Reserve Status " + index, rosterScroll.content, "候补 · 可上阵", Vector2.zero, new Vector2(180, 24), 16, PrototypeUI.Muted);
                reserveStatus[index].rectTransform.anchorMin = reserveStatus[index].rectTransform.anchorMax = new Vector2(0, 1);
            }
            ApplyCardLayout();
            State = RunState.RestoreFormation();
            for (int index = 0; index < slotButtons.Length; index++)
            {
                int slot = index;
                slotButtons[index].onClick.AddListener(() => SelectSlot(slot));
                slotButtons[index].GetComponentInChildren<Text>().color = PrototypeUI.Hex("D9B979");
                slotCards[index].button.onClick.AddListener(() => SelectSlot(slot));
                previewCards[index].button.onClick.AddListener(() => InspectEnemy(slot));
                previewCards[index].Rect.anchoredPosition = new Vector2((index % 3 - 1) * 252, index < 3 ? -94 : 160);
                slotCards[index].gameObject.AddComponent<FormationCardDrag>().Initialize(this, slotCards[index], slot, false);
            }
            for (int index = 0; index < reserveCards.Length; index++)
            {
                int character = index;
                reserveCards[index].button.onClick.AddListener(() => SelectReserve(character));
                reserveCards[index].gameObject.AddComponent<FormationCardDrag>().Initialize(this, reserveCards[index], character, true);
            }
            enemyButton.onClick.AddListener(OpenEnemy);
            closeEnemyButton.onClick.AddListener(CloseEnemy);
            closeInformationButton.onClick.AddListener(HideInformation);
            removeButton.onClick.AddListener(RemoveSelected);
            if (refreshButton != null) refreshButton.onClick.AddListener(ResetFormation);
            enemyOverlay.SetActive(false);
            informationWindow.SetActive(false);
            presentation = new FormationPresentation(this);
            ClearSelection();
        }

        // Migrate existing scene bindings to the approved portrait preparation layout.
        public void ApplyCardLayout()
        {
            for (int index = 0; index < slotCards.Length; index++)
            {
                PrototypeUI.SetPortraitCard(slotCards[index], 150f);
                slotCards[index].Rect.anchoredPosition = MapPresentation.Point(258 + index % 3 * 300, index < 3 ? 585 : 893);
                slotCards[index].homePosition = slotCards[index].Rect.anchoredPosition;
                PrototypeUI.SetEmptyFormationSlot(slotButtons[index]);
                PrototypeUI.SetPortraitCard(previewCards[index], PrototypeUI.ScoutCardWidth);
            }
            foreach (var card in reserveCards) PrototypeUI.SetPortraitCard(card, 200f / 1.2f);
        }

        public void SelectReserve(int index)
        {
            if (locked || draggedCharacter >= 0 || enemyOverlay.activeSelf || index < 0 || index >= availableRoster.Count) return;
            if (State.FindSlot(index) >= 0) return;
            SelectedIndex = index;
            ShowInformation(availableRoster[index]);
            hint.text = "已选择「" + availableRoster[index].displayName + "」· 点击上方阵位即可上阵或换位";
            Refresh();
        }

        public void SelectSlot(int slot)
        {
            if (locked || draggedCharacter >= 0 || enemyOverlay.activeSelf || slot < 0 || slot >= 6) return;
            if (SelectedIndex >= 0 && State.FindSlot(SelectedIndex) != slot)
            {
                State.Place(SelectedIndex, slot);
                GameAudio.Play(SoundCue.CardDrop);
                RunState.SaveFormation(State);
                hint.text = "「" + availableRoster[SelectedIndex].displayName + "」已就位 · 可以继续布置其他伙伴";
                SelectedIndex = -1;
                InformationOpen = false;
            }
            else if (State[slot] >= 0)
            {
                SelectedIndex = State[slot];
                ShowInformation(availableRoster[SelectedIndex]);
                hint.text = "点击其他阵位即可换位，点击「撤下」可让角色返回候补";
            }
            else hint.text = "先从下方伙伴列表选择角色";
            Refresh();
        }

        public void RemoveSelected()
        {
            if (locked || draggedCharacter >= 0 || enemyOverlay.activeSelf || SelectedIndex < 0) return;
            State.Remove(State.FindSlot(SelectedIndex));
            RunState.SaveFormation(State);
            ClearSelection();
            hint.text = "角色已返回候补 · 点击下方伙伴可重新上阵";
        }

        public void ClearSelection()
        {
            if (locked || draggedCharacter >= 0 || enemyOverlay.activeSelf) return;
            SelectedIndex = -1;
            InformationOpen = false;
            informationWindow.SetActive(false);
            hint.text = "点击布阵或长按候补卡片拖动上阵；拖动已上阵角色可交换位置";
            Refresh();
        }

        public void HideInformation()
        {
            if (locked || draggedCharacter >= 0 || enemyOverlay.activeSelf) return;
            InformationOpen = false;
            informationWindow.SetActive(false);
            Refresh();
        }

        public void ResetFormation()
        {
            if (locked || draggedCharacter >= 0 || enemyOverlay.activeSelf) return;
            State.Reset();
            RunState.SaveFormation(State);
            ClearSelection();
            rosterScroll.StopMovement();
            rosterScroll.verticalNormalizedPosition = 1;
        }

        private void ShowInformation(CharacterDefinition definition)
        {
            if (TutorialRun.Active) TutorialRun.Mark("prep.inspect");
            InformationOpen = true;
            informationWindow.SetActive(true);
            informationWindow.transform.SetAsLastSibling();
            informationTitle.text = definition.displayName + "　／　" + definition.ElementName + "属性 · " + definition.RoleName;
            informationStats.text = "生命　" + definition.maxHealth + "　　攻击　" + definition.attack
                + "　　防御　" + definition.defense + "　　速度　" + definition.speed;
            informationSkill.text = "【" + RarityRules.Name(definition.rarity) + "】" + definition.SkillName + "　·　" + definition.SkillDescription
                + (definition.doubleAttack ? "\n道具加成：普攻二连击，每次行动仅获得1点技力。" : "");
            informationEmblem.role = definition.role;
            informationEmblem.color = PrototypeUI.ElementColor(definition.element);
            informationEmblem.SetVerticesDirty();
            informationAccent.color = PrototypeUI.ElementColor(definition.element);
        }

        private void Refresh()
        {
            for (int slot = 0; slot < slotCards.Length; slot++)
            {
                int character = State[slot];
                slotCards[slot].gameObject.SetActive(character >= 0);
                slotButtons[slot].gameObject.SetActive(character < 0);
                if (character < 0) continue;
                slotCards[slot].Show(availableRoster[character]);
                slotCards[slot].Select(character == SelectedIndex);
            }
            int visibleIndex = 0;
            for (int index = 0; index < reserveCards.Length; index++)
            {
                bool inReserve = State.FindSlot(index) < 0;
                reserveCards[index].gameObject.SetActive(inReserve);
                reserveStatus[index].gameObject.SetActive(inReserve);
                if (!inReserve) continue;
                reserveCards[index].Show(availableRoster[index]);
                reserveCards[index].Select(index == SelectedIndex);
                var position = new Vector2((115 + visibleIndex % 4 * 233) / 1.2f, -135f - visibleIndex / 4 * (349f / 1.2f));
                reserveCards[index].Rect.anchoredPosition = position;
                reserveStatus[index].rectTransform.anchoredPosition = position + new Vector2(0, -170f / 1.2f);
                reserveStatus[index].text = "候补 · 可上阵";
                reserveStatus[index].color = MapPresentation.Muted;
                visibleIndex++;
            }
            float viewportHeight = rosterScroll.viewport.rect.height;
            float contentHeight = Mathf.Max(viewportHeight, Mathf.CeilToInt(visibleIndex / 4f) * (349f / 1.2f) + 16);
            if (!Mathf.Approximately(rosterScroll.content.rect.height, contentHeight))
            {
                rosterScroll.StopMovement();
                rosterScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
                float offset = Mathf.Clamp(rosterScroll.content.anchoredPosition.y, 0, contentHeight - viewportHeight);
                rosterScroll.content.anchoredPosition = new Vector2(0, offset);
            }
            teamCount.text = "出战队伍　" + State.Count + " ／ 6";
            battle.startButton.interactable = State.Count > 0 && !enemyOverlay.activeSelf && draggedCharacter < 0;
            removeButton.interactable = SelectedIndex >= 0 && State.FindSlot(SelectedIndex) >= 0;
            if (presentation != null) presentation.Refresh();
        }

        public void OpenEnemy()
        {
            if (locked || draggedCharacter >= 0 || battle.IsBattling) return;
            rosterScroll.StopMovement();
            InformationOpen = false;
            informationWindow.SetActive(false);
            enemyOverlay.SetActive(true);
            if (TutorialRun.Active) TutorialRun.Mark("prep.enemy");
            enemyOverlay.transform.SetAsLastSibling();
            enemyDetails.text = "点击敌方卡片，查看职业、属性、技能和特性。\n下方一排是敌方前排。";
            for (int slot = 0; slot < previewCards.Length; slot++)
            {
                var enemy = battle.enemyRoster[slot];
                previewCards[slot].gameObject.SetActive(enemy != null);
                if (enemy == null) continue;
                previewCards[slot].Show(enemy);
                previewCards[slot].Select(false);
            }
        }

        public void CloseEnemy()
        {
            enemyOverlay.SetActive(false);
            Refresh();
        }

        public void InspectEnemy(int slot)
        {
            if (locked || !enemyOverlay.activeSelf || slot < 0 || slot >= battle.enemyRoster.Count) return;
            var definition = battle.enemyRoster[slot];
            if (definition == null) return;
            enemyDetails.text = definition.displayName + "　·　" + definition.ElementName + "属性 ／ " + definition.RoleName
                + "\n生命　" + definition.maxHealth + "　攻击　" + definition.attack + "　防御　" + definition.defense + "　速度　" + definition.speed
                + "\n" + definition.SkillName + "　·　" + definition.SkillDescription
                + "\n特性：" + definition.TraitDescription;
            for (int index = 0; index < previewCards.Length; index++) previewCards[index].Select(index == slot);
        }

        public List<CharacterDefinition> LockLineup()
        {
            if (draggedCharacter >= 0 || enemyOverlay.activeSelf || State.Count == 0) return null;
            RunState.SaveFormation(State);
            locked = true;
            rosterScroll.StopMovement();
            return State.Lineup(availableRoster);
        }

        public void EnterBattleMode()
        {
            enemyOverlay.SetActive(false);
            InformationOpen = false;
            informationWindow.SetActive(false);
            preparationRoot.SetActive(false);
        }

        public bool BeginCardDrag(CardView source, int character, PointerEventData eventData)
        {
            if (!CanDrag || character < 0 || character >= availableRoster.Count) return false;
            var parent = (RectTransform)preparationRoot.transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position,
                eventData.pressEventCamera, out Vector2 pointer)) return false;
            draggedCharacter = character;
            GameAudio.Play(SoundCue.CardSelect);
            dragSource = source;
            rosterScroll.StopMovement();
            SelectedIndex = character;
            InformationOpen = false;
            informationWindow.SetActive(false);
            Refresh();
            dropHighlight = PrototypeUI.Panel("Drag Destination", parent, Vector2.zero,
                new Vector2(158, 233), new Color(MapPresentation.Cyan.r, MapPresentation.Cyan.g, MapPresentation.Cyan.b, 0.25f));
            dropHighlight.gameObject.SetActive(false);
            dragPreview = Instantiate(source, parent);
            dragPreview.name = "Dragged Character Preview";
            dragPreview.GetComponent<FormationCardDrag>().enabled = false;
            dragPreview.button.enabled = false;
            dragPreview.group.blocksRaycasts = false;
            dragPreview.group.interactable = false;
            dragPreview.group.alpha = 0.9f;
            dragPreview.Rect.anchorMin = dragPreview.Rect.anchorMax = new Vector2(0.5f, 0.5f);
            dragPreview.Rect.localScale = Vector3.one;
            dragOffset = (Vector2)parent.InverseTransformPoint(source.Rect.position) - pointer;
            dragSource.group.alpha = 0.35f;
            UpdateCardDrag(eventData);
            return true;
        }

        public void UpdateCardDrag(PointerEventData eventData)
        {
            if (draggedCharacter < 0) return;
            if (locked || battle.IsBattling || enemyOverlay.activeSelf)
            {
                CancelCardDrag();
                return;
            }
            var parent = (RectTransform)preparationRoot.transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position,
                eventData.pressEventCamera, out Vector2 pointer))
                dragPreview.Rect.anchoredPosition = pointer + dragOffset;
            int target = FindDropSlot(eventData);
            dropHighlight.gameObject.SetActive(target >= 0);
            if (target < 0)
            {
                hint.text = "拖到上方阵位后松手；在阵位外松手可取消";
                return;
            }
            dropHighlight.rectTransform.position = slotCards[target].Rect.position;
            int origin = State.FindSlot(draggedCharacter);
            hint.text = target == origin ? "松手保留原位" : State[target] < 0 ? "松手放入空阵位"
                : origin >= 0 ? "松手交换两名角色的位置" : "松手替换该角色，原角色返回候补";
        }

        private int FindDropSlot(PointerEventData eventData)
        {
            for (int slot = 0; slot < slotCards.Length; slot++)
            {
                RectTransform target = State[slot] >= 0 ? slotCards[slot].Rect : (RectTransform)slotButtons[slot].transform;
                if (RectTransformUtility.RectangleContainsScreenPoint(target, eventData.position, eventData.pressEventCamera))
                    return slot;
            }
            return -1;
        }

        public void EndCardDrag(PointerEventData eventData)
        {
            if (draggedCharacter < 0) return;
            int target = FindDropSlot(eventData);
            bool placed = !locked && !battle.IsBattling && !enemyOverlay.activeSelf && target >= 0
                && State.Place(draggedCharacter, target);
            if (placed)
            {
                RunState.SaveFormation(State);
                GameAudio.Play(SoundCue.CardDrop);
            }
            CancelCardDrag();
            ClearSelection();
            hint.text = placed ? "布阵完成 · 可继续拖动角色上阵或交换位置" : "已取消拖动，阵形保持不变";
        }

        public void CancelCardDrag()
        {
            if (draggedCharacter < 0) return;
            draggedCharacter = -1;
            if (dragSource != null)
            {
                dragSource.GetComponent<FormationCardDrag>().CancelInteraction();
                dragSource.group.alpha = 1;
            }
            if (dragPreview != null) Destroy(dragPreview.gameObject);
            if (dropHighlight != null) Destroy(dropHighlight.gameObject);
            dragSource = null;
            dragPreview = null;
            dropHighlight = null;
            if (isActiveAndEnabled && preparationRoot.activeInHierarchy) Refresh();
        }

        private void OnDisable()
        {
            CancelCardDrag();
        }

        private void Update()
        {
            if (InformationOpen && !InventoryUI.IsOpen && !enemyOverlay.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                HideInformation();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelCardDrag();
        }
    }
}
