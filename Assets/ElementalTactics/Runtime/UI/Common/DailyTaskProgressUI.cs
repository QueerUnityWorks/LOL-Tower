using UnityEngine;
using UnityEngine.UI;

namespace ElementalTactics
{
    public sealed class DailyTaskProgressUI : MonoBehaviour
    {
        private Text title;
        private Text[] values;
        private DailyTaskDefinition[] tasks;

        public static void Build(Transform parent, Vector2 position, Vector2 size, bool compact = false)
        {
            if (!DailyChallengeState.IsDailyRun) return;
            var panel = JourneyPresentation.Panel("Daily Task Progress", parent, position, size, InventoryPresentation.Surface, MapPresentation.Gold);
            var display = panel.gameObject.AddComponent<DailyTaskProgressUI>();
            display.tasks = DailyChallengeState.Tasks;
            float top = size.y / 2;
            display.title = JourneyPresentation.Label("Daily Progress Title", panel.transform, "", new Vector2(0, top - (compact ? 13 : 18)), new Vector2(size.x - 28, 25), compact ? 15 : 16, MapPresentation.Gold, true);
            display.values = new Text[display.tasks.Length];
            for (int index = 0; index < display.tasks.Length; index++)
            {
                float row = top - (compact ? 36 : 52) - index * (compact ? 24 : 34);
                float width = size.x - 30;
                var label = JourneyPresentation.Label("Daily Task Title " + index, panel.transform, display.tasks[index].Title,
                    new Vector2(-width * .15f, row), new Vector2(width * .7f, compact ? 23 : 30), compact ? 16 : 17);
                InventoryPresentation.FitLine(label, compact ? 13 : 15);
                display.values[index] = JourneyPresentation.Label("Daily Task Value " + index, panel.transform, "",
                    new Vector2(width * .35f, row), new Vector2(width * .3f, compact ? 23 : 30), compact ? 14 : 16, MapPresentation.Cyan, false, TextAnchor.MiddleRight);
            }
            display.Refresh();
        }

        private void OnEnable() { DailyChallengeState.Changed += Refresh; }
        private void OnDisable() { DailyChallengeState.Changed -= Refresh; }
        private void Refresh()
        {
            if (title == null || !DailyChallengeState.IsDailyRun || values == null) return;
            title.text = "每日挑战 " + DailyChallengeState.Current.date + " · 难度10";
            for (int index = 0; index < tasks.Length; index++)
            {
                values[index].text = DailyChallengeState.Progress(tasks[index]);
                var status = DailyChallengeState.Status(tasks[index]);
                values[index].color = status == DailyTaskStatus.Failed ? PrototypeUI.Hex("E4A89A")
                    : status == DailyTaskStatus.Completed ? MapPresentation.Gold : MapPresentation.Cyan;
            }
        }
    }
}
