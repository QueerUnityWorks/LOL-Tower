using System.Collections.Generic;
using System.Linq;

namespace ElementalTactics
{
    public sealed class BattleAudio
    {
        private int bladeIndex;
        private readonly HashSet<Fighter> fallen = new HashSet<Fighter>();

        public void Cast(Fighter actor, bool skill)
        {
            var unit = actor.definition;
            SoundCue cue;
            if (skill && unit.SupportSkill) cue = SoundCue.ArcaneCast;
            else if (skill && unit.element == Element.Fire) cue = SoundCue.FireCast;
            else if (unit.role == Role.Mage) cue = SoundCue.ArcaneCast;
            else if (skill || unit.role == Role.Ranger) cue = SoundCue.WindSlash;
            else cue = bladeIndex++ % 2 == 0 ? SoundCue.BladeOne : SoundCue.BladeTwo;
            GameAudio.Play(cue);
        }

        public void Impact(IReadOnlyList<CombatEffect> effects, Fighter actor = null, bool skill = false)
        {
            bool damage = effects.Any(effect => effect.damage > 0);
            bool absorbed = effects.Any(effect => effect.absorbed > 0);
            if (damage || absorbed)
            {
                var cue = !damage ? SoundCue.HitMetal
                    : actor != null && skill && actor.definition.element == Element.Fire ? SoundCue.FireHit
                    : actor != null && (skill || actor.definition.role == Role.Mage) ? SoundCue.ArcaneHit
                    : effects.Any(effect => effect.damage > 0 && effect.target.enemy) ? SoundCue.EnemyHurt : SoundCue.HitMetal;
                GameAudio.Play(cue);
            }
            if (effects.Any(effect => effect.healing > 0)) GameAudio.Play(SoundCue.Heal);
            else if (effects.Any(effect => effect.shielding > 0)) GameAudio.Play(SoundCue.Shield);
            if (effects.Any(effect => effect.energyLost > 0)) GameAudio.Play(SoundCue.Energy);
            bool enemyFell = false;
            foreach (var effect in effects)
                if (effect.target.enemy && !effect.target.Alive && fallen.Add(effect.target)) enemyFell = true;
            if (enemyFell) GameAudio.Play(SoundCue.EnemyDie);
        }
    }
}
