using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// One-time messages, each shown once (kept in the Mail tab; the ids are saved), taking turns so two never land
    /// together (docs/research/story-and-tutorial.md):
    /// tips that teach a mechanic the moment it first matters (the first litter, junk in the pool, a bored lineup, a
    /// rule breaker, a shop customer), and postcards from Uncle Kai, retired and travelling, as the park grows.
    /// </summary>
    public class TutorialTips : MonoBehaviour
    {
        const float Gap = 25f; // seconds between two tips

        static readonly HashSet<string> seen = new();

        /// <summary>Tip ids already shown (saved by <see cref="SaveSystem"/>).</summary>
        public static IEnumerable<string> Seen => seen;

        readonly struct Tip
        {
            public readonly string id, text, from;
            public readonly Func<bool> due;

            /// <param name="from">Where Kai's postcard comes from (null = a plain tip).</param>
            public Tip(string id, Func<bool> due, string text, string from = null)
            {
                this.id = id;
                this.due = due;
                this.text = text;
                this.from = from;
            }
        }

        static bool Owns(string upgradeId) => shop != null && shop.IsOwned(upgradeId);
        static int Level => ParkLevel.Instance != null ? ParkLevel.Instance.Level : 1;
        static int Day => DayCycle.Instance != null ? DayCycle.Instance.Day : 1;
        static UpgradeShop shop;

        static readonly Tip[] Tips =
        {
            new("litter", () => Trash.All.Count > 0,
                "Litter on the deck! Walk up to it and press E to pick it up. Guests hate a messy park."),
            new("lineup_bored", () => AnyBoredInLineup(),
                "Surfers in the lineup are getting bored. Keep the waves coming: press the big button at the console."),
            new("pool_junk", () => PoolDebris.All.Count > 0,
                "Junk floating in the pool. Take the skimmer net from its rack and scoop it out (E). It drifts to the walls, so you can reach it."),
            new("rule_breaker", () => AnyRuleBreaker(),
                "Someone is breaking the park rules! Look at them and press E to throw them out, even from far away."),
            new("shop_customer", () => Shopper.All.Count > 0,
                "A customer at the shop counter! Read the order bubble over their head and make exactly what they asked for."),

            // Postcards from Uncle Kai. {0} = the park's name.
            new("kai_day2", () => Day >= 2, "Heard you sold your first sessions. Told you the magic was still there! Keep the waves coming.", "Bali"),
            new("kai_staff", () => StaffMember.Active.Count > 0, "A boss who hires help is a boss who gets to surf. Just don't let anyone touch my wave machine.", "Fiji"),
            new("kai_intermediate", () => Owns("intermediate_waves"), "Peeling waves at {0}? I'm jealous, and I'm sitting in front of Teahupo'o.", "Tahiti"),
            new("kai_level10", () => Level >= 10, "The locals here are talking about {0}. Somebody called it 'the little pool that could'.", "Hawaii"),
            new("kai_pro", () => Owns("pro_waves"), "Pro waves! The pros will ask for barrels next. Give them barrels.", "Portugal"),
            new("kai_second_pool", () => Owns("pool_reef"), "A reef across the sandbar?! When I left you could barely patch the old pool. I cried a little. Good tears.", "Morocco"),
            new("kai_level20", () => Level >= 20, "A surf camp bus stopped me and asked for directions to {0}. ME!", "Australia"),
            new("kai_home", () => Owns("pool_ranch"), "A foil on a rail, one wave down a 150 m strip?! I saw the photos. I'm dusting off my old board.", "a sailboat"),
        };

        float nextAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => seen.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<TutorialTips>() == null) new GameObject("Tutorial Tips").AddComponent<TutorialTips>();
        }

        public static void Restore(IEnumerable<string> ids)
        {
            seen.Clear();
            if (ids != null) seen.UnionWith(ids);
        }

        static bool AnyBoredInLineup()
        {
            foreach (var surfer in Surfer.All)
                if (surfer.ShowPatience && !surfer.NeedsYou && surfer.Patience < 0.5f) return true;
            return false;
        }

        static bool AnyRuleBreaker()
        {
            foreach (var surfer in Surfer.All)
                if (surfer.IsRuleBreaker) return true;
            return false;
        }

        void Update()
        {
            if (Time.timeScale == 0f || MainMenu.Showing || InputBlocker.Blocked || Time.unscaledTime < nextAt) return;
            nextAt = Time.unscaledTime + 0.5f;
            if (shop == null) shop = FindAnyObjectByType<UpgradeShop>();
            foreach (var tip in Tips)
            {
                if (seen.Contains(tip.id) || !tip.due()) continue;
                seen.Add(tip.id);
                if (tip.from == null) Notifications.Post("Tip: " + tip.text, Notice.Info);
                else
                {
                    Notifications.Post($"Postcard from Uncle Kai ({tip.from}): \"{string.Format(tip.text, ParkName.Name)}\"", Notice.Reward);
                    Sfx.Play(Sound.UiOpen);
                }
                nextAt = Time.unscaledTime + Gap;
                return;
            }
        }
    }
}
