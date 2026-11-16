using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class ShopUI : MonoBehaviour
    {
        private int nodeId;
        private ScrollRect scroll;
        private Text wallet, level, chances;
        private Button refresh;
        public void Build(int node, ShopRecord shop)
        {
            nodeId = node;
            var description = JourneyPresentation.Label("Shop Description", transform, shop.description, new Vector2(0, 553), new Vector2(798, 56), 21, MapPresentation.Muted);
            InventoryPresentation.FitLine(description, 18);
            wallet = JourneyPresentation.Label("Shop Wallet", transform, "", new Vector2(-115, 491), new Vector2(568, 42), 27, MapPresentation.Gold, true);
            level = JourneyPresentation.Label("Shop Level", transform, "", new Vector2(285, 491), new Vector2(228, 42), 22, MapPresentation.Cyan, true, TextAnchor.MiddleRight);
            chances = JourneyPresentation.Label("Shop Chances", transform, "", new Vector2(0, 422), new Vector2(798, 72), 19, MapPresentation.Muted);
            scroll = InventoryPresentation.Scroll("Shop", transform, new Vector2(0, -83), new Vector2(810, 900), 766);
            refresh = JourneyPresentation.Button("Refresh Shop", transform, "", new Vector2(-208, -618), new Vector2(394, 82), () => { if (ShopRun.Refresh()) { Rebuild(); InventoryPresentation.Top(scroll); } });
            JourneyPresentation.Button("Leave Shop", transform, "结束购物  ›", new Vector2(208, -618), new Vector2(394, 82),
                () => { if (MapRun.Complete(nodeId)) SceneManager.LoadScene("MapScene"); }, true);
            Rebuild(); InventoryPresentation.Top(scroll);
        }
        private void Rebuild()
        {
            float position = scroll.verticalNormalizedPosition;
            wallet.text = "金币 " + RunState.Gold + "  ·  伙伴 " + RunState.Characters.Count;
            level.text = Progression.Label; InventoryPresentation.FitLine(level, 17);
            var config = Progression.Current;
            chances.text = "抽取概率  普通 " + config.commonWeight + "%　稀有 " + config.rareWeight + "%　史诗 " + config.epicWeight + "%\n购买经验升级后，下次刷新使用新概率";
            int price = ShopRun.RefreshPrice(nodeId);
            refresh.GetComponentInChildren<Text>().text = "刷新货物 · " + price + " 金币";
            refresh.interactable = RunState.Gold >= price;
            foreach (Transform child in scroll.content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var offers = ShopRun.Offers(nodeId); float top = 16;
            for (int row = 0; row * 3 < offers.Count; row++)
            {
                float height = 412; var cards = new System.Collections.Generic.List<RectTransform>(); var buys = new System.Collections.Generic.List<Button>();
                for (int col = 0; col < 3 && row * 3 + col < offers.Count; col++)
                {
                    var good = offers[row * 3 + col]; var effect = GameContent.Effects.Get(good.effectId);
                    bool experience = effect.type == EffectKind.AddExperience;
                    Color rarity = experience ? MapPresentation.Gold : InventoryPresentation.Rarity(RarityRules.Of(effect));
                    var card = JourneyPresentation.Panel("Good " + good.id, scroll.content, Vector2.zero, new Vector2(240, 412), null, rarity);
                    JourneyPresentation.TopAnchor(card.rectTransform);
                    JourneyPresentation.EffectArtwork(card.transform, effect, new Vector2(0, 100), new Vector2(214, 176));
                    JourneyPresentation.Label("Good Rarity", card.transform, experience ? "固定 · 经验" : RarityRules.Name(RarityRules.Of(effect)) + " · " + JourneyPresentation.EffectKindLabel(effect), new Vector2(0, -7), new Vector2(214, 30), 18, rarity);
                    var name = JourneyPresentation.Label("Good Name", card.transform, good.name, new Vector2(0, -49), new Vector2(214, 46), 24, null, true);
                    InventoryPresentation.FitLine(name, 19);
                    var detail = JourneyPresentation.Label("Good Effect", card.transform, JourneyPresentation.EffectDetail(effect), new Vector2(0, -84), new Vector2(214, 74), 18, MapPresentation.Muted, false, TextAnchor.UpperLeft);
                    detail.rectTransform.pivot = new Vector2(.5f, 1);
                    float detailHeight = Mathf.Max(72, detail.preferredHeight); detail.rectTransform.sizeDelta = new Vector2(214, detailHeight);
                    height = Mathf.Max(height, 84 + detailHeight + 86 + 206);
                    int stock = ShopRun.Stock(nodeId, good); bool capped = experience && Progression.IsMaxLevel;
                    var buy = JourneyPresentation.Button("Buy " + good.id, card.transform, capped ? "已满级" : stock <= 0 ? "售罄" : good.price + " 金币 · 剩" + stock, Vector2.zero, new Vector2(214, 54),
                        () => { if (ShopRun.Buy(good.id)) Rebuild(); }, true);
                    buy.interactable = !capped && stock > 0 && RunState.CanApply(new[] { effect }, good.price);
                    cards.Add(card.rectTransform); buys.Add(buy);
                }
                for (int col = 0; col < cards.Count; col++)
                {
                    var card = cards[col]; card.sizeDelta = new Vector2(240, height); card.anchoredPosition = new Vector2((col - 1) * 253, -top - height / 2);
                    card.Find("Frame").GetComponent<RectTransform>().sizeDelta = card.sizeDelta;
                    foreach (Transform child in card) if (child.name != "Frame" && child != buys[col].transform) ((RectTransform)child).anchoredPosition += new Vector2(0, (height - 412) / 2);
                    buys[col].GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -height / 2 + 40);
                }
                top += height + 18;
            }
            scroll.content.sizeDelta = new Vector2(766, Mathf.Max(scroll.viewport.rect.height, top + 8));
            Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = position;
        }
    }
}
